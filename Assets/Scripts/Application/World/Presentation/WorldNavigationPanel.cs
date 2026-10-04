using UnityEngine;
using UnityEngine.UI;

namespace AIDesktopPetty.Application.World
{
    public sealed class WorldNavigationPanel
        : MonoBehaviour
    {
        [SerializeField]
        private WorldCoordinator worldCoordinator;

        [SerializeField]
        private GameObject root;

        [SerializeField]
        private Button returnButton;


        private void Awake()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }


        private void OnEnable()
        {
            if (worldCoordinator != null)
            {
                worldCoordinator.StateChanged +=
                    HandleWorldStateChanged;
            }

            if (returnButton != null)
            {
                returnButton.onClick.AddListener(
                    HandleReturnClicked);
            }

            Refresh();
        }


        private void OnDisable()
        {
            if (worldCoordinator != null)
            {
                worldCoordinator.StateChanged -=
                    HandleWorldStateChanged;
            }

            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(
                    HandleReturnClicked);
            }
        }


        private void HandleReturnClicked()
        {
            if (worldCoordinator == null)
                return;

            worldCoordinator.RequestExit();
        }


        private void HandleWorldStateChanged(
            WorldState previous,
            WorldState current)
        {
            Refresh();
        }


        private void Refresh()
        {
            if (root == null)
                return;

            bool shouldShow =
                worldCoordinator != null
                &&
                worldCoordinator.State
                == WorldState.Explore;

            root.SetActive(
                shouldShow);
        }
    }
}