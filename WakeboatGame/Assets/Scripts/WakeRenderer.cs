using UnityEngine;

public class WakeRenderer : MonoBehaviour
{
    public Transform boat;

    // Should match RiderController.wakeAngleDeg so the visible wake lines up
    // with where the rider actually launches.
    public float wakeAngleDeg = 19.47f;
    public float wakeLength = 60f;
    public Vector3 sternOffset = new Vector3(0f, 0.05f, -1.5f);

    LineRenderer leftLine;
    LineRenderer rightLine;

    void Awake()
    {
        leftLine = CreateLine("WakeLeft");
        rightLine = CreateLine("WakeRight");
    }

    LineRenderer CreateLine(string lineName)
    {
        GameObject go = new GameObject(lineName);
        go.transform.parent = transform;
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.15f;
        line.endWidth = 0.15f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1f, 1f, 1f, 0.85f);
        line.endColor = new Color(1f, 1f, 1f, 0f);
        line.useWorldSpace = true;
        return line;
    }

    void LateUpdate()
    {
        if (boat == null) return;

        Vector3 stern = boat.position + boat.up * sternOffset.y + boat.forward * sternOffset.z;
        float rad = wakeAngleDeg * Mathf.Deg2Rad;
        Vector3 leftDir = (-boat.forward * Mathf.Cos(rad) - boat.right * Mathf.Sin(rad)).normalized;
        Vector3 rightDir = (-boat.forward * Mathf.Cos(rad) + boat.right * Mathf.Sin(rad)).normalized;

        leftLine.SetPosition(0, stern);
        leftLine.SetPosition(1, stern + leftDir * wakeLength);

        rightLine.SetPosition(0, stern);
        rightLine.SetPosition(1, stern + rightDir * wakeLength);
    }
}
