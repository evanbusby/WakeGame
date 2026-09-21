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

    // The pendulum angle above is the physical arc - it's slow, since it's
    // real momentum swinging out against the rope. But the rider's actual
    // edge (how hard the board is tipped and pointed) is a muscular choice,
    // not momentum, so it has to be able to change fast: press a direction
    // and the board snaps onto that edge and angles its nose into the turn;
    // let go and it snaps back flat, independent of wherever the physical
    // swing has actually gotten to. That's what lets a hard cut toward the
    // wake be squared back up at the last second before launch - the rider
    // keeps carving out on the pendulum, but stands the board back up flat
    // just before crossing, so the jump itself isn't facing the boat.
    // edgeLean only tracks input while grounded (frozen once airborne, same
    // as the pendulum - there's no edge to hold once you're in the air).
    public float edgeResponseRate = 4.5f;
    public float maxLeanDeg = 40f;
    public float maxBoardYawDeg = 25f;
    float edgeLean = 0f;

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
    // angular speed into vertical launch speed.
    //
    // Horizontal distance covered in the air works out to
    // 2 * angularVelocity^2 * horizontalCoastScale * jumpVelocityScale / gravity
    // (airtime * horizontal speed, with airtime = 2*Vy/gravity) - so it
    // depends only on the PRODUCT of these two scales, not on how it's split
    // between them. That product is kept the same as the original tuning
    // (0.7 * 5 = 3.5) so the jump's horizontal reach is unchanged, while
    // shifting the split further toward jumpVelocityScale trades some of
    // that same carve speed for a higher, floatier launch (height scales
    // with jumpVelocityScale^2, airtime scales with jumpVelocityScale) -
    // more pop and hang time without touching gravity or the distance a
    // given carve covers.
    public float horizontalCoastScale = 0.5385f;
    public float jumpVelocityScale = 6.5f;
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
    // at a 360, a 540, or a 720 - and lands facing wherever that rotation
    // happens to end, exactly like a real dismount. On the water there's no
    // edge to keep spinning against, so a surface spin is a quick, quantized
    // 180 - a discrete "turn around and ride switch" rather than a
    // freely-stoppable rotation.
    public float spinRateDegPerSec = 300f;
    public float groundSpinDuration = 0.3f;

    // Landing an air spin at an odd angle (a slightly-short 360, a 450) isn't
    // a deliberate stance choice, it's just wherever the rotation ran out -
    // so once grounded (and not already mid-way through the deliberate
    // ground-spin above), spinDeg drifts back toward whichever real riding
    // stance it's actually closer to: the nearest multiple of 180, which is
    // regular (0) or switch (180), never both - landing closer to switch
    // settles into riding switch, not forced back to regular. Only a
    // landing stuck genuinely sideways (near 90/270, equidistant from both)
    // gets nudged to whichever is nearest. The rate is slow enough to read
    // as the rider settling/squaring up naturally rather than snapping into
    // place.
    public float stanceRecoveryRateDegPerSec = 60f;

    float spinDeg = 0f;
    bool groundSpinning = false;
    float groundSpinTimer = 0f;
    float groundSpinStartDeg = 0f;
    float groundSpinTargetDeg = 0f;

    // Which stance (regular/switch) tilt and flips get corrected for. Only
    // re-derived while grounded - an air spin keeps spinDeg sweeping
    // continuously through the regular/switch boundary (every 90 degrees of
    // rotation), and re-deriving this every frame would make the frozen
    // takeoff lean (edgeLean also only updates on the ground) snap back and
    // forth as the spin crosses that boundary, instead of just rotating
    // smoothly with the rest of the body. Freezing it at whatever it was at
    // takeoff keeps the whole airborne rotation rigid and continuous.
    float stanceSign = 1f;

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

    // A raley (holding UpArrow+DownArrow together) is a held pose, not a
    // spin: the rider lays out into a superman shape, holds it, then draws
    // their legs back under themselves to land. Its three phases (extend
    // into the pose / hold it / recover to a landing stance) are sized as
    // fractions of however much airtime is actually left at the moment
    // it's triggered - same "always finishes exactly as the rider lands"
    // trick the flip timing uses just above - so it never gets caught
    // stretched out mid-pose by the water. Mutually exclusive with
    // isFlipping; the rider commits to one trick or the other per jump.
    // How committed the raley is scales with how hard the rider was
    // carving at the moment they left the wake (launchCarveSpeed, captured
    // pre-horizontalCoastScale so it's the same "~1.2 rad/s fastest the
    // pendulum reaches" reference the jump-height tuning above uses) - a
    // marginal cut only pops partway into the pose, a full commitment gets
    // the full superman extension. Below raleyMinCarveSpeed the trick isn't
    // available at all: not enough of a cut to be worth attempting.
    public float raleyMinCarveSpeed = 0.35f;
    public float raleyFullExtendCarveSpeed = 1.0f;
    float launchCarveSpeed = 0f;

    // A raley is heelside-only. Heelside/toeside is about which lateral
    // direction the rider is actually carving (the sign of angularVelocity)
    // relative to their stance, not which wake line or which side of the
    // boat this particular crossing happens to be on - riding one edge
    // covers an entire swing from one apex through center to the other,
    // and only changes when the carve direction itself reverses, same as
    // real wakeboarding. Combined with only inward crossings launching at
    // all (see the crossing check below), a regular-stance rider's heelside
    // raley can only ever come from carving left (crossing the right wake
    // edge inward); switch mirrors it (carving right, crossing the left
    // edge). See where this gets set for the exact rule.
    bool launchIsHeelside = false;

    // The sign of angularVelocity at the crossing - which lateral direction
    // the rider is actually carving. Regular and switch riders earn a
    // heelside raley from opposite carve directions (see launchIsHeelside
    // above), so the raley pose needs this to know which way to yaw the
    // board/legs: rotating the same direction the rider's existing edge
    // lean was already turning them, rather than a fixed direction that's
    // only correct for one of the two stances and looks like a reversal/180
    // snap into the pose for the other.
    float launchYawSign = 1f;

    bool isRaleying = false;
    float raleyTimer = 0f;
    float raleyExtendDuration = 0f;
    float raleyHoldDuration = 0f;
    float raleyRecoverDuration = 0f;
    float raleyPeakBlend = 0f;

    // A lightweight pose layer, separate from the trick mechanics above: it
    // reads the same physics state (carve speed, edge lean, airborne state,
    // landing) to bend the knees, lean the torso, counter-roll the upper
    // body against the carve lean, and extend the arms - so the rider
    // looks physically connected to the board and rope without changing
    // any of the simulation above. It runs unconditionally every frame,
    // including during flips/raley/spins: RiderRig.SetRaleyBlend blends
    // FROM this pose rather than from a fixed neutral, so a raley eases
    // out of whatever natural stance the rider was already in instead of
    // snapping from a rigid default, and a flip (which only ever rotates
    // the whole rig's root, never individual parts) just carries this
    // pose's crouch/lean along with it for free.
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

        if (!isAirborne && !groundSpinning)
        {
            float nearestStanceDeg = Mathf.Round(spinDeg / 180f) * 180f;
            spinDeg = Mathf.MoveTowards(spinDeg, nearestStanceDeg, stanceRecoveryRateDegPerSec * Time.deltaTime);
        }

        if (!isAirborne)
        {
            stanceSign = Mathf.Cos(spinDeg * Mathf.Deg2Rad) >= 0f ? 1f : -1f;

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
            // Only the outside-to-in crossing launches - carving out past
            // the wake on the way out just rides over it with no pop.
            if (wasOutsideWake && !isOutsideWake)
            {
                // Launch height scales with how fast the rider was turning at
                // the moment they hit the wake - a slow drift over barely gets
                // any air, a hard cut into it launches them.
                verticalVelocity = Mathf.Abs(angularVelocity) * jumpVelocityScale;

                // Captured before horizontalCoastScale is applied below, so
                // it reads as the actual carve speed at the moment they left
                // the wake - what the raley's commitment scales with.
                launchCarveSpeed = Mathf.Abs(angularVelocity);

                // Heelside/toeside is about which lateral direction they're
                // carving, not which side of the boat this crossing happens
                // on: riding one edge covers a whole swing from one apex
                // through center to the other, and only changes when the
                // carve direction itself reverses - same as real
                // wakeboarding. So it's just the sign of angularVelocity
                // (which way they're actually steering) mirrored by stance,
                // not the side of the crossing.
                bool movingLeft = angularVelocity < 0f;
                launchIsHeelside = movingLeft == (stanceSign > 0f);
                launchYawSign = Mathf.Sign(angularVelocity);

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
                landingImpactTimer = landingImpactDuration;
            }
        }

        if (nowGrounded)
        {
            isFlipping = false;
            flipSpinDeg = 0f;
            isRaleying = false;
            raleyTimer = 0f;
            // Only a fresh wake crossing sets these again, so a plain bunny
            // hop (no carve into a wake at all) can't inherit leftover
            // values from an earlier jump and unlock a raley it didn't earn.
            launchCarveSpeed = 0f;
            launchIsHeelside = false;
        }
        else if (!isFlipping && !isRaleying)
        {
            bool frontInput = Input.GetKey(KeyCode.UpArrow);
            bool backInput = Input.GetKey(KeyCode.DownArrow);
            // Both held together is always a raley attempt (or nothing, if
            // it wasn't a heelside launch or the carve wasn't fast enough
            // to earn one) - it must never fall through to the single-key
            // flip branch below, since both frontInput and backInput are
            // true here too.
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

                // stanceSign is already frozen at its takeoff value (see
                // field comment), which is what keeps this constant for the
                // rest of the jump even if an air spin sweeps spinDeg
                // through the regular/switch boundary mid-flight.
                float direction = (frontInput ? -1f : 1f) * stanceSign;
                flipSpeedDegPerSec = direction * 360f / remainingAirTime;
                isFlipping = true;
            }
        }

        if (isFlipping)
        {
            flipSpinDeg += flipSpeedDegPerSec * Time.deltaTime;
        }

        // edgeLean/inputSign are boat-relative (D always steers toward
        // boat.right, matching the swing physics above). tilt is a roll
        // around the rig's pre-yaw Z axis, so its visible lean direction
        // silently follows the rider's OWN current facing rather than the
        // boat frame - riding switch mirrors "left"/"right" the same way
        // turning your own body around does. stanceSign flips it back onto
        // the boat frame so a switch-stance rider leans toward whichever
        // side they're actually steering toward, not the mirror image.
        //
        // boardYaw does NOT need that same flip, even though it's built
        // from the same edgeLean. It's summed directly into the same yaw as
        // spinDeg rather than layered afterward like tilt is, so the visual
        // board (a symmetric box - no distinct nose/tail) already gets its
        // near/far end swapped for free by spinDeg's own 180. That swap
        // alone is what mirrors the on-screen tilt between stances, so
        // applying stanceSign here too would mirror it a second time,
        // right back to backwards.
        //
        // Computed here (rather than down by transform.rotation, where it's
        // used) because the dynamic pose below needs tilt too, to counter-
        // roll the upper body against it.
        float tilt = -edgeLean * maxLeanDeg * stanceSign;
        float boardYaw = edgeLean * maxBoardYawDeg;

        // Carve intensity drives how deep the knees bend, how far the
        // torso pitches forward into the turn, and how far the arms
        // extend - all frozen along with angularVelocity/edgeLean once
        // airborne, so a hard-carved jump keeps its aggressive lean
        // through the whole flight instead of relaxing mid-air.
        float carveIntensity = Mathf.Clamp01(Mathf.Abs(angularVelocity) / referenceCarveSpeed);

        // A brief extra crouch right on touchdown, decaying back to the
        // normal stance - the rider absorbing the landing rather than
        // stopping dead in whatever pose they flew in with.
        landingImpactTimer = Mathf.Max(0f, landingImpactTimer - Time.deltaTime);
        float landingImpactFactor = landingImpactDuration > 0f
            ? landingImpactCrouch * (landingImpactTimer / landingImpactDuration)
            : 0f;

        float crouch = baseCrouch + carveCrouchExtra * carveIntensity;
        if (!nowGrounded) crouch = Mathf.Max(crouch, airborneCrouch);
        crouch = Mathf.Clamp01(crouch + landingImpactFactor);

        float torsoPitchDeg = maxTorsoPitchDeg * carveIntensity;

        // Counters a fraction of the whole-body tilt above: the legs/board
        // (driven by the root's own rotation) lean hard into the carve
        // while the torso/head/arms stay comparatively more upright, the
        // hip/shoulder separation real riders counter-balance a hard edge
        // with.
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

    // Basic projectile kinematics: how much longer the current jump has
    // left in the air, from the current height/vertical velocity. Shared
    // by the flip and the raley so both trick timings fit whatever airtime
    // is actually left, however big or small the jump.
    float RemainingAirTime()
    {
        float discriminant = Mathf.Max(verticalVelocity * verticalVelocity + 2f * gravity * airHeight, 0f);
        float remainingAirTime = (verticalVelocity + Mathf.Sqrt(discriminant)) / gravity;
        return Mathf.Max(remainingAirTime, 0.001f);
    }

    // 0 = standing stance, raleyPeakBlend = as laid-out as this jump earned
    // (1 = fully extended superman, less for a marginal cut). Ramps up over
    // the extend phase, holds flat at that peak, then ramps back down over
    // the recover phase so the rider's feet are back under them by the time
    // raleyTimer runs out - which, since the three durations were sized as
    // fractions of the remaining airtime back when the raley started, lines
    // up with landing.
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
