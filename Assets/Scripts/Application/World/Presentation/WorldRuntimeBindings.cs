using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// 一个已加载 World Scene
    /// 对 Application 暴露的最小运行时入口。
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
            "只有真正负责 World 输入的 Behaviour 才放这里。" +
            "当前没有玩家输入时 Size=0 即可。")]
        [SerializeField]
        private Behaviour[] worldInputBehaviours =
            new Behaviour[0];


        [Header("World Content")]

        [SerializeField]
        private WorldContentBindingsBehaviour
            contentBindings;


        public Camera WorldCamera =>
            worldCamera;

        public AudioListener WorldAudioListener =>
            worldAudioListener;


        private void Awake()
        {
            /*
             * Additive 加载时：
             *
             * 3DScene 刚出现不能自动抢走
             * Camera / Audio / Input 控制权。
             *
             * 只有 WorldCoordinator Commit
             * 以后才能正式启用。
             */
            if (gameObject.scene
                != SceneManager.GetActiveScene())
            {
                SetControlEnabled(false);
            }
        }


        /// <summary>
        /// 只校验。
        ///
        /// 这里不应该切 Active Scene，
        /// 不应该启动 Skybox，
        /// 不应该产生新的运行时副作用。
        /// </summary>
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
                    "World Input Behaviours 数组无效。";

                return false;
            }


            for (int i = 0;
                 i < worldInputBehaviours.Length;
                 i++)
            {
                if (worldInputBehaviours[i] == null)
                {
                    error =
                        $"World Input Behaviours " +
                        $"第 {i} 项为空。";

                    return false;
                }
            }


            if (contentBindings == null)
            {
                error =
                    "World Content Bindings 未绑定。";

                return false;
            }


            if (!contentBindings.TryValidate(
                    this,
                    out string contentError))
            {
                error =
                    $"World Content 无效：" +
                    $"{contentError}";

                return false;
            }


            error = null;

            return true;
        }


        /// <summary>
        /// World Scene 已经成为 Active Scene 后调用。
        /// </summary>
        public bool TryActivateContent(
            out string error)
        {
            if (contentBindings == null)
            {
                error =
                    "World Content Bindings 未绑定。";

                return false;
            }


            return contentBindings.TryActivate(
                this,
                out error);
        }


        public void DeactivateContent()
        {
            if (contentBindings == null)
            {
                return;
            }

            contentBindings.Deactivate();
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


            if (worldInputBehaviours == null)
            {
                return;
            }


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