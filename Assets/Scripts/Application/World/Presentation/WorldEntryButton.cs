using UnityEngine;
using UnityEngine.UI;

namespace AIDesktopPetty.Application.World
{
    public sealed class WorldEntryButton
        : MonoBehaviour
    {
        [SerializeField]
        private WorldCoordinator worldCoordinator;

        [SerializeField]
        private Button enterButton;

        [SerializeField]
        private string worldId =
            "sakuramachi";


        private void OnEnable()
        {
            if (enterButton != null)
            {
                enterButton.onClick.AddListener(
                    HandleEnterClicked);
            }

            if (worldCoordinator != null)
            {
                worldCoordinator.StateChanged +=
                    HandleWorldStateChanged;
            }

            RefreshInteractable();
        }


        private void OnDisable()
        {
            if (enterButton != null)
            {
                enterButton.onClick.RemoveListener(
                    HandleEnterClicked);
            }

            if (worldCoordinator != null)
            {
                worldCoordinator.StateChanged -=
                    HandleWorldStateChanged;
            }
        }


        private void HandleEnterClicked()
        {
            if (worldCoordinator == null)
            {
                Debug.LogError(
                    "[WorldEntryButton] " +
                    "WorldCoordinator 未绑定。");

                return;
            }

            if (!worldCoordinator.TryEnterWorld(
                    worldId,
                    out string error))
            {
                Debug.LogWarning(
                    "[WorldEntryButton] " +
                    $"进入 World 被拒绝：{error}");
            }
        }


        private void HandleWorldStateChanged(
            WorldState previous,
            WorldState current)
        {
            RefreshInteractable();
        }


        private void RefreshInteractable()
        {
            if (enterButton == null)
                return;

            enterButton.interactable =
                worldCoordinator != null
                &&
                worldCoordinator.State
                    == WorldState.Desktop;
        }
    }
}