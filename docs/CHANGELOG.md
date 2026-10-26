# Changelog

Newest first. Each entry: date, agent, what changed, what the owner still needs to test, known issues.

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
