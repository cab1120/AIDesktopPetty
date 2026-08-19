using UnityEngine;

namespace Platform.Windows
{
    [DefaultExecutionOrder(-10000)]
    public sealed class WindowsPlatformBootstrap
        : MonoBehaviour
    {
        public static IWindowService
            WindowService { get; private set; }


        private void Awake()
        {
            if (WindowService != null)
            {
                Destroy(gameObject);
                return;
            }


            DontDestroyOnLoad(gameObject);


            var windowService =
                new WindowsWindowService();


            WindowService =
                windowService;


            bool success =
                windowService.Initialize();


            if (!success)
            {
                Debug.LogError(
                    "[WindowsPlatformBootstrap] " +
                    "Native platform initialization failed."
                );
            }
        }
    }
}