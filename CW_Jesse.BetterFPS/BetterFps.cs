using HarmonyLib;
using BepInEx;

namespace CWJesse.BetterFPS {
    
    [BepInPlugin("CW_Jesse.BetterFPS", "Better FPS", "0.0.0")]
    public class BetterFps : BaseUnityPlugin {
        private readonly Harmony harmony = new Harmony("CW_Jesse.BetterFPS");
    
        void Awake() {
            harmony.PatchAll();

            Game.isModded = true;
        }
        
        void OnDestroy() {
            harmony.UnpatchSelf();
        }
    }
}
