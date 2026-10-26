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
| 4 | Combat + AI: pistol (own synthesized gunshot), bottle throw, damage, ragdoll, NPC FSM (patrol/suspicious/search/attack/flee/surrender), sight + hearing perception on the NavMesh | Next |
| 5 | Characters: procedural humanoid, procedural animation/IK, masks (LED smile, pig, angel) | |
| 6 | Narrative: dialogue + EN/TR subtitles, Ledger, phone calls, demo sequences 1–5, GameClock-driven 19:00 siren | |
| 7 | Polish: main menu (VHS style), tape transitions, settings, shader warm-up (avoids the cyan placeholder flash on first play), performance, build | |

The owner tests every phase in Play mode before the next one starts.
