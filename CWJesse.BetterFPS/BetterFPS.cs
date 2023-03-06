using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using BepInEx;
using UnityEngine;

namespace CWJesse.BetterFPS {
    
    [BepInPlugin("CW_Jesse.BetterFPS", "Better FPS", "0.0.0")]
    public class BetterFps : BaseUnityPlugin {
        private readonly Harmony harmony = new Harmony("CW_Jesse.BetterFPS");
    
        void Awake() {
            harmony.PatchAll();
        }
        
        void OnDestroy() {
            harmony.UnpatchSelf();
        }
    }
    
    
    // animation culling looks fine
    // Animator.GetBoolID might be slow, but caching it here with `Dictionary<(ZSyncAnimation, int), bool>` bool isn't faster
    // which leaves putting ZSyncAnimation.SetBool / ZSyncAnimation.SetFloat on their own thread

    [HarmonyPatch]
    public class BetterFps_Patch_ThreadedAnimations {
        private static Dictionary<Character, Task> updateWalkingTasks = new Dictionary<Character, Task>();

        private static Dictionary<(int, int), bool> setBoolCache =
            new Dictionary<(int, int), bool>();

        private static Dictionary<(int, int), float> setFloatCache =
            new Dictionary<(int, int), float>();

        private static int forward_speed = ZSyncAnimation.GetHash("forward_speed");
        private static int sideway_speed = ZSyncAnimation.GetHash("sideway_speed");
        private static int turn_speed = ZSyncAnimation.GetHash("turn_speed");
        private static int inWater = ZSyncAnimation.GetHash("inWater");
        private static int onGround = ZSyncAnimation.GetHash("onGround");
        private static int encumbered = ZSyncAnimation.GetHash("encumbered");
        private static int flying = ZSyncAnimation.GetHash("flying");
        
        [HarmonyPatch(typeof(Character), "UpdateWalking")]
        [HarmonyPostfix]
        public static void UpdateWalkingThreaded(ref Character __instance, ref ZSyncAnimation ___m_zanim) {
            
            if (!updateWalkingTasks.TryGetValue(__instance, out Task t) || t.IsCompleted) {
                int zanimId = ___m_zanim.GetHashCode();
                
                Animator animator = (Animator)AccessTools.Field(typeof(ZSyncAnimation), "m_animator").GetValue(___m_zanim);
                ZNetView znv = (ZNetView)AccessTools.Field(typeof(ZSyncAnimation), "m_nview").GetValue(___m_zanim);
                ZDO zdo = znv.GetZDO();
                bool isOwner = znv.IsOwner();
                bool smoothSpeeds = (bool)AccessTools.Field(typeof(ZSyncAnimation), "m_smoothCharacterSpeeds").GetValue(___m_zanim);
                
                updateWalkingTasks[__instance] = Task.Run(() => {
                    SetFloatOriginal(forward_speed, setFloatCache[(zanimId, forward_speed)], animator, zdo, isOwner, smoothSpeeds);
                    SetFloatOriginal(sideway_speed, setFloatCache[(zanimId, sideway_speed)], animator, zdo, isOwner, smoothSpeeds);
                    SetFloatOriginal(turn_speed, setFloatCache[(zanimId, turn_speed)], animator, zdo, isOwner, smoothSpeeds);
                    SetBoolOriginal(inWater, setBoolCache[(zanimId, inWater)], animator, zdo, isOwner);
                    SetBoolOriginal(onGround, setBoolCache[(zanimId, onGround)], animator, zdo, isOwner);
                    SetBoolOriginal(encumbered, setBoolCache[(zanimId, encumbered)], animator, zdo, isOwner);
                    SetBoolOriginal(flying, setBoolCache[(zanimId, flying)], animator, zdo, isOwner);
                });
            }
        }

        [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetBool), typeof(int), typeof(bool))]
        [HarmonyPrefix]
        public static bool SetBoolCache(ref ZSyncAnimation __instance, int hash, bool value) {
            setBoolCache[(__instance.GetHashCode(), hash)] = value;
            return false;
        }

        [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetFloat), typeof(int), typeof(float))]
        [HarmonyPrefix]
        public static bool SetFloatCache(ref ZSyncAnimation __instance, int hash, float value) {
            setFloatCache[(__instance.GetHashCode(), hash)] = value;
            return false;
        }
        private static void SetBoolOriginal(int hash, bool value, Animator ___m_animator, ZDO zdo, bool isOwner) {
            if (___m_animator.GetBool(hash) == value) return;
            ___m_animator.SetBool(hash, value);
            if (zdo == null || !isOwner) return;
            zdo.Set(438569 + hash, value);
        }
        private static void SetFloatOriginal(int hash, float value, Animator m_animator, ZDO zdo, bool isOwner, bool m_smoothCharacterSpeeds){
            if ((double) Mathf.Abs(m_animator.GetFloat(hash) - value) < 0.0099999997764825821) return;
            if (m_smoothCharacterSpeeds && (hash == (int)AccessTools.Field(typeof(ZSyncAnimation), "m_forwardSpeedID").GetValue(null) || hash == (int)AccessTools.Field(typeof(ZSyncAnimation), "m_sidewaySpeedID").GetValue(null)))
                m_animator.SetFloat(hash, value, 0.2f, Time.fixedDeltaTime);
            else m_animator.SetFloat(hash, value);
            if (zdo == null || !isOwner) return;
            zdo.Set(438569 + hash, value);
        }
    }
    
}
