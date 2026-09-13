#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SakuramachiToonSetup
{
    [MenuItem("Tools/Sakuramachi/Apply Toon to Selected Scene Instance")]
    private static void Apply()
    {
        var root = Selection.activeGameObject;
        if (!root || EditorUtility.IsPersistent(root))
        {
            Debug.LogWarning("Select a Sakuramachi instance in the scene Hierarchy first.");
            return;
        }
        if (GraphicsSettings.currentRenderPipeline == null ||
            !GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("Universal"))
        {
            Debug.LogWarning("This optional shader setup requires the Universal Render Pipeline.");
            return;
        }
        var shader = Shader.Find("Sakuramachi/Three Band Toon URP");
        if (!shader) { Debug.LogError("Import SakuramachiToon.shader first."); return; }
        const string folder = "Assets/SakuramachiToonMaterials";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "SakuramachiToonMaterials");
        var cache = new Dictionary<Material, Material>();
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Undo.RecordObject(renderer, "Apply Sakuramachi Toon");
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; ++i)
            {
                var original = materials[i];
                if (!original) continue;
                if (!cache.TryGetValue(original, out var converted))
                {
                    string name = original.name.Replace("Portable_", "").Replace(" (Instance)", "");
                    foreach (char ch in System.IO.Path.GetInvalidFileNameChars()) name = name.Replace(ch, '_');
                    string path = folder + "/" + name + ".mat";
                    converted = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (!converted)
                    {
                        converted = new Material(shader) { name = name };
                        AssetDatabase.CreateAsset(converted, path);
                    }
                    // Refresh existing generated materials as well as new ones.
                    Undo.RecordObject(converted, "Refresh Sakuramachi Toon v5");
                    Color color = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") :
                        (original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white);
                    converted.shader = shader;
                    converted.shaderKeywords = new string[0];
                    converted.renderQueue = 2000;
                    converted.SetColor("_BaseColor", color);
                    bool authored = SakuramachiPalette.Linear.TryGetValue(name, out var linear);
                    converted.SetFloat("_UseAuthoredColor", authored ? 1f : 0f);
                    if (authored) converted.SetVector("_AuthoredColor", linear);
                    bool ink = name.Contains("NPR_Ink");
                    bool tunnel = name.Contains("Tunnel_Shade");
                    converted.SetFloat("_Unlit", ink || tunnel ? 1f : 0f);
                    converted.SetFloat("_Cull", tunnel ? 0f : 2f);
                    converted.SetFloat("_ReceiveShadows", 0f);
                    converted.SetFloat("_ShadowThreshold", .23f);
                    converted.SetFloat("_LightThreshold", .62f);
                    converted.SetColor("_EmissionColor", Color.black);
                    converted.SetFloat("_EmissionStrength", 0f);
                    if (name.Contains("Signal_Red") || name.Contains("Signal_Green") || name.Contains("LampWarm"))
                    {
                        converted.SetColor("_EmissionColor", color);
                        converted.SetFloat("_EmissionStrength", name.Contains("Signal_Red") ? 1.2f : 0.1f);
                    }
                    EditorUtility.SetDirty(converted);
                    cache[original] = converted;
                }
                materials[i] = converted;
            }
            renderer.sharedMaterials = materials;
            if (renderer.name.StartsWith("NPR_Outline_", StringComparison.Ordinal))
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            EditorUtility.SetDirty(renderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        AssetDatabase.SaveAssets();
        CheckOverlaps(root);
        Debug.Log("Sakuramachi v5: materials refreshed, linear palette restored, ink shadows disabled. Realtime shadow receiving defaults to 0; enable per material after tuning light bias.");
    }

    [MenuItem("Tools/Sakuramachi/Check Selected Instance for Overlaps")]
    private static void CheckSelected() { if (Selection.activeGameObject) CheckOverlaps(Selection.activeGameObject); }

    private static void CheckOverlaps(GameObject root)
    {
        var meshes = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshes.Length; ++i)
        {
            var a = meshes[i];
            if (!a.sharedMesh || a.name.StartsWith("NPR_Outline_")) continue;
            for (int j = i + 1; j < meshes.Length; ++j)
            {
                var b = meshes[j];
                if (!b.sharedMesh || b.name.StartsWith("NPR_Outline_")) continue;
                if (a.sharedMesh.vertexCount != b.sharedMesh.vertexCount || a.sharedMesh.name != b.sharedMesh.name) continue;
                bool same = true;
                var am = a.transform.localToWorldMatrix; var bm = b.transform.localToWorldMatrix;
                for (int k = 0; k < 16; ++k) if (Mathf.Abs(am[k]-bm[k]) > .0001f) { same = false; break; }
                if (same) Debug.LogWarning("Possible overlapping duplicate: " + a.name + " / " + b.name + ". Use the full FBX OR assembled split FBXs, not both. Nothing was deleted.", b);
            }
        }
    }
}
#endif
