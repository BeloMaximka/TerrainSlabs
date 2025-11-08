using System;
using TerrainSlabs.Source.Commands;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TerrainSlabs.Source.Systems;

public enum TerrainSmoothMode
{
    None,
    Surface,
    Column,
}

public class ServerSettings
{
    public const int ActualVersion = 2;

    public int Version { get; set; } = 0;

    public bool DebugMode { get; set; } = false;

    private TerrainSmoothMode smoothMode = TerrainSmoothMode.Column;
    public event Action<TerrainSmoothMode>? SmoothModeChanged;

    public TerrainSmoothMode SmoothMode
    {
        get => smoothMode;
        set => SetField(ref smoothMode, value, SmoothModeChanged);
    }

    public string[] OffsetBlacklist { get; set; } =
    ["*:lognarrow*", "*:*fence*", "*:*segment*", "*:palisade*", "clutter", "wattle*", "undertangledboughs:*"];

    private static void SetField<T>(ref T field, T value, Action<T>? onChanged)
    {
        if (Equals(field, value))
            return;
        field = value;
        onChanged?.Invoke(value);
    }
}

internal class ConfigSystem : ModSystem
{
    private const string fileName = "terrainslabs-server.json";

    public ServerSettings ServerSettings { get; private set; } = new();

    public override void StartServerSide(ICoreServerAPI api)
    {
        LoadConfig(api);
        ChangeGenerationModeCommand.Register(api);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        ServerSettings.OffsetBlacklist = api.World.Config.GetString(TerrainSlabsGlobals.WorldConfigName, string.Empty).Split('|');
    }

    public void SaveConfig(ICoreServerAPI api)
    {
        api.StoreModConfig<ServerSettings>(ServerSettings, fileName);
        api.World.Config.SetString(TerrainSlabsGlobals.WorldConfigName, string.Join('|', ServerSettings.OffsetBlacklist));
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
            Mod.Logger.Warning("Could not load config from {0}, loading default settings instead.", fileName);
            Mod.Logger.Warning(e);
        }
        TerrainSlabsGlobals.DebugMode = ServerSettings.DebugMode;
    }
}
