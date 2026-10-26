using UnityEngine;
using Echoes.Rendering;

namespace Echoes.Player
{
    /// <summary>
    /// Shoulder-clipped tactical light. Trails the camera slightly (it's strapped to the vest, not the eyes)
    /// and uses a procedural cookie for a realistic hot-spot + reflector ring pattern.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class Flashlight : MonoBehaviour
    {
        public CameraRig rig;
        public Vector3 offset = new Vector3(0.16f, -0.12f, 0.1f);
        public float followSharpness = 14f;
        public float intensity = 5.5f;
        public bool startOn = true;

        Light lamp;
        bool on;
        float flick;
        EchoesInput input;

        void Awake()
        {
            lamp = GetComponent<Light>();
            lamp.type = LightType.Spot;
            lamp.range = 28f;
            lamp.spotAngle = 62f;
            lamp.innerSpotAngle = 18f;
            lamp.color = new Color(1f, 0.95f, 0.88f);
            lamp.shadows = LightShadows.Soft;
            lamp.shadowNearPlane = 0.2f;
            lamp.shadowBias = 0.02f;
            lamp.cookie = ProcTextures.FlashlightCookie(256);
            on = startOn;
            lamp.enabled = on;
            lamp.intensity = intensity;
            if (rig != null) input = rig.GetComponent<EchoesInput>();
        }

        void LateUpdate()
        {
            if (rig == null) return;
            if (input != null && input.FlashlightPressed)
            {
                on = !on;
                lamp.enabled = on;
            }

            Transform c = rig.cam.transform;
            Transform p = rig.pivot;
            transform.position = p.position + p.rotation * offset;
            float k = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, c.rotation, k);

            // Tiny current fluctuation — barely visible, but reads as a real battery LED.
            flick = Mathf.Lerp(flick, Random.Range(-1f, 1f), 0.2f);
            lamp.intensity = intensity * (1f + flick * 0.015f);
        }

        public bool IsOn => on;
    }
}
