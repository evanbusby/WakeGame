using UnityEngine;

public class WaterScroll : MonoBehaviour
{
    public float scrollSpeed = 20f;

    Material water;
    Vector2 offset;

    void Awake()
    {
        // The Water shader (Water.shader) does the actual moving, lit "lake"
        // look. This texture just adds a bit of grayscale brightness
        // variation on top - the shader supplies the color.
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
                // Grayscale, not colored - the shader's own color supplies
                // the hue, this only varies brightness.
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
