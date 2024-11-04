Shader "Neural/Wireframe"
{
    Properties
    {
        _WireframeColor ("Wireframe Color", Color) = (0, 0, 0, 1)
        _WireframeThickness ("Wireframe Thickness", Range(0, 1)) = 0.01
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float3 barycentricCoordinates : TEXCOORD0;
            };

            float3 _WireframeColor;
            float _WireframeThickness;

            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;
                o.barycentricCoordinates = float3(1, 0, 0);
                o.vertex = IN[0].vertex;
                triStream.Append(o);

                o.barycentricCoordinates = float3(0, 1, 0);
                o.vertex = IN[1].vertex;
                triStream.Append(o);

                o.barycentricCoordinates = float3(0, 0, 1);
                o.vertex = IN[2].vertex;
                triStream.Append(o);
            }

            fixed4 frag (g2f i) : SV_Target
            {
                float minBary = min(min(i.barycentricCoordinates.x, i.barycentricCoordinates.y), i.barycentricCoordinates.z);
                float delta = fwidth(minBary);
                float wireframe = smoothstep(_WireframeThickness - delta, _WireframeThickness + delta, minBary);
                return fixed4(_WireframeColor, 1 - wireframe);
            }
            ENDCG
        }
    }
}