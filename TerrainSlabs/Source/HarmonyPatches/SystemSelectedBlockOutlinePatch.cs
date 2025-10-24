using HarmonyLib;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class SystemSelectedBlockOutlinePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SystemSelectedBlockOutline), nameof(SystemSelectedBlockOutline.OnRenderFrame3DPost))]
    public static bool OffsetSelectionBoxes(ClientMain ___game, WireframeCube ___cubeWireFrame)
    {
        if (!ClientSettings.SelectedBlockOutline)
            return false;
        float wireframethickness = ClientSettings.Wireframethickness;
        if (!___game.ShouldRender2DOverlays || ___game.BlockSelection == null)
            return false;
        BlockPos pos = ___game.BlockSelection.Position;
        if (___game.BlockSelection.DidOffset)
            pos = pos.AddCopy(___game.BlockSelection.Face.Opposite);

        // Our code
        Block blockUnder = ___game.WorldMap.RelaxedBlockAccess.GetBlock(pos.Down());
        pos.Up();
        if (!SlabGroupHelper.IsSlab(blockUnder.BlockId))
        {
            return true;
        }
        Block solidBlock = ___game.WorldMap.RelaxedBlockAccess.GetBlock(pos);
        if (solidBlock.SideSolid.OnSide(BlockFacing.DOWN))
        {
            return true;
        }
        // End
        Block block = ___game.WorldMap.RelaxedBlockAccess.GetBlock(pos, 2);
        Cuboidf[] cuboidfArray;
        if (block.SideSolid.Any)
        {
            cuboidfArray = block.GetSelectionBoxes(___game.WorldMap.RelaxedBlockAccess, pos);
        }
        else
        {
            block = ___game.WorldMap.RelaxedBlockAccess.GetBlock(pos);
            cuboidfArray = ___game.GetBlockIntersectionBoxes(pos);
        }
        if (cuboidfArray == null || cuboidfArray.Length == 0)
            return false;
        bool flag = block.DoParticalSelection((IWorldAccessor)___game, pos);
        Vec4f selectionColor = block.GetSelectionColor((ICoreClientAPI)___game.api, pos);
        double num1 = (double)pos.X + ___game.Player.Entity.CameraPosOffset.X;
        double num2 = (double)pos.InternalY + ___game.Player.Entity.CameraPosOffset.Y - 0.5f; // our change
        double num3 = (double)pos.Z + ___game.Player.Entity.CameraPosOffset.Z;
        for (int index = 0; index < cuboidfArray.Length; ++index)
        {
            if (flag)
                index = ___game.BlockSelection.SelectionBoxIndex;
            if (cuboidfArray.Length <= index)
                break;
            Cuboidf cuboidf = cuboidfArray[index];
            // I removed check for DecorSelectionBox, let's see what happens
            ___cubeWireFrame.Render((ICoreClientAPI)___game.api, num1 + (double)cuboidf.X1, num2 + (double)cuboidf.Y1, num3 + (double)cuboidf.Z1, cuboidf.XSize, cuboidf.YSize, cuboidf.ZSize, 1.6f * wireframethickness, selectionColor);
            if (flag)
                break;
        }

        return false;
    }
}
