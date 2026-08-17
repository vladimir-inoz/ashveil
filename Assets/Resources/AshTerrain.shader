Shader "Ashveil/Terrain"
{
    Properties
    {
        _Sand ("Sand", Color) = (0.56, 0.41, 0.24, 1)
        _Dirt ("Dirt", Color) = (0.34, 0.23, 0.13, 1)
        _Rock ("Rock", Color) = (0.30, 0.21, 0.14, 1)
        _ClipRadius ("Clip Inner Radius", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            float4 _Sand, _Dirt, _Rock;
            float _ClipRadius;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldN : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                SHADOW_COORDS(3)
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + 1);
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                return vnoise(p) * 0.58 + vnoise(p * 2.09 + 17.2) * 0.29 + vnoise(p * 4.13 + 8.1) * 0.13;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldN = UnityObjectToWorldNormal(v.normal);
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            float bumpH(float2 xz, float wMid, float wNear)
            {
                float h = fbm(xz * 0.0041) * 1.8;
                h += vnoise(xz * 0.019 + 6.2) * 0.55 * wMid;
                h += vnoise(xz * 0.073 + 2.7) * 0.18 * wNear;
                return h;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_ClipRadius > 1.0)
                    clip(max(abs(i.worldPos.x), abs(i.worldPos.z)) - _ClipRadius);

                float3 meshN = normalize(i.worldN);
                float2 xz = i.worldPos.xz;
                float d = distance(_WorldSpaceCameraPos, i.worldPos);

                float wMid = saturate((2800.0 - d) / 2000.0);
                float wNear = saturate((900.0 - d) / 650.0);
                float wClose = saturate((220.0 - d) / 180.0);

                float n0 = vnoise(xz * 0.00085 + 4.1);
                float n1 = vnoise(xz * 0.0032 + 19.0);
                float3 col = lerp(_Dirt.rgb, _Sand.rgb, n0);
                col = lerp(col, _Dirt.rgb * 0.82, n1 * 0.45);

                float slope = saturate(1.0 - meshN.y);
                col = lerp(col, _Rock.rgb, saturate(slope * 1.35));

                if (wMid > 0.0)
                {
                    float n2 = vnoise(xz * 0.016 + 8.4);
                    float rip = sin(xz.x * 0.19 + xz.y * 0.06 + n2 * 5.5);
                    col *= 1.0 + ((n2 - 0.5) * 0.22 + rip * 0.07) * wMid;
                }
                if (wNear > 0.0)
                {
                    float n3 = vnoise(xz * 0.062 + 1.7);
                    float n4 = vnoise(xz * 0.17 + 11.0);
                    col *= 1.0 + ((n3 - 0.5) * 0.2 + (n4 - 0.5) * 0.12) * wNear;
                    float2 cell = floor(xz * 0.28);
                    float peb = hash21(cell);
                    col = lerp(col, _Rock.rgb, step(0.91, peb) * 0.4 * wNear);
                }
                if (wClose > 0.0)
                {
                    float g = vnoise(xz * 1.35 + 3.3);
                    col *= 1.0 + (g - 0.5) * 0.22 * wClose;
                }

                float e = lerp(3.0, 0.4, wNear);
                float3 n = meshN;
                if (wMid > 0.02)
                {
                    float h0 = bumpH(xz, wMid, wNear);
                    float3 procN = normalize(float3(
                        h0 - bumpH(xz + float2(e, 0), wMid, wNear),
                        e,
                        h0 - bumpH(xz + float2(0, e), wMid, wNear)));
                    n = normalize(lerp(meshN, procN, 0.55 + 0.35 * wNear));
                }

                float ndotl = saturate(dot(n, _WorldSpaceLightPos0.xyz));
                float atten = SHADOW_ATTENUATION(i);
                float3 ambient = ShadeSH9(float4(n, 1));
                float3 lighting = ambient + _LightColor0.rgb * ndotl * atten;

                fixed4 c = fixed4(col * lighting, 1);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            float _ClipRadius;
            struct v2f
            {
                V2F_SHADOW_CASTER;
                float3 worldPos : TEXCOORD1;
            };
            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                if (_ClipRadius > 1.0)
                    clip(max(abs(i.worldPos.x), abs(i.worldPos.z)) - _ClipRadius);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
