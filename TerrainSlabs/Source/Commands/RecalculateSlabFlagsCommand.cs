using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Commands;

public static class RecalculateSlabFlagsCommand
{
    public static void Register(ICoreAPI api)
    {
        api.ChatCommands.GetOrCreate("tslab")
            .WithAlias("ts")
            .RequiresPrivilege(Privilege.buildblockseverywhere)
            .BeginSubCommand("flags")
            .WithAlias("f")
            .BeginSubCommand("refresh")
            .WithAlias("r")
            .RequiresPlayer()
            .HandleWith(OnHandle);
    }

    private static TextCommandResult OnHandle(TextCommandCallingArgs args)
    {
        SlabHelper.InitFlags(args.Caller.Entity.Api);
        return TextCommandResult.Success("Updated flag cache.");
    }
}
