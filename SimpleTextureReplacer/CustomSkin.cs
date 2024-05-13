using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Globalization;
using System.Reflection;
using ExpansionMissionBoard;

namespace SimpleResourceReplacer
{
    [Serializable]
    public class CustomSkin : CustomDragonEquipment
    {
        public string[] TargetRenderers;
        public MaterialProperty[] MaterialData;
        [OptionalField]
        public MeshOverrides Mesh;
        [OptionalField]
        public MaterialProperty[] HWMaterialData;
        [OptionalField]
        public PartPaths BabyShaders;
        [OptionalField]
        public PartPaths TeenShaders;
        [OptionalField]
        public PartPaths AdultShaders;
        [OptionalField]
        public PartPaths TitanShaders;
        [OptionalField]
        public PartPaths HWBabyShaders;
        [OptionalField]
        public PartPaths HWTeenShaders;
        [OptionalField]
        public PartPaths HWAdultShaders;
        [OptionalField]
        public PartPaths HWTitanShaders;
        [Serializable]
        public class PartPaths
        {
            [OptionalField]
            public string Body;
            [OptionalField]
            public string Eyes;
            [OptionalField]
            public string Extra;
            [OptionalField]
            public bool BodyIsMaterial;
            [OptionalField]
            public bool EyesIsMaterial;
            [OptionalField]
            public bool ExtraIsMaterial;
            public (string path, bool material) BodySettings => (Body, BodyIsMaterial);
            public (string path, bool material) EyesSettings => (Eyes, EyesIsMaterial);
            public (string path, bool material) ExtraSettings => (Extra, ExtraIsMaterial);
        }
        //[OptionalField]
        //public string FireballPrefab; // Excluded until a good system is figured for detected "allowed" fireballs for a specific dragon
        [OptionalField]
        [NonSerialized]
        public DragonSkin skin;
        [OptionalField]
        [NonSerialized]
        public DragonSkin hwskin;


        protected override void Reapply()
        {
            if (Mesh != null)
            {
                if (!string.IsNullOrEmpty(Mesh.Baby))
                {
                    var k = new ResouceKey(Mesh.Baby);
                    if (Main.SingleAssets.TryGetValue(k, out var r) && r.TryReplace<Mesh>(out var m))
                    {
                        skin._BabyMesh = m;
                        if (hwskin && skin != hwskin)
                            hwskin._BabyMesh = m;
                    }
                    else
                        Main.logger.LogWarning($"Custom Skin requested baby mesh [bundle={k.bundle},resource={k.resource}] but no mesh was loaded");
                }
                if (!string.IsNullOrEmpty(Mesh.Teen))
                {
                    var k = new ResouceKey(Mesh.Teen);
                    if (Main.SingleAssets.TryGetValue(k, out var r) && r.TryReplace<Mesh>(out var m))
                    { 
                        skin._TeenMesh = m;
                        if (hwskin && skin != hwskin)
                            hwskin._TeenMesh = m;
                    }
                    else
                        Main.logger.LogWarning($"Custom Skin requested teen mesh [bundle={k.bundle},resource={k.resource}] but no mesh was loaded");
                }
                if (!string.IsNullOrEmpty(Mesh.Adult))
                {
                    var k = new ResouceKey(Mesh.Adult);
                    if (Main.SingleAssets.TryGetValue(k, out var r) && r.TryReplace<Mesh>(out var m))
                    { 
                        skin._Mesh = m;
                        if (hwskin && skin != hwskin)
                            hwskin._Mesh = m;
                    }
                    else
                        Main.logger.LogWarning($"Custom Skin requested adult mesh [bundle={k.bundle},resource={k.resource}] but no mesh was loaded");
                }
                if (!string.IsNullOrEmpty(Mesh.Titan))
                {
                    var k = new ResouceKey(Mesh.Titan);
                    if (Main.SingleAssets.TryGetValue(k, out var r) && r.TryReplace<Mesh>(out var m))
                    { 
                        skin._TitanMesh = m;
                        if (hwskin && skin != hwskin)
                            hwskin._TitanMesh = m;
                    }
                    else
                        Main.logger.LogWarning($"Custom Skin requested titan mesh [bundle={k.bundle},resource={k.resource}] but no mesh was loaded");
                }
            }
            EnsureMaterials();
            ApplyMaterialData(skin, MaterialData);
            if (HWMaterialData != null && HWMaterialData.Length != 0)
            {
                ApplyMaterialData(hwskin, MaterialData);
                ApplyMaterialData(hwskin, HWMaterialData);
            }
        }

        static void ApplyMaterialData(DragonSkin skin, MaterialProperty[] data)
        {
            foreach (var md in data)
            {
                var ms = md.Target.StartsWith("Baby") ? skin._BabyMaterials : md.Target.StartsWith("Teen") ? skin._TeenMaterials : md.Target.StartsWith("Adult") ? skin._Materials : md.Target.StartsWith("Titan") ? skin._TitanMaterials : null;
                if (ms == null)
                {
                    Main.logger.LogWarning($"Custom Skin material property target not found [target={md.Target},property={md.Property},value={md.Value}]");
                    continue;
                }
                if (md.Target.EndsWith("Extra") && ms.Length == 2)
                {
                    Main.logger.LogError($"Custom Skin material does not have an Extra layer [target={md.Target},property={md.Property},value={md.Value}]");
                    continue;
                }
                var ind = md.Target.EndsWith("Extra") ? 1 : md.Target.EndsWith("Body") ? 0 : md.Target.EndsWith("Eyes") ? ms.Length - 1 : md.Target.EndsWith("All") ? -2 : -1;
                void TrySetMaterialProperty(Material mat, MaterialProperty prop)
                {
                    if (!mat || !mat.shader)
                        return;
                    if (mat.HasProperty(prop.Property))
                    {
                        if (mat.HasTexture(prop.Property))
                        {
                            var k = new ResouceKey(prop.Value);
                            if (Main.SingleAssets.TryGetValue(k, out var r) && r.TryReplace<Texture>(out var tex))
                                mat.SetTexture(prop.Property, tex);
                            else
                                Main.logger.LogWarning($"Custom Skin material property requested texture [bundle={k.bundle},resource={k.resource}] but no texture was found [target={prop.Target},property={prop.Property},value={prop.Value}]");
                        }
                        else if (mat.HasFloat(prop.Property))
                        {
                            if (float.TryParse(prop.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var r))
                                mat.SetFloat(prop.Property, r);
                            else
                                Main.logger.LogWarning($"Custom Skin material property value is not a valid floating point number [target={prop.Target},property={prop.Property},value={prop.Value}]");
                        }
                        else if (mat.HasInt(prop.Property))
                        {
                            if (int.TryParse(prop.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var r))
                                mat.SetInt(prop.Property, r);
                            else
                                Main.logger.LogWarning($"Custom Skin material property value is not a valid integer [target={prop.Target},property={prop.Property},value={prop.Value}]");
                        }
                        else if (mat.HasVector(prop.Property))
                        {

                            if ((prop.Value.Length == 6 || prop.Value.Length == 8) && uint.TryParse(prop.Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r))
                                mat.SetColor(prop.Property, new Color32((byte)((r >> 16) & 0xFF), (byte)((r >> 8) & 0xFF), (byte)(r & 0xFF), prop.Value.Length == 6 ? (byte)((r >> 24) & 0xFF) : (byte)255));
                            else
                                try
                                {
                                    var a = Newtonsoft.Json.JsonConvert.DeserializeObject<float[]>(prop.Value, new Newtonsoft.Json.JsonSerializerSettings() { Culture = CultureInfo.InvariantCulture });
                                    mat.SetVector(prop.Property, new Vector4(a.Length > 0 ? a[0] : 0, a.Length > 1 ? a[1] : 0, a.Length > 2 ? a[2] : 0, a.Length > 3 ? a[3] : 0));
                                }
                                catch
                                {
                                    Main.logger.LogWarning($"Custom Skin material property value is not a valid vector/color (must be hex or an array of floating point numbers, example: \"[0, 1.3, 2]\") [target={prop.Target},property={prop.Property},value={prop.Value}]");
                                }
                        }
                        else
                            Main.logger.LogInfo($"Material {mat.name} ({mat.shader.name}) has property {prop.Property} but its type is unknown");
                    }
                    else
                        Main.logger.LogWarning($"Custom Skin material does not have that property [target={prop.Target},property={prop.Property},value={prop.Value}]");
                }
                foreach (var m in ind == -2 ? ms : ind == -1 ? new[] { ms[0], ms[ms.Length - 1] } : new[] { ms[ind] })
                    TrySetMaterialProperty(m,md);
            }
        }
        public override void Destroy()
        {
            base.Destroy();
            skin._BabyMaterials.DestroyAll();
            skin._TeenMaterials.DestroyAll();
            skin._Materials.DestroyAll();
            skin._TitanMaterials.DestroyAll();
            Object.Destroy(skin.gameObject);
            if (hwskin)
            {
                hwskin._BabyMaterials.DestroyAll();
                hwskin._TeenMaterials.DestroyAll();
                hwskin._Materials.DestroyAll();
                hwskin._TitanMaterials.DestroyAll();
                Object.Destroy(hwskin.gameObject);
            }
            customAssets.Remove(item.AssetName.After('/'));
        }
        protected override GameObject GetAsset(string key)
        {
            var flag = key.StartsWith("HW");
            if (Main.logging)
                Main.logger.LogInfo($"Found requested asset {(flag ? hwskin : skin)}");
            return (flag ? hwskin : skin)?.gameObject;
        }
        protected override void Setup()
        {
            item.AssetName = $"{Main.CustomBundleName}/DragonSkin_{ItemID}";
            item.Category = new[] { new ItemDataCategory() { CategoryId = Category.DragonSkin } };
            skin = new GameObject(item.AssetName.After('/')).AddComponent<DragonSkin>();
            Object.DontDestroyOnLoad(skin.gameObject);
            void EnsureShader(ref PartPaths paths, PartPaths plate = null)
            {
                if (paths == null)
                {
                    if (plate == null)
                        paths = new PartPaths();
                    else
                    {
                        paths = plate;
                        return;
                    }
                }
                if (paths.Body == null)
                {
                    paths.Body = plate == null ? MainShader : plate.Body;
                    paths.BodyIsMaterial = plate == null ? false : plate.BodyIsMaterial;
                }
                if (paths.Eyes == null)
                {
                    paths.Eyes = plate == null ? MainShader : plate.Eyes;
                    paths.EyesIsMaterial = plate == null ? false : plate.EyesIsMaterial;
                }
                if (paths.Eyes == null && plate != null)
                {
                    paths.Eyes = plate.Extra;
                    paths.EyesIsMaterial = plate.ExtraIsMaterial;
                }
            }
            EnsureShader(ref BabyShaders);
            EnsureShader(ref TeenShaders);
            EnsureShader(ref AdultShaders);
            EnsureShader(ref TitanShaders);
            EnsureShader(ref HWBabyShaders,BabyShaders);
            EnsureShader(ref HWTeenShaders,TeenShaders);
            EnsureShader(ref HWAdultShaders,AdultShaders);
            EnsureShader(ref HWTitanShaders,TitanShaders);
            if (MaterialData != null)
            {
                int GetInitHas(PartPaths paths) => paths.ExtraIsMaterial && paths.Extra != null ? 2 : (paths.BodyIsMaterial && paths.Body != null) || (paths.EyesIsMaterial && paths.Eyes != null) ? 1 : 0;
                var hasBaby = GetInitHas(BabyShaders);
                var hasTeen = GetInitHas(TeenShaders);
                var hasAdult = GetInitHas(AdultShaders);
                var hasTitan = GetInitHas(TitanShaders);
                void UpdateHas(string target, ref int has)
                {
                    if (has == 2)
                        return;
                    if (target.EndsWith("Extra") || target.EndsWith("All"))
                        has = 2;
                    else if (has == 0)
                        has = 1;
                }
                foreach (var d in MaterialData)
                    if (d.Target.StartsWith("Baby"))
                        UpdateHas(d.Target, ref hasBaby);
                    else if (d.Target.StartsWith("Teen"))
                        UpdateHas(d.Target, ref hasTeen);
                    else if (d.Target.StartsWith("Adult"))
                        UpdateHas(d.Target, ref hasAdult);
                    else if (d.Target.StartsWith("Titan"))
                        UpdateHas(d.Target, ref hasTitan);
                Material[] CreateMaterials(int has)
                {
                    if (has == 1)
                        return new Material[2];
                    if (has == 2)
                        return new Material[3];
                    return null;
                }
                void CheckShaderExtra(int has, PartPaths shaders, PartPaths hwshaders)
                {
                    if (has == 2)
                    {
                        if (shaders.Extra == null)
                        {
                            shaders.Extra = TransparentShader;
                            shaders.ExtraIsMaterial = false;
                        }
                        if (hwshaders.Extra == null)
                        {
                            hwshaders.Extra = shaders.Extra;
                            hwshaders.ExtraIsMaterial = shaders.ExtraIsMaterial;
                        }
                    }
                }
                CheckShaderExtra(hasBaby, BabyShaders, HWBabyShaders);
                CheckShaderExtra(hasTeen, TeenShaders, HWTeenShaders);
                CheckShaderExtra(hasAdult, AdultShaders, HWAdultShaders);
                CheckShaderExtra(hasTitan, TitanShaders, HWTitanShaders);

                skin._BabyMaterials = CreateMaterials(hasBaby);
                skin._TeenLODMaterials = skin._TeenMaterials = CreateMaterials(hasTeen);
                skin._LODMaterials = skin._Materials = CreateMaterials(hasAdult);
                skin._TitanLODMaterials = skin._TitanMaterials = CreateMaterials(hasTitan);
            }
            skin._RenderersToChange = TargetRenderers;
            if (HWMaterialData != null)
            {
                if (HWMaterialData.Length == 0)
                    hwskin = skin;
                else
                {
                    hwskin = Object.Instantiate(skin);
                    Object.DontDestroyOnLoad(hwskin.gameObject);
                    hwskin.name = "HW" + skin.name;
                    hwskin._BabyMaterials.InstatiateAll();
                    hwskin._TeenMaterials.InstatiateAll();
                    hwskin._TeenLODMaterials = hwskin._TeenMaterials;
                    hwskin._Materials.InstatiateAll();
                    hwskin._LODMaterials = hwskin._Materials;
                    hwskin._TitanMaterials.InstatiateAll();
                    hwskin._TitanLODMaterials = hwskin._TitanMaterials;
                }
            }
            EnsureMaterials();
            customAssets[item.AssetName.After('/')] = this;
            if (Main.logging)
                Main.logger.LogInfo($"Created skin {Name} as asset {item.AssetName.After('/')}");
        }
        void EnsureMaterials()
        {
            foreach ((DragonSkin skin, PartPaths baby, PartPaths teen, PartPaths adult, PartPaths titan) s in new[] {
                (skin, BabyShaders, TeenShaders, AdultShaders, TitanShaders),
                (hwskin, HWBabyShaders ?? BabyShaders, HWTeenShaders ?? TeenShaders, HWAdultShaders ?? AdultShaders, HWTitanShaders ?? TitanShaders)
            })
                if (s.skin)
                    foreach ((Material[] materials, PartPaths paths) a in new[] {
                        (s.skin._BabyMaterials, s.baby),
                        (s.skin._TeenMaterials, s.teen),
                        (s.skin._Materials, s.adult),
                        (s.skin._TitanMaterials, s.titan)
                    })
                    {
                        if (a.materials == null || a.paths == null)
                            continue;
                        for (int i = 0; i < a.materials.Length; i++)
                            if (!a.materials[i] || !a.materials[i].shader)
                            {
                                var t = i == 0 ? a.paths.BodySettings : i == a.materials.Length - 1 ? a.paths.EyesSettings : a.paths.ExtraSettings;
                                if (t.path == null)
                                    continue;
                                var name = ItemID
                                        + (s.skin == skin ? "" : "HW")
                                        + (s.skin._BabyMaterials == a.materials ? "Baby" : s.skin._TeenMaterials == a.materials ? "Teen" : s.skin._TitanMaterials == a.materials ? "Titan" : "Adult")
                                        + (i == 0 ? "Body" : i == a.materials.Length - 1 ? "Eyes" : "Extra");
                                if (t.material)
                                {
                                    if (Main.SingleAssets.TryGetValue(new ResouceKey(t.path), out var r) && r.TryReplace<Material>(out var m) && m && m.shader)
                                    {
                                        var nm = Object.Instantiate(m);
                                        nm.name = name;
                                        Main.Generated.GetOrCreate(m.name).Add(nm);
                                        a.materials[i] = nm;
                                    }
                                    else
                                    {
                                        Main.logger.LogWarning($"Material prefab asset \"{t.path}\" could not be found for \"{name}\"");
                                        a.materials[i] = null;
                                    }
                                }
                                else
                                {
                                    if (a.materials[i])
                                        Object.Destroy(a.materials[i]);
                                    a.materials[i] = CreateFromTemplate(name, t.path);
                                    if (!a.materials[i])
                                        Main.logger.LogWarning($"Shader asset \"{t.path}\" could not be found for \"{name}\"");
                                }
                            }
                    }
        }
        class TemplateCache
        {
            public TemplateCache(string ShaderName) => shaderName = ShaderName;
            string shaderName;
            Material material;
            Shader shader;
            public Material Create(string name)
            {
                if (!shader)
                {
                    shader = Shader.Find(shaderName);
                    if (!shader && shaderName.Contains("/") && Main.SingleAssets.TryGetValue(new ResouceKey(shaderName), out var a) && a.TryReplace<Shader>(out var s))
                        shader = s;
                    if (!shader)
                        shader = Resources.FindObjectsOfTypeAll<Shader>().FirstOrDefault(x => x.name == shaderName);
                    if (!shader)
                    {
                        Main.logger.LogWarning($"Shader \"{shaderName}\" was not found");
                        return null;
                    }
                }
                if (!material)
                {
                    material = new Material(shader);
                    if (material.HasTexture("_ColorMask"))
                    {
                        var cm = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                        cm.name = "NoColorMask";
                        cm.SetPixel(0, 0, Color.red);
                        cm.Apply();
                        material.SetTexture("_ColorMask", cm);
                    }
                }
                var n = Object.Instantiate(material);
                n.name = name;
                return n;
            }
        }
        const string MainShader = "JS Games/Dragon Bumped Spec";
        const string EyeShader = "JS Games/VertexLit";
        const string TransparentShader = "JS Games/Transparent/Vertex Lit";
        static Dictionary<string, TemplateCache> templates = new Dictionary<string, TemplateCache>()
        {
            { MainShader, new TemplateCache(MainShader) },
            { EyeShader, new TemplateCache(EyeShader) },
            { TransparentShader, new TemplateCache(TransparentShader) }
        };
        static Material CreateFromTemplate(string name,string shader = null)
        {
            if (string.IsNullOrEmpty(shader) || shader == "Default")
                shader = MainShader;
            else if (shader == "Eyes")
                shader = EyeShader;
            else if (shader == "Extra")
                shader = TransparentShader;
            if (!templates.TryGetValue(shader, out var cache))
                templates[shader] = cache = new TemplateCache(shader);
            return cache.Create(name);
        }
    }
    [Serializable]
    public class MaterialProperty
    {
        public string Property;
        public string Value;
        public string Target;
    }
    [Serializable]
    public class MeshOverrides
    {
        [OptionalField]
        public string Baby;
        [OptionalField]
        public string Teen;
        [OptionalField]
        public string Adult;
        [OptionalField]
        public string Titan;
    }
}
