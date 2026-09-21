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

    // A manual hop, independent of the wake - just a fixed vertical launch
    // speed while riding on flat water. It reuses the same airborne coasting
    // and splash logic as a wake jump, so bunny-hopping still gets a landing
    // splash and can be spun like any other jump.
    public float bunnyHopVelocity = 3f;

    // Spin (a surface or air 360/540/720, handle pass and all) is a body
    // rotation around the vertical axis, layered on top of the boat-heading
    // yaw the rider always tracks. In the air it's direct manual control:
    // holding a spin key accumulates rotation at a constant rate for as long
    // as it's held, so the player - not the game - decides whether they stop
    // at a 360, a 540, or a 720. On the water there's no edge to keep
    // spinning against, so a surface spin is a quick, quantized 180 - a
    // discrete "turn around and ride switch" rather than a freely-stoppable
    // rotation - and it isn't reset on landing/afterward: like in real
    // wakeboarding, the rotation becomes the rider's new stance (regular
    // after an even multiple of 180 degrees, switch after an odd one).
    public float spinRateDegPerSec = 300f;
    public float groundSpinDuration = 0.3f;
    public RopeRenderer rope;
    float spinDeg = 0f;
    bool groundSpinning = false;
    float groundSpinTimer = 0f;
    float groundSpinStartDeg = 0f;
    float groundSpinTargetDeg = 0f;

    // A flip's rotation speed is set once, at the moment it's triggered, to
    // exactly (360 degrees / time left in the air) - computed from the
    // current vertical velocity and height via basic projectile kinematics.
    // That guarantees the flip always completes exactly as the rider lands,
    // regardless of how big or small the jump is. The rider stands sideways
    // on the board (shoulder line along the direction of travel, local Z),
    // so a real front/backflip rotates around that same Z axis, exactly like
    // the carve lean (tilt) already does.
    bool isFlipping = false;
    float flipSpinDeg = 0f;
    float flipSpeedDegPerSec = 0f;

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
        if (Input.GetKey(KeyCode.A)) inputSign -= 1f;
        if (Input.GetKey(KeyCode.D)) inputSign += 1f;

        float spinDegBefore = spinDeg;

        if (isAirborne)
        {
            // Free analog air spin: holding a direction accumulates rotation
            // for as long as it's held, so the player controls the exact
            // amount by feel.
            groundSpinning = false;

            float spinInput = 0f;
            if (Input.GetKey(KeyCode.LeftArrow)) spinInput -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) spinInput += 1f;
            spinDeg += spinInput * spinRateDegPerSec * Time.deltaTime;
        }
        else if (groundSpinning)
        {
            // Already committed to a surface 180 - run it to completion
            // regardless of what's held now, so it can't be stopped partway
            // (no 90s on the water).
            groundSpinTimer += Time.deltaTime;
            float t = Mathf.Clamp01(groundSpinTimer / groundSpinDuration);
            spinDeg = Mathf.Lerp(groundSpinStartDeg, groundSpinTargetDeg, t);
            if (t >= 1f) groundSpinning = false;
        }
        else if (inputSign == 0f)
        {
            // Only start a surface spin when not actively steering - you
            // can't carve and spin on the water at the same time.
            float groundSpinDir = 0f;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) groundSpinDir = -1f;
            else if (Input.GetKeyDown(KeyCode.RightArrow)) groundSpinDir = 1f;

            if (groundSpinDir != 0f)
            {
                groundSpinning = true;
                groundSpinTimer = 0f;
                groundSpinStartDeg = spinDeg;
                groundSpinTargetDeg = spinDeg + groundSpinDir * 180f;
            }
        }

        if (rope != null)
        {
            int crossingsBefore = Mathf.FloorToInt(Mathf.Abs(spinDegBefore) / 360f);
            int crossingsNow = Mathf.FloorToInt(Mathf.Abs(spinDeg) / 360f);
            if (crossingsNow != crossingsBefore)
            {
                rope.TriggerPass();
            }
        }

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
            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = bunnyHopVelocity;
            }

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

        bool nowGrounded = airHeight <= 0f;

        if (boardSplash != null)
        {
            boardSplash.SetContinuous(nowGrounded);
            if (isAirborne && nowGrounded)
            {
                boardSplash.Burst(landingSplashCount);
            }
        }

        if (nowGrounded)
        {
            isFlipping = false;
            flipSpinDeg = 0f;
        }
        else if (!isFlipping)
        {
            bool frontInput = Input.GetKey(KeyCode.UpArrow);
            bool backInput = Input.GetKey(KeyCode.DownArrow);
            if (frontInput || backInput)
            {
                float discriminant = Mathf.Max(verticalVelocity * verticalVelocity + 2f * gravity * airHeight, 0f);
                float remainingAirTime = (verticalVelocity + Mathf.Sqrt(discriminant)) / gravity;
                remainingAirTime = Mathf.Max(remainingAirTime, 0.001f);

                float direction = frontInput ? -1f : 1f;
                flipSpeedDegPerSec = direction * 360f / remainingAirTime;
                isFlipping = true;
            }
        }

        if (isFlipping)
        {
            flipSpinDeg += flipSpeedDegPerSec * Time.deltaTime;
        }

        float lateralOffset = Mathf.Sin(angle) * ropeLength;
        float forwardDistance = Mathf.Cos(angle) * ropeLength;

        Vector3 towPoint = boat.position + boat.up * towPointOffset.y + boat.forward * towPointOffset.z;
        Vector3 targetPos = towPoint - boat.forward * forwardDistance + boat.right * lateralOffset;
        targetPos.y = baseHeight + airHeight;
        transform.position = targetPos;

        float tilt = Mathf.Clamp(-angle * Mathf.Rad2Deg, -40f, 40f);
        transform.rotation = Quaternion.Euler(0f, boat.eulerAngles.y + spinDeg, tilt + flipSpinDeg);
    }
}
