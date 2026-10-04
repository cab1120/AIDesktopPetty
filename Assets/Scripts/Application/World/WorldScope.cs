using System;
using UnityEngine.SceneManagement;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// 一次具体 World 实例的生命周期上下文。
    /// </summary>
    public sealed class WorldScope
    {
        public long InstanceId
        {
            get;
        }


        public WorldDefinition Definition
        {
            get;
        }


        public string WorldId =>
            Definition.WorldId;


        public bool ExitRequested
        {
            get;
            private set;
        }


        public WorldSceneHandle SceneHandle
        {
            get;
            private set;
        }


        public DesktopPresentationSnapshot
            DesktopSnapshot
        {
            get;
            private set;
        }


        public WorldRuntimeBindings
            RuntimeBindings
        {
            get;
            private set;
        }


        /*
         * 进入 World 前的 Active Scene。
         *
         * 当前一般就是 SampleScene，
         * 但 World 系统不能把这个名字写死。
         */
        public bool HasPreviousActiveScene
        {
            get;
            private set;
        }


        public Scene PreviousActiveScene
        {
            get;
            private set;
        }


        internal WorldScope(
            long instanceId,
            WorldDefinition definition)
        {
            if (instanceId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(instanceId));
            }


            if (definition == null)
            {
                throw new ArgumentNullException(
                    nameof(definition));
            }


            InstanceId =
                instanceId;

            Definition =
                definition;
        }


        internal void RequestExit()
        {
            ExitRequested =
                true;
        }


        // =====================================================
        // Scene Handle
        // =====================================================

        internal void AttachSceneHandle(
            WorldSceneHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(
                    nameof(handle));
            }


            if (SceneHandle != null)
            {
                throw new InvalidOperationException(
                    $"World {this} 已经拥有 SceneHandle。");
            }


            SceneHandle =
                handle;
        }


        internal WorldSceneHandle
            DetachSceneHandle()
        {
            WorldSceneHandle handle =
                SceneHandle;

            SceneHandle =
                null;

            return handle;
        }


        // =====================================================
        // Desktop Snapshot
        // =====================================================

        internal void AttachDesktopSnapshot(
            DesktopPresentationSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(
                    nameof(snapshot));
            }


            if (DesktopSnapshot != null)
            {
                throw new InvalidOperationException(
                    $"World {this} 已经拥有 " +
                    $"DesktopSnapshot。");
            }


            DesktopSnapshot =
                snapshot;
        }


        // =====================================================
        // Runtime Bindings
        // =====================================================

        internal void AttachRuntimeBindings(
            WorldRuntimeBindings bindings)
        {
            if (bindings == null)
            {
                throw new ArgumentNullException(
                    nameof(bindings));
            }


            if (RuntimeBindings != null)
            {
                throw new InvalidOperationException(
                    $"World {this} 已经拥有 " +
                    $"RuntimeBindings。");
            }


            RuntimeBindings =
                bindings;
        }


        internal void DetachRuntimeBindings()
        {
            RuntimeBindings =
                null;
        }


        // =====================================================
        // Active Scene Snapshot
        // =====================================================

        internal void CapturePreviousActiveScene(
            Scene scene)
        {
            if (HasPreviousActiveScene)
            {
                throw new InvalidOperationException(
                    $"World {this} 已经记录过 " +
                    $"PreviousActiveScene。");
            }


            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                throw new ArgumentException(
                    "Previous Active Scene 无效。",
                    nameof(scene));
            }


            PreviousActiveScene =
                scene;

            HasPreviousActiveScene =
                true;
        }


        public override string ToString()
        {
            return
                $"{WorldId}#{InstanceId}";
        }
    }
}