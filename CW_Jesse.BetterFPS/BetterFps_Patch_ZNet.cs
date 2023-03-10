using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace CWJesse.BetterFPS {
    
    [HarmonyPatch]
    public class BetterFps_Patch_ZNet {
        private const float MIN_UPDATE_DELTA_TIME = 0.1f;
        
        private static Dictionary<int, float> ZNetSceneLastUpdateTime = new Dictionary<int, float>();
        
        [HarmonyPatch(typeof(ZNetScene), "Update")]
        [HarmonyPrefix]
        public static bool ZNetSceneUpdates(ref ZNetScene __instance) {
            if (!BetterFps.ConfigEnabled.Value) return true;

            int instanceId = __instance.GetHashCode();
            if (!ZNetSceneLastUpdateTime.TryGetValue(instanceId, out float lastUpdate)) { ZNetSceneLastUpdateTime[instanceId] = Time.time; }
            
            if (Time.time - lastUpdate < MIN_UPDATE_DELTA_TIME) return false;
            ZNetSceneLastUpdateTime[instanceId] = Time.time;
            return true;
        }
    }
}