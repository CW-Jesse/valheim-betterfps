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
        
        public static void InitConfig(ConfigFile config) {
            ConfigMinFps = config.Bind(
                "BetterFPS",
                "Mininum Target FPS",
                30,
                new ConfigDescription("Attempt to meet this FPS by reducing frequency of physics & animation calculations.", new AcceptableValueRange<int>(1, 120)));
        }
        
        [HarmonyPatch(typeof(Game), "Awake")]
        [HarmonyPostfix]
        public static void ContinuePhysicsNextFrameIfNeeded() {
            Time.maximumDeltaTime = 1.0f; // this slows the game down, which feels worse than a low frame rate
        }
        
        private const float TIME_MEASUREMENT_VELOCITY = 1.0f/128.0f;
        private static float maxFrameTime = Time.fixedDeltaTime;
        
        [HarmonyPatch(typeof(Game), "Update")]
        [HarmonyPostfix]
        public static void MeetMinFps() {
            maxFrameTime = Mathf.Clamp(maxFrameTime * frameTimeAverage / (1.0f / ConfigMinFps.Value), 1.0f / 50.0f, 1.0f / 20.0f);
            Time.fixedDeltaTime = maxFrameTime;
        }
        
        private static float frameTimeAverage = Time.unscaledDeltaTime;
        [HarmonyPatch(typeof(ConnectPanel), "UpdateFps")]
        [HarmonyPrefix]
        public static bool FpsCounterImprovements(ref Text ___m_fps, ref Text ___m_frameTime) {
            frameTimeAverage = Mathf.Lerp(frameTimeAverage, Time.unscaledDeltaTime, TIME_MEASUREMENT_VELOCITY);
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