using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PlaceOnSlabs.Source.Systems;

public class ServerSettings
{
    public const int ActualVersion = 2;

    public int Version { get; set; } = 0;

    public string[] OffsetBlacklist { get; set; } =
    ["*:lognarrow*", "*:*fence*", "*:*segment*", "*:palisade*", "clutter", "wattle*", "*:utb*", "primitivesurvival:monkeybridge*"];
}

internal class ConfigSystem : ModSystem
{
    private const string fileName = "placeonslabs-server.json";

    public ServerSettings ServerSettings { get; private set; } = new();

    public override void StartServerSide(ICoreServerAPI api)
    {
        LoadConfig(api);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        ServerSettings.OffsetBlacklist = api.World.Config.GetString(Globals.WorldConfigName, string.Empty).Split('|');
    }

    public void SaveConfig(ICoreServerAPI api)
    {
        api.StoreModConfig(ServerSettings, fileName);
        api.World.Config.SetString(Globals.WorldConfigName, string.Join('|', ServerSettings.OffsetBlacklist));
    }

    private void LoadConfig(ICoreServerAPI api)
    {
        try
        {
            ServerSettings settings = api.LoadModConfig<ServerSettings>(fileName);
            if (settings is not null && settings.Version == ServerSettings.ActualVersion)
            {
                ServerSettings = settings;
            }
            ServerSettings.Version = ServerSettings.ActualVersion;
            SaveConfig(api);
        }
        catch (Exception e)
        {
            Mod.Logger.Warning("[placeonslabs] Could not load config from {0}, loading default settings instead.", fileName);
            Mod.Logger.Warning(e);
        }
    }
}
