using UnityEngine;

public class WaterScroll : MonoBehaviour
{
    public float scrollSpeed = 20f;

    Material water;
    Vector2 offset;

    void Awake()
    {
        // Custom/Water (Water.shader) does the actual "lake" look - moving
        // wave-perturbed lighting/highlights and a fresnel sky tint - all
        // per-pixel, so it stays cheap regardless of this plane's low
        // vertex count. This texture is just an extra grayscale variation
        // mask layered on top of the shader's own color; the shader
        // supplies the hue, this only modulates brightness.
        water = new Material(Shader.Find("Custom/Water"));
        water.mainTexture = CreateRippleTexture();
        water.mainTextureScale = new Vector2(10f, 200f);
        GetComponent<Renderer>().material = water;
    }

    void Update()
    {
        offset.y -= scrollSpeed * Time.deltaTime * 0.05f;
        water.mainTextureOffset = offset;
    }

    Texture2D CreateRippleTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // A neutral grayscale mask (not a color) - the shader's
                // _Color supplies the actual lake hue, this only modulates
                // brightness, so it stays close to a mid gray rather than
                // baking in a tint of its own.
                float wave = Mathf.Sin(y * 0.6f) * 0.5f + Mathf.Sin(x * 0.15f + y * 0.1f) * 0.5f;
                float shade = Mathf.Clamp01(0.5f + wave * 0.5f);
                tex.SetPixel(x, y, new Color(shade, shade, shade, 1f));
            }
        }

        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }
}
