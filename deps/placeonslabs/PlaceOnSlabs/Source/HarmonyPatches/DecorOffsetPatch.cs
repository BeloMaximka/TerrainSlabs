using System.Diagnostics;
using HarmonyLib;
using PlaceOnSlabs.Source.Utils;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace PlaceOnSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class DecorOffsetPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(
       typeof(JsonTesselator),
       "AddDecor"
   )]
    public static bool OffsetY(TCTCache vars)
    {
        var blocks = Traverse.Create(vars.tct).Field<Block[]>("currentChunkBlocksExt").Value;
        if (blocks is null)
        {
            Debug.WriteLine("[Error] blocks in DecorOffsetPatch is null, skipping the patch");
            return true;
        }

        int indexBelow = vars.extIndex3d + TileSideEnum.MoveIndex[TileSideEnum.Down];
        vars.finalY -= SlabHelper.GetYOffsetFloat(blocks[indexBelow].BlockId);
        return true;
    }
}