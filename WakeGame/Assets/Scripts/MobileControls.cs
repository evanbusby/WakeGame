using UnityEngine;

// On-screen touch controls for mobile/touch browsers only: a steering
// joystick (bottom-left, horizontal-only) and a trick joystick (bottom-
// right, free 2D - not locked to one axis the way the steering stick is),
// plus tapping anywhere else on the screen to bunny hop. IsMobile is false
// on a desktop browser (no touch support), and this component draws and
// reads nothing when it's false, so none of this can be seen or used on a
// computer.
//
// Exposes its state as static fields that RiderController ORs in alongside
// its existing Input.GetKey(...)/GetKeyDown(...) checks, so desktop and
// mobile drive the exact same gameplay code with no separate mobile-only
// physics path.
public class MobileControls : MonoBehaviour
{
    public static bool IsMobile => Application.isMobilePlatform || Input.touchSupported;

    public static bool SteerLeft { get; private set; }
    public static bool SteerRight { get; private set; }
    public static bool SpinLeft { get; private set; }
    public static bool SpinRight { get; private set; }
    public static bool SpinLeftDown { get; private set; }
    public static bool SpinRightDown { get; private set; }
    public static bool LeanForward { get; private set; }
    public static bool LeanBack { get; private set; }

    static bool bunnyHopRequested;

    // Consumed exactly once per request (mirrors Input.GetKeyDown, which is
    // also only true for a single frame per press).
    public static bool ConsumeBunnyHop()
    {
        if (!bunnyHopRequested) return false;
        bunnyHopRequested = false;
        return true;
    }

    // How far across the stick's radius a touch has to move before it
    // counts as "pressed" - the rest of the game only reads these as
    // held-or-not (like a keyboard key), not as an analog value, so this is
    // a deadzone/threshold rather than a sensitivity curve.
    const float PressThreshold = 0.35f;

    class Stick
    {
        public Vector2 center;
        public float radius;
        public bool horizontalOnly;
        public int touchId = -1;
        public Vector2 offset;
    }

    readonly Stick steerStick = new Stick { horizontalOnly = true };
    readonly Stick trickStick = new Stick();

    bool prevSpinLeft;
    bool prevSpinRight;

    Texture2D baseTex;
    Texture2D knobTex;

    void Awake()
    {
        baseTex = MenuUI.CreateRoundedRect(128, 128, 64f, new Color(1f, 1f, 1f, 0.16f));
        knobTex = MenuUI.CreateRoundedRect(128, 128, 64f, new Color(1f, 1f, 1f, 0.4f));
    }

    void Update()
    {
        if (!IsMobile || PauseMenu.IsPaused)
        {
            ResetState();
            return;
        }

        LayoutSticks();
        ProcessTouches();
        UpdateOutputs();
    }

    void LayoutSticks()
    {
        float radius = Mathf.Clamp(UnityEngine.Screen.height * 0.11f, 64f, 110f);
        float margin = radius * 1.3f;

        steerStick.radius = radius;
        steerStick.center = new Vector2(margin, UnityEngine.Screen.height - margin);

        trickStick.radius = radius;
        trickStick.center = new Vector2(UnityEngine.Screen.width - margin, UnityEngine.Screen.height - margin);
    }

    void ProcessTouches()
    {
        Rect pauseRect = PauseMenu.PauseButtonRect;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            // Touch positions have a bottom-left origin; OnGUI/Rects use a
            // top-left origin. Flipping Y once here keeps every comparison
            // below in that same top-left space as the drawn joysticks.
            Vector2 pos = new Vector2(touch.position.x, UnityEngine.Screen.height - touch.position.y);

            if (touch.phase == TouchPhase.Began)
            {
                if (pauseRect.Contains(pos)) continue;

                if (steerStick.touchId < 0 && Vector2.Distance(pos, steerStick.center) <= steerStick.radius)
                {
                    steerStick.touchId = touch.fingerId;
                    steerStick.offset = Vector2.zero;
                    continue;
                }

                if (trickStick.touchId < 0 && Vector2.Distance(pos, trickStick.center) <= trickStick.radius)
                {
                    trickStick.touchId = touch.fingerId;
                    trickStick.offset = Vector2.zero;
                    continue;
                }

                bunnyHopRequested = true;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                UpdateStickOffset(steerStick, touch.fingerId, pos);
                UpdateStickOffset(trickStick, touch.fingerId, pos);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                ReleaseStick(steerStick, touch.fingerId);
                ReleaseStick(trickStick, touch.fingerId);
            }
        }
    }

    void UpdateStickOffset(Stick stick, int fingerId, Vector2 pos)
    {
        if (stick.touchId != fingerId) return;

        Vector2 raw = pos - stick.center;
        if (stick.horizontalOnly) raw.y = 0f;
        stick.offset = Vector2.ClampMagnitude(raw, stick.radius);
    }

    void ReleaseStick(Stick stick, int fingerId)
    {
        if (stick.touchId != fingerId) return;
        stick.touchId = -1;
        stick.offset = Vector2.zero;
    }

    void UpdateOutputs()
    {
        Vector2 steerNorm = steerStick.offset / steerStick.radius;
        SteerLeft = steerNorm.x < -PressThreshold;
        SteerRight = steerNorm.x > PressThreshold;

        Vector2 trickNorm = trickStick.offset / trickStick.radius;

        bool newSpinLeft = trickNorm.x < -PressThreshold;
        bool newSpinRight = trickNorm.x > PressThreshold;
        SpinLeftDown = newSpinLeft && !prevSpinLeft;
        SpinRightDown = newSpinRight && !prevSpinRight;
        prevSpinLeft = newSpinLeft;
        prevSpinRight = newSpinRight;
        SpinLeft = newSpinLeft;
        SpinRight = newSpinRight;

        // Screen space Y grows downward, so dragging the stick up (visually
        // "forward") produces a negative offset.
        LeanForward = trickNorm.y < -PressThreshold;
        LeanBack = trickNorm.y > PressThreshold;
    }

    void ResetState()
    {
        SteerLeft = false;
        SteerRight = false;
        SpinLeft = false;
        SpinRight = false;
        SpinLeftDown = false;
        SpinRightDown = false;
        LeanForward = false;
        LeanBack = false;
        prevSpinLeft = false;
        prevSpinRight = false;
        bunnyHopRequested = false;

        steerStick.touchId = -1;
        steerStick.offset = Vector2.zero;
        trickStick.touchId = -1;
        trickStick.offset = Vector2.zero;
    }

    void OnGUI()
    {
        if (!IsMobile || PauseMenu.IsPaused) return;

        DrawStick(steerStick);
        DrawStick(trickStick);
    }

    void DrawStick(Stick stick)
    {
        float baseSize = stick.radius * 2f;
        Rect baseRect = new Rect(stick.center.x - stick.radius, stick.center.y - stick.radius, baseSize, baseSize);
        GUI.DrawTexture(baseRect, baseTex, ScaleMode.StretchToFill, true);

        float knobSize = stick.radius * 0.9f;
        Vector2 knobCenter = stick.center + stick.offset;
        Rect knobRect = new Rect(knobCenter.x - knobSize / 2f, knobCenter.y - knobSize / 2f, knobSize, knobSize);
        GUI.DrawTexture(knobRect, knobTex, ScaleMode.StretchToFill, true);
    }
}
