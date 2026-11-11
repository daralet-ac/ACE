# Rested experience

Players build up **rested experience** while they're away from the fight, and spend it on their next kills: while they
have some, kill XP earns a bonus (100% more by default), and the bonus is taken out of their rested experience until it's
gone.

## Building it up

- **Logged out**, anywhere: the time since logout counts in full.
- **In a town** out of combat mode: the town's landblock (`Town.TownLandblockIds`, the same check the road speed buff
  uses), its buildings included, never an instanced copy. Combat mode stops it until it's put away.
- Each hour it grows by a share of the player's current level's XP cost that falls as they level, so low levels rest
  faster: `rested_xp_levels_per_hour` (10%) less `rested_xp_levels_per_hour_less_per_level` (0.1%) for each level, but
  never under `rested_xp_min_levels_per_hour` (0). That's 9.9% an hour at level 1, 5% at 50, and nothing from level 100
  on: those players can spend what they have, but no more builds up (`/rested` says so). Raising the least (0.01, say)
  keeps every level resting. There's no most to start (`rested_xp_max_levels` 0): the slowdown
  below alone holds it back. Set `rested_xp_max_levels` to a number of levels (1.5, say) to stop it at that much of the
  level's cost. The most grows as they level; a pool already over it (the properties lowered) isn't cut back.
- It slows by `rested_xp_slowdown_per_level` (50%) for each full level's cost already rested: the first level's worth
  comes at full speed, the second at half, the third at a quarter (`RestedXpRules.RateAt`): at level 50, a level's worth
  in 20 hours, two in 60, three in 140. A rest that crosses a level's worth, a
  long logout, is added up a stretch at each speed (`Accrue`), and so is the time until full (`SecondsToFull`). 0 for no
  slowdown, 1 to stop at a level's worth.
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
- `@playerui xp` (`PlayerUiXp`, `PlayerUiXpData`): the Daralet Toolkit's XP bar, the player's level and the total XP it
  starts and ends at (the client doesn't have the XP table), with their rested experience (`PlayerUiRested`) shaded ahead
  of it. `/rested` reads the same `PlayerUiRested` (`RestedXpRules.Lines`), so the two always agree.

## Server properties

| Property | Default | |
| --- | --- | --- |
| `rested_xp` | true | Off stops building and spending; each player keeps what they have. |
| `rested_xp_bonus` | 1.0 | How much more kills earn while rested (1.0 doubles them). |
| `rested_xp_levels_per_hour` | 0.10 | An hour of rest, as a fraction of the current level's cost, before the level takes its share off. |
| `rested_xp_levels_per_hour_less_per_level` | 0.001 | What each level takes off an hour's rest. |
| `rested_xp_min_levels_per_hour` | 0 | The least an hour's rest comes to, however high the level. |
| `rested_xp_slowdown_per_level` | 0.5 | How much slower it builds for each full level's cost already rested. |
| `rested_xp_max_levels` | 0 | The most, in levels of the current level's cost; 0 for no most. |
