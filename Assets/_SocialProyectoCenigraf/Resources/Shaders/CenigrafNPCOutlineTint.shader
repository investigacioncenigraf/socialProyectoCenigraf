Shader "Cenigraf/NPC Layer Outline Tint"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor("Renderer Color", Color) = (1, 1, 1, 1)
        [PerRendererData] _OutlineColor("Outline Color", Color) = (0.2509804, 0.21568628, 1, 1)
        [PerRendererData] _OutlineSourceColor("Source Color To Replace", Color) = (0.0451862, 0.0451862, 0.0451862, 1)
        [PerRendererData] _OutlineColorTolerance("Source Color Tolerance", Range(0, 0.1)) = 0.015
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

            #pragma vertex OutlineLitVertex
            #pragma fragment OutlineLitFragment
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
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                half4 _OutlineSourceColor;
                half _OutlineColorTolerance;
            CBUFFER_END

            half DetectOutlinePixel(half3 sourceColor)
            {
                half3 difference = abs(sourceColor - _OutlineSourceColor.rgb);
                half largestDifference = max(difference.r, max(difference.g, difference.b));
                return 1.0h - step(_OutlineColorTolerance, largestDifference);
            }

            Varyings OutlineLitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonLitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 OutlineLitFragment(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half outlineMask = DetectOutlinePixel(source.rgb);
                half3 tintedFill = source.rgb * input.color.rgb;
                half3 result = lerp(tintedFill, _OutlineColor.rgb, outlineMask);
                half alpha = source.a * input.color.a * lerp(1.0h, _OutlineColor.a, outlineMask);

                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(result, alpha, mask, normalTS, surfaceData);
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

            #pragma vertex OutlineUnlitVertex
            #pragma fragment OutlineUnlitFragment
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
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                half4 _OutlineSourceColor;
                half _OutlineColorTolerance;
            CBUFFER_END

            half DetectOutlinePixel(half3 sourceColor)
            {
                half3 difference = abs(sourceColor - _OutlineSourceColor.rgb);
                half largestDifference = max(difference.r, max(difference.g, difference.b));
                return 1.0h - step(_OutlineColorTolerance, largestDifference);
            }

            Varyings OutlineUnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 OutlineUnlitFragment(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half outlineMask = DetectOutlinePixel(source.rgb);
                half3 tintedFill = source.rgb * input.color.rgb;
                half3 result = lerp(tintedFill, _OutlineColor.rgb, outlineMask);
                half alpha = source.a * input.color.a * lerp(1.0h, _OutlineColor.a, outlineMask);
                return half4(result, alpha);
            }
            ENDHLSL
        }
    }
}
