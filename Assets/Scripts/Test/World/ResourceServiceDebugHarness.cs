using UnityEngine;
using AIDesktopPetty.Application.World;

namespace AIDesktopPetty.Test.World
{
    public sealed class ResourceServiceDebugHarness
        : MonoBehaviour
    {
        [SerializeField]
        private WorldCatalog worldCatalog;

        [SerializeField]
        private WorldDefinition worldDefinition;
        
        [SerializeField]
        private WorldResourceServiceBehaviour resourceService;

        [ContextMenu("M1-2 / Validate World Catalog")]
        private void ValidateCatalog()
        {
            if (worldCatalog == null)
            {
                Debug.LogError(
                    "[M1-2] WorldCatalog 未绑定。");

                return;
            }

            if (!worldCatalog.TryValidate(
                    out string error))
            {
                Debug.LogError(
                    $"[M1-2] Catalog invalid: {error}");

                return;
            }

            Debug.Log(
                "[M1-2] WorldCatalog valid.");
        }

        [ContextMenu("M1-2 / Validate Local Resource")]
        private void ValidateLocalResource()
        {
            if (worldDefinition == null)
            {
                Debug.LogError(
                    "[M1-2] WorldDefinition 未绑定。");

                return;
            }

            if (resourceService == null)
            {
                Debug.LogError(
                    "[M1-2] ResourceService 未绑定。");

                return;
            }

            if (!resourceService.CanLoadWorldScene(
                    worldDefinition,
                    out string error))
            {
                Debug.LogError(
                    $"[M1-2] Resource invalid: {error}");

                return;
            }

            Debug.Log(
                $"[M1-2] Local resource available: " +
                $"{worldDefinition.ScenePath}");
        }
    }
}