using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public GameBootstrap bootstrap;

    enum Screen { Main, Controls, Started }

    Camera menuCamera;
    Screen screen = Screen.Main;

    void Awake()
    {
        // A plain solid-color camera so the menu isn't shown over a blank
        // black screen before the real gameplay camera exists.
        GameObject camObj = new GameObject("MenuCamera");
        menuCamera = camObj.AddComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.SolidColor;
        menuCamera.backgroundColor = new Color(0.45f, 0.72f, 0.95f);
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
        float buttonWidth = 220f;
        float buttonHeight = 50f;
        float centerX = UnityEngine.Screen.width / 2f - buttonWidth / 2f;
        float playY = UnityEngine.Screen.height * 0.45f;
        float controlsY = playY + buttonHeight + 15f;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 36;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(0f, UnityEngine.Screen.height * 0.25f, UnityEngine.Screen.width, 60f), "Wakeboard Game", titleStyle);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 20;

        if (GUI.Button(new Rect(centerX, playY, buttonWidth, buttonHeight), "Play", buttonStyle))
        {
            StartGame();
        }

        if (GUI.Button(new Rect(centerX, controlsY, buttonWidth, buttonHeight), "Controls", buttonStyle))
        {
            screen = Screen.Controls;
        }
    }

    void DrawControlsOverlay()
    {
        // Covers the whole screen so every line is visible at once, with no
        // need to scroll regardless of screen size.
        GUI.Box(new Rect(0f, 0f, UnityEngine.Screen.width, UnityEngine.Screen.height), GUIContent.none);

        GUIStyle backStyle = new GUIStyle(GUI.skin.button);
        backStyle.fontSize = 24;
        if (GUI.Button(new Rect(20f, 20f, 60f, 50f), "←", backStyle))
        {
            screen = Screen.Main;
        }

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 32;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(0f, UnityEngine.Screen.height * 0.16f, UnityEngine.Screen.width, 50f), "Controls", titleStyle);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 20;
        textStyle.alignment = TextAnchor.MiddleLeft;
        textStyle.normal.textColor = Color.white;

        string controlsText =
            "A - Steer left\n" +
            "D - Steer right\n" +
            "Space - Jump\n" +
            "Left Arrow - Spin left\n" +
            "Right Arrow - Spin right\n" +
            "Up Arrow - Flip forward\n" +
            "Down Arrow - Flip backward";

        float textWidth = 360f;
        float textHeight = 260f;
        Rect textRect = new Rect(
            UnityEngine.Screen.width / 2f - textWidth / 2f,
            UnityEngine.Screen.height / 2f - textHeight / 2f,
            textWidth,
            textHeight);
        GUI.Label(textRect, controlsText, textStyle);
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
}
