# Looking for group

Players can find a group, and fellowships can find more players, with `/lfgroup` and `/lfmore`. A player who is looking for a group (**lfg**) joins the first fellowship that is looking for more for the same thing (**lfm**), as soon as there is one. Nothing needs a plugin. Admins can turn it all off with `lfg_matching_enabled`.

They aren't `/lfg`, `/group` or `/fellow`: the client keeps `/lfg` and `/clfg` for the LFG channel, and `/party`, `/g`, `/group`, `/fellow`, `/fellows` and `/fellowship` for fellowship chat (the command words in `acclient.exe`), so those never reach the server. The LFG channel is still where people see it: each new lfg or lfm is posted there, under the player's name, in a form everyone reads the same way:

```
[LFG] Holmez says, "LFG Green Mire Grave d10 (level 34) - to invite: /lfmore invite Holmez"
[LFG] Arwen says, "LFM Green Mire Grave d10 (3/9, levels 30-35) - to join: /lfgroup join Arwen"
```

## Commands

| Command | Does |
|---|---|
| `/lfgroup <activity>` | Looks for a group. You join the first fellowship whose lfm fits, the longest-waiting first, as soon as there is one. You can't be in a fellowship already. A new lfg replaces your last. |
| `/lfgroup join <name>` | Joins the fellowship that player is in (its leader or anyone else in it, or whoever posted its lfm, so an old line still works after leadership changes hands), if it's looking for more and you fit what it's looking for (the same check as an lfg for it). For a `manual` lfm, you're then looking for a group for it, and the leader is asked about you now. |
| `/lfgroup list` | The players looking for a group, and how to invite each. |
| `/lfgroup stop` | Stops your lfg. |
| `/lfmore <activity> [manual]` | (Fellowship leaders) Looks for more. Players whose lfg fits join at once, or with `manual` you're asked yes or no about each, one at a time. A new lfm replaces your fellowship's last. |
| `/lfmore invite <name>` | The same as `/invite`: the game's own fellowship invitation. It works with looking for group turned off. |
| `/lfmore list` | The fellowships looking for more, and how to join each. |
| `/lfmore stop` | Stops your fellowship's lfm. |
| `/lfgroup`, `/lfmore` | How to use them, and what you're looking for now. |

**Answering a line.** Each line ends with what to type to answer it: `/lfgroup join <name>` on an lfm, `/lfmore invite <name>` on an lfg. The player plugin finds those and puts a `[Join Arwen]` or `[Invite Holmez]` link on the end, for its own players only; everyone else sees the line as it was posted, and can type the command.

**How often lines are posted.** So the LFG channel doesn't fill up, a player's line for the same thing (the same activity, as lfg or as lfm, "lfg Green Mire Grave d10") is posted at most once every `lfg_post_interval_seconds` (300), and any of their lines at most once every `lfg_post_any_interval_seconds` (60), so looking for something different is posted sooner. Their lfg or lfm still changes every time, and matching goes on as ever: only the line waits, and they're told when it can go out again. Stopping and starting again doesn't reset either. 0 turns a limit off.

## Activities

| Activity | lfg | lfm |
|---|---|---|
| `dungeon` or `capstone` | Any capstone you're level enough for. A number (`dungeon 30`) only up to that capstone level, a difficulty (`d5`) at least that, `d5-10` a range. | A capstone, not yet said which, at that difficulty (`dungeon d10`). |
| A capstone by name (`green mire grave`, `gmg`, `hoth`, `mod`...) | That capstone, at exactly that difficulty (`gmg d10`), or at least (`gmg d10+`), or any (`gmg`). You must be the level its portal lets in from. | That capstone, at that difficulty (`gmg d20`, or none). The leader must be its level. |
| `leveling` | A fellowship that would share XP evenly with you in it (`Fellowship.CalculateXPSharing`'s rules: everyone at `fellowship_even_share_level` or above, or within 5 levels of the lowest, Shrouded counting as the lowest up to 25 above it). | The same. |
| `quest [town]` | Quests (in that town). `quest` on its own fits any. | The same. |
| Anything else | Free text, matched by its words either way round: `olthoi` fits `olthoi north`, and so does `the olthoi north`. | The same. |

The capstones, their levels and short names are `LfgCapstones` (the levels are the entrance portals' `MinLevel` in world-db). A difficulty is the Dungeon: Difficulty mod, d1 to d20. An lfm's difficulty is the leader's word: it comes from the DungeonModders the leader uses when the fellowship gets the dungeon, and nothing checks the two agree.

## When they end

Nothing is saved, and nothing runs out. An **lfg** lasts until the player stops it, joins any fellowship (through LFG or not) or logs out. An **lfm** belongs to the fellowship, so it carries on if the leader changes, and lasts until the leader stops it or the fellowship is full, locked or disbanded. The leader is told when it ends.

**Capstones lock the fellowship.** When a fellowship's run of a capstone starts (its copy or instance of the dungeon is made: `Landblock.LockForCapstone`), the fellowship is locked, so its lfm ends and nobody new can be recruited. Members who leave can be recruited back for a while (`Fellowship.DepartedMembers`). A fellowship stays locked until it disbands, as locks always have.

## How it works

- `/lfgroup` and `/lfmore` (`LfgCommand`) hand their words to `LfgManager.HandleCommand`. `list`, `stop` and `help` only count on their own, so an activity can start with one of them. The lfg and lfm lines are posted by `TurbineChatHandler.PostToLfgChannel`, to everyone listening to LFG who hasn't squelched the player, under the same rules as a line they type there (gagged, the arena, Olthoi, the account and character age and level rules, `chat_disable_lfg`): a player who can't send to LFG still gets matched, and is told their line wasn't posted.
- `LfgManager` keeps the lfgs (`LfgSeeker`) and lfms (`LfgListing`), and matches them when one is made and every second (`Tick`, from `WorldManager`), where it also drops the ones that have ended. `LfgParser` reads the activities and `LfgMatching` decides what fits, apart from players and fellowships, so `LfgTests` can test them. Players who have squelched each other aren't matched, and neither is anyone in the arena.
- Joining is `Fellowship.AddConfirmedMember`, as if the leader had recruited the player and they'd said yes: asking for the group was their yes. The fellowship is told who joined through LFG. A manual lfm's question is a `Confirmation_Lfg`; a no (or no answer) means that player isn't put to that lfm again.
- Command replies are System chat, so the player plugin keeps them as the command's reply.
- `@playerui lfg` (`LfgManager.GetPlayerUiReport`) is the player plugin's Find a group tab: what the player or their fellowship is looking for, why they can't post an lfg or lfm now, both lists (with whether each fits them) and the capstones. Unlike the rest of `@playerui` it names other players, but only with what their own lines told the LFG channel and what `/lfgroup list` and `/lfmore list` tell anyone.
- Everything changes under one lock. Fellowships are only changed on the world thread, like the Fellowship panel's own buttons: commands and a leader's yes come from a client's message, and `Tick` from `WorldManager`.

## For later

- Posting open lfms again now and then, so they don't scroll out of sight.
- Checking an lfm's difficulty against the DungeonModders the leader carries.
