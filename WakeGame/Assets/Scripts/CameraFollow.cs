using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 4f, -8f);
    public float smoothSpeed = 8f;

    // Camera.fieldOfView is the VERTICAL field of view - the horizontal FOV
    // you actually see is derived from it and the screen's aspect ratio. The
    // rider swings well out to either side of the boat, so this assumes a
    // widescreen aspect (referenceAspect) is wide enough to keep them in
    // frame at baseFieldOfView, and widens the FOV on narrower/taller screens
    // (e.g. a square or portrait itch.io embed) so the horizontal FOV never
    // shrinks below that same reference, capped at maxFieldOfView to avoid
    // extreme fisheye distortion on very narrow screens.
    public float baseFieldOfView = 60f;
    public float referenceAspect = 16f / 9f;
    public float maxFieldOfView = 100f;

    // On an actual portrait phone screen, the aspect ratio is narrow enough
    // that even the widened FOV above isn't quite enough to keep the rider's
    // full swing in frame - and pushing the FOV even higher starts to look
    // like a fisheye lens. Instead, on mobile only, this pulls the camera
    // back along its existing offset direction as the aspect gets narrower,
    // shrinking the rider's apparent angular swing the way backing up a real
    // camera would, rather than distorting the lens further. Gated to
    // MobileControls.IsMobile so desktop is never affected, even if a
    // desktop browser window is resized to a narrow/tall shape.
    public float maxMobileDistanceScale = 1.15f;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        float aspect = cam != null ? cam.aspect : referenceAspect;

        float distanceScale = 1f;
        if (MobileControls.IsMobile && aspect < referenceAspect)
        {
            distanceScale = Mathf.Clamp(referenceAspect / Mathf.Max(aspect, 0.01f), 1f, maxMobileDistanceScale);
        }

        Vector3 desiredPosition = target.position + offset * distanceScale;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);

        if (cam != null)
        {
            if (aspect < referenceAspect)
            {
                float baseHalfVerticalRad = baseFieldOfView * 0.5f * Mathf.Deg2Rad;
                float referenceHalfHorizontalRad = Mathf.Atan(Mathf.Tan(baseHalfVerticalRad) * referenceAspect);
                float requiredHalfVerticalRad = Mathf.Atan(Mathf.Tan(referenceHalfHorizontalRad) / aspect);
                cam.fieldOfView = Mathf.Min(requiredHalfVerticalRad * 2f * Mathf.Rad2Deg, maxFieldOfView);
            }
            else
            {
                cam.fieldOfView = baseFieldOfView;
            }
        }
    }
}
