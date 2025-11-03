using System;
using System.Linq;
using TerrainSlabs.Source.Commands;
using TerrainSlabs.Source.Network;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace TerrainSlabs.Source.Systems;

public enum TerrainSmoothMode
{
    None,
    Surface,
    Column,
}

public class ServerSettings
{
    public bool DebugMode { get; set; } = false;

    private TerrainSmoothMode smoothMode = TerrainSmoothMode.Column;
    public event Action<TerrainSmoothMode>? SmoothModeChanged;

    public TerrainSmoothMode SmoothMode
    {
        get => smoothMode;
        set => SetField(ref smoothMode, value, SmoothModeChanged);
    }

    public string[] OffsetBlacklist { get; set; } = ["lognarrow-*", "anvil-*", "forge", "firepit-*"];

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

        SlabHelper.InitBlacklist(api, ServerSettings.OffsetBlacklist);
    }

    public int UpdateBlacklist(ICoreServerAPI sapi, string wildcard, bool addMode)
    {
        int count = 0;
        foreach (Block block in sapi.World.SearchBlocks(wildcard))
        {
            if (addMode)
            {
                SlabHelper.AddToOffsetBlacklist(block.Id);
            }
            else
            {
                SlabHelper.RemoveFromOffsetBlacklist(sapi, block);
            }
            count++;
        }
        ServerSettings.OffsetBlacklist = addMode
            ? ServerSettings.OffsetBlacklist.Append(wildcard)
            : ServerSettings.OffsetBlacklist.Remove(wildcard);
        SaveConfig(sapi);

        sapi.Network.GetChannel(TerrainSlabsGlobals.OffsetBlackListNetworkChannel)
            .BroadcastPacket(new UpdateBlocklistMessage() { AddMode = addMode, Wildcard = wildcard });

        return count;
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
            if (settings is not null)
            {
                ServerSettings = settings;
            }
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
