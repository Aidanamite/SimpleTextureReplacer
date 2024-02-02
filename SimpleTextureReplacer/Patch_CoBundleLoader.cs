﻿using HarmonyLib;
using UnityEngine;

namespace SimpleResourceReplacer
{
    [HarmonyPatch(typeof(CoBundleLoader))]
    static class Patch_CoBundleLoader
    {
        [HarmonyPatch("LoadTexture")]
        [HarmonyPrefix]
        static bool LoadTexture(AssetBundle bd, string s, ref Texture __result)
        {
            if (Main.logging)
                Main.logger.LogInfo($"LoadTexture  bundle={bd.name},resource={s}");
            if (Main.SingleAssets.TryGetValue((bd.name, s), out var r) && r.TryReplace<Texture>(out var nResult))
            {
                __result = nResult;
                return false;
            }
            return true;
        }
        [HarmonyPatch("LoadAudioClip")]
        [HarmonyPrefix]
        static bool LoadAudioClip(AssetBundle bd, string s, ref AudioClip __result)
        {
            if (Main.logging)
                Main.logger.LogInfo($"LoadAudioClip  bundle={bd.name},resource={s}");
            if (Main.SingleAssets.TryGetValue((bd.name, s), out var r) && r.TryReplace<AudioClip>(out var nResult))
            {
                __result = nResult;
                return false;
            }
            return true;
        }
        [HarmonyPatch("LoadGameObject")]
        [HarmonyPostfix]
        static void LoadGameObject(AssetBundle bd, string s, GameObject __result)
        {
            if (Main.logging)
                Main.logger.LogInfo($"LoadGameObject  bundle={bd.name},resource={s}");
            PatchMethods.TryChange(bd.name, s, __result);
        }
    }
}