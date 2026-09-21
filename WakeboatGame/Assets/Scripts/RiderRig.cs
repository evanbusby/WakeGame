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

    // Bundles a part with its standing-stance local transform (captured at
    // creation) and its raley ("superman") target, so RiderController can
    // just hand SetRaleyBlend a single 0-1 number and have every part
    // interpolate together.
    class PosedPart
    {
        public readonly Transform transform;
        public readonly Vector3 neutralPos;
        public readonly Quaternion neutralRot;
        public Vector3 raleyPos;
        public Quaternion raleyRot;

        public PosedPart(Transform t, Vector3 raleyPos, Vector3 raleyEuler)
        {
            transform = t;
            neutralPos = t.localPosition;
            neutralRot = t.localRotation;
            this.raleyPos = raleyPos;
            raleyRot = Quaternion.Euler(raleyEuler);
        }
    }

    PosedPart[] posedParts;

    // Referenced individually (in addition to sitting in posedParts) so
    // SetRaleyDirection can re-aim just these three per launch - see there.
    PosedPart boardPosed;
    PosedPart legLPosed;
    PosedPart legRPosed;

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
        GameObject board = CreatePart(PrimitiveType.Cube, "Board",
            new Vector3(0f, 0.04f, 0f), Quaternion.identity,
            new Vector3(0.5f, 0.08f, 1.8f), BoardColor);

        GameObject legL = CreateLimb("LegL", new Vector3(0f, 0.30f, 0.35f), Vector3.zero, new Vector3(0.16f, 0.22f, 0.16f), SkinColor);
        GameObject legR = CreateLimb("LegR", new Vector3(0f, 0.30f, -0.35f), Vector3.zero, new Vector3(0.16f, 0.22f, 0.16f), SkinColor);

        GameObject torso = CreatePart(PrimitiveType.Capsule, "Torso",
            new Vector3(0f, 0.78f, 0f), Quaternion.identity,
            new Vector3(0.20f, 0.26f, 0.26f), VestColor);

        GameObject armL = CreateLimb("ArmL", new Vector3(0f, 0.95f, 0.30f), new Vector3(-60f, 0f, 15f), new Vector3(0.11f, 0.20f, 0.11f), SkinColor);
        GameObject armR = CreateLimb("ArmR", new Vector3(0f, 0.95f, -0.30f), new Vector3(-60f, 0f, -15f), new Vector3(0.11f, 0.20f, 0.11f), SkinColor);

        GameObject head = CreatePart(PrimitiveType.Sphere, "Head",
            new Vector3(0f, 1.18f, 0f), Quaternion.identity,
            new Vector3(0.28f, 0.28f, 0.28f), SkinColor);

        // Raley target pose: torso/head/arms pitch forward and stretch out
        // ahead of the rider, hips/legs/board trail up and back behind -
        // the "flying superman" layout, laid out roughly along the travel
        // axis from reaching arms to trailing board.
        //
        // The board and legs are NOT rotated into the direction of travel.
        // The neutral stance already has the board's long axis along local
        // Z (the travel axis - see the comment above), which is correct for
        // normal riding but would make the raley read as a forward-facing
        // skate stance (front foot leading, board pointed at the boat) if
        // carried over unchanged. A raley keeps the normal sideways
        // stance - board long axis perpendicular to travel - the whole body
        // just lays out horizontal on top of it. So the board gets an extra
        // 90-degree YAW (around Y) to swing its long axis from Z onto X,
        // and the legs move apart along X instead of Z, putting both feet
        // side-by-side across the direction of travel instead of one ahead
        // of the other. Pitching that yawed assembly with an X-axis
        // rotation (as legs already were) doesn't disturb the X-aligned
        // board any further, since X-axis rotations leave X itself fixed -
        // so the board stays cross-wise no matter how far up and back the
        // whole assembly swings.
        //
        // The board sits noticeably further back (-1.6) than the legs
        // (-0.85) so their volumes don't overlap - they were close enough
        // before that the board (once yawed, a good deal wider than it is
        // thick) visually swallowed the leg capsules where they intersected.
        //
        // Board/legL/legR are kept as their own fields, not just entries in
        // posedParts, because their exact raley targets depend on which of
        // the two mirror-image heelside launches this is - SetRaleyDirection
        // (called once per launch, from RiderController) re-aims them.
        boardPosed = new PosedPart(board.transform, new Vector3(0f, 0.45f, -1.6f), new Vector3(0f, 90f, 0f));
        legLPosed = new PosedPart(legL.transform, new Vector3(0.5f, 0.55f, -0.85f), new Vector3(-40f, 0f, 0f));
        legRPosed = new PosedPart(legR.transform, new Vector3(-0.5f, 0.55f, -0.85f), new Vector3(-40f, 0f, 0f));

        posedParts = new PosedPart[]
        {
            boardPosed,
            legLPosed,
            legRPosed,
            new PosedPart(torso.transform, new Vector3(0f, 0.70f, 0.15f), new Vector3(70f, 0f, 0f)),
            new PosedPart(head.transform, new Vector3(0f, 0.85f, 0.65f), Vector3.zero),
            new PosedPart(armL.transform, new Vector3(0.08f, 0.80f, 1.0f), new Vector3(20f, 0f, 10f)),
            new PosedPart(armR.transform, new Vector3(-0.08f, 0.80f, 1.0f), new Vector3(20f, 0f, -10f)),
        };
    }

    // Re-aims the board/legs raley targets for whichever of the two
    // mirror-image heelside launches this one is (yawSign is the sign of
    // angularVelocity at the crossing - see RiderController.launchYawSign).
    // Flipping both the board's yaw and the legs' left/right target
    // together keeps "front foot left, back foot right" consistent for
    // both directions, and keeps the board rotating the same way the
    // rider's existing edge lean was already turning it, instead of a
    // fixed direction that's only continuous for one of the two and looks
    // like a 180 snap for the other.
    public void SetRaleyDirection(float yawSign)
    {
        boardPosed.raleyRot = Quaternion.Euler(0f, 90f * yawSign, 0f);
        legLPosed.raleyPos = new Vector3(0.5f * yawSign, legLPosed.raleyPos.y, legLPosed.raleyPos.z);
        legRPosed.raleyPos = new Vector3(-0.5f * yawSign, legRPosed.raleyPos.y, legRPosed.raleyPos.z);
    }

    // t=0 is the normal standing stance, t=1 is fully laid out in the raley.
    public void SetRaleyBlend(float t)
    {
        t = Mathf.Clamp01(t);
        foreach (PosedPart p in posedParts)
        {
            p.transform.localPosition = Vector3.Lerp(p.neutralPos, p.raleyPos, t);
            p.transform.localRotation = Quaternion.Slerp(p.neutralRot, p.raleyRot, t);
        }
    }

    GameObject CreateLimb(string limbName, Vector3 localPos, Vector3 localEuler, Vector3 localScale, Color color)
    {
        return CreatePart(PrimitiveType.Capsule, limbName, localPos, Quaternion.Euler(localEuler), localScale, color);
    }

    GameObject CreatePart(PrimitiveType type, string partName, Vector3 localPos, Quaternion localRot, Vector3 localScale, Color color)
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

        return part;
    }
}
