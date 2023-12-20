﻿#region

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using nadena.dev.ndmf.model;
using UnityEngine;

#endregion

namespace nadena.dev.ndmf
{
    class TypeComparer : IComparer<Type>
    {
        public int Compare(Type x, Type y)
        {
            if (x == y) return 0;
            if (x == null) return 1;
            if (y == null) return -1;

            return StringComparer.Ordinal.Compare(x.FullName, y.FullName);
        }
    }

    internal class ConcretePass
    {
        internal IPluginInternal Plugin;
        internal string Description { get; }
        internal IPass InstantiatedPass { get; }
        internal ImmutableList<Type> DeactivatePlugins { get; }
        internal ImmutableList<Type> ActivatePlugins { get; }

        public void Execute(BuildContext context)
        {
            InstantiatedPass.Execute(context);
        }

        internal ConcretePass(IPluginInternal plugin, IPass pass, ImmutableList<Type> deactivatePlugins,
            ImmutableList<Type> activatePlugins)
        {
            Plugin = plugin;
            Description = pass.DisplayName;
            InstantiatedPass = pass;
            DeactivatePlugins = deactivatePlugins;
            ActivatePlugins = activatePlugins;
        }
    }

    internal class PluginResolver
    {
        internal ImmutableList<(BuildPhase, IList<ConcretePass>)> Passes { get; }

        public PluginResolver() : this(
            AppDomain.CurrentDomain.GetAssemblies().SelectMany(
                    assembly => assembly.GetCustomAttributes(typeof(ExportsPlugin), false))
                .Select(export => ((ExportsPlugin) export).PluginType)
                .ToImmutableSortedSet(new TypeComparer())
                .Prepend(typeof(InternalPasses))
        )
        {
        }

        public PluginResolver(IEnumerable<Type> plugins) : this(
            plugins.Select(plugin =>
                plugin.GetConstructor(new Type[0]).Invoke(new object[0]) as IPluginInternal)
        )
        {
        }

        public PluginResolver(IEnumerable<IPluginInternal> pluginTemplates)
        {
            var solverContext = new SolverContext();

            foreach (var plugin in pluginTemplates)
            {
                var pluginInfo = new PluginInfo(solverContext, plugin);
                plugin.Configure(pluginInfo);
            }

            Dictionary<string, SolverPass> passByName = new Dictionary<string, SolverPass>();
            Dictionary<BuildPhase, List<SolverPass>> passesByPhase = new Dictionary<BuildPhase, List<SolverPass>>();
            Dictionary<BuildPhase, List<(SolverPass, SolverPass, ConstraintType)>>
                constraintsByPhase = new Dictionary<BuildPhase, List<(SolverPass, SolverPass, ConstraintType)>>();

            foreach (var pass in solverContext.Passes)
            {
                if (!passesByPhase.TryGetValue(pass.Phase, out var list))
                {
                    list = new List<SolverPass>();
                    passesByPhase[pass.Phase] = list;
                }

                list.Add(pass);

                if (passByName.ContainsKey(pass.PassKey.QualifiedName))
                {
                    throw new Exception("Duplicate pass with qualified name " + pass.PassKey.QualifiedName);
                }

                passByName[pass.PassKey.QualifiedName] = pass;
            }

            foreach (var constraint in solverContext.Constraints)
            {
                if (!passByName.TryGetValue(constraint.First.QualifiedName, out var first))
                {
                    continue; // optional dependency
                }

                if (!passByName.TryGetValue(constraint.Second.QualifiedName, out var second))
                {
                    continue; // optional dependency
                }

                if (first.Phase != second.Phase)
                {
                    throw new Exception("Cannot add constraint between passes in different phases: " + constraint);
                }

                if (!constraintsByPhase.TryGetValue(first.Phase, out var list))
                {
                    list = new List<(SolverPass, SolverPass, ConstraintType)>();
                    constraintsByPhase[first.Phase] = list;
                }

                list.Add((first, second, constraint.Type));
            }

            ImmutableList<(BuildPhase, IList<ConcretePass>)> result =
                ImmutableList<(BuildPhase, IList<ConcretePass>)>.Empty;

            foreach (var phase in BuildPhase.BuiltInPhases)
            {
                var passes = passesByPhase.TryGetValue(phase, out var list) ? list : null;
                if (passes == null)
                {
                    result = result.Add((phase, ImmutableList<ConcretePass>.Empty));
                    continue;
                }

                IEnumerable<(SolverPass, SolverPass, ConstraintType)> constraints =
                    constraintsByPhase.TryGetValue(phase, out var constraintList)
                        ? constraintList
                        : new List<(SolverPass, SolverPass, ConstraintType)>();
#if NDMF_INTERNAL_DEBUG
                var dumpString = "";
                foreach (var constraint in constraints)
                {
                    dumpString += $"\"{constraint.Item1.PassKey.QualifiedName}\" -> \"{constraint.Item2.PassKey.QualifiedName}\" [label=\"{constraint.Item3}\"];\n";
                }
                Debug.Log(dumpString);
#endif
                
                var sorted = TopoSort.DoSort(passes, constraints);

                var concrete = ToConcretePasses(phase, sorted);

                result = result.Add((phase, concrete));
            }

            Passes = result;
        }

        ImmutableList<ConcretePass> ToConcretePasses(BuildPhase phase, IEnumerable<SolverPass> sorted)
        {
            HashSet<Type> activeExtensions = new HashSet<Type>();

            var concrete = new List<ConcretePass>();
            foreach (var pass in sorted)
            {
                if (pass.IsPhantom) continue;

                var toDeactivate = new List<Type>();
                var toActivate = new List<Type>();
                activeExtensions.RemoveWhere(t =>
                {
                    if (!pass.IsExtensionCompatible(t))
                    {
                        toDeactivate.Add(t);
                        return true;
                    }

                    return false;
                });

                foreach (var t in pass.RequiredExtensions.ToImmutableSortedSet(new TypeComparer()))
                {
                    if (!activeExtensions.Contains(t))
                    {
                        toActivate.Add(t);
                        activeExtensions.Add(t);
                    }
                }

                concrete.Add(new ConcretePass(pass.Plugin, pass.Pass, toDeactivate.ToImmutableList(),
                    toActivate.ToImmutableList()));
            }

            if (activeExtensions.Count > 0)
            {
                var cleanup = new AnonymousPass("nadena.dev.ndmf.internal.CleanupExtensions." + phase,
                    "Close extensions",
                    ctx => { });

                concrete.Add(new ConcretePass(InternalPasses.Instance, cleanup,
                    activeExtensions.ToImmutableList(),
                    ImmutableList<Type>.Empty
                ));
            }

            return concrete.ToImmutableList();
        }
    }
}