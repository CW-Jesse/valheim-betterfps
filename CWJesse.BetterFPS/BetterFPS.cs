using System.Collections.Generic;
using System.Reflection;
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

        private static HashSet<int> walkAnimationFloatHashes = new HashSet<int>(new [] {
            ZSyncAnimation.GetHash("forward_speed"),
            ZSyncAnimation.GetHash("sideway_speed"),
            ZSyncAnimation.GetHash("turn_speed")
        });
        private static HashSet<int> walkAnimationBoolHashes = new HashSet<int>(new [] {
            ZSyncAnimation.GetHash("inWater"),
            ZSyncAnimation.GetHash("onGround"),
            ZSyncAnimation.GetHash("encumbered"),
            ZSyncAnimation.GetHash("flying")
        });

        private static FieldInfo m_animator = AccessTools.Field(typeof(ZSyncAnimation), "m_animator");
        private static FieldInfo m_nview = AccessTools.Field(typeof(ZSyncAnimation), "m_nview");
        private static FieldInfo m_smoothCharacterSpeeds = AccessTools.Field(typeof(ZSyncAnimation), "m_smoothCharacterSpeeds");
        
        [HarmonyPatch(typeof(Character), "UpdateWalking")]
        [HarmonyPostfix]
        public static void UpdateWalkingThreaded(ref Character __instance, ref ZSyncAnimation ___m_zanim) {
            
            if (!updateWalkingTasks.TryGetValue(__instance, out Task t) || t.IsCompleted) {
                int zanimId = ___m_zanim.GetHashCode();
                
                Animator animator = (Animator)m_animator.GetValue(___m_zanim);
                ZNetView znv = (ZNetView)m_nview.GetValue(___m_zanim);
                ZDO zdo = znv.GetZDO();
                bool isOwner = znv.IsOwner();
                bool smoothSpeeds = (bool)m_smoothCharacterSpeeds.GetValue(___m_zanim);
                
                updateWalkingTasks[__instance] = Task.Run(() => {
                    foreach (int i in walkAnimationFloatHashes) {
                        SetFloatOriginal(i, setFloatCache[(zanimId, i)], animator, zdo, isOwner, smoothSpeeds);
                    }
                    foreach (int i in walkAnimationBoolHashes) {
                        SetBoolOriginal(i, setBoolCache[(zanimId, i)], animator, zdo, isOwner);
                    }
                });
            }
        }

        [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetBool), typeof(int), typeof(bool))]
        [HarmonyPrefix]
        public static bool SetBoolCache(ref ZSyncAnimation __instance, int hash, bool value) {
            if (!walkAnimationFloatHashes.Contains(hash)) return true;
            setBoolCache[(__instance.GetHashCode(), hash)] = value;
            return false;
        }

        [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetFloat), typeof(int), typeof(float))]
        [HarmonyPrefix]
        public static bool SetFloatCache(ref ZSyncAnimation __instance, int hash, float value) {
            if (!walkAnimationBoolHashes.Contains(hash)) return true;
            setFloatCache[(__instance.GetHashCode(), hash)] = value;
            return false;
        }
        private static void SetBoolOriginal(int hash, bool value, Animator ___m_animator, ZDO zdo, bool isOwner) {
            if (___m_animator.GetBool(hash) == value) return;
            ___m_animator.SetBool(hash, value);
            if (zdo == null || !isOwner) return;
            zdo.Set(438569 + hash, value);
        }

        private static FieldInfo m_forwardSpeedID = AccessTools.Field(typeof(ZSyncAnimation), "m_forwardSpeedID");
        private static FieldInfo m_sidewaySpeedID = AccessTools.Field(typeof(ZSyncAnimation), "m_sidewaySpeedID");
        private static void SetFloatOriginal(int hash, float value, Animator m_animator, ZDO zdo, bool isOwner, bool m_smoothCharacterSpeeds){
            if ((double) Mathf.Abs(m_animator.GetFloat(hash) - value) < 0.0099999997764825821) return;
            if (m_smoothCharacterSpeeds && (hash == (int)m_forwardSpeedID.GetValue(null) || hash == (int)m_sidewaySpeedID.GetValue(null)))
                m_animator.SetFloat(hash, value, 0.2f, Time.fixedDeltaTime);
            else m_animator.SetFloat(hash, value);
            if (zdo == null || !isOwner) return;
            zdo.Set(438569 + hash, value);
        }
    }
    
}
