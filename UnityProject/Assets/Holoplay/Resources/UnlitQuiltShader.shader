Shader "Unlit/UnlitQuiltShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _QuiltVec("QuiltTiles", Vector) = (0, 0, 0, 0)
        _QuiltVec2("QuiltTiles2", Vector) = (0, 0, 0, 0)

    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _QuiltVec;
            float4 _QuiltVec2;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            float2 correctedUV(float2 inUV)
            {
                
                inUV.x = inUV.x / _QuiltVec.x + floor(_QuiltVec.z)/_QuiltVec.x ;
                inUV.y = inUV.y / _QuiltVec.y + floor(_QuiltVec.w)/_QuiltVec.y;
                return inUV;
            }
            
            float2 correctedUVPlus(float2 inUV)
            {
                
                inUV.x = inUV.x / _QuiltVec2.x + ceil(_QuiltVec2.z)/_QuiltVec2.x ;
                inUV.y = inUV.y / _QuiltVec2.y + ceil(_QuiltVec2.w)/_QuiltVec2.y;
                return inUV;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, correctedUV(i.uv));
                  fixed4 col2 = 0;
                /*for (int j = 0 ; j < 64  ; j++)
                {
                   col2 = max(col2,tex2D(_MainTex, clamp(correctedUV(i.uv),0,1)));

                }*/
                // apply fog
             //   UNITY_APPLY_FOG(i.fogCoord, col);
                //return max(col,col*col2);
                return col;
            }
            ENDCG
        }
    }
}