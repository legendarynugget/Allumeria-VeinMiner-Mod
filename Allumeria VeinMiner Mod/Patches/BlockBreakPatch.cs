using Allumeria.Blocks.Blocks;
using Allumeria.ChunkManagement;
using Allumeria.EntitySystem.Entities;
using HarmonyLib;
using OpenTK.Mathematics;

namespace VeinMiner
{
    [HarmonyPatch(typeof(PlayerEntity), "BreakBlockAt")]
    public static class Patch_PlayerEntity_BreakBlockAt
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerEntity __instance, Vector3i blockBreakPosition, World world)
        {
            // Only execute for the local player when Vein Miner keybind is held
            if (__instance == null || world?.chunkManager == null || !__instance.IsSelf()) return;
            if (!VeinMinerMod.IsVeinMiningActive()) return;

            PaletteEntry entry = world.chunkManager.GetBlockWithMetadata(
                blockBreakPosition.X,
                blockBreakPosition.Y,
                blockBreakPosition.Z
            );

            Block block = Block.GetBlockFromID(entry.blockID);
            if (VeinMiningLogic.IsVeinMineable(block))
            {
                // Ensure player tool tier is sufficient for this ore
                if (VeinMiningLogic.CanPlayerMine(__instance, block))
                {
                    VeinMiningLogic.MineVein(__instance, world, blockBreakPosition.X, blockBreakPosition.Y, blockBreakPosition.Z, entry.blockID);
                }
            }
        }
    }
}