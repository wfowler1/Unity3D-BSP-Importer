#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;

namespace LibBSP
{
#if UNITY
    using Vector3 = UnityEngine.Vector3;
#elif GODOT
    using Vector3 = Godot.Vector3;
#elif NEOAXIS
    using Vector3 = NeoAxis.Vector3F;
#else
    using Vector3 = System.Numerics.Vector3;
#endif

    /// <summary>
    /// Class containing helper methods for <see cref="Vector3"/> objects.
    /// </summary>
    public static partial class Vector3Extensions
    {

#if !GODOT
        /// <summary>
        /// Vector dot product. This operation is commutative.
        /// </summary>
        /// <param name="vector">This <see cref="Vector3"/>.</param>
        /// <param name="other">The <see cref="Vector3"/> to dot with this <see cref="Vector3"/>.</param>
        /// <returns>Dot product of this <see cref="Vector3"/> and <paramref name="other"/>.</returns>
        public static float Dot(this Vector3 vector, Vector3 other)
        {
            return Vector3.Dot(vector, other);
        }

        /// <summary>
        /// Vector cross product. This operation is NOT commutative.
        /// </summary>
        /// <param name="left">This <see cref="Vector3"/>.</param>
        /// <param name="right">The <see cref="Vector3"/> to have this <see cref="Vector3"/> cross.</param>
        /// <returns>Cross product of these two vectors. Can be thought of as the normal to the plane defined by these two vectors.</returns>
        public static Vector3 Cross(this Vector3 left, Vector3 right)
        {
            return Vector3.Cross(left, right);
        }

        /// <summary>
        /// Returns the distance from this <see cref="Vector3"/> to <paramref name="other"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector3"/>.</param>
        /// <param name="other">The <see cref="Vector3"/> to get the distance to.</param>
        /// <returns>The distance from this <see cref="Vector3"/> to <paramref name="other"/>.</returns>
        public static float DistanceTo(this Vector3 vector, Vector3 other)
        {
            return Vector3.Distance(vector, other);
        }

        /// <summary>
        /// Returns the distance from this <see cref="Vector3"/> to <paramref name="other"/> squared.
        /// </summary>
        /// <param name="vector">This <see cref="Vector3"/>.</param>
        /// <param name="other">The <see cref="Vector3"/> to get the distance to squared.</param>
        /// <returns>The distance from this <see cref="Vector3"/> to <paramref name="other"/> squared.</returns>
        public static float DistanceSquaredTo(this Vector3 vector, Vector3 other)
        {
            return MagnitudeSquared(vector - other);
        }
#endif

        /// <summary>
        /// Returns this <see cref="Vector3"/> with the same direction but a length of one.
        /// </summary>
        /// <param name="vector">This <see cref="Vector3"/>.</param>
        /// <returns><paramref name="vector"/> with a length of one.</returns>
        public static Vector3 GetNormalized(this Vector3 vector)
        {
            if (float.IsNaN(vector.X()) || float.IsNaN(vector.Y()) || float.IsNaN(vector.Z()) || (vector.X() == 0 && vector.Y() == 0 && vector.Z() == 0))
            {
                return new Vector3(0, 0, 0);
            }
#if UNITY
            return vector.normalized;
#elif GODOT
            return vector.Normalized();
#else
            return Vector3.Normalize(vector);
#endif
        }

        /// <summary>
        /// Gets the magnitude of this <see cref="Vector3"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector3"/>.</param>
        /// <returns>The magnitude of this <see cref="Vector3"/>.</returns>
        public static float Magnitude(this Vector3 vector)
        {
#if UNITY
            return vector.magnitude;
#else
            return vector.Length();
#endif
        }

        /// <summary>
        /// Gets the magnitude of this <see cref="Vector3"/> squared. This is useful for when you are comparing the lengths of two vectors
        /// but don't need to know the exact length, and avoids calculating a square root.
        /// </summary>
        public static float MagnitudeSquared(this Vector3 vector)
        {
#if UNITY
            return vector.sqrMagnitude;
#else
            return vector.LengthSquared();
#endif
        }

        /// <summary>
        /// Gets the square of the area of the triangle defined by three points. This is useful when simply comparing two areas when you don't need to know exactly what the area is.
        /// </summary>
        /// <param name="vertex1">First vertex of triangle.</param>
        /// <param name="vertex2">Second vertex of triangle.</param>
        /// <param name="vertex3">Third vertex of triangle.</param>
        /// <returns>Square of the area of the triangle defined by these three vertices.</returns>
        public static float TriangleAreaSquared(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            float side1 = vertex1.DistanceTo(vertex2);
            float side2 = vertex1.DistanceTo(vertex3);
            float side3 = vertex2.DistanceTo(vertex3);
            float semiPerimeter = (side1 + side2 + side3) / 2f;
            return semiPerimeter * (semiPerimeter - side1) * (semiPerimeter - side2) * (semiPerimeter - side3);
        }

        /// <summary>
        /// Gets the area of the triangle defined by three points using Heron's formula.
        /// </summary>
        /// <param name="vertex1">First vertex of triangle.</param>
        /// <param name="vertex2">Second vertex of triangle.</param>
        /// <param name="vertex3">Third vertex of triangle.</param>
        /// <returns>Area of the triangle defined by these three vertices.</returns>
        public static float TriangleArea(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
        {
            return (float)Math.Sqrt(TriangleAreaSquared(vertex1, vertex2, vertex3));
        }

        /// <summary>
        /// Gets a <c>byte</c> array representing the components of this <see cref="Vector3"/> as <c>float</c>s.
        /// </summary>
        /// <param name="vector">This <see cref="Vector3"/>.</param>
        /// <returns><c>byte</c> array with the components' bytes.</returns>
        public static byte[] GetBytes(this Vector3 vector)
        {
            byte[] ret = new byte[12];
            BitConverter.GetBytes(vector.X()).CopyTo(ret, 0);
            BitConverter.GetBytes(vector.Y()).CopyTo(ret, 4);
            BitConverter.GetBytes(vector.Z()).CopyTo(ret, 8);
            return ret;
        }

        /// <summary>
        /// Returns a <see cref="Vector3"/> converted from 12 bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <returns>A <see cref="Vector3"/> representing the converted bytes.</returns>
        public static Vector3 ToVector3(byte[] value, int startIndex = 0)
        {
            return new Vector3(BitConverter.ToSingle(value, startIndex), BitConverter.ToSingle(value, startIndex + 4), BitConverter.ToSingle(value, startIndex + 8));
        }

        /// <summary>
        /// Gets the X component of this <see cref="Vector3"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector3"/>.</param>
        /// <returns>The X component of this <see cref="Vector3"/>.</returns>
        public static float X(this Vector3 vector)
        {
#if UNITY || GODOT
            return vector.x;
#else
            return vector.X;
#endif
        }

        /// <summary>
        /// Gets the Y component of this <see cref="Vector3"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector3"/>.</param>
        /// <returns>The Y component of this <see cref="Vector3"/>.</returns>
        public static float Y(this Vector3 vector)
        {
#if UNITY || GODOT
            return vector.y;
#else
            return vector.Y;
#endif
        }

        /// <summary>
        /// Gets the Z component of this <see cref="Vector3"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector3"/>.</param>
        /// <returns>The Z component of this <see cref="Vector3"/>.</returns>
        public static float Z(this Vector3 vector)
        {
#if UNITY || GODOT
            return vector.z;
#else
            return vector.Z;
#endif
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Lump{Vector3}"/>.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Lump{Vector3}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public static Lump<Vector3> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }
            int structLength = GetStructLength(bsp.MapType, lumpInfo.version);
            int numObjects = data.Length / structLength;
            Lump<Vector3> lump = new Lump<Vector3>(numObjects, bsp, lumpInfo);
            for (int i = 0; i < numObjects; ++i)
            {
                lump.Add(ToVector3(data, i * structLength));
            }
            return lump;
        }

        /// <summary>
        /// Gets the index for the vertex normals lump in the BSP file for a specific map format.
        /// </summary>
        /// <param name="type">The map type.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
        public static int GetIndexForNormalsLump(MapType type)
        {
            if (type == MapType.Nightfire)
            {
                return 5;
            }

            return -1;
        }

        /// <summary>
        /// Gets the index for the patch vertices lump in the BSP file for a specific map format.
        /// </summary>
        /// <param name="type">The map type.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
        public static int GetIndexForPatchVertsLump(MapType type)
        {
            if (type == MapType.CoD || type == MapType.CoDDemo)
            {
                return 25;
            }

            return -1;
        }

        /// <summary>
        /// Gets the length of the <see cref="Vector3"/> struct for the given <see cref="MapType"/> and <paramref name="version"/>.
        /// </summary>
        /// <param name="type">The type of BSP to get struct length for.</param>
        /// <param name="version">Version of the lump.</param>
        /// <returns>The length of the struct for the given <see cref="MapType"/> of the given <paramref name="version"/>.</returns>
        public static int GetStructLength(MapType type, int version)
        {
            return 12;
        }
    }
}
