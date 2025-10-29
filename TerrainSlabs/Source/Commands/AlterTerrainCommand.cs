using System.Collections.Generic;
using System.Diagnostics;
using TerrainSlabs.Source.Utils;
using TerrainSlabs.Source.Utils.WorldGen;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Commands;

public static class AlterTerrainCommand
{
    public static void Register(ICoreServerAPI api)
    {
        api.ChatCommands.GetOrCreate("tslab")
            .WithAlias("ts")
            .RequiresPrivilege(Privilege.buildblockseverywhere)
            .BeginSubCommand("smooth")
            .WithAlias("s")
            .BeginSubCommand("surface")
            .WithAlias("s")
            .RequiresPlayer()
            .WithArgs(api.ChatCommands.Parsers.Int("range"), api.ChatCommands.Parsers.OptionalBool("highlightBlocks", "true"))
            .HandleWith(SmoothSurface);

        api.ChatCommands.GetOrCreate("tslab")
            .WithAlias("ts")
            .RequiresPrivilege(Privilege.buildblockseverywhere)
            .BeginSubCommand("smooth")
            .WithAlias("s")
            .BeginSubCommand("column")
            .WithAlias("c")
            .RequiresPlayer()
            .WithArgs(api.ChatCommands.Parsers.Int("range"), api.ChatCommands.Parsers.OptionalBool("highlightBlocks", "true"))
            .HandleWith(SmoothColumn);

        api.ChatCommands.GetOrCreate("tslab")
            .WithAlias("ts")
            .RequiresPrivilege(Privilege.buildblockseverywhere)
            .BeginSubCommand("unsmooth")
            .WithAlias("us")
            .BeginSubCommand("column")
            .WithAlias("c")
            .RequiresPlayer()
            .WithArgs(api.ChatCommands.Parsers.Int("range"), api.ChatCommands.Parsers.OptionalBool("highlightBlocks", "true"))
            .HandleWith(UnSmoothColumn);

        api.ChatCommands.GetOrCreate("tslab")
            .WithAlias("ts")
            .RequiresPrivilege(Privilege.buildblockseverywhere)
            .BeginSubCommand("unsmooth")
            .WithAlias("us")
            .BeginSubCommand("surface")
            .WithAlias("s")
            .RequiresPlayer()
            .WithArgs(api.ChatCommands.Parsers.Int("range"), api.ChatCommands.Parsers.OptionalBool("highlightBlocks", "true"))
            .HandleWith(UnSmoothSurface);
    }

    public static TextCommandResult SmoothColumn(TextCommandCallingArgs args) => Handle(args, true, false);

    public static TextCommandResult SmoothSurface(TextCommandCallingArgs args) => Handle(args, false, false);

    public static TextCommandResult UnSmoothColumn(TextCommandCallingArgs args) => Handle(args, true, true);

    public static TextCommandResult UnSmoothSurface(TextCommandCallingArgs args) => Handle(args, false, true);

    private static TextCommandResult Handle(TextCommandCallingArgs args, bool columnMode, bool reverse)
    {
        Stopwatch sw = Stopwatch.StartNew();
        int range = (int)args.Parsers[0].GetValue();
        bool highlightBlocks = (bool)args.Parsers[1].GetValue();
        range = 1 + range * 2;
        var bulkAccessor = args.Caller.Entity.Api.World.GetBlockAccessorBulkMinimalUpdate(true);
        var position = args.Caller.Entity.Pos.AsBlockPos.Copy();

        ITerrainReplacer replacer = reverse
            ? new TerrainUnsmoother(args.Caller.Entity.Api, bulkAccessor)
            : new TerrainSmoother(args.Caller.Entity.Api, bulkAccessor);
        position.Z -= range / 2;
        position.X -= range / 2;

        int buffer = columnMode ? TerrainSlabsGlobals.YBufferForStructures : 0;
        List<BlockPos> changedBlockPos = new(highlightBlocks ? range * range : 0);
        int replacedCount = 0;
        for (int x = 0; x < range; x++)
        {
            for (int z = 0; z < range; z++)
            {
                if (!bulkAccessor.AreNeigbourBlocksLoaded(position))
                {
                    position.Z++;
                    continue;
                }

                position.Y = bulkAccessor.GetTerrainMapheightAt(position) + buffer + 1;

                if (replacer.TryReplace(position))
                {
                    replacedCount++;
                    if (highlightBlocks)
                    {
                        changedBlockPos.Add(position.Copy());
                    }
                }
                position.Y--;
                do
                {
                    if (replacer.TryReplace(position))
                    {
                        replacedCount++;
                        if (highlightBlocks)
                        {
                            changedBlockPos.Add(position.Copy());
                        }
                    }
                    position.Y--;
                } while (columnMode && position.Y > 10);

                position.Z++;
            }
            position.Z -= range;
            position.X++;
        }

        bulkAccessor.Commit();

        if (highlightBlocks)
        {
            args.Caller.Entity.Api.World.HighlightBlocks(args.Caller.Player, 1, changedBlockPos);
        }

        sw.Stop();
        return TextCommandResult.Success($"Replaced {replacedCount} blocks in {sw.ElapsedMilliseconds} ms");
    }
}
