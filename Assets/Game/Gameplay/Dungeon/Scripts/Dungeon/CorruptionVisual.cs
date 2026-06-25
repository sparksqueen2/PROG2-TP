using UnityEngine;

public static class CorruptionVisual
{
    public static readonly Color BlockerBase = new Color(0.1f, 0.03f, 0.14f, 0.78f);
    public static readonly Color BlockerEmission = new Color(0.42f, 0.08f, 0.62f, 1f);
    public static readonly Color AuraColor = new Color(0.55f, 0.12f, 0.78f, 0.7f);

    private static Material sharedBlockerMaterial;
    private static Material sharedAuraMaterial;
    private static Material sharedParticleMaterial;
    private static Shader cachedShader;

    public static void ApplyToBlocker(GameObject blocker)
    {
        if (blocker == null)
            return;

        var renderer = blocker.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = GetBlockerMaterial();

        if (blocker.transform.Find("CorruptionMist") != null)
            return;

        CreateMistParticles(blocker.transform);
    }

    public static void ApplyGuardianAura(GameObject enemy)
    {
        if (enemy == null || enemy.transform.Find("GuardianCorruptionAura") != null)
            return;

        const float radius = 1.35f;

        var aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        aura.name = "GuardianCorruptionAura";
        aura.transform.SetParent(enemy.transform, false);
        aura.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        aura.transform.localScale = new Vector3(radius * 2f, 0.035f, radius * 2f);

        var collider = aura.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        var renderer = aura.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = GetAuraMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    public static void SetBlockerParticlesActive(GameObject blocker, bool active)
    {
        if (blocker == null)
            return;

        var mist = blocker.transform.Find("CorruptionMist");
        if (mist == null || !mist.TryGetComponent<ParticleSystem>(out var particles))
            return;

        if (active)
        {
            if (!particles.isPlaying)
                particles.Play();
        }
        else
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private static Material GetBlockerMaterial()
    {
        if (sharedBlockerMaterial == null)
            sharedBlockerMaterial = CreateCorruptionMaterial(BlockerBase, BlockerEmission, transparent: true);
        return sharedBlockerMaterial;
    }

    private static Material GetAuraMaterial()
    {
        if (sharedAuraMaterial == null)
            sharedAuraMaterial = CreateCorruptionMaterial(AuraColor, AuraColor * 2.2f, transparent: true);
        return sharedAuraMaterial;
    }

    private static Material GetParticleMaterial()
    {
        if (sharedParticleMaterial == null)
        {
            sharedParticleMaterial = CreateCorruptionMaterial(
                new Color(0.4f, 0.1f, 0.55f, 0.35f),
                BlockerEmission * 0.5f,
                transparent: true);
        }

        return sharedParticleMaterial;
    }

    private static Material CreateCorruptionMaterial(Color baseColor, Color emission, bool transparent)
    {
        if (cachedShader == null)
            cachedShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        var material = new Material(cachedShader);
        material.color = baseColor;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", baseColor);

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emission);

        if (transparent && material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        return material;
    }

    private static void CreateMistParticles(Transform parent)
    {
        var mist = new GameObject("CorruptionMist");
        mist.transform.SetParent(parent, false);
        mist.transform.localPosition = Vector3.zero;

        var parentScale = parent.lossyScale;
        mist.transform.localScale = new Vector3(
            1f / Mathf.Max(0.01f, parentScale.x),
            1f / Mathf.Max(0.01f, parentScale.y),
            1f / Mathf.Max(0.01f, parentScale.z));

        var particles = mist.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 2f;
        main.startSpeed = 0.25f;
        main.startSize = 0.18f;
        main.maxParticles = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new Color(0.35f, 0.08f, 0.5f, 0.4f);

        var emission = particles.emission;
        emission.rateOverTime = 5f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1f, 0.5f, 1f);

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = 0.2f;

        var renderer = mist.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = GetParticleMaterial();
    }
}
