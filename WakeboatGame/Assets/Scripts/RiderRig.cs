using UnityEngine;

public class RiderRig : MonoBehaviour
{
    // Local-space point (relative to this rig's root, which RiderController drives)
    // where the tow rope should visually attach - roughly the rider's hands.
    // Exposed so GameBootstrap can wire RopeRenderer.riderOffset without duplicating numbers.
    public Vector3 handOffset = new Vector3(0f, 1.0f, 0.55f);

    static readonly Color SkinColor = new Color(0.96f, 0.8f, 0.65f);
    static readonly Color VestColor = new Color(1f, 0.6f, 0.1f);
    static readonly Color BoardColor = new Color(0.8f, 0.15f, 0.15f);

    void Awake()
    {
        Build();
    }

    void Build()
    {
        // Local +Z is the direction of travel (RiderController aligns this rig's
        // rotation with the boat's heading). A real wakeboarder rides sideways -
        // feet spread front-to-back along the board's length, not side-by-side
        // across it - so the stance is built along Z instead of X, with the
        // torso's shoulder line (its wide axis) rotated to match: Z instead of X.
        CreatePart(PrimitiveType.Cube, "Board",
            new Vector3(0f, 0.04f, 0f), Quaternion.identity,
            new Vector3(0.5f, 0.08f, 1.8f), BoardColor);

        CreateLimb("LegL", new Vector3(0f, 0.30f, 0.35f), Vector3.zero, new Vector3(0.16f, 0.22f, 0.16f), SkinColor);
        CreateLimb("LegR", new Vector3(0f, 0.30f, -0.35f), Vector3.zero, new Vector3(0.16f, 0.22f, 0.16f), SkinColor);

        CreatePart(PrimitiveType.Capsule, "Torso",
            new Vector3(0f, 0.78f, 0f), Quaternion.identity,
            new Vector3(0.20f, 0.26f, 0.26f), VestColor);

        CreateLimb("ArmL", new Vector3(0f, 0.95f, 0.30f), new Vector3(-60f, 0f, 15f), new Vector3(0.11f, 0.20f, 0.11f), SkinColor);
        CreateLimb("ArmR", new Vector3(0f, 0.95f, -0.30f), new Vector3(-60f, 0f, -15f), new Vector3(0.11f, 0.20f, 0.11f), SkinColor);

        CreatePart(PrimitiveType.Sphere, "Head",
            new Vector3(0f, 1.18f, 0f), Quaternion.identity,
            new Vector3(0.28f, 0.28f, 0.28f), SkinColor);
    }

    void CreateLimb(string limbName, Vector3 localPos, Vector3 localEuler, Vector3 localScale, Color color)
    {
        CreatePart(PrimitiveType.Capsule, limbName, localPos, Quaternion.Euler(localEuler), localScale, color);
    }

    void CreatePart(PrimitiveType type, string partName, Vector3 localPos, Quaternion localRot, Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPos;
        part.transform.localRotation = localRot;
        part.transform.localScale = localScale;

        Collider col = part.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer r = part.GetComponent<Renderer>();
        if (r != null) r.material.color = color;
    }
}
