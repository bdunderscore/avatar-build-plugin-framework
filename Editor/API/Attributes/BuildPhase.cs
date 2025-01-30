#nullable enable

#region

using System.Collections.Immutable;
using JetBrains.Annotations;

#endregion

namespace nadena.dev.ndmf
{
    /// <summary>
    /// Build Phases provide a coarse mechanism for grouping passes for execution. Each build phase has a recommended
    /// usage to help avoid ordering conflicts without needing explicit constraints.
    ///
    /// Currently, the following phases are defined:
    /// - Resolving
    /// - Generating
    /// - Transforming
    /// - Optimizing
    /// </summary>
    [PublicAPI]
    public sealed class BuildPhase
    {
        public string Name { get; }

        // Prevent extension for now
        internal BuildPhase(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     The FirstChance build phase runs before platform initialization, and should be used for plugins that need to
        ///     run before absolutely everything else. For example, if you want to replace the entire avatar with a different
        ///     one, this is probably the time to do it.
        /// </summary>
        public static readonly BuildPhase FirstChance = new("FirstChance");

        /// <summary>
        ///     The PlatformInit phase runs early in the build process, and is intended for platform backend initialization.
        ///     In particular, this is the phase where common avatar configuration is synced between platforms.
        /// </summary>
        public static readonly BuildPhase PlatformInit = new("PlatformInit");

        /// <summary>
        /// The resolving phase is intended for use by passes which perform very early processing of components and
        /// avatar state, before any large-scale changes have been made. For example, Modular Avatar uses this phase
        /// to resolve string-serialized object passes to their destinations, and to clone animation controllers before
        /// any changes are made to them.
        ///
        /// NDMF also has a built-in phase in Resolving, which removes EditorOnly objects. For more information,
        /// see nadena.dev.ndmf.builtin.RemoveEditorOnlyPass.
        /// </summary>
        /// <see cref="nadena.dev.ndmf.builtin.RemoveEditorOnlyPass"/>
        public static readonly BuildPhase Resolving = new BuildPhase("Resolving");

        /// <summary>
        /// The generating phase is intended for use by asses which generate components used by later plugins. For
        /// example, if you want to generate components that will be used by Modular Avatar, this would be the place
        /// to do it.
        /// </summary>
        public static readonly BuildPhase Generating = new BuildPhase("Generating");

        /// <summary>
        /// The transforming phase is intended for general-purpose avatar transformations. Most of Modular Avatar's
        /// logic runs here.
        /// </summary>
        public static readonly BuildPhase Transforming = new BuildPhase("Transforming");

        /// <summary>
        /// The optimizing phase is intended for pure optimizations that need to run late in the build process.
        /// </summary>
        public static readonly BuildPhase Optimizing = new BuildPhase("Optimizing");

        /// <summary>
        ///     The platform finish phase is run after optimizations, and is intended for platform-specific cleanup.
        ///     For example, validating that we haven't exceeded the VRChat parameter limit.
        /// </summary>
        public static readonly BuildPhase PlatformFinish = new("PlatformFinish");
        
        /// <summary>
        /// This list contains all built-in phases in the order that they will be executed.
        /// </summary>
        public static readonly ImmutableList<BuildPhase> BuiltInPhases
            = ImmutableList.Create(FirstChance, PlatformInit, Resolving, Generating, Transforming, Optimizing,
                PlatformFinish);

        public override string ToString() => Name;
    }
}