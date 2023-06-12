using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace CW_Jesse.BetterFPS {

    class AnimationsInfo {
        private static HashSet<AnimationsInfo> aniInfos = new HashSet<AnimationsInfo>();
        private static Task aniInfosTask = Task.CompletedTask;
        public Dictionary<int, bool> setBoolCache = new Dictionary<int, bool>();
        public Dictionary<int, float> setFloatCache = new Dictionary<int, float>();
        public Animator m_animator;
        public ZNetView m_nview;
        private ZSyncAnimation m_zanim;
        public bool m_smoothCharacterSpeeds;
        
        
        private static FieldInfo m_animatorFieldInfo = AccessTools.Field(typeof(Character), "m_animator");
        private static FieldInfo m_nviewFieldInfo = AccessTools.Field(typeof(Character), "m_nview");
        private static FieldInfo m_zanimFieldInfo = AccessTools.Field(typeof(Character), "m_zanim");
        public static int s_forwardSpeedID = (int)AccessTools.Field(typeof(ZSyncAnimation), "s_forwardSpeedID").GetValue(null);
        public static int s_sidewaySpeedID = (int)AccessTools.Field(typeof(ZSyncAnimation), "s_sidewaySpeedID").GetValue(null);

        public AnimationsInfo(Character character) {
            m_animator = (Animator)m_animatorFieldInfo.GetValue(character);
            m_nview = (ZNetView)m_nviewFieldInfo.GetValue(character);
            m_zanim = (ZSyncAnimation)m_zanimFieldInfo.GetValue(character);
            m_smoothCharacterSpeeds = m_zanim.m_smoothCharacterSpeeds;
            aniInfos.Add(this);
        }

        public void Dispose() {
            aniInfos.Remove(this);
        }

        public static void Start() {
            if (aniInfosTask.IsCompleted) {
                aniInfosTask = Task.Run(ZanimSets);
            }
        }

        private static void ZanimSets() {
            foreach (AnimationsInfo aniInfo in aniInfos) {
                foreach (int i in aniInfo.setFloatCache.Keys) {
                    aniInfo.SetFloatOriginal(i);
                }
                foreach (int i in aniInfo.setBoolCache.Keys) {
                    aniInfo.SetBoolOriginal(i);
                }
            }
        }
        
        private void SetBoolOriginal(int hash) {
            bool value = setBoolCache[hash];
            
            if (m_animator.GetBool(hash) == value) return;
            m_animator.SetBool(hash, value);
            if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
            m_nview.GetZDO().Set(438569 + hash, value);
        }

        private void SetFloatOriginal(int hash) {
            float value = setFloatCache[hash];
            
            if ((double) Mathf.Abs(m_animator.GetFloat(hash) - value) < 0.0099999997764825821) return;
            if (m_smoothCharacterSpeeds && (hash == s_forwardSpeedID || hash == s_sidewaySpeedID))
                m_animator.SetFloat(hash, value, 0.2f, Time.fixedDeltaTime);
            else m_animator.SetFloat(hash, value);
            if (m_nview.GetZDO() == null || !m_nview.IsOwner()) return;
            m_nview.GetZDO().Set(438569 + hash, value);
        }
    }
    
    [HarmonyPatch]
    public class BetterFps_Patch_ThreadedAnimations {

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

        private static Dictionary<int, AnimationsInfo> aniInfos = new Dictionary<int, AnimationsInfo>();

        private static FieldInfo m_animator = AccessTools.Field(typeof(ZSyncAnimation), "m_animator");
        private static FieldInfo m_nview = AccessTools.Field(typeof(ZSyncAnimation), "m_nview");
        private static FieldInfo m_smoothCharacterSpeeds = AccessTools.Field(typeof(ZSyncAnimation), "m_smoothCharacterSpeeds");

        [HarmonyPatch(typeof(Character), "Awake")]
        [HarmonyPostfix]
        public static void OnAwake(ref Character __instance) {
            aniInfos[__instance.GetHashCode()] = new AnimationsInfo(__instance);
        }
        [HarmonyPatch(typeof(Character), nameof(Character.OnDestroy))]
        [HarmonyPostfix]
        public static void OnDestroy(ref Character __instance) {
            if (aniInfos.TryGetValue(__instance.GetHashCode(), out AnimationsInfo vupb_ai)) {
                vupb_ai.Dispose();
            }
        }
        
        [HarmonyPatch(typeof(ZNetScene), "Update")]
        [HarmonyPostfix]
        public static void UpdateWalkingThreaded(ref Character __instance) {
            AnimationsInfo.Start();
        }

        [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetBool), typeof(int), typeof(bool))]
        [HarmonyPrefix]
        public static bool SetBoolCache(ref ZSyncAnimation __instance, int hash, bool value) {
            if (!aniInfos.TryGetValue(__instance.GetHashCode(), out AnimationsInfo aniInfo)) return true;
            aniInfo.setBoolCache[hash] = value;
            return false;
        }

        [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetFloat), typeof(int), typeof(float))]
        [HarmonyPrefix]
        public static bool SetFloatCache(ref ZSyncAnimation __instance, int hash, float value) {
            if (!aniInfos.TryGetValue(__instance.GetHashCode(), out AnimationsInfo aniInfo)) return true;
            aniInfo.setFloatCache[hash] = value;
            return false;
        }
    }
}