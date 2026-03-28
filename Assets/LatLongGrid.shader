Shader "Universal Render Pipeline/LatLongGridURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0.5, 0, 1) // 橙色
        _BaseAlpha ("Base Alpha", Range(0,1)) = 0.0 // 基礎透明度（腳本控制）
        _LineColor ("Line Color (RGBA)", Color) = (1,1,1,0.6) // 固定白色
        _LonStep ("Longitude Step (deg)", Float) = 15
        _LatStep ("Latitude Step (deg)", Float) = 15
        _LineWidth ("Line Width (deg)", Float) = 0.6
        _Feather ("Edge Feather (deg)", Float) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _BaseAlpha;
                float4 _LineColor;
                float _LonStep;
                float _LatStep;
                float _LineWidth;
                float _Feather;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                return output;
            }

            float nearestStepDistDeg(float angleDeg, float stepDeg)
            {
                stepDeg = max(stepDeg, 0.0001);
                float m = fmod(angleDeg, stepDeg);
                if (m < 0) m += stepDeg;
                return min(m, stepDeg - m);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 n = normalize(input.normalWS);
                float lonDeg = degrees(atan2(n.z, n.x));
                float latDeg = degrees(asin(n.y));
                float dLon = nearestStepDistDeg(lonDeg, abs(_LonStep));
                float dLat = nearestStepDistDeg(abs(latDeg), abs(_LatStep));
                float halfW = max(0.0001, _LineWidth * 0.5);
                float fea = max(0.00001, _Feather);
                float lonMask = 1.0 - smoothstep(halfW, halfW + fea, dLon);
                float latMask = 1.0 - smoothstep(halfW, halfW + fea, dLat);
                float lineMask = saturate(max(lonMask, latMask));
                half3 rgb = lerp(_BaseColor.rgb, _LineColor.rgb, lineMask);
                float a = lerp(_BaseAlpha, _LineColor.a, lineMask);
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}