#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;

namespace LibBSP
{
#if UNITY
    using Vector2 = UnityEngine.Vector2;
#elif GODOT
    using Vector2 = Godot.Vector2;
#elif NEOAXIS
    using Vector2 = NeoAxis.Vector2F;
#else
    using Vector2 = System.Numerics.Vector2;
#endif

    /// <summary>
    /// Class containing helper methods for <see cref="Vector2"/> objects.
    /// </summary>
    public static partial class Vector2Extensions
    {

#if !GODOT
        /// <summary>
        /// Vector dot product. This operation is commutative.
        /// </summary>
        /// <param name="vector">This <see cref="Vector2"/>.</param>
        /// <param name="other">The <see cref="Vector2"/> to dot with this <see cref="Vector2"/>.</param>
        /// <returns>Dot product of this <see cref="Vector2"/> and <paramref name="other"/>.</returns>
        public static float Dot(this Vector2 vector, Vector2 other)
        {
            return Vector2.Dot(vector, other);
        }

        /// <summary>
        /// Returns the distance from this <see cref="Vector2"/> to <paramref name="other"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector2"/>.</param>
        /// <param name="other">The <see cref="Vector2"/> to get the distance to.</param>
        /// <returns>The distance from this <see cref="Vector2"/> to <paramref name="other"/>.</returns>
        public static float DistanceTo(this Vector2 vector, Vector2 other)
        {
            return Vector2.Distance(vector, other);
        }

        /// <summary>
        /// Returns the distance from this <see cref="Vector2"/> to <paramref name="other"/> squared.
        /// </summary>
        /// <param name="vector">This <see cref="Vector2"/>.</param>
        /// <param name="other">The <see cref="Vector2"/> to get the distance to squared.</param>
        /// <returns>The distance from this <see cref="Vector2"/> to <paramref name="other"/> squared.</returns>
        public static float DistanceSquaredTo(this Vector2 vector, Vector2 other)
        {
            return MagnitudeSquared(vector - other);
        }
#endif

        /// <summary>
        /// Returns this <see cref="Vector2"/> with the same direction but a length of one.
        /// </summary>
        /// <param name="vector">This <see cref="Vector2"/>.</param>
        /// <returns><paramref name="vector"/> with a length of one.</returns>
        public static Vector2 GetNormalized(this Vector2 vector)
        {
            if (float.IsNaN(vector.X()) || float.IsNaN(vector.Y()) || (vector.X() == 0 && vector.Y() == 0))
            {
                return new Vector2(0, 0);
            }
#if UNITY
            return vector.normalized;
#elif GODOT
            return vector.Normalized();
#else
            return Vector2.Normalize(vector);
#endif
        }

        /// <summary>
        /// Gets the magnitude of this <see cref="Vector2"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector2"/>.</param>
        /// <returns>The magnitude of this <see cref="Vector2"/>.</returns>
        public static float Magnitude(this Vector2 vector)
        {
#if UNITY
            return vector.magnitude;
#else
            return vector.Length();
#endif
        }

        /// <summary>
        /// Gets the magnitude of this <see cref="Vector2"/> squared. This is useful for when you are comparing the lengths of two vectors
        /// but don't need to know the exact length, and avoids calculating a square root.
        /// </summary>
        public static float MagnitudeSquared(this Vector2 vector)
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
        public static float TriangleAreaSquared(Vector2 vertex1, Vector2 vertex2, Vector2 vertex3)
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
        public static float TriangleArea(Vector2 vertex1, Vector2 vertex2, Vector2 vertex3)
        {
            return (float)Math.Sqrt(TriangleAreaSquared(vertex1, vertex2, vertex3));
        }

        /// <summary>
        /// Gets a <c>byte</c> array representing the components of this <see cref="Vector2"/> as <c>float</c>s.
        /// </summary>
        /// <param name="vector">This <see cref="Vector2"/>.</param>
        /// <returns><c>byte</c> array with the components' bytes.</returns>
        public static byte[] GetBytes(this Vector2 vector)
        {
            byte[] ret = new byte[8];
            BitConverter.GetBytes(vector.X()).CopyTo(ret, 0);
            BitConverter.GetBytes(vector.Y()).CopyTo(ret, 4);
            return ret;
        }

        /// <summary>
        /// Returns a <see cref="Vector2"/> converted from eight bytes at a specified position in a byte array.
        /// </summary>
        /// <param name="value">An array of bytes.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <returns>A <see cref="Vector2"/> representing the converted bytes.</returns>
        public static Vector2 ToVector2(byte[] value, int startIndex = 0)
        {
            return new Vector2(BitConverter.ToSingle(value, startIndex), BitConverter.ToSingle(value, startIndex + 4));
        }

        /// <summary>
        /// Gets the X component of this <see cref="Vector2"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector2"/>.</param>
        /// <returns>The X component of this <see cref="Vector2"/>.</returns>
        public static float X(this Vector2 vector)
        {
#if UNITY || GODOT
            return vector.x;
#else
            return vector.X;
#endif
        }

        /// <summary>
        /// Gets the Y component of this <see cref="Vector2"/>.
        /// </summary>
        /// <param name="vector">This <see cref="Vector2"/>.</param>
        /// <returns>The Y component of this <see cref="Vector2"/>.</returns>
        public static float Y(this Vector2 vector)
        {
#if UNITY || GODOT
            return vector.y;
#else
            return vector.Y;
#endif
        }

    }
}
