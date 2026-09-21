using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water";
        water.transform.localScale = new Vector3(50f, 1f, 500f);
        water.transform.position = new Vector3(0f, 0f, 200f);
        water.AddComponent<WaterScroll>();

        GameObject boat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boat.name = "Boat";
        boat.transform.position = new Vector3(0f, 0.5f, 0f);
        boat.transform.localScale = new Vector3(1.5f, 1f, 3f);
        SetColor(boat, new Color(0.6f, 0.2f, 0.1f));
        boat.AddComponent<BoatMover>();

        GameObject rider = new GameObject("Rider");
        rider.transform.position = new Vector3(0f, 0.05f, -12f);
        RiderRig riderRig = rider.AddComponent<RiderRig>();
        RiderController riderController = rider.AddComponent<RiderController>();
        riderController.boat = boat.transform;

        GameObject wake = new GameObject("Wake");
        WakeRenderer wakeRenderer = wake.AddComponent<WakeRenderer>();
        wakeRenderer.boat = boat.transform;

        GameObject rope = new GameObject("Rope");
        rope.AddComponent<LineRenderer>();
        RopeRenderer ropeRenderer = rope.AddComponent<RopeRenderer>();
        ropeRenderer.boatEnd = boat.transform;
        ropeRenderer.riderEnd = rider.transform;
        ropeRenderer.riderOffset = riderRig.handOffset;

        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();
        CameraFollow follow = camObj.AddComponent<CameraFollow>();
        follow.target = boat.transform;
        follow.offset = new Vector3(0f, 6f, -20f);
        camObj.transform.position = boat.transform.position + follow.offset;

        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    void SetColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = color;
        }
    }
}
