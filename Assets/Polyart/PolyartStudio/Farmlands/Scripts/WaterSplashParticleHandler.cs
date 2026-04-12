using UnityEngine;

[ExecuteInEditMode]
public class WaterSplashParticleHandler : MonoBehaviour
{
    private ParticleSystem particle;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;

    public float gravityMultiplier = 0.2f;
    public Vector2 velocityMinMax = new Vector2(14f, 30f);
    public Vector2 scaleMinMax = new Vector2(0.6f, 0.1f);
    public float spawnRate = 40f;

    private void OnEnable()
    {
        particle = GetComponent<ParticleSystem>();
        particle.hideFlags = HideFlags.None;

        OnTransformChanged();
    }

    private void OnValidate()
    {
        particle = GetComponent<ParticleSystem>();

        // Main Module
        var main = particle.main;
        main.gravityModifier = gravityMultiplier;
        main.startSize = new ParticleSystem.MinMaxCurve(scaleMinMax.x, scaleMinMax.y);

        var velocity = particle.velocityOverLifetime;
        velocity.speedModifier = new ParticleSystem.MinMaxCurve(velocityMinMax.x, velocityMinMax.y);

        // Emission Module
        var emission = particle.emission;
        emission.rateOverTime = spawnRate;
    }

    private void Update()
    {
        if (transform.position != lastPosition ||
            transform.rotation != lastRotation ||
            transform.localScale != lastScale)
        {
            OnTransformChanged();

            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastScale = transform.localScale;
        }
    }

    private void OnTransformChanged()
    {
        var shape = particle.shape;
        shape.radius = Mathf.Max(transform.localScale.x, transform.localScale.y);
    }
}
