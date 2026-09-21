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

        // Unity's built-in Plane primitive is a 10x10 unit quad, so half its
        // world size is 5 units per unit of localScale.
        float waterHalfWidth = water.transform.localScale.x * 5f;
        float landHalfWidth = 300f;
        float landCenterOffset = waterHalfWidth + landHalfWidth;
        CreateLand("LandLeft", -landCenterOffset, landHalfWidth, water.transform.position.z, water.transform.localScale.z);
        CreateLand("LandRight", landCenterOffset, landHalfWidth, water.transform.position.z, water.transform.localScale.z);

        GameObject boat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boat.name = "Boat";
        boat.transform.position = new Vector3(0f, 0.5f, 0f);
        boat.transform.localScale = new Vector3(1.5f, 1f, 3f);
        SetColor(boat, new Color(0.6f, 0.2f, 0.1f));
        boat.AddComponent<BoatMover>();

        GameObject roosterTail = new GameObject("RoosterTail");
        roosterTail.transform.SetParent(boat.transform, false);
        roosterTail.transform.localPosition = new Vector3(0f, -0.3f, -1.7f);
        SplashEffect roosterTailEffect = roosterTail.AddComponent<SplashEffect>();
        roosterTailEffect.color = new Color(0.85f, 0.92f, 1f, 0.85f);
        roosterTailEffect.startSpeed = 6f;
        roosterTailEffect.startSize = 0.2f;
        roosterTailEffect.lifetime = 0.8f;
        roosterTailEffect.continuousRate = 40f;
        roosterTailEffect.coneAngle = 12f;
        roosterTailEffect.gravityModifier = 1.2f;
        roosterTailEffect.sprayDirection = new Vector3(0f, 0.6f, -1f);
        roosterTailEffect.SetContinuous(true);

        GameObject rider = new GameObject("Rider");
        rider.transform.position = new Vector3(0f, 0.05f, -12f);
        RiderRig riderRig = rider.AddComponent<RiderRig>();
        RiderController riderController = rider.AddComponent<RiderController>();
        riderController.boat = boat.transform;

        GameObject boardSplash = new GameObject("BoardSplash");
        boardSplash.transform.SetParent(rider.transform, false);
        boardSplash.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        SplashEffect boardSplashEffect = boardSplash.AddComponent<SplashEffect>();
        boardSplashEffect.color = new Color(0.85f, 0.92f, 1f, 0.8f);
        boardSplashEffect.startSpeed = 2.5f;
        boardSplashEffect.startSize = 0.08f;
        boardSplashEffect.lifetime = 0.4f;
        boardSplashEffect.continuousRate = 15f;
        boardSplashEffect.coneAngle = 25f;
        boardSplashEffect.sprayDirection = Vector3.up;
        riderController.boardSplash = boardSplashEffect;

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

    void CreateLand(string name, float centerX, float halfWidth, float centerZ, float zScale)
    {
        GameObject land = GameObject.CreatePrimitive(PrimitiveType.Plane);
        land.name = name;
        land.transform.localScale = new Vector3(halfWidth / 5f, 1f, zScale);
        // Sits a bit above the water plane's y=0, both so it reads as a
        // raised shore rather than more water, and so the two planes aren't
        // perfectly coplanar at the shared edge (which would z-fight/flicker
        // exactly like the wake-vs-water flicker fixed earlier).
        land.transform.position = new Vector3(centerX, 0.1f, centerZ);
        land.AddComponent<LandScroll>();

        // LandFeatures lives on its own identity-scale object rather than
        // under the land plane, since that plane's transform carries a
        // large non-uniform scale that would otherwise warp world-space
        // sized hills/trees (see LandFeatures' own comment).
        int side = centerX >= 0f ? 1 : -1;
        float innerEdgeX = Mathf.Abs(centerX) - halfWidth;

        GameObject featuresRoot = new GameObject(name + "Features");
        featuresRoot.transform.position = new Vector3(0f, 0f, centerZ);
        LandFeatures features = featuresRoot.AddComponent<LandFeatures>();
        features.innerEdgeX = innerEdgeX;
        features.stripWidth = halfWidth * 2f;
        features.stripHalfLength = zScale * 5f;
        features.side = side;
        features.Generate();
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
