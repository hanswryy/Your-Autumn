// Radial-blur battle hand-off.
//
// Plays on a frozen capture of the zoomed overworld frame AFTER the battle
// scene has finished loading. Rendered on a Screen Space Overlay canvas via a
// RawImage (the capture is bound as _MainTex), so it works the same under URP /
// built-in without a custom Renderer Feature. Driven by BattleTransitionController:
//   _Progress  0..1  ramps the blur amount up
//   _Center    xy     viewport point everything blurs toward (the enemy)
Shader "Hidden/BattleRadialBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0,1)) = 0
        _Center ("Center (xy)", Vector) = (0.5, 0.5, 0, 0)
        _BlurStrength ("Radial Blur Strength", Float) = 0.4

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Progress, _BlurStrength;
            float4 _Center;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float p = saturate(_Progress);
                float2 center = _Center.xy;

                // Radial blur: average several samples taken along the line from
                // this pixel toward the focus point. The sample spread grows with
                // progress, so the image smears harder as it rushes inward.
                float2 toC = center - i.uv;
                float3 col = 0;
                const int N = 12;
                [unroll]
                for (int s = 0; s < N; s++)
                {
                    float t = (s / (float)(N - 1)) * p * _BlurStrength;
                    col += tex2D(_MainTex, i.uv + toC * t).rgb;
                }
                col /= N;

                return fixed4(col, i.color.a);
            }
            ENDCG
        }
    }
}
