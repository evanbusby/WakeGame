using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public GameBootstrap bootstrap;

    enum Screen { Main, Controls, Started }

    struct ControlEntry
    {
        public readonly string key;
        public readonly string description;
        public ControlEntry(string key, string description)
        {
            this.key = key;
            this.description = description;
        }
    }

    static readonly ControlEntry[] ControlEntries =
    {
        new ControlEntry("A", "Steer left"),
        new ControlEntry("D", "Steer right"),
        new ControlEntry("SPACE", "Jump"),
        new ControlEntry("LEFT ARROW", "Spin left"),
        new ControlEntry("RIGHT ARROW", "Spin right"),
        new ControlEntry("UP ARROW", "Flip forward"),
        new ControlEntry("DOWN ARROW", "Flip backward"),
    };

    static readonly Color BackgroundColor = new Color(0.05f, 0.28f, 0.48f);
    static readonly Color AccentColor = new Color(1f, 0.6f, 0.1f);
    static readonly Color AccentHoverColor = new Color(1f, 0.7f, 0.28f);
    static readonly Color GhostColor = new Color(1f, 1f, 1f, 0.14f);
    static readonly Color GhostHoverColor = new Color(1f, 1f, 1f, 0.26f);
    static readonly Color PanelColor = new Color(0.04f, 0.09f, 0.15f, 0.94f);
    static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.3f);
    static readonly Color ChipColor = new Color(1f, 1f, 1f, 0.14f);

    Camera menuCamera;
    Screen screen = Screen.Main;

    // The back button previously showed a unicode arrow character ("<-").
    // Unity's default OnGUI font only guarantees ASCII glyphs are present in
    // an exported WebGL build - it renders fine in the Editor (which uses the
    // OS's font) but shows as an empty box once published. Drawing the arrow
    // as a procedurally generated icon texture instead means it doesn't
    // depend on the target's font at all, so it looks identical everywhere.
    Texture2D backArrowTex;

    Texture2D accentButtonTex;
    Texture2D accentButtonHoverTex;
    Texture2D ghostButtonTex;
    Texture2D ghostButtonHoverTex;
    Texture2D circleTex;
    Texture2D circleHoverTex;
    Texture2D panelTex;
    Texture2D shadowTex;
    Texture2D circleShadowTex;
    Texture2D chipTex;
    Texture2D gradientTex;

    GUIStyle titleStyle;
    GUIStyle primaryButtonStyle;
    GUIStyle secondaryButtonStyle;
    GUIStyle backButtonStyle;
    GUIStyle controlsTitleStyle;
    GUIStyle keyChipStyle;
    GUIStyle descriptionStyle;
    GUIStyle panelStyle;
    GUIStyle shadowStyle;
    GUIStyle circleShadowStyle;

    void Awake()
    {
        // A plain solid-color camera so the menu isn't shown over a blank
        // black screen before the real gameplay camera exists.
        GameObject camObj = new GameObject("MenuCamera");
        menuCamera = camObj.AddComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = BackgroundColor;

        BuildTextures();
        BuildStyles();
    }

    void OnGUI()
    {
        if (screen == Screen.Started) return;

        if (screen == Screen.Controls)
        {
            DrawControlsOverlay();
            return;
        }

        DrawMainMenu();
    }

    void DrawMainMenu()
    {
        DrawGradientBackdrop();

        float buttonWidth = 360f;
        float buttonHeight = 84f;
        float spacing = 26f;
        float titleHeight = 100f;
        float gapBelowTitle = 46f;

        float totalHeight = titleHeight + gapBelowTitle + buttonHeight * 2f + spacing;
        float startY = UnityEngine.Screen.height / 2f - totalHeight / 2f;
        float centerX = UnityEngine.Screen.width / 2f - buttonWidth / 2f;

        GUI.Label(new Rect(0f, startY, UnityEngine.Screen.width, titleHeight), "Wakeboard Game", titleStyle);

        float playY = startY + titleHeight + gapBelowTitle;
        float controlsY = playY + buttonHeight + spacing;

        DrawShadow(new Rect(centerX, playY, buttonWidth, buttonHeight), shadowStyle);
        if (GUI.Button(new Rect(centerX, playY, buttonWidth, buttonHeight), "PLAY", primaryButtonStyle))
        {
            StartGame();
        }

        DrawShadow(new Rect(centerX, controlsY, buttonWidth, buttonHeight), shadowStyle);
        if (GUI.Button(new Rect(centerX, controlsY, buttonWidth, buttonHeight), "CONTROLS", secondaryButtonStyle))
        {
            screen = Screen.Controls;
        }
    }

    void DrawControlsOverlay()
    {
        DrawGradientBackdrop();

        // The panel fills nearly the entire screen (a small adaptive margin,
        // not a fixed small card) and every size below is derived from the
        // panel's own dimensions rather than fixed pixel constants, so the
        // controls list is always as large as the screen allows while still
        // fitting every row without scrolling or running off the bottom.
        float marginX = Mathf.Clamp(UnityEngine.Screen.width * 0.03f, 24f, 70f);
        float marginY = Mathf.Clamp(UnityEngine.Screen.height * 0.04f, 24f, 60f);
        float panelX = marginX;
        float panelY = marginY;
        float panelWidth = UnityEngine.Screen.width - marginX * 2f;
        float panelHeight = UnityEngine.Screen.height - marginY * 2f;

        DrawShadow(new Rect(panelX, panelY, panelWidth, panelHeight), shadowStyle);
        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none, panelStyle);

        float titleFontSize = Mathf.Clamp(panelHeight * 0.09f, 32f, 72f);
        float titleAreaHeight = titleFontSize + 50f;
        float bottomPadding = Mathf.Clamp(panelHeight * 0.03f, 16f, 40f);

        int rowCount = ControlEntries.Length;
        float rowHeight = (panelHeight - titleAreaHeight - bottomPadding) / rowCount;

        float descFontSize = Mathf.Clamp(rowHeight * 0.4f, 18f, 40f);
        float chipFontSize = Mathf.Clamp(rowHeight * 0.3f, 14f, 30f);
        float chipHeight = Mathf.Clamp(rowHeight * 0.7f, 32f, 64f);

        float rowPaddingX = Mathf.Clamp(panelWidth * 0.05f, 30f, 90f);
        float chipWidth = Mathf.Clamp(panelWidth * 0.24f, 170f, 340f);
        float chipGap = Mathf.Clamp(panelWidth * 0.025f, 16f, 40f);
        float descWidth = panelWidth - rowPaddingX * 2f - chipWidth - chipGap;

        controlsTitleStyle.fontSize = Mathf.RoundToInt(titleFontSize);
        keyChipStyle.fontSize = Mathf.RoundToInt(chipFontSize);
        descriptionStyle.fontSize = Mathf.RoundToInt(descFontSize);

        GUI.Label(new Rect(panelX, panelY + 14f, panelWidth, titleAreaHeight - 14f), "CONTROLS", controlsTitleStyle);

        float rowsStartY = panelY + titleAreaHeight;
        for (int i = 0; i < rowCount; i++)
        {
            float rowY = rowsStartY + i * rowHeight;

            Rect chipRect = new Rect(panelX + rowPaddingX, rowY + (rowHeight - chipHeight) / 2f, chipWidth, chipHeight);
            GUI.Label(chipRect, ControlEntries[i].key, keyChipStyle);

            Rect descRect = new Rect(panelX + rowPaddingX + chipWidth + chipGap, rowY, descWidth, rowHeight);
            GUI.Label(descRect, ControlEntries[i].description, descriptionStyle);
        }

        float backSize = 68f;
        Rect backRect = new Rect(28f, 28f, backSize, backSize);
        DrawShadow(backRect, circleShadowStyle);
        if (GUI.Button(backRect, GUIContent.none, backButtonStyle))
        {
            screen = Screen.Main;
        }

        float iconSize = 30f;
        Rect iconRect = new Rect(
            backRect.x + (backRect.width - iconSize) / 2f,
            backRect.y + (backRect.height - iconSize) / 2f,
            iconSize, iconSize);
        GUI.DrawTexture(iconRect, backArrowTex, ScaleMode.ScaleToFit, true);
    }

    void DrawGradientBackdrop()
    {
        GUI.DrawTexture(new Rect(0f, 0f, UnityEngine.Screen.width, UnityEngine.Screen.height), gradientTex, ScaleMode.StretchToFill, true);
    }

    // Draws a soft drop shadow using a 9-sliced (bordered) GUIStyle rather
    // than a raw stretched texture, so the shadow's rounded corner stays a
    // fixed radius instead of stretching into an ellipse on non-square rects
    // like the wide Play/Controls buttons or the controls panel.
    void DrawShadow(Rect rect, GUIStyle style)
    {
        rect.y += 6f;
        GUI.Box(rect, GUIContent.none, style);
    }

    void StartGame()
    {
        screen = Screen.Started;

        if (menuCamera != null)
        {
            Destroy(menuCamera.gameObject);
        }

        if (bootstrap != null)
        {
            bootstrap.StartGame();
        }
    }

    void BuildStyles()
    {
        titleStyle = new GUIStyle();
        titleStyle.fontSize = 58;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        primaryButtonStyle = new GUIStyle();
        primaryButtonStyle.fontSize = 30;
        primaryButtonStyle.fontStyle = FontStyle.Bold;
        primaryButtonStyle.alignment = TextAnchor.MiddleCenter;
        primaryButtonStyle.normal.textColor = Color.white;
        primaryButtonStyle.normal.background = accentButtonTex;
        primaryButtonStyle.hover.textColor = Color.white;
        primaryButtonStyle.hover.background = accentButtonHoverTex;
        primaryButtonStyle.active.textColor = Color.white;
        primaryButtonStyle.active.background = accentButtonHoverTex;
        primaryButtonStyle.border = new RectOffset(24, 24, 24, 24);

        secondaryButtonStyle = new GUIStyle(primaryButtonStyle);
        secondaryButtonStyle.normal.background = ghostButtonTex;
        secondaryButtonStyle.hover.background = ghostButtonHoverTex;
        secondaryButtonStyle.active.background = ghostButtonHoverTex;

        backButtonStyle = new GUIStyle();
        backButtonStyle.normal.background = circleTex;
        backButtonStyle.hover.background = circleHoverTex;
        backButtonStyle.active.background = circleHoverTex;
        backButtonStyle.border = new RectOffset(30, 30, 30, 30);

        controlsTitleStyle = new GUIStyle(titleStyle);
        controlsTitleStyle.fontSize = 42;

        keyChipStyle = new GUIStyle();
        keyChipStyle.fontSize = 18;
        keyChipStyle.fontStyle = FontStyle.Bold;
        keyChipStyle.alignment = TextAnchor.MiddleCenter;
        keyChipStyle.normal.textColor = Color.white;
        keyChipStyle.normal.background = chipTex;
        keyChipStyle.border = new RectOffset(16, 16, 16, 16);

        descriptionStyle = new GUIStyle();
        descriptionStyle.fontSize = 24;
        descriptionStyle.alignment = TextAnchor.MiddleLeft;
        descriptionStyle.normal.textColor = Color.white;

        panelStyle = new GUIStyle();
        panelStyle.normal.background = panelTex;
        panelStyle.border = new RectOffset(24, 24, 24, 24);

        shadowStyle = new GUIStyle();
        shadowStyle.normal.background = shadowTex;
        shadowStyle.border = new RectOffset(24, 24, 24, 24);

        circleShadowStyle = new GUIStyle();
        circleShadowStyle.normal.background = circleShadowTex;
        circleShadowStyle.border = new RectOffset(30, 30, 30, 30);
    }

    void BuildTextures()
    {
        accentButtonTex = CreateRoundedRect(64, 64, 20f, AccentColor);
        accentButtonHoverTex = CreateRoundedRect(64, 64, 20f, AccentHoverColor);
        ghostButtonTex = CreateRoundedRect(64, 64, 20f, GhostColor);
        ghostButtonHoverTex = CreateRoundedRect(64, 64, 20f, GhostHoverColor);
        circleTex = CreateRoundedRect(64, 64, 32f, GhostColor);
        circleHoverTex = CreateRoundedRect(64, 64, 32f, GhostHoverColor);
        panelTex = CreateRoundedRect(64, 64, 24f, PanelColor);
        shadowTex = CreateRoundedRect(64, 64, 24f, ShadowColor);
        circleShadowTex = CreateRoundedRect(64, 64, 32f, ShadowColor);
        chipTex = CreateRoundedRect(64, 64, 14f, ChipColor);
        gradientTex = CreateVerticalGradient(256, new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0.35f));
        backArrowTex = CreateLeftArrowTexture(48, Color.white);
    }

    // A rounded rectangle drawn with a signed-distance field so the edge is
    // antialiased (not a hard, jagged pixel boundary) even after Unity
    // stretches this small texture up to fill a much bigger button. Built
    // with a fixed border (see GUIStyle.border above) so the corner radius
    // stays constant regardless of the final button size.
    static Texture2D CreateRoundedRect(int width, int height, float radius, Color color)
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

    static Texture2D CreateVerticalGradient(int height, Color top, Color bottom)
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

    // Draws a "<-" shape (a shaft plus a triangular head) out of plain
    // pixels rather than a font glyph, so the back button's icon can't be
    // affected by which characters a target platform's font happens to
    // include - see the comment on backArrowTex above.
    static Texture2D CreateLeftArrowTexture(int size, Color color)
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
}
