Shader "Ashveil/Terrain"
{
    Properties
    {
        _MainTex ("Sand", 2D) = "white" {}
        _Sand ("Sand", Color) = (0.55, 0.40, 0.24, 1)
        _Dirt ("Dirt", Color) = (0.34, 0.23, 0.13, 1)
        _Sage ("Sage", Color) = (0.27, 0.31, 0.14, 1)
        _Rock ("Rock", Color) = (0.29, 0.21, 0.15, 1)
        _Tiling ("Tiling", Float) = 0.09
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

            sampler2D _MainTex;
            float4 _Sand, _Dirt, _Sage, _Rock;
            float _Tiling;

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
                p = frac(p * float2(123.34, 345.56));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += noise(p) * a;
                    p = p * 2.03 + 17.1;
                    a *= 0.5;
                }
                return v;
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

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.worldN);
                float2 xz = i.worldPos.xz;

                float grain = tex2D(_MainTex, xz * _Tiling).r;
                float mott = fbm(xz * 0.011);
                float patch = fbm(xz * 0.0033 + 40.0);
                float specks = fbm(xz * 0.045);

                float slope = saturate(1.0 - n.y);
                float3 sand = lerp(_Dirt.rgb, _Sand.rgb, mott);
                sand *= lerp(0.78, 1.12, grain);
                sand = lerp(sand, _Rock.rgb, saturate(slope * 1.8 + (specks - 0.5) * 0.25));

                float veg = smoothstep(0.58, 0.82, patch) * (1.0 - slope) * smoothstep(0.35, 0.7, specks);
                float3 sage = _Sage.rgb * lerp(0.85, 1.05, grain);
                float3 albedo = lerp(sand, sage, veg * 0.72);

                float ndotl = saturate(dot(n, _WorldSpaceLightPos0.xyz));
                float atten = SHADOW_ATTENUATION(i);
                float3 ambient = ShadeSH9(float4(n, 1));
                float3 lighting = ambient + _LightColor0.rgb * ndotl * atten;
                float3 col = albedo * lighting;

                fixed4 c = fixed4(col, 1);
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
            struct v2f { V2F_SHADOW_CASTER; };
            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
