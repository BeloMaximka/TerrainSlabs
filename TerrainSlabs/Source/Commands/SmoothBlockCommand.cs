using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Commands;

public static class SmoothBlockCommand
{
    public static void Register(ICoreServerAPI api)
    {
        api.ChatCommands.GetOrCreate("tslab")
            .WithAlias("ts")
            .RequiresPrivilege(Privilege.buildblockseverywhere)
            .BeginSubCommand("smooth")
            .WithAlias("s")
            .BeginSubCommand("block")
            .WithAlias("b")
            .RequiresPlayer()
            .WithArgs(api.ChatCommands.Parsers.WorldPosition("position"))
            .HandleWith(OnHandle);
    }

    private static TextCommandResult OnHandle(TextCommandCallingArgs args)
    {
        BlockPos position = ((Vec3d)args.Parsers[0].GetValue()).AsBlockPos;
        IBlockAccessor accessor = args.Caller.Entity.Api.World.BlockAccessor;
        TerrainSlabReplacer replacer = new(args.Caller.Entity.Api, accessor);
        position.Y = accessor.GetTerrainMapheightAt(position);
        replacer.TryReplaceWithSlab(position);

        return TextCommandResult.Success("Done.");
    }
}
