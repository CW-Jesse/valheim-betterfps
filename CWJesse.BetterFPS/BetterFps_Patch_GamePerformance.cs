using HarmonyLib;
using UnityEngine;

namespace CWJesse.BetterFPS {
    [HarmonyPatch]
    public class BetterFps_Patch_GamePerformance {

        [HarmonyPatch(typeof(Game), "Awake")]
        [HarmonyPostfix]
        public static void ReducePhysicsCalculations() {
            Time.maximumDeltaTime = 1.0f/30.0f;
        }
        
    }
}