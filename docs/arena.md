# The arena

Players can duel each other. Every duel is fought in an **instance of its own** of an arena map (see [instanced-landblocks.md](instanced-landblocks.md)), so nobody else is ever in there with them, any number of duels can use the same map at once, and nothing is left behind when it is over. Nobody loses anything by being defeated, and everybody goes back to exactly where they were afterwards. Only non-player killers and player killer lites can duel.

This is phase 1: one against one, from a challenge or from the queue. Spectating, fellowship against fellowship, a team queue and level scaling are for later (see the end).

## Commands

| Command | Does |
|---|---|
| `/arena` | How to use it, and where you stand (in the queue, or in a duel). |
| `/arena challenge <name>` | Asks a player to a duel. They get a yes/no question. Someone who says no can't be challenged by the same player again for a minute. A player who has squelched you can't be challenged by you. |
| `/arena queue [levels]` | Waits for an opponent. The queue pairs players in the order they came. With a number, you are only matched with someone within that many levels of you (and you are only matched with someone whose own band you are within). Without one, the server's `arena_queue_level_band` (0, any level, by default). Using it again changes your band and keeps your place. |
| `/arena leave` | Leaves the queue, calls off a duel that has not begun, or gives up the one you are fighting. |
| `/arena stats [name]` | Your arena rating and record, or someone else's. |
| `/arena top` | The ten best arena ratings. |
| `/arena maps` | The arenas duels are fought in. |
| `/arena list` | (Sentinel and up) The duels going on, and who is in the queue. |
| `/arena cancel <duel>` | (Sentinel and up) Calls a duel off. Fighters who are in the arena are taken home. |

## How a duel goes

1. **Yes.** A challenge asks the one who is challenged. When the queue finds two players, it asks both. They have `arena_accept_seconds` (20) to answer. If someone says no or doesn't answer, the duel is off. From the queue, whoever said no (or didn't answer) leaves the queue, and the other one goes back to the place they had. Someone who is challenged while they wait in the queue keeps their place whatever they answer.
2. **To the arena.** One of the enabled maps is picked at random, an instance of it is made, and every fighter is taken to a different one of its starts, picked at random. Where they were, and their player killer status, are kept, to put back afterwards.
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

### Ratings

Every character has an arena rating, starting at 1400 (`PropertyInt.ArenaRating`), and a record of wins, losses and draws (`ArenaWins`, `ArenaLosses`, `ArenaDraws`). They are server only properties.

A rated duel changes both ratings by Elo, with a K of `arena_elo_k` (50): beating an even opponent is worth 25 points, an upset more, beating someone far below at least 1. Draws don't change ratings. Duels from the queue are rated. Challenges are rated if `arena_rated_challenges` is on. A duel between two players connected from the same IP address is never rated while `arena_block_same_ip` is on, and the queue never pairs them. Every duel counts in the record, rated or not.

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
| `arena_enabled` | true | Whether players can use `/arena` at all. |
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
| `apps/server/Arena/ArenaQueue.cs` | The queue and its pairing rules. |
| `apps/server/Arena/ArenaElo.cs` | Ratings. |
| `apps/server/Arena/ArenaMap.cs`, `ArenaMapConfig.cs`, `ArenaMaps.cs` | Maps, reading `arenas.json`, and the maps that are loaded. |
| `apps/server/Arena/ArenaConfirmation.cs` | A yes/no question that also says when the answer is no. |
| `apps/server/WorldObjects/Player_Arena.cs` | What a duel does to a player: getting ready, beginning, being defeated, going home. |
| `apps/server/Commands/PlayerCommands/ArenaCommand.cs` | `/arena`. |

The hooks into the rest of the server: `Player.CheckPKStatusVsTarget` and `Healer` (who may harm or help whom), `Player.OnDeath` and `Player.Die` (defeat instead of death), `Player.FinalizeLogout` (logging out in a duel), `InstanceManager.GetReturnPosition` (`IInstanceReturnPositions`), `WorldManager` (the tick, every 250 ms) and `Program` (loading the maps). `ConfirmationManager.EnqueueAbort` can close a question without saying the player took too long.

Tests: `apps/server-tests/ArenaTests.cs` (ratings, the queue, maps and the `arenas.json` that comes with the server).

## Not done yet

- **Testing on a live server.** Nothing here has been run against a real client yet. Things to try: a challenge and a queue duel to the end each way (defeat, giving up, logging out, a portal out, the time limit with `/modifylong arena_time_limit_minutes 1`), saying no and not answering, a player killer lite and a non-player killer fighting, `/die` in the countdown, a fighter whose lifestone is somewhere else, logging out while falling, and `/arena cancel`. Watch that statuses come back right (`/pk` as a developer shows yours) and that nobody is left in an instance (`/instance list`).
- **Spectating.** The arena already refuses harm and help between fighters and anyone else in the instance, so spectators can be sent in with `InstanceManager.Enter` and taken home by the duel like fighters.
- **Fellowship against fellowship, and a team queue.** `ArenaFighter.Side` and the rules for who may harm whom already work by side, and a duel ends when a side has nobody standing. Only one against one is rated.
- **Level scaling**, like shroud scaling: a choice for the queue (a scaled duel, or a level band). Fighters are made ready in `Player.PrepareForArenaDuel` and the fight begins in `Player.BeginArenaDuel`, which is where scaling would be put on, and `RestoreAfterArena` where it would be taken off.
- **More maps**: the curated list of indoor and outdoor places, in `arenas.json`.
