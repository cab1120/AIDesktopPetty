namespace AIDesktopPetty.Application.World
{
    public sealed class WorldSceneLoadResult
    {
        public bool IsSuccess { get; }

        public WorldSceneHandle Handle { get; }

        public string Error { get; }

        private WorldSceneLoadResult(
            bool isSuccess,
            WorldSceneHandle handle,
            string error)
        {
            IsSuccess = isSuccess;
            Handle = handle;
            Error = error;
        }

        public static WorldSceneLoadResult Success(
            WorldSceneHandle handle)
        {
            return new WorldSceneLoadResult(
                true,
                handle,
                null);
        }

        public static WorldSceneLoadResult Failure(
            string error)
        {
            return new WorldSceneLoadResult(
                false,
                null,
                error);
        }
    }
}