Shader "DeskSlayer/EnemyDissolve"
{
    // 敵人死亡溶解特效。以 URP 內建 Sprite-Lit-Default 為基礎（保留原本 2D 光照三個 Pass 的行為），
    // 額外加入程序式雜訊（Value Noise，非貼圖取樣）驅動的溶解裁切與邊緣高亮，
    // _DissolveAmount 由 EnemyDeathVisualDispatcher 透過 MaterialPropertyBlock 驅動，0~1 由 DOTween 補間。
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [Header(Dissolve)]
        _DissolveAmount("Dissolve Amount", Range(0, 1)) = 0
        _NoiseScale("Noise Scale", Float) = 8
        _EdgeWidth("Edge Width", Range(0.001, 0.3)) = 0.08
        _EdgeColor("Edge Color", Color) = (0.6, 1, 1, 1)

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color        : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _DissolveAmount;
                float _NoiseScale;
                float _EdgeWidth;
                half4 _EdgeColor;
            CBUFFER_END

            float DissolveHash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float DissolveValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = DissolveHash21(i);
                float b = DissolveHash21(i + float2(1, 0));
                float c = DissolveHash21(i + float2(0, 1));
                float d = DissolveHash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                float noise = DissolveValueNoise(input.uv * _NoiseScale);
                float dissolveEdge = noise - _DissolveAmount;
                clip(dissolveEdge);

                half4 main = CommonLitFragment(input, input.color);
                float edgeBand = 1.0 - smoothstep(0.0, _EdgeWidth, dissolveEdge);
                main.rgb = lerp(main.rgb, _EdgeColor.rgb, edgeBand * _EdgeColor.a);
                return main;
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4   color           : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                float _DissolveAmount;
                float _NoiseScale;
                float _EdgeWidth;
                half4 _EdgeColor;
            CBUFFER_END

            float DissolveHash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float DissolveValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = DissolveHash21(i);
                float b = DissolveHash21(i + float2(1, 0));
                float c = DissolveHash21(i + float2(0, 1));
                float d = DissolveHash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                float noise = DissolveValueNoise(input.uv * _NoiseScale);
                clip(noise - _DissolveAmount);

                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _DissolveAmount;
                float _NoiseScale;
                float _EdgeWidth;
                half4 _EdgeColor;
            CBUFFER_END

            float DissolveHash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float DissolveValueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = DissolveHash21(i);
                float b = DissolveHash21(i + float2(1, 0));
                float c = DissolveHash21(i + float2(0, 1));
                float d = DissolveHash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color *_Color * unity_SpriteColor;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                float noise = DissolveValueNoise(input.uv * _NoiseScale);
                float dissolveEdge = noise - _DissolveAmount;
                clip(dissolveEdge);

                half4 main = CommonUnlitFragment(input, input.color);
                float edgeBand = 1.0 - smoothstep(0.0, _EdgeWidth, dissolveEdge);
                main.rgb = lerp(main.rgb, _EdgeColor.rgb, edgeBand * _EdgeColor.a);
                return main;
            }
            ENDHLSL
        }
    }
}
