#region
using System;
#endregion

namespace nadena.dev.ndmf
{
    /// <summary>
    /// This attribute declares specific type of component as root object of an avatar.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class AvatarDescriptorComponent : Attribute
    {
        public Type AvatarDescriptorComponentType { get; }

        public AvatarDescriptorComponent(Type avatarDescriptorComponentType)
        {
            AvatarDescriptorComponentType = avatarDescriptorComponentType;
        }
    }
}