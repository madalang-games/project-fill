Shader "UI/GlowFlow"
{
    Properties
    {
        [PerRendererData][HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0.91, 0.63, 0.125, 1)
        _FlowSpeed ("Flow Speed", Float) = 0.25
        _PulseSpeed ("Pulse Speed", Float) = 1.2
        _RingRadius ("Ring Radius", Range(0.05, 0.5)) = 0.40
        _RingWidth ("Ring Width", Range(0.01, 0.25)) = 0.08
        _NoiseScale ("Noise Scale", Float) = 5.0
        _Intensity ("Intensity", Range(0, 1)) = 0.70

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil ReadMask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma shader_feature_local_fragment _ UNITY_UI_ALPHACLIP

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

            fixed4 _GlowColor;
            float  _FlowSpeed;
            float  _PulseSpeed;
            float  _RingRadius;
            float  _RingWidth;
            float  _NoiseScale;
            float  _Intensity;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.texcoord = v.texcoord;
                o.color = v.color * _GlowColor;
                return o;
            }

            float2 _hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453);
            }

            float gradNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float n = lerp(
                    lerp(dot(_hash22(i + float2(0,0)), f - float2(0,0)),
                         dot(_hash22(i + float2(1,0)), f - float2(1,0)), u.x),
                    lerp(dot(_hash22(i + float2(0,1)), f - float2(0,1)),
                         dot(_hash22(i + float2(1,1)), f - float2(1,1)), u.x), u.y);
                return n * 0.5 + 0.5;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord - 0.5;
                float dist = length(uv);

                float a = _Time.y * _FlowSpeed;
                float ca = cos(a), sa = sin(a);
                float2 rotUV = float2(uv.x * ca - uv.y * sa, uv.x * sa + uv.y * ca);

                float theta = atan2(rotUV.y, rotUV.x) * (1.0 / 6.28318);
                float2 noiseUV = float2(theta * _NoiseScale + _Time.y * _FlowSpeed * 0.4,
                                        dist  * _NoiseScale * 1.8);
                float n = gradNoise(noiseUV);

                float dDist = dist + (n - 0.5) * 0.05;
                float ringD = abs(dDist - _RingRadius);
                float ring  = 1.0 - smoothstep(0.0, _RingWidth, ringD);

                float halo  = max(0.0, 1.0 - ringD / (_RingWidth * 2.5));
                halo = halo * halo * 0.4;

                float noiseOnRing = n * ring * 0.25;

                float pulse = 0.72 + 0.28 * sin(_Time.y * _PulseSpeed);

                float alpha = saturate((ring + halo + noiseOnRing) * pulse * _Intensity);
                fixed4 col = fixed4(IN.color.rgb, IN.color.a * alpha);

                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
        ENDCG
        }
    }
}
