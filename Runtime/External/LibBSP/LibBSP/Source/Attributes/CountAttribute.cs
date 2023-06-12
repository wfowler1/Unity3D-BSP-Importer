using System;

namespace LibBSP
{
    /// <summary>
    /// Custom Attribute class to mark a member of a struct as an count for another lump. The
    /// member this Attribute is applied to should always be paired with a member with an 
    /// <see cref="IndexAttribute"/> applied to it. The two attributes can the be used to grab a 
    /// range of objects from the specified lump through the
    /// <see cref="BSP.GetReferencedObjects&lt;T&gt;(object, string)"/> method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CountAttribute : Attribute
    {

        public string lumpName;

        /// <summary>
        /// Constructs a new instance of a <see cref="CountAttribute"/> object. The member this Attribute
        /// is applied to will be used as a count of objects in the lump referenced by <paramref name="lumpName"/>.
        /// </summary>
        /// <param name="lumpName">The lump the member is an count for. Corresponds to the public properties in the <see cref="BSP"/> class.</param>
        public CountAttribute(string lumpName)
        {
            this.lumpName = lumpName;
        }
    }
}
