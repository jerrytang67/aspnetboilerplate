using System;

namespace Abp
{
    /// <summary>
    /// Indicates that properties of this class should not be automatically wired by the IoC container.
    /// This is an ABP-specific attribute that replaces Castle Windsor's DoNotWire attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class DoNotWireAttribute : Attribute
    {
    }
}
