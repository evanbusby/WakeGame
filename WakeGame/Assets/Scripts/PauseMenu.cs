using UnityEngine;
using UnityEngine.SceneManagement;

// In-game pause button and pause overlay. Pausing sets Time.timeScale to 0,
// which freezes every Time.deltaTime-driven behavior in the scene (the
// boat, rider, water/land scroll, camera) without touching OnGUI - OnGUI
// runs independently of timeScale - so the frozen game stays visible behind
// the overlay instead of needing to be hidden or faked.
public class PauseMenu : MonoBehaviour
{
    bool paused = false;

    Texture2D pauseIconTex;
    Texture2D circleTex;
    Texture2D circleHoverTex;
    Texture2D circleShadowTex;
    Texture2D accentButtonTex;
    Texture2D accentButtonHoverTex;
    Texture2D ghostButtonTex;
    Texture2D ghostButtonHoverTex;
    Texture2D panelTex;
    Texture2D shadowTex;
    Texture2D dimTex;

    GUIStyle pauseButtonStyle;
    GUIStyle circleShadowStyle;
    GUIStyle titleStyle;
    GUIStyle primaryButtonStyle;
    GUIStyle secondaryButtonStyle;
    GUIStyle panelStyle;
    GUIStyle shadowStyle;
    GUIStyle dimStyle;

    void Awake()
    {
        BuildTextures();
        BuildStyles();
    }

    void OnGUI()
    {
        if (!paused)
        {
            DrawPauseButton();
            return;
        }

        DrawPauseOverlay();
    }

    void DrawPauseButton()
    {
        float size = 60f;
        Rect rect = new Rect(24f, 24f, size, size);

        Rect shadowRect = rect;
        shadowRect.y += 6f;
        GUI.Box(shadowRect, GUIContent.none, circleShadowStyle);

        if (GUI.Button(rect, GUIContent.none, pauseButtonStyle))
        {
            SetPaused(true);
        }

        float iconSize = 22f;
        Rect iconRect = new Rect(rect.x + (rect.width - iconSize) / 2f, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize);
        GUI.DrawTexture(iconRect, pauseIconTex, ScaleMode.ScaleToFit, true);
    }

    void DrawPauseOverlay()
    {
        // A dimmed full-screen layer (not an opaque one) plus a centered
        // card, rather than a full-screen panel like the controls screen -
        // so the paused game stays visible at the edges instead of being
        // fully covered.
        GUI.Box(new Rect(0f, 0f, UnityEngine.Screen.width, UnityEngine.Screen.height), GUIContent.none, dimStyle);

        // Every size below is derived from the panel's own height (not fixed
        // pixel constants), the same technique used for the controls screen,
        // so the three buttons are always as large as the available space
        // allows while the layout math guarantees they can never overflow
        // past the bottom of the panel or the screen.
        float marginX = Mathf.Clamp(UnityEngine.Screen.width * 0.05f, 30f, 90f);
        float marginY = Mathf.Clamp(UnityEngine.Screen.height * 0.06f, 20f, 90f);

        float panelWidth = Mathf.Min(460f, UnityEngine.Screen.width - marginX * 2f);
        float panelHeight = Mathf.Min(560f, UnityEngine.Screen.height - marginY * 2f);

        float panelX = UnityEngine.Screen.width / 2f - panelWidth / 2f;
        float panelY = UnityEngine.Screen.height / 2f - panelHeight / 2f;

        Rect panelShadowRect = new Rect(panelX, panelY, panelWidth, panelHeight);
        panelShadowRect.y += 6f;
        GUI.Box(panelShadowRect, GUIContent.none, shadowStyle);
        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none, panelStyle);

        float topPadding = Mathf.Clamp(panelHeight * 0.06f, 18f, 40f);
        float bottomPadding = topPadding;
        float titleFontSize = Mathf.Clamp(panelHeight * 0.09f, 28f, 56f);
        float titleAreaHeight = titleFontSize + 34f;
        float gapBelowTitle = Mathf.Clamp(panelHeight * 0.05f, 14f, 32f);
        float spacing = Mathf.Clamp(panelHeight * 0.03f, 12f, 26f);

        const int buttonCount = 3;
        float buttonsAreaHeight = panelHeight - topPadding - titleAreaHeight - gapBelowTitle - bottomPadding - spacing * (buttonCount - 1);
        float buttonHeight = buttonsAreaHeight / buttonCount;
        float buttonFontSize = Mathf.Clamp(buttonHeight * 0.34f, 18f, 30f);
        float buttonWidth = Mathf.Min(360f, panelWidth - 80f);

        titleStyle.fontSize = Mathf.RoundToInt(titleFontSize);
        primaryButtonStyle.fontSize = Mathf.RoundToInt(buttonFontSize);
        secondaryButtonStyle.fontSize = Mathf.RoundToInt(buttonFontSize);

        GUI.Label(new Rect(panelX, panelY + topPadding, panelWidth, titleAreaHeight), "PAUSED", titleStyle);

        float buttonX = panelX + (panelWidth - buttonWidth) / 2f;
        float firstButtonY = panelY + topPadding + titleAreaHeight + gapBelowTitle;

        Rect continueRect = new Rect(buttonX, firstButtonY, buttonWidth, buttonHeight);
        Rect resetRect = new Rect(buttonX, firstButtonY + (buttonHeight + spacing), buttonWidth, buttonHeight);
        Rect mainMenuRect = new Rect(buttonX, firstButtonY + (buttonHeight + spacing) * 2f, buttonWidth, buttonHeight);

        DrawShadow(continueRect, shadowStyle);
        if (GUI.Button(continueRect, "CONTINUE", primaryButtonStyle))
        {
            SetPaused(false);
        }

        DrawShadow(resetRect, shadowStyle);
        if (GUI.Button(resetRect, "RESET", secondaryButtonStyle))
        {
            GameBootstrap.skipMenuOnLoad = true;
            ReloadScene();
        }

        DrawShadow(mainMenuRect, shadowStyle);
        if (GUI.Button(mainMenuRect, "MAIN MENU", secondaryButtonStyle))
        {
            ReloadScene();
        }
    }

    void DrawShadow(Rect rect, GUIStyle style)
    {
        rect.y += 6f;
        GUI.Box(rect, GUIContent.none, style);
    }

    void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = paused ? 0f : 1f;
    }

    void ReloadScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnDestroy()
    {
        // Guards against the scene reloading (or this object being torn
        // down) while paused, which would otherwise leave the next scene
        // permanently frozen at timeScale 0.
        Time.timeScale = 1f;
    }

    void BuildStyles()
    {
        pauseButtonStyle = new GUIStyle();
        pauseButtonStyle.normal.background = circleTex;
        pauseButtonStyle.hover.background = circleHoverTex;
        pauseButtonStyle.active.background = circleHoverTex;
        pauseButtonStyle.border = new RectOffset(30, 30, 30, 30);

        circleShadowStyle = new GUIStyle();
        circleShadowStyle.normal.background = circleShadowTex;
        circleShadowStyle.border = new RectOffset(30, 30, 30, 30);

        titleStyle = new GUIStyle();
        titleStyle.fontSize = 44;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        primaryButtonStyle = new GUIStyle();
        primaryButtonStyle.fontSize = 26;
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

        panelStyle = new GUIStyle();
        panelStyle.normal.background = panelTex;
        panelStyle.border = new RectOffset(24, 24, 24, 24);

        shadowStyle = new GUIStyle();
        shadowStyle.normal.background = shadowTex;
        shadowStyle.border = new RectOffset(24, 24, 24, 24);

        dimStyle = new GUIStyle();
        dimStyle.normal.background = dimTex;
    }

    void BuildTextures()
    {
        circleTex = MenuUI.CreateRoundedRect(64, 64, 32f, MenuUI.GhostColor);
        circleHoverTex = MenuUI.CreateRoundedRect(64, 64, 32f, MenuUI.GhostHoverColor);
        circleShadowTex = MenuUI.CreateRoundedRect(64, 64, 32f, MenuUI.ShadowColor);
        accentButtonTex = MenuUI.CreateRoundedRect(64, 64, 20f, MenuUI.AccentColor);
        accentButtonHoverTex = MenuUI.CreateRoundedRect(64, 64, 20f, MenuUI.AccentHoverColor);
        ghostButtonTex = MenuUI.CreateRoundedRect(64, 64, 20f, MenuUI.GhostColor);
        ghostButtonHoverTex = MenuUI.CreateRoundedRect(64, 64, 20f, MenuUI.GhostHoverColor);
        panelTex = MenuUI.CreateRoundedRect(64, 64, 24f, MenuUI.PanelColor);
        shadowTex = MenuUI.CreateRoundedRect(64, 64, 24f, MenuUI.ShadowColor);
        dimTex = MenuUI.CreateSolid(MenuUI.DimColor);
        pauseIconTex = MenuUI.CreatePauseIconTexture(32, Color.white);
    }
}
