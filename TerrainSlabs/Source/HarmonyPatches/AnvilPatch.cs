using HarmonyLib;
using System;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class AnvilPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AnvilWorkItemRenderer), nameof(AnvilWorkItemRenderer.OnRenderFrame))]
    public static bool OffserForSlabs(
        AnvilWorkItemRenderer __instance,
        ICoreClientAPI ___api,
        MeshRef ___workItemMeshRef,
        int ___texId,
        ItemStack ___ingot,
        SurvivalCoreSystem ___coreMod,
        Vec4f ___glowRgb,
        BlockPos ___pos,
        Matrixf ___ModelMat,
        EnumRenderStage stage
    )
    {
        if (___workItemMeshRef == null)
            return false;

        if (!SlabHelper.IsSlab(___api.World.BlockAccessor.GetBlockBelow(___pos).Id))
        {
            return true;
        }

        if (stage == EnumRenderStage.AfterFinalComposition)
        {
            if (___api.World.Player?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible is ItemHammer)
            {
                Traverse.Create(__instance).Method("RenderRecipeOutLine").GetValue();
            }
            return false;
        }

        IRenderAPI rpi = ___api.Render;
        IClientWorldAccessor worldAccess = ___api.World;
        Vec3d camPos = worldAccess.Player.Entity.CameraPos;
        int temp = (int)___ingot.Collectible.GetTemperature(___api.World, ___ingot);

        Vec4f lightrgbs = worldAccess.BlockAccessor.GetLightRGBs(___pos.X, ___pos.Y, ___pos.Z);
        int extraGlow = GameMath.Clamp((temp - 550) / 2, 0, 255);
        float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(temp);
        ___glowRgb.R = glowColor[0];
        ___glowRgb.G = glowColor[1];
        ___glowRgb.B = glowColor[2];
        ___glowRgb.A = extraGlow / 255f;

        rpi.GlDisableCullFace();

        IShaderProgram prog = ___coreMod.anvilShaderProg;
        prog.Use();
        rpi.BindTexture2d(___texId);
        prog.Uniform("rgbaAmbientIn", rpi.AmbientColor);

        prog.Uniform("rgbaFogIn", rpi.FogColor);
        prog.Uniform("fogMinIn", rpi.FogMin);
        prog.Uniform("dontWarpVertices", 0);
        prog.Uniform("addRenderFlags", 0);
        prog.Uniform("fogDensityIn", rpi.FogDensity);
        prog.Uniform("rgbaTint", ColorUtil.WhiteArgbVec);
        prog.Uniform("rgbaLightIn", lightrgbs);
        prog.Uniform("rgbaGlowIn", ___glowRgb);
        prog.Uniform("extraGlow", extraGlow);

        prog.UniformMatrix(
            "modelMatrix",
            ___ModelMat.Identity().Translate(___pos.X - camPos.X, ___pos.Y - camPos.Y - 0.5f, ___pos.Z - camPos.Z).Values // our change
        );
        prog.UniformMatrix("viewMatrix", rpi.CameraMatrixOriginf);
        prog.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);

        rpi.RenderMesh(___workItemMeshRef);

        prog.UniformMatrix("modelMatrix", rpi.CurrentModelviewMatrix);
        prog.Stop();

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AnvilWorkItemRenderer), "RenderRecipeOutLine")]
    public static bool OffsetSelectionForSlabs(
        Vec4f ___outLineColorMul,
        Vec3f ___origin,
        ICoreClientAPI ___api,
        MeshRef ___recipeOutlineMeshRef,
        BlockPos ___pos,
        Matrixf ___ModelMat
    )
    {
        if (___recipeOutlineMeshRef == null || ___api.HideGuis)
            return false;

        if (!SlabHelper.IsSlab(___api.World.BlockAccessor.GetBlockBelow(___pos).Id))
        {
            return true;
        }

        IRenderAPI rpi = ___api.Render;
        IClientWorldAccessor worldAccess = ___api.World;
        EntityPos plrPos = worldAccess.Player.Entity.Pos;
        Vec3d camPos = worldAccess.Player.Entity.CameraPos;
        ___ModelMat.Set(rpi.CameraMatrixOriginf).Translate(___pos.X - camPos.X, ___pos.Y - camPos.Y - 0.5f, ___pos.Z - camPos.Z); // our change
        ___outLineColorMul.A = 1 - GameMath.Clamp((float)Math.Sqrt(plrPos.SquareDistanceTo(___pos.X, ___pos.Y, ___pos.Z)) / 5 - 1f, 0, 1);

        float linewidth = 2 * ___api.Settings.Float["wireframethickness"];
        rpi.LineWidth = linewidth;
        rpi.GLEnableDepthTest();
        rpi.GlToggleBlend(true);

        IShaderProgram prog = rpi.GetEngineShader(EnumShaderProgram.Wireframe);
        prog.Use();
        prog.Uniform("origin", ___origin);
        prog.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
        prog.UniformMatrix("modelViewMatrix", ___ModelMat.Values);
        prog.Uniform("colorIn", ___outLineColorMul);
        rpi.RenderMesh(___recipeOutlineMeshRef);
        prog.Stop();

        if (linewidth != 1.6f)
            rpi.LineWidth = 1.6f;

        rpi.GLDepthMask(false);

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockEntityAnvil), nameof(BlockEntityAnvil.Initialize))]
    public static void OffsetAnimatableOnSlab(BlockEntityAnvil __instance, ref float ___voxYOff, ICoreAPI ___Api)
    {
        if (SlabHelper.IsSlab(___Api.World.BlockAccessor.GetBlockBelow(__instance.Pos).Id))
        {
            ___voxYOff -= 0.5f;
        }
    }
}
