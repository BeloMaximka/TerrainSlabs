using HarmonyLib;
using System.Linq;
using TerrainSlabs.Source.Commands;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace TerrainSlabs.Source;

public class TerrainSlabsModSystem : ModSystem
{
    private Harmony harmonyInstance = null!;

    public override double ExecuteOrder() => 0.2;

    public override void StartPre(ICoreAPI api)
    {
        harmonyInstance = new(Mod.Info.ModID);
        if (api.Side == EnumAppSide.Client && !harmonyInstance.GetPatchedMethods().Any())
        {
            harmonyInstance.PatchAll();
        }
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        if (api is ICoreServerAPI sapi)
        {
            SlabGroupHelper.SetChiselBlockId(sapi.World.GetBlock("game:chiseledblock")?.BlockId ?? 0);

            // TODO: Move to config and get rid of tuples. Currently it is (wildcard, excludeWildcard)
            (string, string)[] slabCodes = [("soil-*", ""), ("sand-*", "sand-*-*"), ("gravel-*", "gravel-*-*"), ("forestfloor-*", "")];
            bool idsNonSequential = false;
            int prevBlockId = 0;
            int slabIdStart = 0;
            int slabIdEnd = 0;
            AssetLocation slabShapeCode = new("block/basic/slab/slab-down");
            foreach (var wildcard in slabCodes)
            {
                Block[] blocks = sapi.World.SearchBlocks(wildcard.Item1);
                foreach (var block in blocks.Where(block => wildcard.Item2.Length == 0 || !WildcardUtil.Match(new(wildcard.Item2), block.Code)))
                {
                    Block slabBlock = block.ProperClone();
                    slabBlock.Code.Domain = "terrainslabs";
                    slabBlock.SideOpaque.Fill(false);
                    slabBlock.SideOpaque[BlockFacing.indexDOWN] = true;
                    slabBlock.SideSolid[BlockFacing.indexEAST] = false;
                    slabBlock.SideSolid[BlockFacing.indexWEST] = false;
                    slabBlock.SideSolid[BlockFacing.indexNORTH] = false;
                    slabBlock.SideSolid[BlockFacing.indexSOUTH] = false;
                    slabBlock.EmitSideAo = 0;
                    foreach (var box in slabBlock.CollisionBoxes)
                    {
                        box.Y2 -= 0.5f;
                    }
                    foreach (var box in slabBlock.SelectionBoxes)
                    {
                        box.Y2 -= 0.5f;
                    }

                    slabBlock.Shape.Base = slabShapeCode;
                    if (slabBlock.Shape.Alternates is not null)
                    {
                        foreach (var shape in slabBlock.Shape.Alternates)
                        {
                            shape.Base = slabShapeCode;
                        }
                    }

                    sapi.RegisterBlock(slabBlock);
                    if (prevBlockId != 0 && slabBlock.BlockId - 1 != prevBlockId)
                    {
                        idsNonSequential = true;
                    }
                    if (slabIdStart == 0)
                    {
                        slabIdStart = slabBlock.BlockId;
                    }
                    slabIdEnd = slabBlock.BlockId;
                }
            }
            if (idsNonSequential)
            {
                sapi.Logger.Notification("Slab ids are not sequential, remapper will be used to group them in one place.");
                SlabGroupHelper.RemapSlabIdIntoGroup(sapi);
            }
            else
            {
                SlabGroupHelper.UpdateIdRange(slabIdStart, slabIdEnd);
            }
        }
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        WorldGenUtils.RegisterSlabReplacementWorldGenEvent(api);
        ReplaceWithTerrainSlabsCommand.Register(api);
        ReplaceBlockWithTerrainSlabCommand.Register(api);
    }

    public override void Dispose()
    {
        harmonyInstance.UnpatchAll(harmonyInstance.Id);
    }
}
