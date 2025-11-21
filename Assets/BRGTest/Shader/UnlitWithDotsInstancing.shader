Shader "Universal Render Pipeline/Custom/UnlitWithDotsInstancing"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Colour", Color) = (1, 1, 1, 1)
        _UVTransform ("UV Transform", Vector) = (1, 1, 0, 0)
        _Flip ("Flip", Vector) = (1, 1, 0, 0)
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
            "DisableBatching"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Normal Render"

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma enable_d3d11_debug_symbols
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _UVTransform;
                float4 _Flip;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
                    UNITY_DOTS_INSTANCED_PROP(float4, _UVTransform)
                    UNITY_DOTS_INSTANCED_PROP(float4, _Flip)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
                #define _BaseColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
                #define _UVTransform UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _UVTransform)
                #define _Flip UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _Flip)
            #endif

            TEXTURE2D(_DynamicBaseMap);
            SAMPLER(sampler_DynamicBaseMap);

            Varyings UnlitPassVertex(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4 newOS = input.positionOS;
                newOS = float4(newOS.xy * _Flip.xy, newOS.z, 1.0);
                const VertexPositionInputs positionInputs = GetVertexPositionInputs(newOS);
                output.positionCS = positionInputs.positionCS;
                output.uv = input.uv * _UVTransform.zw + _UVTransform.xy;
                output.color = input.color;
                return output;
            }

            half4 UnlitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                // half4 baseMap = half4(SAMPLE_TEXTURE2D(_DynamicBaseMap, sampler_DynamicBaseMap, input.uv));

                // baseMap.rgb *= baseMap.a;

                return _BaseColor;
            }
            ENDHLSL
        }
    }
}