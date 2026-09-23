using UnityEngine;

// A code-only stand-in for the boat's wake - just a few line-renderer
// ribbons with a procedural foam texture, no fluid simulation. Purely
// visual: RiderController has its own wakeAngleDeg for where the rider
// actually launches, so nothing here touches physics.
public class WakeRenderer : MonoBehaviour
{
    public Transform boat;

    // Should match RiderController.wakeAngleDeg so the visible wake lines up
    // with where the rider actually launches.
    public float wakeAngleDeg = 19.47f;
    public float wakeLength = 60f;
    public float sternZOffset = -1.5f;

    // The boat's immediate wake trough - a wide, short foam patch running
    // straight back from the stern, covering the area the rider actually
    // rides and jumps from.
    public float centralWakeLength = 18f;
    public float centralWakeWidth = 3f;

    // Anchored to a fixed water height rather than the boat's own height,
    // since the boat sits well above the water while the rider's board sits
    // right at it - using the boat's height would draw the wake in front of
    // the rider instead of underneath them.
    //
    // Sits slightly above the water's actual surface rather than exactly on
    // it, since two perfectly overlapping flat surfaces flicker between
    // frames (floating-point rounding).
    public float waterHeight = 0.02f;

    // How fast the foam texture's UV drifts along each ribbon's length, for
    // a cheap sense of the foam churning/flowing rather than sitting static.
    public float foamScrollSpeed = 0.6f;

    // Points per ribbon - just enough for the width/fade curves below to
    // read as smooth tapers rather than a single straight-sided wedge.
    const int RibbonResolution = 8;

    Material foamMaterial;
    LineRenderer leftTrail;
    LineRenderer rightTrail;
    LineRenderer centralWake;
    Vector2 foamOffset;

    void Awake()
    {
        foamMaterial = new Material(Shader.Find("Sprites/Default"));
        foamMaterial.mainTexture = CreateFoamTexture();
        foamMaterial.mainTextureScale = new Vector2(1f, 6f);

        // Narrow near the hull, fanning out with distance - the classic
        // V-wake look.
        AnimationCurve trailWidth = new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 1.6f));
        leftTrail = CreateRibbon("WakeTrailLeft", 0.6f, trailWidth,
            new Color(1f, 1f, 1f, 0.8f), new Color(1f, 1f, 1f, 0f));
        rightTrail = CreateRibbon("WakeTrailRight", 0.6f, trailWidth,
            new Color(1f, 1f, 1f, 0.8f), new Color(1f, 1f, 1f, 0f));

        // Wide right behind the boat, tapering as the churn settles.
        AnimationCurve centralWidth = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.4f, 0.85f),
            new Keyframe(1f, 0.3f));
        centralWake = CreateRibbon("WakeCentral", centralWakeWidth, centralWidth,
            new Color(1f, 1f, 1f, 0.9f), new Color(1f, 1f, 1f, 0f));
    }

    LineRenderer CreateRibbon(string rendererName, float widthMultiplier, AnimationCurve widthCurve, Color startColor, Color endColor)
    {
        GameObject go = new GameObject(rendererName);
        go.transform.parent = transform;

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.positionCount = RibbonResolution;
        line.material = foamMaterial;
        line.widthMultiplier = widthMultiplier;
        line.widthCurve = widthCurve;
        line.textureMode = LineTextureMode.Tile;
        line.numCapVertices = 2;
        line.startColor = startColor;
        line.endColor = endColor;
        line.useWorldSpace = true;

        // Default billboard alignment would tilt the ribbon to face the
        // camera, lifting it off the water. TransformZ alignment keeps it
        // flat on the water instead, regardless of camera angle.
        line.alignment = LineAlignment.TransformZ;
        go.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);

        return line;
    }

    void LateUpdate()
    {
        if (boat == null) return;

        Vector3 stern = boat.position + boat.forward * sternZOffset;
        stern.y = waterHeight;

        float rad = wakeAngleDeg * Mathf.Deg2Rad;
        Vector3 leftDir = (-boat.forward * Mathf.Cos(rad) - boat.right * Mathf.Sin(rad)).normalized;
        Vector3 rightDir = (-boat.forward * Mathf.Cos(rad) + boat.right * Mathf.Sin(rad)).normalized;

        SetRibbonPoints(leftTrail, stern, leftDir, wakeLength);
        SetRibbonPoints(rightTrail, stern, rightDir, wakeLength);
        SetRibbonPoints(centralWake, stern, -boat.forward, centralWakeLength);

        foamOffset.y -= foamScrollSpeed * Time.deltaTime;
        foamMaterial.mainTextureOffset = foamOffset;
    }

    static void SetRibbonPoints(LineRenderer line, Vector3 origin, Vector3 direction, float length)
    {
        for (int i = 0; i < RibbonResolution; i++)
        {
            float t = i / (float)(RibbonResolution - 1);
            line.SetPosition(i, origin + direction * (t * length));
        }
    }

    // Blended noise so the foam reads as uneven clumps of whitewater rather
    // than a smooth painted stripe.
    Texture2D CreateFoamTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float coarse = Mathf.PerlinNoise(x * 0.12f, y * 0.12f);
                float fine = Mathf.PerlinNoise(x * 0.4f + 50f, y * 0.4f + 50f);
                float foam = Mathf.Clamp01(coarse * 0.65f + fine * 0.45f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, foam));
            }
        }

        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }
}
