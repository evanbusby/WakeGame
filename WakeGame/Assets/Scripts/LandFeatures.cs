using UnityEngine;

// Turns a flat strip of land into rolling, tree-covered hills - all built
// from basic shapes (squashed spheres for hills, cylinder-plus-sphere
// "lollipop" trees), no imported models. Lives on its own object rather than
// as a child of the land plane, since that plane's stretched scale would
// distort the hills and trees' sizes.
public class LandFeatures : MonoBehaviour
{
    // X distance from the world's center line to the water's edge - hills
    // start here and extend outward across the strip.
    public float innerEdgeX;

    // Full width of the land strip (from the water's edge to its outer edge).
    public float stripWidth;

    // Half the strip's length along Z (matches the land plane's own half-length).
    public float stripHalfLength;

    // +1 for the strip on the +X side of the water, -1 for the -X side.
    public int side = 1;

    public float hillSpacing = 100f;
    public int treesPerHill = 14;

    static Material trunkMaterial;
    static Material canopyMaterial;
    static Material[] hillMaterials;

    static readonly Color[] HillColors =
    {
        new Color(0.22f, 0.45f, 0.18f),
        new Color(0.27f, 0.5f, 0.2f),
        new Color(0.2f, 0.4f, 0.16f),
    };
    static readonly Color CanopyColor = new Color(0.16f, 0.42f, 0.15f);
    static readonly Color TrunkColor = new Color(0.35f, 0.25f, 0.12f);

    // Called explicitly by GameBootstrap after it sets the size/position
    // fields above, since Awake() would otherwise run before those fields
    // are assigned.
    public void Generate()
    {
        if (hillMaterials == null) hillMaterials = new Material[HillColors.Length];
        Build();
    }

    // Clones whatever shader Unity's primitive already uses, rather than
    // hardcoding one, so this keeps working under different render
    // pipelines.
    static Material CloneMaterial(Renderer r, Color color)
    {
        Material mat = new Material(r.sharedMaterial);
        mat.color = color;
        return mat;
    }

    void Build()
    {
        float zStart = transform.position.z - stripHalfLength;
        float zEnd = transform.position.z + stripHalfLength;

        int hillIndex = 0;
        for (float hz = zStart; hz < zEnd; hz += hillSpacing)
        {
            BuildHill(hillIndex, hz);
            hillIndex++;
        }
    }

    void BuildHill(int index, float hz)
    {
        // Deterministic per-hill randomness so the layout stays the same for
        // the whole session instead of reshuffling on reload.
        System.Random rng = new System.Random(index * 7919 + side * 104729);

        float hillRadius = Mathf.Lerp(18f, 40f, (float)rng.NextDouble());
        float hillHeight = hillRadius * Mathf.Lerp(0.55f, 0.95f, (float)rng.NextDouble());
        float hx = side * (innerEdgeX + Mathf.Lerp(hillRadius, Mathf.Max(hillRadius, stripWidth - hillRadius), (float)rng.NextDouble()));

        GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hill.name = "Hill";
        hill.transform.SetParent(transform, false);
        hill.transform.position = new Vector3(hx, 0f, hz);
        hill.transform.localScale = new Vector3(hillRadius * 2f, hillHeight * 2f, hillRadius * 2f);
        StripCollider(hill);

        Renderer hillRenderer = hill.GetComponent<Renderer>();
        int colorIndex = index % hillMaterials.Length;
        if (hillMaterials[colorIndex] == null)
        {
            hillMaterials[colorIndex] = CloneMaterial(hillRenderer, HillColors[colorIndex]);
        }
        hillRenderer.sharedMaterial = hillMaterials[colorIndex];

        for (int i = 0; i < treesPerHill; i++)
        {
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
            // Biases trees toward the outer part of the hill so they don't
            // all bunch up at the peak.
            float dist = hillRadius * Mathf.Sqrt((float)rng.NextDouble()) * 0.9f;
            float tx = hx + Mathf.Cos(angle) * dist;
            float tz = hz + Mathf.Sin(angle) * dist;

            float normalizedDist = Mathf.Clamp01(dist / hillRadius);
            float ty = hillHeight * Mathf.Sqrt(Mathf.Max(0f, 1f - normalizedDist * normalizedDist));

            BuildTree(new Vector3(tx, ty, tz), rng);
        }
    }

    void BuildTree(Vector3 basePos, System.Random rng)
    {
        float trunkHeight = Mathf.Lerp(2f, 4f, (float)rng.NextDouble());
        float canopyRadius = Mathf.Lerp(2.2f, 4f, (float)rng.NextDouble());

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "TreeTrunk";
        trunk.transform.SetParent(transform, false);
        trunk.transform.position = basePos + Vector3.up * (trunkHeight * 0.5f);
        trunk.transform.localScale = new Vector3(0.4f, trunkHeight * 0.5f, 0.4f);
        StripCollider(trunk);
        Renderer trunkRenderer = trunk.GetComponent<Renderer>();
        if (trunkMaterial == null) trunkMaterial = CloneMaterial(trunkRenderer, TrunkColor);
        trunkRenderer.sharedMaterial = trunkMaterial;

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "TreeCanopy";
        canopy.transform.SetParent(transform, false);
        canopy.transform.position = basePos + Vector3.up * (trunkHeight + canopyRadius * 0.6f);
        canopy.transform.localScale = Vector3.one * canopyRadius * 2f;
        StripCollider(canopy);
        Renderer canopyRenderer = canopy.GetComponent<Renderer>();
        if (canopyMaterial == null) canopyMaterial = CloneMaterial(canopyRenderer, CanopyColor);
        canopyRenderer.sharedMaterial = canopyMaterial;
    }

    static void StripCollider(GameObject go)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }
}
