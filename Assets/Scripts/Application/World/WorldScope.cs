using System;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// 一次具体世界实例的生命周期上下文。
    /// </summary>
    public sealed class WorldScope
    {
        public long InstanceId { get; }

        public WorldDefinition Definition { get; }

        public string WorldId => Definition.WorldId;

        public WorldSceneHandle SceneHandle
        {
            get;
            private set;
        }

        public bool ExitRequested
        {
            get;
            private set;
        }
        
        public WorldRuntimeBindings RuntimeBindings
        {
            get;
            private set;
        }

        public DesktopPresentationSnapshot DesktopSnapshot
        {
            get;
            private set;
        }

        internal WorldScope(
            long instanceId,
            WorldDefinition definition)
        {
            if (instanceId <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(instanceId));

            if (definition == null)
                throw new ArgumentNullException(
                    nameof(definition));

            InstanceId = instanceId;
            Definition = definition;
        }

        internal void RequestExit()
        {
            ExitRequested = true;
        }

        internal void AttachSceneHandle(
            WorldSceneHandle handle)
        {
            if (handle == null)
                throw new ArgumentNullException(
                    nameof(handle));

            if (SceneHandle != null)
            {
                throw new InvalidOperationException(
                    $"World {this} 已经拥有 SceneHandle。");
            }

            SceneHandle = handle;
        }

        internal WorldSceneHandle DetachSceneHandle()
        {
            WorldSceneHandle handle = SceneHandle;
            SceneHandle = null;

            return handle;
        }

        public override string ToString()
        {
            return $"{WorldId}#{InstanceId}";
        }
        
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
                    $"World {this} 已经存在 DesktopSnapshot。");
            }

            DesktopSnapshot = snapshot;
        }

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
                    $"World {this} 已经存在 RuntimeBindings。");
            }

            RuntimeBindings = bindings;
        }
        
        

        internal void DetachRuntimeBindings()
        {
            RuntimeBindings = null;
        }
    }
}