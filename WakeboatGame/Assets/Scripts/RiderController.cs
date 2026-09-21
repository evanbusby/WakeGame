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

    // The wake is the raised wall of water trailing off the boat's stern at
    // roughly the Kelvin wake angle (~19.47 degrees for a deep-water wake).
    // Since the rider's swing is measured as an angle from directly-behind
    // the boat, and the wake edges are straight lines from the same origin
    // at that angle, crossing the wake is simply |angle| crossing
    // wakeAngleDeg - no separate distance math needed.
    public float wakeAngleDeg = 19.47f;
    public float baseHeight = 0.05f;

    // The wake acts like a ramp: it redirects some of the rider's carving
    // momentum upward instead of just adding height on top of it, so takeoff
    // horizontal speed is lower than on-water carve speed while vertical
    // speed is higher. horizontalCoastScale is how much carve speed survives
    // as horizontal glide once airborne; jumpVelocityScale converts takeoff
    // angular speed into vertical launch speed. Both are tuned together so
    // that carving in at roughly the fastest speed the pendulum reaches
    // (~1.2 rad/s) still covers about 1.5x the wake's width (wakeWidth =
    // 2 * ropeLength * sin(wakeAngleDeg) =~ 8 units here, so ~12 units of
    // distance) while getting noticeably more air under it.
    public float horizontalCoastScale = 0.7f;
    public float jumpVelocityScale = 5f;
    public float gravity = 9.81f;

    public SplashEffect boardSplash;
    public int landingSplashCount = 24;

    float angle = 0f;
    float angularVelocity = 0f;
    float prevAngle = 0f;
    float verticalVelocity = 0f;
    float airHeight = 0f;

    void Update()
    {
        if (boat == null) return;

        bool isAirborne = airHeight > 0f;

        float inputSign = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) inputSign -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) inputSign += 1f;

        if (!isAirborne)
        {
            // Edge input and water resistance only exist while the board is
            // actually in the water. Once airborne there's nothing left to
            // carve against, so the swing just coasts at its takeoff speed -
            // this is what keeps a jump's horizontal distance bounded by
            // gravity and airtime instead of growing with continued input.
            bool reversing = inputSign != 0f &&
                (Mathf.Abs(angularVelocity) < 0.01f || inputSign != Mathf.Sign(angularVelocity));
            float appliedSteerAccel = reversing ? directionChangeAccel : steerAccel;
            float inputAccel = inputSign * appliedSteerAccel;

            float restoring = -restoringAccel * Mathf.Sin(angle);
            angularVelocity += (inputAccel + restoring) * Time.deltaTime;
            angularVelocity *= Mathf.Clamp01(1f - damping * Time.deltaTime);
        }

        angle += angularVelocity * Time.deltaTime;

        float safetyMaxAngleRad = safetyMaxAngleDeg * Mathf.Deg2Rad;
        if (angle > safetyMaxAngleRad) { angle = safetyMaxAngleRad; angularVelocity = 0f; }
        if (angle < -safetyMaxAngleRad) { angle = -safetyMaxAngleRad; angularVelocity = 0f; }

        if (!isAirborne)
        {
            float wakeAngleRad = wakeAngleDeg * Mathf.Deg2Rad;
            bool wasOutsideWake = Mathf.Abs(prevAngle) > wakeAngleRad;
            bool isOutsideWake = Mathf.Abs(angle) > wakeAngleRad;
            if (wasOutsideWake != isOutsideWake)
            {
                // Launch height scales with how fast the rider was turning at
                // the moment they hit the wake - a slow drift over barely gets
                // any air, a hard cut into it launches them.
                verticalVelocity = Mathf.Abs(angularVelocity) * jumpVelocityScale;

                // The ramp trades some of that carve speed for height rather
                // than adding height for free, so the horizontal coast slows
                // down for the remainder of the flight.
                angularVelocity *= horizontalCoastScale;
            }
        }
        prevAngle = angle;

        airHeight += verticalVelocity * Time.deltaTime;
        verticalVelocity -= gravity * Time.deltaTime;
        if (airHeight < 0f)
        {
            airHeight = 0f;
            verticalVelocity = 0f;
        }

        if (boardSplash != null)
        {
            bool nowGrounded = airHeight <= 0f;
            boardSplash.SetContinuous(nowGrounded);
            if (isAirborne && nowGrounded)
            {
                boardSplash.Burst(landingSplashCount);
            }
        }

        float lateralOffset = Mathf.Sin(angle) * ropeLength;
        float forwardDistance = Mathf.Cos(angle) * ropeLength;

        Vector3 towPoint = boat.position + boat.up * towPointOffset.y + boat.forward * towPointOffset.z;
        Vector3 targetPos = towPoint - boat.forward * forwardDistance + boat.right * lateralOffset;
        targetPos.y = baseHeight + airHeight;
        transform.position = targetPos;

        float tilt = Mathf.Clamp(-angle * Mathf.Rad2Deg, -40f, 40f);
        transform.rotation = Quaternion.Euler(0f, boat.eulerAngles.y, tilt);
    }
}
