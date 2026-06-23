using PlaceOnSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PlaceOnSlabs.Source.Commands;

public static class RecalculateSlabFlagsCommand
{
    public static void Register(ICoreAPI api)
    {
        api.ChatCommands.GetOrCreate("placeonslab")
            .WithAlias("pos")
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
