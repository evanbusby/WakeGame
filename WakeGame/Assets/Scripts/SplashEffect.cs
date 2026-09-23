using UnityEngine;

// A lightweight, code-only water-spray effect, reused for the rider's board
// spray, the wake-landing splash, and the boat's rooster tail.
public class SplashEffect : MonoBehaviour
{
    public Color color = new Color(0.85f, 0.92f, 1f, 0.85f);
    public float startSpeed = 3f;
    public float startSize = 0.12f;
    public float lifetime = 0.5f;
    public float continuousRate = 20f;
    public float coneAngle = 20f;
    public float gravityModifier = 1.5f;

    // World-space direction the spray cone points, set once at Awake.
    public Vector3 sprayDirection = Vector3.up;

    ParticleSystem system;
    ParticleSystem.EmissionModule emission;

    void Awake()
    {
        system = gameObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = system.main;
        main.startColor = color;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.startLifetime = lifetime;
        main.gravityModifier = gravityModifier;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        emission = system.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = coneAngle;
        shape.radius = 0.05f;

        if (sprayDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.FromToRotation(Vector3.forward, sprayDirection.normalized);
        }

        ParticleSystemRenderer psRenderer = GetComponent<ParticleSystemRenderer>();
        psRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    public void SetContinuous(bool on)
    {
        emission.rateOverTime = on ? continuousRate : 0f;
    }

    public void Burst(int count)
    {
        system.Emit(count);
    }
}
