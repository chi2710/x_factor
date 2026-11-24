// Thanh Mana cho uGUI Image: năng lượng chảy bên trong, mép sáng ở đầu thanh,
// và phần "bị rút" (giữa _Fill và _DrainFill) chạy trễ phía sau.
// Sprite chỉ dùng làm mask (alpha), màu lấy từ Image.color nên sprite màu gì cũng được.
// Dựa trên UI/Default: giữ stencil/clip để chạy được với Mask và RectMask2D.
Shader "UI/ManaBar"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Fill ("Fill", Range(0, 1)) = 1
        _DrainFill ("Drain Fill", Range(0, 1)) = 1

        [HDR] _FlowColor ("Flow Color", Color) = (0.2, 0.9, 1.6, 1)
        _FlowScale ("Flow Scale (X, Y)", Vector) = (6, 1.5, 0, 0)
        _FlowSpeed ("Flow Speed", Float) = 0.8
        _FlowStrength ("Flow Strength", Range(0, 1)) = 0.6

        [HDR] _EdgeColor ("Edge Color", Color) = (1.5, 2.5, 2.5, 1)
        _EdgeWidth ("Edge Width", Range(0.001, 0.2)) = 0.04
        _EdgeFlicker ("Edge Flicker", Range(0, 1)) = 0.3

        [HDR] _DrainColor ("Drain Color", Color) = (1, 1, 1, 0.75)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _Fill;
            float _DrainFill;
            float4 _FlowColor;
            float4 _FlowScale;
            float _FlowSpeed;
            float _FlowStrength;
            float4 _EdgeColor;
            float _EdgeWidth;
            float _EdgeFlicker;
            float4 _DrainColor;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Value noise mượt, đủ cho dòng năng lượng mà không cần texture noise.
            float valueNoise(float2 p)
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

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float t = _Time.y;
                half mask = (tex2D(_MainTex, uv) + _TextureSampleAdd).a;

                // Khử răng cưa ở mép cắt bằng độ rộng 1 pixel theo UV.
                float aa = max(fwidth(uv.x), 1e-4);
                float filled = saturate((_Fill - uv.x) / aa + 0.5);
                float drained = saturate((_DrainFill - uv.x) / aa + 0.5) * (1 - filled);

                // Hai lớp noise trôi ngược chiều thanh để thấy năng lượng chảy.
                float2 p = uv * _FlowScale.xy;
                float n = valueNoise(p + float2(-t * _FlowSpeed, 0)) * 0.6
                        + valueNoise(p * 2.3 + float2(-t * _FlowSpeed * 1.7, t * 0.3)) * 0.4;
                float3 col = IN.color.rgb * (1 - _FlowStrength * 0.5) + _FlowColor.rgb * n * _FlowStrength;

                // Mép sáng ngay trong đầu thanh, nhấp nháy nhẹ.
                float edge = saturate(1 - (_Fill - uv.x) / _EdgeWidth) * filled;
                edge *= 1 - _EdgeFlicker * (0.5 + 0.5 * sin(t * 25 + uv.y * 12));
                col += _EdgeColor.rgb * edge;

                // Phần bị rút nhấp nháy mờ dần.
                float drainPulse = 0.75 + 0.25 * sin(t * 18);
                col = lerp(col, _DrainColor.rgb, drained);

                half alpha = mask * IN.color.a * (filled + drained * _DrainColor.a * drainPulse);
                half4 color = half4(col, alpha);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
