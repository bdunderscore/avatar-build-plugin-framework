#region

#if NDMF_VRCSDK3_AVATARS
using VRC.SDK3.Avatars.Components;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using nadena.dev.ndmf.cs;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

namespace nadena.dev.ndmf.preview
{
    [PublicAPI]
    [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
    public static partial class ComputeContextQueries
    {
        /// <summary>
        /// Gets the avatar root for a game object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static GameObject GetAvatarRoot(
            this ComputeContext context, GameObject obj,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
        )
        {
            if (obj == null) return null;

            GameObject candidate = null;
            foreach (var elem in context.ObservePath(obj.transform))
            {
#if NDMF_VRCSDK3_AVATARS
                if (context.GetComponent<VRCAvatarDescriptor>(elem.gameObject) != null)
                {
                    candidate = elem.gameObject;
                    break;
                }
#else
                if (context.GetComponent<Animator>(elem.gameObject) != null)
                {
                    candidate = elem.gameObject;
                }
#endif
            }

            return candidate;
        }
        
        /// <summary>
        ///     Observes a value published via the given PublishedValue token. The calling computation will be re-executed
        ///     whenever the value changes (as determined by IEquatable)
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="val"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Observe<T>(
            this ComputeContext ctx, PublishedValue<T> val,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
        )
            where T : IEquatable<T>
        {
            return ctx.Observe(val, v => v, callerPath: callerPath, callerLine: callerLine);
        }

        /// <summary>
        ///     Observes a value published via the given PublishedValue token, and extracts a value from it using the given
        ///     transformation. The calling computation will be re-executed whenever the extracted value changes
        ///     (as determined by IEquatable)
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="val"></param>
        /// <param name="extract"></param>
        /// <typeparam name="T">Type of the value in the PublishedValue</typeparam>
        /// <typeparam name="R">Type of the extracted value</typeparam>
        /// <returns>The extracted value</returns>
        public static R Observe<T, R>(
            this ComputeContext ctx, PublishedValue<T> val, Func<T, R> extract,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
        )
            where R : IEquatable<R>
        {
            return ctx.Observe(val, extract, (a, b) => a is null ? b is null : a.Equals(b), callerPath: callerPath, callerLine: callerLine);
        }

        /// <summary>
        ///     Observes a value published via the given PublishedValue token, and extracts a value from it using the given
        ///     transformation. The calling computation will be re-executed whenever the extracted value changes
        ///     (as determined by the given equality function)
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="val"></param>
        /// <param name="extract"></param>
        /// <param name="eq"></param>
        /// <typeparam name="T">Type of the value in the PublishedValue</typeparam>
        /// <typeparam name="R">Type of the extracted value</typeparam>
        /// <returns>The extracted value</returns>
        public static R Observe<T, R>(this ComputeContext ctx, PublishedValue<T> val, Func<T, R> extract,
            Func<R, R, bool> eq,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
            )
        {
            return val.Observe(ctx, extract, eq, callerPath: callerPath, callerLine: callerLine);
        }

        /// <summary>
        /// Monitors a given Unity object for changes, and recomputes when changes are detected.
        ///
        /// This will recompute when properties of the object change, when the object is destroyed, or (in the case of
        /// a GameObject), when the children of the GameObject changed. However, it will only respond to changes which
        /// are recorded in the Undo system; in particular, it will not respond to animation previews. This is provided
        /// to deal with cases where asset changes can't be reported in any other way.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Observe<T>(this ComputeContext ctx, T obj,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
        ) where T : Object
        {
            ObjectWatcher.Instance.MonitorObjectProps(obj, ctx, _ => 0, (_, _) => false, false,
                callerPath, callerLine
            );

            return obj;
        }

        /// <summary>
        ///     Monitors a given Unity object for changes, and recomputes when changes are detected. The `extract` function
        ///     is used to extract the specific information of interest from the object, and the `compare` function (or,
        ///     if not provided, the default equality comparer) is used to determine if the extracted information has changed.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="obj"></param>
        /// <param name="extract"></param>
        /// <param name="compare"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <returns></returns>
        public static R Observe<T, R>(this ComputeContext ctx, T obj, Func<T, R> extract,
            Func<R, R, bool> compare = null, [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
            where T : Object
        {
            return ObjectWatcher.Instance.MonitorObjectProps(obj, ctx, extract, compare, true, callerPath, callerLine);
        }

        /// <summary>
        /// Observes the full path from the scene root to the given transform. The calling computation will be
        /// re-executed if any of the objects in this path are reparented.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="obj"></param>
        /// <returns>An enumerable of transforms in the path, starting from the leaf.</returns>
        public static IEnumerable<Transform> ObservePath(this ComputeContext ctx, Transform obj)
        {
            ObjectWatcher.Instance.MonitorObjectPath(obj, ctx);

            return FollowPath(obj);

            IEnumerable<Transform> FollowPath(Transform obj)
            {
                while (obj != null)
                {
                    yield return obj;
                    obj = obj.parent;
                }
            }
        }

        /// <summary>
        /// Observes the world space position of a given transform.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="t"></param>
        public static Transform ObserveTransformPosition(this ComputeContext ctx, Transform t)
        {
            foreach (var node in ctx.ObservePath(t))
            {
                ctx.Observe(node, obj => (obj.localPosition, obj.localRotation, obj.localScale), (a, b) =>
                {
                    return Vector3.Distance(a.Item1, b.Item1) > 0.0001f ||
                           Quaternion.Angle(a.Item2, b.Item2) > 0.0001f ||
                           Vector3.Distance(a.Item3, b.Item3) > 0.0001f;
                });
            }

            return t;
        }

        /// <summary>
        /// Observes whether a given game object and all its parents are active.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool ActiveInHierarchy(this ComputeContext ctx, GameObject obj,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
        )
        {
            foreach (var node in ObservePath(ctx, obj.transform)) ctx.Observe(node, n => n.gameObject.activeSelf, callerPath: callerPath, callerLine: callerLine);
            return obj.activeInHierarchy;
        }

        /// <summary>
        /// Observes whether a component is enabled, and its heirarchy path is active.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool ActiveAndEnabled(this ComputeContext ctx, Behaviour c,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
        )
        {
            return ActiveInHierarchy(ctx, c.gameObject) && ctx.Observe(c, c2 => c2.enabled, callerPath: callerPath, callerLine: callerLine);
        }

        private static C InternalGetComponent<C>(GameObject obj) where C : class
        {
            if (obj != null && obj.TryGetComponent<C>(out var c)) return c;
            return null;
        }

        private static Component InternalGetComponent(GameObject obj, Type type)
        {
            if (obj != null && obj.TryGetComponent(type, out var c)) return c;
            return null;
        }

        public static C GetComponent<C>(this ComputeContext ctx, GameObject obj,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
        ) where C : class
        {
            if (obj == null) return null;

            ObjectWatcher.Instance.MonitorGetComponents(obj, ctx, false);

            return obj.GetComponent<C>();
        }

        public static Component GetComponent(this ComputeContext ctx, GameObject obj, Type type,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
        )
        {
            if (obj == null) return null;

            ObjectWatcher.Instance.MonitorGetComponents(obj, ctx, false);

            return obj.GetComponent(type);
        }

        public static C[] GetComponents<C>(this ComputeContext ctx, GameObject obj,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
        ) where C : class
        {
            if (obj == null) return Array.Empty<C>();

            ObjectWatcher.Instance.MonitorGetComponents(obj, ctx, false);

            return obj.GetComponents<C>();
        }

        public static Component[] GetComponents(this ComputeContext ctx, GameObject obj, Type type,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
        )
        {
            if (obj == null) return Array.Empty<Component>();

            ObjectWatcher.Instance.MonitorGetComponents(obj, ctx, false);

            return obj.GetComponents(type);
        }

        public static C[] GetComponentsInChildren<C>(this ComputeContext ctx, GameObject obj, bool includeInactive,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
        )
            where C : class
        {
            if (obj == null) return Array.Empty<C>();

            ObjectWatcher.Instance.MonitorGetComponents(obj, ctx, true);

            return obj.GetComponentsInChildren<C>(includeInactive);
        }

        public static Component[] GetComponentsInChildren(this ComputeContext ctx, GameObject obj, Type type,
            bool includeInactive,
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0
        )
        {
            if (obj == null) return Array.Empty<Component>();

            ObjectWatcher.Instance.MonitorGetComponents(obj, ctx, true);

            return obj.GetComponentsInChildren(type, includeInactive);
        }
    }
}