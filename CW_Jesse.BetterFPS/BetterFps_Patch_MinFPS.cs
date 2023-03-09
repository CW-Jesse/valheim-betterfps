using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CWJesse.BetterFPS {
    [HarmonyPatch]
    public class BetterFps_Patch_MinFPS {

        [HarmonyPatch(typeof(Game), "Awake")]
        [HarmonyPostfix]
        public static void ContinuePhysicsNextFrameIfNeeded() {
            Time.maximumDeltaTime = 1.0f/30.0f;
        }

        [HarmonyPatch(typeof(ConnectPanel), "UpdateFps")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MakeFpsCounterAccurate(IEnumerable<CodeInstruction> instructions) {
            MethodInfo deltaTimeFI = AccessTools.Property(typeof(Time), nameof(Time.deltaTime)).GetMethod;
            MethodInfo unscaledDeltaTimeFI = AccessTools.Property(typeof(Time), nameof(Time.unscaledDeltaTime)).GetMethod;

            foreach (var i in instructions) {
                if (i.Calls(deltaTimeFI))
                    yield return new CodeInstruction(OpCodes.Call, unscaledDeltaTimeFI);
                else
                    yield return i;                
            }
        }

    }
}