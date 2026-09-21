using UnityEngine;

// Mirrors WaterScroll's approach: the land itself never actually moves (it's
// a static plane, same as the water), so a plain flat color would give no
// visual cue that the boat is moving at all. Scrolling a blotchy texture
// across it fakes that motion cheaply, same trick as the water ripples.
public class LandScroll : MonoBehaviour
{
    // Matches BoatMover's default speed so the land appears to slide past at
    // roughly the same rate the boat is actually travelling.
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
        // Same sign/axis as WaterScroll: negative V scroll reads as the
        // surface sliding toward the rear of the boat, i.e. backwards,
        // as the boat moves forward.
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
