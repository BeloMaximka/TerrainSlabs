using HarmonyLib;
using System;
using TerrainSlabs.Source.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TerrainSlabs.Source.HarmonyPatches;

[HarmonyPatch]
public static class ForgePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ForgeContentsRenderer), nameof(ForgeContentsRenderer.OnRenderFrame))]
    public static bool OffserForSlabs(
        ICoreClientAPI ___capi,
        MeshRef ___workItemMeshRef,
        int ___textureId,
        BlockPos ___pos,
        Matrixf ___ModelMat,
        float ___fuelLevel,
        ItemStack ___stack,
        bool ___burning,
        TextureAtlasPosition ___embertexpos,
        TextureAtlasPosition ___coaltexpos,
        MeshRef ___emberQuadRef,
        MeshRef ___coalQuadRef
    )
    {
        if (___stack == null && ___fuelLevel == 0)
            return false;

        if (!SlabHelper.IsSlab(___capi.World.BlockAccessor.GetBlockBelow(___pos).Id))
        {
            return true;
        }

        IRenderAPI rpi = ___capi.Render;
        IClientWorldAccessor worldAccess = ___capi.World;
        Vec3d camPos = worldAccess.Player.Entity.CameraPos;

        rpi.GlDisableCullFace();
        IStandardShaderProgram prog = rpi.StandardShader;
        prog.Use();
        prog.RgbaAmbientIn = rpi.AmbientColor;
        prog.RgbaFogIn = rpi.FogColor;
        prog.FogMinIn = rpi.FogMin;
        prog.FogDensityIn = rpi.FogDensity;
        prog.RgbaTint = ColorUtil.WhiteArgbVec;
        prog.DontWarpVertices = 0;
        prog.AddRenderFlags = 0;
        prog.ExtraGodray = 0;
        prog.OverlayOpacity = 0;

        if (___stack != null && ___workItemMeshRef != null)
        {
            int temp = (int)___stack.Collectible.GetTemperature(___capi.World, ___stack);

            Vec4f lightrgbs = ___capi.World.BlockAccessor.GetLightRGBs(___pos.X, ___pos.Y, ___pos.Z);
            float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(temp);
            int extraGlow = GameMath.Clamp((temp - 550) / 2, 0, 255);

            prog.NormalShaded = 1;
            prog.RgbaLightIn = lightrgbs;
            prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], extraGlow / 255f);

            prog.ExtraGlow = extraGlow;
            prog.Tex2D = ___textureId;
            prog.ModelMatrix = ___ModelMat
                .Identity()
                .Translate(___pos.X - camPos.X, ___pos.Y - camPos.Y + 10 / 16f + ___fuelLevel * 0.65f - 0.5f, ___pos.Z - camPos.Z) // our change
                .Values;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

            rpi.RenderMesh(___workItemMeshRef);
        }

        if (___fuelLevel > 0)
        {
            Vec4f lightrgbs = ___capi.World.BlockAccessor.GetLightRGBs(___pos.X, ___pos.Y, ___pos.Z);

            long seed = ___capi.World.ElapsedMilliseconds + ___pos.GetHashCode();
            float flicker = (float)(Math.Sin(seed / 40.0) * 0.2f + Math.Sin(seed / 220.0) * 0.6f + Math.Sin(seed / 100.0) + 1) / 2f;

            if (___burning)
            {
                float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(1200);

                glowColor[0] *= 1f - flicker * 0.15f;
                glowColor[1] *= 1f - flicker * 0.15f;
                glowColor[2] *= 1f - flicker * 0.15f;

                prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], 1);
            }
            else
            {
                prog.RgbaGlowIn = new Vec4f(0, 0, 0, 0);
            }

            prog.NormalShaded = 0;
            prog.RgbaLightIn = lightrgbs;
            prog.TempGlowMode = 1;

            int glow = 255 - (int)(flicker * 50);

            prog.ExtraGlow = ___burning ? glow : 0;

            // The coal or embers
            rpi.BindTexture2d(___burning ? ___embertexpos.atlasTextureId : ___coaltexpos.atlasTextureId);

            prog.ModelMatrix = ___ModelMat
                .Identity()
                .Translate(___pos.X - camPos.X, ___pos.Y - camPos.Y + 10 / 16f + ___fuelLevel * 0.65f - 0.5f, ___pos.Z - camPos.Z) // our change
                .Values;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

            rpi.RenderMesh(___burning ? ___emberQuadRef : ___coalQuadRef);
        }

        prog.Stop();

        return false;
    }
}
