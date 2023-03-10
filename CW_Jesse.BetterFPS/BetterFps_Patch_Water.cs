using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace CWJesse.BetterFPS {
    
    [HarmonyPatch]
    public class BetterFps_Patch_Water {
        private const float MIN_UPDATE_DELTA_TIME = 0.1f;
        
        private static Dictionary<int, float> WaterVolumeLastUpdateTime = new Dictionary<int, float>();
        
        [HarmonyPatch(typeof(WaterVolume), "Update")]
        [HarmonyPrefix]
        public static bool WaterVolumeUpdates(ref WaterVolume __instance) {

            int instanceId = __instance.GetHashCode();
            if (!WaterVolumeLastUpdateTime.TryGetValue(instanceId, out float lastUpdate)) { WaterVolumeLastUpdateTime[instanceId] = Time.time; }
            
            if (Time.time - lastUpdate < MIN_UPDATE_DELTA_TIME) return false;
            WaterVolumeLastUpdateTime[instanceId] = Time.time;
            return true;
        }
        
        private static Dictionary<int, float> FishLastUpdateTime = new Dictionary<int, float>();
        
        [HarmonyPatch(typeof(Fish), "FixedUpdate")]
        [HarmonyPrefix]
        public static bool FishUpdates(ref Fish __instance) {

            int instanceId = __instance.GetHashCode();
            if (!FishLastUpdateTime.TryGetValue(instanceId, out float lastUpdate)) { FishLastUpdateTime[instanceId] = Time.time; }
            
            if (Time.time - lastUpdate < MIN_UPDATE_DELTA_TIME) return false;
            FishLastUpdateTime[instanceId] = Time.time;
            return true;
        }
        //
        // [HarmonyPatch(typeof(WaterVolume), nameof(WaterVolume.CalcWave))]
        // [HarmonyPrefix]
        // public static bool CalcWaveThreaded() {
        //     return true;
        // }
        //
        // public static float GetWaterSurfaceThreaded(Transform transform, float waveFactor = 1f) {
        //
        // }
        //
        //
        // private struct CalcWaveJob : IJob {
        //     public float wrappedDayTimeSeconds;
        //     public float depth;
        //     public float waterSurface;
        //     
        //     public NativeArray<float> result;
        //     
        //
        //     private static MethodInfo CalcWaveMI = AccessTools.Method(typeof(WaterVolume), "CalcWave");
        //
        //     private static Func<Vector3, float, float, float, float> CalcWave =
        //         (Func<Vector3, float, float, float, float>)Delegate.CreateDelegate(typeof(Func<Vector3, float, float, float, float>), CalcWaveMI);
        //    
        //     public void Execute() {
        //         float wrappedDayTimeSeconds = ZNet.instance.GetWrappedDayTimeSeconds();
        //         float depth = this.Depth(point);
        //         float waterSurface = this.transform.position.y + CalcWave(point, depth, wrappedDayTimeSeconds, waveFactor) + this.m_surfaceOffset;
        //         if ((double) Utils.LengthXZ(point) > 10500.0 && (double) this.m_forceDepth < 0.0)
        //             waterSurface -= 100f;
        //         return waterSurface;
        //     }
        // }
        
        
    }
}