using BoboEngine;
using BoboEngine.Shaders;
using BoboEngine.Utils;
using Minecraft.Random;
namespace Minecraft;

public class Sky : ObjectBehavior
{
    private Skybox skybox;
    private TriangleFanMesh skyTop;
    private TriangleFanMesh sunRiseSet;
    private GameObject sun;
    private Mesh moon;
    private Mesh stars;

    private Float3 fog_Color = new(1,0,1);
    private Float3 sky_Color = new(0.9f,0.1f,1);

    public override void Start()
    {
        base.Start();

        skybox = gameObject.GetComponent<Skybox>();

        // Shaders

        ShaderManager.EnsureShader("SkyDim", "Shader/skyShader.vert", "Shader/skyShader.frag");
        ShaderManager.EnsureShader("SunRise", "Shader/sunRise.vert", "Shader/sunRise.frag");
        ShaderManager.EnsureShader("Moon", "Shader/moonShader.vert", "Shader/moonShader.frag");
        ShaderManager.EnsureShader("Stars", "Shader/starShader.vert", "Shader/starShader.frag");

        // Sky Top

        skyTop = new GameObject("SkyTop").AddComponent<TriangleFanMesh>();
        skyTop.transform.parent = transform;
        skyTop.LoadRawData(BuildSkyDisc(16));
        skyTop.material = new Material("SkyDim", transformMode: RenderTranformMode.LocalTransform, useDepth: false, renderOrder: -5);

        // Sun Riseset

        sunRiseSet = new GameObject("SunRiseSet").AddComponent<TriangleFanMesh>();
        sunRiseSet.transform.parent = transform;
        sunRiseSet.material = new Material("SunRise", transformMode: RenderTranformMode.LocalTransform, useDepth: false, renderOrder: -4);

        // Sun

        sun = new GameObject("Sun");

        sun.transform.parent = transform;
        Mesh sunMesh = sun.AddComponent<Mesh>();

        var sunMeshGenerated = BuildPlane(30, 100);

        sunMesh.LoadRawData(sunMeshGenerated.vertices, sunMeshGenerated.faces, sunMeshGenerated.normals, sunMeshGenerated.textureCoords);
        sunMesh.material = new Material(texture: new Texture2D(TextureManager.GetPathToTexture("environment\\sun.png"), "sun", TextureSampleType.Nearest), blendMode: BlendMode.Blend, useDepth: false, transformMode: RenderTranformMode.LocalTransform, renderOrder: -3);

        // Moon

        moon = new GameObject("Moon").AddComponent<Mesh>();

        moon.transform.parent = transform;

        var moonMeshGenerated = BuildPlane(20, 100, true);

        moon.LoadRawData(moonMeshGenerated.vertices, moonMeshGenerated.faces, moonMeshGenerated.normals, moonMeshGenerated.textureCoords);
        moon.material = new Material("Moon", texture: new Texture2DArray(TextureManager.GetPathToTexture("environment\\moon_phases.png"), "moon", 32, 32, TextureSampleType.Nearest), blendMode: BlendMode.Blend, useDepth: false, transformMode: RenderTranformMode.LocalTransform, renderOrder: -2);
        moon.material.SetFloat("phase", 0);


        // Stars

        stars = new GameObject("Stars").AddComponent<Mesh>();

        stars.transform.parent = transform;
        BuildStars();
        stars.material = new Material("Stars", blendMode: BlendMode.Blend, useDepth:false, transformMode: RenderTranformMode.LocalTransform, renderOrder: -1);

        // Load Biome Colors

        var file = Path.Combine(Engine.ProgramDirectory, "Data\\data\\minecraft\\worldgen\\biome\\plains.json").Replace('/', '\\');

        var data = FileParser.ParseJson("plains", File.ReadAllText(file));

        var effectsD = data.GetItem("effects");

        if (effectsD)
        {
            var fColorD = effectsD.GetItem("fog_color");

            if (fColorD) fog_Color = SplitInt(fColorD.GetValue<int>());

            var sColorD = effectsD.GetItem("sky_color");

            if (sColorD) sky_Color = SplitInt(sColorD.GetValue<int>());
        }
    }

    public override void Update()
    {
        base.Update();

        float angleFromY = TimeOfDay.GetTimeOfDay() * 360;

        sun.transform.rotation = new(y: -90, x: -angleFromY - 180);
        moon.transform.rotation = new(y: -90, x: -angleFromY);
        stars.transform.rotation = new(y: -90, z: -angleFromY);

        var skyColor = GetSkyColor(angleFromY);
        var fogColor = ComputeFogColor(angleFromY, 12);

        skybox.skyColor = fogColor;

        skyTop.material.SetVec3("skyColor", skyColor);
        skyTop.material.SetVec3("fogColor", fogColor);

        stars.material.SetFloat("brightness", GetStarBrightness(angleFromY));

        if (IsSunriseOrSunset(angleFromY))
        {
            var color = GetSunrisesetColor(angleFromY);

            float a = Maths.Sin(-angleFromY) < 0.0F ? 180.0F : 0.0F;

            sunRiseSet.transform.rotation = new(90, 0, a + 90);

            sunRiseSet.gameObject.enabled = true;
            sunRiseSet.material.SetVec4("color", color);
            GenerateSunriseset(color.a);
        }
        else
        {
            sunRiseSet.gameObject.enabled = false;
        }

        SetMoonPhase(Maths.Mod(TimeOfDay.time / 24000, 8));
    }

    // Minecraft Functions!!!
    private void BuildStars()
    {
        List<Float3> verticies = new();

        RandomSource randomsource = RandomSource.Create(10842L);

        for (int j = 0; j < 1500; j++)
        {
            float x = randomsource.NextFloat() * 2.0f - 1.0f;
            float y = randomsource.NextFloat() * 2.0f - 1.0f;
            float z = randomsource.NextFloat() * 2.0f - 1.0f;

            Float3 pos = new Float3(x, y, z);

            float starSize = 0.15f + randomsource.NextFloat() * 0.1f;
            float distance = pos.LengthSquared();
            if (!(distance <= 0.01f) && !(distance >= 1.0F))
            {
                Float3 starCenter = new Float3(x, y, z).Normalized() * 100;

                float zRotation = (float)(randomsource.NextDouble() * 360f);

                BaseVectors rotation = BaseVectors.FromZRotation(-zRotation).TransformByBaseVectors(BaseVectors.FacingTowards(starCenter, Float3.yAxis));

                verticies.Add(rotation.TransformVector(new(starSize, -starSize, 0f)) + starCenter);
                verticies.Add(rotation.TransformVector(new(starSize, starSize, 0f)) + starCenter);
                verticies.Add(rotation.TransformVector(new(-starSize, starSize, 0f)) + starCenter);
                verticies.Add(rotation.TransformVector(new(-starSize, -starSize, 0f)) + starCenter);
            }
        }

        List<FaceInfo> faces = new();

        for (int i = 0; i < verticies.Count; i += 4)
        {
            foreach (var face in FaceInfo.GetTriangulatedFaces($"f {i + 1} {i + 2} {i + 3} {i + 4}"))
            {
                faces.Add(face);
            }
        }

        stars.LoadRawData(verticies.ToArray(), faces.ToArray());
    }
    private (Float3[] vertices, FaceInfo[] faces, Float3[] normals, Float2[] textureCoords) BuildPlane(float width, float distance, bool flipTextureCoords = false)
    {
        Float3[] vertices =
            [
                new Float3(-width, -distance, width),
                new Float3(width, -distance, width),
                new Float3(width, -distance, -width),
                new Float3(-width, -distance, -width),
            ];
        Float2[] textureCoords =
            [
                new Float2(0,0),
                new Float2(1,0),
                new Float2(1,1),
                new Float2(0,1),
            ];
        Float3[] normals =
            [
                new Float3(0, -1, 0),
            ];

        string faceInfo = flipTextureCoords ? "f 1/4/1 2/3/1 3/2/1 4/1/1" : "f 1/1/1 2/2/1 3/3/1 4/4/1";

        FaceInfo[] faces = FaceInfo.GetTriangulatedFaces(faceInfo);

        return (vertices, faces, normals, textureCoords);
    }
    private Float3[] BuildSkyDisc(float height)
    {
        List<Float3> vertices = new();

        float root = Math.Sign(height) * 512.0F;
        vertices.Add(new(0.0F, height, 0.0F));

        for (int i = -180; i <= 180; i += 45)
        {
            vertices.Add(new(root * Maths.Cos(i), height, 512.0F * Maths.Sin(i)));
        }

        return vertices.ToArray();
    }

    private Float3 SplitInt(int input)
    {
        string binary = Convert.ToString(input, 2);
        binary = binary.PadLeft(24, '0');

        string binaryR = "";
        string binaryG = "";
        string binaryB = "";

        int i = 0;
        foreach (var c in binary.ToCharArray())
        {
            if (i < 8)
            {
                binaryR += c;
            }
            else if(i < 16)
            {
                binaryG += c;
            }
            else
            {
                binaryB += c;
            }
            i++;
        }

        float r = Convert.ToInt32(binaryR, 2) / 255f;
        float g = Convert.ToInt32(binaryG, 2) / 255f;
        float b = Convert.ToInt32(binaryB, 2) / 255f;

        return new(r,g,b);
    }

    public void GenerateSunriseset(float transparency)
    {
        List<Float3> vertices = new();
        List<Float4> colors = new();

        vertices.Add(new(0.0F, 100.0F, 0.0F));
        colors.Add(new(1, 1, 1, 1));

        for (int i = 0; i <= 16; i++)
        {
            float angle = i * 22.5f; // ((Math.PI * 2) / 16.0F) Radians turns into 22.5 degrees

            float sin = Maths.Sin(angle);
            float cos = Maths.Cos(angle);

            vertices.Add(new(sin * 120.0F, cos * 120.0F, -cos * 40.0F * transparency));
            colors.Add(new(1, 1, 1, 0));
        }

        sunRiseSet.DeleteMesh();
        sunRiseSet.LoadRawData(vertices.ToArray(), colors.ToArray());
    }
    public bool IsSunriseOrSunset(float angleFromY)
    {
        float f = Maths.Cos(angleFromY);
        return f >= -0.4F && f <= 0.4F;
    }
    public Float4 GetSunrisesetColor(float angleFromY)
    {
        float angle = Maths.Cos(angleFromY);

        float intensity = angle / 0.4F * 0.5F + 0.5F;

        float alpha = 1.0F - (1.0F - Maths.Sin(intensity * 180) * 0.99F);
        alpha *= alpha;

        return new(intensity * 0.3F + 0.7F, intensity * intensity * 0.7F + 0.2F, 0.2F, alpha);
    }
    public Float3 ComputeFogColor(float angleFromY, int renderDistance)//, float darkenAmount)
    {
        float blend = 0.25F + 0.75F * (float)renderDistance / 32.0F;
        blend = 1.0F - (float)Math.Pow((double)blend, 0.25);

        float brightness = Maths.Clamp(Maths.Cos(angleFromY), 0.0F, 1.0F);

        Float3 fogColor = GetBrightnessDependentFogColor(fog_Color, brightness);

        if (renderDistance >= 4)
        {
            float sign = Maths.Sin(angleFromY) > 0.0F ? -1.0F : 1.0F;
            Float3 sunDirection = new Float3(sign, 0.0F, 0.0F);
            float dot = Float3.Dot(Camera.main.transform.baseVectors.forwardVector, sunDirection);
            if (dot < 0.0F)
            {
                dot = 0.0F;
            }

            if (dot > 0.0F && IsSunriseOrSunset(angleFromY))
            {
                Float4 sunColor = GetSunrisesetColor(angleFromY);

                dot *= sunColor.a;

                fogColor = fogColor * (1f - dot) + (Float3)sunColor * dot;
            }
        }

        fogColor += (sky_Color - fogColor) * blend;

        /* Void Darkness
        float f8 = ((float)camera.getPosition().y - (float)level.getMinY()) * level.getLevelData().getClearColorScale();
        FogRenderer.MobEffectFogFunction fogrenderer$mobeffectfogfunction = getPriorityFogFunction(entity, p_364035_);
        if (fogrenderer$mobeffectfogfunction != null) {
            LivingEntity livingentity = (LivingEntity)entity;
            f8 = fogrenderer$mobeffectfogfunction.getModifiedVoidDarkness(livingentity, livingentity.getEffect(fogrenderer$mobeffectfogfunction.getMobEffect()), f8, p_364035_);
        }

        if (f8 < 1.0F)
        {
            if (f8 < 0.0F)
            {
                f8 = 0.0F;
            }

            f8 *= f8;
            r *= f8;
            g *= f8;
            b *= f8;
        }

        if (darkenAmount > 0.0F)
        {
            r = r * (1.0F - darkenAmount) + r * 0.7F * darkenAmount;
            g = g * (1.0F - darkenAmount) + g * 0.6F * darkenAmount;
            b = b * (1.0F - darkenAmount) + b * 0.6F * darkenAmount;
        }

        */

        return fogColor;
    }
    public Float3 GetSkyColor(float angleFromY)
    {
        Float3 color = sky_Color;

        float brightness = Maths.Cos(angleFromY) * 2.0F + 0.5F;
        brightness = Maths.Clamp(brightness, 0.0F, 1.0F);
        color *= brightness;

        return color;
    }

    public Float3 GetBrightnessDependentFogColor(Float3 color, float brightness)
    {
        return color *= new Float3(brightness * 0.94F + 0.06F, brightness * 0.94F + 0.06F, brightness * 0.91F + 0.09F);
    }
    public float GetStarBrightness(float angleFromY)
    {
        float value = 1.0F - (Maths.Cos(angleFromY) * 2.0F + 0.25F);
        value = Maths.Clamp(value, 0.0F, 1.0F);

        return value * value * 0.5F;
    }

    public float BlendLerp(float start, float end, float t)
    {
        return (t - start) / (end - start);
    }

    public void SetMoonPhase(float value)
    {
        moon.material.SetFloat("phase", value);
    }
}
