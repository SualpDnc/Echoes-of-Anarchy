using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Echoes.Rendering
{
    /// <summary>
    /// 8d: the big LED screen the Smilers hung on the school's facade. Before the show it loops a grin and "STARTING
    /// SOON"; when Vance picks Daniel out it switches to Daniel's own bodycam (a second camera on the player's camera
    /// renders into two textures in turn, so the screen never samples the one being drawn: looking at it gives the
    /// tunnel of screens a live feed should). A light in front washes the yard in the screen's colour.
    /// </summary>
    public class StreamScreen : MonoBehaviour
    {
        public float width = 6.4f, height = 3.6f;

        Material mat;
        Light wash;
        Texture idle;
        Camera feed;
        RenderTexture[] rts;
        int frame;
        bool live;
        static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

        public static StreamScreen Build(Transform parent, Vector3 centre, float yaw)
        {
            var go = new GameObject("StreamScreen");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(centre, Quaternion.Euler(0f, yaw, 0f));
            var s = go.AddComponent<StreamScreen>();
            s.Make();
            return s;
        }

        void Make()
        {
            var frameMat = RuntimeMaterials.Lit(new Color(0.03f, 0.03f, 0.035f), 0.4f, 0.5f);
            PrimitiveMeshes.Part(transform, "Back", PrimitiveType.Cube, new Vector3(0f, 0f, 0.12f), new Vector3(width + 0.3f, height + 0.3f, 0.2f), frameMat);
            // Truss legs down to a ledge, and the cables.
            foreach (float s in new[] { -1f, 1f })
                PrimitiveMeshes.Part(transform, "Truss", PrimitiveType.Cube, new Vector3(s * (width / 2f + 0.25f), -0.3f, 0.1f), new Vector3(0.12f, height + 0.9f, 0.12f), frameMat);

            idle = IdleTexture();
            mat = new Material(RuntimeMaterials.Lit(Color.black, 0.7f, 0f, Color.white)) { name = "RT_StreamScreen", hideFlags = HideFlags.DontSave };
            mat.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.02f));
            mat.SetColor("_EmissionColor", Color.white * 1.6f);
            mat.SetTexture(EmissionMap, idle);
            mat.SetTexture(BaseMap, idle);
            PrimitiveMeshes.Part(transform, "Panel", PrimitiveType.Quad, new Vector3(0f, 0f, 0f), new Vector3(width, height, 1f), mat, null, false); // a quad faces −Z: the yard

            var lg = new GameObject("ScreenWash", typeof(Light));
            lg.transform.SetParent(transform, false);
            lg.transform.localPosition = new Vector3(0f, -0.5f, -3f);
            wash = lg.GetComponent<Light>();
            wash.type = LightType.Point;
            wash.range = 16f;
            wash.intensity = 2.2f;
            wash.color = new Color(1f, 0.85f, 0.35f);
            wash.shadows = LightShadows.None;
        }

        /// <summary>A yellow LED grin on black, "STARTING SOON" under it.</summary>
        static Texture2D IdleTexture()
        {
            const int w = 320, h = 180;
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0.01f, 0.01f, 0.03f);
            var led = new Color(1f, 0.82f, 0.15f);
            void Dot(float cx, float cy, float r)
            {
                for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
                for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = Mathf.Clamp01(r + 0.5f - d);
                    px[y * w + x] = Color.Lerp(px[y * w + x], led, a);
                }
            }
            Dot(130f, 128f, 8f);
            Dot(190f, 128f, 8f);
            for (int k = 0; k <= 12; k++)
            {
                float u = k / 12f * 2f - 1f;
                Dot(160f + u * 62f, 95f - 26f * (1f - u * u), 4.5f);
            }
            var mask = PixelFont.Mask("STARTING SOON", false, 3, 7, out int tw, out int th);
            int ox = (w - tw) / 2, oy = 14;
            for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                int X = ox + x, Y = oy + y;
                if (X < 0 || Y < 0 || X >= w || Y >= h) continue;
                px[Y * w + X] = Color.Lerp(px[Y * w + X], new Color(0.95f, 0.95f, 0.9f), mask[y * tw + x]);
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false) { name = "StreamIdle", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>Daniel's bodycam on the screen (`source` = the player's camera).</summary>
        public void ShowFeed(Camera source)
        {
            if (source == null || live) return;
            live = true;
            rts = new RenderTexture[2];
            for (int i = 0; i < 2; i++) rts[i] = new RenderTexture(384, 216, 16) { name = "StreamFeed" + i, hideFlags = HideFlags.DontSave };
            var go = new GameObject("StreamFeedCam", typeof(Camera));
            go.transform.SetParent(source.transform, false);
            feed = go.GetComponent<Camera>();
            feed.fieldOfView = source.fieldOfView;
            feed.nearClipPlane = source.nearClipPlane;
            feed.farClipPlane = 220f;
            feed.cullingMask = source.cullingMask;
            feed.depth = source.depth - 1;
            feed.targetTexture = rts[0];
            var data = feed.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.requiresDepthTexture = false;
            wash.color = new Color(0.75f, 0.85f, 1f);
        }

        /// <summary>Back to the grin (a checkpoint restart before the reveal).</summary>
        public void ShowIdle()
        {
            live = false;
            if (feed != null) Destroy(feed.gameObject);
            mat.SetTexture(EmissionMap, idle);
            mat.SetTexture(BaseMap, idle);
            wash.color = new Color(1f, 0.85f, 0.35f);
        }

        void LateUpdate()
        {
            if (!live || feed == null) return;
            // Draw into one texture while the screen shows the other.
            int i = frame++ & 1;
            feed.targetTexture = rts[i];
            mat.SetTexture(EmissionMap, rts[1 - i]);
            mat.SetTexture(BaseMap, rts[1 - i]);
            // A live feed flickers a little.
            wash.intensity = 2f + Mathf.PerlinNoise(Time.time * 3f, 0.3f) * 0.8f;
        }

        void OnDestroy()
        {
            if (rts == null) return;
            foreach (var r in rts) if (r != null) r.Release();
        }
    }
}
