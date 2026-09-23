using UnityEngine;

// The land is a static plane that never actually moves, so scrolling a
// blotchy grass texture across it fakes the sense of motion as the boat
// travels - same trick as the water ripples (see WaterScroll).
public class LandScroll : MonoBehaviour
{
    // Matches the boat's own speed so the land appears to slide past at the
    // same rate the boat is moving.
    public float scrollSpeed = 20f;

    Renderer rend;
    Vector2 offset;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        rend.material.mainTexture = CreateGrassTexture();

        Vector3 scale = transform.localScale;
        rend.material.mainTextureScale = new Vector2(scale.x * 0.2f, scale.z * 0.4f);
    }

    void Update()
    {
        // Scrolls backward as the boat moves forward, same direction as the
        // water.
        offset.y -= scrollSpeed * Time.deltaTime * 0.05f;
        rend.material.mainTextureOffset = offset;
    }

    Texture2D CreateGrassTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color baseColor = new Color(0.28f, 0.5f, 0.2f);
        Color patchColor = new Color(0.19f, 0.38f, 0.15f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float blotch = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                tex.SetPixel(x, y, Color.Lerp(patchColor, baseColor, blotch));
            }
        }

        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }
}
