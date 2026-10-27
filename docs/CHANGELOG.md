# Changelog

Newest first. Each entry: date, agent, what changed, what the owner still needs to test, known issues.

## 2026-10-27 — Claude Code (Opus 5.5), 6d fixes: stray gas-station hint, the firework
- Owner approved 6c, the blood and damage numbers, and 6d: committed together; 6e started.
- Owner: "Looters at the gas station" text appeared in unrelated places. Cause: `GasStationSequence.Play` starts on the first frame, while Daniel still stands on the scene's default spawn (−2, −88), 25 m from the station, so its approach trigger fired at once (during the intro card, in the ambulance, at the alleys start). Now it waits until Daniel is free (not `LookOnly`/`InputLocked`/mounted) and on Harlow or the forecourt side (x > −27, z −100..−40) within 34 m.
- Owner: the Smiler at the end should not wave but set off a firework and run. New `Rendering/Firework` (tube, fuse sparks and glow, the shell on a spark trail, a burst in the shape of the Smilers' grin facing the camera plus glitter, a cyan light flash over the yard); new sounds `FireworkLaunch` and `FireworkBurst` (appended to `SoundId`; rendered offline: no NaN, peaks 0.8/0.9). `SmilerWatcher.kneeling`. `AlleySequence.FireworkScene`: when Daniel sees him he kneels, lights it, runs into the school (door bang, despawn); Daniel: "A grin... They're putting on a show." (`DemoLines.FireworkSeen` replaces `WaveSeen`).
- **Owner needs to test:** rebuild. Start from the beginning (or `startAt = CrashSite`/`Alleys`): the gas-station hint must only appear when walking up to the station on Harlow. Through the shutter, look at the yard from the back alley: kneel, fuse, launch, grin burst, the runner into the school.
- Not verified in Unity: the burst's size and readability from the back alley, the particle trails, the kneel pose next to the tube, the runner reaching the school door on the NavMesh.

## 2026-10-27 — Claude Code (Opus 5.5), phase 6d (the alleys) + brighter blood and damage numbers
- Owner: the blood and the damage numbers were invisible at range; make them brighter; then start 6d. 6c, blood and 6d are not committed yet (waiting for the owner's test).
- Blood: bright saturated red particles (`BloodPuff`/`BloodDrop` materials at 1.6–1.7 intensity), about twice the size and count, bursts scaled up to 3x with camera distance (`ImpactFX.EmitBlood`); surface blood brighter with a faint emission (`RuntimeMaterials.Decal` takes an emission colour) and larger splats, spatter and pools.
- Damage numbers: 32 px (headshots 40 px), near-opaque, outline + drop shadow, never smaller with range, a little longer on screen.
- 6d world (`World/AlleyBuilder`, own rng `seed + 404`, called from `DistrictGenerator` after the gas station): gate alley, north alley (dumpster, burnt car, pallets, a steel palisade at z −30, a dead-end pocket with a fire drum), the garage (roll-up shutter stuck at 1.3 m, tube light, car, bench, back door), back alley, the school yard (court lines, hoop, flagpole, palisade gate) and the school (two-storey brick, floodlights on). Wall lamps, a shadowed work lamp over the shutter, reflection probes. `Solid` moved out of `GasStationBuilder` into its own file.
- 6d story (`Narrative/AlleySequence`): the clone's call a few steps past the gate (`DemoLines.SchoolCall`), Luis's radio about the school's power; three hunters (`NPCBrain.hunting`) through the back gate: they miss on purpose and hit half as hard until Daniel hurts one (`HuntProvoked`); taunt lines (Luis's death if he was left), Luis on the radio otherwise; hint at the shutter; the hunters bang on it, "See you at school, Danny!", walk off and despawn out of sight. In the back alley a Smiler waves at the camera from the yard (`SmilerWatcher.waving`, hand IK). The yard gate ends 6d. A magazine (7 rounds) on a crate in the dead-end pocket. `DemoDirector.StartAt.Alleys` starts at the back gate.
- Owner (third try): too easy, because he kept walking during the call and the Smilers only started once he was far away; wants him held for the call and many Smilers behind him; the wave began only after he had reached the court. Now: `LookOnly` during the call (look, no movement); six hunters (4 pistols, 2 bats) spawn behind the back gate the moment he hangs up; Luis's "the school has power" radio moved to after the escape; the shutter aftermath (bangs, "See you at school") runs on its own coroutine so the sequence goes straight on; the waving Smiler is placed in the yard as soon as Daniel is through and waves until he sees him (Daniel's line plays at once, not queued behind the taunts).
- Owner (second try): "there is no way through", no car there, and the Smilers should say they are not out to kill him. Cause: the burnt sedan in the north alley stood across it (a `PropFactory.Sedan` at yaw 0 is 4.8 m long along X; the alley is 4 m wide) and closed it completely; the garage car was turned the same wrong way. The car is replaced by a junk pile against the west wall (crates, a steel cabinet, a mattress; 2.2 m+ stays clear), the garage car lies along the north wall, the shutter gap is 1.5 m (crouched Daniel 1.15 m, NavMesh agent 2 m). New lines: "Relax, Danny, nobody's killing you! Can't let the fun end this early!" at the start of the chase and "Those were warning shots, Danny!" after about 9 s (if he hasn't shot one).
- Owner (first try of 6d): could not tell where to run. Now: the chase hint and objective name the way (north, the lit shutter on the left, crouch); the shutter hint repeats within 14 m; Daniel and a hint react at the fence ("the garage, under the shutter!") and in the dead end ("back, north!"); the shutter lamp is brighter and light spills out under the shutter at floor level.
- **Owner needs to test:** rebuild the scene. Blood and numbers from close and far (20–30 m). Then play 6d: from the start, or `startAt = Alleys` on the "Demo" object. Listen to the call, run from the hunters (sprint), crouch (C) under the lit shutter, watch the wave, go through the yard gate. Also try turning to fight them.
- Not verified in Unity: the alley geometry (gaps between buildings, the facades' fire escapes over 4 m alleys), that the player fits under the shutter crouched and the hunters really can't, the NavMesh through the 3 m station gate for the hunters, how hard the chase is (hunter speed 4.8 vs sprint 5.4), the wave's arm pose.

## 2026-10-27 — Claude Code (Opus 5.5), blood and damage numbers (with 6c, uncommitted)
- Owner asked: shot people should bleed, headshots should bleed better, and every hit should show a faint floating damage number that fits the game's look.
- `ImpactFX.Bleed`: a puff at the wound, a spray of droplets out the back, spatter on the wall behind (or on the ground behind), 1–3 drips under the wound. Headshots: a mist, about three times the droplets, a bigger spatter, and a new sound `SoundId.ImpactHead` (rendered offline: no NaN, peak 0.85). `ImpactFX.BloodPool` spreads a pool under every dead NPC (`NPCBrain.OnDied`). Surface blood is a lit, wet decal (`RuntimeMaterials.Decal`; textures `ProcTextures.BloodSplat/BloodSpatter/BloodPool`), in its own pools (60 splats, 10 pools), never on people or moving objects.
- `UI/DamageNumbers` (added by `BodycamHUD`): listens to the new static `Health.AnyDamaged`; only damage Daniel deals. Small monospace numbers on the hit point that rise and fade in ~1.1 s; headshots warm red with a tiny "H E A D" tag; a kill holds 1.6 s and cools to a dried red; hits within 0.45 s on the same target add into one number. **F2** toggles (`GameSettings.DamageNumbers`, PlayerPrefs).
- **Owner needs to test:** shoot looters in the body and in the head (gas station, or `startAt = Sandbox`), next to a wall and in the open; look at the spatter and the pool under a body with the flashlight; check the numbers are readable but faint; F2 off/on.
- Not verified in Unity: the lit transparent decals (sorting against bullet holes, how dark they read at night), the numbers' size at 1080p.

## 2026-10-27 — Claude Code (Opus 5.5), phase 6c (gas station)
- 6b committed (`ec8701e`). 6c started with the layout the owner approved; see `docs/DESIGN.md` ("Sequence 3").
- World: `GasStationBuilder` (canopy with generator lights, pump islands, walk-in store with smashed windows, counter, shelves of stock, flickering tubes, back door; lot walls; dark north corridor with a van, dumpster and pallets; lit back gate). `DistrictGenerator.GasStationLot` removes the west-row buildings on the lot after generation (own rng: nothing else in the district moves) and closes Harlow at z −95 (barriers, burning wreck, invisible wall).
- `GasStationSequence`: three looters (pistol at the door, bat inside, the kid at the pumps), positional chatter while they are unaware, objective and hint on approach, the kid surrenders once the other two are down (lines on surrender and when spared), the clerk behind the counter (talk: she points to the gate, gives two bottles), the end through the back gate, Luis on the radio if he has it.
- AI: `NPCSpawner.Create`, `NPCBrain.AnySurrendered` / `Surrender()` / `startCowering`. The `Looters_Barrel` test spawner is gone (it stood here).
- **Owner needs to test:** rebuild the scene. Play from the start, or set `startAt = GasStation` on the "Demo" object to start on Harlow just north of the station. Try sneaking the north corridor to the gate, a bottle, a fight (the kid should give up: F to let him go, or not), talking to the clerk, and the radio lines if Luis was treated.
- Not verified in Unity: the store and lot geometry (walls meeting the neighbouring buildings, the door openings for the NavMesh), how easy the sneak is (the door looter's view of the corridor), NPCs inside the store.

## 2026-10-27 — Claude Code (Opus 5.5), 6b final polish (committed with 6b)
- Owner asked: a more realistic face for Luis, stamina lasting 15 s, a reload you can see and hear, a bit faster.
- Luis's face (`MaskFactory.SculptCloseUp` / `CloseUpFace`): smile lines, eye bags, chin cleft, sloping forehead, hollow temples, squarer jaw; brows as rows of tufts; lash line; two-tone iris; a wet, slightly pink eye white.
- Stamina `staminaMax` 15 s.
- Reload (`WeaponPistol.Reload`): the gun comes up with the magwell toward the camera; the old magazine slides out and drops as a real object (clatters on the ground); the support hand (now its own group, `WeaponModels.Hands(..., out supportHand)`) fetches a fresh magazine and seats it; on an empty gun it racks the slide. Tactical 1.55 → 1.3 s, empty 2.0 → 1.7 s. The magazine is now a whole model (body, base plate, top round). New sounds `MagPouch`, `MagSeat`, `MagDrop`; mag-out/in/rack louder (the old clicks were very quiet, RMS 0.035).
- Owner had walked south and found a dead end: the demo content currently ends at the crash site; the looters at the gas-station sign are the phase 4 test encounter. 6c builds the gas station there.
- **Owner needs to test:** rebuild the scene (stamina). Reload with R (half and empty mag). Look at Luis's face.
- Not verified in Unity: the reload hand path and the dropped magazine (watch for the hand clipping through the gun).

## 2026-10-27 — Claude Code (Opus 5.5), intro card + one-language subtitles
- Owner asked for: a short explanation of the Purge and the date on the opening black screen, with a reactionary quote below; subtitles in one language only (Turkish without English, or English without Turkish).
- `IntroCard`: first start asks "[1] English  [2] Türkçe"; then the date, what the Purge is (the fictional 2029 Renewal Act) and the quote from Senator Harlan Voss, in the chosen language; Space/Enter/click skips. The clock and the ambulance wait for it.
- `GameSettings` (PlayerPrefs) holds the subtitle language; `BodycamHUD.ShowLine` shows one language (speaker name in colour); F3 switches it; reading time follows the shown text.
- **Owner needs to test:** Play from the start: the language choice (only the first time; to see it again delete the PlayerPrefs key `Echoes.SubtitleLanguage`, e.g. Edit > Clear All PlayerPrefs), the intro text and quote, then the subtitles in one language; press F3 to switch.
- Not verified in Unity yet (compiled with `dotnet build`).

## 2026-10-27 — Claude Code (Opus 5.5), phase 6b revision (owner's first test)
- Owner feedback: Dispatch voice good, Mia OK, Daniel and Luis too synthetic; Luis has no face and can't hold the wheel; unclear where to go after the crash; dialogue a bit quieter; police siren should fade with distance; more stamina; the Luis choice must have real consequences.
- **Voices:** direct voices (Daniel, Luis, generic, Hale) get a rougher source (pitch jitter, shimmer, slow vibrato, darker tilt, more breath) and a new `VoiceChannel.Room` (mic band, light saturation, early reflections). `DialogueDirector.voiceVolume` 1 → 0.65. Radio/phone/Smiler voices unchanged. Rendered offline: no NaN.
- **Luis:** `CharacterLook.closeUp` → `MaskFactory.CloseUpFace` (denser skull with nose bridge/tip/wings, philtrum, lips, eyeballs with iris and pupil under lids, fuller brows, stubble shell); fixed mid skin tone; brighter cab light at face height. Hands on the wheel via new `ProcAnimator` hand IK; the steering wheel is its own object and turns with the path's bends.
- **Way forward:** Luis gives the way and points south; a burning Smiler barricade closes Harlow north of the crash (z 19.5, with a painted grin; the watchers stand behind it); lit gas-station pylon at x −8.6, z −75 (`PropFactory.GasStationSign`, new palette materials `signRed`/`signWhite`); avenue lamps south come back on emergency power; HUD heading line (`HDG 182° S`) and `SetObjective`; "Home's south" if Daniel walks to the barricade.
- **Consequences (`StoryState`):** treated → Luis gives his radio (`HasLuisRadio`), radio call at z −45 about looters at the gas station (new `Speaker.LuisRadio` / `SoundId.VoiceLuisRadio`). Left → the Smilers come back: scream, two shots, Luis dead (`LuisAlive = false`), a grin beside his body. Treating him before the shots cancels it.
- Police drive-by: custom rolloff to silence at 340 m, distance/occlusion low-pass, fades at both ends. Stamina 6 → 8.5 s, regen 1.1 → 1.4.
- **Owner needs to test:** rebuild the scene (needed: stamina, voice volume and the gas sign are baked in by the builder). Listen to Daniel/Luis in the cab; look at Luis's face and his hands through the bends; at the crash site check that the way south is obvious; try both choices (treat → radio and the call further south; leave → walk away, hear the shots, go back).
- **Not verified in Unity:** compiled with `dotnet build` only. Watch for: lids or irises poking through on Luis's face, elbows bending oddly with the hand IK, the grin beside Luis floating off the wreck, the barricade overlapping the mailbox or signal poles.

## 2026-10-27 — Claude Code (Opus 5.5), phase 6b
- 6a was committed by the owner (`2f05882`), so 6b started: demo sequences 1 (cold open) and 2 (crash site). Decisions are in `docs/DESIGN.md` ("Demo route and sequences 1–2").
- **Vehicles (runtime, all code):** `VehicleFactory.Ambulance` (box ambulance "Medic 14": cab shell, a full cab interior with dash, gauges, radio, MDT screen, steering wheel, seats and mirrors, see-through glass, patient compartment with cabinets and a stretcher, light bar, rear doors) and `RamTruck` (black pickup, ram plate with a painted grin, roof light bar). `ScriptedVehicle` drives them along smooth paths with suspension sway, wheel spin and engine sound.
- **Sequence 1 (`ColdOpenSequence`):** "EXHIBIT 14" card; Daniel in the passenger seat (`CameraRig.Mount`, look-only input), Luis driving (new `Seated` pose). Radio traffic, cab talk, Mia's voicemail. Tape skip to 18:59:54, south on Harlow Ave, 19:00 siren, Luis switches on the light bar, the grid drops, the truck flares its lights and rams them, the ambulance rolls, cut to black.
- **Sequence 2 (`CrashSiteSequence`):** ringing ears and a muffled world; Daniel wakes on the asphalt and gets up at 55 % health. Luis sits against the wreck (new `Slumped` pose) and calls out. Two med kits lie where the jump bag spilled (Daniel's own kit is gone). Treat Luis (F, costs a kit, `HelpedWounded`) or walk 28 m away (`AbandonedWounded`). Two `SmilerWatcher`s at the intersection (one filming) trigger a stinger and lines, then walk off. Hints for movement and med kits; objective "head south".
- Crash site: truck idling with its lights on, steaming radiator, stuck horn that dies after ~11 s, glass and debris, the ambulance's light bar still flashing, a flickering light inside the open back.
- New sounds (`SynthLibraryVehicle`, appended to `SoundId`): diesel and V8 engine loops, horn, crash impact, metal scrape, tyre squeal, tinnitus, tape skip. Rendered offline: no NaN, peaks 0.55–0.95.
- Other: HUD `ShowHint`, `Glitch`, `SetVitalsVisible`; `Perception.PlayerConcealed`; `PurgeClock.GridDown` / `SecondsToGridDown`; `MedKitPickup`; `F4` skips the ride; the `Smilers_Intersection` test spawner was removed.
- **Owner needs to test:** rebuild the scene (Tools > Echoes > Build Harlow District), then press Play and watch from the start. Check: the cab (look around, Luis at the wheel), the subtitles, the tape skip, the 19:00 siren, the light bar, the blackout, the truck, the crash. At the crash site: getting up, Luis, the med kits behind the ambulance, H, treating Luis with F (or walking away), the Smilers at the intersection. F4 skips the ride.
- **Known issues / not verified in Unity:** compiled with `dotnet build` only; nothing has been seen in the editor yet. Watch for: inside-out faces on the vehicles (especially the cab interior), Luis's hands not quite on the wheel and his feet under the cab floor (hidden by the dash), the transparent glass, the exact layout of the crash site (Luis against the underside, parked cars nearby), the timing of the truck. Before 19:00 the street still has the burning cars from "the first hour of the Purge" (an old inconsistency).

## 2026-10-27 — Claude Code (Opus 5.5), phase 6a
- Phase 5 and the camera fix were committed (`d1898fb`) at the owner's request. Phase 6 is split into steps 6a–6e (see `docs/DESIGN.md`).
- Owner decisions: babble voices; English and Turkish lines shown together.
- **6a, narrative foundation (code done):**
  - `SynthLibraryVoice`: babble syllables for 9 voices (Daniel, Luis, Mia on the phone, the cloned Mia with glitches, Dispatch on the radio, generic male and female, a muffled Smiler, Det. Hale), plus phone ring, pickup, hangup, voicemail beep and radio squelch. Rendered offline: no NaN, even loudness.
  - `DialogueDirector`: lines with syllables timed to the words, pitch declination and question rise; radio squelches; calls (ring ×2, auto pick-up, call timer on the HUD); voicemail.
  - HUD: `ShowLine` (English on top, Turkish below), `ShowCall` / `HideCall`.
  - `PurgeClock` (19:00 beat) with `StreetLight` on every street lamp (`PropFactory`).
  - `DemoLines`: cold-open radio, Mia's voicemail, the cloned school call.
  - `StoryDevHotkeys`: F5 and F6.
- **Owner needs to test:** rebuild the scene (lamps need the new component). Press **F5**: radio traffic, then the voicemail, then an incoming call (listen to the babble, read the EN + TR lines, watch the call indicator at the top). Press **F6**: the clock jumps to 18:59:50; at 19:00 the EBS tone and sirens play, then the lamps die in a wave and some come back dim and flickering, then distant gunfire starts.
- **Known issues / unverified:** not run in Unity yet. The voice quality is the main unknown; the syllable recipe can be tuned (formant table, `Syllable` lengths, `DialogueDirector` pacing).

## 2026-10-27 — Claude Code (Opus 5.5), camera jitter fix
- The owner tested phase 5 and liked it. They reported a leftover from earlier phases: the camera trembles up and down even when standing still, and sometimes bobs too much when running.
- Fixes:
  - `FPSController`:
    - **Debounced grounded flag** (`groundedGraceTime` 0.12 s). Single-frame contact losses on seams and curbs, or at high frame rates (vSync is off), used to toggle sprint, FOV kick and bob.
    - **Snap down** onto ground within step height when walking off a curb, instead of a small fall.
    - The **step cycle** now advances by distance / stride, so changing speed no longer makes the bob phase jump (it used to be recomputed as distance ÷ current stride).
  - `CameraRig`:
    - **Height smoothing** (`heightFollow`) absorbs the capsule snapping up curbs and millimetre contact jitter.
    - The bob curve is now sin², with no jolt at the footfall. Sprint bob ×1.3 instead of ×1.6, and slightly smaller amplitudes.
    - Idle and fatigue sway and breathing are much weaker (`idleSway` 0.12, `fatigueSway` 0.6, `breathing` 0.06 + `fatigueBreathing` 0.3).
  - The `WeaponPistol` viewmodel bob matches.
- **Owner needs to test:** rebuild the scene (the new camera defaults come from the builder). Then stand still, walk, sprint (also up and down curbs), and stand still after sprinting.
- **Not verified in Unity yet:** the root cause was not reproduced here. These fixes cover every source found in the code.

## 2026-10-27 — Claude Code (Opus 5.5), phase 5
- **Phase 5 (characters), code done:**
  - Bodies: the primitive-per-bone humanoid is replaced by **one skinned mesh** per character, lofted from superellipse cross-sections (torso, arms with hands and thumbs, legs) and blended across joints. Proportions vary: height, bulk, belly, male/female.
  - Clothing: garment layers with real hems, cuffs, ribbed waistbands and collars; shoes with soles (sneaker, boot, dress shoe). Details: hoodie pocket and drawstrings, work-jacket zip and chest pockets, open jacket over a shirt, suit with shirt front, lapels, buttons and a tie in the LED colour, cargo pockets. Runtime fabric detail maps (`ClothTextures`).
  - Heads: sculpted skull (jaw, chin, nose, brow, sockets), faces for the unmasked (eyes, brows, lips, ears), short/long hair, beanie, cap. Masks: LED grin (Smilers), hood + bandana, **pig**, **porcelain angel + glowing wire halo** (looters).
  - Animation: layered `ProcAnimator`: gait curves with push-off and heel strike, pelvis bob/yaw/roll, counter-rotating torso, idle breathing and weight shift, leaning into acceleration and turns, shuffle steps when turning on the spot, per-bone pose blending (legs keep walking while aiming), head-gaze stabilisation, **two-bone foot IK** with pelvis drop and ground-normal foot tilt.
  - Dev tool: `CharacterLineup` (F8 shows one of every look in front of the player, F7 cycles the animation; they can be shot).
- **Owner needs to test:** rebuild the scene (Tools > Echoes > Build Harlow District), Play, press F8 and cycle with F7. Also walk to the test fights to see NPCs moving, aiming, surrendering, dying (ragdoll with the skinned mesh), and on curbs (foot IK).
- **Known issues / not verified in Unity:** compiled with `dotnet build` only; nothing has been seen in the editor yet. Watch for: inverted faces, garments clipping at extreme poses (hands up, cower), the first spawn being slower while fabric textures are generated. The `.meta` files for the new scripts are created by Unity on first import and should be committed with this work.

## 2026-10-27 — Claude Code (Opus 5.5)
- **Phase 4 (combat + AI):**
  - Player side: pistol viewmodel with nitrile-gloved hands, recoil, tactical and empty reloads, casings, muzzle flash; bottle throw; med kits (H); death screen ("NO SIGNAL").
  - NPC side: segmented NPCs with ragdolls; Perception (light-aware sight, hearing); `NPCBrain` FSM with morale, surrender and spare; hidden Ledger.
- **Changes after owner feedback:**
  - Added a VITALS health bar (bottom-left).
  - Added and then **removed** threat-direction arcs: the owner disliked them.
  - Fairness changes:
    - NPCs carry lights (Smilers: head stream light with a red LIVE tally; armed looters: phone light).
    - LED masks are bigger and brighter.
    - NPCs only fire within 26 m, and the first 2 shots of a fight always miss.
  - Dropped ammo glows.
  - Melee on **F** when nothing is interactable: pistol-whip, or a gloved punch with its own viewmodel. Damage lands on the contact frame.
  - Camera jitter fix: loose rigidbodies ignore the player capsule, and a landing needs more than 0.2 s in the air.
- **Owner still needs to test:** the melee animation and the jitter fix.
- **Open question:** the owner may want melee on a separate key (V) instead of F.

## 2026-10-26 — Claude Code (Opus 5.5)
- Phases 1–3: foundation (input, FPS controller, bodycam rig, post-FX, HUD), world (texture synthesis, buildings, props, sky, FX, probes, NavMesh) and audio (all synthesized). All three tested and approved by the owner.
