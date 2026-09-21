using UnityEngine;

public class RopeRenderer : MonoBehaviour
{
    public Transform boatEnd;
    public Transform riderEnd;
    public Vector3 boatOffset = new Vector3(0f, 0.55f, -1.5f);
    public Vector3 riderOffset = new Vector3(0f, 0.3f, 0.3f);

    LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.white;
        line.endColor = Color.white;
        line.useWorldSpace = true;
    }

    void LateUpdate()
    {
        if (boatEnd == null || riderEnd == null) return;

        Vector3 boatPoint = boatEnd.position + boatEnd.up * boatOffset.y + boatEnd.forward * boatOffset.z + boatEnd.right * boatOffset.x;
        Vector3 riderPoint = riderEnd.position + riderEnd.up * riderOffset.y + riderEnd.forward * riderOffset.z + riderEnd.right * riderOffset.x;

        line.SetPosition(0, boatPoint);
        line.SetPosition(1, riderPoint);
    }
}
