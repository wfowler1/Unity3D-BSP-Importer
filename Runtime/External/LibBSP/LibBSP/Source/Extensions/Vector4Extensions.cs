#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;

namespace LibBSP
{
#if UNITY
    using Vector4 = UnityEngine.Vector4;
#elif GODOT
    using Vector4 = Godot.Quat;
#elif NEOAXIS
    using Vector4 = NeoAxis.Vector4F;
#else
    using Vector4 = System.Numerics.Vector4;
#endif

    /// <summary>
    /// Class containing helper methods for <see cref="Vector4"/> objects.
    /// </summary>
    public static partial class Vector4Extensions
    {

#if !GODOT
        /// <summary>
        /// Vector dot product. This operation is commutative.
        /// </summary>
        /// <param name="vector">This <see cref="Vector4"/>.</param>
        /// <param name="other">The <see cref="Vector4"/> to dot with this <see cref="Vector4"/>.</param>
        /// <returns>Dot product of this <see cref="Vector4"/> and <paramref name="other"/>.</returns>
        public static float Dot(this Vector4 vector, Vector4 other)
        {
            return Vector4.Dot(vector, other);
        }
#endif

        /// <summary>
        /// Returns this <see cref="Vector4"/> with the same direction but a length of one.
        /// </summary>
        /// <param name="vector">This <see cref="Vector4"/>.</param>
        /// <returns><paramref name="vector"/> with a length of one.</returns>
        public static Vector4 GetNormalized(this Vector4 vector)
        {
            if (float.IsNaN(vector.X()) || float.IsNaN(vector.Y()) || float.IsNaN(vector.Z()) || float.IsNaN(vector.W()) || (vector.X() == 0 && vector.Y() == 0 && vector.Z() == 0 && vector.W() == 0))
            {
                return new Vector4(0, 0, 0, 0);
            }
#if UNITY
            return vector.normalized;
#elif GODOT
            return vector.Normalized();
#else
            return Vector4.Normalize(vector);
#endif
        }

        /// <summary>
        /// Returns the distance from this <see cref="Vector4"/> to <paramref name="other"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector4"/>.</param>
        /// <param name="other">The <see cref="Vector4"/> to get the distance to.</param>
        /// <returns>The distance from this <see cref="Vector4"/> to <paramref name="other"/>.</returns>
        public static float DistanceTo(this Vector4 vector, Vector4 other)
        {
#if GODOT
            return Magnitude(vector - other);
#else
            return Vector4.Distance(vector, other);
#endif
        }

        /// <summary>
        /// Returns the distance from this <see cref="Vector4"/> to <paramref name="other"/> squared.
        /// </summary>
        /// <param name="vector">This <see cref="Vector4"/>.</param>
        /// <param name="other">The <see cref="Vector4"/> to get the distance to squared.</param>
        /// <returns>The distance from this <see cref="Vector4"/> to <paramref name="other"/> squared.</returns>
        public static float DistanceSquaredTo(this Vector4 vector, Vector4 other)
        {
            return MagnitudeSquared(vector - other);
        }

        /// <summary>
        /// Gets the magnitude of this <see cref="Vector4"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector4"/>.</param>
        /// <returns>The magnitude of this <see cref="Vector4"/>.</returns>
        public static float Magnitude(this Vector4 vector)
        {
#if UNITY
            return vector.magnitude;
#elif GODOT
            return (float)Math.Sqrt(MagnitudeSquared(vector));
#else
            return vector.Length();
#endif
        }

        /// <summary>
        /// Gets the magnitude of this <see cref="Vector4"/> squared. This is useful for when you are comparing the lengths of two vectors
        /// but don't need to know the exact length, and avoids calculating a square root.
        /// </summary>
        public static float MagnitudeSquared(this Vector4 vector)
        {
#if UNITY
            return vector.sqrMagnitude;
#elif GODOT
            return (vector.x * vector.x) + (vector.y * vector.y) + (vector.z * vector.z) + (vector.w * vector.w);
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
        public static float TriangleAreaSquared(Vector4 vertex1, Vector4 vertex2, Vector4 vertex3)
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
        public static float TriangleArea(Vector4 vertex1, Vector4 vertex2, Vector4 vertex3)
        {
            return (float)Math.Sqrt(TriangleAreaSquared(vertex1, vertex2, vertex3));
        }

        /// <summary>
        /// Gets a <c>byte</c> array representing the components of this <see cref="Vector4"/> as <c>float</c>s.
        /// </summary>
        /// <param name="vector">This <see cref="Vector4"/>.</param>
        /// <returns><c>byte</c> array with the components' bytes.</returns>
        public static byte[] GetBytes(this Vector4 vector)
        {
            byte[] ret = new byte[16];
            BitConverter.GetBytes(vector.X()).CopyTo(ret, 0);
            BitConverter.GetBytes(vector.Y()).CopyTo(ret, 4);
            BitConverter.GetBytes(vector.Z()).CopyTo(ret, 8);
            BitConverter.GetBytes(vector.W()).CopyTo(ret, 12);
            return ret;
        }

        /// <summary>
        /// Returns a <see cref="Vector4"/> converted from 16 bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <returns>A <see cref="Vector4"/> representing the converted bytes.</returns>
        public static Vector4 ToVector4(byte[] value, int startIndex = 0)
        {
            return new Vector4(BitConverter.ToSingle(value, startIndex), BitConverter.ToSingle(value, startIndex + 4), BitConverter.ToSingle(value, startIndex + 8), BitConverter.ToSingle(value, startIndex + 12));
        }

        /// <summary>
        /// Gets the X component of this <see cref="Vector4"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector4"/>.</param>
        /// <returns>The X component of this <see cref="Vector4"/>.</returns>
        public static float X(this Vector4 vector)
        {
#if UNITY || GODOT
            return vector.x;
#else
            return vector.X;
#endif
        }

        /// <summary>
        /// Gets the Y component of this <see cref="Vector4"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector4"/>.</param>
        /// <returns>The Y component of this <see cref="Vector4"/>.</returns>
        public static float Y(this Vector4 vector)
        {
#if UNITY || GODOT
            return vector.y;
#else
            return vector.Y;
#endif
        }

        /// <summary>
        /// Gets the Z component of this <see cref="Vector4"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector4"/>.</param>
        /// <returns>The Z component of this <see cref="Vector4"/>.</returns>
        public static float Z(this Vector4 vector)
        {
#if UNITY || GODOT
            return vector.z;
#else
            return vector.Z;
#endif
        }

        /// <summary>
        /// Gets the Z component of this <see cref="Vector4"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector4"/>.</param>
        /// <returns>The Z component of this <see cref="Vector4"/>.</returns>
        public static float W(this Vector4 vector)
        {
#if UNITY || GODOT
            return vector.w;
#else
            return vector.W;
#endif
        }

    }
}
