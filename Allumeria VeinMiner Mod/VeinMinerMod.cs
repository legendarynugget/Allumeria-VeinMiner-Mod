using System;
using System.IO;
using Allumeria;
using Allumeria.Input;
using Allumeria.Rendering;
using Allumeria.UI;
using Allumeria.UI.UINodes;
using HarmonyLib;
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

            Logger.Info("[VeinMiner] Registering VeinMiner mod...");

            // Apply patches only. (Do NOT create InputChannel or UI nodes here!)
            Harmony harmony = new Harmony("com.allumeria.mod.veinminer");
            harmony.PatchAll();
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
                // Safe to register input channel once Game.OnLoad runs
                KeybindVeinMine = new InputChannel("vein_mine", Keys.V, InputChannel.ActionType.Both);
            }

            [HarmonyPostfix]
            public static void Postfix()
            {
                while (!Game.threadedLoadDone)
                {
                    // Wait for main loading thread
                }

                if (Game.menu_HUD?.panel_main != null)
                {
                    IndicatorText = (UIText)Game.menu_HUD.panel_main.RegisterNode(new UIText("vein_miner_indicator", "⛏ Vein Miner"));
                    IndicatorText.color = new Vector4(0.3f, 1.0f, 0.5f, 1f);
                    IndicatorText.show = false;
                }
            }
        }

        [HarmonyPatch(typeof(Game), "OnUpdateFrame")]
        public static class Patch_Game_OnUpdateFrame
        {
            public static void Prefix()
            {
                if (IndicatorText != null && Game.menu_HUD != null)
                {
                    bool active = IsVeinMiningActive() && Game.menu_HUD.show && !Game.hideHUD;
                    IndicatorText.show = active;

                    if (active)
                    {
                        // Position above the hotbar
                        IndicatorText.x = (UIManager.scaledWidth / 2) - 40;
                        IndicatorText.y = UIManager.scaledHeight - 65;
                    }
                }
            }
        }
    }
}