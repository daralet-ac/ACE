# Rested experience

Players build up **rested experience** while they're away from the fight, and spend it on their next kills: while they
have some, kill XP earns a bonus (100% more by default), and the bonus is taken out of their rested experience until it's
gone.

## Building it up

- **In a town** out of combat mode: the town's landblock (`Town.TownLandblockIds`, the same check the road speed buff
  uses), its buildings included, never an instanced copy. Combat mode stops it until it's put away.
- **Logged out**, anywhere: at `rested_xp_offline_rate` (half) of the rate in a town. WoW gives a quarter outside its
  inns and cities; here it's half wherever you log out, so playing in town is what's rewarded.
- **From level 10** (`rested_xp_min_level`): below it nothing builds up, in town or logged out, so a new character banks
  none (`/rested` says when it starts). Rest is per character, as WoW's is, and kept as XP at the level it was made at,
  so an alt parked low banks little that's worth anything later.
- Each hour it grows by a share of the player's current level's XP cost that falls as they level, so low levels rest
  faster: `rested_xp_rate_curve` (`RestedXpCurve`), pieces of `level:start:less` in percent, each for the levels above
  its level, start less "less" for each level past it. The default, `0:10:0, 10:10:0.225, 50:1:0.02`, is 10% an hour
  to level 10, then 10% less 0.225% for each level past 10 (9.775% at 11, 5.5% at 30, 1% at 50), then 1% less 0.02% for
  each level past 50 (0.98% at 51, 0.5% at 75, nothing from 100: those players can spend what they have, but no more builds
  up, and `/rested` says so). A curve that can't be read is logged and the default used. It's never under
  `rested_xp_min_levels_per_hour` (0); raising that (0.01, say) keeps every level resting.
- There's no most to start (`rested_xp_max_levels` 0): the slowdown below alone holds it back. Set
  `rested_xp_max_levels` to a number of levels (1.5, say) to stop it at that much of the level's cost. The most grows as
  they level; a pool already over it (the properties lowered) isn't cut back.
- It slows by `rested_xp_slowdown_per_level` (50%) for each full level's cost already rested: the first level's worth
  comes at full speed, the second at half, the third at a quarter (`RestedXpRules.RateAt`): at level 50, a level's worth
  in 100 hours, two in 300, three in 700. A rest that crosses a level's worth, a long logout, is added up a stretch at each speed (`Accrue`), and so is the time until full (`SecondsToFull`). 0 for no
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
| `rested_xp_min_level` | 10 | The level rest starts building at. |
| `rested_xp_offline_rate` | 0.5 | Logged out, the share of the rate in a town. |
| `rested_xp_rate_curve` | `0:10:0, 10:10:0.225, 50:1:0.02` | An hour of rest at each level, in percent of the level's cost. |
| `rested_xp_min_levels_per_hour` | 0 | The least an hour's rest comes to, however high the level. |
| `rested_xp_slowdown_per_level` | 0.5 | How much slower it builds for each full level's cost already rested. |
| `rested_xp_max_levels` | 0 | The most, in levels of the current level's cost; 0 for no most. |
