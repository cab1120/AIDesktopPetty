using UnityEngine;

namespace Platform.Windows
{
    [DefaultExecutionOrder(-10000)]
    public sealed class WindowsPlatformBootstrap
        : MonoBehaviour
    {
        public static IWindowService
            WindowService { get; private set; }
        
        private WindowsWindowService
            _windowsWindowService;


        private bool _initializationResult;
        
        private void Awake()
        {
            if (WindowService != null)
            {
                Destroy(gameObject);
                return;
            }


            DontDestroyOnLoad(gameObject);


            
            _windowsWindowService =
                new WindowsWindowService();


            WindowService =
                _windowsWindowService;


            _initializationResult =
                _windowsWindowService
                    .Initialize();
        }
        private void Start()
        {
            LogStartupResult();
        }


        private void LogStartupResult()
        {
            if (_initializationResult)
            {
                _windowsWindowService
                    ?.LogStartupDiagnostics(
                        _initializationResult);
            }
        }
    }
}