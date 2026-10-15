// BUG//REPORT hologram: a fresnel-lit, scan-lined, occasionally tearing translucent surface used for
// firewalls, intangible (ghost) objects, bridges and desync outlines. Unlit, alpha blended, URP.
Shader "BugReport/Hologram"
{
    Properties
    {
        [HDR] _Color ("Colour", Color) = (0.4, 1.2, 2.0, 1)
        _Alpha ("Base alpha", Range(0, 1)) = 0.25
        _RimPower ("Rim power", Range(0.5, 8)) = 2.5
        _RimBoost ("Rim boost", Range(0, 3)) = 1.2
        _ScanDensity ("Scanline density", Float) = 36
        _ScanSpeed ("Scanline speed", Float) = 0.8
        _ScanStrength ("Scanline strength", Range(0, 1)) = 0.35
        _Glitch ("Glitch tearing", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Hologram"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Alpha;
                half _RimPower;
                half _RimBoost;
                half _ScanDensity;
                half _ScanSpeed;
                half _ScanStrength;
                half _Glitch;
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
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // An occasional horizontal band slips sideways, like a torn video frame.
                float band = floor(positionWS.y * 6.0 + _Time.y * 4.0);
                float tear = step(0.93, Hash(band)) * (Hash(band + 3.7) - 0.5);
                positionWS.x += tear * 0.35 * _Glitch;

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 normal = normalize(input.normalWS);
                float rim = pow(1.0 - saturate(abs(dot(normal, viewDir))), _RimPower) * _RimBoost;

                float scan = 0.5 + 0.5 * sin(input.positionWS.y * _ScanDensity - _Time.y * _ScanSpeed * 6.2831853);
                scan = lerp(1.0, scan, _ScanStrength);
                float flicker = 0.92 + 0.08 * sin(_Time.y * 37.0);

                half3 colour = _Color.rgb * (0.6 + rim) * scan * flicker;
                half alpha = saturate((_Alpha + rim * 0.55) * scan) * _Color.a;
                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
