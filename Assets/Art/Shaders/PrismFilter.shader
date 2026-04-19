Shader "PrismZone/PrismFilter"
{
    // Fullscreen shader for URP FullScreenPassRendererFeature.
    // _FilterMode: 0 = passthrough, 1 = red-strip (hide what red ink conceals),
    //              2 = green-strip, 3 = blue-strip.
    // Applied on the base 640x360 colour pass BEFORE the Pixel Perfect upscale,
    // so the strip is pixel-exact and doesn't bloom under RT scaling.
    Properties
    {
        _FilterMode("Filter Mode (0=none,1=R,2=G,3=B)", Float) = 0
        _Boost("Channel Boost", Float) = 1.15
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "PrismFilter"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _FilterMode;
            float _Boost;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                int mode = (int)_FilterMode;
                // Strip the target channel -> content drawn in that channel becomes
                // invisible and content hidden beneath it is revealed.
                if (mode == 1)
                {
                    col.r = 0;
                    col.gb *= _Boost;
                }
                else if (mode == 2)
                {
                    col.g = 0;
                    col.rb *= _Boost;
                }
                else if (mode == 3)
                {
                    col.b = 0;
                    col.rg *= _Boost;
                }
                return col;
            }
            ENDHLSL
        }
    }
}
