using UnityEngine;

public class RopeRenderer : MonoBehaviour
{
    public Transform boatEnd;
    public Transform riderEnd;
    public Vector3 boatOffset = new Vector3(0f, 0.55f, -1.5f);
    public Vector3 riderOffset = new Vector3(0f, 0.3f, 0.3f);

    // On a real handle pass, the rope briefly routes behind the rider's back
    // as it's handed from one grip to the other during a spin. We don't
    // simulate individual hands, so this fakes it: for a short window the
    // rope's midpoint bulges out behind the rider's back and returns, giving
    // a visible "passing it around the body" cue instead of the rope just
    // snapping straight through the spin.
    public float passDuration = 0.35f;
    float passTimer = 0f;

    LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 3;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.white;
        line.endColor = Color.white;
        line.useWorldSpace = true;
    }

    public void TriggerPass()
    {
        passTimer = passDuration;
    }

    void LateUpdate()
    {
        if (boatEnd == null || riderEnd == null) return;

        Vector3 boatPoint = boatEnd.position + boatEnd.up * boatOffset.y + boatEnd.forward * boatOffset.z + boatEnd.right * boatOffset.x;
        Vector3 riderPoint = riderEnd.position + riderEnd.up * riderOffset.y + riderEnd.forward * riderOffset.z + riderEnd.right * riderOffset.x;
        Vector3 midPoint = Vector3.Lerp(boatPoint, riderPoint, 0.5f);

        if (passTimer > 0f)
        {
            passTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(passTimer / passDuration);
            Vector3 behindBack = riderEnd.position + riderEnd.up * riderOffset.y - riderEnd.forward * 0.4f;
            midPoint = Vector3.Lerp(midPoint, behindBack, Mathf.Sin(t * Mathf.PI));
        }

        line.SetPosition(0, boatPoint);
        line.SetPosition(1, midPoint);
        line.SetPosition(2, riderPoint);
    }
}
