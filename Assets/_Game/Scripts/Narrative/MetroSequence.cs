using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Echoes.AI;
using Echoes.Audio;
using Echoes.Combat;
using Echoes.Rendering;
using Echoes.UI;
using Echoes.World;

namespace Echoes.Narrative
{
    /// <summary>
    /// 8c, the dead subway. Out of the house, the back alley is burning north of the station entrance. At the stairs
    /// Vance calls (his voice, the first time) and tells Daniel who is down there: Boone, brainwashed, huge, blind,
    /// bullet-proof, faster than him. Daniel goes down (he says where it comes up: in Mia's schoolyard). Past the
    /// turnstiles Boone stands on the platform and hears him come in; he hunts by sound (BlindHunter) and bullets spark
    /// off him. Daniel slips across (through the standing train, or down the ramps over the track) to the far platform,
    /// where the passage to the exit stairs is closed by a gate rusted shut. Forcing it takes six seconds of holding F and
    /// screams further and further, so Boone has to be lured away first; then he comes running, Daniel sprints up the
    /// stairs into the yard and drags the street grille shut behind him. MetroBuilder builds the station with the scene;
    /// this adds Boone, the gates, the signs and a crate of bottles.
    /// </summary>
    public class MetroSequence : MonoBehaviour
    {
        /// <summary>Daniel is down in the station (the checkpoint).</summary>
        public bool Reached { get; private set; }
        public bool Done { get; private set; }

        static readonly Vector3[] Route =
        {
            new Vector3(-80f, MetroBuilder.FL, -37f), new Vector3(-100f, MetroBuilder.FL, -37f), new Vector3(-98f, MetroBuilder.FL, -32.3f),
            new Vector3(-98f, MetroBuilder.FL, -27f), new Vector3(-80f, MetroBuilder.FL, -27f), new Vector3(-72f, MetroBuilder.FL, -27f),
            new Vector3(-92f, MetroBuilder.FL, -27f), new Vector3(-92f, MetroBuilder.FL, -32.3f),
        };

        GameObject player;
        BlindHunter hunter;
        Transform gateLeaf, grille;
        Collider gateBlock, grilleBlock;
        bool built, talking, gateOpen, forcing, bottlesTaken;
        int heardLines;

        // ------------------------------------------------------------------ setup (after the NavMesh bake)

        public void Setup()
        {
            if (built) return;
            built = true;
            var root = new GameObject("MetroRuntime").transform;
            root.SetParent(transform, false);
            Signs(root);
            Gates(root);
            Bottles(root);
            SpawnHunter();
        }

        void SpawnHunter()
        {
            hunter = BlindHunter.Spawn(new Vector3(-90f, MetroBuilder.FL, -37f), 270f, Route, 4471);
            if (hunter == null) return;
            hunter.Hold();
            hunter.ModeChanged += OnHunterMode;
        }

        void Signs(Transform root)
        {
            var white = new Color(0.9f, 0.9f, 0.86f);
            float fl = MetroBuilder.FL;
            // Station name on both platform walls, the way out over the passage, the name on the two street posts.
            for (float x = -104f; x < -68f; x += 16f)
            {
                Stencil(root, "KESSLER ST", new Vector3(x, fl + 2.4f, MetroBuilder.SP0 + 0.02f), 1.8f, white, 180f);
                Stencil(root, "KESSLER ST", new Vector3(x + 8f, fl + 2.4f, MetroBuilder.NP1 - 0.02f), 1.8f, white, 0f);
            }
            float ex = (MetroBuilder.EX0 + MetroBuilder.EX1) / 2f;
            Stencil(root, "EXIT\nSCHOOL", new Vector3(ex, fl + 3.1f, MetroBuilder.NP1 - 0.17f), 1.1f, new Color(0.55f, 1f, 0.6f), 0f, true);
            Stencil(root, "SUBWAY\nKESSLER ST", new Vector3(MetroBuilder.AX1 + 0.45f, DistrictGenerator.Curb + 2.75f, MetroBuilder.AZTop + 0.2f - 0.035f), 0.46f, white, 0f);
            Stencil(root, "SUBWAY\nKESSLER ST", new Vector3(MetroBuilder.EX1 + 0.45f, DistrictGenerator.Curb + 2.75f, MetroBuilder.BZTop - 0.2f + 0.035f), 0.46f, white, 180f);
        }

        /// <summary>A stencil quad facing −Z rotated by yaw (0 faces −Z, 180 faces +Z); `glow` makes it lit (the exit sign).</summary>
        static void Stencil(Transform root, string text, Vector3 centre, float width, Color tint, float yaw, bool glow = false)
        {
            var aspect = PixelFont.Aspect(text, false);
            var mat = RuntimeMaterials.Decal("Stencil_" + text.Replace('\n', '_'), PixelFont.Paint(text, false, 6, text.Length), tint, 0.3f, glow ? tint * 1.5f : (Color?)null);
            PrimitiveMeshes.Part(root, "Sign", PrimitiveType.Quad, centre, new Vector3(width, width * aspect.y / aspect.x, 1f), mat, Quaternion.Euler(0f, yaw, 0f), false);
        }

        /// <summary>The rusted gate across the exit passage (shut), and the street grille at the top of the exit stairs (open).</summary>
        void Gates(Transform root)
        {
            var rust = RuntimeMaterials.Lit(new Color(0.3f, 0.16f, 0.09f), 0.25f, 0.6f);
            float x0 = MetroBuilder.EX0, x1 = MetroBuilder.EX1, fl = MetroBuilder.FL;
            gateLeaf = Grille(root, "RustedGate", new Vector3(x0, fl, MetroBuilder.GateZ), x1 - x0, 2.4f, rust);
            gateBlock = gateLeaf.GetComponentInChildren<BoxCollider>();
            var use = new GameObject("GateUse", typeof(BoxCollider));
            use.transform.SetParent(root, false);
            use.transform.position = new Vector3((x0 + x1) / 2f, fl + 1f, MetroBuilder.GateZ - 0.3f);
            var bc = use.GetComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(x1 - x0, 2f, 0.5f);
            StoryInteractable.Add(use, "Force the gate (hold)", () => !gateOpen && !forcing, p => StartCoroutine(ForceGate()), 2.6f);

            // The street grille: hinged at the east railing, folded back along it for now (8d: the party's hoarding
            // stands round the top of the stairs, so it can no longer slide out sideways).
            float y = DistrictGenerator.Curb;
            grille = Grille(root, "StreetGrille", new Vector3(x1 + 0.15f, y, MetroBuilder.BZTop + 0.3f), x1 - x0 + 0.2f, 2.2f, RuntimeMaterials.Lit(new Color(0.2f, 0.2f, 0.22f), 0.4f, 0.8f));
            grille.rotation = Quaternion.Euler(0f, 90f, 0f);
            grilleBlock = grille.GetComponentInChildren<BoxCollider>();
            grilleBlock.enabled = false;
        }

        /// <summary>A grille of bars on a pivot at its left end, lying along +X; one box collider covers it.</summary>
        static Transform Grille(Transform root, string name, Vector3 pivotPos, float width, float height, Material m)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(root, false);
            pivot.position = pivotPos;
            for (float u = 0.06f; u < width; u += 0.14f)
                PrimitiveMeshes.Part(pivot, "Bar", PrimitiveType.Cube, new Vector3(u, height / 2f, 0f), new Vector3(0.03f, height, 0.03f), m, null, false);
            foreach (float h in new[] { 0.15f, height / 2f, height - 0.1f })
                PrimitiveMeshes.Part(pivot, "Rail", PrimitiveType.Cube, new Vector3(width / 2f, h, 0f), new Vector3(width, 0.05f, 0.04f), m, null, false);
            var col = new GameObject("Block", typeof(BoxCollider));
            col.transform.SetParent(pivot, false);
            col.transform.localPosition = new Vector3(width / 2f, height / 2f, 0f);
            col.GetComponent<BoxCollider>().size = new Vector3(width, height, 0.12f);
            return pivot;
        }

        /// <summary>A crate of empty bottles by the ticket machines: something to throw.</summary>
        void Bottles(Transform root)
        {
            var pos = new Vector3(MetroBuilder.CX0 + 1.1f, MetroBuilder.FL, MetroBuilder.CZ0 + 5.6f);
            var crate = PrimitiveMeshes.Part(root, "BottleCrate", PrimitiveType.Cube, pos + Vector3.up * 0.18f, new Vector3(0.5f, 0.36f, 0.35f),
                                             RuntimeMaterials.Lit(new Color(0.2f, 0.3f, 0.18f), 0.3f), null, true);
            var glass = RuntimeMaterials.Lit(new Color(0.12f, 0.3f, 0.15f), 0.9f, 0f, new Color(0.05f, 0.12f, 0.06f));
            for (int i = 0; i < 4; i++)
                PrimitiveMeshes.Part(root, "Bottle", PrimitiveType.Cylinder, pos + new Vector3(-0.15f + i * 0.1f, 0.45f, 0f), new Vector3(0.06f, 0.11f, 0.06f), glass, null, false);
            crate.AddComponent<BoxCollider>();
            StoryInteractable.Add(crate, "Take the bottles", () => !bottlesTaken, p =>
            {
                bottlesTaken = true;
                if (p.TryGetComponent(out ThrowController t)) t.bottles += 3;
                if (BodycamHUD.Instance != null) BodycamHUD.Instance.ShowAmmo("BOTTLES +3", 2f);
                foreach (Transform c in crate.transform.parent) if (c.name == "Bottle") c.gameObject.SetActive(false);
            });
        }

        // ------------------------------------------------------------------ the scene

        public IEnumerator Play(HouseSequence house)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) yield break;
            var hud = BodycamHUD.Instance;

            // Out of the house, up the back alley: it is burning further on, the station stairs are right here.
            while (!house.Done && !Reached) yield return null;
            if (!Reached)
            {
                while (!NearEntrance() && !Underground()) yield return null;
                if (!Underground()) yield return VanceCall();
                if (hud != null)
                {
                    hud.SetObjective("The school - through the subway, Kessler St");
                    hud.ShowHint("The alley is blocked. Down the subway stairs: the station comes up in the school yard.", 7f);
                }
                yield return Speak(DemoLines.MetroBlocked);
            }

            // Down.
            while (!Underground()) yield return null;
            Reached = true;
            if (hud != null) hud.ShowHint("No power. T flashlight.", 4f);
            yield return Speak(DemoLines.MetroDark);

            // Past the turnstiles: he is on the platform, and he hears Daniel.
            while (!PastTurnstiles()) yield return null;
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.TensionHit, 0.6f);
            if (hunter != null) yield return Speak(DemoLines.BlindReveal.At(hunter.transform));
            yield return Speak(DemoLines.BlindThought);
            if (hud != null)
            {
                hud.SetObjective("The exit - the far platform, through the train");
                hud.ShowHint("He is blind, and bullets won't stop him. Crouch (C), don't run, throw bottles (G) to lure him away. The exit is on the far platform: cross through the train.", 10f);
            }
            if (hunter != null) hunter.Release();

            // The gate, then the run.
            while (!gateOpen) yield return null;
            if (hud != null) hud.ShowHint("RUN! Up the stairs!", 4f);
            if (hunter != null) StartCoroutine(Speak(DemoLines.BlindChase.At(hunter.transform)));
            while (!AtTheTop()) yield return null;
            yield return CloseGrille();
            Done = true;
            if (hud != null) hud.SetObjective("Find Mia - the school");
            if (hunter != null)
            {
                // He comes up to the grille and stops there: from now on he can't hear past it (owner: he got through the
                // shut grille and attacked at the party; a NavMesh agent walks through colliders, so it is carved too).
                hunter.HuntAt(MetroBuilder.ExitTop + Vector3.back * 2.2f);
                yield return new WaitForSeconds(2.5f);
                if (hunter != null) hunter.Hold();
                if (hunter != null) yield return Speak(DemoLines.BlindGrille.At(hunter.transform));
                yield return new WaitForSeconds(4f);
                if (hunter != null) Destroy(hunter.gameObject);
            }
        }

        /// <summary>Vance rings from an unknown number and tells Daniel who is waiting below. Daniel stops for it, as for every call.</summary>
        IEnumerator VanceCall()
        {
            var input = player.GetComponent<Player.EchoesInput>();
            if (input != null) input.LookOnly = true;
            while (talking) yield return null;
            talking = true;
            if (DialogueDirector.Instance != null) yield return DialogueDirector.Instance.Call("UNKNOWN", true, DemoLines.VanceCall);
            talking = false;
            if (input != null) input.LookOnly = false;
        }

        bool NearEntrance()
        {
            Vector3 p = player.transform.position;
            return Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(MetroBuilder.EntranceTop.x, 0f, MetroBuilder.EntranceTop.z)) < 9f;
        }

        bool Underground()
        {
            Vector3 p = player.transform.position;
            return p.y < MetroBuilder.FL + 1.5f && p.z > MetroBuilder.CZ0 - 0.5f;
        }

        bool PastTurnstiles()
        {
            Vector3 p = player.transform.position;
            return p.y < MetroBuilder.FL + 1f && p.z > MetroBuilder.TurnZ + 0.4f;
        }

        bool AtTheTop()
        {
            Vector3 p = player.transform.position;
            // Past the grille's line, so it closes behind him, not in front.
            return p.y > DistrictGenerator.Curb - 0.6f && p.z > MetroBuilder.BZTop + 1.2f && p.x > MetroBuilder.EX0 - 3f && p.x < MetroBuilder.EX1 + 3f;
        }

        void OnHunterMode(BlindHunter.Mode m)
        {
            // When something makes him turn and come, he says so (not every time).
            if (m != BlindHunter.Mode.Hunt || gateOpen || talking || hunter == null) return;
            if (Random.value < 0.6f) StartCoroutine(Speak(DemoLines.BlindHeard(heardLines++).At(hunter.transform)));
        }

        /// <summary>
        /// The gate won't open quietly (owner: running to it and out was too easy). Daniel has to hold F with his
        /// shoulder in it for six seconds; it screams from the first second, louder and further as it gives. Lure Boone
        /// to the far end of the station first, or he is on you before it opens. Letting go pauses it; the progress stays.
        /// </summary>
        IEnumerator ForceGate()
        {
            forcing = true;
            StartCoroutine(Speak(DemoLines.GateRusted));
            var input = player.GetComponent<Player.EchoesInput>();
            var rig = player.GetComponent<Player.CameraRig>();
            var hud = BodycamHUD.Instance;
            var am = AudioManager.Instance;
            Vector3 g = MetroBuilder.ExitGate + Vector3.up;
            const float needed = 6f;
            float progress = 0f, nextNoise = 0f, nextScreech = 0f;
            while (progress < needed)
            {
                bool pushing = input != null && input.InteractHeld && Vector3.Distance(player.transform.position, MetroBuilder.ExitGate) < 2.2f;
                if (pushing)
                {
                    progress += Time.deltaTime;
                    float k = progress / needed;
                    gateLeaf.rotation = Quaternion.Euler(0f, -25f * k, 0f); // it gives a little at a time
                    if (Time.time > nextScreech) { nextScreech = Time.time + 2.4f; if (am != null) am.PlayAt(SoundId.GateScreech, g, 1f, Random.Range(0.9f, 1.05f), 3f, 120f); }
                    if (Time.time > nextNoise) { nextNoise = Time.time + 0.5f; NoiseSystem.Emit(g, Mathf.Lerp(15f, 60f, k), NoiseKind.Impact, player, true); }
                    if (rig != null && Random.value < 0.08f) rig.AddTrauma(0.04f);
                    if (hud != null)
                    {
                        int bars = Mathf.RoundToInt(k * 12f);
                        hud.ShowAmmo("FORCING THE GATE  " + new string('|', bars) + new string('.', 12 - bars), 0.25f);
                    }
                }
                yield return null;
            }
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                gateLeaf.rotation = Quaternion.Euler(0f, Mathf.Lerp(-25f, -100f, t / 0.4f), 0f);
                yield return null;
            }
            if (am != null) am.PlayAt(SoundId.ImpactMetal, g, 1f, 0.7f, 3f, 80f);
            if (gateBlock != null) gateBlock.enabled = false;
            if (hunter != null) hunter.HuntAt(g);
            gateOpen = true;
            forcing = false;
        }

        /// <summary>At the top, Daniel drags the street grille across behind him.</summary>
        IEnumerator CloseGrille()
        {
            var am = AudioManager.Instance;
            if (am != null) am.PlayAt(SoundId.MetalScrape, grille.position + Vector3.up, 0.8f, 1.3f, 2f, 40f);
            // Swung on its hinge from along the railing (90) across the stairwell (180).
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                grille.rotation = Quaternion.Euler(0f, Mathf.Lerp(90f, 180f, Mathf.SmoothStep(0f, 1f, t / 0.5f)), 0f);
                yield return null;
            }
            ShutGrille();
            if (am != null) am.PlayAt(SoundId.ImpactMetal, MetroBuilder.ExitTop + Vector3.up, 1f, 0.7f, 2f, 50f);
        }

        void ShutGrille()
        {
            grille.rotation = Quaternion.Euler(0f, 180f, 0f);
            if (grilleBlock == null) return;
            grilleBlock.enabled = true;
            if (!grilleBlock.TryGetComponent(out NavMeshObstacle o))
            {
                o = grilleBlock.gameObject.AddComponent<NavMeshObstacle>();
                o.shape = NavMeshObstacleShape.Box;
                var size = grilleBlock is BoxCollider bc ? bc.size : new Vector3(2.4f, 2.2f, 0.12f);
                o.size = new Vector3(size.x, size.y, 0.6f);
                o.center = Vector3.zero;
                o.carving = true;
            }
        }

        /// <summary>A checkpoint restart down in the station: the descent has been made.</summary>
        public void RestoreAfterCheckpoint() => Reached = true;

        /// <summary>A checkpoint restart past the station (8d, the party): up and out, the grille shut, Boone gone.</summary>
        public void RestoreDone()
        {
            Reached = Done = gateOpen = true;
            ShutGrille();
            if (hunter != null) Destroy(hunter.gameObject);
            if (BodycamHUD.Instance != null) BodycamHUD.Instance.SetObjective("");
        }

        IEnumerator Speak(params Line[] lines)
        {
            while (talking) yield return null;
            talking = true;
            if (DialogueDirector.Instance != null) yield return DialogueDirector.Instance.Conversation(lines);
            talking = false;
        }
    }
}
