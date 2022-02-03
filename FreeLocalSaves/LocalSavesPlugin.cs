using System;
using System.Diagnostics;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace FreeLocalSaves {
    [BepInPlugin("ca.sanae.saves", "Free Local Saves", "1.0.0")]
    public class LocalSavesPlugin : BaseUnityPlugin {
        public new static ManualLogSource Logger { get; private set; }
        public static ConfigEntry<string> SaveFolder;
        public static ConfigEntry<bool> SaveToCloud;

        private void Awake() {
            Logger = base.Logger;
            SaveFolder = Config.Bind("General", "SaveFolderLocation", Application.persistentDataPath, "Base folder where the Taiko save folder will be located");
            SaveToCloud = Config.Bind("General", "SaveToCloud", false, "Whether to save your save to the cloud and to disk, or just to save to disk (this won't make anything load from cloud unless the mod is removed)");
            Harmony harmony = new Harmony("ca.sanae.saves");
            harmony.PatchAll();
        }

        public static string SaveFolderLocation => $"{SaveFolder.Value}/Saves";
        public static string GetSaveFileLocation(string filename) => $"{SaveFolderLocation}/{filename}";
    }

    public class DisableFullScreen {
        [HarmonyPatch(typeof(FocusManager), nameof(FocusManager.SetScreenType))]
        [HarmonyPatch(typeof(FocusManager), nameof(FocusManager.OnApplicationFocus))]
        [HarmonyPrefix]
        public static bool Prefix(FocusManager __instance) {
            Screen.fullScreen = true;
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow, Screen.currentResolution.refreshRate);
            return false;
        }
    }
}