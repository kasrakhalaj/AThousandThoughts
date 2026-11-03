Shader "AfterHours/RoadFog"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            float _AfterHoursFogDensity;
            float4 _AfterHoursFogColor;
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float rawDepth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    if (rawDepth < .00001) return scene;
                    float depth = rawDepth;
                #else
                    if (rawDepth > .99999) return scene;
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, rawDepth);
                #endif
                float3 world = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float distanceThroughFog = max(0, distance(world, _WorldSpaceCameraPos) - 12);
                float density = _AfterHoursFogDensity * exp(-max(world.y, 0) * .025);
                float opacity = 1 - exp(-pow(distanceThroughFog * density, 2));
                return half4(lerp(scene.rgb, _AfterHoursFogColor.rgb, saturate(opacity)), scene.a);
            }
            ENDHLSL
        }
    }
}
