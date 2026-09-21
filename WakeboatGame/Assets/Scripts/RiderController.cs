using UnityEngine;

public class RiderController : MonoBehaviour
{
    public Transform boat;
    public float ropeLength = 12f;
    public Vector3 towPointOffset = new Vector3(0f, 0.55f, -1.5f);

    // The rider swings on the rope like a pendulum: "restoringAccel" is the
    // gravity-analog that always pulls them back toward directly behind the
    // boat, growing with sin(angle) - so it barely resists a small carve but
    // strongly resists a wide one. "steerAccel" is the edge power the player
    // adds while holding a direction. Because steerAccel < restoringAccel,
    // the carve settles into a natural max angle instead of ever reaching
    // 90 degrees - matching how much harder it gets to carve wider.
    // The pendulum's period scales with 1/sqrt(restoringAccel); these values
    // are tuned so a full side-to-side cut takes a few seconds, matching the
    // cadence of a real slalom/wakeboard cut across the wake rather than a
    // near-instant snap.
    public float restoringAccel = 0.75f;
    public float steerAccel = 0.6f;
    public float damping = 0.4f;
    public float safetyMaxAngleDeg = 85f;

    // A real edge change (flipping from toe-side to heel-side) is quick even
    // though carving the resulting arc out wide is slow. Model that as a
    // stronger acceleration that only applies while steering against the
    // current swing direction; once the swing actually turns to match the
    // input, it drops back to the normal, slow steerAccel.
    public float directionChangeAccel = 2.4f;

    float angle = 0f;
    float angularVelocity = 0f;

    void Update()
    {
        if (boat == null) return;

        float inputSign = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) inputSign -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) inputSign += 1f;

        bool reversing = inputSign != 0f &&
            (Mathf.Abs(angularVelocity) < 0.01f || inputSign != Mathf.Sign(angularVelocity));
        float appliedSteerAccel = reversing ? directionChangeAccel : steerAccel;
        float inputAccel = inputSign * appliedSteerAccel;

        float restoring = -restoringAccel * Mathf.Sin(angle);
        angularVelocity += (inputAccel + restoring) * Time.deltaTime;
        angularVelocity *= Mathf.Clamp01(1f - damping * Time.deltaTime);

        angle += angularVelocity * Time.deltaTime;

        float safetyMaxAngleRad = safetyMaxAngleDeg * Mathf.Deg2Rad;
        if (angle > safetyMaxAngleRad) { angle = safetyMaxAngleRad; angularVelocity = 0f; }
        if (angle < -safetyMaxAngleRad) { angle = -safetyMaxAngleRad; angularVelocity = 0f; }

        float lateralOffset = Mathf.Sin(angle) * ropeLength;
        float forwardDistance = Mathf.Cos(angle) * ropeLength;

        Vector3 towPoint = boat.position + boat.up * towPointOffset.y + boat.forward * towPointOffset.z;
        Vector3 targetPos = towPoint - boat.forward * forwardDistance + boat.right * lateralOffset;
        targetPos.y = transform.position.y;
        transform.position = targetPos;

        float tilt = Mathf.Clamp(-angle * Mathf.Rad2Deg, -40f, 40f);
        transform.rotation = Quaternion.Euler(0f, boat.eulerAngles.y, tilt);
    }
}
