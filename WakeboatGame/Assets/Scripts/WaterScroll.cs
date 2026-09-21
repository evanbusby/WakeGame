using UnityEngine;

public class WaterScroll : MonoBehaviour
{
    public float scrollSpeed = 20f;

    Renderer rend;
    Vector2 offset;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        rend.material.mainTexture = CreateRippleTexture();
        rend.material.mainTextureScale = new Vector2(10f, 200f);
    }

    void Update()
    {
        offset.y -= scrollSpeed * Time.deltaTime * 0.05f;
        rend.material.mainTextureOffset = offset;
    }

    Texture2D CreateRippleTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color baseColor = new Color(0.1f, 0.4f, 0.7f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float wave = Mathf.Sin(y * 0.6f) * 0.5f + Mathf.Sin(x * 0.15f + y * 0.1f) * 0.5f;
                float shade = 1f + wave * 0.2f;
                tex.SetPixel(x, y, baseColor * shade);
            }
        }

        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }
}
