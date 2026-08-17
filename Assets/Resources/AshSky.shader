Shader "Ashveil/Sky"
{
    Properties
    {
        _SkyTop ("Sky Top", Color) = (0.55, 0.62, 0.85, 1)
        _SkyHorizon ("Horizon", Color) = (0.92, 0.62, 0.32, 1)
        _Ground ("Ground", Color) = (0.42, 0.28, 0.14, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _SkyTop;
            float4 _SkyHorizon;
            float4 _Ground;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(float4 vertex : POSITION)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(vertex);
                o.dir = mul((float3x3)unity_ObjectToWorld, vertex.xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                if (h > 0.0)
                    return lerp(_SkyHorizon, _SkyTop, saturate(h * 1.4));
                return lerp(_SkyHorizon, _Ground, saturate(-h * 1.6));
            }
            ENDCG
        }
    }
}
