using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;

namespace CWJesse.BetterFPS {
    
    [BepInPlugin("CW_Jesse.BetterFPS", "Better FPS", "0.0.0")]
    public class BetterFps : BaseUnityPlugin {
        
        private readonly Harmony harmony = new Harmony("CW_Jesse.BetterFPS");
    
        void Awake() {
            harmony.PatchAll();
            
            BetterFps_Patch_MinFPS.InitConfig(Config);

            Game.isModded = true;
        }
        
        void OnDestroy() {
            harmony.UnpatchSelf();
        }
    }
}
