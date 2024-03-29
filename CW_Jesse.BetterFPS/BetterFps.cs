﻿using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace CWJesse.BetterFPS {
    
    [BepInPlugin("CW_Jesse.BetterFPS", "CW_Jesse's Valheim Framerate Booster", "1.1.2")]
    public class BetterFps : BaseUnityPlugin {
        
        public static ConfigEntry<bool> ConfigEnabled;
        private static float OriginalFixedDeltaTime = Time.fixedDeltaTime;
        private static float OriginalMaximumDeltaTime = Time.maximumDeltaTime;
        
        private readonly Harmony harmony = new Harmony("CW_Jesse.BetterFPS");
        
        void Awake() {
            harmony.PatchAll();
            
            BetterFps.InitConfig(Config);
            BetterFps_Patch_MinFPS.InitConfig(Config);

            Game.isModded = true;
        }
        
        void OnDestroy() {
            harmony.UnpatchSelf();
        }
        
        private static void InitConfig(ConfigFile config) {
            ConfigEnabled = config.Bind(
                "BetterFPS",
                "Enabled",
                true,
                new ConfigDescription("Enable mod"));

            ConfigEnabled.SettingChanged += (sender, args) => {
                if (ConfigEnabled.Value) {
                    // BetterFps_Patch_MinFPS.Start();
                } else {
                    Time.fixedDeltaTime = OriginalFixedDeltaTime; // re-enabled by BetterFps_Patch_MinFPS.MeetMinFps()
                    Time.maximumDeltaTime = OriginalMaximumDeltaTime; // re-enabled by BetterFps_Patch_MinFPS.Start()
                }
            };
        }
    }
}
