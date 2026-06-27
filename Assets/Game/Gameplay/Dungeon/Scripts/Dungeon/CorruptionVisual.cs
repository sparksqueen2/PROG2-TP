using UnityEngine;

public static class CorruptionVisual
{
    public static readonly Color BlockerBase = new Color(0.1f, 0.03f, 0.14f, 0.78f);
    public static readonly Color BlockerEmission = new Color(0.42f, 0.08f, 0.62f, 1f);
    public static readonly Color AuraColor = new Color(0.72f, 0.42f, 1f, 0.85f);
    public static readonly Color HaloLightColor = new Color(0.78f, 0.5f, 1f, 1f);

    private const string BlockerMaterialResourcePath = "Dungeon/CorruptionBarrier";
    private const string HaloMaterialResourcePath = "Dungeon/GuardianHalo";

    private static Material sharedBlockerMaterial;
    private static Material sharedHaloMaterial;
    private static Material sharedParticleMaterial;

    public static void ApplyToBlocker(GameObject blocker)
    {
        if (blocker == null)
            return;

        EnsureCorruptionVeil(blocker.transform);

        if (blocker.transform.Find("CorruptionMist") == null)
            CreateMistParticles(blocker.transform);
    }

    public static void ApplyGuardianAura(GameObject enemy)
    {
        if (enemy == null)
            return;

        RemoveLegacyGuardianAura(enemy.transform);
        EnsureGroundHalo(enemy.transform);
        EnsureHaloLight(enemy.transform);
    }

    private static void RemoveLegacyGuardianAura(Transform parent)
    {
        var legacy = parent.Find("GuardianCorruptionAura");
        if (legacy != null)
            Object.Destroy(legacy.gameObject);
    }

    private static void EnsureGroundHalo(Transform parent)
    {
        const float diameter = 2.8f;

        var halo = parent.Find("GuardianLightHalo");
        if (halo == null)
        {
            var haloObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            haloObject.name = "GuardianLightHalo";
            halo = haloObject.transform;
            halo.SetParent(parent, false);

            var collider = haloObject.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
        }

        halo.localRotation = Quaternion.Euler(90f, 0f, 0f);
        halo.localPosition = new Vector3(0f, 0.04f, 0f);
        halo.localScale = new Vector3(diameter, diameter, 1f);

        var renderer = halo.GetComponent<MeshRenderer>();
        if (renderer == null)
            return;

        renderer.sharedMaterial = GetHaloMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private static void EnsureHaloLight(Transform parent)
    {
        if (parent.Find("GuardianHaloLight") != null)
            return;

        var lightObject = new GameObject("GuardianHaloLight");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.35f, 0f);

        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = HaloLightColor;
        light.intensity = 1.35f;
        light.range = 5f;
        light.shadows = LightShadows.None;
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

    private static void EnsureCorruptionVeil(Transform blocker)
    {
        var veilTransform = blocker.Find("CorruptionVeil");
        GameObject veilObject;

        if (veilTransform == null)
        {
            veilObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            veilObject.name = "CorruptionVeil";
            veilTransform = veilObject.transform;
            veilTransform.SetParent(blocker, false);
            veilTransform.localPosition = Vector3.zero;
            veilTransform.localRotation = Quaternion.identity;
            veilTransform.localScale = new Vector3(1.02f, 1.02f, 1.02f);

            var collider = veilObject.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);
        }
        else
        {
            veilObject = veilTransform.gameObject;
        }

        var renderer = veilObject.GetComponent<Renderer>();
        if (renderer == null)
            return;

        renderer.sharedMaterial = GetBlockerMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Material GetBlockerMaterial()
    {
        if (sharedBlockerMaterial == null)
        {
            sharedBlockerMaterial = Resources.Load<Material>(BlockerMaterialResourcePath);
            if (sharedBlockerMaterial == null)
                sharedBlockerMaterial = CreateCorruptionMaterial(BlockerBase, BlockerEmission, transparent: true);
        }

        return sharedBlockerMaterial;
    }

    private static Material GetHaloMaterial()
    {
        if (sharedHaloMaterial == null)
        {
            sharedHaloMaterial = Resources.Load<Material>(HaloMaterialResourcePath);
            if (sharedHaloMaterial == null)
            {
                var shader = Shader.Find("Game/GuardianGroundHalo");
                if (shader != null)
                {
                    sharedHaloMaterial = new Material(shader);
                    sharedHaloMaterial.SetColor("_GlowColor", AuraColor);
                }
            }
        }

        return sharedHaloMaterial;
    }

    private static Material CloneBarrierMaterial(Color baseColor, Color emission)
    {
        var template = Resources.Load<Material>(BlockerMaterialResourcePath);
        if (template == null)
            return CreateCorruptionMaterial(baseColor, emission, transparent: true);

        var material = new Material(template);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", emission);
        return material;
    }

    private static Material GetParticleMaterial()
    {
        if (sharedParticleMaterial == null)
        {
            sharedParticleMaterial = CloneBarrierMaterial(
                new Color(0.4f, 0.1f, 0.55f, 0.35f),
                BlockerEmission * 0.5f);
        }

        return sharedParticleMaterial;
    }

    private static Material CreateCorruptionMaterial(Color baseColor, Color emission, bool transparent)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("[CorruptionVisual] No se encontró el shader URP/Lit.");
            return null;
        }

        var material = new Material(shader);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", baseColor);
        else
            material.color = baseColor;

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emission);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        if (transparent && material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_ReceiveShadows", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            material.EnableKeyword("_SPECULAR_SETUP");
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("SHADOWCASTER", false);
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
