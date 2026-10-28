using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Echoes.AI;
using Echoes.Audio;
using Echoes.Combat;
using Echoes.Core;
using Echoes.Rendering;
using Echoes.UI;
using Echoes.World;

namespace Echoes.Narrative
{
    /// <summary>
    /// 8b, Daniel's house. Past the gas station's back gate "Mia" calls (the clone): she is hiding in the closet, they
    /// are inside, hurry. The house is across the alley, dark, the front door open. Footsteps overhead; upstairs Mia's
    /// music box starts by itself and a door slams. In Mia's closet a phone loops her voice (Daniel: "It's a recording.
    /// They're using her voice."); on her bed, an old unit photo with Vance's face scratched out and a note: "SCHOOL.
    /// COME ALONE. :)". Then headlights: the truck that rammed the ambulance pulls up and the Smilers come in through the
    /// front; Daniel gets out through the back gate, and they let him go ("The show's at the school!").
    /// HouseBuilder builds the static house with the scene; this adds the runtime parts (fabric, doors, the props).
    /// </summary>
    public class HouseSequence : MonoBehaviour
    {
        public bool Done { get; private set; }

        GameObject player;
        Transform phone, closetL, closetR, danielDoor, photo, note, backGate;
        Light phoneLight;
        bool built, talking, closetOpen, phoneTaken, bedSeen, raiding, backGateOpen;
        readonly List<NPCBrain> hunters = new List<NPCBrain>();
        VehicleRig truck;

        // ------------------------------------------------------------------ setup (after the NavMesh bake)

        public void Setup()
        {
            if (built) return;
            built = true;
            var root = new GameObject("HouseRuntime").transform;
            root.SetParent(transform, false);
            Fabrics(root);
            Doors(root);
            MiasRoom(root);
            ClosetAndPhone(root);
            BedClues(root);
            Entrance(root);
        }

        /// <summary>The scene builder made the soft furniture from boxes; here it gets real fabric, plus the soft things that have no collider.</summary>
        void Fabrics(Transform root)
        {
            void Dress(string name, Color c, Fabric f)
            {
                var go = GameObject.Find("HouseFabric_" + name);
                if (go == null || !go.TryGetComponent(out MeshRenderer mr)) return;
                var mats = mr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = RuntimeMaterials.Fabric(c, 0.2f, f);
                mr.sharedMaterials = mats;
            }
            Dress("Sofa", new Color(0.18f, 0.2f, 0.17f), Fabric.Wool);
            Dress("BedMia", new Color(0.42f, 0.3f, 0.45f), Fabric.Cotton);
            Dress("BedDaniel", new Color(0.08f, 0.1f, 0.16f), Fabric.Cotton);

            float g = HouseBuilder.F0, u = HouseBuilder.F1;
            var rug = RuntimeMaterials.Fabric(new Color(0.32f, 0.12f, 0.08f), 0.15f, Fabric.Wool);
            PrimitiveMeshes.Part(root, "Rug", PrimitiveType.Cube, new Vector3(-71f, g + 0.006f, -68f), new Vector3(2.4f, 0.012f, 3f), rug, null, false);
            PrimitiveMeshes.Part(root, "MiaRug", PrimitiveType.Cube, new Vector3(-70.6f, u + 0.006f, -66.6f), new Vector3(1.6f, 0.012f, 1.2f),
                                 RuntimeMaterials.Fabric(new Color(0.55f, 0.5f, 0.6f), 0.1f, Fabric.Knit), null, false);
            // Pillows and a blanket folded over the end of Mia's bed.
            PrimitiveMeshes.Part(root, "Pillow", PrimitiveType.Cube, new Vector3(-73.05f, u + 0.56f, -66.1f), new Vector3(0.4f, 0.12f, 0.7f),
                                 RuntimeMaterials.Fabric(new Color(0.75f, 0.72f, 0.7f), 0.2f, Fabric.Cotton), null, false);
            PrimitiveMeshes.Part(root, "Blanket", PrimitiveType.Cube, new Vector3(-71.75f, u + 0.52f, -66.1f), new Vector3(0.65f, 0.06f, 1.04f),
                                 RuntimeMaterials.Fabric(new Color(0.65f, 0.45f, 0.2f), 0.15f, Fabric.Knit), null, false);
            // Curtains at Mia's front window and the living-room window, half drawn.
            var curtain = RuntimeMaterials.Fabric(new Color(0.5f, 0.42f, 0.36f), 0.1f, Fabric.Cotton);
            foreach (float y in new[] { g, u })
                foreach (float z in new[] { -67.45f, -65.35f })
                    PrimitiveMeshes.Part(root, "Curtain", PrimitiveType.Cube, new Vector3(HouseBuilder.X1 - HouseBuilder.T - 0.06f, y + 1.3f, z), new Vector3(0.03f, 2.3f, 0.5f), curtain, null, true);
        }

        /// <summary>Door leaves on hinges: the front door standing open, Mia's and the bathroom's open, Daniel's open (for now).</summary>
        void Doors(Transform root)
        {
            var wood = RuntimeMaterials.Lit(new Color(0.42f, 0.36f, 0.3f), 0.35f);
            var white = RuntimeMaterials.Lit(new Color(0.7f, 0.69f, 0.66f), 0.3f);
            // A leaf lies along the pivot's +X; yaw −90 points it along +Z, 90 along −Z, 180 along −X.
            Transform Leaf(string name, Vector3 hinge, float width, float yawNow, Material m)
            {
                var pivot = new GameObject(name).transform;
                pivot.SetParent(root, false);
                pivot.SetPositionAndRotation(hinge, Quaternion.Euler(0f, yawNow, 0f));
                var leaf = PrimitiveMeshes.Part(pivot, "Leaf", PrimitiveType.Cube, new Vector3(width / 2f, 1.04f, 0f), new Vector3(width, 2.08f, 0.045f), m, null, true);
                leaf.AddComponent<BoxCollider>();
                return pivot;
            }
            float g = HouseBuilder.F0, u = HouseBuilder.F1, x1 = HouseBuilder.X1 - HouseBuilder.T / 2f, xm = HouseBuilder.XM;
            // Front door: hinged at its south edge, the leaf along +Z when shut (yaw −90), swung in.
            Leaf("FrontDoor", new Vector3(x1, g, -61f), 0.98f, -150f, wood); // ajar, clear of the way in
            // Daniel's door off the landing: hinged at its north edge, swung into his room (it will slam).
            danielDoor = Leaf("DanielDoor", new Vector3(xm, u, -57.77f), 0.9f, 175f, white);
            Leaf("MiaDoor", new Vector3(-70.22f, u, HouseBuilder.ZM), 0.95f, 95f, white);
            Leaf("BathDoor", new Vector3(-75.02f, u, HouseBuilder.ZM), 0.95f, 85f, white);
            BackGate(root);
        }

        /// <summary>
        /// The back gate in the board fence, bolted shut (the owner walked round the house and down the subway before
        /// the house was done). It opens from the yard once the raid starts: that is the way out.
        /// </summary>
        void BackGate(Transform root)
        {
            var boards = RuntimeMaterials.Lit(new Color(0.33f, 0.26f, 0.2f), 0.2f);
            float y = DistrictGenerator.Curb, w = 1.2f;
            backGate = new GameObject("BackGate").transform;
            backGate.SetParent(root, false);
            backGate.SetPositionAndRotation(new Vector3(HouseBuilder.LotX0 + 0.05f, y, HouseBuilder.BackDoorZ - w / 2f), Quaternion.Euler(0f, -90f, 0f));
            for (int i = 0; i < 8; i++)
                PrimitiveMeshes.Part(backGate, "Board", PrimitiveType.Cube, new Vector3(0.08f + i * 0.148f, 0.88f, 0f), new Vector3(0.14f, 1.75f - (i % 2) * 0.04f, 0.04f), boards, null, false);
            foreach (float h in new[] { 0.3f, 1.45f })
                PrimitiveMeshes.Part(backGate, "Brace", PrimitiveType.Cube, new Vector3(w / 2f, h, -0.035f), new Vector3(w - 0.06f, 0.1f, 0.03f), boards, null, false);
            PrimitiveMeshes.Part(backGate, "Bolt", PrimitiveType.Cube, new Vector3(w - 0.12f, 1f, -0.06f), new Vector3(0.2f, 0.04f, 0.03f),
                                 RuntimeMaterials.Lit(new Color(0.25f, 0.25f, 0.26f), 0.5f, 0.8f), null, false);
            var block = new GameObject("Block", typeof(BoxCollider));
            block.transform.SetParent(backGate, false);
            block.transform.localPosition = new Vector3(w / 2f, 1.1f, 0f);
            block.GetComponent<BoxCollider>().size = new Vector3(w, 2.2f, 0.1f);
            StoryInteractable.Add(block, "Open the back gate", () => !backGateOpen && (raiding || Done), p => OpenBackGate(true), 2.6f);
        }

        void OpenBackGate(bool animate)
        {
            if (backGateOpen || backGate == null) return;
            backGateOpen = true;
            if (animate) StartCoroutine(Slam(backGate, -170f, false)); // out into the alley, away from Daniel (owner: it swung into him)
            else backGate.rotation = Quaternion.Euler(0f, -170f, 0f);
        }

        void MiasRoom(Transform root)
        {
            float u = HouseBuilder.F1;
            // A night-light on the desk (batteries; the power is out): the only warm light in the house, seen from the stairs.
            var lampGo = new GameObject("NightLight", typeof(Light));
            lampGo.transform.SetParent(root, false);
            lampGo.transform.position = new Vector3(-69.1f, u + 0.95f, -69.6f);
            var lamp = lampGo.GetComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.72f, 0.5f);
            lamp.intensity = 0.9f;
            lamp.range = 5f;
            lamp.shadows = LightShadows.Soft;
            PrimitiveMeshes.Part(root, "NightLightGlobe", PrimitiveType.Sphere, new Vector3(-69.1f, u + 0.84f, -69.6f), Vector3.one * 0.12f,
                                 RuntimeMaterials.Lit(new Color(1f, 0.8f, 0.6f), 0.6f, 0f, new Color(1f, 0.7f, 0.45f) * 2.5f), null, false);
            // The music box beside it, and posters on the walls.
            PrimitiveMeshes.Part(root, "MusicBox", PrimitiveType.Cube, new Vector3(-69.05f, u + 0.81f, -69f), new Vector3(0.14f, 0.09f, 0.1f),
                                 RuntimeMaterials.Lit(new Color(0.55f, 0.3f, 0.35f), 0.6f), null, true);
            Color[] posters = { new Color(0.15f, 0.35f, 0.6f), new Color(0.65f, 0.45f, 0.15f), new Color(0.45f, 0.15f, 0.4f) };
            for (int i = 0; i < posters.Length; i++)
                PrimitiveMeshes.Part(root, "Poster", PrimitiveType.Cube, new Vector3(-72.3f + i * 0.9f, u + 1.6f + (i % 2) * 0.1f, HouseBuilder.Z0 + HouseBuilder.T + 0.01f),
                                     new Vector3(0.5f, 0.7f, 0.01f), RuntimeMaterials.Lit(posters[i], 0.3f), null, false);
        }

        /// <summary>Mia's closet, doors shut; inside, a phone on the floor, screen lit, looping her voice.</summary>
        void ClosetAndPhone(Transform root)
        {
            float u = HouseBuilder.F1, x = HouseBuilder.ClosetX1, z0 = HouseBuilder.ClosetZ0, z1 = HouseBuilder.ClosetZ1, w = (z1 - z0) / 2f;
            var wood = RuntimeMaterials.Lit(new Color(0.62f, 0.58f, 0.52f), 0.3f);
            closetL = new GameObject("ClosetDoorL").transform;
            closetL.SetParent(root, false);
            closetL.position = new Vector3(x, u, z0 + 0.04f);
            PrimitiveMeshes.Part(closetL, "Leaf", PrimitiveType.Cube, new Vector3(0.01f, 1.08f, w / 2f - 0.02f), new Vector3(0.025f, 2.12f, w - 0.04f), wood, null, true);
            closetR = new GameObject("ClosetDoorR").transform;
            closetR.SetParent(root, false);
            closetR.position = new Vector3(x, u, z1 - 0.04f);
            PrimitiveMeshes.Part(closetR, "Leaf", PrimitiveType.Cube, new Vector3(0.01f, 1.08f, -(w / 2f - 0.02f)), new Vector3(0.025f, 2.12f, w - 0.04f), wood, null, true);

            // Owner: the phone couldn't be seen. Now it stands propped on a shoebox at knee height, facing out, the
            // screen bright (a voice memo playing on repeat) and lighting the inside of the closet.
            PrimitiveMeshes.Part(root, "Shoebox", PrimitiveType.Cube, new Vector3(x - 0.33f, u + 0.09f, (z0 + z1) / 2f), new Vector3(0.3f, 0.18f, 0.42f),
                                 RuntimeMaterials.Lit(new Color(0.55f, 0.18f, 0.16f), 0.3f), null, true);
            phone = new GameObject("ClosetPhone").transform;
            phone.SetParent(root, false);
            Vector3 face = new Vector3(0.97f, 0.26f, 0f), along = new Vector3(-0.26f, 0.97f, 0f); // screen out and a little up; long side up
            phone.SetPositionAndRotation(new Vector3(x - 0.36f, u + 0.18f + 0.075f, (z0 + z1) / 2f), Quaternion.LookRotation(along, face));
            PrimitiveMeshes.Part(phone, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.075f, 0.009f, 0.15f), RuntimeMaterials.Lit(new Color(0.03f, 0.03f, 0.035f), 0.8f), null, false);
            var screenMat = new Material(RuntimeMaterials.Lit(Color.black, 0.9f, 0f, Color.white * 2.6f)) { name = "RT_PhoneScreen", hideFlags = HideFlags.DontSave };
            screenMat.SetTexture("_EmissionMap", ProcTextures.PhoneScreen());
            PrimitiveMeshes.Part(phone, "Screen", PrimitiveType.Cube, new Vector3(0f, 0.005f, 0f), new Vector3(0.067f, 0.002f, 0.138f), screenMat, null, false);
            var lg = new GameObject("PhoneGlow", typeof(Light));
            lg.transform.SetParent(phone, false);
            lg.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            phoneLight = lg.GetComponent<Light>();
            phoneLight.type = LightType.Point;
            phoneLight.color = new Color(0.7f, 0.82f, 1f);
            phoneLight.intensity = 1.4f;
            phoneLight.range = 2.6f;

            // One trigger over the closet front: first it opens the doors, then it takes the phone.
            var hit = new GameObject("ClosetUse", typeof(BoxCollider));
            hit.transform.SetParent(root, false);
            hit.transform.position = new Vector3(x + 0.05f, u + 1f, (z0 + z1) / 2f);
            var bc = hit.GetComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(0.3f, 2f, z1 - z0);
            StoryInteractable closet = null;
            closet = StoryInteractable.Add(hit, "Open the closet", () => !phoneTaken && !talking && !raiding, p =>
            {
                if (!closetOpen) { closetOpen = true; closet.prompt = "Pick up the phone"; StartCoroutine(OpenCloset()); }
                else StartCoroutine(TakePhone());
            }, 2.6f);
        }

        void BedClues(Transform root)
        {
            Vector3 bed = HouseBuilder.MiaBed + Vector3.up * 0.502f; // on the mattress
            var photoMat = new Material(RuntimeMaterials.Lit(Color.white, 0.4f)) { name = "RT_UnitPhoto", hideFlags = HideFlags.DontSave };
            photoMat.SetTexture("_BaseMap", ProcTextures.UnitPhoto());
            var t = photoMat.GetTexture("_BaseMap");
            float aspect = t.width / (float)t.height;
            photo = PrimitiveMeshes.Part(root, "UnitPhoto", PrimitiveType.Quad, bed + new Vector3(0.1f, 0.006f, -0.05f), new Vector3(0.3f * aspect * 0.5f, 0.3f * 0.5f, 1f) * 1.4f,
                                         photoMat, Quaternion.Euler(90f, -84f, 0f), false).transform;
            var noteMat = new Material(RuntimeMaterials.Lit(Color.white, 0.2f)) { name = "RT_MiaNote", hideFlags = HideFlags.DontSave };
            noteMat.SetTexture("_BaseMap", ProcTextures.MiaNote());
            note = PrimitiveMeshes.Part(root, "Note", PrimitiveType.Quad, bed + new Vector3(-0.2f, 0.004f, 0.22f), new Vector3(0.21f, 0.165f, 1f),
                                        noteMat, Quaternion.Euler(90f, -105f, 0f), false).transform;

            var hit = new GameObject("BedUse", typeof(BoxCollider));
            hit.transform.SetParent(root, false);
            hit.transform.position = bed + new Vector3(0.25f, 0.1f, 0f);
            var bc = hit.GetComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(1.2f, 0.3f, 1f);
            StoryInteractable.Add(hit, "Look at the photo", () => !bedSeen && !talking && !raiding, p => StartCoroutine(LookAtBed()), 2.8f);
        }

        /// <summary>
        /// The way in (owner: make the entrance look good): a doormat, the house number by the door, a mailbox with the
        /// family name at the gate, and solar garden lights along the fence, still glowing in the blackout: from the gate
        /// alley they mark the house.
        /// </summary>
        void Entrance(Transform root)
        {
            float g = HouseBuilder.F0, y = DistrictGenerator.Curb;
            PrimitiveMeshes.Part(root, "Doormat", PrimitiveType.Cube, new Vector3(HouseBuilder.X1 + 0.4f, g + 0.008f, HouseBuilder.DoorZ), new Vector3(0.5f, 0.016f, 0.85f),
                                 RuntimeMaterials.Fabric(new Color(0.22f, 0.16f, 0.1f), 0.1f, Fabric.Wool), null, false);
            Stencil(root, "14", new Vector3(HouseBuilder.X1 + 0.03f, g + 1.75f, HouseBuilder.DoorZ + 0.85f), 0.24f, new Color(0.85f, 0.8f, 0.65f));

            // Mailbox on a post outside the gate, REYES stencilled on the side that faces the alley.
            var post = new Vector3(HouseBuilder.LotX1 + 0.25f, y, HouseBuilder.DoorZ - 1.1f);
            var dark = RuntimeMaterials.Lit(new Color(0.08f, 0.08f, 0.09f), 0.5f, 0.6f);
            PrimitiveMeshes.Part(root, "MailPost", PrimitiveType.Cube, post + Vector3.up * 0.55f, new Vector3(0.08f, 1.1f, 0.08f), RuntimeMaterials.Lit(new Color(0.35f, 0.3f, 0.25f), 0.2f), null, true);
            PrimitiveMeshes.Part(root, "Mailbox", PrimitiveType.Cube, post + Vector3.up * 1.22f, new Vector3(0.24f, 0.24f, 0.46f), dark, null, true);
            Stencil(root, "REYES", post + new Vector3(0.125f, 1.22f, 0f), 0.36f, new Color(0.9f, 0.88f, 0.82f));

            // Solar stake lights inside the fence, either side of the gate.
            var stakeMat = RuntimeMaterials.Lit(new Color(0.9f, 0.95f, 0.85f), 0.6f, 0f, new Color(0.8f, 0.95f, 0.75f) * 2.2f);
            foreach (float dz in new[] { -2.3f, -1.1f, 1.1f, 2.3f })
            {
                var p = new Vector3(HouseBuilder.LotX1 - 0.45f, y, HouseBuilder.DoorZ + dz);
                PrimitiveMeshes.Part(root, "SolarStake", PrimitiveType.Cylinder, p + Vector3.up * 0.18f, new Vector3(0.025f, 0.18f, 0.025f), dark, null, false);
                PrimitiveMeshes.Part(root, "SolarLamp", PrimitiveType.Cylinder, p + Vector3.up * 0.4f, new Vector3(0.07f, 0.04f, 0.07f), stakeMat, null, false);
                var lg = new GameObject("SolarLight", typeof(Light));
                lg.transform.SetParent(root, false);
                lg.transform.position = p + Vector3.up * 0.45f;
                var l = lg.GetComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.85f, 0.95f, 0.8f);
                l.intensity = 0.45f;
                l.range = 2.2f;
            }
        }

        /// <summary>Stencilled text on a wall that faces +X (the alley side), `width` metres wide.</summary>
        static void Stencil(Transform root, string text, Vector3 centre, float width, Color tint)
        {
            var aspect = PixelFont.Aspect(text, false);
            var mat = RuntimeMaterials.Decal("Stencil_" + text, PixelFont.Paint(text, false, 6, text.Length), tint, 0.3f);
            PrimitiveMeshes.Part(root, "Stencil", PrimitiveType.Quad, centre, new Vector3(width, width * aspect.y / aspect.x, 1f), mat, Quaternion.Euler(0f, -90f, 0f), false);
        }

        // ------------------------------------------------------------------ the scene

        public IEnumerator Play(GasStationSequence station)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || Done) yield break;
            var hud = BodycamHUD.Instance;
            var dd = DialogueDirector.Instance;
            var input = player.GetComponent<Player.EchoesInput>();

            // Out of the back gate, a few steps into the alley, once Luis (if he's on the radio) has had his say.
            while (!InGateAlley(-47f) && !InLot()) yield return null;
            while (station.Talking) yield return null;
            if (Done) yield break;

            // "Mia" again. Daniel stops for it (as with every call): he can only look round.
            if (input != null) input.LookOnly = true;
            talking = true;
            if (dd != null) yield return dd.Call("MIA", true, DemoLines.HouseCall);
            talking = false;
            if (input != null) input.LookOnly = false;
            if (hud != null)
            {
                hud.SetObjective("Home - the house across the alley");
                hud.ShowHint("Your house is across the alley: the white fence. T flashlight.", 6f);
            }
            StartCoroutine(PhoneLoop());

            // In through the open front door.
            while (!InHouse()) yield return null;
            yield return Speak(DemoLines.HouseEnter);
            if (hud != null) hud.SetObjective("Find Mia - upstairs");
            yield return new WaitForSeconds(0.6f);
            yield return Footsteps();
            yield return Speak(DemoLines.HouseSteps);

            // Upstairs: the music box plays by itself; a door slams.
            while (!Upstairs()) yield return null;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.MusicBox, new Vector3(-69.05f, HouseBuilder.F1 + 0.85f, -69f), 0.55f, 1f, 1.5f, 18f);
            yield return new WaitForSeconds(4.5f);
            StartCoroutine(Slam(danielDoor, 90f));

            // The clues: the phone in the closet and the bed.
            while (!(phoneTaken && bedSeen)) yield return null;
            while (talking) yield return null;
            yield return new WaitForSeconds(1.5f);
            yield return Raid();
        }

        bool InGateAlley(float pastX)
        {
            Vector3 p = player.transform.position;
            return p.x < pastX && p.x > AlleyBuilder.NorthAlleyX0 && p.z > AlleyBuilder.GateAlleyZ0 - 1f && p.z < AlleyBuilder.GateAlleyZ1 + 1f;
        }

        bool InLot()
        {
            Vector3 p = player.transform.position;
            return p.x > HouseBuilder.LotX0 && p.x < HouseBuilder.LotX1 && p.z > HouseBuilder.LotZ0 && p.z < HouseBuilder.LotZ1;
        }

        bool InHouse()
        {
            Vector3 p = player.transform.position;
            return p.x > HouseBuilder.X0 && p.x < HouseBuilder.X1 - 0.3f && p.z > HouseBuilder.Z0 && p.z < HouseBuilder.Z1;
        }

        bool Upstairs() => InHouse() && player.transform.position.y > HouseBuilder.F1 - 0.3f;

        /// <summary>Her voice from the closet, over and over, while Daniel is in the house and nothing else is being said.</summary>
        IEnumerator PhoneLoop()
        {
            var dd = DialogueDirector.Instance;
            if (dd == null) yield break;
            var lines = DemoLines.MiaCall;
            while (!phoneTaken)
            {
                if (!InHouse() || talking) { yield return null; continue; }
                for (int i = 0; i < 2 && !phoneTaken && !talking; i++) yield return dd.Say(lines[i].At(phone));
                for (float t = 0f; t < 3f && !phoneTaken; t += Time.deltaTime) yield return null;
            }
        }

        /// <summary>Someone crossing the floor upstairs, from the landing into Mia's room. There is nobody there.</summary>
        IEnumerator Footsteps()
        {
            var am = AudioManager.Instance;
            if (am == null) yield break;
            float y = HouseBuilder.F1 + 0.05f;
            Vector3 a = new Vector3(-70.2f, y, -60f), b = new Vector3(-70.6f, y, -66f);
            for (int i = 0; i < 6; i++)
            {
                am.PlayAt(SoundId.StepWood, Vector3.Lerp(a, b, i / 5f), 0.55f, 0.9f, 2f, 25f);
                yield return new WaitForSeconds(0.55f);
            }
            am.PlayAt(SoundId.DoorSlam, b, 0.15f, 1.7f, 2f, 25f); // something small knocked over
        }

        /// <summary>Swings a door to `to`; `bang` = slammed hard (the scare), otherwise shoved open (the back gate).</summary>
        IEnumerator Slam(Transform door, float to, bool bang = true)
        {
            if (door == null) yield break;
            float from = door.eulerAngles.y, time = bang ? 0.25f : 0.45f;
            if (!bang && AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.DoorSlam, door.position + Vector3.up, 0.35f, 1.3f, 2f, 25f);
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                float k = t / time;
                door.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(from, to, bang ? k * k : 1f - (1f - k) * (1f - k)), 0f);
                yield return null;
            }
            door.rotation = Quaternion.Euler(0f, to, 0f);
            if (!bang) yield break;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.DoorSlam, door.position + Vector3.up, 0.9f, 1f, 2f, 40f);
            var rig = player.GetComponent<Player.CameraRig>();
            if (rig != null) rig.AddTrauma(0.12f);
        }

        IEnumerator OpenCloset()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayAt(SoundId.DoorSlam, closetL.position + Vector3.up, 0.12f, 1.9f, 1f, 10f);
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.7f);
                closetL.rotation = Quaternion.Euler(0f, 100f * k, 0f);   // both leaves swing out into the room
                closetR.rotation = Quaternion.Euler(0f, -100f * k, 0f);
                yield return null;
            }
        }

        IEnumerator TakePhone()
        {
            phoneTaken = true;
            talking = true;
            if (AudioManager.Instance != null) AudioManager.Instance.Play2D(SoundId.PhonePickup, 0.5f);
            // Into his hand (hidden): what is left of the line in progress comes from the phone he is holding.
            if (phone != null)
            {
                phone.SetParent(player.transform, false);
                phone.localPosition = new Vector3(0.15f, 1.2f, 0.3f);
                foreach (var r in phone.GetComponentsInChildren<Renderer>()) r.enabled = false;
                if (phoneLight != null) phoneLight.enabled = false;
            }
            var dd = DialogueDirector.Instance;
            // The screen up close: a voice memo on repeat. No doubt left about what it is (owner: make the story clear).
            yield return Inspect(ProcTextures.PhoneScreen(), true);
            if (dd != null) yield return dd.Conversation(DemoLines.ClosetPhone);
            yield return Inspect(ProcTextures.PhoneScreen(), false);
            talking = false;
            if (!bedSeen && BodycamHUD.Instance != null) BodycamHUD.Instance.SetObjective("Search Mia's room");
        }

        IEnumerator LookAtBed()
        {
            bedSeen = true;
            talking = true;
            var hud = BodycamHUD.Instance;
            var dd = DialogueDirector.Instance;
            var input = player.GetComponent<Player.EchoesInput>();
            if (input != null) input.LookOnly = true;
            yield return Inspect(ProcTextures.UnitPhoto(), true);
            if (dd != null) yield return dd.Conversation(DemoLines.UnitPhotoLook);
            yield return Inspect(ProcTextures.UnitPhoto(), false);
            yield return Inspect(ProcTextures.MiaNote(), true);
            if (dd != null) yield return dd.Conversation(DemoLines.NoteLook);
            yield return Inspect(ProcTextures.MiaNote(), false);
            if (input != null) input.LookOnly = false;
            talking = false;
            if (hud != null && !phoneTaken) hud.SetObjective("The voice - Mia's closet");
        }

        static IEnumerator Inspect(Texture tex, bool show)
        {
            var hud = BodycamHUD.Instance;
            if (hud == null) yield break;
            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { hud.ShowInspect(tex, show ? t / 0.4f : 1f - t / 0.4f); yield return null; }
            hud.ShowInspect(tex, show ? 1f : 0f);
        }

        // ------------------------------------------------------------------ the raid

        static readonly Vector3[] TruckRoute =
        {
            new Vector3(-47f, DistrictGenerator.Curb, -74.2f), new Vector3(-56f, DistrictGenerator.Curb, -74.4f), new Vector3(-62.6f, DistrictGenerator.Curb, -74.6f),
            new Vector3(-64.8f, DistrictGenerator.Curb, -72.4f), new Vector3(-64.8f, DistrictGenerator.Curb, -69.5f), new Vector3(-64.8f, DistrictGenerator.Curb, -67f),
        };

        static readonly Vector3[] HunterSpawns =
        {
            new Vector3(-64.8f, DistrictGenerator.Curb, -71.6f), new Vector3(-63.3f, DistrictGenerator.Curb, -71.9f),
            new Vector3(-64.2f, DistrictGenerator.Curb, -73.3f), new Vector3(-63f, DistrictGenerator.Curb, -74.2f),
        };
        static readonly bool[] HunterBats = { false, true, false, true };

        /// <summary>
        /// The truck that rammed the ambulance turns into the alley, its high beams sweeping the front windows; four
        /// Smilers get out and come in through the front. They are herding, not killing (shots go wide until Daniel hurts
        /// one). Through the back gate and up the back alley, they let him go.
        /// </summary>
        IEnumerator Raid()
        {
            raiding = true;
            var hud = BodycamHUD.Instance;
            var am = AudioManager.Instance;
            truck = VehicleFactory.RamTruck(transform);
            var drive = truck.gameObject.AddComponent<ScriptedVehicle>();
            drive.engineSound = SoundId.EngineTruck;
            drive.engineVolume = 0.85f;
            drive.idlePitch = 0.8f;
            drive.pitchPerSpeed = 0.05f;
            drive.SetPath(TruckRoute, 5f);
            drive.targetSpeed = 5f;
            truck.SetHeadlights(true);
            if (truck.headlights != null && truck.headlights.Length > 0 && truck.headlights[0] != null) truck.headlights[0].shadows = LightShadows.Soft;
            yield return new WaitForSeconds(2.5f);
            if (am != null) am.Play2D(SoundId.TensionHit, 0.55f);
            yield return Speak(DemoLines.RaidSeen);
            if (hud != null)
            {
                hud.SetObjective("Get out - the back door, the kitchen");
                hud.ShowHint("Downstairs, through the kitchen, out of the back door and the back gate.", 6f);
            }

            while (drive.Remaining > 0.4f && drive.Speed > 0.05f) yield return null;
            if (am != null) am.PlayAt(SoundId.DoorSlam, truck.transform.position + Vector3.up, 0.6f, 0.8f, 3f, 60f);
            var rng = new System.Random(808);
            for (int i = 0; i < HunterSpawns.Length; i++)
            {
                var b = NPCSpawner.Create(CharacterLook.Random(Faction.Smiler, rng), rng, HunterSpawns[i], 270f, HunterBats[i] ? NPCWeapon.Bat : NPCWeapon.Pistol, false, 0f);
                if (b == null) continue;
                b.hunting = true;
                hunters.Add(b);
            }
            var taunts = DemoLines.RaidTaunts;
            if (hunters.Count > 0) StartCoroutine(Speak(taunts[0].At(hunters[0].transform), taunts[1].At(hunters[hunters.Count - 1].transform)));

            // Out through the back gate and up the back alley: they let him go.
            while (!Escaped()) yield return null;
            NPCBrain nearest = null;
            float best = float.MaxValue;
            foreach (var h in hunters)
            {
                if (h == null || h.State == NPCState.Dead) continue;
                float d = Vector3.Distance(h.transform.position, player.transform.position);
                if (d < best) { best = d; nearest = h; }
            }
            if (nearest != null) StartCoroutine(Speak(DemoLines.RaidWithdraw.At(nearest.transform)));
            foreach (var h in hunters) if (h != null && h.State != NPCState.Dead) StartCoroutine(Withdraw(h));
            Done = true;
            raiding = false;
            if (hud != null) hud.SetObjective("The school - north up the back alley");
        }

        bool Escaped()
        {
            Vector3 p = player.transform.position;
            return p.x < HouseBuilder.LotX0 - 0.3f && p.x > AlleyBuilder.BackAlleyX0 - 1f && p.z > HouseBuilder.BackDoorZ + 4f;
        }

        /// <summary>A hunter walks back to the truck and is gone once nobody is looking.</summary>
        IEnumerator Withdraw(NPCBrain h)
        {
            h.hunting = false;
            h.enabled = false; // the brain lets go; the agent and animator keep working
            var agent = h.GetComponent<NavMeshAgent>();
            var anim = h.GetComponent<ProcAnimator>();
            var hp = h.GetComponent<Health>();
            anim.pose = ProcAnimator.Pose.Normal;
            anim.aimTarget = null;
            anim.lookTarget = null;
            agent.updateRotation = true;
            agent.speed = 1.6f;
            if (agent.isOnNavMesh) { agent.isStopped = false; agent.SetDestination(TruckRoute[TruckRoute.Length - 1] + new Vector3(1.4f, 0f, -2f)); }
            float unseen = 0f;
            while (h != null && !hp.IsDead)
            {
                var cam = Camera.main;
                Vector3 vp = cam != null ? cam.WorldToViewportPoint(h.transform.position + Vector3.up) : Vector3.zero;
                bool inView = vp.z > 0f && vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f;
                unseen = inView ? 0f : unseen + Time.deltaTime;
                if (unseen > 3f) { Destroy(h.gameObject); yield break; }
                yield return null;
            }
        }

        /// <summary>A checkpoint restart past the house: it has been searched and the Smilers have gone.</summary>
        public void RestoreAfterCheckpoint()
        {
            Done = true;
            phoneTaken = bedSeen = closetOpen = true;
            if (phone != null) Destroy(phone.gameObject);
            OpenBackGate(false);
        }

        /// <summary>One voice at a time in this scene.</summary>
        IEnumerator Speak(params Line[] lines)
        {
            while (talking) yield return null;
            talking = true;
            if (DialogueDirector.Instance != null) yield return DialogueDirector.Instance.Conversation(lines);
            talking = false;
        }
    }
}
