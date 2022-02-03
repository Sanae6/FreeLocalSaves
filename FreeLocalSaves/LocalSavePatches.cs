using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Xbox;
using UnityEngine;

namespace FreeLocalSaves {
    [HarmonyPatch(typeof(PlayDataManager), "SaveObjectAsync")]
    public static class SaveObjectAsync {
        public static async void RealSaveObjectAsync(PlayDataManager pdm) {
            try {
                pdm.GetSaveData(out SaveData saveData);
                if (saveData.dataBody.SaveDataVersion <= DataConst.SaveDataVersion) {
                    saveData.dataBody.SaveDataVersion = DataConst.SaveDataVersion;
                }

                if (!pdm.IsSaveProcessing) {
                    // LocalSavesPlugin.Logger.LogInfo("Save started");
                    pdm.IsSaveProcessing = true;
                    saveData.dataBody.playerInfo.deviceName = SystemInfo.deviceName;
                    saveData.dataBody.playerInfo.savetime = DateTime.Now;
                    byte[] array = await Task.Run(() => SaveDataProc.SaveObjectInfo(saveData));
                    if (!Directory.Exists(LocalSavesPlugin.SaveFolderLocation))
                        Directory.CreateDirectory(LocalSavesPlugin.SaveFolderLocation);
                    File.WriteAllBytes(LocalSavesPlugin.GetSaveFileLocation("save_data_blob"), array);
                    if (LocalSavesPlugin.SaveToCloud.Value)
                        SaveDataExtOutProc.SaveCloudData("x_game_save_default_container", "save_data_blob", ref array);
                    pdm.IsSaveProcessing = false;
                    // LocalSavesPlugin.Logger.LogInfo("Save completed!");
                }
            } catch (Exception e) {
                LocalSavesPlugin.Logger.LogError($"Failed to save! Reason: {e}");
            }
        }

        [HarmonyPrefix]
        public static bool Prefix(PlayDataManager __instance) {
            RealSaveObjectAsync(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayDataManager), "SaveObject")]
    public class SaveObject {
        [HarmonyPrefix]
        public static bool Prefix(PlayDataManager __instance) {
            __instance.GetSaveData(out SaveData saveData);
            if (!Directory.Exists(LocalSavesPlugin.SaveFolderLocation))
                Directory.CreateDirectory(LocalSavesPlugin.SaveFolderLocation);
            byte[] saveOut = SaveDataProc.SaveObjectInfo(saveData);
            File.WriteAllBytes(LocalSavesPlugin.GetSaveFileLocation("save_data_blob"), saveOut);
            if (LocalSavesPlugin.SaveToCloud.Value)
                SaveDataExtOutProc.SaveCloudData("x_game_save_default_container", "save_data_blob", ref saveOut);

            return false;
        }
    }

    [HarmonyPatch(typeof(PlayDataManager), "SaveSystemOptionAsync")]
    public class SaveSystemOptionAsync {
        public static void RealSaveObjectAsync(PlayDataManager pdm) {
            pdm.GetSystemOption(out SystemOption systemOption);
            byte[] sysOut = SaveDataProc.SaveObjectInfo(systemOption);
            if (!Directory.Exists(LocalSavesPlugin.SaveFolderLocation))
                Directory.CreateDirectory(LocalSavesPlugin.SaveFolderLocation);
            File.WriteAllBytes(LocalSavesPlugin.GetSaveFileLocation("system_data_blob"), sysOut);
            if (LocalSavesPlugin.SaveToCloud.Value)
                SaveDataExtOutProc.SaveCloudData("x_game_save_default_container", "system_data_blob", ref sysOut);
        }

        [HarmonyPrefix]
        public static bool Prefix(PlayDataManager __instance) {
            RealSaveObjectAsync(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayDataManager), "LoadData")]
    public class LoadData {
        public static async void Trollface(PlayDataManager pdm, string blobName) {
            bool isSystemOption = !blobName.StartsWith("save");
            string logTemp = isSystemOption ? "system options" : "save data";
            LocalSavesPlugin.Logger.LogInfo($"Loading {logTemp}");
            try {
                byte[] data = File.ReadAllBytes(LocalSavesPlugin.GetSaveFileLocation(blobName));
                // await Task.Delay(3000); // really fucking funny
                if (blobName == "save_data_blob") {
                    SaveDataProc.ReadObjectInfo(data, out SaveData saveData);
                    pdm.SetSaveData(ref saveData);
                    AccessTools.MethodDelegate<Action<PlayDataManager>>(AccessTools.FirstMethod(typeof(PlayDataManager), m => m.Name == "CheckSaveData"))(pdm);
                } else {
                    // it's system_data_blob instead
                    SaveDataProc.ReadObjectInfo(data, out SystemOption options);
                    LocalSavesPlugin.Logger.LogInfo($"sys 1 {options.isAgreements[0]} {options.isAgreements[1]} {options.isAgreements[2]} {options.IsValid()}");
                    pdm.SetSystemOption(ref options);
                    AccessTools.MethodDelegate<Action<PlayDataManager>>(AccessTools.FirstMethod(typeof(PlayDataManager), m => m.Name == "CheckSystemData"))(pdm);
                    LocalSavesPlugin.Logger.LogWarning($"sys 2 {options.isAgreements[0]} {options.isAgreements[1]} {options.isAgreements[2]} {options.IsValid()}");
                }

                LoadHelpers.Success = true;
            } catch (Exception e) {
                LocalSavesPlugin.Logger.LogWarning($"waaaa {e}");
                LoadHelpers.Failure = true;
            }

            LocalSavesPlugin.Logger.LogInfo($"Loaded {logTemp}!");
            LoadHelpers.Success = true;
        }

        public static bool Prefix(PlayDataManager __instance, string blobName, ref bool __result) {
            bool isSystemOption = !blobName.StartsWith("save");
            string logTemp = isSystemOption ? "system options" : "save data";
            Traverse pdmt = Traverse.Create<PlayDataManager>();
            try {
                LocalSavesPlugin.Logger.LogWarning($"Looking for {logTemp} in {LocalSavesPlugin.GetSaveFileLocation(blobName)} (exists = {File.Exists(LocalSavesPlugin.GetSaveFileLocation(blobName))})");
                if (File.Exists(LocalSavesPlugin.GetSaveFileLocation(blobName))) {
                    Trollface(__instance, blobName);
                    __result = true;
                } else {
                    __result = false;
                    LocalSavesPlugin.Logger.LogWarning($"Couldn't find {logTemp}, saving defaults");
                    if (isSystemOption) {
                        if (!Directory.Exists(LocalSavesPlugin.SaveFolderLocation))
                            Directory.CreateDirectory(LocalSavesPlugin.SaveFolderLocation);
                        SystemOption data = new SystemOption();
                        data.Reset();
                        byte[] saveOut = SaveDataProc.SaveObjectInfo(data);
                        File.WriteAllBytes(LocalSavesPlugin.GetSaveFileLocation("system_data_blob"), saveOut);
                    } else {
                        if (!Directory.Exists(LocalSavesPlugin.SaveFolderLocation))
                            Directory.CreateDirectory(LocalSavesPlugin.SaveFolderLocation);
                        SaveData data = new SaveData();
                        data.Reset();
                        byte[] saveOut = SaveDataProc.SaveObjectInfo(data);
                        File.WriteAllBytes(LocalSavesPlugin.GetSaveFileLocation("save_data_blob"), saveOut);
                    }

                    Trollface(__instance, blobName);
                    __result = true; // :)
                }
            } catch (Exception e) {
                LocalSavesPlugin.Logger.LogWarning($"Failed to load! Reason: {e}");
                // LoadHelpers.Failure = true;
                __result = false;
            }

            return false;
        }
    }

    [HarmonyPatch]
    public static class LoadHelpers {
        public static bool Success;
        public static bool Failure;

        [HarmonyPatch(typeof(GdkHelpers), nameof(GdkHelpers.ResetLoadFlag))]
        [HarmonyPrefix]
        public static bool ResetLoadFlag() {
            Success = false;
            Failure = false;
            return false;
        }

        [HarmonyPatch(typeof(GdkHelpers), nameof(GdkHelpers.IsLoadCompleted))]
        [HarmonyPrefix]
        public static bool IsLoadCompleted(ref bool __result) {
            // LocalSavesPlugin.Logger.LogInfo($"damn daniel ar ar ar ar ar {Success}");
            __result = Success;
            return false;
        }

        [HarmonyPatch(typeof(GdkHelpers), nameof(GdkHelpers.IsLoadFailure))]
        [HarmonyPrefix]
        public static bool IsLoadFailure(ref bool __result) {
            // LocalSavesPlugin.Logger.LogInfo($"he just like me fr!!! {Failure}");
            __result = Failure;
            return false;
        }
    }
}