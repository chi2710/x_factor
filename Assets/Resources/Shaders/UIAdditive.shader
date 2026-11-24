// Hiện RenderTexture của VFX lên UI bằng cách cộng màu (additive).
// RenderTexture xóa nền về đen trong suốt, và shader particle cũ chỉ ghi RGB (alpha luôn 0),
// nên không thể blend theo alpha như UI/Default: cộng màu thì nền đen không hiện, chỗ sáng thì phát sáng.
Shader "UI/Additive"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Alpha của Graphic / CanvasGroup làm mờ phần cộng thêm.
                fixed3 c = tex2D(_MainTex, i.uv).rgb * i.color.rgb * i.color.a;
                return fixed4(c, 0);
            }
            ENDCG
        }
    }
}
