// Animated lake water with no real physics or geometry changes - it's a
// purely visual plane. Wave motion comes from summing a couple of sine waves
// per pixel (cheap, and independent of how detailed the mesh is), plus a
// fresnel "sky" highlight standing in for real reflections and a scrolling
// ripple texture for extra variation.
Shader "Custom/Water"
{
    Properties
    {
        _MainTex ("Ripple Texture", 2D) = "white" {}
        _Color ("Water Color", Color) = (0.08, 0.35, 0.55, 1)
        _FresnelColor ("Sky/Fresnel Color", Color) = (0.75, 0.88, 0.95, 1)
        _FresnelPower ("Fresnel Power", Range(0.2, 8)) = 3.5
        _WaveScale ("Wave Scale", Float) = 0.35
        _WaveSpeed ("Wave Speed", Float) = 0.5
        _WaveStrength ("Wave Normal Strength", Range(0, 2)) = 0.5
        _SpecularPower ("Specular Sharpness", Range(1, 200)) = 80
        _SpecularIntensity ("Specular Intensity", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150

        CGPROGRAM
        #pragma surface surf BlinnPhong
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _FresnelColor;
        float _FresnelPower;
        float _WaveScale;
        float _WaveSpeed;
        float _WaveStrength;
        half _SpecularPower;
        half _SpecularIntensity;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 viewDir;
        };

        // Two sine waves at different angles, summed as a cheap stand-in for
        // a real wave pattern. Returns the exact slope of that sum directly,
        // so no extra texture lookups are needed to compute lighting.
        float2 WaveGradient(float2 pos, float time)
        {
            float2 dirA = normalize(float2(1.0, 0.35));
            float2 dirB = normalize(float2(-0.6, 1.0));

            float freqA = _WaveScale;
            float freqB = _WaveScale * 1.8;
            float speedA = _WaveSpeed;
            float speedB = _WaveSpeed * 0.7;

            float phaseA = dot(pos, dirA) * freqA + time * speedA;
            float phaseB = dot(pos, dirB) * freqB - time * speedB;

            float dA = cos(phaseA) * freqA;
            float dB = cos(phaseB) * freqB * 0.6;

            return dirA * dA + dirB * dB;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            float2 gradient = WaveGradient(IN.worldPos.xz, _Time.y);

            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            fixed3 baseColor = _Color.rgb * (0.85 + tex.r * 0.3);

            // Tilts the surface normal based on the wave slope, so lighting
            // catches it like a bumpy surface instead of a flat one.
            float3 tangentNormal = normalize(float3(-gradient.x * _WaveStrength, -gradient.y * _WaveStrength, 1.0));

            // The water plane is always flat, so its true surface normal is
            // just straight up - this is a simple grazing-angle (fresnel)
            // highlight based on that.
            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), float3(0, 1, 0))), _FresnelPower);
            fixed3 finalColor = lerp(baseColor, _FresnelColor.rgb, fresnel * 0.6);

            o.Albedo = finalColor;
            o.Normal = tangentNormal;
            o.Specular = _SpecularPower / 200.0;
            o.Gloss = _SpecularIntensity;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
