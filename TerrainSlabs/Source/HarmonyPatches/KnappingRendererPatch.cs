using HarmonyLib;
using System;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class KnappingRendererPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(KnappingRenderer), nameof(KnappingRenderer.OnRenderFrame))]
    public static bool OffserForSlabs(
        KnappingRenderer __instance,
        ICoreClientAPI ___api,
        MeshRef ___workItemMeshRef,
        int ___texId,
        BlockPos ___pos,
        Matrixf ___ModelMat,
        float deltaTime,
        EnumRenderStage stage
    )
    {
        if (___workItemMeshRef == null)
            return false;
        if (stage == EnumRenderStage.AfterFinalComposition)
        {
            Traverse.Create(__instance).Method("RenderRecipeOutLine").GetValue();
            return false;
        }

        if (!SlabHelper.IsSlab(___api.World.BlockAccessor.GetBlockBelow(___pos).Id))
        {
            return true;
        }

        IRenderAPI rpi = ___api.Render;
        IClientWorldAccessor worldAccess = ___api.World;
        Vec3d camPos = worldAccess.Player.Entity.CameraPos;

        rpi.GlDisableCullFace();
        IStandardShaderProgram prog = rpi.PreparedStandardShader(___pos.X, ___pos.Y, ___pos.Z);
        rpi.BindTexture2d(___texId);

        prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
        prog.ViewMatrix = rpi.CameraMatrixOriginf;
        prog.ModelMatrix = ___ModelMat.Identity().Translate(___pos.X - camPos.X, ___pos.Y - camPos.Y - 0.5f, ___pos.Z - camPos.Z).Values;

        rpi.RenderMesh(___workItemMeshRef);

        prog.ModelMatrix = rpi.CurrentModelviewMatrix;
        prog.Stop();

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(KnappingRenderer), "RenderRecipeOutLine")]
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

        ___outLineColorMul.A = 1 - GameMath.Clamp((float)Math.Sqrt(plrPos.SquareDistanceTo(___pos.X, ___pos.Y, ___pos.Z)) / 5 - 1f, 0, 1);
        ___ModelMat.Set(rpi.CameraMatrixOriginf).Translate(___pos.X - camPos.X, ___pos.Y - camPos.Y - 0.5f, ___pos.Z - camPos.Z);

        float linewidth = ___api.Settings.Float["wireframethickness"];
        rpi.LineWidth = linewidth;
        rpi.GLEnableDepthTest();
        rpi.GlToggleBlend(true);

        IShaderProgram prog = rpi.GetEngineShader(EnumShaderProgram.Wireframe);
        prog.Use();
        prog.Uniform("origin", ___origin);
        prog.Uniform("colorIn", ___outLineColorMul);
        prog.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);
        prog.UniformMatrix("modelViewMatrix", ___ModelMat.Values);
        rpi.RenderMesh(___recipeOutlineMeshRef);
        prog.Stop();

        if (linewidth != 1.6f)
            rpi.LineWidth = 1.6f;

        rpi.GLDepthMask(false);

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockEntityKnappingSurface), "spawnParticles")]
    public static bool OffsetParticlesForSlabs(BlockEntityKnappingSurface __instance, Vec3d pos)
    {
        if (SlabHelper.IsSlab(__instance.Api.World.BlockAccessor.GetBlockBelow(pos.AsBlockPos).Id))
        {
            pos.Y -= 0.5f;
        }
        return true;
    }
}
