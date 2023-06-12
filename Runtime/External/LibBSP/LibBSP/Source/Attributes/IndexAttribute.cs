using System;

namespace LibBSP
{
    /// <summary>
    /// Custom Attribute class to mark a member of a struct as an index into another lump. The
    /// member this Attribute is applied to can be paired with a member with a <see cref="CountAttribute"/>
    /// applied to it. The two attributes can the be used to grab a range of objects
    /// from the specified lump through the
    /// <see cref="BSP.GetReferencedObjects&lt;T&gt;(object, string)"/> method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class IndexAttribute : Attribute
    {

        public string lumpName;

        /// <summary>
        /// Constructs a new instance of an <see cref="IndexAttribute"/> object. The member this Attribute
        /// is applied to will be used as an index into the lump referenced by <paramref name="lumpName"/>.
        /// </summary>
        /// <param name="lumpName">The lump the member is an index into. Corresponds to the public properties in the <see cref="BSP"/> class.</param>
        public IndexAttribute(string lumpName)
        {
            this.lumpName = lumpName;
        }
    }
}
