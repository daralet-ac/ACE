# Rested experience

Players build up **rested experience** while they're away from the fight, and spend it on their next kills: while they
have some, kill XP earns a bonus (100% more by default), and the bonus is taken out of their rested experience until it's
gone.

## Building it up

- **Logged out**, anywhere: the time since logout counts in full.
- **In a town** out of combat mode: the town's landblock (`Town.TownLandblockIds`, the same check the road speed buff
  uses), its buildings included, never an instanced copy. Combat mode stops it until it's put away.
- It grows by `rested_xp_levels_per_hour` of the player's current level's XP cost each hour (1% to start: about a quarter
  of a level a day), up to `rested_xp_max_levels` of it (a level and a half). The most grows as they level; a pool already
  over the most (the properties lowered) isn't cut back.
- Nothing builds up at the highest level when `allow_xp_at_max_level` is off: there's no XP to spend it on.

## Spending it

Kill XP (`XpType.Kill`) and a fellow's share of a kill (`XpType.Fellowship` from a kill, `killShare` in `GrantXP`) earn
`rested_xp_bonus` more, up to what's left. The bonus is worked out after every other modifier (the server's
`xp_modifier`, buffs, augmentations, the alt and regional bonuses, the overlevel penalty), so it's the XP the player was
about to get, times the bonus. Each fellow spends their own. Quest XP, allegiance passup, item XP, vitae and Loyalty go by
the XP before the bonus. "You've used up your rested experience." when it runs out.

## Where it lives

- `PropertyFloat.RestedXp` (20058): what's left, a float so a heartbeat's fraction of a point isn't lost.
- `PropertyFloat.RestedXpTimestamp` (20059): when it was last added up. Set every heartbeat and at logout, so login only
  counts the time logged out, even after a crash (out by at most `player_save_interval`). A character from before this
  starts counting at its next login.
- `WorldObjects/Player_RestedXp.cs` keeps the pool (`RestedXpTick` from `Heartbeat` and `SetPropertiesAtLogOut`,
  `HandleRestedXpOnLogin`, `ConsumeRestedXp` from `GrantXP`'s queued action, on the player's own thread);
  `Entity/RestedXpRules.cs` has the sums and the words, tested in `RestedXpTests`.

## Seeing it

- At login, if they have some: how much they rested while away, what they have, and the bonus.
- `/rested`: what's left of the most, the bonus, whether they're resting now (and why not), how much an hour adds and
  when it's full, and what their kills have earned from it since login.
- `@playerui rested` (`PlayerUiRested`): the same for the Daralet Toolkit's Rested window. `/rested` and the window are
  made from one reply (`RestedXpRules.Lines` reads it), so they always agree.

## Server properties

| Property | Default | |
| --- | --- | --- |
| `rested_xp` | true | Off stops building and spending; each player keeps what they have. |
| `rested_xp_bonus` | 1.0 | How much more kills earn while rested (1.0 doubles them). |
| `rested_xp_levels_per_hour` | 0.01 | An hour of rest, as a fraction of the current level's cost. |
| `rested_xp_max_levels` | 1.5 | The most, as a fraction of the current level's cost. |
