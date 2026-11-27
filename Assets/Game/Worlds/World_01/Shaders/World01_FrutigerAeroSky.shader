Shader "Game/World01/FrutigerAeroSky"
{
    Properties
    {
        _GroundColor ("Ground Bounce Color", Color) = (0.659, 0.886, 0.847, 1)
        _HorizonColor ("Horizon Pearl Color", Color) = (0.910, 0.980, 1.000, 1)
        _MidSkyColor ("Mid Sky Cerulean Color", Color) = (0.490, 0.847, 0.973, 1)
        _ZenithColor ("Zenith Azure Color", Color) = (0.180, 0.612, 0.902, 1)
        _SunTint ("Sun Halo Tint", Color) = (1.000, 0.973, 0.910, 1)
        _SunDir ("Sun Direction", Vector) = (-0.35, 0.72, 0.59, 0)
        _SunStrength ("Sun Halo Strength", Range(0, 1)) = 0.18
        _Exposure ("Exposure", Range(0.2, 2.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDirWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _GroundColor;
                half4 _HorizonColor;
                half4 _MidSkyColor;
                half4 _ZenithColor;
                half4 _SunTint;
                float4 _SunDir;
                half _SunStrength;
                half _Exposure;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.viewDirWS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.viewDirWS);
                // Remap [-0.25, 0.55] -> [0.0, 1.0] to match 6.5 deg downward 2.5D ortho framing
                float t = saturate((dir.y + 0.25) / 0.80);

                half3 col = _GroundColor.rgb;
                if (t < 0.28)
                {
                    col = lerp(_GroundColor.rgb, _HorizonColor.rgb, saturate(t / 0.28));
                }
                else if (t < 0.60)
                {
                    col = lerp(_HorizonColor.rgb, _MidSkyColor.rgb, saturate((t - 0.28) / 0.32));
                }
                else
                {
                    col = lerp(_MidSkyColor.rgb, _ZenithColor.rgb, saturate((t - 0.60) / 0.40));
                }

                float3 sunDir = normalize(_SunDir.xyz);
                float sunDot = saturate(dot(dir, sunDir));
                float halo = pow(sunDot, 12.0) * 0.45 + pow(sunDot, 96.0) * 0.55;
                col += _SunTint.rgb * (halo * _SunStrength);

                return half4(col * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
