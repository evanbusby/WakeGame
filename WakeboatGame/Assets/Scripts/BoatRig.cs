using UnityEngine;

// Purely cosmetic detail on top of the existing boat hull cube (built and
// driven entirely by GameBootstrap/BoatMover) - a windshield, rub-rails,
// an engine cover, and two seats, each with its own PBR material. This
// project targets the Built-in Render Pipeline (confirmed via
// ProjectSettings/GraphicsSettings.asset - no custom render pipeline
// asset assigned), so Standard is a safe, direct shader name here and
// gets real specular highlights plus a cheap skybox-based reflection for
// free, with no reflection probes or real-time reflections to set up.
// Stays mobile-friendly by keeping geometry to a handful of extra
// primitive boxes (no imported meshes, no added draw-call-heavy effects)
// and using opaque materials throughout - even the windshield, which
// fakes "glass" with a light color and high smoothness rather than actual
// alpha transparency, since transparency/sorting overdraw is one of the
// pricier things to avoid on low-end mobile GPUs.
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
        // Re-materials the hull box (this same GameObject) rather than
        // replacing its geometry - BoatMover, the rope, the wake anchor
        // and the rooster tail all reference this transform/scale/
        // collider directly, so none of that is touched.
        ApplyMaterial(gameObject, HullColor, metallic: 0.25f, smoothness: 0.55f);

        // This transform's own scale (1.5, 1, 3) is non-uniform and load-
        // bearing for other scripts' hardcoded offsets (tow point, wake
        // anchor, rooster tail), so it can't change. Detail parts are
        // built under a child with the reciprocal scale instead, which
        // cancels the parent's scale back out to an effective 1:1 - so
        // the positions/sizes below can be authored directly in the
        // boat's own world-space half-extents (0.75 x 0.5 x 1.5) rather
        // than having to divide every number by (1.5, 1, 3) by hand.
        GameObject visuals = new GameObject("BoatVisuals");
        visuals.transform.SetParent(transform, false);
        Vector3 parentScale = transform.localScale;
        visuals.transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
        Transform v = visuals.transform;

        // Boat forward (+Z) is the bow; rub-rails run most of the hull's
        // length just below deck height, the windshield sits forward of
        // the seats and is raked back, and the engine cover sits toward
        // the stern (-Z).
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

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);
        r.material = mat;
    }
}
