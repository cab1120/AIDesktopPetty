using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

using AIDesktopPetty.Application.World;

namespace AIDesktopPetty.Art.Scenes
{
    [DisallowMultipleComponent]
    public sealed class SakuramachiWorldBindings
        : WorldContentBindingsBehaviour
    {
        [Header("Station Loop")]

        [SerializeField]
        private SakuramachiSceneLoop sceneLoop;

        [SerializeField]
        private PlayableDirector director;


        [Header("Environment")]

        [SerializeField]
        private SakuraWeatherController
            sakuraWeather;

        [SerializeField]
        private SkyboxRotator
            skyboxRotator;


        public SakuramachiSceneLoop SceneLoop =>
            sceneLoop;

        public PlayableDirector Director =>
            director;


        // =====================================================
        // Validate
        // =====================================================

        /// <summary>
        /// 这里只验证结构。
        ///
        /// 不启动 Skybox。
        /// 不修改 Active Scene。
        /// 不修改 Camera。
        /// </summary>
        public override bool TryValidate(
            WorldRuntimeBindings runtimeBindings,
            out string error)
        {
            if (runtimeBindings == null)
            {
                error =
                    "WorldRuntimeBindings 为空。";

                return false;
            }


            if (sceneLoop == null)
            {
                error =
                    "SakuramachiSceneLoop 未绑定。";

                return false;
            }


            if (director == null)
            {
                error =
                    "PlayableDirector 未绑定。";

                return false;
            }


            if (sakuraWeather == null)
            {
                error =
                    "SakuraWeatherController 未绑定。";

                return false;
            }


            if (skyboxRotator == null)
            {
                error =
                    "SkyboxRotator 未绑定。";

                return false;
            }


            // ---------------------------------------------
            // Scene Loop
            // ---------------------------------------------

            if (!sceneLoop.ValidateBindings(
                    out string loopError))
            {
                error =
                    $"SceneLoop 无效：" +
                    $"{loopError}";

                return false;
            }


            PlayableDirector loopDirector =
                sceneLoop.GetComponent<
                    PlayableDirector>();


            if (loopDirector != director)
            {
                error =
                    "SakuramachiSceneLoop 与绑定的 " +
                    "PlayableDirector 不属于同一个循环 Host。";

                return false;
            }


            // ---------------------------------------------
            // Timeline
            // ---------------------------------------------

            TimelineAsset timeline =
                director.playableAsset
                    as TimelineAsset;


            if (timeline == null)
            {
                error =
                    "PlayableDirector 没有有效的 " +
                    "TimelineAsset。";

                return false;
            }


            if (Math.Abs(
                    timeline.duration -
                    SakuramachiSceneLoop.CycleSeconds)
                > 0.001)
            {
                error =
                    $"Timeline 长度应为 " +
                    $"{SakuramachiSceneLoop.CycleSeconds} 秒，" +
                    $"实际为 {timeline.duration:F3}。";

                return false;
            }


            if (director.extrapolationMode
                != DirectorWrapMode.Loop)
            {
                error =
                    "PlayableDirector Wrap Mode " +
                    "必须为 Loop。";

                return false;
            }


            if (!director.playOnAwake)
            {
                error =
                    "当前樱町循环依赖 " +
                    "PlayableDirector Play On Awake。";

                return false;
            }


            SakuramachiLoopTrack loopTrack =
                null;

            int loopTrackCount =
                0;


            foreach (
                TrackAsset track
                in timeline.GetOutputTracks())
            {
                SakuramachiLoopTrack candidate =
                    track as SakuramachiLoopTrack;

                if (candidate == null)
                {
                    continue;
                }

                loopTrack =
                    candidate;

                loopTrackCount++;
            }


            if (loopTrackCount != 1)
            {
                error =
                    $"Timeline 应且仅应存在一个 " +
                    $"SakuramachiLoopTrack，" +
                    $"实际为 {loopTrackCount}。";

                return false;
            }


            UnityEngine.Object trackBinding =
                director.GetGenericBinding(
                    loopTrack);


            if (trackBinding != sceneLoop)
            {
                error =
                    "SakuramachiLoopTrack 没有绑定到 " +
                    "当前 SakuramachiSceneLoop。";

                return false;
            }


            /*
             * 这里只检查配置值。
             *
             * 不再使用：
             * skyboxRotator.isActiveAndEnabled
             *
             * 因为那是 Runtime State，
             * 不是配置合法性的证明。
             */
            if (!skyboxRotator.instantiateMaterial)
            {
                error =
                    "SkyboxRotator 的 " +
                    "Instantiate Material 必须为 true。";

                return false;
            }


            error = null;

            return true;
        }


        // =====================================================
        // Activate
        // =====================================================

        /// <summary>
        /// 此方法调用时：
        ///
        /// 3DScene 必须已经成为 Active Scene。
        /// </summary>
        public override bool TryActivate(
            WorldRuntimeBindings runtimeBindings,
            out string error)
        {
            if (runtimeBindings == null)
            {
                error =
                    "WorldRuntimeBindings 为空。";

                return false;
            }


            /*
             * SakuraWeatherController 如果没有显式 Camera，
             * Awake 会 fallback 到 Camera.main。
             *
             * Additive 情况下 Camera.main 很可能还是
             * SampleScene 的桌宠 Camera。
             *
             * 所以 World Commit 时再次明确指定。
             *
             * 该组件本身已经提供 SetTargetCamera API。
             */
            sakuraWeather.SetTargetCamera(
                runtimeBindings.WorldCamera);


            /*
             * Active Scene 已经切到 3DScene。
             *
             * 所以此时 RenderSettings.skybox
             * 才应属于 3DScene。
             */
            if (!skyboxRotator.TryActivate(
                    out string skyboxError))
            {
                error =
                    $"Skybox 激活失败：" +
                    $"{skyboxError}";

                return false;
            }


            error = null;

            return true;
        }


        // =====================================================
        // Deactivate
        // =====================================================

        public override void Deactivate()
        {
            if (skyboxRotator != null)
            {
                skyboxRotator.Deactivate();
            }
        }
    }
}