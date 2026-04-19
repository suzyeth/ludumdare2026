Shader "PrismZone/PrismFilter"
{
    // Fullscreen shader for URP FullScreenPassRendererFeature.
    //
    //   _FilterMode: 0 = passthrough, 1 = red lens, 2 = green lens.
    //   Blue dropped in v1.2 — only red / green remain in the player rotation.
    //
    // Only effect: edge vignette tint (red / green glow on screen perimeter).
    // Centre keeps the original image unaltered — colour-based reveal gameplay is
    // handled at the GameObject level by GlassesVisibility, not by a channel strip.
    //
    // Applied on the 640x360 colour pass BEFORE Pixel Perfect upscale.
    Properties
    {
        _FilterMode("Filter Mode (0=none,1=R,2=G)", Float) = 0
        _VignetteStart("Vignette Start (0=centre)", Range(0,1.4)) = 0.78
        _VignetteEnd("Vignette End (>=Start)", Range(0,1.5)) = 1.1
        _VignetteIntensity("Vignette Intensity", Range(0,1)) = 0.9
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
            float _VignetteStart;
            float _VignetteEnd;
            float _VignetteIntensity;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                int mode = (int)_FilterMode;

                // Pick lens colour; mode 0 = no tint.
                half3 lensColour = half3(0, 0, 0);
                bool tint = false;
                if      (mode == 1) { lensColour = half3(1.0, 0.12, 0.12); tint = true; }
                else if (mode == 2) { lensColour = half3(0.12, 0.95, 0.25); tint = true; }

                if (tint)
                {
                    float2 d2 = (uv - 0.5) * 2.0;   // 1 ≈ corner
                    float dist = length(d2);
                    float v = saturate((dist - _VignetteStart) / max(0.0001, (_VignetteEnd - _VignetteStart)));
                    v = v * v * (3.0 - 2.0 * v);    // smoothstep
                    v *= _VignetteIntensity;
                    col.rgb = lerp(col.rgb, max(col.rgb, lensColour), v);
                }

                return col;
            }
            ENDHLSL
        }
    }
}
