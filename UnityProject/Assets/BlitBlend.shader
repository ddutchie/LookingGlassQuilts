Shader "Hidden/BlitBlend"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LastTexture ("Texture", 2D) = "white" {}
        _Blend ("Texture", float) = 0.5
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _LastTexture;
            float _Blend;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float2 flippeduv = i.uv;
                flippeduv.y=1-flippeduv.y;
                fixed4 colPRev = tex2D(_LastTexture, flippeduv);
                // just invert the colors
               // col.rgb = 1 - col.rgb;
                return lerp(col, colPRev, _Blend);
            }
            ENDCG
        }
               
        
    }
}
