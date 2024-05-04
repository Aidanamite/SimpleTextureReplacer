﻿using System;
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
        public Shaders BabyShaders;
        [OptionalField]
        public Shaders TeenShaders;
        [OptionalField]
        public Shaders AdultShaders;
        [OptionalField]
        public Shaders TitanShaders;
        [Serializable]
        public class Shaders {
            [OptionalField]
            public string Body;
            [OptionalField]
            public string Eyes;
            [OptionalField]
            public string Extra;
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
                    if (mat.HasProperty(prop.Property))
                    {
                        if (mat.HasTexture(prop.Property))
                        {
                            var k = new ResouceKey(prop.Value);
                            if (Main.SingleAssets.TryGetValue(k, out var r) && r.TryReplace<Texture>(out var tex))
                                mat.SetTexture(prop.Property, tex);
                            else
                                Main.logger.LogWarning($"Custom Skin material property requested texture [bundle={k.bundle},resource={k.resource}] but no texture was loaded [target={prop.Target},property={prop.Property},value={prop.Value}]");
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
            if (MaterialData != null)
            {
                var hasBaby = 0;
                var hasTeen = 0;
                var hasAdult = 0;
                var hasTitan = 0;
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
                Material[] CreateMaterials(int has,string prefix,Shaders shaders)
                {
                    if (has == 1)
                        return new[]
                        {
                            CreateFromTemplate(prefix + "Body",shaders?.Body),
                            CreateFromTemplate(prefix + "Eyes",shaders?.Eyes)
                        };
                    if (has == 2)
                        return new[]
                        {
                            CreateFromTemplate(prefix + "Body",shaders?.Body),
                            CreateFromTemplate(prefix + "Extra",shaders?.Extra ?? TransparentShader),
                            CreateFromTemplate(prefix + "Eyes",shaders?.Eyes)
                        };
                    return null;
                }
                skin._BabyMaterials = CreateMaterials(hasBaby,"Baby",BabyShaders);
                skin._TeenLODMaterials = skin._TeenMaterials = CreateMaterials(hasTeen, "Teen",TeenShaders);
                skin._LODMaterials = skin._Materials = CreateMaterials(hasAdult, "",AdultShaders);
                skin._TitanLODMaterials = skin._TitanMaterials = CreateMaterials(hasTitan, "Titan",TitanShaders);
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
                    hwskin._BabyMaterials.InstatiateAll((n, o) => n.name = "HW" + o.name);
                    hwskin._TeenMaterials.InstatiateAll((n, o) => n.name = "HW" + o.name);
                    hwskin._TeenLODMaterials = hwskin._TeenMaterials;
                    hwskin._Materials.InstatiateAll((n, o) => n.name = "HW" + o.name);
                    hwskin._LODMaterials = hwskin._Materials;
                    hwskin._TitanMaterials.InstatiateAll((n, o) => n.name = "HW" + o.name);
                    hwskin._TitanLODMaterials = hwskin._TitanMaterials;
                }
            }
            customAssets[item.AssetName.After('/')] = this;
            if (Main.logging)
                Main.logger.LogInfo($"Created skin {Name} as asset {item.AssetName.After('/')}");
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
