using System;
using TerrainSlabs.Source.Commands;
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
    public const int ActualVersion = 3;

    public int Version { get; set; } = 0;

    public bool DebugMode { get; set; } = false;

    private TerrainSmoothMode smoothMode = TerrainSmoothMode.Column;
    public event Action<TerrainSmoothMode>? SmoothModeChanged;

    public TerrainSmoothMode SmoothMode
    {
        get => smoothMode;
        set => SetField(ref smoothMode, value, SmoothModeChanged);
    }

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

    public void SaveConfig(ICoreServerAPI api)
    {
        api.StoreModConfig<ServerSettings>(ServerSettings, fileName);
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
            Mod.Logger.Warning("[terrainslabs] Could not load config from {0}, loading default settings instead.", fileName);
            Mod.Logger.Warning(e);
        }
        TerrainSlabsGlobals.DebugMode = ServerSettings.DebugMode;
    }
}
