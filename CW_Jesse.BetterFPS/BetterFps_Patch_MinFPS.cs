using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.AccessControl;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CWJesse.BetterFPS {
    [HarmonyPatch]
    public class BetterFps_Patch_MinFPS {
        private static ConfigEntry<int> ConfigMinFps;
        
        private const float TIME_MEASUREMENT_VELOCITY = 1.0f/16.0f;

        private const float MIN_FIXED_DELTA_TIME = 1.0f / 50.0f;
        private const float MAX_FIXED_DELTA_TIME = 1.0f / 20.0f;

        private const int MIN_MINFPS = 5;
        private const int MAX_MINFPS = 120;
        
        private const float MIN_FRAME_TIME = 0.05f;
        private const float MAX_FRAME_TIME = 0.2f;
        private const float MAX_HICCUP_PERCENT = 1.25f;

        private static float frameTimeAverage = Time.unscaledDeltaTime;
        // private static float desiredFps = 1.0f / 30.0f;
        
        public static void InitConfig(ConfigFile config) {
            ConfigMinFps = config.Bind(
                "BetterFPS",
                "Minimum Target FPS",
                30,
                new ConfigDescription("Automatically reduces frequency of physics & animation calculations to meet this target FPS.", 
                    new AcceptableValueRange<int>(MIN_MINFPS, MAX_MINFPS)));

            // ConfigMinFps.SettingChanged += (sender, args) => desiredFps = ConfigMinFps.Value;
        }
        
        // [HarmonyPatch(typeof(Game), "Awake")]
        // [HarmonyPostfix]
        // public static void Start() {
        //     Time.maximumDeltaTime = 1.0f / 20.0f; // this slows the game down, which feels worse than a low frame rate
        // }
        
        private static float maxFrameTime = Time.fixedDeltaTime;
        private static float prevMaximumDeltaTime = Time.maximumDeltaTime;
        
        [HarmonyPatch(typeof(Game), "Update")]
        [HarmonyPostfix]
        public static void MeetMinFps() {
            if (!BetterFps.ConfigEnabled.Value) return;
            
            maxFrameTime = Mathf.Clamp(maxFrameTime * frameTimeAverage * ConfigMinFps.Value, MIN_FIXED_DELTA_TIME, MAX_FIXED_DELTA_TIME);
            Time.fixedDeltaTime = maxFrameTime;
            
            // prevent hiccups greater than 25% at low frame rates, but also try for a minimum of 5 FPS
            var newMaximumDeltaTime = Mathf.Clamp(frameTimeAverage * MAX_HICCUP_PERCENT, MIN_FRAME_TIME, MAX_FRAME_TIME);
            if (newMaximumDeltaTime != prevMaximumDeltaTime) {
                Time.maximumDeltaTime = newMaximumDeltaTime;
                prevMaximumDeltaTime = newMaximumDeltaTime;
            }
        }
        
        [HarmonyPatch(typeof(ConnectPanel), "UpdateFps")]
        [HarmonyPrefix]
        public static bool FpsCounterImprovements(ref Text ___m_fps, ref Text ___m_frameTime) {
            if (!BetterFps.ConfigEnabled.Value) return true;
            
            frameTimeAverage = Mathf.Lerp(frameTimeAverage, Time.unscaledDeltaTime, Time.unscaledDeltaTime); // the lower the frame rate, the faster it finds the current frame rate
            ___m_fps.text = (1.0f/frameTimeAverage).ToString("0");
            ___m_frameTime.text = $"({(frameTimeAverage * 1000f).ToString("00")}ms)";

            return false;
        }

        // [HarmonyPatch(typeof(ConnectPanel), "UpdateFps")]
        // [HarmonyTranspiler]
        // public static IEnumerable<CodeInstruction> MakeFpsCounterAccurate(IEnumerable<CodeInstruction> instructions) {
        //     MethodInfo deltaTimeFI = AccessTools.Property(typeof(Time), nameof(Time.deltaTime)).GetMethod;
        //     MethodInfo unscaledDeltaTimeFI = AccessTools.Property(typeof(Time), nameof(Time.unscaledDeltaTime)).GetMethod;
        //
        //     foreach (var i in instructions) {
        //         if (i.Calls(deltaTimeFI))
        //             yield return new CodeInstruction(OpCodes.Call, unscaledDeltaTimeFI);
        //         else
        //             yield return i;                
        //     }
        // }
    }
}