using System;
using System.Collections.Generic;
using Allumeria;
using Allumeria.Audio;
using Allumeria.Blocks.Blocks;
using Allumeria.ChunkManagement;
using Allumeria.EntitySystem.Entities;
using Allumeria.Items;
using Allumeria.Networking;
using Allumeria.Networking.Packets;
using Allumeria.Particles;
using OpenTK.Mathematics;

namespace VeinMiner
{
    public static class VeinMiningLogic
    {
        [ThreadStatic]
        public static bool IsMiningVein = false;

        public static bool IsVeinMineable(Block? b)
        {
            if (b == null || b == Block.empty) return false;

            // 1. Ore Material Types
            if (b.blockMaterial == BlockMaterial.hard_ore || b.blockMaterial == BlockMaterial.soft_ore) return true;

            // 2. Explicit Vanilla Ores
            if (b == Block.coal_ore || b == Block.copper_ore || b == Block.iron_ore ||
                b == Block.silver_ore || b == Block.gold_ore || b == Block.cobalt_ore ||
                b == Block.palladium_ore || b == Block.allumerium_ore || b == Block.amethyst_ore ||
                b == Block.sapphire_ore || b == Block.diamond_ore)
            {
                return true;
            }

            // 3. Modded Ores matching standard naming conventions
            if (!string.IsNullOrEmpty(b.strID))
            {
                string id = b.strID.ToLowerInvariant();
                if (id.EndsWith("_ore") || id.Contains("ore_") || id.Contains("_ore_"))
                {
                    return true;
                }
            }

            return false;
        }

        public static void MineVein(PlayerEntity player, World world, int originX, int originY, int originZ, ushort targetBlockId)
        {
            if (IsMiningVein) return;
            IsMiningVein = true;

            try
            {
                if (player == null || world?.chunkManager == null) return;

                ChunkManager chunkManager = world.chunkManager;
                Queue<Vector3i> queue = new Queue<Vector3i>();
                HashSet<Vector3i> visited = new HashSet<Vector3i>();
                HashSet<Chunk> modifiedChunks = new HashSet<Chunk>();

                Vector3i startPos = new Vector3i(originX, originY, originZ);
                queue.Enqueue(startPos);
                visited.Add(startPos);

                int blocksMined = 0;
                Block? originBlock = null;

                // Precompute 26 3D neighbor offsets (Moore neighborhood)
                List<Vector3i> neighborOffsets = new List<Vector3i>(26);
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dy == 0 && dz == 0) continue;
                            neighborOffsets.Add(new Vector3i(dx, dy, dz));
                        }
                    }
                }

                while (queue.Count > 0 && blocksMined < VeinMinerMod.MaxVeinSize)
                {
                    Vector3i current = queue.Dequeue();

                    // World bounds check
                    if (current.Y < 0 || current.Y > 255) continue;

                    int cx = current.X >> 5;
                    int cy = current.Y >> 5;
                    int cz = current.Z >> 5;

                    Chunk? chunk = chunkManager.RequestChunk(cx, cy, cz);
                    if (chunk == null) continue;

                    PaletteEntry pe = chunkManager.GetBlockWithMetadataFast(chunk, current.X, current.Y, current.Z);
                    if (pe.blockID == targetBlockId)
                    {
                        Block b = Block.GetBlockFromID(pe.blockID);
                        if (b != null && b != Block.empty)
                        {
                            if (originBlock == null) originBlock = b;

                            // 1. Remove block and update lighting
                            chunkManager.SetBlockWithUpdateAndLight(current.X, current.Y, current.Z, Block.empty, 0U, true, false, false);
                            chunkManager.MarkNeighboursDirty(current.X, current.Y, current.Z);
                            modifiedChunks.Add(chunk);

                            // 2. Spawn particles
                            ParticleBehaviour.block_break.Burst(new Vector3((float)current.X, (float)current.Y, (float)current.Z), 8, b);

                            // 3. Deliver drops (Only in Singleplayer / Server Host)
                            if (!NetworkManager.IsClient())
                            {
                                DeliverDropsToPlayer(player, world, b);
                            }

                            // 4. Send packet to server if playing on a multiplayer server
                            if (NetworkManager.IsClient())
                            {
                                NetworkManager.client.SendPacketToServer(new PacketPlayerBreakBlock((short)current.X, (short)current.Y, (short)current.Z));
                            }

                            blocksMined++;
                        }

                        // Search neighbors
                        foreach (Vector3i offset in neighborOffsets)
                        {
                            Vector3i next = current + offset;
                            if (next.Y >= 0 && next.Y <= 255 && !visited.Contains(next))
                            {
                                visited.Add(next);
                                queue.Enqueue(next);
                            }
                        }
                    }
                }

                // Ensure chunk save flags are set
                foreach (Chunk c in modifiedChunks)
                {
                    c.preserveForUpgrade = true;
                }

                // Audio polish: Play break sound and pitch-scaled pop sound
                if (blocksMined > 0)
                {
                    if (originBlock?.blockMaterial?.breakSound != null)
                    {
                        AudioPlayer.PlaySoundWorldRandom(originBlock.blockMaterial.breakSound, startPos, 1.0f);
                    }

                    if (AudioPlayer.pop != null)
                    {
                        float pitch = Math.Clamp(1.0f + (blocksMined * 0.015f), 1.0f, 1.6f);
                        AudioPlayer.PlaySoundWorld(AudioPlayer.pop, player.position, pitch, 0.8f);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("[VeinMiner] Error during vein mining: " + ex);
            }
            finally
            {
                IsMiningVein = false;
            }
        }

        private static void DeliverDropsToPlayer(PlayerEntity player, World world, Block b)
        {
            try
            {
                if (b.customLoot != null)
                {
                    List<ItemStack> drops = b.customLoot.GenerateLoot(world.random, player);
                    if (drops != null)
                    {
                        foreach (ItemStack stack in drops)
                        {
                            if (stack != null) GiveOrDrop(player, world, stack);
                        }
                    }
                }
                else
                {
                    Item dropItem = b.dropItem ?? b.item;
                    if (dropItem != null)
                    {
                        GiveOrDrop(player, world, new ItemStack(dropItem, 1));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("[VeinMiner] Drop delivery error: " + ex.Message);
            }
        }

        private static void GiveOrDrop(PlayerEntity player, World world, ItemStack stack)
        {
            if (player?.inventory?.inventory == null || stack == null) return;

            player.GiveItem(stack, out ItemStack remainder);

            if (remainder != null && remainder.amount > 0)
            {
                world.DropItemAt(remainder, player.position + new Vector3(0f, 0.25f, 0f), Vector3.Zero);
            }
        }
    }
}