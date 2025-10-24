using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TerrainSlabs.Source.Utils;

public static class BlockCloneUtils
{
    // Vanilla Clone() fucking crashes with NRE (and does not clone all fields properly)
    public static Block ProperClone(this Block block)
    {
        var cloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        Block cloned = (Block)cloneMethod.Invoke(block, null)!;

        cloned.Code = block.Code.Clone();
        if (block.MiningSpeed != null)
        {
            cloned.MiningSpeed = new Dictionary<EnumBlockMaterial, float>(block.MiningSpeed);
        }
        if (block.Textures is FastSmallDictionary<string, CompositeTexture> fastTextures)
        {
            cloned.Textures = fastTextures.Clone();
        }
        else if (block.Textures is not null)
        {
            cloned.Textures = new FastSmallDictionary<string, CompositeTexture>(block.Textures.Count);
            foreach (KeyValuePair<string, CompositeTexture> var2 in block.Textures)
            {
                cloned.Textures[var2.Key] = var2.Value.Clone();
            }
        }
        if (block.TexturesInventory is FastSmallDictionary<string, CompositeTexture> fastInvTextures)
        {
            cloned.TexturesInventory = fastInvTextures.Clone();
        }
        else if (block.TexturesInventory is not null)
        {
            cloned.TexturesInventory = new Dictionary<string, CompositeTexture>();
            foreach (KeyValuePair<string, CompositeTexture> var in block.TexturesInventory)
            {
                cloned.TexturesInventory[var.Key] = var.Value.Clone();
            }
        }

        if (block.CollisionBoxes != null)
        {
            cloned.CollisionBoxes = new Cuboidf[block.CollisionBoxes.Length];
            for (int i = 0; i < block.CollisionBoxes.Length; i++)
            {
                cloned.CollisionBoxes[i] = block.CollisionBoxes[i].Clone();
            }
        }
        if (block.SelectionBoxes != null)
        {
            cloned.SelectionBoxes = new Cuboidf[block.SelectionBoxes.Length];
            for (int i = 0; i < block.SelectionBoxes.Length; i++)
            {
                cloned.SelectionBoxes[i] = block.SelectionBoxes[i].Clone();
            }
        }

        cloned.Shape = block.Shape.Clone();
        cloned.LightHsv = block.LightHsv;
        if (block.ParticleProperties != null)
        {
            cloned.ParticleProperties = new AdvancedParticleProperties[block.ParticleProperties.Length];
            for (int j = 0; j < block.ParticleProperties.Length; j++)
            {
                cloned.ParticleProperties[j] = block.ParticleProperties[j].Clone();
            }
        }
        if (block.Drops != null)
        {
            cloned.Drops = new BlockDropItemStack[block.Drops.Length];
            for (int i = 0; i < block.Drops.Length; i++)
            {
                cloned.Drops[i] = block.Drops[i].Clone();
            }
        }
        cloned.SideOpaque = block.SideOpaque;
        cloned.SideSolid = block.SideSolid;
        cloned.SideAo = block.SideAo;
        if (block.CombustibleProps != null)
        {
            cloned.CombustibleProps = block.CombustibleProps.Clone();
        }
        if (block.NutritionProps != null)
        {
            cloned.NutritionProps = block.NutritionProps.Clone();
        }
        if (block.GrindingProps != null)
        {
            cloned.GrindingProps = block.GrindingProps.Clone();
        }
        if (block.Attributes != null)
        {
            cloned.Attributes = block.Attributes.Clone();
        }
        return cloned;
    }
}
