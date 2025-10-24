using System;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace TerrainSlabs.Source;

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

public static class ReflectionHelpers
{
    public static T? GetSystem<T>(this ICoreServerAPI sapi)
        where T : ServerSystem
    {
        IServerAPI sapiInstance = sapi.Server;
        if (sapiInstance == null)
            return null;

        BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        try
        {
            object? serverMainObj =
                GetMemberValue(sapiInstance, "Server", instanceFlags) ?? GetMemberValue(sapiInstance, "server", instanceFlags);
            if (serverMainObj == null)
            {
                return null;
            }

            object? systemsObj =
                GetMemberValue(serverMainObj, "Systems", instanceFlags) ?? GetMemberValue(serverMainObj, "systems", instanceFlags);
            if (systemsObj == null)
            {
                return null;
            }

            if (systemsObj is ServerSystem[] arr)
            {
                return arr.FirstOrDefault(system => system is T) as T;
            }

            return null;
        }
        catch (Exception e)
        {
            sapi.Logger.Error(e);
            return null;
        }
    }

    /// <summary>
    /// Helper: tries to get a property or field value by name, returns null if not found.
    /// </summary>
    private static object? GetMemberValue(object obj, string name, BindingFlags flags)
    {
        if (obj == null)
            return null;

        var t = obj.GetType();

        // Property first
        var prop = t.GetProperty(name, flags);
        if (prop != null)
        {
            try
            {
                return prop.GetValue(obj);
            }
            catch
            { /* ignore access exceptions */
            }
        }

        // Field
        var field = t.GetField(name, flags);
        if (field != null)
        {
            try
            {
                return field.GetValue(obj);
            }
            catch
            { /* ignore access exceptions */
            }
        }

        return null;
    }
}
