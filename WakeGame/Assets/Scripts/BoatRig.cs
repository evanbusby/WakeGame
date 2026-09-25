using UnityEngine;

// Cosmetic detail added on top of the plain boat hull cube - a windshield,
// rub-rails, an engine cover, and seats. Uses the project's Custom/SimpleLit
// shader (a Standard-shader stand-in that survives WebGL build stripping,
// see SimpleLit.shader) for cheap, realistic-looking reflections, and keeps
// everything opaque (even the "glass" windshield, which just fakes the look
// with color and shininess) since transparency is expensive to render on
// low-end mobile GPUs.
public class BoatRig : MonoBehaviour
{
    static readonly Color HullColor = new Color(0.55f, 0.1f, 0.07f);
    static readonly Color WindshieldColor = new Color(0.75f, 0.85f, 0.88f);
    static readonly Color MetalColor = new Color(0.72f, 0.74f, 0.76f);
    static readonly Color SeatColor = new Color(0.15f, 0.14f, 0.13f);

    void Awake()
    {
        Build();
    }

    void Build()
    {
        // Re-materials the existing hull object rather than replacing it,
        // since other scripts (BoatMover, the rope, the wake, the rooster
        // tail) reference this transform directly.
        ApplyMaterial(gameObject, HullColor, metallic: 0.25f, smoothness: 0.55f);

        // The boat's own scale is stretched and non-uniform, and other
        // scripts depend on it staying that way. Detail parts are built
        // under a child object with the inverse scale, which cancels that
        // stretch back out to 1:1, so positions below can be authored in
        // normal real-world units.
        GameObject visuals = new GameObject("BoatVisuals");
        visuals.transform.SetParent(transform, false);
        Vector3 parentScale = transform.localScale;
        visuals.transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
        Transform v = visuals.transform;

        // Boat forward (+Z) is the bow (front): rub-rails run along the
        // hull's sides, the windshield sits ahead of the seats, and the
        // engine cover sits toward the stern (back).
        CreatePart(v, PrimitiveType.Cube, "Windshield",
            new Vector3(0f, 0.68f, 0.55f), Quaternion.Euler(-20f, 0f, 0f),
            new Vector3(1.3f, 0.5f, 0.06f), WindshieldColor, metallic: 0.1f, smoothness: 0.85f);

        CreatePart(v, PrimitiveType.Cube, "RubRailLeft",
            new Vector3(0.76f, 0.15f, 0f), Quaternion.identity,
            new Vector3(0.06f, 0.12f, 2.6f), MetalColor, metallic: 0.9f, smoothness: 0.6f);
        CreatePart(v, PrimitiveType.Cube, "RubRailRight",
            new Vector3(-0.76f, 0.15f, 0f), Quaternion.identity,
            new Vector3(0.06f, 0.12f, 2.6f), MetalColor, metallic: 0.9f, smoothness: 0.6f);

        CreatePart(v, PrimitiveType.Cube, "EngineCover",
            new Vector3(0f, 0.65f, -1.05f), Quaternion.identity,
            new Vector3(1.0f, 0.3f, 0.65f), MetalColor, metallic: 0.7f, smoothness: 0.5f);

        CreatePart(v, PrimitiveType.Cube, "SeatLeft",
            new Vector3(-0.35f, 0.75f, 0f), Quaternion.identity,
            new Vector3(0.4f, 0.35f, 0.45f), SeatColor, metallic: 0f, smoothness: 0.2f);
        CreatePart(v, PrimitiveType.Cube, "SeatRight",
            new Vector3(0.35f, 0.75f, 0f), Quaternion.identity,
            new Vector3(0.4f, 0.35f, 0.45f), SeatColor, metallic: 0f, smoothness: 0.2f);
    }

    void CreatePart(Transform parent, PrimitiveType type, string partName, Vector3 localPos, Quaternion localRot, Vector3 localScale, Color color, float metallic, float smoothness)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localRotation = localRot;
        part.transform.localScale = localScale;

        Collider col = part.GetComponent<Collider>();
        if (col != null) Destroy(col);

        ApplyMaterial(part, color, metallic, smoothness);
    }

    void ApplyMaterial(GameObject go, Color color, float metallic, float smoothness)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;

        Material mat = new Material(Shader.Find("Custom/SimpleLit"));
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);
        r.material = mat;
    }
}
