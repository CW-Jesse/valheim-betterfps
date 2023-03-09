using System.Net.Mime;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CWJesse.BetterFPS {
    [HarmonyPatch]
    public class BetterFps_Patch_MinFPS {

        [HarmonyPatch(typeof(Game), "Awake")]
        [HarmonyPostfix]
        public static void ContinuePhysicsNextFrameIfNeeded() {
            Time.maximumDeltaTime = 1.0f/30.0f;
        }

        [HarmonyPatch(typeof(ConnectPanel), "UpdateFps")]
        [HarmonyPrefix]
        public static bool MakeFpsCounterAccurate(
            ref float ___m_frameTimer,
            ref int ___m_frameSamples,
            ref Text ___m_fps,
            ref Text ___m_frameTime) {
            
            ___m_frameTimer += Time.unscaledDeltaTime;
            ++___m_frameSamples;
            if ((double) ___m_frameTimer <= 1.0) return false;
            
            float num = ___m_frameTimer / (float) ___m_frameSamples;
            ___m_fps.text = (1f / num).ToString("0.0");
            ___m_frameTime.text = "( " + (num * 1000f).ToString("00.0") + "ms )";
            ___m_frameSamples = 0;
            ___m_frameTimer = 0.0f;
            return false;
        }

    }
}