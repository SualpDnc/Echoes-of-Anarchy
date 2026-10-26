# Changelog

Newest first. Each entry: date, agent, what changed, what the owner still needs to test, known issues.

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
