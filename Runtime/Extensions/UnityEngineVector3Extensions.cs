using UnityEngine;

namespace BSPImporter
{

    /// <summary>
    /// Static class with extension methods for <see cref="Vector3"/> objects.
    /// </summary>
    public static class UnityEngineVector3Extensions
    {

        /// <summary>
        /// Swaps the Y and Z coordinates of a Vector, converting between Quake's Z-Up coordinates to Unity's Y-Up.
        /// </summary>
        /// <param name="v">This <see cref="Vector3"/>.</param>
        /// <returns>The "swizzled" Vector.</returns>
        public static Vector3 SwizzleYZ(this Vector3 v)
        {
            return new Vector3(v.x, v.z, v.y);
        }

    }
}
