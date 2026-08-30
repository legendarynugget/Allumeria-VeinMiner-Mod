using System;
using Allumeria;
using Allumeria.Input;
using Allumeria.UI;
using Allumeria.UI.UINodes;
using HarmonyLib;
using Ignitron.Aluminium.Events;
using Ignitron.Loader;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace VeinMiner
{
    public class VeinMinerMod : IModEntrypoint
    {
        public static VeinMinerMod Instance { get; private set; } = null!;
        public static ModBox ModBox { get; private set; } = null!;

        public static InputChannel? KeybindVeinMine;
        public static UIText? IndicatorText;

        public const int MaxVeinSize = 64;

        public void Main(ModBox box)
        {
            Instance = this;
            ModBox = box;

            Logger.Info("[VeinMiner] Initializing Vein Miner mod...");

            // Apply all Harmony patches
            Harmony harmony = new Harmony("com.allumeria.mod.veinminer");
            harmony.PatchAll();

            // Hook into Aluminium event to safely register UI after loading finishes
            ClientLoopEvents.LoadedEverything += OnLoadedEverything;
        }

        private static void OnLoadedEverything(Game game)
        {
            if (Game.menu_HUD?.panel_main != null && IndicatorText == null)
            {
                IndicatorText = (UIText)Game.menu_HUD.panel_main.RegisterNode(new UIText("vein_miner_indicator", "⛏ Vein Miner"));
                IndicatorText.color = new Vector4(0.3f, 1.0f, 0.5f, 1.0f);
                IndicatorText.show = false;
                Logger.Info("[VeinMiner] HUD Indicator registered successfully.");
            }
        }

        public static bool IsVeinMiningActive()
        {
            if (KeybindVeinMine == null) return false;
            try
            {
                return KeybindVeinMine.IsDown();
            }
            catch
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(Game), "OnLoad")]
        public static class Patch_Game_OnLoad
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                // Register custom keybind safely before keybinds are loaded from disk
                if (KeybindVeinMine == null)
                {
                    KeybindVeinMine = new InputChannel("vein_mine", Keys.V, InputChannel.ActionType.Both);
                }
            }
        }

        [HarmonyPatch(typeof(Game), "OnUpdateFrame")]
        public static class Patch_Game_OnUpdateFrame
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (IndicatorText != null && Game.menu_HUD != null)
                {
                    bool active = IsVeinMiningActive() && Game.menu_HUD.show && !Game.hideHUD && Game.inGame;
                    IndicatorText.show = active;

                    if (active)
                    {
                        // Position centered just above the active hotbar
                        IndicatorText.x = (UIManager.scaledWidth / 2) - 40;
                        IndicatorText.y = UIManager.scaledHeight - 65;
                    }
                }
            }
        }
    }
}