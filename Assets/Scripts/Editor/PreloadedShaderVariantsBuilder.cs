#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds a Shader Variant Collection for environment Shader Graphs and registers it
/// in Graphics Settings → Preloaded Shaders so URP cannot strip those variants from player builds.
/// </summary>
public static class PreloadedShaderVariantsBuilder
{
    public const string CollectionPath = "Assets/Settings/PreloadedShaderVariants.shadervariants";

    public static readonly string[] ExtraShaderPaths =
    {
        "Assets/Art/Environment/BlockKit/SG_SMEO_Master.shadergraph",
        "Assets/Art/Environment/Blocker/SG_Blocker.shadergraph",
        "Assets/Art/Environment/WindowKit/SG_GlassBlur.shadergraph",
        "Assets/Art/Shaders/Standard.shadergraph",
    };

    // Combinations this project's PC URP asset actually uses at runtime.
    // Invalid ones are skipped by ShaderVariant's constructor.
    static readonly string[][] LightingKeywordSets =
    {
        Array.Empty<string>(),
        new[] { "_FORWARD_PLUS" },
        new[] { "_CLUSTER_LIGHT_LOOP" },
        new[] { "LIGHTMAP_ON" },
        new[] { "LIGHTMAP_ON", "SHADOWS_SHADOWMASK" },
        new[] { "LIGHTMAP_ON", "DIRLIGHTMAP_COMBINED" },
        new[] { "LIGHTMAP_ON", "SHADOWS_SHADOWMASK", "LIGHTMAP_SHADOW_MIXING" },
        new[] { "PROBE_VOLUMES_L1" },
        new[] { "_LIGHT_LAYERS" },
        new[] { "_DBUFFER_MRT3" },
        new[] { "_FORWARD_PLUS", "LIGHTMAP_ON" },
        new[] { "_FORWARD_PLUS", "LIGHTMAP_ON", "SHADOWS_SHADOWMASK" },
        new[] { "_FORWARD_PLUS", "LIGHTMAP_ON", "SHADOWS_SHADOWMASK", "LIGHTMAP_SHADOW_MIXING" },
        new[] { "_FORWARD_PLUS", "LIGHTMAP_ON", "DIRLIGHTMAP_COMBINED", "SHADOWS_SHADOWMASK" },
        new[] { "_FORWARD_PLUS", "PROBE_VOLUMES_L1" },
        new[] { "_FORWARD_PLUS", "LIGHTMAP_ON", "PROBE_VOLUMES_L1" },
        new[] { "_FORWARD_PLUS", "LIGHTMAP_ON", "SHADOWS_SHADOWMASK", "PROBE_VOLUMES_L1", "_LIGHT_LAYERS" },
        new[]
        {
            "_FORWARD_PLUS", "_MAIN_LIGHT_SHADOWS_CASCADE", "_ADDITIONAL_LIGHTS",
            "_ADDITIONAL_LIGHT_SHADOWS", "_SHADOWS_SOFT", "PROBE_VOLUMES_L1",
            "_LIGHT_LAYERS", "_DBUFFER_MRT3", "_REFLECTION_PROBE_BLENDING",
            "_REFLECTION_PROBE_BOX_PROJECTION"
        },
        new[]
        {
            "_FORWARD_PLUS", "_MAIN_LIGHT_SHADOWS_CASCADE", "_ADDITIONAL_LIGHTS",
            "_ADDITIONAL_LIGHT_SHADOWS", "_SHADOWS_SOFT", "LIGHTMAP_ON",
            "SHADOWS_SHADOWMASK", "LIGHTMAP_SHADOW_MIXING", "PROBE_VOLUMES_L1",
            "_LIGHT_LAYERS", "_DBUFFER_MRT3", "_REFLECTION_PROBE_BLENDING",
            "_REFLECTION_PROBE_BOX_PROJECTION"
        },
        new[] { "_CLUSTER_LIGHT_LOOP", "LIGHTMAP_ON" },
        new[] { "_CLUSTER_LIGHT_LOOP", "LIGHTMAP_ON", "SHADOWS_SHADOWMASK" },
        new[] { "_CLUSTER_LIGHT_LOOP", "LIGHTMAP_ON", "SHADOWS_SHADOWMASK", "PROBE_VOLUMES_L1", "_LIGHT_LAYERS" },
        new[]
        {
            "_CLUSTER_LIGHT_LOOP", "_MAIN_LIGHT_SHADOWS_CASCADE", "LIGHTMAP_ON",
            "SHADOWS_SHADOWMASK", "LIGHTMAP_SHADOW_MIXING", "PROBE_VOLUMES_L1",
            "_LIGHT_LAYERS", "_DBUFFER_MRT3"
        },
    };

    // Unity 6.6: ScriptableRenderPipeline is the URP Lit/Forward+ color pass (enum value 13).
    static readonly PassType[] PassTypes =
    {
        PassType.ScriptableRenderPipeline,
        PassType.ShadowCaster,
        PassType.Meta,
        PassType.MotionVectors,
    };

    [MenuItem("Tools/Shaders/Build Preloaded Shader Variants")]
    public static void Build()
    {
        var collection = LoadOrCreateCollection();
        collection.Clear();

        var shaders = CollectShaders();
        int added = 0;
        int skipped = 0;

        foreach (var shader in shaders)
        {
            if (shader == null)
            {
                continue;
            }

            var keywordSets = CollectKeywordSetsForShader(shader);
            foreach (var passType in PassTypes)
            {
                foreach (var keywords in keywordSets)
                {
                    if (TryAddVariant(collection, shader, passType, keywords))
                    {
                        added++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
            }
        }

        EditorUtility.SetDirty(collection);
        AssignToGraphicsSettings(collection);
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"[PreloadedShaderVariants] Saved {collection.shaderCount} shaders / {collection.variantCount} variants " +
            $"({added} added, {skipped} invalid) → {CollectionPath} and Graphics Settings Preloaded Shaders.");
    }

    static ShaderVariantCollection LoadOrCreateCollection()
    {
        var collection = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(CollectionPath);
        if (collection != null)
        {
            return collection;
        }

        collection = new ShaderVariantCollection();
        AssetDatabase.CreateAsset(collection, CollectionPath);
        return collection;
    }

    static HashSet<Shader> CollectShaders()
    {
        var shaders = new HashSet<Shader>();
        foreach (var path in ExtraShaderPaths)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader != null)
            {
                shaders.Add(shader);
            }
        }

        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Art" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null && material.shader != null)
            {
                shaders.Add(material.shader);
            }
        }

        return shaders;
    }

    static List<string[]> CollectKeywordSetsForShader(Shader shader)
    {
        var sets = new List<string[]>();
        var seen = new HashSet<string>();

        void AddSet(IEnumerable<string> keywords)
        {
            var filtered = keywords
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct()
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToArray();
            var key = string.Join(" ", filtered);
            if (seen.Add(key))
            {
                sets.Add(filtered);
            }
        }

        foreach (var lighting in LightingKeywordSets)
        {
            AddSet(lighting);
        }

        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Art" }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material == null || material.shader != shader)
            {
                continue;
            }

            var materialKeywords = material.shaderKeywords ?? Array.Empty<string>();
            AddSet(materialKeywords);
            foreach (var lighting in LightingKeywordSets)
            {
                AddSet(materialKeywords.Concat(lighting));
            }
        }

        return sets;
    }

    static bool TryAddVariant(
        ShaderVariantCollection collection,
        Shader shader,
        PassType passType,
        string[] keywords)
    {
        try
        {
            var variant = new ShaderVariantCollection.ShaderVariant(shader, passType, keywords);
            return collection.Add(variant);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    static void AssignToGraphicsSettings(ShaderVariantCollection collection)
    {
        var graphicsSettings = GraphicsSettings.GetGraphicsSettings();
        var serialized = new SerializedObject(graphicsSettings);
        var preloaded = serialized.FindProperty("m_PreloadedShaders");
        if (preloaded == null)
        {
            Debug.LogError("[PreloadedShaderVariants] GraphicsSettings.m_PreloadedShaders not found.");
            return;
        }

        var alreadyAssigned = false;
        for (var i = 0; i < preloaded.arraySize; i++)
        {
            if (preloaded.GetArrayElementAtIndex(i).objectReferenceValue == collection)
            {
                alreadyAssigned = true;
                break;
            }
        }

        if (!alreadyAssigned)
        {
            var index = preloaded.arraySize;
            preloaded.arraySize++;
            preloaded.GetArrayElementAtIndex(index).objectReferenceValue = collection;
        }

        var startupAction = serialized.FindProperty("m_CollectionStartupAction");
        if (startupAction != null)
        {
            startupAction.intValue = 1;
        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(graphicsSettings);
    }
}
#endif
