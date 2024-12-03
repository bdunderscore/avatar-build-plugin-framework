#nullable enable

namespace nadena.dev.ndmf.platform
{
    internal static class AmbientPlatform
    {
        /// <summary>
        ///     The default platform to use in legacy calls like AvatarProcessor.ProcessAvatar
        /// </summary>
        public static INDMFPlatformProvider DefaultPlatform { get; internal set; } = new GenericPlatform();
    }
}