namespace AIDesktopPetty.Application.World
{
    public sealed class WorldSceneReleaseResult
    {
        public bool IsSuccess { get; }

        public string Error { get; }

        private WorldSceneReleaseResult(
            bool isSuccess,
            string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static WorldSceneReleaseResult Success()
        {
            return new WorldSceneReleaseResult(
                true,
                null);
        }

        public static WorldSceneReleaseResult Failure(
            string error)
        {
            return new WorldSceneReleaseResult(
                false,
                error);
        }
    }
}