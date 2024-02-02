﻿using HarmonyLib;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SimpleResourceReplacer
{
    [HarmonyPatch(typeof(RsResourceManager))]
    static class Patch_RsResourceManager
    {
        [HarmonyPatch("LoadAssetFromBundle", typeof(string), typeof(string), typeof(RsResourceEventHandler), typeof(Type), typeof(bool), typeof(object))]
        [HarmonyPrefix]
        static void LoadAssetFromBundleAsync(string inBundleURL, string inAssetName, ref RsResourceEventHandler inCallback, Type inType, bool inDontDestroy = false, object inUserData = null)
        {
            if (Main.logging)
                Main.logger.LogInfo($"LoadAssetFromBundleAsync  bundle={inBundleURL},resource={inAssetName}");
            if ((typeof(Component).IsAssignableFrom(inType) || inType == typeof(GameObject)) ? Main.GameObjects.ContainsKey((inBundleURL, inAssetName)) : Main.SingleAssets.ContainsKey((inBundleURL, inAssetName)))
            {
                var originalCallback = inCallback;
                inCallback = (url, even, progress, obj, data) =>
                {
                    if (even == RsResourceLoadEvent.COMPLETE && obj as Object)
                    {
                        if (typeof(Component).IsAssignableFrom(inType) || inType == typeof(GameObject))
                            PatchMethods.TryChange(inBundleURL, inAssetName, obj is Component c ? c.gameObject : (GameObject)obj);
                        else if (Main.SingleAssets.TryGetValue((inBundleURL, inAssetName), out var r) && r.TryReplace(obj.GetType(), out var no))
                            obj = no;
                    }
                    originalCallback?.Invoke(url, even, progress, obj, data);
                };
            }
        }
        [HarmonyPatch("LoadAssetFromBundle", typeof(string), typeof(string), typeof(Type))]
        [HarmonyPrefix]
        static bool LoadAssetFromBundle_Prefix(string inBundlePath, string inAssetName, Type inType, ref object __result)
        {
            if (Main.logging)
                Main.logger.LogInfo($"LoadAssetFromBundle  bundle={inBundlePath},resource={inAssetName}");
            if (typeof(Component).IsAssignableFrom(inType) || inType == typeof(GameObject))
                return true;
            if (Main.SingleAssets.TryGetValue((inBundlePath,inAssetName),out var r) && r.TryReplace(inType,out var nObj))
            {
                __result = nObj;
                return false;
            }
            return true;
        }
        [HarmonyPatch("LoadAssetFromBundle", typeof(string), typeof(string), typeof(Type))]
        [HarmonyPostfix]
        static void LoadAssetFromBundle_Postfix(string inBundlePath, string inAssetName, Type inType, object __result)
        {
            var go = __result is GameObject g ? g : __result is Component c ? c.gameObject : null;
            if (go && Main.GameObjects.TryGetValue((inBundlePath, inAssetName), out var d))
                PatchMethods.TryChange(inBundlePath, inAssetName, go);
        }
    }
}