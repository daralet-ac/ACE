# Instanced landblocks: test plan

What to run, and what to expect, to trust the instancing system on a live server. Read [instanced-landblocks.md](instanced-landblocks.md) first for how it works and what every command does.

- **Build under test:** branch `feat/instanced-landblocks`, from the commit that adds this file. It includes the two fixes that came out of writing and running this plan: "a player can't walk out of an instance" and "fellowship XP, pets and house purchases don't cross into instances".
- **Priorities:** **P0** must pass before instances are used on the live server. **P1** should pass; a failure needs a decision. **P2** is worth a look, or informational.
- **Where to run it:** a development server with a copy of the shard and world databases. Take a backup first. Some tests kill the server and edit the database.

## 1. What is covered already, and what is not

**Already covered by automated tests** (`dotnet test apps/server-tests`, 117 tests, no database or DATs needed): templates, rings and the `instances.json` parser; the instance registry, timers and `FindOrRegister`; instance-only landblocks and `CanEnter`; the ids the statics of an instance get, and the translation of world-database guids; cell identity across instances; which objects can be moved; the description helpers. In a checkout without a database or DAT config 111 pass. The 6 that fail (`Sphere_CollideWithPoint`, `Sphere_SlideSphere`, `CanParseStarterGearJson`, `DatabaseManager_Initialize`, `WorldManager_Initialize`, `CommandManager_Initialize`) fail the same way on the commit before instancing existed, because they need a database, DATs and config. On your machine some of them may pass.

**Checked once against the real DATs, with no database** (152 checks, a console harness that is not in the repository): physics isolation between instances, landblock loading and adjacency, ids and teardown, islands with rings, solid edges, the world-guid lookup, and moving objects between instances.

**Not covered by anything, so this plan is where they are tested:** every real player flow (login, logout, death, teleport, portals, trade, fellowship), the commands, the fellowship-dungeon path, the database, parallel landblock ticking, load, and the persistent world itself (the physics change touches it).

**Where the bugs are most likely:** (1) the player flows, which have never run live; (2) the change to shared physics, because it can affect the persistent world; (3) parallel landblock ticking; (4) systems that look at positions instead of landblocks, which do not know about instances (three were found while writing this plan, so expect more); (5) the database.

## 2. Setup

**Accounts.** `ADMIN` (Admin level), and ordinary players `A`, `B`, `C`. Two clients are enough for the smoke test, and four are better for the full plan. Give `A`/`B` enough level for the dungeon used in section G.

**Baseline, before anything else.** Restart the server, log in `ADMIN`, and record `/serverstatus` (memory, threads, loaded and dormant landblocks), and the database counts from appendix B.

**Settings used by the tests**

| Setting | How | Used for |
|---|---|---|
| `instance_empty_timeout_minutes` | `/modifylong instance_empty_timeout_minutes 1` (default 15) | Lifecycle tests. Never set it to 0: an instance nobody has entered yet would be deleted at once. |
| `capstone_instanced_dungeons` | `/modifystring capstone_instanced_dungeons Glenden Wood Dungeon` (comma separated; empty by default) | Section G |
| `Server.Threading.MultiThreadedLandblockGroupTicking` and `...MultiThreadedLandblockGroupPhysicsTicking` | `Config.js`, both `true` in `Config.js.example` | Run the P0 tests once with both `true` and once with both `false` (J1, J2) |
| `apps/server/instances.json` | Edit and restart. Read once at startup. | Sections A and F. Samples in appendix C. |

**Logs.** Console (Information and up) and the JSON file set in `appsettings.json` (`c:\ACE\Logs\ace-<date>.log`, every line is a JSON object with the rendered text). Search for `[INSTANCE]`, `NullReference`, `cross-thread` and `REPORT THIS`. Appendix A says which `[INSTANCE]` lines are expected. `Unable to find object_id ... in Cell` lines are old noise from the server DAT (see `server-dat-broken-landblocks.txt`), not from instancing.

**Record results** in the table in appendix D.

## 3. Smoke test (P0, about 45 minutes, `ADMIN` + `A` + `B`)

Run this on every new build. If any step fails, stop.

| # | Do | Expect |
|---|---|---|
| S1 | Start the server with the shipped `instances.json`. `/instance list`. | No `[INSTANCE]` errors. "Instance templates (0)", "Open instances (0)". |
| S2 | Stand somewhere outdoors with `A` next to you. `/instance here 1`, then `/instance info`, `/myloc`. | You arrive at the same spot. `info` says instance N, inside the instance. `myloc` has an `Instance:` line. `A` does not see you and you do not see `A`. |
| S3 | `/ci` an item and a monster next to you. | `A` (persistent world) sees neither. The monster attacks you, not `A`. |
| S4 | Run to the edge of the 3 x 3 landblocks, and keep pushing on past it for 30 seconds. Then `/myloc`. | You are stopped at the edge: about a second after crossing it the client is put back, and again each time you push on. `Location` and `Physics` in `/myloc` are both inside the block and agree to within a few metres. No `[INSTANCE] ... which is not part of instance` warning in the log. |
| S5 | `/instance enter N A`, then `/instance info A`, then `/instance leave A`. | `A` is sent in, sees the monster, and is sent out. The audit channel shows both. |
| S6 | `/instance enter N A` again, and let the monster kill `A`. | No corpse. `A` keeps every item. Sanctuary respawn in the persistent world (do this away from the lifestone, see E5). Vitae applies as usual. |
| S7 | `/instance enter N A` a third time. `A` tries to drop an item and to split a stack onto the ground. Then `/instance leave A`. | "You can't drop items here." Nothing dropped. |
| S8 | `/instance leave`, wait one minute, `/instance list`. | The instance shows "empty for x of 1 min", then is gone. Log: `[INSTANCE] Deleted instance N`. `/serverstatus` landblock count is back to the baseline. |
| S9 | Put an island from appendix C in `instances.json`, restart, `/instance open test-holtburg`, walk into the ring. | Message "You can go no further that way." and you are back inside the island within a second or two. |
| S10 | `A` logs out inside an instance, then logs in. | `A` is in the persistent world at the return position. `/instance list` shows 0 players. |
| S11 | Run appendix B query 1 (ephemeral ids). | 0 rows. |
| S12 | Persistent world: `B` dies to a monster, drops an item, recalls. | Corpse and drops as before. Everything as before. |

## 4. Full test matrix

### A. Startup and `instances.json`

| ID | P | Steps | Expected |
|---|---|---|---|
| A1 | P0 | Start with the shipped file (`"islands": []`). | No `[INSTANCE]` line at all. No islands. |
| A2 | P1 | Delete `instances.json` from the output folder, start. | `[INSTANCE] There is no ...instances.json, so there are no islands`. Server starts normally. |
| A3 | P0 | Add `test-holtburg` (appendix C), start. | `[INSTANCE] Island test-holtburg: 9 landblocks, 8 of them ring`. `/instance list` shows it with "9 landblock(s), 8 of them ring". |
| A4 | P1 | One mistake at a time, each next to a good island (table below). | The error is logged as `[INSTANCE] <path>: island 'x': <text>`, that island is missing from `/instance list`, the good island loads, and the server starts. |
| A5 | P2 | Comments, trailing commas, property names in capitals (`"Name"`). Then `"instanceOnly": "true"` as text. | The first three are accepted. The text is rejected: it has to be a JSON boolean, and the island is left out with an error. |
| A6 | P1 | Put a landblock of an instance-only island on the server's landblock pre-load list, if you use one. | One `[INSTANCE] Something asked the persistent world for landblock XXXX ...` error at startup, the landblock is not loaded. Take it off the list. |

Mistakes for A4 and the text to expect: no `name` (`it has no name`), a name used twice (`another island has the same name`), a name starting `capstone:` (`are for the capstone dungeons`), `instanceOnly` missing (`instanceOnly has to be set`), landblock `"E7"` (`is not a landblock`), no landblocks (`it has no landblocks`), entry in the ring or outside (`not one of the island's landblocks`), no entry (`entry: it is missing`), a cell that is not eight hex digits (`is not a cell`), `qw` 0 (`the rotation is empty`), `return` inside the island (`inside the island`), `return` in an instance-only island (`only exists as an instance`), more than 400 landblocks (`400`), `bufferRing` 5 (`bufferRing has to be from 0 to 4`), text that is not JSON (`The file can't be read`).

### B. Lifecycle

| ID | P | Steps | Expected |
|---|---|---|---|
| B1 | P0 | `/instance here 0`. | "Opened instance N (1 landblock(s))...". You arrive at the same spot. `list` shows "N: here-XXXX - 1 player(s) in it". |
| B2 | P0 | `/instance leave`. | Back at the spot you left from. `/instance info` says instance 0. `list` shows "empty for x of y min". |
| B3 | P1 | `/instance here 3` and `/instance here 9`. | 49 landblocks each (9 is clamped to 3). All loaded, no errors. Note how long it took. Landblock count in `/serverstatus` is up by 49. |
| B4 | P0 | Timeout 1 minute. Enter, leave, watch `/instance list`. | Deleted after one minute. Log `[INSTANCE] Deleted instance N`. Landblock count and memory return to the baseline. Repeat 10 times: memory does not keep growing. |
| B5 | P0 | `/instance close N` with `A` inside. | `A` is sent to the return position (or sanctuary). The instance is gone within a couple of ticks. Closing it again says "There is no instance". |
| B6 | P1 | `/instance open test-holtburg` from two accounts. | Both are in the same instance. `open test-holtburg new` makes another one. |
| B7 | P1 | `/instance enter 0`, `enter 99999`, `enter abc`, `leave` when not in an instance, `here` when already in one. | `enter 0` is a leave. "There is no instance 99999." Usage text. "You are not in an instance." "You are already in an instance. Leave it first." |
| B8 | P2 | A Developer-level and a Player account try `/instance`. | Not available. |

### C. Isolation

Use two characters at the same coordinates: one in an instance, one in the persistent world.

| ID | P | Steps | Expected |
|---|---|---|---|
| C1 | P0 | `ADMIN` in an instance, `A` outside, same spot. | Neither sees the other. `/who` still lists both. Global chat and tells work. |
| C2 | P0 | `/ci` an item in the persistent world, enter an instance, look. Then the other way round. | Items are only seen from the instance they were made in. |
| C3 | P0 | A monster made in the instance next to `ADMIN`, with `A` outside. Then `/instance enter N A`. | It attacks only `ADMIN`. After `A` is sent in it can attack `A` too. |
| C4 | P1 | `A` casts a bolt, a ring spell and fires an arrow at the other's spot. | Nothing is hit, no effects are seen across. Both directions. |
| C5 | P1 | Both run through each other's spot. | No collision, no pushing. |
| C6 | P0 | Two instances of `test-holtburg`. Kill a monster and open a door in one. | The other instance's copy is untouched. Corpses and loot are not seen across. |
| C7 | P0 | Hard-coded activation targets: in a landblock from the table in appendix C, `/instance here 0`, use the button or lever. Have a second character stand by the same door in the persistent world. | The door in the **instance** reacts. The persistent world's door does not. No `couldn't find activation target` warning in the log. |
| C8 | P1 | A lever or plate with linked children (`landblock_instance_link`) in an instance. | The children act and appear in the instance only. |
| C9 | P1 | A spawn camp (generator) in an instance. Kill its monsters. | They respawn in the instance. The persistent world's copy does not change. |
| C10 | P1 | Local chat, `/say`, emotes. | Not heard across instances. Tells, fellowship and allegiance chat work across. |
| C11 | P2 | A portal summoned inside an instance. | Seen and usable only there. Its destination is in the persistent world. |
| C12 | P2 | Chess in an instance. | The pieces appear in the instance. |
| C13 | P2 | A vendor and an NPC with a quest inside an instance. | Buying, selling and quest flags work. |

### D. Movement, portals and teleports

| ID | P | Steps | Expected |
|---|---|---|---|
| D1 | P0 | From inside an instance: lifestone recall, `/telepoi`, a portal spell. | You arrive in the persistent world. `/instance info` says 0. No errors. |
| D2 | P0 | `/tele` or `/teleloc` to coordinates inside the instance's landblocks, then outside them. | Inside: you stay in the instance. Outside: persistent world. |
| D3 | P0 | `/teleto A` with `A` in an instance, from the persistent world. `/teletome A`, `/movetome` on `A`, `/teleallto`. | You join `A`'s instance. `A` is brought into yours. `teleallto` brings everyone into the destination's instance. |
| D4 | P1 | With an instance-only island configured: `/tele` to it, and a portal weenie whose destination is in it. | "That place is no longer there." or "only exists as an instance". You stay where you were. |
| D5 | P1 | Portals inside a dungeon instance, and its exit portal. | Inside portals keep you in the instance. The exit goes to the persistent world. |
| D6 | P1 | 20 times quickly: `/instance enter N`, `/instance leave` (or the same on `A`). | No stuck "teleporting", no duplicate models, no client crash, no errors. You end up where expected. |
| D7 | P1 | Send `A` into an instance while `A` is casting a recall. | The end state is consistent: `/instance info A` has no `MISMATCH`. |
| D8 | P2 | `/telereturn` after `/teletome` brought a player into an instance. | The player goes back to the old coordinates. They stay in the instance only if that spot is inside its landblocks, otherwise they are in the persistent world: the instance the spot was in is not remembered. |

### E. Player lifecycle

| ID | P | Steps | Expected |
|---|---|---|---|
| E1 | P0 | Log out inside an instance, log in. | In the persistent world at the return position (the sanctuary if the template has none). The instance's player count went down. |
| E2 | P0 | Pull the network on a client inside an instance. | Treated as a logout when the server drops the session. `list` shows 0 players. |
| E3 | P0 | With a player in a normal (not instance-only) instance: (a) stop the server cleanly, start it, log in; (b) do it again but kill the process instead. | (a) The player is logged out first, so they are at the return position, as in E1. (b) The player is at their last saved coordinates in the persistent world, which is the same spot. In both, `/instance list` is empty: instances do not survive a restart. |
| E4 | P0 | Kill the server process with a player inside an **instance-only** island. Start, log in. (Or, with the server stopped, edit the character's `biota_properties_position` row, see appendix B.) | The player is moved to the return position or sanctuary. Log: `[INSTANCE] <name> was saved inside <template>, which only exists as an instance. Moving them to ...`. |
| E5 | P0 | Die in an instance. Do it away from the lifestone: if the sanctuary is inside the instance's landblocks you respawn inside it. | No corpse. No items lost. Vitae applied. Respawn at the sanctuary. |
| E6 | P0 | Die in the persistent world. | Corpse, drops and corpse run as before. |
| E7 | P0 | In an instance: drop a stack, split a stack onto the ground, drop an equipped item. Then move items between containers, give to an NPC, trade. | The drops are refused ("You can't drop items here."), nothing changes. The rest works. |
| E8 | P1 | Trade with a player in the same instance. Try to trade with someone outside. | Works inside. Impossible across (they cannot be seen). |
| E9 | P1 | Fellowship, `A` and `B` in the same instance. Kill a monster. | XP and kill credit are shared. |
| E10 | P0 | **Fellowship across instances.** `A` in the persistent world and `B` in an instance, same coordinates, same fellowship. `B` kills a monster. Check with `/fellow-info` and `/fellow-dist`. | `A` gets no shared XP and no kill-task credit. The distance scalar is 0 across instances and 1 inside one. Quest XP is still shared. |
| E11 | P0 | **Pets.** `A` summons a pet in the persistent world, then `/instance enter N A`. Also summon inside an instance, then leave. | The pet is destroyed, within seconds (its landblock has to tick, so a dormant one may take a little longer). `A` can summon another. No stranded pet in the old instance. |
| E12 | P0 | **Housing.** Inside an instance copy of a landblock that has a slumlord: try to buy and to rent. | "You can't do that here." No change to the character's house data (appendix B query 5). |
| E13 | P1 | Send `A` into an instance while `A` has a vendor or chest window open. | Nothing breaks: no errors, and the vendor or chest cannot be used from the instance. (Whether the window closes by itself is up to the existing teleport code.) |
| E14 | P1 | Allegiance: a vassal kills in an instance. | The patron's XP passup works (allegiance is global by design). |
| E15 | P2 | Quest kill counts and collect items in an instance. | Counted. |
| E16 | P2 | House recall from an instance. | Works. You are in the persistent world. |

### F. Islands, rings and instance-only

Use `test-holtburg` (1 landblock plus ring) and `test-big` (3 x 3 plus ring) with `instanceOnly: false` first. For F7 to F9 use a landblock without housing and set `instanceOnly: true`.

| ID | P | Steps | Expected |
|---|---|---|---|
| F1 | P0 | `/instance open test-big`. | You arrive at the entry position. `list` shows "25 landblock(s), 16 of them ring". |
| F2 | P0 | Walk over the borders between the interior landblocks. | No gaps, no invisible walls, buildings and trees are seen across borders. |
| F3 | P0 | Walk into the ring from each side and each corner, five times. | "You can go no further that way." You are teleported to the last spot you stood on in the interior. No loops, no `MISMATCH`, no errors. |
| F4 | P1 | Run with buffs, jump over the border, get knocked into the ring by a strong monster or spell. | Turned back every time. Never in the void, never stuck outside. |
| F5 | P1 | Run out of the entry position into the ring before doing anything else. | You are sent back to the entry position. |
| F6 | P0 | At the outer edge of the outermost ring landblock. | Solid: players are put back, monsters, pets and projectiles stop. |
| F7 | P0 | **Instance-only.** Set `instanceOnly: true`, restart. From the persistent world: `/tele` to it, walk toward it from the next landblock, and keep pushing on. | `/tele` is refused. You are stopped at the border like at a wall (put back each time you push on), and `/myloc` `Location` stays in the persistent landblock. At most one `[INSTANCE] Something asked the persistent world for landblock ...` error for each landblock. Walking along its border does not make the loaded landblock count in `/serverstatus` grow. |
| F8 | P1 | Two instances of the same island, one player in each. | The ring works separately in each. Nothing is shared. |
| F9 | P1 | `/instance close` the island with a player in it. | The player goes to the `return` position, or the sanctuary if there is none. |
| F10 | P1 | An island with `bufferRing: 0`. | The edge is solid, nobody is turned back. Objects that would spawn within about a metre of the edge do not appear. |
| F11 | P2 | An island at the edge of the map (x or y 0 or 254). | The ring stops at the map edge. No errors. |
| F12 | P2 | A 5 x 5 island (49 landblocks with the ring). | Creation time and memory noted. `close` gives the memory back. |
| F13 | P2 | Entry position with a `z` under the ground, and one where the cell does not match `x` and `y`. | Note what happens. These are mistakes in the file, not bugs, but it shows what an admin sees when the entry is wrong. |
| F14 | P0 | **Run past the edge, then teleport back.** In an instance run past the edge for 30 seconds, then `/telepoi` or `/tele` to the middle of the block, in the same instance. (This was the bug found by S4: see section 7.) | All the landblock's objects (NPCs, monsters, portals, lifestone) are still shown. `/instance info` has no `MISMATCH`. If a character is ever in this state, `/instance leave` then `/instance enter N` (or logging out and in) fixes it. |

### G. Fellowship dungeons (`capstone_instanced_dungeons`)

| ID | P | Steps | Expected |
|---|---|---|---|
| G1 | P0 | Property empty. Enter a capstone dungeon. | Exactly as before: numbered copies, `@capstone` shows them, no `capstone:` instance. |
| G2 | P0 | Set it to `Glenden Wood Dungeon`. Fellowship `A` (leader) + `B`; `A` enters, then `B` enters. | Both are in the same instance. `@capstone` shows "instance N, 2 player(s)". `/instance info` shows template `capstone:Glenden Wood Dungeon`. |
| G3 | P0 | A second fellowship enters. | A different instance. The two do not see or affect each other. |
| G4 | P0 | Landblock modifiers chosen by the leader (loot quality, lethality...). | They apply to monsters and loot in the instance, the same as in a copy. Compare with a copy of the same dungeon. |
| G5 | P0 | `A` and `B` go through the entrance at the same moment, 10 times (a new fellowship each time, or wait until the instance has been deleted). | One instance for the fellowship, never two: `/instance list` shows one `capstone:` instance. |
| G6 | P1 | Leave by the exit. Re-enter within the timeout. Then leave and wait past it. | Within it: the same instance, monsters killed stay dead. After it: deleted, and a new entry gives a fresh dungeon. |
| G7 | P1 | Disband or change the leader while inside. | Players stay. No errors. The instance is deleted after everyone has left and the timeout has passed. |
| G8 | P1 | Enter the dungeon without a fellowship. | Same as before. No errors. |
| G9 | P1 | Add `Lugian Mines` and `Mines of Despair` to the property. | Ignored: they still use their copies. |
| G10 | P1 | Die inside. | No corpse, nothing lost, sanctuary respawn. |
| G11 | P1 | Monster levels and health against `docs/fellowship-dungeon-scaling.md`, for the same fellowship size, instance against copy. | The same. |
| G12 | P1 | Stop the server with a fellowship inside, start, log in. | Sanctuary, as for the copies. |
| G13 | P2 | `@capstone <dungeon>` for an instanced dungeon. | It only looks at the copies, so it can say "No instances of ... are active" while an instance is. Known: use plain `@capstone`, `/instance list` and `/instance info`. |

### H. Admin tools

| ID | P | Steps | Expected |
|---|---|---|---|
| H1 | P0 | `/instance info` with nothing selected, with an object appraised, with `A`, with `0x<guid>` of an object in another instance, with nonsense. | You, and the selected object, are described (instance, place, no `MISMATCH`). A guid works from anywhere. Nonsense gives a clear message. |
| H2 | P0 | `/myloc`, `/getinfo` on an appraised object. | Show `Instance:`. |
| H3 | P0 | `/instance enter N A`, `leave A`, `enter 0 A`. | `A` is moved. The audit channel shows who sent whom. |
| H4 | P0 | `/instance move N` on an item and on a monster made in the persistent world (`/ci`). Then `move 0` to bring them back. | They disappear from one instance and appear in the other, at the same spot. A monster fights only players in the instance it is in. An item can be picked up there. |
| H5 | P0 | Refusals of `/instance move`: a player, a door or statue (by guid), a monster from a spawn camp, a generator, an item in a bag, a closing instance, an instance that does not exist, a place outside the target's landblocks, `move 0` into an instance-only landblock. | Each is refused with its reason, and the object is exactly as it was. |
| H6 | P1 | `/create`, `/ci`, `/createnamed`, `/createliveops`, `/moveto` inside an instance. `/getinfo` on the result. | The object is in your instance. |
| H7 | P1 | `/nudge`, `/rotate` and the content commands on a static in an instance. | Refused. No new file in the `Content` folder, and no world-database change. |
| H8 | P2 | `@capstone`. | Lists open capstone instances. |

### I. Persistence and database

| ID | P | Steps | Expected |
|---|---|---|---|
| I1 | P0 | Record appendix B query 4. Run a whole instance session without picking anything up: create, `/ci` 20 items and 10 monsters, kill some, let corpses drop loot, close. Wait two minutes. Record again. | Nothing new except what belongs to the characters (login time, position). Query 1 gives 0. |
| I2 | P0 | In an instance pick up an item made there, buy from a vendor, use consumables. Log out, log in. | The character's items are saved normally. |
| I3 | P0 | After H7 and F-tests. | World database unchanged (`landblock_instance` counts, `last_Modified`), and the `Content` folder has no new files. |
| I4 | P1 | An item made in the persistent world, saved to the shard, then `/instance move`d into an instance. Close the instance, restart. | The item does not reappear in the persistent world. If its row stays in `biota`, record it. |
| I5 | P0 | Appendix B queries 1, 2 and 3 after everything in this plan. | 0 rows. |
| I6 | P1 | Appendix B query 5 after E12. | No house rows changed. |

### J. Concurrency, capacity and soak

| ID | P | Steps | Expected |
|---|---|---|---|
| J1 | P0 | **Parallel ticking on** (both flags true). Run the smoke test and C1 to C6 with at least three players in two instances plus the persistent world. | No `cross-thread operation`, no `[INSTANCE] Cell ... was looked up in the persistent world by code running for instance`, no `[INSTANCE] ... was spawned into the persistent world`, no NullReference. |
| J2 | P1 | The same with both flags false. | The same. |
| J3 | P1 | 30 times: `/instance here 1`, enter, leave, then wait for the deletion (or `/instance close N` to skip the wait). | After all: `/serverstatus` landblocks and memory are back near the baseline. Instance ids keep counting up. No errors. |
| J4 | P1 | `/instance here 2` and `/instance close N` as soon as you arrive. Also right after `/instance open`. | No exception. |
| J5 | P1 | Three or four players `/instance enter N` at the same second. Two admins `/instance open test-holtburg` at the same second. | Everyone is in. One shared instance, not two. |
| J6 | P1 | Soak: two or three players in two instances playing normally for two hours. `/serverstatus` every 15 minutes. | Memory levels off. No error growth in the log. No lag. When everyone leaves and the timeouts pass, memory comes back. |
| J7 | P2 | Ten instances of `test-big` at once (250 landblocks), then compare tick time with the baseline. Empty ones: does the server load fall? | Note the numbers. Instances are kept loaded until deleted, but idle landblocks should go dormant like others. |
| J8 | P2 | Create and close `test-big` 20 times. | No `Out of ephemeral static GUIDs` in the log. |

### K. The persistent world must not have changed

The physics change is the biggest risk: a place with no landblock is now solid instead of passable.

| ID | P | Steps | Expected |
|---|---|---|---|
| K1 | P0 | With islands and `capstone_instanced_dungeons` empty: a full session of normal play (walk, combat, spells, loot, vendors, trade, fellowship XP, allegiance, chat, portals and recalls, house entry, storage chests). | Everything as before. |
| K2 | P0 | Teleport somewhere remote, then run across several landblock borders in a row, on foot, fast, and flying (admin). | No invisible walls at borders of landblocks that were not loaded yet. |
| K3 | P0 | Walk, swim and jump at the edge of the map (landblock x or y 0 or 254). | The same as before: you cannot leave the map, nothing gets lost. |
| K4 | P0 | Leave an area and wait for its landblocks to unload. Come back. | They unload and load as before. No errors. |
| K5 | P1 | Dungeons, building interiors, stairs, doors, cell seams. | No sticking. |
| K6 | P1 | Monsters, pets and projectiles crossing landblock borders. | Normal. |
| K7 | P1 | `/serverstatus`, `/serverperformance`, `/landblockstats` under the same load as a baseline build (`main`). | No measurable regression (within noise). |
| K8 | P1 | Buy, abandon and pay rent for a house in the persistent world. Guest lists. | As before (the new guard only applies inside instances). |
| K9 | P2 | `/tele`, `/teleto`, `/teletome`, `/movetome`, `/create`, `/ci` with nobody in an instance. | As before. |
| K10 | P2 | Startup time and memory with the feature unused. | The same as the baseline. |

### L. Systems that look at positions

| ID | P | Steps | Expected |
|---|---|---|---|
| L1 | P1 | **Portal storms / resonance zones** (`ResonanceZoneService`). It groups every online player by landblock number and checks coordinates, without the instance. Stand in an instance of a landblock that has an active storm zone. | Record whether the storm hits you. **A decision is needed** (section 5). |
| L2 | P1 | Look for other systems that scan players or objects by position: search the server code for `GetAllOnline()`, `Location.Landblock` and `DistanceTo(`. For each, run the test "two players at the same coordinates in different instances" and see if one affects the other. | Nothing crosses. Anything that does goes on the list in section 5. |
| L3 | P2 | Events (`EventManager`) that turn generators on and off. | Instance copies follow the event like the persistent landblock. Note it. |

### M. Failures

| ID | P | Steps | Expected |
|---|---|---|---|
| M1 | P1 | `/forcelogoff` a player standing in an island. | No exception. The player is removed. Instance membership is cleaned up. |
| M2 | P1 | Two admins `close` and `enter` the same instance at the same time. | No exception. Consistent afterwards. |
| M3 | P2 | `close` while a player is dead or in portal space. | They arrive in the persistent world. Not stuck. |
| M4 | P2 | Count `[INSTANCE]` lines in a ten minute idle session. | A handful, not one per tick. |

## 5. Decisions needed

1. **Portal storm zones in instances (L1).** Today a storm hits players in every instance of that landblock. Should instances be exempt, or is a storm an event of the world that reaches everyone?
2. **Fellowship XP across instances (E10).** Fixed so that members in different instances do not share kill XP or credit. Quest XP is unchanged. Confirm this is what you want.
3. **Death in an instance (E5).** No corpse and no loss of items, my reading of "prevent corpses and drops". Vitae still applies. Confirm.
4. **Anything L2 finds.**

## 6. Exit criteria

- Every P0 passes, on both threading settings (J1 and J2).
- No unexpected `[INSTANCE]` error, exception or `cross-thread` line in the logs, and no ephemeral ids in the database (appendix B).
- K1 to K4 pass, and K7 shows no regression.
- Every P1 failure is fixed or accepted in writing.

## 7. Findings from running the plan

| Date | Test | Result | What it showed | Status |
|---|---|---|---|---|
| 2026-09-20 | S1, S2, S3 | Pass | | |
| 2026-09-20 | S4 | **Fail** | A player ran nine landblocks past the edge of a `/instance here 1`. `/myloc` had `Location` in landblock DD4D while `Physics` was still at the west edge of E64E and `CurrentLandblock` was E64E. The log had ten `[INSTANCE] ... moved to ..., which is not part of instance 1` warnings and no exception. A `/telepoi hebian-to` back to E74E, in the same instance, then showed none of the landblock's objects. | Cause: the client moves itself and reports positions; the server stopped its physics object at the edge but accepted the reported position, so the client left and dropped the objects while the server still counted them as known, and did not send them again on the teleport back. Fixed in `Player_Tick.cs` (the position is refused and the client put back), and `/instance info` now reports a position outside the instance as a `MISMATCH`. Retested below. |
| 2026-09-20 | S4, F14 | Pass | Run again after the fix: the client is put back at the edge, and the landblock's objects are still there after teleporting back. The server log since the restart has no `moved to ..., which is not part of instance` warning. | Fixed and verified. |

## Appendix A. `[INSTANCE]` log lines

Expected in normal use: `Created instance N: <template> (x landblocks)`, `Deleted instance N: ...`, `Island <name>: ...` (startup).

Only when provoked on purpose: `Something asked the persistent world for landblock XXXX, which only exists in instances` (F7, D4, A6), `<name> was saved inside <template>, which only exists as an instance. Moving them to ...` (E4), `There is no ...instances.json, so there are no islands` (A2), `<path>: island 'x': <mistake>` (A4).

**Always a bug, please report with the stack trace that comes with the line:**

- `... entered cell X of instance M. That cell was looked up in the wrong instance.`
- `Cell X was looked up in the persistent world by code running for instance N.`
- `... was spawned into the persistent world by code running for instance N.`
- `... can't enter instance N at <place>, which is not part of it` and `... moved to <place>, which is not part of instance N`.
- `Landblock ... entered AddWorldObjectInternal (or RemoveWorldObjectInternal) in a cross-thread operation.`
- `Out of ephemeral static GUIDs`.

## Appendix B. SQL checks (shard database)

Run 1 to 3 after every test session. The range `2146435072` to `2147483647` is `0x7FF00000` to `0x7FFFFFFF`, the ids that the statics of instances get. Nothing may ever be saved with one.

```sql
-- 1. objects saved with an id from the range for instances (expect 0)
SELECT COUNT(*) FROM biota WHERE id BETWEEN 2146435072 AND 2147483647;

-- 2. anything that points at such an object (expect 0)
SELECT COUNT(*) FROM biota_properties_i_i_d WHERE value BETWEEN 2146435072 AND 2147483647;

-- 3. characters saved inside an instance-only island (expect 0). 0xA9B4 is an example: use your island's landblocks
SELECT object_Id, obj_Cell_Id FROM biota_properties_position
WHERE position_Type = 1 AND (obj_Cell_Id >> 16) IN (0xA9B4);

-- 4. counts to record before and after a session
SELECT (SELECT COUNT(*) FROM biota) AS biotas,
       (SELECT COUNT(*) FROM biota_properties_position) AS positions,
       (SELECT COUNT(*) FROM biota_properties_i_i_d) AS iids,
       (SELECT COUNT(*) FROM house_permission) AS house_permissions;

-- 5. a character's house (type 33 is House). Compare before and after E12
SELECT object_Id, type, value FROM biota_properties_i_i_d WHERE type = 33 AND object_Id = <character guid>;
```

To stage E4, stop the server, then: `UPDATE biota_properties_position SET obj_Cell_Id = <a cell in the island> WHERE object_Id = <character guid> AND position_Type = 1;`, and start the server. (The server caches offline characters, so restart it after the edit.)

## Appendix C. Test data

**Sample `instances.json`** (Holtburg is used because `0xA9B40019 [84 7.1 94]` is the default sanctuary, a place known to be valid. It is not instance only, so nothing changes in the persistent world):

```json
{
  "islands": [
    {
      "name": "test-holtburg",
      "landblocks": [ "A9B4" ],
      "bufferRing": 1,
      "instanceOnly": false,
      "entry": { "cell": "0xA9B40019", "x": 84, "y": 7.1, "z": 94, "qz": -0.0784591, "qw": 0.996917 }
    },
    {
      "name": "test-big",
      "rectangles": [ { "from": "A8B3", "to": "AAB5" } ],
      "bufferRing": 1,
      "instanceOnly": false,
      "entry": { "cell": "0xA9B40019", "x": 84, "y": 7.1, "z": 94, "qz": -0.0784591, "qw": 0.996917 }
    }
  ]
}
```

For the instance-only tests (F7 to F9) pick a landblock **without player housing**, stand in the middle of it, run `/myloc`, and copy the cell, `x`, `y`, `z` and rotation into `entry`. Do it on the copy of the database.

**Hard-coded activation targets (for C7).** Your audit of `ace_world` found 38 weenies that name a static object by its world-database guid (`ActivationTarget`). The target is in the landblock the guid says; the activator is normally in the same one. Try the landblocks with the most, first (6545 and 6546 have 14 of the 38).

| Landblock | Activator weenies (wcid) | Target guids |
|---|---|---|
| 6545 | 26534, 26586, 26635, 26650, 26651, 26652, 26653, 26654, 26655, 26656 | 76545074, 76545067, 7654504E, 76545072, 7654507B, 7654507D, 7654507E, 76545081, 76545082, 76545084 |
| 6546 | 26657, 26658, 26668, 26669 | 7654608A, 7654607E, 76546093, 7654608F |
| 5E4D | 25726, 25711, 25712 | 75E4D0AE, 75E4D001, 75E4D003 |
| 6049 | 25493, 1034400, 25453 | 7604902E, 76049043 (both of the last two) |
| 604A | 25580, 25587 | 7604A0D0, 7604A008 |
| 6448 | 25753, 25754 | 76448001, 76448000 |
| 9EB4 | 14921, 14922, 14923 | 79EB4007 (all three) |
| 00F2, 2581, 5C4C, 5F4F, 5F50, 5F51, 6243, 7111, 7211, 828E, B095 | 29583, 24493, 27949, 27918, 27917, 27919, 27160, 7432, 7431, 5636, 50131 | 700F2003, 72581042, 75C4C077, 75F4F03E, 75F5003E, 75F5103E, 762430EB, 77111002, 77211000, 7828E001, 7B095001 |

## Appendix D. Results

Copy this table, one row per test you ran.

| ID | Result (Pass / Fail / Blocked) | Build | Tester | Notes, log lines, bug |
|---|---|---|---|---|
| S1 | | | | |
