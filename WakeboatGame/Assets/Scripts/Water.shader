// Lightweight animated lake water: no fluid simulation and no vertex
// displacement (the water plane's actual geometry/collision never
// changes, so nothing about water "physics" is touched - there wasn't any
// to begin with, it's a purely visual plane). All motion is a per-pixel
// analytic normal perturbation from a small sum of directional sine waves,
// so it costs the same regardless of how coarse the underlying mesh is,
// combined with a cheap fresnel "sky" highlight standing in for real
// reflections and a scrolling ripple texture (still fed in from
// WaterScroll.cs) for extra surface variation.
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

        // Two directional sine waves at different angles/frequencies,
        // summed - a cheap stand-in for a real wave spectrum. Returns the
        // gradient analytically (exact derivative of the sine sum) so the
        // normal doesn't need extra texture samples or neighbor lookups.
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

            // Tangent-space normal perturbation: z stays near 1 (mostly
            // facing straight out) while x/y lean with the wave slope -
            // works regardless of the mesh's world orientation, since
            // Surface Shaders convert this to world space automatically
            // using the plane's own tangent basis.
            float3 tangentNormal = normalize(float3(-gradient.x * _WaveStrength, -gradient.y * _WaveStrength, 1.0));

            // The plane is always exactly flat/horizontal, so its true
            // world-space geometric normal is just world-up - viewDir here
            // is world space too, so this is a straightforward grazing-angle
            // (fresnel) term without needing the tangent-space normal above
            // converted into world space.
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
