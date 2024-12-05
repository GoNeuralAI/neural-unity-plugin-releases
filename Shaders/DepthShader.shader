Shader "Neural/DepthGrayscale" 
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        Pass
        {
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD0;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float dist = distance(_WorldSpaceCameraPos, i.worldPos.xyz);
                float normalizedDepth = saturate(dist / 10.0); // Adjust the 10.0 to change depth range
                float grayscale = 1 - normalizedDepth;
                return float4(grayscale.xxx, 1);  // Use .xxx to copy the value to all RGB channels
            }
            ENDCG
        }
    }
}