using UnityEngine;

// Shared look-and-feel for every OnGUI screen in the game (the main menu,
// its controls overlay, and the in-game pause overlay) - one palette and one
// set of procedurally generated textures, so all three read as the same
// interface instead of three separately styled ones.
public static class MenuUI
{
    public static readonly Color BackgroundColor = new Color(0.05f, 0.28f, 0.48f);
    public static readonly Color AccentColor = new Color(1f, 0.6f, 0.1f);
    public static readonly Color AccentHoverColor = new Color(1f, 0.7f, 0.28f);
    public static readonly Color GhostColor = new Color(1f, 1f, 1f, 0.14f);
    public static readonly Color GhostHoverColor = new Color(1f, 1f, 1f, 0.26f);
    public static readonly Color PanelColor = new Color(0.04f, 0.09f, 0.15f, 0.94f);
    public static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.3f);
    public static readonly Color ChipColor = new Color(1f, 1f, 1f, 0.14f);
    public static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);

    // A rounded rectangle drawn with a signed-distance field so the edge is
    // antialiased (not a hard, jagged pixel boundary) even after Unity
    // stretches this small texture up to fill a much bigger control. Used
    // with a matching GUIStyle.border (9-slice) so the corner radius stays
    // constant regardless of the final control's size.
    public static Texture2D CreateRoundedRect(int width, int height, float radius, Color color)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Vector2 half = new Vector2(width * 0.5f, height * 0.5f);
        Vector2 innerHalf = new Vector2(Mathf.Max(half.x - radius, 0f), Mathf.Max(half.y - radius, 0f));

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                float qx = Mathf.Max(Mathf.Abs(p.x) - innerHalf.x, 0f);
                float qy = Mathf.Max(Mathf.Abs(p.y) - innerHalf.y, 0f);
                float dist = Mathf.Sqrt(qx * qx + qy * qy) - radius;
                float alpha = Mathf.Clamp01(1f - (dist + 0.5f) / 1.5f);

                Color c = color;
                c.a *= alpha;
                tex.SetPixel(x, y, c);
            }
        }

        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }

    public static Texture2D CreateVerticalGradient(int height, Color top, Color bottom)
    {
        Texture2D tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            tex.SetPixel(0, y, Color.Lerp(top, bottom, t));
        }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }

    public static Texture2D CreateSolid(Color color)
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, color);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        return tex;
    }

    // Draws a "<-" shape (a shaft plus a triangular head) out of plain
    // pixels rather than a font glyph, so this icon can't be affected by
    // which characters a target platform's font happens to include - unlike
    // a unicode arrow character, which renders fine in the Editor (OS font)
    // but shows as an empty box in an exported WebGL build.
    public static Texture2D CreateLeftArrowTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(color.r, color.g, color.b, 0f);

        float midY = size * 0.5f;
        float shaftHalfThick = size * 0.09f;
        float shaftStartX = size * 0.30f;
        float shaftEndX = size * 0.78f;
        float headTipX = size * 0.14f;
        float headBaseX = size * 0.48f;
        float headHalfHeight = size * 0.24f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float fx = x + 0.5f;
                float fy = y + 0.5f;

                bool inShaft = fx >= shaftStartX && fx <= shaftEndX && Mathf.Abs(fy - midY) <= shaftHalfThick;

                bool inHead = false;
                if (fx >= headTipX && fx <= headBaseX)
                {
                    float t = (fx - headTipX) / (headBaseX - headTipX);
                    float halfHeightAtX = t * headHalfHeight;
                    inHead = Mathf.Abs(fy - midY) <= halfHeightAtX;
                }

                tex.SetPixel(x, y, (inShaft || inHead) ? color : clear);
            }
        }

        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }

    // Two vertical bars, drawn as plain pixels for the same font-independence
    // reason as CreateLeftArrowTexture above.
    public static Texture2D CreatePauseIconTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(color.r, color.g, color.b, 0f);

        float barWidth = size * 0.22f;
        float gap = size * 0.16f;
        float barHeight = size * 0.7f;
        float centerY = size * 0.5f;
        float leftBarStartX = size * 0.5f - gap * 0.5f - barWidth;
        float rightBarStartX = size * 0.5f + gap * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float fx = x + 0.5f;
                float fy = y + 0.5f;

                bool inLeftBar = fx >= leftBarStartX && fx <= leftBarStartX + barWidth
                    && Mathf.Abs(fy - centerY) <= barHeight * 0.5f;
                bool inRightBar = fx >= rightBarStartX && fx <= rightBarStartX + barWidth
                    && Mathf.Abs(fy - centerY) <= barHeight * 0.5f;

                tex.SetPixel(x, y, (inLeftBar || inRightBar) ? color : clear);
            }
        }

        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }
}
