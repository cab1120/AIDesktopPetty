using UnityEngine;
using AIDesktopPetty.Application.World;
using UnityEngine.SceneManagement;

namespace AIDesktopPetty.Test.World {
    public sealed class WorldCoordinatorDebugHarness
        : MonoBehaviour
    {
        [SerializeField]
        private WorldCoordinator coordinator;

        [SerializeField]
        private string testWorldId = "sakuramachi";

        private void OnEnable()
        {
            if (coordinator != null)
            {
                coordinator.StateChanged += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            if (coordinator != null)
            {
                coordinator.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(
            WorldState previous,
            WorldState current)
        {
            Debug.Log(
                $"[World Test] {previous} -> {current}");
        }

        [ContextMenu("M1-1 / Enter Test World")]
        private void EnterTestWorld()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning(
                    "请在 Play Mode 中执行 M1-1 测试。");

                return;
            }

            if (coordinator == null)
            {
                Debug.LogError(
                    "WorldCoordinator 未绑定。");

                return;
            }

            if (!coordinator.TryEnterWorld(
                    testWorldId,
                    out string error))
            {
                Debug.LogWarning(
                    $"Enter rejected: {error}");
            }
        }

        [ContextMenu("M1-1 / Request Exit")]
        private void RequestExit()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning(
                    "请在 Play Mode 中执行 M1-1 测试。");

                return;
            }

            if (coordinator == null)
            {
                Debug.LogError(
                    "WorldCoordinator 未绑定。");

                return;
            }

            bool accepted =
                coordinator.RequestExit();

            Debug.Log(
                $"[World Test] Exit accepted = {accepted}");
        }

        [ContextMenu("M1-1 / Print Status")]
        private void PrintStatus()
        {
            if (coordinator == null)
            {
                Debug.LogError(
                    "WorldCoordinator 未绑定。");

                return;
            }

            string scope =
                coordinator.CurrentScope == null
                    ? "<none>"
                    : coordinator.CurrentScope.ToString();

            Debug.Log(
                $"[World Test] State={coordinator.State}, Scope={scope}");
        }
        [ContextMenu("M1-3 / Print Loaded Scenes")]
        private void PrintLoadedScenes()
        {
            Debug.Log(
                $"[M1-3] Loaded scene count = " +
                $"{SceneManager.sceneCount}");

            for (int i = 0;
                 i < SceneManager.sceneCount;
                 i++)
            {
                Scene scene =
                    SceneManager.GetSceneAt(i);

                Debug.Log(
                    $"[M1-3] Scene[{i}] " +
                    $"name={scene.name}, " +
                    $"path={scene.path}, " +
                    $"loaded={scene.isLoaded}");
            }

            Scene active =
                SceneManager.GetActiveScene();

            Debug.Log(
                $"[M1-3] Active Scene = " +
                $"{active.name}");
        }
        [ContextMenu(
            "M1-3 / Enter Then Immediately Exit")]
        private void EnterThenImmediatelyExit()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning(
                    "请在 Play Mode 中测试。");

                return;
            }

            if (coordinator == null)
            {
                Debug.LogError(
                    "WorldCoordinator 未绑定。");

                return;
            }

            if (!coordinator.TryEnterWorld(
                    testWorldId,
                    out string error))
            {
                Debug.LogError(
                    $"Enter rejected: {error}");

                return;
            }

            Debug.Log(
                "[M1-3] Enter accepted; " +
                "requesting Exit immediately.");

            coordinator.RequestExit();
        }
    }
}