using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Opt-in scene-instance swap. Does not edit imported FBX or original materials.
public static class CharacterStyleAMaterials
{
    private const string Original = "Assets/Art/Models/168/Materails/";
    private const string Presets = "Assets/Shaders/Improved2.0/Materials/";
    private static readonly string[] Names =
    {
        "Body/彩叶和服", "Body/彩叶基础衣", "Body/彩叶猫耳", "Body/彩叶身体",
        "Body/彩叶头发", "Body/尾巴", "Face/彩叶头", "Face/彩叶眼睛"
    };

    [MenuItem("Tools/Character Style A/Apply to Selected Character")]
    private static void Apply() { Swap(false); }

    [MenuItem("Tools/Character Style A/Restore Original on Selected Character")]
    private static void Restore() { Swap(true); }

    [MenuItem("Tools/Character Style A/Apply to Selected Character", true)]
    [MenuItem("Tools/Character Style A/Restore Original on Selected Character", true)]
    private static bool CanSwap()
    {
        return Selection.activeGameObject != null && !EditorUtility.IsPersistent(Selection.activeGameObject);
    }

    private static void Swap(bool restore)
    {
        var replacements = new Dictionary<Material, Material>();
        foreach (var name in Names)
        {
            var oldMaterial = AssetDatabase.LoadAssetAtPath<Material>(Original + name + ".mat");
            var leaf = name.Substring(name.LastIndexOf('/') + 1);
            var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(Presets + leaf + " A.mat");
            if (oldMaterial == null || newMaterial == null)
            {
                Debug.LogError("Character Style A: Missing material pair: " + name);
                return;
            }
            replacements.Add(restore ? newMaterial : oldMaterial, restore ? oldMaterial : newMaterial);
        }
        int count = 0;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(restore ? "Restore character materials" : "Apply character style A");
        foreach (var renderer in Selection.activeGameObject.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && replacements.TryGetValue(materials[i], out var replacement))
                {
                    materials[i] = replacement;
                    changed = true;
                    count++;
                }
            }
            if (!changed) continue;
            Undo.RecordObject(renderer, "Change character materials");
            renderer.sharedMaterials = materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
        }
        Undo.CollapseUndoOperations(group);
        Debug.Log("Character Style A: replaced " + count + " material slots. Save the scene to keep this change; Undo is supported.");
    }
}
