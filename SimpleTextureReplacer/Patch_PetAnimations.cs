using HarmonyLib;
using PlayFab.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleResourceReplacer
{
    [HarmonyPatch]
    static class Patch_PetAnimations
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(SanctuaryPetItemData), "ApplyAnimBundles");
            yield return AccessTools.Method(typeof(SanctuaryPet), "ApplyAnimation");
            yield return AccessTools.Method(typeof(SanctuaryPet), "LoadAnimBundles");
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iL, MethodBase method)
        {
            var code = instructions.ToList();
            if (method.Name == "LoadAnimBundles")
            {
                var ind = code.FindIndex(x => x.operand is MethodInfo m && m.Name == "LoadAllAssetsAsync");
                code.Insert(ind + 1, new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_PetAnimations),nameof(RecordRequester))));
                code.Insert(ind, new CodeInstruction(OpCodes.Dup));
            }
            else
            {
                var loc = iL.DeclareLocal(typeof(string));
                var flag = method.Name == "ApplyAnimBundles";
                var loc2 = flag ? null : iL.DeclareLocal(typeof(AssetBundleRequest));
                if (!flag)
                    code.InsertRange(code.FindIndex(x => x.operand is MethodInfo m && m.Name == "get_Current") + 1, new[]
                    {
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Stloc,loc2)
                    });
                code.InsertRange(code.FindIndex(x => x.operand is MethodInfo m && m.Name == "AddClip"),
                    flag
                    ? new[]
                    {
                        new CodeInstruction(OpCodes.Stloc,loc),
                        new CodeInstruction(OpCodes.Ldloc_1),
                        new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_PetAnimations),nameof(ReplaceBundleClip))),
                        new CodeInstruction(OpCodes.Ldloc,loc)
                    }
                    : new[]
                    {
                        new CodeInstruction(OpCodes.Stloc,loc),
                        new CodeInstruction(OpCodes.Ldloc,loc2),
                        new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_PetAnimations),nameof(ReplaceRequestClip))),
                        new CodeInstruction(OpCodes.Ldloc,loc)
                    }
                );
            }
            return code;
        }
        static AnimationClip ReplaceBundleClip(AnimationClip clip, AssetBundle bundle) => ReplaceClip(clip, bundle.name);
        static AnimationClip ReplaceRequestClip(AnimationClip clip, AssetBundleRequest request) => requesterName.TryGetValue(request,out var bundle) ? ReplaceClip(clip, bundle) : clip;
        static AnimationClip ReplaceClip(AnimationClip clip, string bundle)
        {
            if (Main.logging)
                Main.logger.LogInfo($"LoadAnimation  bundle={bundle},resource={clip.name}");
            return Main.SingleAssets.TryGetValue(new ResouceKey(bundle, clip.name), out var replace) && replace.TryReplace<AnimationClip>(out var newAsset) ? newAsset : clip;
        }
        static ConditionalWeakTable<AssetBundleRequest, string> requesterName = new ConditionalWeakTable<AssetBundleRequest, string>();
        static AssetBundleRequest RecordRequester(AssetBundle bundle, AssetBundleRequest request)
        {
            requesterName.Add(request, bundle.name);
            return request;
        }
    }
}
