using UnityEngine;
using Echoes.Rendering;

namespace Echoes.World
{
    /// <summary>
    /// The parts of a generated vehicle that scripts drive: the sprung body (sways on its suspension), wheels,
    /// seats, lights and the pieces that change in a crash. Filled in by VehicleFactory.
    /// </summary>
    public class VehicleRig : MonoBehaviour
    {
        [Header("Body")]
        [Tooltip("Child holding everything above the axles; tilts with acceleration and cornering.")]
        public Transform body;
        public Transform[] wheels;
        public float wheelRadius = 0.4f;

        [Header("Seats (ambulance)")]
        [Tooltip("Daniel's body root in the passenger seat; +Z looks out of the windscreen.")]
        public Transform passengerMount;
        [Tooltip("Where the driver's pelvis sits.")]
        public Transform driverSeat;
        [Tooltip("Turns with the bends (ScriptedVehicle); the driver's hands hold gripL/gripR at ten and two.")]
        public Transform steeringWheel, gripL, gripR;

        [Header("Lights")]
        public Light[] headlights;
        public Renderer[] headlightLenses;
        public Color headlightEmission = new Color(1f, 0.95f, 0.85f) * 6f;
        public LightBar lightBar;
        public Light interiorLight;

        [Header("Crash")]
        public GameObject windshield;
        public Transform rearDoorL, rearDoorR;
        public Light compartmentLight;

        MaterialPropertyBlock mpb;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public void SetHeadlights(bool on, float level = 1f)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            if (headlights != null)
                foreach (var l in headlights)
                    if (l != null) l.enabled = on;
            if (headlightLenses == null) return;
            foreach (var r in headlightLenses)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(EmissionId, on ? headlightEmission * level : Color.black);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
