using System;
using System.Collections;
using UnityEngine;

namespace AIDesktopPetty.Application.World
{
    public enum WorldResourceFaultMode
    {
        None,

        FailLoad,

        FailRelease
    }


    /// <summary>
    /// M1-6 专用资源故障注入器。
    ///
    /// 它不替代 LocalSceneResourceService，
    /// 而是包住真实 ResourceService。
    /// </summary>
    public sealed class FaultInjectingResourceService
        : WorldResourceServiceBehaviour
    {
        [SerializeField]
        private WorldResourceServiceBehaviour
            inner;


        [Header("Fault Injection")]

        [SerializeField]
        private WorldResourceFaultMode
            faultMode =
                WorldResourceFaultMode.None;


        [Tooltip(
            "真实 Load 完成以后，" +
            "延迟多久才把结果交给 Coordinator。" +
            "用于稳定测试 Entering 中 Exit。")]
        [SerializeField, Min(0f)]
        private float completionDelaySeconds;


        [SerializeField]
        private bool oneShot =
            true;


        private bool faultConsumed;


        public override bool CanLoadWorldScene(
            WorldDefinition definition,
            out string error)
        {
            if (inner == null)
            {
                error =
                    "Inner ResourceService 未绑定。";

                return false;
            }

            /*
             * 非常重要：
             *
             * FailLoad 不能在 Preflight 阶段失败，
             * 否则测试不到真正的异步进入失败。
             */
            return inner.CanLoadWorldScene(
                definition,
                out error);
        }


        public override IEnumerator LoadWorldScene(
            WorldDefinition definition,
            Action<WorldSceneLoadResult> completed)
        {
            if (inner == null)
            {
                completed(
                    WorldSceneLoadResult.Failure(
                        "Fault injector inner service " +
                        "is missing."));

                yield break;
            }


            if (ShouldFail(
                    WorldResourceFaultMode.FailLoad))
            {
                yield return null;

                completed(
                    WorldSceneLoadResult.Failure(
                        "[Injected] Load failure."));

                yield break;
            }


            WorldSceneLoadResult result =
                null;


            yield return inner.LoadWorldScene(
                definition,
                loadResult =>
                {
                    result =
                        loadResult;
                });


            if (completionDelaySeconds > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        completionDelaySeconds);
            }


            completed(result);
        }


        public override IEnumerator ReleaseWorldScene(
            WorldSceneHandle handle,
            Action<WorldSceneReleaseResult> completed)
        {
            if (inner == null)
            {
                completed(
                    WorldSceneReleaseResult.Failure(
                        "Fault injector inner service " +
                        "is missing."));

                yield break;
            }


            if (ShouldFail(
                    WorldResourceFaultMode.FailRelease))
            {
                yield return null;

                completed(
                    WorldSceneReleaseResult.Failure(
                        "[Injected] Release failure."));

                yield break;
            }


            yield return inner.ReleaseWorldScene(
                handle,
                completed);
        }


        private bool ShouldFail(
            WorldResourceFaultMode target)
        {
            if (faultMode != target)
            {
                return false;
            }


            if (oneShot &&
                faultConsumed)
            {
                return false;
            }


            faultConsumed =
                true;


            return true;
        }


        [ContextMenu("M1-6 / Reset Fault")]
        private void ResetFault()
        {
            faultConsumed =
                false;
        }
    }
}