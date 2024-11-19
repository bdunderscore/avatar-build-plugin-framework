using nadena.dev.ndmf.animator;
using UnityEngine;

namespace nadena.dev.ndmf.UnitTestSupport
{
    public class VirtualizedComponent : MonoBehaviour, IVirtualizeAnimatorController
    {
        public RuntimeAnimatorController AnimatorController { get; set; }
    }
}