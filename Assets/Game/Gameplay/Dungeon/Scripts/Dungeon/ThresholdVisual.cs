using UnityEngine;

public static class ThresholdVisual
{
    public static readonly Color GlowColor = new Color(0.95f, 0.78f, 0.38f, 0.88f);
    public static readonly Color InnerGlowColor = new Color(1f, 0.92f, 0.62f, 0.95f);
    public static readonly Color LightColor = new Color(1f, 0.86f, 0.52f, 1f);

    private static Material sharedOuterMaterial;
    private static Material sharedInnerMaterial;

    public static void Apply(Transform root)
    {
        if (root == null)
            return;

        CreateGroundQuad(root, "OuterGlow", 6.5f, GetOuterMaterial());
        CreateGroundQuad(root, "InnerGlow", 3.2f, GetInnerMaterial());

        var lightObject = new GameObject("ThresholdLight");
        lightObject.transform.SetParent(root, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = LightColor;
        light.intensity = 2.2f;
        light.range = 9f;
        light.shadows = LightShadows.None;

        CreateMotes(root);

        if (root.GetComponent<PurifiedThresholdPulse>() == null)
            root.gameObject.AddComponent<PurifiedThresholdPulse>();
    }

    private static void CreateGroundQuad(Transform parent, string name, float diameter, Material material)
    {
        var quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadObject.name = name;
        var quadTransform = quadObject.transform;
        quadTransform.SetParent(parent, false);
        quadTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quadTransform.localPosition = new Vector3(0f, 0.05f, 0f);
        quadTransform.localScale = new Vector3(diameter, diameter, 1f);

        var collider = quadObject.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        var renderer = quadObject.GetComponent<MeshRenderer>();
        if (renderer == null || material == null)
            return;

        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private static void CreateMotes(Transform parent)
    {
        var motes = new GameObject("IronMotes");
        motes.transform.SetParent(parent, false);
        motes.transform.localPosition = Vector3.zero;

        var particles = motes.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 2.4f;
        main.startSpeed = 0.35f;
        main.startSize = 0.08f;
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new Color(1f, 0.88f, 0.55f, 0.55f);

        var emission = particles.emission;
        emission.rateOverTime = 6f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 2.2f;

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = 0.45f;

        var renderer = motes.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = CreateMoteMaterial();
    }

    private static Material GetOuterMaterial()
    {
        if (sharedOuterMaterial == null)
            sharedOuterMaterial = CreateHaloMaterial(GlowColor, falloff: 2.1f, innerBoost: 0.25f);

        return sharedOuterMaterial;
    }

    private static Material GetInnerMaterial()
    {
        if (sharedInnerMaterial == null)
            sharedInnerMaterial = CreateHaloMaterial(InnerGlowColor, falloff: 1.8f, innerBoost: 0.55f);

        return sharedInnerMaterial;
    }

    private static Material CreateHaloMaterial(Color glowColor, float falloff, float innerBoost)
    {
        var shader = Shader.Find("Game/GuardianGroundHalo");
        if (shader == null)
            return null;

        var material = new Material(shader);
        material.SetColor("_GlowColor", glowColor);
        material.SetFloat("_Falloff", falloff);
        material.SetFloat("_InnerBoost", innerBoost);
        return material;
    }

    private static Material CreateMoteMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");

        var material = new Material(shader);
        material.SetColor("_BaseColor", new Color(1f, 0.9f, 0.6f, 0.7f));
        return material;
    }
}
