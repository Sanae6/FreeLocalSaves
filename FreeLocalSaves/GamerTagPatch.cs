using HarmonyLib;
using Microsoft.Xbox;

namespace FreeLocalSaves; 

[HarmonyPatch(typeof(GdkHelpers), nameof(GdkHelpers.GetGamerTag))]
public static class GamerTagPatch {
    public static bool Prefix(ref string __result) {
        if (LocalSavesPlugin.GamerTag.Value.Length == 0) return true;
        __result = LocalSavesPlugin.GamerTag.Value;
        return false;
    }
}