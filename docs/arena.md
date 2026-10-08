# The arena

Players can duel each other. Every duel is fought in an **instance of its own** of an arena map (see [instanced-landblocks.md](instanced-landblocks.md)), so nobody else is ever in there with them, any number of duels can use the same map at once, and nothing is left behind when it is over. Nobody loses anything by being defeated, and everybody goes back to exactly where they were afterwards. Only non-player killers and player killer lites can duel, from the level the server asks for (`arena_dueling_minimum_level`). Admins can turn the whole thing off at once (`arena_dueling_enabled`).

Duels are one against one, or a whole fellowship against another of the same size (2v2 up to 9v9), from a challenge or from the queue. They can be raw or scaled, and rated or unrated, and every kind of rated duel has its own leaderboard. Rewards and spectating are for later (see the end).

## Commands

| Command | Does |
|---|---|
| `/arena` | How to use it, and where you stand (in the queue, or in a duel). |
| `/arena challenge <name> [scaled] [unrated] [fellowship]` | Asks a player to a duel (raw and rated unless asked otherwise, see below). With `fellowship`, your fellowship challenges the one `<name>` is in (see Fellowship duels). They get a yes/no question. Someone who says no can't be challenged by the same player again for a minute. A player who has squelched you can't be challenged by you. |
| `/arena queue [levels] [scaled] [unrated] [fellowship]` | Waits for an opponent (with `fellowship`, your whole fellowship waits for another of its size). The queue pairs players in the order they came, and only with someone who asked for the same kind of duel (scaled or raw, rated or unrated). With a number, you are only matched with someone within that many levels of you (and you are only matched with someone whose own band you are within). Without one, the server's `arena_queue_level_band` (0, any level, by default). Using it again changes your band and keeps your place. |
| `/arena leave` | Leaves the queue (anyone in a waiting fellowship takes the whole fellowship out), calls off a duel that has not begun, or gives up the one you are fighting. |
| `/arena stats [name]` | Your arena ratings and records on every board you have fought a rated duel on, or someone else's. |
| `/arena top [2v2] [scaled]` | The ten best ratings on a board: `1v1` (the default), `2v2`, `3v3`... raw (the default) or `scaled`. |
| `/arena reset <name> [2v2] [scaled]` | (Admin) Puts a character's rating back to 1400 and clears their record, on every board they have fought on, or only on the one named. Works on offline characters. |
| `/arena restore <name>` | (Admin) Gives back what `/arena reset` took (kept in `PropertyString.ArenaResetBackup`), replacing whatever the character has on those boards now. A second reset of a board that is already at the start keeps the first backup. |
| `/arena unrank <name>` | (Admin) Takes a character off `/arena top`. Their ratings and records are kept, and they can still duel. |
| `/arena rerank <name>` | (Admin) Puts them back on it. |
| `/arena maps` | The arenas duels are fought in. |
| `/arena list` | (Sentinel and up) The duels going on, and who is in the queue. |
| `/arena cancel <duel>` | (Sentinel and up) Calls a duel off. Fighters who are in the arena are taken home. |

## How a duel goes

1. **Yes.** A challenge asks the one who is challenged. When the queue finds two players, it asks both. They have `arena_accept_seconds` (20) to answer. If someone says no or doesn't answer, the duel is off. From the queue, whoever said no (or didn't answer) leaves the queue, and the other one goes back to the place they had. Someone who is challenged while they wait in the queue keeps their place whatever they answer.
2. **To the arena.** One of the enabled maps is picked at random, an instance of it is made, and every side is taken to a different one of its starts, picked at random (a fellowship starts together). Where they were, and their player killer status, are kept, to put back afterwards.
3. **Countdown.** When everyone has arrived, they get `arena_countdown_seconds` (10). During it they are all non-player killers, so nothing can harm anyone. Their harmful enchantments are taken away and their vitals filled. Their beneficial enchantments stay with them. If someone leaves, or doesn't arrive within a minute, the duel is off.
4. **Fight.** Everyone becomes a player killer lite with full vitals, and the fight is on. Only opponents can harm each other, and nobody can heal or buff an opponent. It ends when one side has nobody standing, or after `arena_time_limit_minutes` (20), which is a draw.
5. **Home.** The loser is taken home once they have finished falling, with three quarters of their vitals. Whoever is still standing is taken home 5 seconds after the end. Everyone gets their own player killer status back, and harmful enchantments from the duel are taken away. 20 seconds after the end the instance is closed, which sends anyone who is still in it home too.

### Being defeated is not dying

A fighter who is killed in the arena is defeated. `Player.OnDeath` and `Player.Die` ask `ArenaManager.IsDefeatNotDeath`, and for a fighter they do none of what a death does:

- no vitae, and no enchantments lost (the "augmentation keeps your enchantments" rule doesn't come into it),
- no corpse (there never is one in an instance), no death counted, no lifestone,
- no kill: no player killer death broadcast, no `PlayerKillsPkl`, no kill tracking quests, no `OnKill` emotes, no fellowship death message,
- no `pk_respite_timer`: they can use /pkl straight away.

They still fall, and they and their opponent still see who defeated whom. Every way of dying goes through `Player.Die`, so this covers melee, missiles, spells, damage over time, falling, `/die` and the rest.

### Leaving

- **Logging out** in a duel gives it up (or calls it off, if it had not begun). The fighter is saved where they were before the duel, with their own status: the instance's owner (the duel) tells the instance system where they go (`IInstanceReturnPositions`).
- **Leaving the arena any other way** (a portal in it, a recall, being moved by an admin) gives it up too, and gets them their own status back where they are.
- If the server crashes in a duel, nothing needs mending: player killer lite status is never kept over a login (it is turned back to non-player killer), and the arena landblocks of `arenas.json` are ones where players log in at their lifestone.

## Fellowship duels

A fellowship fights another fellowship of the same size, everyone in each. Only the leader can take a fellowship into the arena.

- **Challenge**: `/arena challenge <name> fellowship` (with `scaled` and `unrated` as for any duel). `<name>` is anybody in the other fellowship. Both fellowships have to be the same size, and everyone in them has to be able to duel. Everyone on both sides is asked, the challenger's own fellowship too, and the duel is off if anybody says no.
- **Queue**: `/arena queue fellowship [levels]`. The fellowship waits as one, and is only paired with a fellowship of the same size that asked for the same kind of duel. A level band compares the highest level on each side. Anyone in it who was waiting on their own now waits with it. Everyone is asked when opponents are found. If someone says no, their fellowship leaves the queue and the other goes back to its place. A fellowship that changes while it waits (someone leaves or joins it, or logs out) is taken out of the queue, and its leader can put it back. `/arena leave` by anybody in it takes the whole fellowship out.
- Someone who waits in the queue with their fellowship can't be challenged into another duel (on their own or with another fellowship) until it leaves the queue.
- **In the arena** each fellowship starts together, at a start of its own. Nobody can harm their own side, and teammates can heal and buff each other as usual. A fighter who is defeated is taken home straight away, as in any duel; their side fights on. A side is beaten when everyone on it has been defeated, has given up, or has left; then the other side has won. The time limit is the same, and ends in a draw.
- **Scaled**: scaling works between any two fighters, by their two levels: whoever is the higher of the two fights the other at their level. A heal on a higher-level teammate counts for more, and on a lower-level one for less, as shroud scaling does for Shrouded fellows.

## Kinds of duel

Every duel is **raw** or **scaled**, and **rated** or **unrated**. Without `scaled` or `unrated`, a duel is raw and rated. Whoever is challenged is told what kind of duel it is before they answer.

### Scaled duels

In a scaled duel the higher-level fighter fights at their opponent's level, the way a Shrouded player fights at a monster's: their attack skill, defense skill, armor, ward and resistances count as they would at the lower fighter's level, relative to the average at each level, so better-than-average gear for your level stays exactly as much better. It uses the same tables and code as shroud scaling (`LevelScaling`), with the opponent in the monster's place, whether or not anyone is Shrouded. A player is not a monster, so the monster tables are left out: instead of the lower fighter's armor and ward counting for more, damage both ways follows the fighters' average health at their levels (the higher fighter does damage as if to someone of the lower one's health, and the lower fighter's damage, harms and drains count against the higher one as if they were the same level). Nothing is scaled between fighters of the same level.

Scaling doesn't go below level 10 (the tables don't), so scaled duels are for level 10 and up. A raw duel is never scaled, even if a fighter is Shrouded. The fighters are told who fights at whose level when the countdown starts.

A level band still works for the scaled queue, but it is there for raw duels: with scaling, levels don't need to be close.

### Ratings

There is a board for each kind of rated duel: its size (1v1, 2v2, ... 9v9) and whether it was scaled (`ArenaBoard`). Each has its own rating (starting at 1400) and record of wins, losses and draws, so a 3v3 scaled rating has nothing to do with a 1v1 raw one. They are kept on the character as server only properties (`ArenaBoards`): the 1v1 boards in properties of their own (raw: `PropertyInt.ArenaRating`, `ArenaWins`, `ArenaLosses`, `ArenaDraws`; scaled: `ArenaScaledRating`, `ArenaScaledWins`, `ArenaScaledLosses`, `ArenaScaledDraws`), and every team board together in `PropertyString.ArenaTeamBoards`, as JSON keyed by the board's name (`{"2v2":{"Rating":1425,"Wins":1,...},"3v3 scaled":{...}}`), so a new size needs nothing new on the character.

A rated duel changes every fighter's rating on its board by Elo, with a K of `arena_elo_k` (50): beating an even opponent is worth 25 points, an upset more, beating someone far below at least 1. In a fellowship duel each fighter is rated against the average rating of the other side, as if it were one opponent (`ArenaElo.RateTeams`), so the weaker players of a winning fellowship gain the most. Draws don't change ratings, but count in the record. An unrated duel changes nothing: no rating, and no record.

`/arena top` reads every character's boards (a scan of every player, online or not), which is fine at a server's size. If it ever is not, the boards can move to a table of their own (character, board, rating, wins, losses, draws), and `/arena top` becomes a query.

Who decides whether a duel is rated:

- A player, by asking for `unrated`. The queue only pairs unrated players with each other.
- The server: challenges are only rated while `arena_rated_challenges` is on (the challenger is told when their challenge can't be rated). A duel in which anybody on one side is connected from the same IP address as anybody on the other is never rated while `arena_block_same_ip` is on, and the queue never pairs them.

## Turning it off, and the minimum level

Both are server properties, so they are changed in the game (by an admin) and take effect straight away, without a restart:

- `/modifybool arena_dueling_enabled false` turns arena dueling off. At once (the arena checks four times a second), the queue is emptied, every duel that is going on is called off (fighters who are in the arena already are taken home, as when any duel is called off), and nobody can queue, challenge or be challenged. Records and ratings can still be looked at (`/arena stats`, `/arena top`). `/modifybool arena_dueling_enabled true` turns it on again.
- `/modifylong arena_dueling_minimum_level 50` lets only characters of level 50 and up duel: to queue, to challenge, and to be challenged. 1 (the default) is any level. It is checked whenever someone queues or challenges, again when the queue pairs them, and again just before the fighters are sent to the arena, so someone who no longer qualifies is taken out of the queue (and told why) when their turn comes. A duel that has begun is not stopped by it.

## Arena maps: `arenas.json`

The maps are in `apps/server/arenas.json`, which the build copies next to the server every time (the list is curated with the server, unlike `instances.json`). It is read once, when the server starts. A mistake in one map is logged (`[ARENA] ...`) and only leaves that map out. The file explains its own format, and allows comments and trailing commas.

Every map is registered as an instance template called `arena:<name>`, so an admin can look around one without a duel: `/instance open arena:pkl-arena`, then `/myloc` to find coordinates, and `/instance leave`.

| Map | Landblock | Enabled | |
|---|---|---|---|
| `pkl-arena` | `0067` | yes | The PKL Arena. Nothing in it. The starts are where its five portals put players. |
| `derethian-combat-arena` | `00AB` | no | Full of the Derethian Combat Arena's own content: the DCA Devices generators, and the Statues of Death and Illumination, which give rewards on a timer to whoever uses them. In a duel those could be had without the 4 MMDs and level 150 the Arena Master asks for. Enable it once that content is dealt with. |
| `derethian-combat-pit` | `00AC` | no | The other half of the Derethian Combat Arena, the same. |

### Adding maps

- **A dungeon** needs its landblock and at least two starts, far enough apart. Its walls are its edge. Check there is nothing in it that would be wrong to give away for free (rewards, quest items, portals that skip content), and nothing that would get in the way.
- **Outdoors** there are no walls, so a map also needs a `bufferRing` of at least 1 (the landblocks around it are loaded, and players who get into them are turned back), and a `center` and `radius`: the fighting area. A fighter who stays outside it for 5 seconds is disqualified, after a warning. Every start has to be inside it. A ring of 1 around one landblock is 9 landblocks loaded for every duel, so keep outdoor maps to one landblock where possible.
- Don't use landblocks that have houses, or a town's landblock: a duel's instance is a copy of everything in it.
- A map can be at most 49 landblocks with its ring.

## Server properties

| Property | Default | |
|---|---|---|
| `arena_dueling_enabled` | true | Whether arena dueling is on at all. Turning it off calls off everything that is going on (see above). |
| `arena_dueling_minimum_level` | 1 | The lowest level that can duel. 1 is any level. |
| `arena_accept_seconds` | 20 | How long players have to say yes (at least 5). |
| `arena_countdown_seconds` | 10 | How long the countdown is (at least 3). |
| `arena_time_limit_minutes` | 20 | How long a duel lasts at most. Then it is a draw. |
| `arena_elo_k` | 50 | The K-factor of ratings. |
| `arena_queue_level_band` | 0 | The level band of players who don't ask for one. 0 is any level. |
| `arena_block_same_ip` | true | The queue doesn't pair players from the same IP address, and duels between them are not rated. |
| `arena_rated_challenges` | true | Whether challenges are rated (the queue's duels always are). |

## Code

| | |
|---|---|
| `apps/server/Arena/ArenaManager.cs` | Everything a duel does, from the question to the end, and the hooks the rest of the server calls. One lock. Whatever is done to a player is queued on the player. |
| `apps/server/Arena/ArenaMatch.cs` | A duel and its fighters. It is the `Owner` of its instance, and says where fighters go when they leave it. |
| `apps/server/Arena/ArenaManager.Fellowship.cs` | Fellowship challenges, and putting a fellowship in the queue. |
| `apps/server/Arena/ArenaQueue.cs` | The queue and its pairing rules. An entry is a player, or a fellowship. |
| `apps/server/Arena/ArenaElo.cs` | Ratings, one against one and for teams. |
| `apps/server/Arena/ArenaBoards.cs` | The boards (1v1, 2v2 scaled, ...) and where a character's rating and record on each are kept. |
| `apps/server/Arena/ArenaMap.cs`, `ArenaMapConfig.cs`, `ArenaMaps.cs` | Maps, reading `arenas.json`, and the maps that are loaded. |
| `apps/server/Arena/ArenaConfirmation.cs` | A yes/no question that also says when the answer is no. |
| `apps/server/Entity/LevelScaling.cs` | Scaled duels: `CanScalePlayer` asks `ArenaManager.IsScaledDuel`, and `GetDuelDamageScalar` replaces the monster health and armor tables between two fighters. |
| `apps/server/WorldObjects/Player_Arena.cs` | What a duel does to a player: getting ready, beginning, being defeated, going home. |
| `apps/server/Commands/PlayerCommands/ArenaCommand.cs` | `/arena`. |

The hooks into the rest of the server: `Player.CheckPKStatusVsTarget` and `Healer` (who may harm or help whom), `Player.OnDeath` and `Player.Die` (defeat instead of death), `Player.FinalizeLogout` (logging out in a duel), `InstanceManager.GetReturnPosition` (`IInstanceReturnPositions`), `WorldManager` (the tick, every 250 ms) and `Program` (loading the maps). `ConfirmationManager.EnqueueAbort` can close a question without saying the player took too long.

Tests: `apps/server-tests/ArenaTests.cs` (ratings, the queue, maps and the `arenas.json` that comes with the server).

## Roadmap

### Still to do from phase 1

- **Testing on a live server.** Nothing here has been run against a real client yet. Things to try: a challenge and a queue duel to the end each way (defeat, giving up, logging out, a portal out, the time limit with `/modifylong arena_time_limit_minutes 1`), saying no and not answering, turning `arena_dueling_enabled` off in the queue, while asked, in the countdown and in a fight, a challenge across `arena_dueling_minimum_level`, a player killer lite and a non-player killer fighting, `/die` in the countdown, a fighter whose lifestone is somewhere else, logging out while falling, and `/arena cancel`. Watch that statuses come back right (`/pk` as a developer shows yours) and that nobody is left in an instance (`/instance list`).
- **More maps**: the curated list of indoor and outdoor places, in `arenas.json`.

### Phase 2

- **Unrated duels** and **scaled duels**: done (see Kinds of duel). To test on a live server: a scaled duel between levels far apart both ways (melee, missile, war and void magic, damage over time, harms and drains), a scaled duel between fighters of the same level, a raw duel with a Shrouded fighter (nothing must be scaled), and `/modifybool debug_level_scaling_system true` to see the scalars on the console.

### Phase 3

- **Fellowship duels**, from a challenge or the queue, and **leaderboards** per size and kind: done (see Fellowship duels and Ratings). To test on a live server: a 2v2 and a 3v3 challenge and queue duel to the end (one side wiped out, a fighter giving up or logging out while their side fights on, the time limit), someone saying no on either side, a fellowship changing while it waits (someone leaves, joins, logs out), `/arena leave` by a member who is not the leader, heals and buffs between teammates and none on opponents (during the countdown too), a scaled 2v2 with mixed levels on each side, and `/arena stats` and `/arena top 2v2` afterwards.
- Not done: fellowships of different sizes against each other, and putting together teams from players who queue on their own (Shoff's team queue, planned as phase 4). Both would need to say how a side's strength is weighed, which a same-size fellowship duel doesn't.

### Phase 4 (planned): teams for players who queue on their own

To start once phase 3 has been tested on a live server: pickup teams fight exactly as fellowship teams do once they are in the arena, so a problem with team fights would show in both.

A player who is not in a fellowship can queue for a team size, `/arena queue 3v3` (with `scaled`, `unrated` and a level band as for any duel). When there are enough players who want the same size and the same kind of duel (six, for 3v3), the queue splits them into two teams, and everyone is asked as for any duel.

Already there from phase 3: sides, nobody harming their own side while teammates heal and buff each other, a start of their own for each side, team boards and team Elo, everyone being asked and who goes back in the queue when someone says no, and scaling between any two fighters by their two levels.

To build:

- **The team builder.** Today the queue pairs two entries; this waits for twice the team size in compatible players who asked for the same size and kind of duel, and then splits them. Players who wait longer go first, as now, and every pair of players is within each other's level band.
- **A fair split.** The two teams are made as even as can be: by rating on the board the duel goes on, and for a raw duel by level too. Players connected from the same IP address go on the same side (or wait for another duel, while `arena_block_same_ip` is on), so nobody can throw a duel for someone they share a connection with. Every way of splitting the players can be tried (fewer than 25,000 even for 9v9), so this can be the best split rather than a greedy one.
- **A fellowship for each team.** Without one, teammates don't see each other's vitals in the fellowship panel, which makes healing them hard, and have no fellowship chat. The server can make a fellowship (`Player.FellowshipCreate`), add players to it without asking them (`Fellowship.AddConfirmedMember`), and disband it when they leave the arena. A player can only be in one fellowship, so queueing on your own for a team means not being in one: a fellowship that wants to fight together uses `/arena queue fellowship`.
- **Seeing the queue fill.** A 3v3 needs six players who want the same size and the same kind of duel (scaled or raw, rated or unrated) at the same time, which on a quiet server can take a while. Players should see how close it is ("4 of 6 waiting for 3v3"), and it may turn out that only some sizes are worth offering on their own.

To decide before it is built (what we would do first):

- **Do pickup teams meet fellowships?** Not at first: a fellowship that plays together has an edge over players who have just met.
- **Their own boards?** Yes, "3v3 solo" next to "3v3": if the two never meet, one board would rank two pools of players that never fought each other.
- **Fellowships with room left, filled up with players on their own** (a fellowship of two and one other player against three)? Not in phase 4. Most of the difficulty of Shoff's team queue is there, which is why his is greedy.

### Later (not designed yet)

- **Weekly rewards and rating resets.** Once a week: the top of each board is rewarded (items, titles, luminance, to be decided), the board's standings are kept as history (last week's winners), and ratings go back to the start (or part way back toward it, so the best players don't start from nothing). Needs a scheduled job (the server has event and timer infrastructure to hang it on), a record of each week's results, and rewards that reach players who are offline (given at their next login). Watch for farming: rewards make the same-IP rule and rated-challenge abuse (two friends trading wins) matter much more, so a reward may need a minimum number of duels against different opponents.
- **Watching duels.** `/arena watch` lists the duels going on (fighters, map, how long it has been running), and `/arena watch <number | name>` takes you in as a spectator. Spectators are invisible (cloaked, as admins are, so fighters can't see or target them and they don't get in the way), can't harm or help anyone (the arena already refuses that between fighters and anyone else in the instance), keep their own status, and are taken back to where they were when the duel ends or they `/arena leave`, the same way fighters are (`IInstanceReturnPositions`). A duel might be watchable only if its fighters allow it, or only when it is rated.
