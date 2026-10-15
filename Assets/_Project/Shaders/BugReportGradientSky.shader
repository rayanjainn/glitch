// BUG//REPORT sky: a three-colour gradient with optional twinkling stars and a faint "system grid".
// Used per chapter so the same level geometry reads as a different place. URP skybox.
Shader "BugReport/GradientSky"
{
    Properties
    {
        _Top ("Zenith", Color) = (0.15, 0.3, 0.7, 1)
        _Horizon ("Horizon", Color) = (0.75, 0.82, 0.95, 1)
        _Bottom ("Below the horizon", Color) = (0.2, 0.2, 0.25, 1)
        _Exponent ("Gradient sharpness", Range(0.2, 4)) = 0.8
        _Stars ("Stars", Range(0, 1)) = 0
        _Grid ("System grid", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Background"
            "Queue" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "Sky"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Top;
                half4 _Horizon;
                half4 _Bottom;
                half _Exponent;
                half _Stars;
                half _Grid;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.dir = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.dir);
                float h = d.y;

                half3 colour = h >= 0.0
                    ? lerp(_Horizon.rgb, _Top.rgb, pow(saturate(h), _Exponent))
                    : lerp(_Horizon.rgb, _Bottom.rgb, pow(saturate(-h), 0.5));

                float azimuth = atan2(d.z, d.x);

                // Stars: one random point per cell, twinkling, fading towards the horizon.
                float2 cell = floor(float2(azimuth * 40.0, h * 80.0));
                float rnd = frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453);
                float star = step(0.995, rnd) * saturate(h * 2.0) * (0.6 + 0.4 * sin(_Time.y * 3.0 + rnd * 40.0));
                colour += star * _Stars;

                // A faint lat/long grid, the world's wireframe showing through.
                float gx = abs(frac(azimuth * 4.0 / PI) - 0.5);
                float gy = abs(frac(h * 8.0) - 0.5);
                float lines = smoothstep(0.47, 0.5, max(gx, gy));
                colour += lines * _Grid * half3(0.2, 0.6, 0.8) * saturate(1.0 - abs(h) * 1.5);

                return half4(colour, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
