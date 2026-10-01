using System;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// 一次具体世界实例的生命周期上下文。
    ///
    /// 例如：
    /// 第一次进入樱町站台和第二次进入樱町站台，
    /// 即使 WorldId 相同，也属于两个不同的 WorldScope。
    /// </summary>
    public sealed class WorldScope
    {
        public long InstanceId { get; }

        public string WorldId { get; }

        public bool ExitRequested { get; private set; }

        internal WorldScope(long instanceId, string worldId)
        {
            if (instanceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(instanceId));

            if (string.IsNullOrWhiteSpace(worldId))
                throw new ArgumentException(
                    "WorldId cannot be empty.",
                    nameof(worldId));

            InstanceId = instanceId;
            WorldId = worldId;
        }

        internal void RequestExit()
        {
            ExitRequested = true;
        }

        public override string ToString()
        {
            return $"{WorldId}#{InstanceId}";
        }
    }
}