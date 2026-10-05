#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

using AIDesktopPetty.Art.Scenes;

using Process =
    System.Diagnostics.Process;

namespace AIDesktopPetty.Application.World
{
    public sealed class WorldM1StressRunner
        : MonoBehaviour
    {
        [SerializeField]
        private WorldCoordinator coordinator;

        [SerializeField]
        private string worldId =
            "sakuramachi";

        [SerializeField, Min(1)]
        private int cycles =
            20;

        [Tooltip(
            "每次进入 Explore 后停留多久。" +
            "M1-7 主要测生命周期，" +
            "完整 180 秒循环另行验收。")]
        [SerializeField, Min(0f)]
        private float exploreHoldSeconds =
            2f;

        [SerializeField, Min(0f)]
        private float desktopSettleSeconds =
            0.5f;

        [SerializeField, Min(1f)]
        private float transitionTimeoutSeconds =
            30f;


        private readonly List<float>
            enterDurations =
                new List<float>();

        private readonly List<float>
            exitDurations =
                new List<float>();

        private readonly List<Sample>
            samples =
                new List<Sample>();


        private bool running;


        [ContextMenu(
            "M1-7 / Run 20 Round Trips")]
        private void RunStressTest()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning(
                    "[M1-7] 请在 Play Mode 中运行。");

                return;
            }

            if (running)
            {
                Debug.LogWarning(
                    "[M1-7] Stress test 已经在运行。");

                return;
            }

            if (coordinator == null)
            {
                Debug.LogError(
                    "[M1-7] WorldCoordinator 未绑定。");

                return;
            }

            StartCoroutine(
                RunStressRoutine());
        }


        private IEnumerator RunStressRoutine()
        {
            running = true;

            enterDurations.Clear();
            exitDurations.Clear();
            samples.Clear();


            try
            {
                if (coordinator.State
                    != WorldState.Desktop)
                {
                    Debug.LogError(
                        "[M1-7] 测试必须从 Desktop 开始。");

                    yield break;
                }


                LogEnvironment();


                // =====================================
                // Baseline
                // =====================================

                yield return null;
                yield return null;

                Sample baseline =
                    CaptureSample(
                        0,
                        "DesktopBaseline");

                samples.Add(
                    baseline);

                LogSample(
                    baseline);


                // =====================================
                // 20 round trips
                // =====================================

                for (int cycle = 1;
                     cycle <= cycles;
                     cycle++)
                {
                    Debug.Log(
                        $"[M1-7] ===== " +
                        $"Cycle {cycle}/{cycles} =====");


                    // ---------------------------------
                    // Enter
                    // ---------------------------------

                    float enterStart =
                        Time.realtimeSinceStartup;


                    if (!coordinator.TryEnterWorld(
                            worldId,
                            out string enterError))
                    {
                        Debug.LogError(
                            $"[M1-7] Cycle {cycle} " +
                            $"Enter rejected: " +
                            $"{enterError}");

                        yield break;
                    }


                    bool entered =
                        false;


                    yield return WaitForState(
                        WorldState.Explore,
                        success =>
                        {
                            entered =
                                success;
                        });


                    if (!entered)
                    {
                        Debug.LogError(
                            $"[M1-7] Cycle {cycle} " +
                            "没有进入 Explore。");

                        yield break;
                    }


                    float enterDuration =
                        Time.realtimeSinceStartup
                        - enterStart;


                    enterDurations.Add(
                        enterDuration);


                    Sample exploreSample =
                        CaptureSample(
                            cycle,
                            "Explore");

                    samples.Add(
                        exploreSample);

                    LogSample(
                        exploreSample);


                    yield return
                        new WaitForSecondsRealtime(
                            exploreHoldSeconds);


                    // ---------------------------------
                    // Exit
                    // ---------------------------------

                    float exitStart =
                        Time.realtimeSinceStartup;


                    if (!coordinator.RequestExit())
                    {
                        Debug.LogError(
                            $"[M1-7] Cycle {cycle} " +
                            "Exit request 被拒绝。");

                        yield break;
                    }


                    bool exited =
                        false;


                    yield return WaitForState(
                        WorldState.Desktop,
                        success =>
                        {
                            exited =
                                success;
                        });


                    if (!exited)
                    {
                        Debug.LogError(
                            $"[M1-7] Cycle {cycle} " +
                            "没有恢复到 Desktop。");

                        yield break;
                    }


                    float exitDuration =
                        Time.realtimeSinceStartup
                        - exitStart;


                    exitDurations.Add(
                        exitDuration);


                    /*
                     * 给 Destroy / Scene unload /
                     * runtime material destruction
                     * 几帧完成善后。
                     */
                    yield return null;
                    yield return null;


                    if (desktopSettleSeconds > 0f)
                    {
                        yield return
                            new WaitForSecondsRealtime(
                                desktopSettleSeconds);
                    }


                    Sample desktopSample =
                        CaptureSample(
                            cycle,
                            "DesktopAfterExit");

                    samples.Add(
                        desktopSample);

                    LogSample(
                        desktopSample);


                    if (!ValidateDesktopAgainstBaseline(
                            baseline,
                            desktopSample,
                            out string invariantError))
                    {
                        Debug.LogError(
                            $"[M1-7] Cycle {cycle} " +
                            $"生命周期不变量失败：\n" +
                            $"{invariantError}");

                        yield break;
                    }
                }


                SaveCsv();

                LogSummary();


                Debug.Log(
                    "[M1-7] PASS: " +
                    $"{cycles} 次 World 往返完成，" +
                    "结构性生命周期检查全部通过。");
            }
            finally
            {
                running =
                    false;
            }
        }


        private IEnumerator WaitForState(
            WorldState target,
            Action<bool> completed)
        {
            float deadline =
                Time.realtimeSinceStartup
                + transitionTimeoutSeconds;


            while (coordinator.State != target)
            {
                if (Time.realtimeSinceStartup
                    >= deadline)
                {
                    completed(false);
                    yield break;
                }

                yield return null;
            }


            completed(true);
        }


        private Sample CaptureSample(
            int cycle,
            string phase)
        {
            /*
             * 先读 Memory Counter。
             *
             * 后面的 FindObjectsOfType 本身会产生
             * 少量诊断分配，因此不要让它污染
             * 本次 Memory Counter。
             */
            long managedBytes =
                GC.GetTotalMemory(false);

            long unityAllocatedBytes =
                Profiler
                    .GetTotalAllocatedMemoryLong();

            long unityReservedBytes =
                Profiler
                    .GetTotalReservedMemoryLong();

            long processPrivateBytes =
                TryGetProcessPrivateMemory();


            Camera[] cameras =
                FindObjectsOfType<Camera>(true);

            AudioListener[] listeners =
                FindObjectsOfType<AudioListener>(true);

            AudioSource[] audioSources =
                FindObjectsOfType<AudioSource>(true);

            WorldRuntimeBindings[] bindings =
                FindObjectsOfType<
                    WorldRuntimeBindings>(true);

            SakuramachiSceneLoop[] loops =
                FindObjectsOfType<
                    SakuramachiSceneLoop>(true);


            int enabledCameras =
                0;

            int enabledListeners =
                0;

            int playingAudioSources =
                0;


            for (int i = 0;
                 i < cameras.Length;
                 i++)
            {
                if (cameras[i].isActiveAndEnabled)
                {
                    enabledCameras++;
                }
            }


            for (int i = 0;
                 i < listeners.Length;
                 i++)
            {
                if (listeners[i].isActiveAndEnabled)
                {
                    enabledListeners++;
                }
            }


            for (int i = 0;
                 i < audioSources.Length;
                 i++)
            {
                if (audioSources[i].isActiveAndEnabled
                    &&
                    audioSources[i].isPlaying)
                {
                    playingAudioSources++;
                }
            }


            Scene activeScene =
                SceneManager.GetActiveScene();


            Material skybox =
                RenderSettings.skybox;


            return new Sample
            {
                cycle =
                    cycle,

                phase =
                    phase,

                state =
                    coordinator.State.ToString(),

                hasScope =
                    coordinator.CurrentScope != null,

                sceneCount =
                    SceneManager.sceneCount,

                activeScene =
                    activeScene.IsValid()
                        ? activeScene.name
                        : "<invalid>",

                worldRuntimeBindings =
                    bindings.Length,

                sakuramachiLoops =
                    loops.Length,

                enabledCameras =
                    enabledCameras,

                enabledAudioListeners =
                    enabledListeners,

                playingAudioSources =
                    playingAudioSources,

                stateSubscribers =
                    coordinator
                        .DebugStateChangedSubscriberCount,

                skyboxInstanceId =
                    skybox != null
                        ? skybox.GetInstanceID()
                        : 0,

                managedBytes =
                    managedBytes,

                unityAllocatedBytes =
                    unityAllocatedBytes,

                unityReservedBytes =
                    unityReservedBytes,

                processPrivateBytes =
                    processPrivateBytes
            };
        }


        private static bool
            ValidateDesktopAgainstBaseline(
                Sample baseline,
                Sample current,
                out string error)
        {
            StringBuilder builder =
                new StringBuilder();


            if (current.state
                != WorldState.Desktop.ToString())
            {
                builder.AppendLine(
                    $"State={current.state}");
            }


            if (current.hasScope)
            {
                builder.AppendLine(
                    "CurrentScope 仍然存在。");
            }


            if (current.sceneCount
                != baseline.sceneCount)
            {
                builder.AppendLine(
                    $"SceneCount: " +
                    $"{baseline.sceneCount} -> " +
                    $"{current.sceneCount}");
            }


            if (current.activeScene
                != baseline.activeScene)
            {
                builder.AppendLine(
                    $"ActiveScene: " +
                    $"{baseline.activeScene} -> " +
                    $"{current.activeScene}");
            }


            if (current.worldRuntimeBindings
                != baseline.worldRuntimeBindings)
            {
                builder.AppendLine(
                    $"WorldRuntimeBindings: " +
                    $"{baseline.worldRuntimeBindings} -> " +
                    $"{current.worldRuntimeBindings}");
            }


            if (current.sakuramachiLoops
                != baseline.sakuramachiLoops)
            {
                builder.AppendLine(
                    $"SakuramachiSceneLoop: " +
                    $"{baseline.sakuramachiLoops} -> " +
                    $"{current.sakuramachiLoops}");
            }


            if (current.enabledCameras
                != baseline.enabledCameras)
            {
                builder.AppendLine(
                    $"Enabled Cameras: " +
                    $"{baseline.enabledCameras} -> " +
                    $"{current.enabledCameras}");
            }


            if (current.enabledAudioListeners
                != baseline.enabledAudioListeners)
            {
                builder.AppendLine(
                    $"Enabled AudioListeners: " +
                    $"{baseline.enabledAudioListeners} -> " +
                    $"{current.enabledAudioListeners}");
            }


            if (current.stateSubscribers
                != baseline.stateSubscribers)
            {
                builder.AppendLine(
                    $"State subscribers: " +
                    $"{baseline.stateSubscribers} -> " +
                    $"{current.stateSubscribers}");
            }


            if (current.skyboxInstanceId
                != baseline.skyboxInstanceId)
            {
                builder.AppendLine(
                    $"Skybox instance: " +
                    $"{baseline.skyboxInstanceId} -> " +
                    $"{current.skyboxInstanceId}");
            }


            error =
                builder.ToString();


            return error.Length == 0;
        }


        private void LogEnvironment()
        {
            string quality =
                QualitySettings.names[
                    QualitySettings.GetQualityLevel()];


            Debug.Log(
                "[M1-7 Environment]\n" +
                $"Unity={UnityEngine.Application.unityVersion}\n" +
                $"DebugBuild={Debug.isDebugBuild}\n" +
                $"OS={SystemInfo.operatingSystem}\n" +
                $"CPU={SystemInfo.processorType}\n" +
                $"RAM={SystemInfo.systemMemorySize} MB\n" +
                $"GPU={SystemInfo.graphicsDeviceName}\n" +
                $"GraphicsAPI={SystemInfo.graphicsDeviceType}\n" +
                $"VRAM={SystemInfo.graphicsMemorySize} MB\n" +
                $"Resolution={Screen.width}x{Screen.height}\n" +
                $"Quality={quality}\n" +
                $"VSync={QualitySettings.vSyncCount}\n" +
                $"TargetFPS={UnityEngine.Application.targetFrameRate}");
        }


        private static long
            TryGetProcessPrivateMemory()
        {
            try
            {
                using (Process process =
                       Process.GetCurrentProcess())
                {
                    return
                        process.PrivateMemorySize64;
                }
            }
            catch
            {
                return -1;
            }
        }


        private static void LogSample(
            Sample sample)
        {
            Debug.Log(
                $"[M1-7 Sample] " +
                $"Cycle={sample.cycle}, " +
                $"Phase={sample.phase}, " +
                $"Scenes={sample.sceneCount}, " +
                $"Active={sample.activeScene}, " +
                $"WorldBindings=" +
                $"{sample.worldRuntimeBindings}, " +
                $"Loops={sample.sakuramachiLoops}, " +
                $"Cameras={sample.enabledCameras}, " +
                $"Listeners=" +
                $"{sample.enabledAudioListeners}, " +
                $"PlayingAudio=" +
                $"{sample.playingAudioSources}, " +
                $"Subscribers=" +
                $"{sample.stateSubscribers}, " +
                $"Managed=" +
                $"{ToMb(sample.managedBytes):F1}MB, " +
                $"UnityAllocated=" +
                $"{ToMb(sample.unityAllocatedBytes):F1}MB, " +
                $"UnityReserved=" +
                $"{ToMb(sample.unityReservedBytes):F1}MB, " +
                $"ProcessPrivate=" +
                $"{ToMb(sample.processPrivateBytes):F1}MB");
        }


        private void LogSummary()
        {
            Debug.Log(
                "[M1-7 Timing]\n" +
                $"Enter P50=" +
                $"{Percentile(enterDurations, 0.50f):F3}s\n" +
                $"Enter P95=" +
                $"{Percentile(enterDurations, 0.95f):F3}s\n" +
                $"Exit P50=" +
                $"{Percentile(exitDurations, 0.50f):F3}s\n" +
                $"Exit P95=" +
                $"{Percentile(exitDurations, 0.95f):F3}s");
        }


        private void SaveCsv()
        {
            string path =
                Path.Combine(
                    UnityEngine.Application.persistentDataPath,
                    "m1-world-stress-" +
                    DateTime.Now.ToString(
                        "yyyyMMdd-HHmmss",
                        CultureInfo.InvariantCulture)
                    +
                    ".csv");


            StringBuilder builder =
                new StringBuilder();


            builder.AppendLine(
                "cycle,phase,state,hasScope," +
                "sceneCount,activeScene," +
                "worldBindings,sceneLoops," +
                "enabledCameras,enabledListeners," +
                "playingAudio,stateSubscribers," +
                "skyboxId,managedBytes," +
                "unityAllocatedBytes," +
                "unityReservedBytes," +
                "processPrivateBytes");


            foreach (Sample s in samples)
            {
                builder.AppendLine(
                    $"{s.cycle}," +
                    $"{s.phase}," +
                    $"{s.state}," +
                    $"{s.hasScope}," +
                    $"{s.sceneCount}," +
                    $"{s.activeScene}," +
                    $"{s.worldRuntimeBindings}," +
                    $"{s.sakuramachiLoops}," +
                    $"{s.enabledCameras}," +
                    $"{s.enabledAudioListeners}," +
                    $"{s.playingAudioSources}," +
                    $"{s.stateSubscribers}," +
                    $"{s.skyboxInstanceId}," +
                    $"{s.managedBytes}," +
                    $"{s.unityAllocatedBytes}," +
                    $"{s.unityReservedBytes}," +
                    $"{s.processPrivateBytes}");
            }


            File.WriteAllText(
                path,
                builder.ToString());


            Debug.Log(
                $"[M1-7] CSV saved: {path}");
        }


        private static float Percentile(
            List<float> values,
            float percentile)
        {
            if (values.Count == 0)
                return 0f;


            float[] sorted =
                values.ToArray();

            Array.Sort(
                sorted);


            int index =
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        percentile *
                        sorted.Length) - 1,
                    0,
                    sorted.Length - 1);


            return
                sorted[index];
        }


        private static double ToMb(
            long bytes)
        {
            if (bytes < 0)
                return -1;

            return
                bytes /
                1024d /
                1024d;
        }


        [Serializable]
        private struct Sample
        {
            public int cycle;

            public string phase;

            public string state;

            public bool hasScope;

            public int sceneCount;

            public string activeScene;

            public int worldRuntimeBindings;

            public int sakuramachiLoops;

            public int enabledCameras;

            public int enabledAudioListeners;

            public int playingAudioSources;

            public int stateSubscribers;

            public int skyboxInstanceId;

            public long managedBytes;

            public long unityAllocatedBytes;

            public long unityReservedBytes;

            public long processPrivateBytes;
        }
        [ContextMenu("M1-7 / Diagnostic Cleanup")]
        private void RunDiagnosticCleanup()
        {
            StartCoroutine(
                DiagnosticCleanupRoutine());
        }


        private IEnumerator DiagnosticCleanupRoutine()
        {
            Debug.Log(
                "[M1-7] Diagnostic cleanup start.");


            yield return Resources.UnloadUnusedAssets();


            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();


            yield return null;
            yield return null;


            Sample sample =
                CaptureSample(
                    999,
                    "AfterDiagnosticCleanup");


            LogSample(
                sample);
        }
    }
    
}

#endif