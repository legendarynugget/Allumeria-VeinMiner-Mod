using System;
using Allumeria;
using Allumeria.Blocks.Blocks;
using Allumeria.ChunkManagement;
using Allumeria.EntitySystem.Entities;
using HarmonyLib;
using OpenTK.Mathematics;

namespace VeinMiner.Patches
{
    [HarmonyPatch(typeof(PlayerEntity))]
    public static class BlockBreakPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("BreakBlockAt", new Type[] { typeof(Vector3i), typeof(World) })]
        public static bool Prefix(PlayerEntity __instance, Vector3i blockBreakPosition, World world)
        {
            try
            {
                if (VeinMiningLogic.IsMiningVein) return true;
                if (!VeinMinerMod.IsVeinMiningActive()) return true;
                if (world?.chunkManager == null) return true;

                PaletteEntry pe = world.chunkManager.GetBlockWithMetadata(blockBreakPosition.X, blockBreakPosition.Y, blockBreakPosition.Z);
                ushort blockId = pe.blockID;

                if (blockId != 0 && blockId != Block.empty.intID)
                {
                    Block? b = Block.GetBlockFromID(blockId);
                    if (VeinMiningLogic.IsVeinMineable(b))
                    {
                        // 1. Reset the player's internal punch counter so it doesn't overflow or break blocks every frame
                        __instance.currentPunchValue = 0;
                        __instance.selectScale = 0.1f;

                        // 2. Mine the vein cleanly
                        VeinMiningLogic.MineVein(__instance, world, blockBreakPosition.X, blockBreakPosition.Y, blockBreakPosition.Z, blockId);

                        // 3. Mark chunk preserved for upgrade (just like vanilla BreakBlockAt)
                        Chunk? chunk = world.chunkManager.RequestChunkFromCoords(blockBreakPosition.X, blockBreakPosition.Y, blockBreakPosition.Z);
                        if (chunk != null)
                        {
                            chunk.preserveForUpgrade = true;
                        }

                        // Suppress vanilla wall drop for the root block
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("[VeinMiner] Error in BreakBlockAt hook: " + ex);
            }

            return true;
        }
    }
}