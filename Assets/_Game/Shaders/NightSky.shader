// Purge-night sky: light-polluted horizon, city-lit overcast drifting overhead, stars and a cratered moon.
Shader "Echoes/NightSky"
{
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.008, 0.011, 0.022, 1)
        _HorizonColor ("Horizon", Color) = (0.05, 0.056, 0.075, 1)
        _GlowColor ("City Glow", Color) = (0.22, 0.12, 0.06, 1)
        _CloudLitColor ("Cloud Underlight", Color) = (0.16, 0.1, 0.07, 1)
        _CloudDarkColor ("Cloud Shadow", Color) = (0.025, 0.027, 0.035, 1)
        _CloudCover ("Cloud Cover", Range(0, 1)) = 0.55
        _CloudSpeed ("Cloud Speed", Float) = 0.004
        _StarDensity ("Star Density", Range(0.99, 0.9999)) = 0.9975
        _StarBrightness ("Star Brightness", Float) = 1.6
        _MoonDirection ("Moon Direction (to moon)", Vector) = (-0.4, 0.55, 0.7, 0)
        _MoonColor ("Moon Color", Color) = (0.9, 0.93, 1.0, 1)
        _MoonSize ("Moon Size", Range(0.995, 0.99995)) = 0.99965
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ZenithColor, _HorizonColor, _GlowColor, _CloudLitColor, _CloudDarkColor;
                float _CloudCover, _CloudSpeed, _StarDensity, _StarBrightness;
                float4 _MoonDirection, _MoonColor;
                float _MoonSize;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            float hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float vnoise(float3 p)
            {
                float3 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = hash13(i), n100 = hash13(i + float3(1,0,0));
                float n010 = hash13(i + float3(0,1,0)), n110 = hash13(i + float3(1,1,0));
                float n001 = hash13(i + float3(0,0,1)), n101 = hash13(i + float3(1,0,1));
                float n011 = hash13(i + float3(0,1,1)), n111 = hash13(i + float3(1,1,1));
                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            float fbm(float3 p)
            {
                float s = 0, a = 0.5;
                [unroll] for (int i = 0; i < 5; i++) { s += vnoise(p) * a; p = p * 2.03 + 17.1; a *= 0.5; }
                return s;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                float up = saturate(h);

                // Base gradient + sodium light pollution hugging the horizon.
                float3 col = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(up, 0.4));
                col += _GlowColor.rgb * exp(-up * 7.0) * 0.9;

                // Stars: 3D cell hash on the direction; dimmed near the polluted horizon.
                float3 sp = d * 380.0;
                float3 cell = floor(sp);
                float star = hash13(cell);
                float3 cpos = cell + 0.5 + (float3(hash13(cell + 1.3), hash13(cell + 2.7), hash13(cell + 4.1)) - 0.5) * 0.6;
                float sd = length(sp - cpos);
                float twinkle = 0.7 + 0.3 * sin(_Time.y * (2.0 + star * 6.0) + star * 100.0);
                float starMask = step(_StarDensity, star) * smoothstep(0.35, 0.0, sd) * twinkle * smoothstep(0.05, 0.4, h);
                col += starMask * _StarBrightness * lerp(float3(1, 0.85, 0.7), float3(0.75, 0.85, 1), hash13(cell + 9.0));

                // Moon disc with crater mottling + halo.
                float3 md = normalize(_MoonDirection.xyz);
                float mdot = dot(d, md);
                float disc = smoothstep(_MoonSize, _MoonSize + 0.00008, mdot);
                float craters = 0.75 + 0.25 * fbm(d * 900.0);
                col = lerp(col, _MoonColor.rgb * 2.2 * craters, disc);
                col += _MoonColor.rgb * (pow(saturate(mdot), 600.0) * 0.35 + pow(saturate(mdot), 30.0) * 0.04);

                // Clouds: project onto a dome, drift; lit orange from the city below, darker toward zenith.
                float2 cuv = d.xz / (h + 0.18);
                float3 cp = float3(cuv * 1.6 + _Time.y * _CloudSpeed * float2(1.0, 0.4), _Time.y * _CloudSpeed * 0.5);
                float c = fbm(cp);
                float cov = smoothstep(1.0 - _CloudCover, 1.0 - _CloudCover + 0.35, c);
                float3 cloudCol = lerp(_CloudLitColor.rgb, _CloudDarkColor.rgb, saturate(h * 1.6));
                cloudCol += _MoonColor.rgb * pow(saturate(mdot), 8.0) * 0.08 * (1.0 - c); // moon-lit edges
                float cloudFade = smoothstep(-0.02, 0.12, h);
                col = lerp(col, cloudCol, cov * cloudFade * 0.92);

                // Below the horizon blends into fog colour so geometry gaps never show a seam.
                col = lerp(_HorizonColor.rgb, col, smoothstep(-0.08, 0.02, h));
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
