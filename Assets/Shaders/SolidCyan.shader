Shader "EchoLine/SolidCyan"
{


    Properties
    {
        // SpriteRenderer requires _MainTex to exist on the shader.
        // This shader never samples it — it's here to silence the warning only.
        _MainTex ("Sprite Texture", 2D) = "white" {}
    }
    // Swap in _BaseColor later when promoting to SonarReveal.

    SubShader
    {
        Tags
        {
            "RenderType"      = "Opaque"
            "Queue"           = "Geometry"
            "RenderPipeline"  = "UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Gives you TransformObjectToHClip and the URP matrix uniforms.
            // This is the only include you need for a bare unlit shader.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            // #00E5FF as linear-space floats.
            // Unity expects linear colour values in shader output when the
            // project uses Linear colour space (the correct setting for URP + Bloom).
            // #00E5FF in sRGB  → (0.000, 0.898, 1.000)
            // Converted to linear → (0.000, 0.792, 1.000)
            half4 frag(Varyings IN) : SV_Target
            {
                return half4(0.000, 0.792, 1.000, 1.0);
            }

            ENDHLSL
        }
    }
}
