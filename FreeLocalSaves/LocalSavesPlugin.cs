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
        public static ConfigEntry<string> GamerTag;

        private void Awake() {
            Logger = base.Logger;
            SaveFolder = Config.Bind("General", "SaveFolderLocation", Application.persistentDataPath, "Base folder where the Taiko save folder will be located");
            SaveToCloud = Config.Bind("General", "SaveToCloud", false, "Whether to save your save to the cloud and to disk, or just to save to disk (this won't make anything load from cloud unless the mod is removed)");
            GamerTag = Config.Bind("General", "FakeGamertag", "", "A name to replace your Gamer Tag, or a lack thereof if you aren't playing entirely legitimately :)");
            Harmony harmony = new Harmony("ca.sanae.saves");
            harmony.PatchAll();
        }

        public static string SaveFolderLocation => $"{SaveFolder.Value}/Saves";
        public static string GetSaveFileLocation(string filename) => $"{SaveFolderLocation}/{filename}";
    }
}