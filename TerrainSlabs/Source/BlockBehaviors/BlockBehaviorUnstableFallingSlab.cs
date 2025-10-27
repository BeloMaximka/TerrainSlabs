using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source.BlockBehaviors;

/// <summary>
/// Modified version of <see cref="BlockBehaviorUnstableFalling"/>
/// </summary>
/// <param name="slab"></param>
public class BlockBehaviorUnstableFallingSlab(Block slab) : BlockBehavior(slab)
{
    private float dustIntensity;
    private float fallSidewaysChance = 0.3f;
    private AssetLocation? fallSound;
    private float impactDamageMul;
    private Cuboidi[]? attachmentAreas;
    private BlockFacing[]? attachableFaces;
    private AssetLocation[]? exceptions;
    private bool fallSideways;

    string? fallingBlockCode;
    private Block? fallingBlock;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        if (fallingBlockCode is not null)
        {
            fallingBlock = api.World.GetBlock(fallingBlockCode);
        }
    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        fallingBlockCode = properties["fallingBlock"].AsString();

        attachableFaces = null;

        if (properties["attachableFaces"].Exists)
        {
            string[] faces = properties["attachableFaces"].AsArray<string>();
            attachableFaces = new BlockFacing[faces.Length];

            for (int i = 0; i < faces.Length; i++)
            {
                attachableFaces[i] = BlockFacing.FromCode(faces[i]);
            }
        }

        var areas = properties["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
        attachmentAreas = new Cuboidi[6];
        if (areas != null)
        {
            foreach (var val in areas)
            {
                val.Value.Origin.Set(8, 8, 8);
                BlockFacing face = BlockFacing.FromFirstLetter(val.Key[0]);
                attachmentAreas[face.Index] = val.Value.RotatedCopy().ConvertToCuboidi();
            }
        }
        else
        {
            attachmentAreas[4] = properties["attachmentArea"].AsObject<Cuboidi>();
        }

        exceptions = properties["exceptions"].AsObject(System.Array.Empty<AssetLocation>(), block.Code.Domain);
        fallSideways = properties["fallSideways"].AsBool(false);
        dustIntensity = properties["dustIntensity"].AsFloat(0);

        fallSidewaysChance = properties["fallSidewaysChance"].AsFloat(0.3f);
        string sound = properties["fallSound"].AsString(null);
        if (sound != null)
        {
            fallSound = AssetLocation.Create(sound, block.Code.Domain);
        }

        impactDamageMul = properties["impactDamageMul"].AsFloat(1f);
    }

    public override bool CanPlaceBlock(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel,
        ref EnumHandling handling,
        ref string failureCode
    )
    {
        handling = EnumHandling.PassThrough;
        if (block.Attributes?["allowUnstablePlacement"].AsBool() == true)
            return true;

        Cuboidi? attachmentArea = attachmentAreas?[4];

        BlockPos pos = blockSel.Position.DownCopy();
        Block onBlock = world.BlockAccessor.GetBlock(pos);
        if (
            blockSel != null
            && !IsAttached(world.BlockAccessor, blockSel.Position)
            && !onBlock.CanAttachBlockAt(world.BlockAccessor, block, pos, BlockFacing.UP, attachmentArea)
            && !onBlock.WildCardMatch(exceptions)
        )
        {
            handling = EnumHandling.PreventSubsequent;
            failureCode = "requiresolidground";
            return false;
        }

        return true;
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
    {
        TryFalling(world, blockPos, ref handling);
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
    {
        base.OnNeighbourBlockChange(world, pos, neibpos, ref handling);

        if (world.Side == EnumAppSide.Client)
            return;

        EnumHandling bla = EnumHandling.PassThrough;
        TryFalling(world, pos, ref bla);
    }

    private void TryFalling(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
    {
        if (world.Api is not ICoreServerAPI sapi)
            return;
        if (!fallSideways && IsAttached(world.BlockAccessor, pos))
            return;
        if (!sapi.World.Config.GetBool("allowFallingBlocks"))
            return;

        if (
            IsReplacableBeneath(world, pos)
            || (fallSideways && world.Rand.NextDouble() < fallSidewaysChance && IsReplacableBeneathAndSideways(world, pos))
        )
        {
            BlockPos ourPos = pos.Copy();
            // Must run a frame later. This method is called from OnBlockPlaced, but at this point - if this is a freshly settled falling block, then the BE does not have its full data yet (because EntityBlockFalling makes a SetBlock, then only calls FromTreeAttributes on the BE
            sapi.Event.EnqueueMainThreadTask(
                () =>
                {
                    var block = world.BlockAccessor.GetBlock(ourPos);
                    if (this.block != block)
                        return; // Block was already removed

                    // Prevents duplication
                    Entity entity = world.GetNearestEntity(
                        ourPos.ToVec3d().Add(0.5, 0.5, 0.5),
                        1,
                        1.5f,
                        (e) =>
                        {
                            return e is EntityBlockFalling ebf && ebf.initialPos.Equals(ourPos);
                        }
                    );
                    if (entity != null)
                        return;

                    var be = world.BlockAccessor.GetBlockEntity(ourPos);
                    EntityBlockFalling entityBf = new(fallingBlock ?? block, be, ourPos, fallSound, impactDamageMul, true, dustIntensity);

                    world.SpawnEntity(entityBf);
                },
                "falling"
            );

            handling = EnumHandling.PreventSubsequent;
            return;
        }

        handling = EnumHandling.PassThrough;
    }

    public bool IsAttached(IBlockAccessor blockAccessor, BlockPos pos)
    {
        BlockPos tmpPos;

        if (attachableFaces == null)
        {
            tmpPos = pos.DownCopy();
            Block block = blockAccessor.GetBlock(tmpPos);
            return block.CanAttachBlockAt(blockAccessor, this.block, tmpPos, BlockFacing.UP, attachmentAreas?[5]);
        }

        tmpPos = new();
        for (int i = 0; i < attachableFaces.Length; i++)
        {
            BlockFacing face = attachableFaces[i];

            tmpPos.Set(pos).Add(face);
            Block block = blockAccessor.GetBlock(tmpPos);
            if (block.CanAttachBlockAt(blockAccessor, this.block, tmpPos, face.Opposite, attachmentAreas?[face.Index]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsReplacableBeneathAndSideways(IWorldAccessor world, BlockPos pos)
    {
        for (int i = 0; i < 4; i++)
        {
            BlockFacing facing = BlockFacing.HORIZONTALS[i];

            Block nBlock = world.BlockAccessor.GetBlockOrNull(pos.X + facing.Normali.X, pos.Y + facing.Normali.Y, pos.Z + facing.Normali.Z);
            if (nBlock != null && nBlock.Replaceable >= 6000)
            {
                nBlock = world.BlockAccessor.GetBlockOrNull(
                    pos.X + facing.Normali.X,
                    pos.Y + facing.Normali.Y - 1,
                    pos.Z + facing.Normali.Z
                );
                if (nBlock != null && nBlock.Replaceable >= 6000)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsReplacableBeneath(IWorldAccessor world, BlockPos pos)
    {
        Block bottomBlock = world.BlockAccessor.GetBlockBelow(pos);
        return bottomBlock.Replaceable > 6000;
    }
}
