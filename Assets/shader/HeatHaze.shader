Shader "Unlit/HeatHaze"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DistortionMap ("Distortion Map (R=x, G=y)", 2D) = "gray" {}
        _Strength ("Distortion Strength", Range(0, 0.1)) = 0.01
        _Speed ("Animation Speed", Range(0, 5)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // 화면 뒤의 이미지를 가져오는 GrabPass
        GrabPass { "_BackgroundTexture" }

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

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
                float4 grabPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _DistortionMap;
            float4 _DistortionMap_ST;
            sampler2D _BackgroundTexture;
            float _Strength;
            float _Speed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // DistortionMap의 UV를 시간에 따라 스크롤합니다.
                float2 distortedUV = i.uv;
                distortedUV.x += _Time.y * _Speed;
                distortedUV.y += _Time.y * _Speed * 0.5; // x와 y를 다른 속도로 움직여 더 자연스럽게

                // 노이즈 텍스처를 샘플링하여 왜곡 방향과 강도를 얻습니다. (0~1 범위)
                float4 distortion = tex2D(_DistortionMap, distortedUV);

                // 왜곡 방향을 -1 ~ 1 범위로 변환합니다.
                float2 offset = (distortion.rg - 0.5) * 2.0;

                // grabPos에 왜곡을 적용합니다.
                i.grabPos.x += offset.x * _Strength;
                i.grabPos.y += offset.y * _Strength;

                // 왜곡된 좌표로 화면 뒤 텍스처를 샘플링합니다.
                fixed4 col = tex2Dproj(_BackgroundTexture, i.grabPos);
                
                // 이 오브젝트 자체의 텍스처(MainTex)의 알파값을 적용하여 반투명하게 만듭니다.
                col.a = tex2D(_MainTex, i.uv).a;

                return col;
            }
            ENDCG
        }
    }
}
