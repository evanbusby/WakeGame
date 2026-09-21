using UnityEngine;

// A lightweight, code-only stand-in for the boat's wake: no fluid sim, no
// per-frame mesh deformation - just a handful of LineRenderer ribbons
// sharing one procedurally-generated foam texture, matching the rest of
// this project's "no imported textures/assets" style (see WaterScroll).
// Purely cosmetic - RiderController has its own independent wakeAngleDeg
// for where the rider actually launches, so nothing here touches physics.
public class WakeRenderer : MonoBehaviour
{
    public Transform boat;

    // Should match RiderController.wakeAngleDeg so the visible wake lines up
    // with where the rider actually launches.
    public float wakeAngleDeg = 19.47f;
    public float wakeLength = 60f;
    public float sternZOffset = -1.5f;

    // The boat's immediate prop-wash trough: a wide, short patch of foam
    // running straight back from the stern (not diverging like the two
    // side trails), roughly covering the area a rider actually rides and
    // jumps from.
    public float centralWakeLength = 18f;
    public float centralWakeWidth = 3f;

    // The wake decal must sit at the water surface, not at the boat's own
    // height. boat.position.y (0.5) is the hull's height above the water,
    // and the rider's board rests just above the water too (baseHeight in
    // RiderController is 0.05), so if the wake reused boat.position.y it
    // would float ~0.5 units above both the water and the rider - genuinely
    // closer to an elevated, looking-down camera, so depth testing would
    // (correctly, given that wrong height) draw it in front of the board and
    // rider instead of under them. Anchoring to a fixed water height instead
    // of boat.position.y keeps it below the rider so the rider always
    // occludes it.
    //
    // It sits a hair above the water plane's own y=0 (rather than exactly on
    // it) because two coplanar surfaces at identical depth flicker between
    // frames as floating-point rounding randomly picks the winner each draw -
    // that's the "blinking". A small, deliberate gap makes the wake
    // unambiguously above the water while staying comfortably below the
    // rider's board.
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

        // Narrow where it peels off the hull, quickly fanning out - the
        // classic V-wake spreading-and-fading-with-distance look.
        AnimationCurve trailWidth = new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 1.6f));
        leftTrail = CreateRibbon("WakeTrailLeft", 0.6f, trailWidth,
            new Color(1f, 1f, 1f, 0.8f), new Color(1f, 1f, 1f, 0f));
        rightTrail = CreateRibbon("WakeTrailRight", 0.6f, trailWidth,
            new Color(1f, 1f, 1f, 0.8f), new Color(1f, 1f, 1f, 0f));

        // Wide right behind the boat, tapering as the churn settles - the
        // bigger foam patch the rider actually rides and jumps out of.
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

        // Default (View) alignment billboards the ribbon to face the camera,
        // which tilts it up off the water toward an angled third-person
        // camera - making it stick up and cover the board/rider instead of
        // lying flat on the surface. TransformZ alignment keeps the ribbon's
        // width in the plane perpendicular to this transform's local Z axis,
        // so pointing that axis straight up keeps the ribbon flat on the
        // water (in the horizontal XZ plane) regardless of camera angle.
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

    // A patchy white-noise alpha mask (two blended Perlin octaves) rather
    // than a flat color, so the foam reads as uneven clumps of whitewater
    // instead of a smooth painted stripe.
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
