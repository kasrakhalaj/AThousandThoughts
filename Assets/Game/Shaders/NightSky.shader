Shader "AfterHours/NightSky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            float _AfterHoursFog;
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.direction = v.positionOS.xyz; return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction);
                float horizon = pow(saturate(1 - max(0, d.y)), 5);
                float3 color = lerp(float3(.006, .012, .026), float3(.055, .08, .105), horizon);
                float2 uv = float2(atan2(d.x, d.z) / 6.2831853 + .5, asin(d.y) / 3.14159265 + .5);
                float2 grid = uv * float2(210, 100);
                float hash = frac(sin(dot(floor(grid), float2(127.1, 311.7))) * 43758.5453);
                float star = (1 - smoothstep(.015, .085, length(frac(grid) - .5))) * step(.984, hash) * smoothstep(.02, .2, d.y);
                color += star * lerp(float3(.7, .8, 1), float3(1, .9, .7), hash);
                float3 moonDirection = normalize(float3(-.32, .14, 1));
                float alignment = dot(d, moonDirection);
                float moon = smoothstep(.99865, .99885, alignment);
                float halo = pow(saturate(alignment), 2400) * .11;
                float2 moonUV = float2(dot(d, normalize(cross(float3(0,1,0),moonDirection))), d.y-moonDirection.y) * 21;
                float craters = .035 * sin(moonUV.x * 9 + sin(moonUV.y * 7)) * sin(moonUV.y * 11);
                color += halo * float3(.65, .73, .8);
                color = lerp(color, float3(1.2, 1.22, 1.13) + craters, moon);
                color = lerp(color, float3(.065, .09, .115), saturate(_AfterHoursFog * horizon * .65));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
