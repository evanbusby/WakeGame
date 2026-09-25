// A minimal stand-in for Unity's built-in "Standard" shader, used for every
// runtime-created material in the scene (boat, rider, hills, trees, land).
// Standard is never referenced by name from any asset that ships in a build -
// only created at runtime via Shader.Find - so WebGL builds (like an itch.io
// export) strip it out and everything using it renders pink. This shader is
// a real project asset with its own GUID, and it's added to Always Included
// Shaders in ProjectSettings/GraphicsSettings.asset (the same fix already
// applied to Custom/Water), so it always survives stripping.
Shader "Custom/SimpleLit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Glossiness ("Smoothness", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Metallic;
        half _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
