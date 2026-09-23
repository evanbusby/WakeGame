using UnityEngine;

public class RiderController : MonoBehaviour
{
    public Transform boat;
    public float ropeLength = 12f;
    public Vector3 towPointOffset = new Vector3(0f, 0.55f, -1.5f);

    // The rider swings side-to-side on the rope like a pendulum. restoringAccel
    // pulls them back toward directly behind the boat, harder the wider they
    // swing; steerAccel is how hard the player can push against that pull
    // while turning ("carving"). Since steerAccel is weaker, a turn settles at
    // a natural max angle instead of ever reaching 90 degrees, and the values
    // are tuned so a full side-to-side turn takes a few seconds, like a real
    // wakeboard cut.
    public float restoringAccel = 0.75f;
    public float steerAccel = 0.6f;
    public float damping = 0.4f;
    public float safetyMaxAngleDeg = 85f;

    // Switching edges (which way the board is tipped) happens quickly in real
    // wakeboarding, even though the resulting wide turn is slow. This stronger
    // acceleration only kicks in while steering against the current swing
    // direction, then eases back to the normal steerAccel once the turn
    // catches up.
    public float directionChangeAccel = 2.4f;

    // The pendulum swing above is slow, real momentum. But how hard the board
    // is tipped (the visual "lean") is a quick muscular choice, so it snaps to
    // match input immediately instead of trailing the swing - this is what
    // lets a rider square the board back up right before launching, even
    // mid-turn. Lean is frozen once airborne, same as the pendulum, since
    // there's no water to edge against in the air.
    public float edgeResponseRate = 4.5f;
    public float maxLeanDeg = 40f;
    public float maxBoardYawDeg = 25f;
    float edgeLean = 0f;

    // The wake is the V-shaped ridge of water the boat pushes up behind it,
    // roughly 19.47 degrees off straight-back for a boat in deep water. Since
    // the rider's swing is already measured as an angle from directly behind
    // the boat, crossing the wake is just that angle passing wakeAngleDeg.
    public float wakeAngleDeg = 19.47f;
    public float baseHeight = 0.05f;

    // The wake acts like a ramp, converting some of the rider's sideways
    // turning speed into height rather than adding height for free - so
    // takeoff is slower sideways but launches upward. horizontalCoastScale
    // keeps some of that speed as glide in the air; jumpVelocityScale turns it
    // into vertical launch speed. Only their product actually controls how
    // far the jump travels, so the split between them can be tuned for a
    // higher, floatier jump without changing the total distance.
    public float horizontalCoastScale = 0.5385f;
    public float jumpVelocityScale = 6.5f;
    public float gravity = 9.81f;

    public SplashEffect boardSplash;
    public int landingSplashCount = 24;

    // A manual hop off flat water, with no wake involved - just a fixed
    // upward launch that reuses the same in-air and landing logic as a real
    // wake jump, so it can still be spun and gets a landing splash.
    public float bunnyHopVelocity = 3f;

    // A spin is the rider rotating around their vertical axis, anywhere from a
    // quick 180 to a full 720. In the air, holding the spin key keeps
    // rotating at a constant rate for as long as it's held, so the player
    // decides when to stop and land. On the water there's nothing to spin
    // against, so a surface spin is a quick, fixed 180 - turning around to
    // ride facing backward ("switch").
    public float spinRateDegPerSec = 300f;
    public float groundSpinDuration = 0.3f;

    // Landing an air spin at an odd angle isn't a deliberate stance, it's just
    // wherever the rotation ran out. Once grounded, spinDeg drifts toward
    // whichever "clean" stance is closer - facing forward (regular) or
    // backward (switch) - slowly enough to read as the rider naturally
    // settling rather than snapping into place.
    public float stanceRecoveryRateDegPerSec = 60f;

    float spinDeg = 0f;
    bool groundSpinning = false;
    float groundSpinTimer = 0f;
    float groundSpinStartDeg = 0f;
    float groundSpinTargetDeg = 0f;

    // Whether the rider is currently facing forward (regular) or backward
    // (switch) - only updated while grounded. Freezing it during a spin keeps
    // the takeoff lean rotating smoothly with the body instead of snapping
    // every time the spin crosses the regular/switch boundary.
    float stanceSign = 1f;

    // A flip's rotation speed is set once, at the moment it starts, to
    // exactly whatever speed completes a full 360 by the time the rider is
    // calculated to land - so flips always finish right as they touch down,
    // regardless of jump size.
    bool isFlipping = false;
    float flipSpinDeg = 0f;
    float flipSpeedDegPerSec = 0f;

    // A raley (holding Up+Down together) is a held "superman" pose, not a
    // spin: the rider lays out flat in the air, holds it, then pulls their
    // legs back under them to land - timed like the flip above, so it always
    // finishes on landing. How far the rider extends into the pose depends on
    // how hard they were turning at takeoff: a marginal turn only pops
    // partway in, a full commitment gets a full extension. Below
    // raleyMinCarveSpeed there isn't enough speed to attempt it at all.
    public float raleyMinCarveSpeed = 0.35f;
    public float raleyFullExtendCarveSpeed = 1.0f;
    float launchCarveSpeed = 0f;

    // A raley only works riding "heelside" - leaning back on your heels
    // rather than your toes - which depends on which way the rider is
    // turning relative to their stance, not which side of the boat the
    // crossing happens on. Combined with only inward wake crossings launching
    // at all, a regular-stance rider earns a heelside raley only by turning
    // left, and a switch-stance rider only by turning right.
    bool launchIsHeelside = false;

    // Which lateral direction the rider was actually turning at takeoff. Used
    // so the raley pose rotates the same way the rider's existing lean was
    // already turning them, instead of snapping the opposite way for one of
    // the two stances.
    float launchYawSign = 1f;

    bool isRaleying = false;
    float raleyTimer = 0f;
    float raleyExtendDuration = 0f;
    float raleyHoldDuration = 0f;
    float raleyRecoverDuration = 0f;
    float raleyPeakBlend = 0f;

    // A lightweight, always-on pose layer, separate from the trick mechanics
    // above: it bends the knees, leans the torso into turns, and reaches the
    // arms, so the rider looks physically connected to the board and rope. It
    // runs every frame, including during flips/raleys/spins, and a raley
    // eases out of whatever pose this layer already had rather than snapping
    // from a rigid default.
    public float referenceCarveSpeed = 1.2f;
    public float maxTorsoPitchDeg = 12f;
    public float counterRollFraction = 0.35f;
    public float baseCrouch = 0.12f;
    public float carveCrouchExtra = 0.35f;
    public float airborneCrouch = 0.28f;
    public float landingImpactCrouch = 0.45f;
    public float landingImpactDuration = 0.2f;
    float landingImpactTimer = 0f;

    float angle = 0f;
    float angularVelocity = 0f;
    float prevAngle = 0f;
    float verticalVelocity = 0f;
    float airHeight = 0f;

    RiderRig rig;

    void Awake()
    {
        rig = GetComponent<RiderRig>();
    }

    void Update()
    {
        if (boat == null) return;

        bool isAirborne = airHeight > 0f;

        float inputSign = 0f;
        if (Input.GetKey(KeyCode.A)) inputSign -= 1f;
        if (Input.GetKey(KeyCode.D)) inputSign += 1f;

        if (isAirborne)
        {
            // Holding a direction keeps rotating for as long as it's held, so
            // the player controls the exact spin amount by feel.
            groundSpinning = false;

            float spinInput = 0f;
            if (Input.GetKey(KeyCode.LeftArrow)) spinInput -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) spinInput += 1f;
            spinDeg += spinInput * spinRateDegPerSec * Time.deltaTime;
        }
        else if (groundSpinning)
        {
            // Once a surface 180 starts, it runs to completion regardless of
            // new input - there's no stopping halfway on the water.
            groundSpinTimer += Time.deltaTime;
            float t = Mathf.Clamp01(groundSpinTimer / groundSpinDuration);
            spinDeg = Mathf.Lerp(groundSpinStartDeg, groundSpinTargetDeg, t);
            if (t >= 1f) groundSpinning = false;
        }
        else if (inputSign == 0f)
        {
            // Can't turn and spin on the water at the same time, so only
            // start a surface spin when the player isn't actively steering.
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

        if (!isAirborne && !groundSpinning)
        {
            float nearestStanceDeg = Mathf.Round(spinDeg / 180f) * 180f;
            spinDeg = Mathf.MoveTowards(spinDeg, nearestStanceDeg, stanceRecoveryRateDegPerSec * Time.deltaTime);
        }

        if (!isAirborne)
        {
            stanceSign = Mathf.Cos(spinDeg * Mathf.Deg2Rad) >= 0f ? 1f : -1f;

            // Steering and water resistance only apply while the board is in
            // the water. Once airborne, the swing just coasts at takeoff
            // speed, which keeps jump distance limited by gravity and airtime
            // instead of growing with more input.
            bool reversing = inputSign != 0f &&
                (Mathf.Abs(angularVelocity) < 0.01f || inputSign != Mathf.Sign(angularVelocity));
            float appliedSteerAccel = reversing ? directionChangeAccel : steerAccel;
            float inputAccel = inputSign * appliedSteerAccel;

            float restoring = -restoringAccel * Mathf.Sin(angle);
            angularVelocity += (inputAccel + restoring) * Time.deltaTime;
            angularVelocity *= Mathf.Clamp01(1f - damping * Time.deltaTime);

            edgeLean = Mathf.MoveTowards(edgeLean, inputSign, edgeResponseRate * Time.deltaTime);
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
            // Only crossing the wake from outside to in launches a jump -
            // turning out past the wake just rolls over it with no pop.
            if (wasOutsideWake && !isOutsideWake)
            {
                // Height scales with how fast the rider was turning when they
                // hit the wake - a lazy drift barely gets any air, a hard
                // turn launches high.
                verticalVelocity = Mathf.Abs(angularVelocity) * jumpVelocityScale;

                // Captured before horizontalCoastScale below, so it reflects
                // the real turning speed at takeoff.
                launchCarveSpeed = Mathf.Abs(angularVelocity);

                // Heelside/toeside depends on which way the rider is turning,
                // mirrored by stance - not which side of the boat the
                // crossing happens on.
                bool movingLeft = angularVelocity < 0f;
                launchIsHeelside = movingLeft == (stanceSign > 0f);
                launchYawSign = Mathf.Sign(angularVelocity);

                // The ramp trades some turning speed for height rather than
                // adding height for free, so horizontal speed slows down for
                // the rest of the flight.
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
                landingImpactTimer = landingImpactDuration;
            }
        }

        if (nowGrounded)
        {
            isFlipping = false;
            flipSpinDeg = 0f;
            isRaleying = false;
            raleyTimer = 0f;
            // Only reset by an actual wake crossing, so a plain bunny hop
            // can't inherit leftover values from an earlier jump and unlock a
            // raley it didn't earn.
            launchCarveSpeed = 0f;
            launchIsHeelside = false;
        }
        else if (!isFlipping && !isRaleying)
        {
            bool frontInput = Input.GetKey(KeyCode.UpArrow);
            bool backInput = Input.GetKey(KeyCode.DownArrow);
            // Both keys held together is always treated as a raley attempt
            // (or nothing, if the launch didn't qualify) so it can't fall
            // through to the single-key flip case below.
            if (frontInput && backInput)
            {
                if (launchIsHeelside && launchCarveSpeed >= raleyMinCarveSpeed)
                {
                    float remainingAirTime = RemainingAirTime();
                    raleyExtendDuration = remainingAirTime * 0.2f;
                    raleyHoldDuration = remainingAirTime * 0.55f;
                    raleyRecoverDuration = remainingAirTime * 0.25f;
                    raleyPeakBlend = Mathf.Clamp01(Mathf.InverseLerp(raleyMinCarveSpeed, raleyFullExtendCarveSpeed, launchCarveSpeed));
                    raleyTimer = 0f;
                    isRaleying = true;
                    if (rig != null) rig.SetRaleyDirection(launchYawSign);
                }
            }
            else if (frontInput || backInput)
            {
                float remainingAirTime = RemainingAirTime();

                // stanceSign is frozen at takeoff (see its field comment
                // above), keeping this direction constant even if an air spin
                // later crosses the regular/switch boundary.
                float direction = (frontInput ? -1f : 1f) * stanceSign;
                flipSpeedDegPerSec = direction * 360f / remainingAirTime;
                isFlipping = true;
            }
        }

        if (isFlipping)
        {
            flipSpinDeg += flipSpeedDegPerSec * Time.deltaTime;
        }

        // tilt is mirrored by stanceSign so a switch-stance rider leans
        // toward whichever side they're actually steering, not the mirror
        // image. boardYaw doesn't need that same mirroring: the board is a
        // symmetric box, and spinDeg's own 180 already swaps its ends for
        // free when riding switch.
        float tilt = -edgeLean * maxLeanDeg * stanceSign;
        float boardYaw = edgeLean * maxBoardYawDeg;

        // Drives how deep the knees bend, how far the torso pitches into the
        // turn, and how far the arms extend. Frozen along with
        // angularVelocity/edgeLean once airborne, so a hard-carved jump keeps
        // its aggressive lean through the whole flight instead of relaxing
        // mid-air.
        float carveIntensity = Mathf.Clamp01(Mathf.Abs(angularVelocity) / referenceCarveSpeed);

        // A brief extra crouch on touchdown that eases back to normal - the
        // rider absorbing the landing instead of stopping dead.
        landingImpactTimer = Mathf.Max(0f, landingImpactTimer - Time.deltaTime);
        float landingImpactFactor = landingImpactDuration > 0f
            ? landingImpactCrouch * (landingImpactTimer / landingImpactDuration)
            : 0f;

        float crouch = baseCrouch + carveCrouchExtra * carveIntensity;
        if (!nowGrounded) crouch = Mathf.Max(crouch, airborneCrouch);
        crouch = Mathf.Clamp01(crouch + landingImpactFactor);

        float torsoPitchDeg = maxTorsoPitchDeg * carveIntensity;

        // Counters part of the whole-body lean above so the torso stays more
        // upright than the legs/board, like a real rider counter-balancing a
        // hard turn.
        float counterRollDeg = -tilt * counterRollFraction;

        if (rig != null) rig.SetDynamicPose(crouch, torsoPitchDeg, counterRollDeg, carveIntensity);

        float raleyBlend = 0f;
        if (isRaleying)
        {
            raleyTimer += Time.deltaTime;
            raleyBlend = RaleyBlend();
        }
        if (rig != null) rig.SetRaleyBlend(raleyBlend);

        float lateralOffset = Mathf.Sin(angle) * ropeLength;
        float forwardDistance = Mathf.Cos(angle) * ropeLength;

        Vector3 towPoint = boat.position + boat.up * towPointOffset.y + boat.forward * towPointOffset.z;
        Vector3 targetPos = towPoint - boat.forward * forwardDistance + boat.right * lateralOffset;
        targetPos.y = baseHeight + airHeight;
        transform.position = targetPos;

        transform.rotation = Quaternion.Euler(0f, boat.eulerAngles.y + spinDeg + boardYaw, tilt + flipSpinDeg);
    }

    // How much longer the current jump has left in the air, from basic
    // projectile math. Shared by the flip and the raley so both trick
    // timings always fit the actual airtime.
    float RemainingAirTime()
    {
        float discriminant = Mathf.Max(verticalVelocity * verticalVelocity + 2f * gravity * airHeight, 0f);
        float remainingAirTime = (verticalVelocity + Mathf.Sqrt(discriminant)) / gravity;
        return Mathf.Max(remainingAirTime, 0.001f);
    }

    // 0 = standing, raleyPeakBlend = how far this jump earned into the pose.
    // Ramps up, holds, then ramps back down so the rider's feet are back
    // under them exactly as they land.
    float RaleyBlend()
    {
        if (raleyTimer < raleyExtendDuration)
        {
            float t = raleyExtendDuration > 0f ? raleyTimer / raleyExtendDuration : 1f;
            return t * raleyPeakBlend;
        }

        float afterExtend = raleyTimer - raleyExtendDuration;
        if (afterExtend < raleyHoldDuration)
        {
            return raleyPeakBlend;
        }

        float recoverElapsed = afterExtend - raleyHoldDuration;
        float recoverT = raleyRecoverDuration > 0f ? Mathf.Clamp01(1f - recoverElapsed / raleyRecoverDuration) : 0f;
        return recoverT * raleyPeakBlend;
    }
}
