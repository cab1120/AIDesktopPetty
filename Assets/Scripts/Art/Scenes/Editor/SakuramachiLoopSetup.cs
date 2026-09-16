#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace AIDesktopPetty.Art.Scenes.Editor
{
    public static class SakuramachiLoopSetup
    {
        public const string GeneratedFolder = "Assets/Scripts/Art/Scenes/Generated";

        [MenuItem("Tools/Sakuramachi/Set Up 180 Second Scene Loop")]
        public static void SetupActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogError("请退出播放模式后配置循环。"); return; }
            try
            {
                var loop = Setup(SceneManager.GetActiveScene());
                Selection.activeGameObject = loop.gameObject;
                EditorGUIUtility.PingObject(loop);
                Debug.Log("樱町 180 秒 Timeline 已配置。场景尚未自动保存；可撤销。选中 Sakuramachi Scene Loop 后打开 Timeline 预览。", loop);
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        public static SakuramachiSceneLoop Setup(Scene scene)
        {
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var existing = all.Select(t => t.GetComponent<SakuramachiSceneLoop>()).FirstOrDefault(c => c != null);
            if (existing != null) return existing; // Re-running never duplicates a director or overwrites audio edits.
            Transform One(string name) => all.Single(t => t.name == name && t.gameObject.activeInHierarchy);
            var train = One("Train");
            var frame = train.parent;
            if (frame == null) throw new InvalidOperationException("列车必须位于共同场景父物体下。");
            var hinges = new[] { One("GateArm_Left"), One("GateArm_Right") };
            var lenses = all.Where(t => t.gameObject.activeInHierarchy && t.name.StartsWith("Signal_") && t.name.Contains("Lens"))
                .Select(t => t.GetComponent<Renderer>()).Where(r => r != null).ToArray();
            if (lenses.Length != 6) throw new InvalidOperationException("应找到六个独立灯罩；请检查重复动态模型。");
            var rightMask = One("Tunnel_Dark_Right_DepthMask").GetComponent<Renderer>();
            var leftMask = One("Tunnel_Dark_Left_DepthMask").GetComponent<Renderer>();
            float rightX = frame.InverseTransformPoint(rightMask.bounds.center).x;
            float leftX = frame.InverseTransformPoint(leftMask.bounds.center).x;
            var trainBounds = BoundsInFrame(train, frame);
            var station = frame.InverseTransformPoint(train.position);
            float direction = Mathf.Sign(leftX - rightX);
            if (direction == 0) throw new InvalidOperationException("两个黑幕不能重合。");
            var entry = station;
            var exit = station;
            float frontOffset = (direction > 0 ? trainBounds.max.x : trainBounds.min.x) - station.x;
            float rearOffset = (direction > 0 ? trainBounds.min.x : trainBounds.max.x) - station.x;
            entry.x = rightX - frontOffset;
            exit.x = leftX - rearOffset + direction * .03f;

            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Scripts/Art/Scenes/SakuramachiTrainTunnel.shader");
            if (shader == null) throw new InvalidOperationException("缺少列车隧道裁剪 Shader。");
            foreach (var renderer in train.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.shader.name != "Sakuramachi/Three Band Toon URP")
                        throw new InvalidOperationException("列车材质必须使用现有 Sakuramachi Toon Shader，避免转换其他 Shader 时丢失外观。");

            EnsureFolder(GeneratedFolder);
            EnsureFolder(GeneratedFolder + "/Materials");
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Set up Sakuramachi scene loop");
            var host = new GameObject("Sakuramachi Scene Loop");
            Undo.RegisterCreatedObjectUndo(host, "Create scene loop");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.transform.SetParent(frame, false);
            var loop = Undo.AddComponent<SakuramachiSceneLoop>(host);
            loop.railwayFrame = frame;
            loop.train = train;
            loop.stationPosition = station;
            loop.entryPosition = entry;
            loop.exitPosition = exit;
            loop.tunnelMinX = Mathf.Min(rightX, leftX);
            loop.tunnelMaxX = Mathf.Max(rightX, leftX);
            loop.lenses = lenses;
            loop.gates = hinges.Select(h => GatePose(h, frame)).ToArray();

            foreach (var renderer in train.GetComponentsInChildren<Renderer>(true))
            {
                Undo.RecordObject(renderer, "Assign tunnel materials");
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => TunnelMaterial(m, shader)).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            foreach (var t in train.GetComponentsInChildren<Transform>(true).Concat(hinges.SelectMany(h => h.GetComponentsInChildren<Transform>(true))))
            {
                Undo.RecordObject(t.gameObject, "Keep animated objects dynamic");
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
            }
            var trainAudio = AudioChild(train, "Train Movement Audio");
            trainAudio.spatialBlend = 1;
            loop.movementAudio = trainAudio;
            var director = Undo.AddComponent<PlayableDirector>(host);
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "Sakuramachi 180 Seconds";
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            timeline.fixedDuration = SakuramachiSceneLoop.CycleSeconds;
            AssetDatabase.CreateAsset(timeline, AssetDatabase.GenerateUniqueAssetPath(GeneratedFolder + "/Sakuramachi180.playable"));
            var track = timeline.CreateTrack<SakuramachiLoopTrack>(null, "列车 · 灯色 · 栏杆（统一时钟）");
            var clip = track.CreateClip<SakuramachiLoopClip>();
            clip.start = 0;
            clip.duration = SakuramachiSceneLoop.CycleSeconds;
            clip.displayName = "0预警 / 10出洞 / 30停稳 / 45发车 / 65隐藏 / 180循环";
            director.playableAsset = timeline;
            director.SetGenericBinding(track, loop);
            director.extrapolationMode = DirectorWrapMode.Loop;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            director.playOnAwake = true;
            foreach (var name in new[] { "警示音（0–3 / 30–33秒）", "栏杆音（0–5 / 30–35秒）", "到站与发车音（30 / 45秒）" })
            {
                var audioTrack = timeline.CreateTrack<AudioTrack>(null, name);
                director.SetGenericBinding(audioTrack, AudioChild(host.transform, name));
            }
            EditorUtility.SetDirty(timeline);
            EditorUtility.SetDirty(loop);
            EditorUtility.SetDirty(director);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            return loop;
        }

        static SakuramachiSceneLoop.Gate GatePose(Transform hinge, Transform frame)
        {
            var mesh = hinge.GetComponentsInChildren<MeshFilter>().First(f => !f.name.StartsWith("NPR_Outline"));
            var center = hinge.InverseTransformPoint(mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center));
            var lowered = hinge.localEulerAngles;
            lowered.y = 0;
            var plus = new Vector3(lowered.x, 90, lowered.z);
            var minus = new Vector3(lowered.x, -90, lowered.z);
            Vector3 Center(Vector3 euler) => hinge.parent.TransformPoint(hinge.localPosition + Quaternion.Euler(euler) * Vector3.Scale(center, hinge.localScale));
            var raised = Vector3.Dot(Center(plus) - Center(minus), frame.up) > 0 ? plus : minus;
            return new SakuramachiSceneLoop.Gate { hinge = hinge, loweredEuler = lowered, raisedEuler = raised };
        }

        static AudioSource AudioChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create audio interface");
            go.transform.SetParent(parent, false);
            var source = Undo.AddComponent<AudioSource>(go);
            source.playOnAwake = false;
            return source;
        }

        static Material TunnelMaterial(Material source, Shader shader)
        {
            string sourceGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string path = GeneratedFolder + "/Materials/" + source.name + "_" + sourceGuid + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result != null) return result;
            result = new Material(source) { shader = shader, name = source.name + " (Train Tunnel)" };
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        public static Bounds BoundsInFrame(Transform root, Transform frame)
        {
            bool found = false;
            var result = new Bounds();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var b = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 point = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    point = frame.InverseTransformPoint(filter.transform.TransformPoint(point));
                    if (!found) { result = new Bounds(point, Vector3.zero); found = true; }
                    else result.Encapsulate(point);
                }
            }
            if (!found) throw new InvalidOperationException("列车没有网格。");
            return result;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
    }
}
#endif
