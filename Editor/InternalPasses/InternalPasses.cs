#region

using nadena.dev.ndmf.builtin;
using nadena.dev.ndmf.runtime;

#endregion

namespace nadena.dev.ndmf
{
    [NDMFInternal]
    internal class InternalPasses : Plugin<InternalPasses>
    {
        public override string QualifiedName => "nadena.dev.ndmf.InternalPasses";
        public override string DisplayName => "NDM Framework";

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving)
                .Run(RemoveMissingScriptComponents.Instance)
                .Then.Run(RemoveEditorOnlyPass.Instance);
        }
    }
}