using System.Collections.Generic;
using UnityEngine;

namespace nadena.dev.ndmf.animator
{
    internal interface IPlatformAnimatorBindings
    {
        /// <summary>
        ///     If true, the motion asset should be maintained as-is without replacement or modification.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        bool IsSpecialMotion(Motion m);

        /// <summary>
        ///     Returns any animator controllers that are referenced by platform-specific assets (e.g. VRCAvatarDescriptor).
        ///     The bool flag indicates whether the controller is overridden (true) or left as default (false).
        /// </summary>
        /// <returns></returns>
        IEnumerable<(object, RuntimeAnimatorController, bool)> GetInnateControllers();

        /// <summary>
        ///     Updates any innate controllers to reference new animator controllers.
        /// </summary>
        /// <param name="controllers"></param>
        void CommitInnateControllers(IEnumerable<(object, RuntimeAnimatorController)> controllers);
    }
}