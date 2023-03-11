using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CWJesse.BetterFPS {
    [HarmonyPatch]
    public class BetterFps_Patch_MinFPS {
        private static ConfigEntry<int> ConfigMinFps;

        private const float MIN_FIXED_DELTA_TIME = 1.0f / 50.0f;
        private const float MAX_FIXED_DELTA_TIME = 1.0f / 20.0f;

        private const int DEFAULT_MINFPS = 20;
        private const int MIN_MINFPS = 5;
        private const int MAX_MINFPS = 120;
        
        private const float MIN_FRAME_TIME = 0.05f;
        private const float MAX_FRAME_TIME = 0.2f;
        private const float MAX_HICCUP_PERCENT = 1.25f;

        private static float FrameTimeAverage = Time.unscaledDeltaTime;
        // private static float FrameTimeAverageAverage = Time.unscaledDeltaTime;
        // private static float FrameTimeAverageAcceleration = 0.0f;
        private static float MaxFrameTime = Time.fixedDeltaTime;
        
        public static void InitConfig(ConfigFile config) {
            ConfigMinFps = config.Bind(
                "BetterFPS",
                "Minimum Target FPS",
                DEFAULT_MINFPS,
                new ConfigDescription("Automatically reduces frequency of physics & animation calculations to meet this target FPS.", 
                    new AcceptableValueRange<int>(MIN_MINFPS, MAX_MINFPS)));
        }
        
        [HarmonyPatch(typeof(Game), "Update")]
        [HarmonyPostfix]
        public static void MeetMinFps() {
            if (!BetterFps.ConfigEnabled.Value) return;
            
            FrameTimeAverage = Mathf.Lerp(FrameTimeAverage, Time.unscaledDeltaTime, Time.unscaledDeltaTime); // the lower the frame rate, the faster it finds the current frame rate
            // FrameTimeAverageAverage = Mathf.Lerp(FrameTimeAverage, FrameTimeAverage, Time.unscaledDeltaTime);
            // FrameTimeAverageAcceleration = FrameTimeAverage - FrameTimeAverageAverage;
            
            MaxFrameTime = Mathf.Clamp(MaxFrameTime * FrameTimeAverage * FrameTimeAverage + 3, MIN_FIXED_DELTA_TIME, MAX_FIXED_DELTA_TIME);
            Time.fixedDeltaTime = MaxFrameTime;
            
            // prevent hiccups greater than 25% at low frame rates, but also try for a minimum FPS of MIN_FRAME_TIME
            Time.maximumDeltaTime = Mathf.Clamp(FrameTimeAverage * MAX_HICCUP_PERCENT, MIN_FRAME_TIME, MAX_FRAME_TIME);
        }
        
        [HarmonyPatch(typeof(ConnectPanel), "UpdateFps")]
        [HarmonyPrefix]
        public static bool FpsCounterImprovements(ref Text ___m_fps, ref Text ___m_frameTime) {
            if (!BetterFps.ConfigEnabled.Value) return true;
            
            ___m_fps.text = $"{(1.0f/FrameTimeAverage):0}";
            ___m_frameTime.text = $"({(FrameTimeAverage * 1000f):0}ms)";

            return false;
        }
    }
}