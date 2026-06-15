Shader "UI/PulseGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil ReadMask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        
        _GlowColor ("Glow Color", Color) = (1, 0.85, 0.3, 1)
        _PulseSpeed ("Pulse Speed", Float) = 3.0
        _GlowScale ("Glow Scale", Float) = 0.1
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

            #pragma shader_feature_local_fragment _ _UseUIAlphaClip

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            fixed4 _GlowColor;
            float _PulseSpeed;
            float _GlowScale;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);

                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                o.color = v.color * _Color;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                // Procedural pulse glow overlay based on distance from center
                float2 dist = IN.texcoord - float2(0.5, 0.5);
                float radius = length(dist);
                
                // Pulsing animation
                float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                float glow = smoothstep(0.5, 0.3, radius) * (1.0 - smoothstep(0.5, 0.45 + _GlowScale * pulse, radius));
                
                fixed4 finalColor = color;
                finalColor.rgb += _GlowColor.rgb * glow * _GlowColor.a * pulse;
                finalColor.a = max(color.a, glow * pulse * _GlowColor.a);

                // Apply UI Rect Mask clipping
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                return finalColor;
            }
        ENDCG
        }
    }
}
