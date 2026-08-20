# Contracts (Task) System — Design & Technical Reference

## Purpose

The Contracts system (surfaced to players as the "Task" panel) guides new
players through early server features and areas. Contracts are granted
automatically as players explore, talk to NPCs, and complete quests, and
completion is likewise detected automatically from the player's underlying
quest-flag state — there is no separate "turn in a contract" action.

## Data model

Contract *definitions* are static game data, read once at server startup.
Per-character contract *state* is minimal and persisted; the actual
progress/completion status is never stored directly — it's re-derived live
from the character's quest flags every time it's needed.

### Contract definitions (DAT file, static)

Source: `client_portal.dat`, file `0x0E00001D`, table type `ContractTable`
(`libs/dat-loader/FileTypes/ContractTable.cs`). Loaded once into
`DatManager.PortalDat.ContractTable.Contracts` (a `Dictionary<uint, Contract>`)
via `PortalDatDatabase`'s constructor (`libs/dat-loader/PortalDatDatabase.cs:13`).

Each `Contract` (`libs/dat-loader/Entity/Contract.cs`) has:

| Field | Meaning |
|---|---|
| `ContractId` | Numeric ID, matches `ACE.Entity.Enum.ContractId` |
| `ContractName` | Display name shown in the Task panel |
| `Description` / `DescriptionProgress` | Flavor text (progress text may contain a `%d` placeholder for the current count) |
| `NameNPCStart` / `NameNPCEnd` | NPC(s) associated with starting/ending the contract (flavor only — not enforced by server logic) |
| `QuestflagStarted` | Quest flag whose presence marks the contract "in progress" |
| `QuestflagStamped` | Alternate/additional "in progress" flag |
| `QuestflagProgress` | Quest flag whose *solve count* drives a counter-style contract; some contracts in the dat use this with no separate `QuestflagFinished` at all (none of the 39 contracts this server actually uses are like this — see gotchas below) |
| `QuestflagFinished` | Quest flag whose presence marks the contract fully done |
| `QuestflagTimer` | Quest flag used for a timed contract's remaining time |
| `QuestflagRepeatTime` | Quest flag used to compute when a repeatable contract becomes available again |
| `LocationNPCStart/End/QuestArea` | Positions (unused by current server-side logic, present for client/tooling reference) |

A contract is **not required** to use every flag — most only set one or two
(e.g. just `QuestflagStarted` + `QuestflagFinished`, or just `QuestflagProgress`
for a pure counter). See the full catalog at the bottom of this document.

### Per-character contract registry (DB, persisted)

Table `character_properties_contract_registry`
(`Database/Base/ShardBase.sql:633`), model
`ACE.Database.Models.Shard.CharacterPropertiesContractRegistry`:

```
character_Id            -- owning character
contract_Id             -- which Contract (DAT ContractId)
delete_Contract          -- bit flag: tells the client to drop this from the list
set_As_Display_Contract  -- bit flag: legacy/deprecated "pinned" flag
```

That's it — **no stage, progress count, or completion timestamp is stored**.
The registry only records *which contracts the character has been granted*.
Everything about a contract's current state (in progress / done / how many
solves so far / time remaining) is computed fresh, on demand, from the
character's live quest-flag data (see next section).

### Live state derivation (`ContractTracker`)

`apps/server/Network/Structure/ContractTracker.cs` computes the client-facing
state for a single contract, built fresh every time one is sent:

```csharp
public enum ContractStage
{
    Available = 0x1,
    InProgress = 0x2,
    DoneOrPendingRepeat = 0x3,
    ProgressCounter = 0x4   // + N, e.g. ProgressCounter + 2 == "2 of N" for the client
}
```

`ContractTracker`'s constructor (`Init`/`CheckAndSetStage`) queries
`Player.QuestManager` directly — `HasQuest`, `IsMaxSolves`, `GetQuest(...).NumTimesCompleted`,
`GetNextSolveTime` — to decide the stage:

- `QuestflagStarted` or `QuestflagStamped` present → `InProgress`.
- `QuestflagTimer` present → `InProgress`, with `TimeWhenDone` set.
- `QuestflagProgress` present → `ProgressCounter + NumTimesCompleted`, or
  `DoneOrPendingRepeat` once `QuestManager.IsMaxSolves(QuestflagProgress)` is true.
- `QuestflagFinished` present → `DoneOrPendingRepeat`.
- `QuestflagRepeatTime` present and satisfied → `DoneOrPendingRepeat` with
  `TimeWhenRepeats` set; once the repeat window has actually elapsed, stage
  is recomputed as if freshly available again.

Because this is recomputed from scratch on every send, the Task panel's
progress count and Done/not-Done state are always consistent with the
character's actual quest-flag data — they cannot drift out of sync with the
DB registry.

## Server-side components

### `ContractManager` (`apps/server/WorldObjects/Managers/ContractManager.cs`)

One instance per `Player` (`Player.cs`, field `ContractManager`, constructed
in `SetEphemeralValues()`). Responsibilities:

- `MonitoredQuestFlags`: a `Dictionary<uint contractId, HashSet<string> questFlags>`
  built by `RefreshMonitoredQuestFlags()`, listing every quest flag (`Started`,
  `Stamped`, `Finished`, `Progress`, `Timer`, `RepeatTime`) relevant to each
  contract the player currently holds. Rebuilt whenever the registry changes
  (grant/abandon/erase).
- `Add(contractId)` — grants a new contract: creates the registry row, sends
  a tracker update, a chat line, and a popup (see **Client notification**
  below), then immediately checks whether the completion condition is
  *already* satisfied (e.g. granting a contract for a quest the player has
  already solved) and calls `Update(...)` right away if so.
- `Update(contractId, triggeringQuestName)` — called whenever a *monitored*
  quest flag changes for this player (see `NotifyOfQuestUpdate` below). Always
  refreshes the client's tracker for that contract. If the flag that changed
  is either (a) an exact match for `QuestflagFinished`, or (b) a match for
  `QuestflagProgress` where `QuestManager.IsMaxSolves(...)` is now true, it
  also sends the "Task Completed" chat line + popup.
- `NotifyOfQuestUpdate(questName)` — called by `QuestManager` (see below)
  every time any quest flag on the player changes. Looks up
  `MonitoredQuestFlags` for any contract watching that flag and calls
  `Update(contractId, questName)`.
- `CheckAndBestowContractsOnQuestStamp(questName)` — called on quest stamp;
  scans the whole DAT `ContractTable` for any contract whose
  `QuestflagStarted` matches the just-stamped flag and the player doesn't
  already have, and grants it via `Add(...)`.
- `HandleContractsOnLogin()` — on login, re-checks every contract the player
  already holds and calls `Update(contractId)` (no triggering flag, so no
  "completed" chat/popup spam) for any that are already satisfied — this
  just makes sure the client's tracker list is in sync after a session gap,
  without renotifying for things completed in a previous session.
- `Abandon`/`Erase`/`EraseAll` — remove a contract from the registry; the
  client is told via `DeleteContract = true` on the next tracker send.

### `QuestManager` integration (`apps/server/Managers/QuestManager.cs`)

Every place a quest flag is stamped/updated for a player
(`QuestManager.Update`, `Stamp`, `Increment`, etc. — lines 226, 263, 476, 496,
676, 699, 735) calls `player.ContractManager.NotifyOfQuestUpdate(questName)`
after the quest flag itself changes. This is the single hook point that
drives *all* contract progress/completion — contracts never poll or get
directly notified by whatever gameplay system stamped the quest; they only
ever react to "some quest flag changed."

### `Player_Contracts.cs`

Handles the client's abandon-contract action
(`HandleActionAbandonContract`). A hardcoded `NonAbandonableContracts` set
(21 IDs — the Holtburg/Shoushi/Yaraq newbie city progression: Training
Academy, Town Elder, Rumors, Trade Alliance, Trophy Pouch, Horn of Hometown,
Capital City, Town Attunement, Portal Magic, Sigil Slot) blocks abandoning
unless that contract's tracker stage is already `DoneOrPendingRepeat` — these
are treated as mandatory progression, not optional side content.

## Client notification (chat + popup)

Two things happen client-side whenever a contract is granted or completed,
from `ContractManager.Add()` and `ContractManager.Update()`:

1. **Chat line** — `GameMessageSystemChat`, colored pink via
   `ChatMessageType.CombatSelf` (the only chat type documented as pure "Pink
   Text" in `ACE.Entity.Enum.ChatMessageType`; chosen deliberately to stand
   out from ordinary white/system text since there's no dedicated "task"
   chat channel).
   - `"You have received a new task: {ContractName}."`
   - `"You have completed the task: {ContractName}."`
2. **Popup** — routed through `Player.PopupManager`
   (`apps/server/WorldObjects/Managers/PopupManager.cs`), which wraps
   `GameEventPopupString` (opcode `GameEventType.PopupString`, the same
   plain OK-dismiss message box used for the login MOTD and the Scribing
   Table's help text).

### Why popups are batched

The AC client can display multiple popups stacked on top of each other —
they don't clobber one another — but some moments (logging in with several
starter contracts already satisfied, or turning in a quest that completes
one contract and grants several more in a chain) can fire ten or more of
these in the same instant. Showing that many separate stacked boxes is poor
UX on its own merits, independent of any client limitation.

`PopupManager` batches: any popups queued within a 2-second window
(`BatchWindowSeconds`) are merged into a single `GameEventPopupString`, one
line per event, joined with blank lines. Within a batch, **all "Task
Completed" lines are listed before any "New Task" lines**, regardless of the
order the underlying events actually fired in — `EnqueueTaskCompleted(...)`
and `EnqueueNewTask(...)` append to two separate lists, and `Flush()`
concatenates completed-first, new-task-second.

```
Enqueue*(...) → add to the relevant list → if no flush is already scheduled,
                schedule one via ActionChain.AddDelaySeconds(2.0)
Flush()       → join completed[] then newTasks[], clear both lists,
                send one GameEventPopupString if there's anything to send
```

This mirrors the existing pattern in `WorldManager.cs` (login MOTD/welcome
text), which already concatenates multiple text blocks into one popup rather
than sending several.

## Known gotchas / design notes

- **Progress-based completion is a two-place check.** A contract using only
  `QuestflagProgress` (no separate `QuestflagFinished`) completes when
  `QuestManager.IsMaxSolves(QuestflagProgress)` flips true. This has to be
  checked both in `Add()` (a freshly-granted contract might already be
  satisfied — e.g. re-granting after already having solved the underlying
  quest) and in `Update()` (an already-held contract crossing the threshold
  on a later stamp). Missing the `Update()` half of this check was a real
  bug fixed on this branch — the tracker's *displayed* progress count still
  updated correctly on every stamp (since `ContractTracker` recomputes stage
  independently, see above), but the "Task Completed" chat/popup notification
  never fired for pure-progress contracts once the counter reached max,
  because `Update()` only compared the triggering flag against
  `QuestflagFinished`. **This doesn't affect any of the 39 contracts this
  server actually uses** — every one of IDs 323–361 sets a real
  `QuestflagFinished` (see catalog below), so none of them are pure-progress.
  The fix is still correct general `ContractManager` behavior and matters if
  this server ever enables one of the legacy progress-only contracts (e.g.
  the various "Kill:"/"Golem Hunters:" entries below ID 323 in the full dat).
- **`MaxSolves` itself lives outside this system.** If a counter-style
  contract's Task panel entry genuinely never flips to Done despite the
  count increasing, check that quest flag's `MaxSolves` value in the World
  DB quest table (`QuestManager.IsMaxSolves` reads `quest.MaxSolves` from
  there) — a `MaxSolves` of `-1` (unlimited) or a mismatched count is a data
  issue, not a `ContractManager` bug.
- **"Complete 3 Rumors" (ID 330) is not actually progress-only** — per the
  catalog below it has `QuestflagStarted = ArrivedStarterTown`,
  `QuestflagProgress = QuestStarterTown`, *and* `QuestflagFinished =
  AttunedStarterTown`. So its real completion signal is `AttunedStarterTown`
  getting stamped, not `QuestStarterTown` reaching 3 solves — those are two
  independent flags. If this contract still won't flip to Done, the
  `Update()` fix above isn't the relevant path; check whatever quest/emote
  content is supposed to stamp `AttunedStarterTown` once the three
  town-specific Rumors contracts (327–329) are turned in — that's a content
  script, not `ContractManager` logic.
- **Stage is never trusted from the DB.** Because the registry only stores
  `contract_Id`/`delete_Contract`/`set_As_Display_Contract`, there is no way
  for stored state to drift from actual quest progress — the tradeoff is
  that every `ContractTracker` construction does several `QuestManager`
  lookups, which is fine at contract-grant/quest-stamp frequency but would
  not scale to being called every tick.

## Appendix: full contract catalog

Extracted directly from this server's `client_portal.dat`
(`C:\ACE2\Dats\client_portal.dat`, `ContractTable` file `0x0E00001D`).
The dat file defines 361 contracts total, but this server only uses
**IDs 323 and up (39 contracts)** — the Holtburg/Shoushi/Yaraq newbie
progression plus the crafting-skill and combat-ability unlocks that
follow it. Everything below ID 323 is legacy/event content left over in
the dat and not part of the live task flow, so it's omitted here. Blank
cells mean that flag is unused for that contract.

| ID | Contract Name | QuestflagStarted | QuestflagProgress | QuestflagFinished | QuestflagStamped | QuestflagTimer | QuestflagRepeatTime |
|---|---|---|---|---|---|---|---|
| 323 | Training Academy | AcademyStart | QuestAcademy | AttunedStarter |  |  |  |
| 324 | Town elder | AttunedStarterAluvian |  | ArrivedStarterTown |  |  |  |
| 325 | Town elder | AttunedStarterSho |  | ArrivedStarterTown |  |  |  |
| 326 | Town elder | AttunedStarterGharu |  | ArrivedStarterTown |  |  |  |
| 327 | Rumors | ArrivedHoltburg |  | StarterRumorPurchased |  |  |  |
| 328 | Rumors | ArrivedShoushi |  | StarterRumorPurchased |  |  |  |
| 329 | Rumors | ArrivedYaraq |  | StarterRumorPurchased |  |  |  |
| 330 | Complete 3 rumors | ArrivedStarterTown | QuestStarterTown | AttunedStarterTown |  |  |  |
| 331 | Trade Alliance Quest | AttunedHoltburg | TradeAllianceTask | NewbieTradeAlliance |  |  |  |
| 332 | Trade Alliance Quest | AttunedShoushi | TradeAllianceTask | NewbieTradeAlliance |  |  |  |
| 333 | Trade Alliance Quest | AttunedYaraq | TradeAllianceTask | NewbieTradeAlliance |  |  |  |
| 334 | Trophy Pouch | NewbieTradeAlliance |  | TrophyPouchAcquired |  |  |  |
| 335 | Horn of Hometown | NewbieTradeAlliance |  | HornOfHometownComplete |  |  |  |
| 336 | Capital: Cragstone | NewbieTradeAlliance |  | ArrivedCragstone |  |  |  |
| 337 | Capital: Hebian-to | NewbieTradeAlliance |  | ArrivedHebianTo |  |  |  |
| 338 | Capital: Zaikhal | NewbieTradeAlliance |  | ArrivedZaikhal |  |  |  |
| 339 | Town Attunement | ArrivedCragstone | QuestCragstone | AttunedCragstone |  |  |  |
| 340 | Town Attunement | ArrivedHebianTo | QuestHebianTo | AttunedHebianTo |  |  |  |
| 341 | Town Attunement | ArrivedZaikhal | QuestZaikhal | AttunedZaikhal |  |  |  |
| 342 | Portal Magic | HornOfHometownComplete |  | PortalMagic1 |  |  |  |
| 343 | Crafting skill: Alchemy | HornOfHometownComplete | TradeSkill | TwoTradeSkillsLearned |  |  |  |
| 344 | Crafting skill: Blacksmithing | HornOfHometownComplete | TradeSkill | TwoTradeSkillsLearned |  |  |  |
| 345 | Crafting skill: Cooking | HornOfHometownComplete | TradeSkill | TwoTradeSkillsLearned |  |  |  |
| 346 | Crafting skill: Jewelcrafting | HornOfHometownComplete | TradeSkill | TwoTradeSkillsLearned |  |  |  |
| 347 | Crafting skill: Spellcrafting | HornOfHometownComplete | TradeSkill | TwoTradeSkillsLearned |  |  |  |
| 348 | Crafting skill: Tailoring | HornOfHometownComplete | TradeSkill | TwoTradeSkillsLearned |  |  |  |
| 349 | Crafting skill: Woodworking | HornOfHometownComplete | TradeSkill | TwoTradeSkillsLearned |  |  |  |
| 350 | Ability: Aegis | HornOfHometownComplete |  | AbilityLearnedAegis |  |  |  |
| 351 | Ability: Backstab | HornOfHometownComplete |  | AbilityLearnedBackstab |  |  |  |
| 352 | Ability: Battery | HornOfHometownComplete |  | AbilityLearnedBattery |  |  |  |
| 353 | Ability: Evasive Stance | HornOfHometownComplete |  | AbilityLearnedEvasiveStance |  |  |  |
| 354 | Ability: Fury | HornOfHometownComplete |  | AbilityLearnedFury |  |  |  |
| 355 | Ability: Mana Barrier | HornOfHometownComplete |  | AbilityLearnedManaBarrier |  |  |  |
| 356 | Ability: Provoke | HornOfHometownComplete |  | AbilityLearnedProvoke |  |  |  |
| 357 | Ability: Reflect | HornOfHometownComplete |  | AbilityLearnedReflect |  |  |  |
| 358 | Ability: Relentless | HornOfHometownComplete |  | AbilityLearnedRelentless |  |  |  |
| 359 | Ability: Riposte | HornOfHometownComplete |  | AbilityLearnedRiposte |  |  |  |
| 360 | Ability: Smokescreen | HornOfHometownComplete |  | AbilityLearnedSmokescreen |  |  |  |
| 361 | Ability: Steady Strike | HornOfHometownComplete |  | AbilityLearnedSteadyStrike |  |  |  |