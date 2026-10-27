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
| 7 | Polish: main menu (VHS style), tape transitions, settings, shader warm-up (avoids the cyan placeholder flash on first play), performance, build | |

The owner tests every phase in Play mode before the next one starts.

### Phase 6 steps (agreed with the owner: each step is tested on its own)
| Step | Content | Status |
|---|---|---|
| 6a | Narrative foundation: dialogue director, EN + TR subtitles, babble voices, phone call / voicemail / radio, 19:00 Purge beat (siren, blackout wave, emergency lamps), sample lines | **Done**, committed (`2f05882`) |
| 6b | Sequences 1–2: ambulance cold open, voicemail, siren, truck crash; Luis wounded (treat or leave → Ledger), movement/med-kit tutorial, first Smilers. Needs ambulance, truck, crash site | **Done**, tested by the owner and committed (with the revision, intro card and reload polish) |
| 6c | Sequence 3: gas station (hidden civilian, three looters; sneak, distract or fight; spare or execute). Needs the gas station | |
| 6d | Sequence 4: alleys, the cloned "Daddy, come to the school" call, chase, a Smiler waves at the camera | |
| 6e | Sequence 5: school door, Marine photo, "SMILE, DANNY. YOU'RE LIVE.", footage cuts, detective note, TO BE CONTINUED | |

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
| Route through Harlow | Cold open on the cross street (z 30, eastbound), a tape skip, then south down Harlow Ave. The crash is on the avenue just south of the intersection (wreck rests around x −0.6, z −18). From there Daniel goes **south**: gas station (6c) on the southern avenue, then the alley (6d, run north to south or the reverse, to be decided in 6d), school (6e) at the end | Uses the existing district without rebuilding it; the Smilers can watch from the lit intersection |
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
