# Echoes of Anarchy — Game Design

A Purge-night action/story game for PC. Full game is about 60 minutes; the demo is about 15 minutes (Act 1).
**Every asset is made in code:** geometry, textures, materials, lighting, VFX and audio. Nothing is imported from outside.

## Decisions agreed with the owner
| Topic | Decision |
|---|---|
| Concept | "Reach home": paramedic Daniel Reyes crosses the city on Purge night to reach his daughter Mia (12), then defends the house until 07:00 |
| Camera | First person, chest-mounted **bodycam** (wide lens, burned-in REC/timestamp) |
| Gameplay | Balanced action/stealth: scarce ammo and lethal combat. Hide, distract, flee or fight |
| Language | English UI and voice text, Turkish subtitles |
| Protagonist | Daniel Reyes: ex-Marine, divorced, paramedic, afraid of becoming violent again |
| Twist | Some of Mia's phone calls are **AI voice clones** made by "The Smilers", who are luring Daniel along routes for their livestream |
| Morality | A hidden **Ledger** records actions caught on the bodycam and decides 3 endings |
| Visual taste | The owner wants a **clean image**: keep grain, scanlines and chromatic aberration very low, and keep the streets readable rather than pitch dark |

## Character look (phase 5, proposed by the agent; the owner can still change it)
| Faction | Look | Why |
|---|---|---|
| Smilers | Black suits, white gloves, glossy black full-head mask with the LED grin; the **tie matches the LED colour** | A "uniform" for the stream; the grin stays the signature image |
| Looters | Hoodies or work jackets, jeans/cargo/sweatpants. Head: hood + bandana (most), **rubber pig mask**, **porcelain angel face under a hood with a faintly glowing wire halo**, or bare face with a beanie/cap | Disorganised Purge crowd; pig and angel were requested masks. The halo glows a little so it reads at night, like the LEDs |
| Civilians | T-shirts, sweaters or open jackets; visible faces, hair (short/long), some caps | Readable as non-threats |
Bodies vary in height, bulk, belly and male/female proportions.

## Story
- **Frame:** 22 March 2034, 07:40. Detective Hale reviews Daniel's bodycam footage ("EXHIBIT 14"). Chapter transitions are tape rewind/fast-forward with the detective's notes.
- **Factions:**
  - **The Smilers:** neon LED smile masks; a hunting party that livestreams.
  - **Looters:** disorganised, can flee or surrender.
  - **Purifiers:** armoured militia; late game only.
- **Twist hints:** small glitches in Mia's voice, and things Mia couldn't possibly know.
- **Endings** (from the Ledger):
  1. **Saviour:** Daniel and Mia see the morning; the footage brings the Smilers down.
  2. **Echo:** Mia survives but fears her father; the footage convicts him.
  3. **Broadcast:** Daniel gets home, but the Smilers' stream wins.

## Full game (~60 min)
| Act | Time | Location | Play |
|---|---|---|---|
| 1 — Siren (DEMO) | 18:52–20:10 | Harlow Ave, gas station, alleys | Crash → escape → first fight, first moral choice, first fake-voice hint |
| 2 — Broadcast | 20:10–23:30 | Abandoned subway, mall | The Smilers' "game course"; the twist is revealed |
| 3 — Way Home | 23:30–03:00 | Suburbs, Walt's house | Stealth, Purifier patrols |
| 4 — 07:00 | 03:00–07:00 | Reyes house | Barricades + home defence, finale, trial |

## Demo flow (15 min)
1. **0–2 min, cold open:** in the ambulance, radio chatter, Mia's voicemail. At 19:00 the Purge siren sounds and the street lights die. A truck rams the ambulance and it rolls over.
2. **2–5 min, escape:** partner Luis is wounded; treat him or leave him (first Ledger entry). Movement, crouch and med-kit tutorial. First Smilers sighting.
3. **5–9 min, gas station:** a civilian hides behind the counter; three looters at the door. Sneak past, throw a bottle as a distraction, or fight with a 7-round pistol. One looter surrenders: spare or execute.
4. **9–13 min, alleys:** a call — "Daddy, come to the school" — with a slight voice glitch. A chase. A Smiler waves at the camera.
5. **13–15 min, cliffhanger:** at the school door hangs Daniel's Marine photo and the words "SMILE, DANNY. YOU'RE LIVE." The footage cuts, the detective leaves a note, then TO BE CONTINUED.

## Development phases
| # | Phase | Status |
|---|---|---|
| 1 | Foundation: input, FPS controller, bodycam rig, flashlight, post-FX, HUD | **Done**, tested by the owner |
| 2 | World: texture synthesis, buildings, props, night sky, fire/smoke FX, reflection probes, NavMesh | **Done**, tested by the owner |
| 3 | Audio: synthesized foley, ambience, distant Purge events, Purge siren, reverb zones | **Done**, tested by the owner |
| 4 | Combat + AI: pistol (own synthesized gunshot), bottle throw, damage, ragdoll, NPC FSM (patrol/suspicious/search/attack/flee/surrender), sight + hearing perception on the NavMesh | **Done**, committed by the owner (the melee animation and jitter fix were not explicitly confirmed) |
| 5 | Characters: upgrade the basic segmented humanoid from phase 4 (better body shapes, clothing detail, IK feet, more masks such as pig and angel, smoother animation) | **Done**, tested by the owner (liked it) |
| 6 | Narrative: dialogue + EN/TR subtitles, Ledger, phone calls, demo sequences 1–5, GameClock-driven 19:00 siren | **In progress**, split into steps 6a–6e (below) |
| 7 | Polish (to be re-planned for the 30-minute game): main menu (VHS style), tape transitions, settings, shader warm-up (avoids the cyan placeholder flash on first play), performance, build | A first shareable build exists (`Tools > Echoes > Build Demo (Windows)`, owner request before phase 7); the rest is not started |

The owner tests every phase in Play mode before the next one starts.

### Phase 6 steps (agreed with the owner: each step is tested on its own)
| Step | Content | Status |
|---|---|---|
| 6a | Narrative foundation: dialogue director, EN + TR subtitles, babble voices, phone call / voicemail / radio, 19:00 Purge beat (siren, blackout wave, emergency lamps), sample lines | **Done**, committed (`2f05882`) |
| 6b | Sequences 1–2: ambulance cold open, voicemail, siren, truck crash; Luis wounded (treat or leave → Ledger), movement/med-kit tutorial, first Smilers. Needs ambulance, truck, crash site | **Done**, tested by the owner and committed (with the revision, intro card and reload polish) |
| 6c | Sequence 3: gas station (hidden civilian, three looters; sneak, distract or fight; spare or execute). Needs the gas station | **Done**, tested by the owner and committed (with blood, damage numbers and 6d) |
| 6d | Sequence 4: alleys, the cloned "Daddy, come to the school" call, chase, a Smiler waves at the camera | **Done**, tested by the owner (several rounds) and committed |
| 6e | Sequence 5: school door, Marine photo, "SMILE, DANNY. YOU'RE LIVE.", footage cuts, detective note, TO BE CONTINUED | **Code done**, in the first build; after it the owner asked for fixes (checkpoints, cab camera, gunshot, the clerk, melee, characters: see the last section), awaiting their test (`startAt = School` on the "Demo" object to start in the school yard) |

Phases 1–7 cover the 15-minute demo (Act 1). Acts 2–4 of the full game are not planned into phases yet; they will be planned after the demo.

## Narrative decisions (phase 6)
| Topic | Decision | Why |
|---|---|---|
| Voices | **Babble voices**: synthesized speech-like syllables timed to each line (no real words); meaning comes from subtitles. Phone and radio voices are band-limited and distorted | Owner's choice: no imported audio, and real TTS would sound robotic |
| Subtitles | **One language only** (changed by the owner after testing 6b): English (the voice text) or Turkish, with the speaker's name in colour. Chosen on the very first start on the intro card, saved in PlayerPrefs (`GameSettings`); F3 switches it until the phase 7 settings menu | Owner's choice (first both languages together, then one at a time) |
| Intro card | Over black before "EXHIBIT 14": the date (21 March 2034), what the Purge is (the 2029 Renewal Act: 12 hours, every crime legal, no police/fire/ambulance 19:00–07:00; "Night of Renewal" officially), and below it a cold, reactionary quote from the law's fictional author, Senator Harlan Voss. In the chosen language; Space/Enter/click skips. The clock and the ride wait for it | Owner asked for it: sets up the night before the footage starts |
| Cloned Mia | Labelled "MIA", exactly like the real Mia. Her voice is cleaner, then glitches (stutters, dropouts, digital crunch). She says **"Daddy"**, while the real Mia says **"Dad"**, and she mentions things Mia can't know ("Everyone's watching") | The hints must be subtle and only make sense in hindsight (proposed by the agent) |
| Phone | Incoming calls ring twice, then Daniel answers on his own; there is no answer key | Keeps scripted calls simple (proposed by the agent; can change) |
| 19:00 | EBS tone and sirens; about 9 s later the street lights die in a wave rolling out from the player, then about 55 % come back on emergency power (dim and failing). Before 19:00 the city is quiet (no distant Purge events) | "The street lights die" in the story, while keeping the streets readable as the owner wants |

## Demo route and sequences 1–2 (phase 6b, proposed by the agent; the owner can still change them)
| Topic | Decision | Why |
|---|---|---|
| Route through Harlow | Cold open on the cross street (z 30, eastbound), a tape skip, then south down Harlow Ave. The crash is on the avenue just south of the intersection (wreck rests around x −0.6, z −18). From there Daniel goes **south**: gas station (6c) on the southern avenue, then west through the alleys behind the west row (6d, see "Sequence 4"), to the school (6e) at their north-west end | Uses the existing district without rebuilding it; the Smilers can watch from the lit intersection |
| Length of the ride | About 45 s in the cab (radio, Mia's voicemail, a few lines with Luis), then the footage **skips** from about 18:53 to 18:59:54 (tape glitch), then about 19 s to the impact | The 8 real minutes to 19:00 would be too long; a jump cut fits "bodycam footage as evidence" |
| In the cab | Daniel sits in the passenger seat and can look around (about ±105° yaw), but cannot move or use anything. Luis drives (seated pose), glancing at him now and then. No HUD vitals during the ride | Lets the player take in the cab and the city without breaking the scene |
| The crash | 19:00 siren → Luis switches on the light bar → the grid drops and the lamps die in a wave → about 3 s later a black ram truck with its lights off swerves across, flares its high beams and roof bar, and hits the front left (Luis's side). The ambulance rolls onto its right side; the footage cuts to black on the landing. The drive is paced so these land in this order every time | The siren, the blackout and the crash are the beats of the cold open in the story |
| The ram truck | Black pickup with a welded steel plate that has a crude grin painted on it. It stays at the scene: engine idling, lights on (they light the crash site), radiator steaming, horn stuck for about 11 s | A first hint that the Smilers set it up; the lights keep the scene readable |
| Waking up | Black with ringing ears (the world is muffled for about 14 s), fade in lying on the asphalt, Luis calls, Daniel gets up at 55 % health. Hints: movement keys, later med kits and H | Teaches the controls inside the scene |
| Med kits | Daniel's kit is lost in the crash (0 kits). **Two** kits lie behind the ambulance where the jump bag spilled. Treating Luis costs one of them | Makes the choice cost something real |
| Luis's choice | Treat him (F on Luis, about 3.5 s, `HelpedWounded`) or leave: walking 28 m away without treating him records `AbandonedWounded`, and Luis shouts after him. Coming back later and treating him still counts (both entries stay). Shooting him records `KilledCivilian` | A first Ledger entry with a cost on each side | (consequences: see the revision below)
| First Smilers | Two Smilers stand at the intersection, about 40 m north; one films with a phone. A stinger and lines play when Daniel sees them (or Luis points them out after talking); they then walk off west and vanish once out of sight. They don't fight, and run if shot at | A sighting that promises a threat without starting a fight |
| Luis's look | Navy EMS work jacket and cargo trousers, boots, blue nitrile gloves (like Daniel's) | Reads as Daniel's partner |

### 6b revision after the owner's first test (decided with the owner)
| Topic | Decision | Why |
|---|---|---|
| Luis's choice: consequences | **Treated:** Luis gives Daniel his radio (`StoryState.HasLuisRadio`) and warns him over it; first call when Daniel reaches z ≈ −45: looters at the gas station, go round the back. 6c and later use the radio for more warnings. **Left:** once Daniel is far (or hasn't looked back for a while) the Smilers return: a scream, two shots, Luis is dead (`StoryState.LuisAlive = false`), a grin is daubed on the wreck beside him. Daniel keeps both kits. 6e's stream should taunt him with it ("You left him, Danny."). Coming back and treating him before the shots cancels it | Owner: the choice must really lead somewhere, both ways (owner picked this option) |
| Way forward after the crash | In-world first, plus a small HUD: Luis gives the way and points south; the Smilers closed Harlow behind the ambulance with a burning barricade (z ≈ 19.5; the watchers stand behind it); the gas-station pylon (x −8.6, z −75, own generator, always lit) is the visible goal; the avenue lamps south come back on emergency power; HUD shows a GPS heading (`HDG 182° S`) and a persistent objective line; walking up to the barricade, Daniel says "Home's south" | Owner couldn't tell where to go (owner picked this option) |
| Direct voices | Daniel, Luis and the generic voices go through a "room" channel (mic band, light saturation, early reflections) with a rougher source; all dialogue sits lower (`voiceVolume` 0.65). Radio, phone and the Smiler voices are unchanged | Owner: Dispatch sounds good, Mia OK, Daniel and Luis too synthetic |
| Close-up faces | Characters seen at arm's length (`CharacterLook.closeUp`, Luis now) get a denser, more detailed face | Owner: Luis looked faceless in the cab |

## Sequence 3: the gas station (phase 6c, layout approved by the owner)
| Topic | Decision | Why |
|---|---|---|
| Place | West side of Harlow, lot z −92..−62, x −11..−42 (the west-row buildings standing there are removed after generation, so the rest of the district stays exactly as it was). Harlow is closed just south of it (barriers, a burning wreck, an invisible wall at z −95): the only way on is through the station | The owner hit a dead end going south; now the route is forced and readable |
| Layout | Lit canopy (generator power) with two pump islands and a car at a pump; a walk-in store (smashed front windows, door facing the canopy, counter, shelves, flickering tubes, back door); a walled lot; a dark corridor along the north wall with a van, a dumpster and pallets as cover; a lit back gate in the west wall | Light marks danger (the forecourt) and the goal (the gate); the dark corridor is the way round |
| People | Three looters: a pistol at the store door watching the street, a bat inside at the shelves, a scared kid with a bat at the pumps. The clerk hides behind the counter | Matches the demo plan; each looter makes a different problem |
| Play | Sneak the north corridor (crouch, light off), throw bottles to pull them, or fight. While unaware they talk (positional lines), which tells the player where they are. When the other two are down and Daniel is close, the kid surrenders: spare (F) or execute, recorded in the Ledger | Stealth needs information; the moral beat must happen at least once |
| The clerk | Talking to her (while she hides): she says the back gate is open and gives Daniel two bottles | Rewards a non-violent approach; points to the exit |
| Luis | With his radio: the warning before the station (6b) and "you made it past?" after the gate | The treat choice keeps paying off |
| The end of 6c | Going through the back gate ends the sequence (hint: the alleys of 6d continue from there). 6d builds the back alleys behind the west row, starting at that gate | Keeps the steps testable on their own |

## Hit feedback: blood and damage numbers (owner's request, during 6c)
| Topic | Decision | Why |
|---|---|---|
| Blood | (Revised after the owner found it invisible at range: bright, saturated red, about twice the size, bursts that grow up to 3x with distance, and surface blood with a faint self-glow so it never goes black.) Every bullet hit on a person bleeds: a puff at the wound, droplets sprayed out the back, spatter on the wall behind (or the ground behind when there is no wall within ~2.5 m), a few drips under the wound, and a pool that spreads under a dead body over ~20 s. Blood on surfaces is a **lit** decal (wet, catches the flashlight), not an unlit one | Owner: shot people should bleed |
| Headshots | A pink-red mist, about three times the droplets, a bigger and longer spatter, and their own sound (`SoundId.ImpactHead`: bone crack, wet slap, spatter tail) | Owner: a headshot should look better than a body hit |
| Damage numbers | (Revised: the owner found them too faint: now 32 px, 40 px for headshots, near-opaque, outlined, never smaller with range.) Numbers in the HUD monospace that sit on the hit point, drift up and fade (about 1 s). Body hits off-white at ~55 % opacity; headshots warm red with a tiny "H E A D" tag; a killing hit holds longer and cools to a dried red; rapid hits on one target add into one number. Only for damage Daniel deals. **F2** toggles them (saved in PlayerPrefs; phase 7 moves it to the settings menu) | Owner wanted a "barely there" number that fits the bodycam look; the toggle keeps the clean-footage option |

## Sequence 4: the alleys (phase 6d, proposed by the agent; the owner can still change it)
| Topic | Decision | Why |
|---|---|---|
| Place | The empty block behind the west row, west of the gas station (x −104..−42, z −90..5), built by `AlleyBuilder` with its own rng. Out of the station's back gate the **gate alley** runs west to a junction; the **north alley** runs north to a steel palisade (z −30), with a dead-end pocket to the south (a fire in a drum, a loose magazine on a crate); halfway up, on the left, a **garage** with its roll-up shutter stuck at 1.5 m; its back door opens on the **back alley**, which runs north to the **school yard** gate. The school (two storeys of brick, its floodlights on in the blackout) faces the yard. Buildings face the alleys and their backs close the area: the route is the only way | The demo plan's "alleys", one readable route, and the school as the goal you can see the light of |
| The call | A few steps into the gate alley the phone rings: "MIA" (the clone), "Daddy, come to the school... everyone's watching" (`DemoLines.SchoolCall`, glitched). Daniel: "Hold on, sweetheart." **Daniel stops for the call** (he can look, not move; owner: walking on during it made the chase too easy). Objective: get to the school. With Luis's radio, once he is through: the school has power while the grid is down | First fake-voice hint; Luis's radio keeps paying off |
| The chase | The moment he hangs up, **six** Smilers (four pistols, two bats) come through the station's back gate a few metres behind him (owner wanted many). They say it straight away: "Relax, Danny, nobody's killing you! Can't let the fun end this early!" (owner asked for it), and "Those were warning shots" a little later. They run him down and keep closing, but **only shoot to miss** (rounds crack past his head) and hit half as hard, because they want him alive on camera; if Daniel hurts one, all of them shoot to hit (`NPCBrain.hunting`, `HuntProvoked`). If Luis was left, they taunt Daniel with it; with the radio, Luis hears them on the band. Fighting is possible but costly; running is the way | A chase with a choice in it; herding fits the Smilers' plan (the stream) |
| The escape | The shutter is lit by a work lamp and a hint says "Crouch (C)". Only a crouching man fits under it; the NavMesh doesn't go under it, so they can't follow. They bang on it, laugh ("See you at school, Danny!") and walk off | Uses crouch; "See you at school" is something they couldn't know (hint) |
| The firework (was: the wave) | As soon as Daniel is through the shutter, a Smiler stands in the floodlit yard, in line with the back alley, a firework tube at his feet. When Daniel sees him: a stinger; he kneels, lights it, and runs into the school (the door bangs). The shell bursts into the Smilers' grin in cyan stars (face ring, eyes, smile) about 22 m up and lights the yard; Daniel: "A grin... They're putting on a show." Owner replaced the wave with this (he used to start only after Daniel had reached the court; fixed too) | The demo plan's "a Smiler waves at the camera", in a louder form: the stream's calling card |
| The end of 6d | Through the yard gate. The school door scene is 6e | Each step testable on its own |

## Sequence 5: the school door (phase 6e, proposed by the agent; the owner can still change it)
| Topic | Decision | Why |
|---|---|---|
| The door | The school's front doors on the yard (x −86, z −6): steel double doors in a stone surround, chained and padlocked. Sprayed across them in red: "SMILE, DANNY." and "YOU'RE LIVE."; between the lines, Daniel's Marine boot-camp photo, taped on. The board above reads "KESSLER STREET MIDDLE SCHOOL" (Mia's school, on Kessler). Built at runtime (`SchoolDoorBuilder`) because the paint and the photo are runtime textures (`PixelFont`, `ProcTextures.MarinePhoto`) | The demo plan's cliffhanger image, readable without UI |
| The camera | Three metres out, a phone on a tripod films the door: red REC dot, its torch on the doors (Daniel's shadow falls on them when he steps in). "Mia" speaks from it when Daniel comes within 9 m: "Daddy? I'm inside. Look at the door, Daddy." | "You're live" made literal: they are streaming from it; the clone's voice comes out of a phone |
| Looking | F on the door ("Look at the photo"): Daniel can only look; the photo is held up to the lens (`BodycamHUD.ShowInspect`). Daniel: "That's me. Parris Island, twenty-sixteen. This was on Mia's wall. In her room." | They have been in his house: the stakes for Acts 2–4 |
| The stream | The Smilers' stream breaks into the bodycam picture (`StreamOverlay`: a red LIVE badge, "SMILE.TV/danny", a climbing viewer count from ~38 000, chat on the right). The chat knows what the bodycam saw (executed or spared the kid, saved Luis). From the phone: "Smile, Danny. You're live. Forty thousand people want to see what a Marine does to get his little girl back." If Luis was left: "They already loved the part where you left Luis." If Luis has the radio: "you're on my phone, on every phone". Daniel: "Where is she?!" — "Round two starts now." The chat is in-world text, so it stays English | Pays off the Luis choice (6e was planned to read `StoryState.LuisAlive`) and the Ledger; sets up Act 2 "Broadcast" |
| The cut | The floodlights die, a last glitched "Daddy—", a tape tear: black. The world goes silent (`AudioListener.pause`). Over black: Detective Hale's note (EXHIBIT 14, the recording's end time, "three hours are missing… I want to know who, and what Reyes did in the gap", 22 March 2034 07:58) with his voice ("Three hours. Gone. Where were you, Mister Reyes?"), then TO BE CONTINUED, then "ECHOES OF ANARCHY — end of the demo" | The frame story (Hale reviewing the footage) and the demo's end |


## Owner feedback after the first build (decided with the owner)
| Topic | Decision | Why |
|---|---|---|
| Checkpoints | A death restarts at the last checkpoint instead of the ambulance ride (`Core/Checkpoint`, saved by `DemoDirector`): **the crash** (the crash site plays again from waking up), **heading down Harlow** (Luis treated or dead, Daniel past z −50), **through the gas station's back gate** (before the call and the chase), **in the school yard**. The Ledger, `StoryState`, the in-game time and Daniel's kit (health, med kits, rounds, bottles) go back to what they were at the checkpoint; at least 60 health so a restart is never a death trap. Later restarts put the night back as it was: the Purge already under way (no siren, lamps out, some on emergency power), Luis treated or dead by the wreck (the grin painted if the Smilers came back for him), the looters gone, the clerk's body if Daniel killed her | Owner: dying sent him back to the start |
| The cab | Daniel's camera in the passenger seat sits about a hand below Luis's eyes, not at his chest | Owner: "like a dwarf" next to the driver |
| Gunshots | No tuned low tone in any gunshot (a falling low sine is how a kick drum is made). The pistol is a pressure spike plus gas noise, clipped by the body-camera mic, the slide's clack, the street's reflections and a long dull tail | Owner: the shot sounded like a drum |
| The dead are silent | A line said by someone standing in the world stops when they die, and a conversation ends once anyone in it is dead (`DialogueDirector`) | Owner: the clerk kept talking after he killed her |
| Melee | Strikes do half the damage they did (20, the punch 12; head ×3.5): a blow staggers, it rarely ends a fight | Owner request |
| Characters | Every bare or half-covered face is the detailed one (iris and lids, lash line, lid crease, wet rim, nostrils, two lips, shaped ears; stubble on men only; a softer jaw, arched brows and longer lashes on women; eye colour from skin tone); bodies are lofted about twice as densely and the hands have four fingers. Still made only of code: no textures beyond the fabric detail maps | Owner: "all characters as realistic as you can make them" |
| Luis's face | Luis gets extra bone structure (`CharacterLook.definition` = 1, `MaskFactory.Rugged`: cheekbones and hollows, a jaw ridge, heavier brow, deeper sockets, a narrow nose with a bump, deep nose-to-mouth folds, forehead lines) on a denser skull | Owner: sharper, more realistic lines |
| Stay with Luis | After the crash Daniel can't go more than 11 m from Luis until Luis has finished talking (directions and the choice); Daniel: "Luis first. I'm not leaving him like this." | Owner: you could walk off before he finished |
| The gas-station fight | Harder, in waves, but only once shooting starts (sneaking past still works): wave 2 (pistol, bat, pistol) comes over the barricade on Harlow south of the lot 7 s after the fight starts; wave 3 (two pistols, a bat) flanks from the store's north corridor and the back gate once wave 2 is down to one. Reinforcements are braver (courage 0.75) and spawn only out of Daniel's sight | Owner request |
| Ammo | Daniel wakes at the crash site with a loaded gun and five spare magazines (7 + 35) | For the harder station fight |
| The photo | The Marine photo is a rendered 3D portrait (`Rendering/MarinePortrait`, a small ray marcher over a signed-distance bust, studio-lit), then aged as a print | Owner: the drawn photo didn't look real |

## New direction: a 30-minute portfolio game (decided with the owner, 2026-10-28)
The owner changed the goal: instead of a ~60-minute game, a **tight, genuinely exciting and fun ~30-minute game** for
their portfolio, with a public GitHub repository and a README aimed at employers. Every design decision for it is taken
**together with the owner, one at a time** (ask before deciding). Decided so far:

| Topic | Decision | Why |
|---|---|---|
| Structure | A **new, tight flow built from scratch** for 30 minutes. Existing pieces (the ambulance, the crash, the gas station, the alleys chase, the school, the Smilers' stream) can be kept, changed or dropped, decided scene by scene with the owner | Owner's choice over "Act 1 + school + finale" and "Act 1 + school + home defence" |
| Ending | **One strong ending** for everyone. Choices (the Ledger, StoryState) still change lines, the stream chat and details, not the ending | Owner's choice |
| Clarity | **Daniel says what is happening** at the key moments, in one short line (the clone: "That wasn't Mia... they copied her voice"; the stream: "They've got my camera. I'm the show") | A friend who played the first build didn't understand the stream ("what is this?") |
| Repository | Commit and push after the owner tests this round; then the owner makes the repository public (Settings › General › Danger Zone › Change visibility; there is no `gh` CLI on the dev machine). `README.md` (English) is the portfolio page: the pitch, the zero-imported-assets pipeline, the systems, and an honest "How it was made" (the owner designed and directed; AI coding agents implemented under that direction) | Owner request |
| Opening | A **short ambulance ride** (about 45 s instead of 70) with **a real call from Mia**, whispering that someone is at the door, smiling; the line cuts; 19:00, the grid drops, the truck hits. The goal and the threat are personal within the first minute | Owner's choice over a flash-forward to the school door |
| Tone and spine | A **grounded survival journey** (the current tone): serious and dark, big set pieces between the walking. The stream stays in the background and comes out toward the end (Daniel says what it is when it does) | Owner's choice over a "game show" structure and over Luis as a radio companion |
| Ending | **A twist: Mia was never there.** The Smilers led Daniel across the city with a cloned voice to make content of him; the finale is a confrontation in which Daniel turns his own camera on them | Owner's choice over a studio boss fight and over an escape carrying Mia |
| The villain | **Someone Daniel knows** leads the Smilers: the twist is personal ("why me?" has an answer) | Owner's choice over a masked host and over a faceless crowd |
| Who | **Daniel's old Marine comrade** (working name: Sgt. Eli Vance). Something happened over there that is the root of Daniel's fear of becoming violent again; Vance wants everyone to see who Daniel really is. The Marine photo on the school door is his clue | Owner's choice over the father of a patient Daniel lost and over Luis |
| Where Mia is | **Safe at her mother's.** Even the opening call was the clone: the player was fooled from the first minute. At the end Daniel calls the real Mia, who answers half asleep ("Dad? What time is it?") | Owner's choice |
| The last shot | The player holds the gun on Vance and **decides** (fire or not); the ending plays out the same either way, but the lines around it and Hale's note change ("...and Reyes pulled the trigger" / "...didn't") | Owner's choice over a fixed "Daniel doesn't kill" and over a staged killing |
| Scenes kept | **The crash site with Luis's choice**, and **the gas station** (stealth or the waves). The alleys chase and the school-door scene are not kept as they are (their parts may be reused) | Owner's choice |
| The past | **Daniel reported Vance** for a war crime over there; Vance went to military prison. Tonight Vance wants to prove on camera that Daniel is no different: the whole night is built to push Daniel into violence, and the stream keeps the tally (the Ledger) | Owner's choice over "Daniel left Vance wounded" and "Daniel killed men who surrendered" |
| The lure | After the gas station the clone ("Dad, I'm hiding in the closet, come home!") draws Daniel to **his own house**: a tense search in the dark | Owner's choice over the school and the hospital |
| Middle scenes | **A dark underpass** (only the flashlight; something hunts by sound) and **a Purge party** crossed in a mask (social stealth). Owner's addition: just as Daniel thinks he is through, he is recognised, and he has to get on **a machine gun mounted on a pickup** and mow them down, **infinite ammo** | Owner's choice |
| Finale | **The school gym**, turned into the Smilers' broadcast set. The school yard and the door (photo, the stream breaking into the bodycam) lead into it | Owner's choice over a recruiting office and a TV tower roof |

| Scene order | Home, then the underpass, then the party and the turret, then the school: the tension climbs step by step, the biggest action comes right before the finale | Owner's choice over putting the party before the underpass |
| The turret | **Stationary**: the pickup is parked; Daniel holds off the crowd and their vehicles in waves, then escapes on foot | Owner's choice over a moving pickup driven by someone he helped |
| Why Daniel thinks Mia is home | In the ambulance a voicemail from his ex-wife **Rachel** comes in; Daniel dismisses it unheard ("Not now, Rach"). In the gym he finally plays it: "Dan, I picked Mia up early. She's with me tonight. Call me." | Owner's choice over a custody mix-up explained by Vance |
| The first call | "Mia" whispers: someone is at the door, knocking, "they're smiling, Dad. They're all smiling." Daniel: lock your door, get in the closet, I'm coming. The line dies in static. The voice is almost perfect; only the last syllable catches (a hint for later) | Owner's choice over a screaming call |
| Vance's first trace | **At home**: where the Marine photo hung, an old unit photo is left, Vance's face scratched out; Daniel: "Vance..." | Owner's choice over Luis mentioning his name in the ambulance |
| Intro card | **Shortened** to about 8 s: the date and one sentence of rules; the Voss quote is dropped | Owner's choice |
| The house (8b) | **A two-storey detached house**: a small front yard and porch; downstairs the hall, the living room and the kitchen; upstairs Mia's room (with the closet), the bathroom and Daniel's room | Owner's choice over a flat and a townhouse |
| Inside | **A tense search with the flashlight, no enemies**, scares made of sound and light; once the clues are found, headlights sweep the windows: the Smilers arrive and Daniel gets out through the back | Owner's choice over a search only and over a Smiler hiding inside |
| The closet | **A phone playing Mia's voice** on a loop ("Dad? There's someone at the door..."). Daniel: "It's a recording... they're using her voice." He understands the clone but believes they have taken her; "Mia was never here" stays for the finale | Owner's choice over an empty closet with signs of a struggle |
| The clue | **On Mia's bed**: the old unit photo with Vance's face scratched out, and the note "SCHOOL. COME ALONE. :)". Daniel: "Vance..." | Owner's choice over the living-room photo wall |
| The underpass (8c) | **A dead subway station** (Kessler St): down from the back alley, a ticket hall with turnstiles, two platforms either side of the track, a dark train standing at one of them, up the far stairs into the school yard. The back alley is blocked by a burning wreck north of the entrance | Owner's choice over a road underpass and a storm drain |
| The hunter (8c) | **A blind Smiler in bullet-proof armour**: blindfolded over the LED mask, he hunts by sound only (running, landing, broken glass, gunshots, a thrown bottle), and bullets only spark off him, so Daniel has to slip past him. He hums the music-box tune through his mask, so he can be tracked by ear | Owner: "a blind Smiler, but bullet-proof, and we have to sneak away from him" |
| The way out (8c) | **Sneak to the far stairs, then a chase**: the gate at the foot of the exit stairs is rusted shut; forcing it screams through the station and the hunter comes running; a short sprint up the stairs to the yard, where Daniel drags the street gate shut behind him | Owner's choice over stealth only |
| Who warns him (8c) | **Vance calls** from an unknown number at the station stairs (his voice for the first time): below is Boone, brainwashed, huge, blind, bullet-proof, faster than Daniel ("Walk softly, Marine. Don't run.") | Owner: someone should say on the phone who is down there; owner's choice over Luis on the radio and a stranger |
| Harder (8c) | **Running is deadly**: hunting, Boone is faster than Daniel sprints (6.2 against 5.4 m/s) and one blow (70) nearly finishes him and throws him back; the second kills. **The gate takes six seconds of holding F**, screaming further as it gives (15 to 60 m), so Boone has to be lured to the far end first | Owner: "I ran to the gate and got out, too easy"; owner's picks over a key to find and noise traps |
| Pause menu | **Esc opens a pause menu** (resume, settings, restart from the last checkpoint, quit to desktop); settings: mouse sensitivity, volume, brightness, subtitle language, damage numbers. It replaces "hold Esc to quit" | Owner request |
| Daniel's inner voice | Lines Daniel says to himself are **thoughts: silent, italic subtitles only** (first a whisper; the owner then asked for no sound); lines said to someone stay his normal voice. The thoughts were rewritten to say more (what he sees, what it means, what he will do) | Owner: the inner voice should be a whisper, clearly a thought, and the lines more striking |
| Luis's talk | Once Luis starts talking Daniel can't step more than about 3 m from him until Luis has finished (before that, 11 m round the wreck) | Owner: stay right beside him while he talks |
| The party (8d) | **In the school yard**: the subway comes up inside a plywood hoarding in the yard's corner, and the yard is a Smilers' party waiting for "the show" (him). A big screen on the school counts down; a stage with a DJ at the east end; a VIP strip along the facade behind crowd barriers, its one gap by the stage guarded. The yard did not need to grow: putting the VIP gap at the far end makes the walk cross the whole crowd and come back along the facade | Owner's choice over a separate party street (moving the subway exit) |
| The mask (8d) | **From a reveller passed out behind the hoarding**: his LED mask and his jacket. The hoarding's door stays shut until Daniel wears them ("They'd know my face in a second") | Owner's choice over a mask table at the entrance and a Smiler taken down |
| Blending in (8d) | **A suspicion meter**: guards in LED grins who can see Daniel (people in between hide him) grow suspicious if he runs, crouches, has the gun out, stares at them from close or brushes past them; an eye at the top of the screen fills. A shot or a blow gives him away at once, a smashed bottle adds to it. Full: caught early, and the fight starts wherever he is | Owner's choice over copying the crowd's moves and over both |
| Recognised (8d) | **Vance on the big screen**: a step from the door the music cuts, the screen switches to Daniel's own bodycam (he sees himself), a spotlight finds him, Vance on the PA: "Take off the mask, Marine. Show them who you are." | Owner's choice over the gas-station kid and a ringing phone |
| Who attacks (8d) | **Everyone**: the whole party comes for him (bats, a few pistols), then six Smilers out of the backstage door | Owner's choice over armed Smilers only (the crowd scattering) |
| The end of the turret (8d) | **The ram truck** that hit the ambulance smashes the yard gate and charges the pickup: stopped, it blows up and Daniel runs for the school door; not stopped, it kills him | Owner's choice over an overheating gun and Vance calling it off |
| Hale's case | Every crime is legal 19:00–07:00, so Hale's case is **what came before the siren**: the clone's first call (18:53) and the Smilers in Daniel's house; Vance confesses it on the tape ("From the first call, before the siren even sounded"). The night itself costs them **socially**: in the "lowered" ending every face in the gym is online and they lose jobs and homes; Vance is picked up at 07:02. In the "fired" ending Vance can't answer for it, the others can | Owner: "in a Purge, isn't a detective's case and reporting someone absurd? Everything is legal"; owner's choice: both ideas together |
| The back gate (8b fix) | **Bolted until the raid**: the owner walked round the house and down the subway before the house was done | Owner report |

### The 30-minute flow (agreed with the owner; times approximate)
| Time | Scene | What happens |
|---|---|---|
| 0:00 | Cold open | A short intro card; the ambulance with Luis; "Mia" calls, whispering that someone is at the door, smiling (it is already the clone); the line cuts; 19:00, the grid drops; the truck hits |
| 1:00 | Crash site | Waking up, the med kits, Luis's choice (treat him or leave him); Daniel can't leave until Luis has finished talking |
| 4:30 | Gas station | Stealth, or the fight in waves; the kid who surrenders; the clerk |
| 10:00 | The lure | The clone: "Dad, I'm hiding in the closet, come home!" |
| 10:30 | Home | Daniel's dark house: the door open, Mia's room empty, the Marine photo torn off the wall, a note: "SCHOOL. COME ALONE. SMILE." |
| 15:00 | The underpass | No power: only the flashlight; something hunts by sound |
| 19:00 | The Purge party | The school yard is a masked party waiting for the show; Daniel crosses it in a reveller's mask without being noticed (8d) |
| 22:30 | The turret | Recognised at the last moment: a machine gun on a parked pickup, infinite ammo, waves of the crowd and their vehicles; then away on foot |
| 25:00 | The school | The yard and the door: the photo, the stream breaks into his camera (Daniel: "They've got my camera. I'm the show.") |
| 26:30 | The gym | The Smilers' set: Vance, his mask comes off, the past (Daniel reported him); "Mia was never here"; Daniel calls the real Mia: "Dad? What time is it?"; Vance: "Do it, Danny. Everyone's watching." The player fires or doesn't |
| 29:30 | The end | The tape cuts; Hale's note, which changes with the shot |

### Phases for the 30-minute game (proposed; the owner tests each one)
| Phase | Content | Status |
|---|---|---|
| 8a | Re-cut cold open (about 45 s: the radio, Rachel's voicemail dismissed, the clone's whispered call that dies), the 8-second intro card | **Done**, tested by the owner. (Daniel's clarity lines and Vance's first trace come with the scenes they belong to: 8b, 8e) |
| 8b | Home: Daniel's house (exterior and interior generator), the search, the clue | **Done** (owner committed it); fixes after the owner's test in 8c's round: the phone, the banister, the entrance |
| 8c | The underpass: tunnel set piece, the sound-hunting enemy | **Code done**, awaiting the owner's test (rebuild the scene; `startAt = Metro`) |
| 8d | The Purge party (mask, social stealth, being recognised) and the stationary pickup turret | **Code done**, awaiting the owner's test together with 8c (rebuild the scene; `startAt = Party`) |
| 8e | The school and the gym finale: Vance, the reveal, the call to Mia, the shot, Hale's note | **Code done**, awaiting the owner's test (`startAt = School`) |
| 8f | Polish: menu, performance, the build, README screenshots, making the repository public | **Code done** (the owner asked to finish quickly): title screen (once per launch, before the intro card), the end goes back to it with a fresh game, Hale's case made consistent, README for the finished game, a security scan for going public. Left to the owner: the test, README screenshots, the Windows build |

The 60-minute plan above (Acts 2–4, home defence) is replaced by this flow; keep it only as background.
