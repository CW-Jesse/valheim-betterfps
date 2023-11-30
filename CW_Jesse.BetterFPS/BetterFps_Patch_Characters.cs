using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace CWJesse.BetterFPS {
    
    [HarmonyPatch]
    public class BetterFps_Patch_Characters {
        private const float MIN_UPDATE_DELTA_TIME = 0.05f;
        
        private static Dictionary<int, float> CharacterLastUpdateTime = new Dictionary<int, float>();
        
        private static Action<Character> UpdateLayer = (Action<Character>)Delegate.CreateDelegate(typeof(Action<Character>), AccessTools.Method(typeof(Character), "UpdateLayer"));
        private static Action<Character> UpdateContinousEffects = (Action<Character>)Delegate.CreateDelegate(typeof(Action<Character>), AccessTools.Method(typeof(Character), "UpdateContinousEffects"));
        private static Action<Character, float> UpdateWater = (Action<Character, float>)Delegate.CreateDelegate(typeof(Action<Character, float>), AccessTools.Method(typeof(Character), "UpdateWater"));
        private static Action<Character, float> UpdateGroundTilt = (Action<Character, float>)Delegate.CreateDelegate(typeof(Action<Character, float>), AccessTools.Method(typeof(Character), "UpdateGroundTilt"));
        private static Action<Character, bool> SetVisible = (Action<Character, bool>)Delegate.CreateDelegate(typeof(Action<Character, bool>), AccessTools.Method(typeof(Character), "SetVisible"));
        private static Action<Character, float> UpdateLookTransition = (Action<Character, float>)Delegate.CreateDelegate(typeof(Action<Character, float>), AccessTools.Method(typeof(Character), "UpdateLookTransition"));
        private static Action<Character, float> UpdateGroundContact = (Action<Character, float>)Delegate.CreateDelegate(typeof(Action<Character, float>), AccessTools.Method(typeof(Character), "UpdateGroundContact"));
        private static Action<Character, float> UpdateNoise = (Action<Character, float>)Delegate.CreateDelegate(typeof(Action<Character, float>), AccessTools.Method(typeof(Character), "UpdateNoise"));
        private static Action<Character, float> UpdateStagger = (Action<Character, float>)Delegate.CreateDelegate(typeof(Action<Character, float>), AccessTools.Method(typeof(Character), "UpdateStagger"));
        private static Action<Character, float> UpdatePushback = (Action<Character, float>)Delegate.CreateDelegate(typeof(Action<Character, float>), AccessTools.Method(typeof(Character), "UpdatePushback"));
        private static Action<Character, float> UpdateMotion = (Action<Character, float>)Delegate.CreateDelegate(typeof(Action<Character, float>), AccessTools.Method(typeof(Character), "UpdateMotion"));
        private static Action<Character, float> UpdateSmoke = (Action<Character, float>)Delegate.CreateDelegate(typeof(Action<Character, float>), AccessTools.Method(typeof(Character), "UpdateSmoke"));
        private static Action<Character, float> UnderWorldCheck = (Action<Character, float>)Delegate.CreateDelegate(typeof(Action<Character, float>), AccessTools.Method(typeof(Character), "UnderWorldCheck"));
        private static Action<Character> SyncVelocity = (Action<Character>)Delegate.CreateDelegate(typeof(Action<Character>), AccessTools.Method(typeof(Character), "SyncVelocity"));
        private static Action<Character> CheckDeath = (Action<Character>)Delegate.CreateDelegate(typeof(Action<Character>), AccessTools.Method(typeof(Character), "CheckDeath"));
        
        [HarmonyPatch(typeof(Character), nameof(Character.CustomFixedUpdate))]
        [HarmonyPrefix]
        public static bool CharacterUpdates(ref Character __instance, ref ZNetView ___m_nview, ref float ___m_lastGroundTouch, ref float ___m_jumpTimer, ref float ___m_acceleration) {
            if (!BetterFps.ConfigEnabled.Value) return true;
            
            ___m_acceleration = 50.0f / Time.fixedDeltaTime; // fix acceleration not being tied to fixedDeltaTime
            
            if (!___m_nview.IsValid()) return false;
            if (__instance is Player) return true;
            
            
            float fixedDeltaTime = Time.fixedDeltaTime;
            ZDO zDO = ___m_nview.GetZDO();

            UpdateLayer(__instance);
            UpdateContinousEffects(__instance);
            UpdateWater(__instance, fixedDeltaTime);
            UpdateGroundTilt(__instance, fixedDeltaTime);
            SetVisible(__instance, ___m_nview.HasOwner());
            UpdateLookTransition(__instance, fixedDeltaTime);
            if (!___m_nview.IsOwner()) return false;
            UpdateGroundContact(__instance, fixedDeltaTime);
            UpdateNoise(__instance, fixedDeltaTime);
            __instance.GetSEMan().Update(zDO, fixedDeltaTime);
            UpdateStagger(__instance, fixedDeltaTime);
            UpdatePushback(__instance, fixedDeltaTime);
            
            
            int instanceId = __instance.GetHashCode();
            if (!CharacterLastUpdateTime.TryGetValue(instanceId, out float lastUpdate)) { CharacterLastUpdateTime[instanceId] = Time.fixedTime; }

            if (Time.fixedTime - lastUpdate > MIN_UPDATE_DELTA_TIME) {
                float longFixedDeltaTime = Time.fixedTime - lastUpdate;
                CharacterLastUpdateTime[instanceId] = Time.fixedTime;
                
                // fix acceleration not being tied to fixedDeltaTime
                ___m_acceleration = 50.0f / longFixedDeltaTime;
                
                UpdateMotion(__instance, longFixedDeltaTime); // HEAVY
                
                // fix these values being set to Time.fixedDeltaTime instead of the local dt
                if (!__instance.IsDead() && !__instance.IsDebugFlying()) {
                    ___m_lastGroundTouch += -fixedDeltaTime + longFixedDeltaTime;
                    ___m_jumpTimer += -fixedDeltaTime + longFixedDeltaTime;
                }
            }
            
            
            UpdateSmoke(__instance, fixedDeltaTime);
            UnderWorldCheck(__instance, fixedDeltaTime);
            SyncVelocity(__instance);
            CheckDeath(__instance);

            return false;
        }


        
        // [HarmonyPatch(typeof(Character), "FixedUpdate")]
        // [HarmonyTranspiler]
        // public static IEnumerable<CodeInstruction> CharacterGetFixedTimeDelta(IEnumerable<CodeInstruction> instructions) {
        //     MethodInfo fixedDeltaTimeMI = AccessTools.Property(typeof(Time), nameof(Time.deltaTime)).GetMethod;
        //
        //     // BEFORE
        //     // [43 13 - 43 41]
        //     // IL_0047: ldarg.0      // this
        //     // IL_0048: call         float32 [UnityEngine.CoreModule]UnityEngine.Time::get_fixedDeltaTime()
        //     // IL_004d: stfld        float32 CWJesse.BetterFPS.BetterFps_Patch_Characters/'<CharacterGetFixedTimeDelta>d__3'::'<x>5__2'
        //
        //     // AFTER:
        //     // [34 13 - 34 66]
        //     // IL_0047: ldarg.0      // this
        //     // IL_0048: call         float32 [UnityEngine.CoreModule]UnityEngine.Time::get_fixedDeltaTime()
        //     // IL_004d: ldc.r4       0.1
        //     // IL_0052: add
        //     // IL_0053: stfld        float32 CWJesse.BetterFPS.BetterFps_Patch_Characters/'<CharacterGetFixedTimeDelta>d__3'::'<x>5__2'
        //
        //     var x = Time.fixedDeltaTime;
        //     
        //     foreach (var i in instructions) {
        //         if (i.Calls(fixedDeltaTimeMI)) {
        //             yield return i;
        //             yield return new CodeInstruction(OpCodes.Ldc_R4, MIN_UPDATE_DELTA_TIME);
        //             yield return new CodeInstruction(OpCodes.Add);
        //         } else {
        //             yield return i;   
        //         }
        //     }
        // }

    }
}