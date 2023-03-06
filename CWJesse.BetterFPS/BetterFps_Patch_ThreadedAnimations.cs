using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace CW_Jesse.BetterFPS {

    class AnimationsInfo {
        public Task updateWalkingTask = Task.CompletedTask;
        public Dictionary<int, bool> setBoolCache = new Dictionary<int, bool>();
        public Dictionary<int, float> setFloatCache = new Dictionary<int, float>();
        public Animator m_animator;
        public ZNetView m_nview;
        private ZSyncAnimation m_zanim;
        public bool m_smoothCharacterSpeeds;
        public static int m_forwardSpeedID = ZSyncAnimation.GetHash("forward_speed");
        public static int m_sidewaySpeedID = ZSyncAnimation.GetHash("sideway_speed");
        
        
        private static FieldInfo m_animatorFieldInfo = AccessTools.Field(typeof(Character), "m_animator");
        private static FieldInfo m_nviewFieldInfo = AccessTools.Field(typeof(Character), "m_nview");
        private static FieldInfo m_zanimFieldInfo = AccessTools.Field(typeof(Character), "m_zanim");
        private static FieldInfo m_forwardSpeedIDFieldInfo = AccessTools.Field(typeof(ZSyncAnimation), "m_forwardSpeedID");
        private static FieldInfo m_sidewaySpeedIDFieldInfo = AccessTools.Field(typeof(ZSyncAnimation), "m_sidewaySpeedID");
        public AnimationsInfo(Character character) {
            m_animator = (Animator)m_animatorFieldInfo.GetValue(character);
            m_nview = (ZNetView)m_nviewFieldInfo.GetValue(character);
            m_zanim = (ZSyncAnimation)m_zanimFieldInfo.GetValue(character);
            m_smoothCharacterSpeeds = m_zanim.m_smoothCharacterSpeeds;
        }
    }
    
    [HarmonyPatch]
    public class VUPB_Patch_ThreadedAnimations {

        // private static MethodInfo zanimSetBool = AccessTools.Method(
        //     typeof(ZSyncAnimation),
        //     nameof(ZSyncAnimation.SetBool),
        //     new []{ typeof(int), typeof(bool) });
        // private static MethodInfo zanimSetFloat = AccessTools.Method(
        //     typeof(ZSyncAnimation),
        //     nameof(ZSyncAnimation.SetFloat),
        //     new []{ typeof(int), typeof(float) });
        //
        // [HarmonyPatch(typeof(Character), "UpdateWalking")]
        // [HarmonyTranspiler]
        // public static IEnumerable<CodeInstruction> RemoveZanimSets(IEnumerable<CodeInstruction> instructions) {
        //     return instructions.Where(i => !i.Calls(zanimSetBool) && !i.Calls(zanimSetFloat));
        // }

        private static Dictionary<int, AnimationsInfo> vupbAnimationInfos = new Dictionary<int, AnimationsInfo>();
        
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

        [HarmonyPatch(typeof(Character), "Awake")]
        [HarmonyPostfix]
        public static void OnAwake(ref Character __instance) {
            vupbAnimationInfos[__instance.GetHashCode()] = new AnimationsInfo(__instance);
        }
        [HarmonyPatch(typeof(Character), nameof(Character.OnDestroy))]
        [HarmonyPostfix]
        public static void OnDestroy(ref Character __instance) {
            if (vupbAnimationInfos.TryGetValue(__instance.GetHashCode(), out AnimationsInfo vupb_ai)) {
                vupb_ai.updateWalkingTask.Wait();
            }
            vupbAnimationInfos.Remove(__instance.GetHashCode());
        }
        
        [HarmonyPatch(typeof(Character), "UpdateWalking")]
        [HarmonyPostfix]
        public static void UpdateWalkingThreaded(ref Character __instance, ref ZSyncAnimation ___m_zanim) {
            
            if (vupbAnimationInfos.TryGetValue(__instance.GetHashCode(), out AnimationsInfo animationsInfo) && animationsInfo.updateWalkingTask.IsCompleted) {
                animationsInfo.updateWalkingTask = Task.Run(() => ZanimSets(animationsInfo));
            }
        }

        private static void ZanimSets(AnimationsInfo animationsInfo) {
            foreach (int i in walkAnimationFloatHashes) {
                SetFloatOriginal(animationsInfo, i);
            }
            foreach (int i in walkAnimationBoolHashes) {
                SetBoolOriginal(animationsInfo, i);
            }
        }

        [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetBool), typeof(int), typeof(bool))]
        [HarmonyPrefix]
        public static bool SetBoolCache(ref ZSyncAnimation __instance, int hash, bool value) {
            if (!walkAnimationFloatHashes.Contains(hash)) return true;
            vupbAnimationInfos[__instance.GetHashCode()].setBoolCache[hash] = value;
            return false;
        }

        [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetFloat), typeof(int), typeof(float))]
        [HarmonyPrefix]
        public static bool SetFloatCache(ref ZSyncAnimation __instance, int hash, float value) {
            if (!walkAnimationBoolHashes.Contains(hash)) return true;
            vupbAnimationInfos[__instance.GetHashCode()].setFloatCache[hash] = value;
            return false;
        }
        private static void SetBoolOriginal(AnimationsInfo ai, int hash) {
            if (!ai.setBoolCache.TryGetValue(hash, out bool value)) return;
            
            if (ai.m_animator.GetBool(hash) == value) return;
            ai.m_animator.SetBool(hash, value);
            if (ai.m_nview.GetZDO() == null || !ai.m_nview.IsOwner()) return;
            ai.m_nview.GetZDO().Set(438569 + hash, value);
        }

        private static void SetFloatOriginal(AnimationsInfo ai, int hash) {
            if (!ai.setFloatCache.TryGetValue(hash, out float value)) return;
            
            if ((double) Mathf.Abs(ai.m_animator.GetFloat(hash) - value) < 0.0099999997764825821) return;
            if (ai.m_smoothCharacterSpeeds && (hash == AnimationsInfo.m_forwardSpeedID || hash == AnimationsInfo.m_sidewaySpeedID))
                ai.m_animator.SetFloat(hash, value, 0.2f, Time.fixedDeltaTime);
            else ai.m_animator.SetFloat(hash, value);
            if (ai.m_nview.GetZDO() == null || !ai.m_nview.IsOwner()) return;
            ai.m_nview.GetZDO().Set(438569 + hash, value);
        }
    }
}