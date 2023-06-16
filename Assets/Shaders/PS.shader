Shader "OPT/PS"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            
            uniform float4 _MainTex_TexelSize;

            sampler2D _MainTex;
            sampler2D _SubTex;

            half4 _MainTex_ST;

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

            float gaussianKernel[9] = 
            {
                0.075f, 0.124f, 0.075f,
                0.124f, 0.204f, 0.124f,
                0.075f, 0.124f, 0.075f
            };

            fixed4 frag(v2f i) : SV_Target
            {
                // HMD 사용 안할 때 
                //fixed4 col = tex2D(_MainTex, i.uv);
                //fixed4 opt = tex2D(_SubTex,i.uv);

                fixed4 col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
                fixed4 opt = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));

                //float2 offset = 1.0f / _MainTex_TexelSize.xy;
                //fixed4 opt1 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
                //fixed4 opt2 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(offset.x, 0.0f), _MainTex_ST));
                //fixed4 opt3 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(0.0f, offset.y), _MainTex_ST));
                //fixed4 opt4 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + offset, _MainTex_ST));
                //opt = (opt1 + opt2 + opt3 + opt4) / 4.0;
                
                // 렌더링된 화면과, 옵티컬플로우 텍스처 색상 값을 블렌딩 한다.
                if (dot(opt.rgb, opt.rgb) > 0.25f)
                {
                    col.rgb = opt;// fixed4(1.0f, 1.0f, 1.0f, 1.0f);

                    //col.rgb = fixed4(1.0f, 0f, 0f, 1.0f);
                }

                return col;
                
                /*
                if(0.01f > dot(opt,opt))
                {
                    return col;
                }


                opt = fixed4(0.0f, 0.0f, 0.0f, 1.0f);
                float2 offset = 1.0f / _MainTex_TexelSize.xy;
                
                fixed4 opt1 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(-1.0f, -1.0f) * offset, _MainTex_ST)) *  0.075f;
                fixed4 opt2 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(-1.0f, 0.0f) * offset, _MainTex_ST)) *  0.124f;
                fixed4 opt3 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(-1.0f, 1.0f) * offset, _MainTex_ST)) *  0.075f;
                fixed4 opt4 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(0.0f, -1.0f) * offset, _MainTex_ST)) *  0.124f;
                fixed4 opt5 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST)) * 0.204f;
                fixed4 opt6 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(0.0f, 1.0f) * offset, _MainTex_ST)) *  0.124f;
                fixed4 opt7 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(1.0f, -1.0f) * offset, _MainTex_ST)) *  0.075f;
                fixed4 opt8 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(1.0f, 0.0f) * offset, _MainTex_ST)) * 0.124f;
                fixed4 opt9 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(1.0f, 1.0f) * offset, _MainTex_ST)) * 0.075;

                opt = opt1 + opt2 + opt3 + opt4 + opt5 + opt6 + opt7 + opt8 + opt9;

                //[loop]
                //for (float x = -1; x <= 1; x++)
                //{
                //    for (float y = -1; y <= 1; y++)
                //    {
                //        int idx = (x+1) + 3 * (y+1);
                //        float coef = gaussianKernel[idx];
                //        float2 uv = i.uv + (offset * float2(x,y));
                //        opt += tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST)) * coef;
                //    }
                //}


                //col += opt;
                return opt;
                */
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            uniform float4 _MainTex_TexelSize;

            sampler2D _MainTex;
            sampler2D _SubTex;

            half4 _MainTex_ST;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float gaussianKernel[9] =
            {
                0.075f, 0.124f, 0.075f,
                0.124f, 0.204f, 0.124f,
                0.075f, 0.124f, 0.075f
            };

            fixed4 frag(v2f i) : SV_Target
            {
                // HMD 사용 안할 때 
                //fixed4 col = tex2D(_MainTex, i.uv);
                //fixed4 opt = tex2D(_SubTex,i.uv);

                fixed4 col = tex2D(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
                fixed4 opt = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));

                //float2 offset = 1.0f / _MainTex_TexelSize.xy;
                //fixed4 opt1 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));
                //fixed4 opt2 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(offset.x, 0.0f), _MainTex_ST));
                //fixed4 opt3 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(0.0f, offset.y), _MainTex_ST));
                //fixed4 opt4 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + offset, _MainTex_ST));
                //opt = (opt1 + opt2 + opt3 + opt4) / 4.0;

                // 렌더링된 화면과, 옵티컬플로우 텍스처 색상 값을 블렌딩 한다.
                if (dot(opt.rgb, opt.rgb) > 0.25f)
                {
                    col.rgb = opt;// fixed4(1.0f, 1.0f, 1.0f, 1.0f);

                    //col.rgb = fixed4(1.0f, 0f, 0f, 1.0f);
                }

                return col;

                /*
                if(0.01f > dot(opt,opt))
                {
                    return col;
                }


                opt = fixed4(0.0f, 0.0f, 0.0f, 1.0f);
                float2 offset = 1.0f / _MainTex_TexelSize.xy;

                fixed4 opt1 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(-1.0f, -1.0f) * offset, _MainTex_ST)) *  0.075f;
                fixed4 opt2 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(-1.0f, 0.0f) * offset, _MainTex_ST)) *  0.124f;
                fixed4 opt3 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(-1.0f, 1.0f) * offset, _MainTex_ST)) *  0.075f;
                fixed4 opt4 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(0.0f, -1.0f) * offset, _MainTex_ST)) *  0.124f;
                fixed4 opt5 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST)) * 0.204f;
                fixed4 opt6 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(0.0f, 1.0f) * offset, _MainTex_ST)) *  0.124f;
                fixed4 opt7 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(1.0f, -1.0f) * offset, _MainTex_ST)) *  0.075f;
                fixed4 opt8 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(1.0f, 0.0f) * offset, _MainTex_ST)) * 0.124f;
                fixed4 opt9 = tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(i.uv + float2(1.0f, 1.0f) * offset, _MainTex_ST)) * 0.075;

                opt = opt1 + opt2 + opt3 + opt4 + opt5 + opt6 + opt7 + opt8 + opt9;

                //[loop]
                //for (float x = -1; x <= 1; x++)
                //{
                //    for (float y = -1; y <= 1; y++)
                //    {
                //        int idx = (x+1) + 3 * (y+1);
                //        float coef = gaussianKernel[idx];
                //        float2 uv = i.uv + (offset * float2(x,y));
                //        opt += tex2D(_SubTex, UnityStereoScreenSpaceUVAdjust(uv, _MainTex_ST)) * coef;
                //    }
                //}


                //col += opt;
                return opt;
                */
            }
            ENDCG
        }
    }
}
