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
    }
}