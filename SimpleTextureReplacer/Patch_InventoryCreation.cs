using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using System.Reflection;

namespace SimpleResourceReplacer
{
    [HarmonyPatch]
    static class Patch_InventoryCreation
    {
        public static bool hasRun = false;
        static IEnumerable<MethodBase> TargetMethods() => new MethodBase[]
        {
            AccessTools.Constructor(typeof(CommonInventoryData)),
            AccessTools.Method(typeof(CommonInventoryData), "Clear"),
            AccessTools.Method(typeof(CommonInventoryData), "InitDefault")
        };
        public static void Postfix(CommonInventoryData __instance)
        {
            hasRun = true;
            if (Main.logging)
                Main.logger.LogInfo("Adding custom skin items to inventory");
            foreach (var i in Main.equipmentFiles.Values)
            {
                if (Main.logging)
                    Main.logger.LogInfo("Adding item to inventory: " + Newtonsoft.Json.JsonConvert.SerializeObject(i.userItem));
                __instance.AddToCategories(i.userItem);
                ItemData.AddToCache(i.item);
            }
        }
    }
    [HarmonyPatch]
    static class Patch_InventoryUncreation
    {
        static IEnumerable<MethodBase> TargetMethods() => new MethodBase[]
        {
            AccessTools.Method(typeof(CommonInventoryData), "Reset"),
            AccessTools.Method(typeof(CommonInventoryData), "ReInit")
        };
        public static void Prefix()
        {
            Patch_InventoryCreation.hasRun = false;
        }
    }
}
