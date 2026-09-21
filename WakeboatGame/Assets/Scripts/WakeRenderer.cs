using UnityEngine;

public class WakeRenderer : MonoBehaviour
{
    public Transform boat;

    // Should match RiderController.wakeAngleDeg so the visible wake lines up
    // with where the rider actually launches.
    public float wakeAngleDeg = 19.47f;
    public float wakeLength = 60f;
    public float sternZOffset = -1.5f;

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

    // Foam along each wake edge, sparse near the boat and building up toward
    // the far end - which is also the end nearest the camera (the camera
    // trails even further behind the boat than the wake's near end), so
    // "builds up toward the far end" is the same thing as "builds up toward
    // the bottom of the screen".
    public int foamPointsPerSide = 14;
    public float foamRateNear = 0f;
    public float foamRateFar = 16f;
    public Color foamColor = new Color(0.85f, 0.92f, 1f, 0.8f);

    LineRenderer leftLine;
    LineRenderer rightLine;
    SplashEffect[] leftFoam;
    SplashEffect[] rightFoam;

    void Awake()
    {
        leftLine = CreateLine("WakeLeft");
        rightLine = CreateLine("WakeRight");
        leftFoam = CreateFoamRow("WakeFoamLeft");
        rightFoam = CreateFoamRow("WakeFoamRight");
    }

    SplashEffect[] CreateFoamRow(string rowName)
    {
        SplashEffect[] row = new SplashEffect[foamPointsPerSide];
        for (int i = 0; i < row.Length; i++)
        {
            GameObject go = new GameObject(rowName + i);
            go.transform.parent = transform;
            SplashEffect splash = go.AddComponent<SplashEffect>();
            splash.color = foamColor;
            splash.startSpeed = 0.6f;
            splash.startSize = 0.16f;
            splash.lifetime = 0.5f;
            splash.coneAngle = 30f;
            splash.gravityModifier = 1.5f;
            splash.sprayDirection = Vector3.up;
            row[i] = splash;
        }
        return row;
    }

    void UpdateFoamRow(SplashEffect[] row, Vector3 stern, Vector3 dir)
    {
        for (int i = 0; i < row.Length; i++)
        {
            // Sample the midpoint of each point's slice of the line so the
            // first point isn't sitting right on top of the boat's stern.
            float t = (i + 0.5f) / row.Length;
            row[i].transform.position = stern + dir * (t * wakeLength);
            row[i].continuousRate = Mathf.Lerp(foamRateNear, foamRateFar, t);
            row[i].SetContinuous(true);
        }
    }

    LineRenderer CreateLine(string lineName)
    {
        GameObject go = new GameObject(lineName);
        go.transform.parent = transform;
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.15f;
        line.endWidth = 0.15f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1f, 1f, 1f, 0.85f);
        line.endColor = new Color(1f, 1f, 1f, 0f);
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

        leftLine.SetPosition(0, stern);
        leftLine.SetPosition(1, stern + leftDir * wakeLength);

        rightLine.SetPosition(0, stern);
        rightLine.SetPosition(1, stern + rightDir * wakeLength);

        UpdateFoamRow(leftFoam, stern, leftDir);
        UpdateFoamRow(rightFoam, stern, rightDir);
    }
}
