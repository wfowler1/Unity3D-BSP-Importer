using System;
using System.Reflection;

namespace LibBSP
{

    /// <summary>
    /// Contains static methods for retrieving custom attributes.
    /// </summary>
    public static partial class CustomAttributeExtensions
    {

        /// <summary>
        /// Retrieves a custom attribute of a specified type that is applied to a specified member.
        /// </summary>
        /// <param name="element">The member to inspect.</param>
        /// <returns>A custom attribute that matches <typeparamref name="T"/>, or <c>null</c> if no such attribute is found.</returns>
        /// <typeparam name="T">The type of attribute to search for.</typeparam>
        public static T GetCustomAttribute<T>(this MemberInfo element) where T : Attribute
        {
            return Attribute.GetCustomAttribute(element, typeof(T)) as T;
        }

    }
}
