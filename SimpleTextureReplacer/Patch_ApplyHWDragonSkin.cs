﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;

namespace SimpleResourceReplacer
{
    [HarmonyPatch(typeof(SanctuaryPet), "ApplyCustomSkin")]
    static class Patch_ApplyHWDragonSkin
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            code.InsertRange(code.FindIndex(x => x.operand is FieldInfo f && f.Name == "_ResourcePath") + 1,
                new[] {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call,AccessTools.Method( typeof(Patch_ApplyHWDragonSkin),nameof(GetResourcePath)))
                });
            return code;
        }

        static string GetResourcePath(string original, SanctuaryPet pet)
        {
            if (pet && pet.pData?.Accessories != null)
                foreach (var a in pet.pData.Accessories)
                    if (a != null && a.Type == RaisedPetAccType.Materials.ToString())
                    {
                        var i = CommonInventoryData.pInstance.FindItem(pet.pData.GetAccessoryItemID(a));
                        if (i?.Item?.AssetName != null)
                        {
                            var k = new ResouceKey(i.Item.AssetName);
                            if (k.bundle == Main.CustomBundleName)
                            {
                                k.resource = "HW" + k.resource;
                                var s = CustomDragonEquipment.GetCustomAsset(k.resource);
                                if (s)
                                    return k.FullResourceString;
                            }
                        }
                    }
            return original;
        }
    }
}
