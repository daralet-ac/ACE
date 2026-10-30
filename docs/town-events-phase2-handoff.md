# Town events: handoff

Phases 1 and 2 are built and staged (2026-10-26), with some of phase 3 pulled forward. This file is the starting point for
a new session.

## Read first

- **The design doc:** "Town Event System — Design" (Downloads, or the claude.ai artifact). Its phased rollout is the
  plan; where we changed a decision, it's listed under "Changed from the design doc" below and the code wins.
- **What's built:** `docs/town-events.md`, the source of truth for how it works now, and its "Not built yet" list.
- **The code:** `apps/server/TownEvents/`.

| File | What it is |
| --- | --- |
| `TownEventManager` | Unrest, triggering, the hook queue, reward claims, commands |
| `TownEvent` | One attack: families, waves, the chest raid, scoring, rewards |
| `TownSupply` | Guard supply levels, the drain, the minimum hold, saving (`town_events_supply`) |
| `TownCaptain` | The Captain of the Guard: report, donations, the donor XP buff, claims |
| `TownSupplyChest` | Finds a town's Supply Chest |
| `TownGuardUpgrades` | Dresses guards for the supply level: chainmail or kit, colours, armor level, ward, weapon damage and buffs |
| `TownArchetypeMods` | Warlord, Elite and Raider scaling by supply level |
| `TownCreaturePool` | The creature pool, with casters and humanoids marked |
| `TownEventRecorder` | The event log (`town_event`, `town_event_participant`) |
| `TownGeography` | Regions, roads in, finding guards and objects near the centre |

## Where things stand

Everything is staged, nothing committed. The user commits with GitHub Desktop: leave work staged, never commit. New
branches: `git switch -c <name> --no-track origin/main`.

| Repo | Branch | State |
| --- | --- | --- |
| daralet-ACE | `feature/town-events` | Staged: phases 1 and 2, supply, Captain, guard upgrades, chest raids, the event log |
| daralet-admin-plugin | `feat/town-events-tab` | Staged: the Town Events tab (one Towns list with unrest and supply, a details panel), `Widgets.Literal` on every ImGui text call |
| world-db | `feat/town-events-phase2` | Staged: Holtburg's Captain (2020058) and Supply Chest (2020059) and their placement in `A9B4 Holtburg.sql`, and chainmail added to all 240 town guards. `scripts/town-guards/` (with the chainmail in `town_guards.py`) is untracked |
| daralet-ACE | `spike/outdoor-navmesh` (worktree `.claude/worktrees/reverent-turing-4fc3ee`) | A navmesh spike from another session, not hooked in |

The user's dev server imports from `dist/apps/server/net10.0/Content/sql` (weenies, landblocks, and
`weenies/Town Guards`). The Captain, the chest, the Holtburg landblock and all 240 guards have been copied there.

## Phase status (against the design doc's rollout)

**Phase 1 (MVP):** done.

**Phase 2 (stakes and the Captain):**

- Done: the Captain and claims, the Supply Chest objective and fail state, the Warlord, healing and tanking stats,
  creature families.
- Placeholder: the leader-corpse reward. The Warlord's pyreals are paid straight to defenders on its death.
- Simplified: the variant cost is toughness × lethality, not the doc's `TierPower` kill-time and damage ratios.
- Not built: the Captain as a raid objective.

**Phase 3 (breadth):**

- Done early: guard donations and upgrades, the admin UI section, the event log.
- Partly: the Warlord archetype (every raid's last wave, no names or escort); all 30 towns (guards and chainmail are
  everywhere, the Captain and chest only in Holtburg).
- Not built: the Infiltration archetype, the capitals' square guards, more guards where routes leave gaps, the player
  plugin scoreboard.

**Smaller items not built:** the fast guard respawn, reduced kill XP and loot for ordinary attackers, leaders'
nearby-player scaling, loot caches (and the top players' bonus caches), the `Quest{Town}` stamp on rewards, saving
unrest across restarts, keeping attackers off lifestones and portals, counting only recently active players for wave
size, the Celdon, Koujia and Amuli palettes.

## Changed from the design doc

- **Supply makes raids more frequent:** each level takes 5% off the unrest threshold (level 10 halves it). The doc had
  it raising the threshold.
- **Level costs:** level 1 is 500 at tier 0, then 1,000 × tier²; each level costs 10% more than the last, rounded to
  50. The doc had 1,000 × T² growing 1.5×.
- **Weapon buffs at 7–10:** Defender, Heart Seeker, Blood Drinker, Biting Strike (the original spells, level by tier,
  and a crit chance). Swift Killer does nothing on monsters, whose attack timing ignores weapon speed.
- **Guard stats:** worn armor counts, at the tier's Covenant base in chainmail (50 at tier 0) up to double in gold, and
  a guard takes the higher of its archetype armor and its worn armor. Ward scales the same; weapon damage ×1.15, 1.3,
  1.5 with colour.
- **Donor buff:** 10% XP for an hour, for a gift of at least level 1's full cost, and only for players in the town's
  own level band. It isn't a spell (a new one needs the client dat).
- **Looting the chest** also doubles the next unrest threshold.
- **Chest raids** use only humanoids and the undead (`TownCreaturePool.HumanoidTypes`); every attack mixes casters and
  fighters.
- **Rewards:** completion and claims pay XP only; the pyreals come from the Warlord.

## Open questions

- Non-chest attacks can still be any creature. Restrict them to humanoids, or give beast attacks a thematic reason?
  (Asked, not answered.)
- The leader-corpse reward and its sizes.
- The Celdon, Koujia and Amuli looks.
- Whether the client handles a fellowship of more than 9 (the server cap is lifted).
- The capitals' square guards, and further over-level deterrents if the kill log shows a need.

## Building and testing

- **Locked output:** the dev server locks `dist/apps/server`. Build to another folder with `-p:OutDir=<scratch>/`;
  the plugin builds to `bin` while the game runs with the loader.
- **Never pipe `dotnet build`/`test`:** use `NX_DAEMON=false` and redirect to a file.
- **Expected test failures:** with `OutDir` redirected, 5 server tests always fail on environment problems
  (`StartupTests` ×3, `Sphere_*` ×2).
- **In game:**
  - `@townevent start <town> [chest | nochest]`, `stop`, `status`, `unrest`, `supply [<town> [<amount> | level <n>]]`,
    `history [<town>] [<count>]`
  - `/towndefense join`
  - the admin plugin's Town Events tab
  - Logs start with `[TOWN EVENT]` (`C:\ACE\Logs`).
- **Imports:** `@import-sql <wcid>`, `@import-sql landblock <id>`, then `@reload-landblock`.
- **The event log table** comes from `Database/Updates/Shard/2026-10-26-00-Town-Event-Log.sql` (note the capital D in
  git), applied by the server's database updates.

## Conventions

- Braces on every `if`.
- Comments explain why, in plain words.
- In the plugin, every ImGui text call that takes non-constant text goes through `Widgets.Literal`.
- Keep `docs/town-events.md` and its "Not built yet" list current.
