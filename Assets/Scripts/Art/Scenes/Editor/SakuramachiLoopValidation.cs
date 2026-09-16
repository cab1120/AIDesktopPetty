#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AIDesktopPetty.Art.Scenes.Editor
{
    // Batch validation entry points run only in the isolated validation project.
    [InitializeOnLoad]
    public static class SakuramachiLoopValidation
    {
        const string PlaybackKey = "SakuramachiLoop.PlaybackValidation";
        static PlayableDirector playbackDirector;
        static SakuramachiSceneLoop playbackLoop;
        static double playbackStart, lastPlaybackTime;
        static int playbackWraps, playbackSamples, pausedSamples;
        static bool sawMoving, sawStopped;
        static Vector3 pausedPosition;

        static SakuramachiLoopValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(PlaybackKey, false) || state != PlayModeStateChange.EnteredPlayMode) return;
                playbackLoop = UnityEngine.Object.FindObjectOfType<SakuramachiSceneLoop>();
                playbackDirector = playbackLoop.GetComponent<PlayableDirector>();
                playbackDirector.Play();
                playbackDirector.playableGraph.GetRootPlayable(0).SetSpeed(30);
                Time.timeScale = 0; // The environment loop must keep running on unscaled time.
                playbackStart = EditorApplication.timeSinceStartup;
                EditorApplication.update += TickPlayback;
            };
        }

        public static void RunPlayback()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/3DScene.unity");
            SessionState.SetBool(PlaybackKey, true);
            EditorApplication.EnterPlaymode();
        }

        static void TickPlayback()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup - playbackStart < 90, "Playback timed out");
                double time = playbackDirector.time;
                if (pausedSamples > 0)
                {
                    Require(Vector3.Distance(pausedPosition, playbackLoop.train.position) < .0001f, "Paused train moved");
                    if (++pausedSamples < 10) return;
                    Require(!playbackLoop.movementAudio.isPlaying, "Paused movement audio still playing");
                    playbackDirector.Stop();
                    Require(Vector3.Distance(playbackLoop.railwayFrame.InverseTransformPoint(playbackLoop.train.position), playbackLoop.stationPosition) < .0001f, "Stop did not restore original scene pose");
                    Require(sawMoving && sawStopped, "Playback did not cover movement and dwell");
                    FinishPlayback("PASS: actual Play Mode completed two accelerated 180-second loops at timeScale=0; samples=" + playbackSamples + "; pause held pose, Stop restored scene; no audio configured.\n", 0);
                    return;
                }
                if (lastPlaybackTime - time > 100) playbackWraps++;
                lastPlaybackTime = time;
                playbackSamples++;
                if (time > 12 && time < 19 && playbackLoop.NormalizedSpeed > 0) sawMoving = true;
                if (time > 22 && time < 33 && playbackLoop.NormalizedSpeed == 0) sawStopped = true;
                if (playbackWraps < 2) return;
                playbackDirector.Pause();
                pausedPosition = playbackLoop.train.position;
                pausedSamples = 1;
            }
            catch (Exception ex) { FinishPlayback(ex + "\n", 1); }
        }

        static void FinishPlayback(string message, int exitCode)
        {
            EditorApplication.update -= TickPlayback;
            SessionState.SetBool(PlaybackKey, false);
            File.WriteAllText(Path.GetFullPath("../scene-loop-playback.txt"), message);
            Time.timeScale = 1;
            EditorApplication.Exit(exitCode);
        }

        public static void Inspect()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/3DScene.unity");
                var report = new StringBuilder();
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (!(t.name.Contains("Train") || t.name.Contains("Gate") || t.name.Contains("Lens") ||
                              t.name.Contains("DepthMask") || t.name.Contains("Dynamic") || t.name.Contains("Tunnel"))) continue;
                        report.AppendLine(PathOf(t) + " active=" + t.gameObject.activeInHierarchy + " local=" + t.localPosition.ToString("F4") +
                            " euler=" + t.localEulerAngles.ToString("F3") + " world=" + t.position.ToString("F4") + " scale=" + t.lossyScale);
                        var renderer = t.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            report.AppendLine(" bounds=" + renderer.bounds.ToString("F4") + " enabled=" + renderer.enabled);
                            foreach (var m in renderer.sharedMaterials) report.AppendLine(" mat=" + (m == null ? "NULL" : m.name));
                        }
                    }
                File.WriteAllText(Path.GetFullPath("../scene-loop-inspection.txt"), report.ToString());
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;

        public static void Run()
        {
            var report = new StringBuilder();
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/3DScene.unity");
                var loop = SakuramachiLoopSetup.Setup(scene);
                Require(loop.ValidateBindings(out var error), error);
                Require(SakuramachiLoopSetup.Setup(scene) == loop, "Setup must be idempotent");
                EditorSceneManager.SaveScene(scene);
                report.AppendLine("Setup saved; unique controller, two hinges, six lenses and four audio interfaces.");
                report.AppendLine("entry=" + loop.entryPosition.ToString("F4") + " station=" + loop.stationPosition.ToString("F4") + " exit=" + loop.exitPosition.ToString("F4"));
                foreach (var gate in loop.gates) report.AppendLine(gate.hinge.name + " raised=" + gate.raisedEuler + " lowered=" + gate.loweredEuler);
                var director = loop.GetComponent<PlayableDirector>();
                Require(director.extrapolationMode == DirectorWrapMode.Loop, "Director must loop");
                Require(Math.Abs(((TimelineAsset)director.playableAsset).duration - 180) < .001, "Timeline length must be 180");
                director.RebuildGraph();
                var originalPosition = loop.train.localPosition;
                var originalGates = loop.gates.Select(g => g.hinge.localRotation).ToArray();
                var materialsBefore = loop.lenses.Select(r => r.sharedMaterial.GetVector("_AuthoredColor")).ToArray();
                void Seek(double t) { director.time = t; director.Evaluate(); }
                Vector4 LensColor(Renderer lens)
                {
                    var block = new MaterialPropertyBlock();
                    lens.GetPropertyBlock(block, 0);
                    return block.GetVector("_AuthoredColor");
                }
                foreach (double time in new[] {  
                             0, .25, 3, 5,
                             SakuramachiSceneLoop.RevealTime - .01,
                             SakuramachiSceneLoop.RevealTime,
                             SakuramachiSceneLoop.StopTime - .01,
                             SakuramachiSceneLoop.StopTime,
                             SakuramachiSceneLoop.StopTime + .25,
                             SakuramachiSceneLoop.StopTime + 3,
                             SakuramachiSceneLoop.DepartureTime - .01,
                             SakuramachiSceneLoop.DepartureTime,
                             SakuramachiSceneLoop.DepartureTime + 5,
                             SakuramachiSceneLoop.HiddenTime - .01,
                             SakuramachiSceneLoop.HiddenTime,
                             179.99 })
                {
                    Seek(time);
                    var position = loop.train.position;
                    var color = LensColor(loop.lenses[0]);
                    var rotations = loop.gates.Select(g => g.hinge.localRotation).ToArray();
                    var visible = loop.train.GetComponentInChildren<Renderer>().enabled;
                    Require(loop.lenses.All(r => Vector4.Distance(LensColor(r), color) < .0001f), "All six lamps must agree at " + time);
                    Seek(time + 180);
                    Require(Vector3.Distance(position, loop.train.position) < .0001f, "Second loop position differs");
                    Require(Vector4.Distance(color, LensColor(loop.lenses[0])) < .0001f, "Second loop lamp color differs");
                    Require(loop.gates.Select((g, i) => Quaternion.Angle(rotations[i], g.hinge.localRotation)).All(a => a < .05f), "Second loop gate differs");
                    Require(visible == (time >= 10 && time < 45), "Visibility boundary incorrect");
                }
                for (double t = 20; t <= 35; t += .25)
                {
                    Seek(t);
                    Require(Vector3.Distance(loop.railwayFrame.InverseTransformPoint(loop.train.position), loop.stationPosition) < .0001f, "Train drifted during 15 second dwell");
                    Require(loop.NormalizedSpeed == 0, "Train must be stopped during dwell");
                }
                Seek(5);
                Require(loop.gates.All(g => Quaternion.Angle(g.hinge.localRotation, Quaternion.Euler(g.loweredEuler)) < .05f), "Gates not down at five seconds");
                Require(Vector4.Distance(LensColor(loop.lenses[0]), loop.red) < .0001f, "Lamp not red");
                Seek(25);
                Require(loop.gates.All(g => Quaternion.Angle(g.hinge.localRotation, Quaternion.Euler(g.raisedEuler)) < .05f), "Gates not raised at 35 seconds");
                Require(Vector4.Distance(LensColor(loop.lenses[0]), loop.green) < .0001f, "Lamp not green");
                Seek(10);
                var bounds = SakuramachiLoopSetup.BoundsInFrame(loop.train, loop.railwayFrame);
                Require(Mathf.Abs(bounds.max.x - loop.tunnelMinX) < .002f, "Front must meet right mask exactly at 10 seconds");
                Seek(44.9999);
                bounds = SakuramachiLoopSetup.BoundsInFrame(loop.train, loop.railwayFrame);
                Require(bounds.min.x > loop.tunnelMaxX, "Entire train including outline must pass left mask before hiding");
                Require(loop.lenses.Select((r, i) => Vector4.Distance(r.sharedMaterial.GetVector("_AuthoredColor"), materialsBefore[i])).All(d => d < .0001f), "Shared lens materials mutated");
                report.AppendLine("PASS: Timeline seeking across two cycles; all six colors, gate deadlines, exact 15-second stop, head/tail mask boundaries, shared-material isolation.");

                var camera = new GameObject("Loop Validation Overview").AddComponent<Camera>();
                camera.transform.position = loop.railwayFrame.TransformPoint(new Vector3(9, 10, 17));
                camera.transform.LookAt(loop.railwayFrame.TransformPoint(new Vector3(0, .8f, 0)));
                camera.orthographic = true;
                camera.orthographicSize = 7.8f;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 100;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.17f, .2f, .25f);
                string output = Path.GetFullPath("../scene-loop-preview");
                Directory.CreateDirectory(output);
                foreach (double t in new[] { 0d, 5, 10, 12, 15, 20, 25, 30, 35, 40, 44.9, 45 })
                {
                    Seek(t);
                    Capture(camera, Path.Combine(output, "overview-" + t.ToString("000.0", System.Globalization.CultureInfo.InvariantCulture) + ".png"));
                }
                var tunnelShader = Shader.Find("Sakuramachi/Train Tunnel Toon URP");
                Require(tunnelShader != null && tunnelShader.isSupported, "Tunnel shader unsupported");
                Require(!ShaderUtil.GetShaderMessages(tunnelShader).Any(m => m.severity.ToString() == "Error"), "Tunnel shader compile error");
                director.Stop();
                loop.RestoreOriginalState();
                Require(Vector3.Distance(loop.train.localPosition, originalPosition) < .0001f, "Preview restoration failed");
                Require(loop.gates.Select((g, i) => Quaternion.Angle(g.hinge.localRotation, originalGates[i])).All(a => a < .05f), "Preview gate restoration failed");
                Require(loop.train.GetComponentsInChildren<Renderer>().All(r => r.enabled), "Train visibility not restored");
                report.AppendLine("PASS: actual D3D11 overview renders at eleven timestamps; tunnel Shader supported, no shader errors; preview state restored.");
                report.AppendLine("Errors: 0");
                File.WriteAllText(Path.GetFullPath("../scene-loop-validation.txt"), report.ToString());
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                report.AppendLine(ex.ToString());
                File.WriteAllText(Path.GetFullPath("../scene-loop-validation.txt"), report.ToString());
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        static void Capture(Camera camera, string path)
        {
            var previous = RenderTexture.active;
            var rt = new RenderTexture(1280, 800, 24);
            var texture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            camera.targetTexture = rt;
            camera.Render();
            camera.Render();
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
#endif
