Shader "Cenigraf/Selective Head Tint"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor("Renderer Color", Color) = (1, 1, 1, 1)
        [PerRendererData] _FaceColor("Face Tint", Color) = (1, 1, 1, 1)
        _TintMinimum("Tint Minimum Luminance", Range(0, 1)) = 0.35
        _TintMaximum("Tint Maximum Luminance", Range(0, 1)) = 0.88
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
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

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]
        ColorMask [_ColorMask]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex SelectiveLitVertex
            #pragma fragment SelectiveLitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _FaceColor;
                half _TintMinimum;
                half _TintMaximum;
            CBUFFER_END

            half4 ApplySelectiveTint(
                half4 source,
                half4 headTint,
                half4 faceTint)
            {
                half luminance = dot(source.rgb, half3(0.299h, 0.587h, 0.114h));
                half headMask = step(_TintMinimum, luminance) *
                    (1.0h - step(_TintMaximum, luminance));
                half faceMask = step(_TintMaximum, luminance);
                half3 result = lerp(
                    source.rgb,
                    source.rgb * headTint.rgb,
                    headMask);
                result = lerp(
                    result,
                    source.rgb * faceTint.rgb,
                    faceMask);
                half alphaTint = lerp(1.0h, headTint.a, headMask);
                alphaTint = lerp(alphaTint, faceTint.a, faceMask);
                return half4(result, source.a * alphaTint);
            }

            Varyings SelectiveLitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(
                    input.positionOS,
                    unity_SpriteProps.xy);

                Varyings output = CommonLitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 SelectiveLitFragment(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    input.uv);
                half4 main = ApplySelectiveTint(
                    source,
                    input.color,
                    _FaceColor);
                half4 mask = SAMPLE_TEXTURE2D(
                    _MaskTex,
                    sampler_MaskTex,
                    input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(
                    _NormalMap,
                    sampler_NormalMap,
                    input.uv));

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(
                    main.rgb,
                    main.a,
                    mask,
                    normalTS,
                    surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex SelectiveUnlitVertex
            #pragma fragment SelectiveUnlitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

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

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _FaceColor;
                half _TintMinimum;
                half _TintMaximum;
            CBUFFER_END

            Varyings SelectiveUnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(
                    input.positionOS,
                    unity_SpriteProps.xy);

                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 SelectiveUnlitFragment(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    input.uv);
                half luminance = dot(
                    source.rgb,
                    half3(0.299h, 0.587h, 0.114h));
                half headMask = step(_TintMinimum, luminance) *
                    (1.0h - step(_TintMaximum, luminance));
                half faceMask = step(_TintMaximum, luminance);
                half3 result = lerp(
                    source.rgb,
                    source.rgb * input.color.rgb,
                    headMask);
                result = lerp(
                    result,
                    source.rgb * _FaceColor.rgb,
                    faceMask);
                half alphaTint = lerp(1.0h, input.color.a, headMask);
                alphaTint = lerp(alphaTint, _FaceColor.a, faceMask);
                return half4(result, source.a * alphaTint);
            }
            ENDHLSL
        }
    }
}
