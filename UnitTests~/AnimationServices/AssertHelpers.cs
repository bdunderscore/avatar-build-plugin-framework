using System;
using nadena.dev.ndmf.animator;

namespace UnitTests.AnimationServices
{
    public class AssertInvalidate : IDisposable
    {
        private bool wasInvalidated;

        public AssertInvalidate(VirtualNode node)
        {
            node.RegisterCacheObserver(() => { wasInvalidated = true;});
        }
        
        public void Dispose()
        {
            if (!wasInvalidated)
            {
                throw new Exception("Expected node to be invalidated");
            }
        }
    }
}