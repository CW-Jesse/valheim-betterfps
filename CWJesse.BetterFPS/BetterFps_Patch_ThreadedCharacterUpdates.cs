using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace CWJesse.BetterFPS {
    [HarmonyPatch]
    public class BetterFps_Patch_ThreadedCharacterUpdates {
        
        private static Dictionary<int, CharacterThread> characterThreads = new Dictionary<int, CharacterThread>();
        
        class CharacterThread {
            
            private Task task = Task.CompletedTask;

            private Character character;
            private ZNetView m_nview;

            public CharacterThread(Character character) {
                this.character = character;
                m_nview = (ZNetView)AccessTools.Field(typeof(Character), "m_nview").GetValue(character);

                UpdateLayer = (Action)Delegate.CreateDelegate(typeof(Action), character, AccessTools.Method(typeof(Character), "UpdateLayer"));
                UpdateContinousEffects = (Action)Delegate.CreateDelegate(typeof(Action), character, AccessTools.Method(typeof(Character), "UpdateContinousEffects"));
                UpdateWater = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), character, AccessTools.Method(typeof(Character), "UpdateWater"));
                UpdateGroundTilt = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), character, AccessTools.Method(typeof(Character), "UpdateGroundTilt"));
                SetVisible = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), character, AccessTools.Method(typeof(Character), "SetVisible"));
                UpdateLookTransition = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), character, AccessTools.Method(typeof(Character), "UpdateLookTransition"));
                UpdateGroundContact = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), character, AccessTools.Method(typeof(Character), "UpdateGroundContact"));
                UpdateNoise = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), character, AccessTools.Method(typeof(Character), "UpdateNoise"));
                UpdateStagger = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), character, AccessTools.Method(typeof(Character), "UpdateStagger"));
                UpdatePushback = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), character, AccessTools.Method(typeof(Character), "UpdatePushback"));
                UpdateMotion = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), character, AccessTools.Method(typeof(Character), "UpdateMotion"));
                UpdateSmoke = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), character, AccessTools.Method(typeof(Character), "UpdateSmoke"));
                UnderWorldCheck = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), character, AccessTools.Method(typeof(Character), "UnderWorldCheck"));
                SyncVelocity = (Action)Delegate.CreateDelegate(typeof(Action), character, AccessTools.Method(typeof(Character), "SyncVelocity"));
                CheckDeath = (Action)Delegate.CreateDelegate(typeof(Action), character, AccessTools.Method(typeof(Character), "CheckDeath"));
            }

            public void Start() {
                if (task.IsCompleted) {
                    FixedUpdateOriginal();
                    // task = Task.Run(() => FixedUpdateOriginal());
                }
            }

            private Action UpdateLayer;
            private Action UpdateContinousEffects;
            private Action<float> UpdateWater;
            private Action<float> UpdateGroundTilt;
            private Action<bool> SetVisible;
            private Action<float> UpdateLookTransition;
            private Action<float> UpdateGroundContact;
            private Action<float> UpdateNoise;
            private Action<float> UpdateStagger;
            private Action<float> UpdatePushback;
            private Action<float> UpdateMotion;
            private Action<float> UpdateSmoke;
            private Action<float> UnderWorldCheck;
            private Action SyncVelocity;
            private Action CheckDeath;
            private void FixedUpdateOriginal() {
                if (!m_nview.IsValid())
                    return;
                float fixedDeltaTime = Time.fixedDeltaTime;
                UpdateLayer();
                UpdateContinousEffects();
                UpdateWater(fixedDeltaTime);
                UpdateGroundTilt(fixedDeltaTime);
                SetVisible(m_nview.HasOwner());
                UpdateLookTransition(fixedDeltaTime);
                if (!m_nview.IsOwner())
                    return;
                UpdateGroundContact(fixedDeltaTime);
                UpdateNoise(fixedDeltaTime);
                character.GetSEMan().Update(fixedDeltaTime);
                UpdateStagger(fixedDeltaTime);
                UpdatePushback(fixedDeltaTime);
                task = Task.Run(() => {
                    UpdateMotion(fixedDeltaTime);
                });
                UpdateSmoke(fixedDeltaTime);
                UnderWorldCheck(fixedDeltaTime);
                SyncVelocity();
                CheckDeath();
            }
        }

        [HarmonyPatch(typeof(Character), "Awake")]
        [HarmonyPostfix]
        public static void OnAwake(ref Character __instance) {
            characterThreads.Add(__instance.GetHashCode(), new CharacterThread(__instance));
        }
        [HarmonyPatch(typeof(Character), nameof(Character.OnDestroy))]
        [HarmonyPrefix]
        public static bool OnDestroy(ref Character __instance) {
            characterThreads.Remove(__instance.GetHashCode());
            return true;
        }

        [HarmonyPatch(typeof(Character), "FixedUpdate")]
        [HarmonyPrefix]
        public static bool FixedUpdate(ref Character __instance) {
            if (characterThreads.TryGetValue(__instance.GetHashCode(), out CharacterThread characterThread)) {
                characterThread.Start();
            }
            return false;
        }
        
    }
}