using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// 一个 World Scene 对应用层暴露的最小运行时控制入口。
    ///
    /// M1-4 只负责：
    /// Camera / AudioListener / Input ownership。
    ///
    /// 列车、Timeline、VFX 等属于 M1-5。
    /// </summary>
    public sealed class WorldRuntimeBindings
        : MonoBehaviour
    {
        [Header("World Camera")]

        [SerializeField]
        private Camera worldCamera;

        [SerializeField]
        private AudioListener worldAudioListener;

        [Header("World Input")]

        [Tooltip(
            "只有真正负责世界输入的 Behaviour 才放这里。")]
        [SerializeField]
        private Behaviour[] worldInputBehaviours;

        public Camera WorldCamera =>
            worldCamera;

        public AudioListener WorldAudioListener =>
            worldAudioListener;

        private void Awake()
        {
            /*
             * 当 3DScene 作为 Additive World 加载时，
             * 它不应该自行抢走控制权。
             *
             * SampleScene 仍然是 Active Scene，
             * 所以这里先保持 World control disabled。
             */
            Scene activeScene =
                SceneManager.GetActiveScene();

            if (gameObject.scene
                != activeScene)
            {
                SetControlEnabled(false);
            }
        }

        public bool TryValidate(
            out string error)
        {
            if (worldCamera == null)
            {
                error =
                    "World Camera 未绑定。";

                return false;
            }

            if (worldAudioListener == null)
            {
                error =
                    "World AudioListener 未绑定。";

                return false;
            }

            if (worldInputBehaviours == null)
            {
                error =
                    "World Input Behaviours 数组为空引用。";

                return false;
            }

            for (int i = 0;
                 i < worldInputBehaviours.Length;
                 i++)
            {
                if (worldInputBehaviours[i]
                    == null)
                {
                    error =
                        $"World Input Behaviours " +
                        $"第 {i} 项为空。";

                    return false;
                }
            }

            error = null;
            return true;
        }

        public void SetControlEnabled(
            bool enabled)
        {
            if (worldCamera != null)
            {
                worldCamera.enabled =
                    enabled;
            }

            if (worldAudioListener != null)
            {
                worldAudioListener.enabled =
                    enabled;
            }

            /*if (worldInputBehaviours == null)
                return;*/

            for (int i = 0;
                 i < worldInputBehaviours.Length;
                 i++)
            {
                Behaviour behaviour =
                    worldInputBehaviours[i];

                if (behaviour != null)
                {
                    behaviour.enabled =
                        enabled;
                }
            }
        }
    }
}