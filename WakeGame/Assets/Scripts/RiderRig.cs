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

    // Pairs a body part with its normal standing pose and its raley
    // ("superman") target pose, so the whole rig can be blended between the
    // two with one number.
    class PosedPart
    {
        public readonly Transform transform;
        public readonly Vector3 neutralPos;
        public readonly Quaternion neutralRot;
        public Vector3 raleyPos;
        public Quaternion raleyRot;

        // The pose SetDynamicPose currently wants this part in.
        // SetRaleyBlend eases from here toward the raley pose, rather than
        // from a fixed neutral, so a raley flows out of whatever pose the
        // rider was already in.
        public Vector3 basePos;
        public Quaternion baseRot;

        public PosedPart(Transform t, Vector3 raleyPos, Vector3 raleyEuler)
        {
            transform = t;
            neutralPos = t.localPosition;
            neutralRot = t.localRotation;
            this.raleyPos = raleyPos;
            raleyRot = Quaternion.Euler(raleyEuler);
            basePos = neutralPos;
            baseRot = neutralRot;
        }
    }

    PosedPart[] posedParts;

    // Kept as individual fields (not just entries in posedParts) so
    // SetRaleyDirection/SetDynamicPose can re-aim specific parts.
    PosedPart boardPosed;
    PosedPart legLPosed;
    PosedPart legRPosed;
    PosedPart torsoPosed;
    PosedPart headPosed;
    PosedPart armLPosed;
    PosedPart armRPosed;

    void Awake()
    {
        Build();
    }

    void Build()
    {
        // Real wakeboarders ride sideways - feet spread front-to-back along
        // the board, not side-by-side across it - so the stance here is
        // built along the travel axis (Z) instead of across it.
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

        // The raley ("superman") pose: torso/head/arms reach forward, hips/
        // legs/board trail up and back. The board and legs get an extra
        // 90-degree turn so the board stays crosswise (the normal riding
        // stance) instead of pointing forward like a skateboard - only the
        // body's pitch changes, not the board's sideways orientation. The
        // board sits further back than the legs so their shapes don't
        // visually overlap.
        //
        // Board/legL/legR are kept as separate fields (not just entries in
        // posedParts) because their raley targets mirror depending on which
        // of the two heelside launch directions this is - see
        // SetRaleyDirection.
        boardPosed = new PosedPart(board.transform, new Vector3(0f, 0.45f, -1.6f), new Vector3(0f, 90f, 0f));
        legLPosed = new PosedPart(legL.transform, new Vector3(0.5f, 0.55f, -0.85f), new Vector3(-40f, 0f, 0f));
        legRPosed = new PosedPart(legR.transform, new Vector3(-0.5f, 0.55f, -0.85f), new Vector3(-40f, 0f, 0f));

        torsoPosed = new PosedPart(torso.transform, new Vector3(0f, 0.70f, 0.15f), new Vector3(70f, 0f, 0f));
        headPosed = new PosedPart(head.transform, new Vector3(0f, 0.85f, 0.65f), Vector3.zero);
        armLPosed = new PosedPart(armL.transform, new Vector3(0.08f, 0.80f, 1.0f), new Vector3(20f, 0f, 10f));
        armRPosed = new PosedPart(armR.transform, new Vector3(-0.08f, 0.80f, 1.0f), new Vector3(20f, 0f, -10f));

        posedParts = new PosedPart[]
        {
            boardPosed, legLPosed, legRPosed, torsoPosed, headPosed, armLPosed, armRPosed,
        };
    }

    // A simple, always-on reaction to turning/airborne/landing state - bent
    // knees, a forward torso lean, a counter-roll for the upper body, and
    // reaching arms - layered underneath the raley pose rather than fighting
    // it.
    //   crouch: 0 standing, 1 deepest bend.
    //   torsoPitchDeg: forward lean into the turn.
    //   counterRollDeg: upper body rolls opposite the whole-body lean, so it
    //     reads as more upright than the hard-leaning legs/board.
    //   armExtend: 0 relaxed, 1 reaching.
    public void SetDynamicPose(float crouch, float torsoPitchDeg, float counterRollDeg, float armExtend)
    {
        crouch = Mathf.Clamp01(crouch);
        armExtend = Mathf.Clamp01(armExtend);

        Vector3 legOffset = new Vector3(0f, -crouch * 0.10f, 0f);
        Quaternion legRot = Quaternion.Euler(crouch * 8f, 0f, 0f);
        legLPosed.basePos = legLPosed.neutralPos + legOffset;
        legLPosed.baseRot = legRot;
        legRPosed.basePos = legRPosed.neutralPos + legOffset;
        legRPosed.baseRot = legRot;

        torsoPosed.basePos = torsoPosed.neutralPos + new Vector3(0f, -crouch * 0.08f, 0f);
        torsoPosed.baseRot = Quaternion.Euler(torsoPitchDeg, 0f, counterRollDeg);

        headPosed.basePos = headPosed.neutralPos + new Vector3(0f, -crouch * 0.08f, 0f);
        headPosed.baseRot = Quaternion.Euler(0f, 0f, counterRollDeg * 0.6f);

        float armPitch = Mathf.Lerp(-60f, -42f, armExtend);
        float armSpread = Mathf.Lerp(15f, 10f, armExtend);
        armLPosed.basePos = armLPosed.neutralPos + new Vector3(0f, -crouch * 0.05f, armExtend * 0.12f);
        armLPosed.baseRot = Quaternion.Euler(armPitch, 0f, armSpread + counterRollDeg);
        armRPosed.basePos = armRPosed.neutralPos + new Vector3(0f, -crouch * 0.05f, -armExtend * 0.12f);
        armRPosed.baseRot = Quaternion.Euler(armPitch, 0f, -armSpread + counterRollDeg);
    }

    // Mirrors the board/leg raley targets for whichever of the two heelside
    // launch directions this is, so the pose always rotates the same way the
    // rider's existing lean was already turning, instead of snapping
    // backward for one of the two directions.
    public void SetRaleyDirection(float yawSign)
    {
        boardPosed.raleyRot = Quaternion.Euler(0f, 90f * yawSign, 0f);
        legLPosed.raleyPos = new Vector3(0.5f * yawSign, legLPosed.raleyPos.y, legLPosed.raleyPos.z);
        legRPosed.raleyPos = new Vector3(-0.5f * yawSign, legRPosed.raleyPos.y, legRPosed.raleyPos.z);
    }

    // t=0 is the current pose from SetDynamicPose (call that first each
    // frame), t=1 is fully laid out in the raley.
    public void SetRaleyBlend(float t)
    {
        t = Mathf.Clamp01(t);
        foreach (PosedPart p in posedParts)
        {
            p.transform.localPosition = Vector3.Lerp(p.basePos, p.raleyPos, t);
            p.transform.localRotation = Quaternion.Slerp(p.baseRot, p.raleyRot, t);
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
