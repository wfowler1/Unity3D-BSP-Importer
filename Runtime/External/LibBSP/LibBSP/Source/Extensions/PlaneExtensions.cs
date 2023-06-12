#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;

namespace LibBSP
{
#if UNITY
    using Plane = UnityEngine.Plane;
    using Vector3 = UnityEngine.Vector3;
    using Ray = UnityEngine.Ray;
#elif GODOT
    using Plane = Godot.Plane;
    using Vector3 = Godot.Vector3;
#elif NEOAXIS
    using Plane = NeoAxis.PlaneF;
    using Vector3 = NeoAxis.Vector3F;
    using Ray = NeoAxis.RayF;
#else
    using Plane = System.Numerics.Plane;
    using Vector3 = System.Numerics.Vector3;
#endif

    /// <summary>
    /// Static class containing helper methods for <see cref="Plane"/> objects.
    /// </summary>
    public static partial class PlaneExtensions
    {

        /// <summary>
        /// Array of base texture axes. When referenced properly, provides a good default texture axis for any given plane.
        /// </summary>
        public static readonly Vector3[] baseAxes = new Vector3[] {
            new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(0, -1, 0),
            new Vector3(0, 0, -1), new Vector3(1, 0, 0), new Vector3(0, -1, 0),
            new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, -1),
            new Vector3(-1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, -1),
            new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, -1),
            new Vector3(0, -1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, -1)
        };

        /// <summary>
        /// Gets the normal of this <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">This <see cref="Plane"/>.</param>
        /// <returns>The normal of this <see cref="Plane"/>.</returns>
        public static Vector3 Normal(this Plane plane)
        {
#if UNITY
            return plane.normal;
#else
            return plane.Normal;
#endif
        }

        /// <summary>
        /// Gets the distance of this <see cref="Plane"/> from the origin.
        /// </summary>
        /// <param name="plane">This <see cref="Plane"/>.</param>
        /// <returns>The distance of this <see cref="Plane"/> from the origin.</returns>
        public static float Distance(this Plane plane)
        {
#if UNITY
            return plane.distance;
#else
            return plane.D;
#endif
        }

        /// <summary>
        /// Intersects three <see cref="Plane"/>s at a <see cref="Vector3"/>. Returns <see cref="float.NaN"/> for all components if two or more <see cref="Plane"/>s are parallel.
        /// </summary>
        /// <param name="plane1">First <see cref="Plane"/> to intersect.</param>
        /// <param name="plane2">Second <see cref="Plane"/> to intersect.</param>
        /// <param name="plane3">Third <see cref="Plane"/> to intersect.</param>
        /// <returns>Point of intersection if all three <see cref="Plane"/>s meet at a point, (NaN, NaN, NaN) otherwise.</returns>
        public static Vector3 Intersect3(Plane plane1, Plane plane2, Plane plane3)
        {
            float denominator = plane1.Normal().Cross(plane2.Normal()).Dot(plane3.Normal());
            if (denominator == 0)
            {
                return new Vector3(float.NaN, float.NaN, float.NaN);
            }

            return (plane2.Normal().Cross(plane3.Normal()) * plane1.Distance() +
                plane3.Normal().Cross(plane1.Normal()) * plane2.Distance() +
                plane1.Normal().Cross(plane2.Normal()) * plane3.Distance()) / denominator;
        }

        /// <summary>
        /// Intersects a <see cref="Plane"/> "<paramref name="plane"/>" with a ray at a <see cref="Vector3"/>. Returns NaN for all components if they do not intersect.
        /// </summary>
        /// <param name="plane">This <see cref="Plane"/>.</param>
        /// <param name="origin">The origin point of the ray.</param>
        /// <param name="direction">The direction of the ray.</param>
        /// <returns>Point of intersection if the ray intersects "<paramref name="p"/>", (NaN, NaN, NaN) otherwise.</returns>
        public static Vector3 Intersection(this Plane plane, Vector3 origin, Vector3 direction)
        {
#if NEOAXIS
            if (plane.Intersects(new Ray(origin, direction), out Vector3 intersectionPoint))
            {
                return intersectionPoint;
            }
#else
            float enter;
            direction = direction.GetNormalized();
            bool intersected = plane.Raycast(origin, direction, out enter);
            if (intersected || enter != 0)
            {
                return origin + (enter * direction);
            }
#endif

            return new Vector3(float.NaN, float.NaN, float.NaN);
        }

        /// <summary>
        /// Raycasts a <see cref="Ray"/> against this <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">This <see cref="Plane"/>.</param>
        /// <param name="origin">The origin point of the ray.</param>
        /// <param name="direction">The direction of the ray.</param>
        /// <param name="enter"><c>out</c> parameter that will contain the distance along <paramref name="ray"/> where the collision happened.</param>
        /// <returns>
        /// <c>true</c> and <paramref name="enter"/> is positive or 0 if the ray intersects this <see cref="Plane"/> in front of the ray,
        /// <c>false</c> and <paramref name="enter"/> is negative if the ray intersects this <see cref="Plane"/> behind the ray,
        /// <c>false</c> and <paramref name="enter"/> is 0 if the ray is parallel to this <see cref="Plane"/>.
        /// </returns>
        public static bool Raycast(this Plane plane, Vector3 origin, Vector3 direction, out float enter)
        {
#if UNITY
            return plane.Raycast(new Ray(origin, direction), out enter);
#elif NEOAXIS
            return plane.Intersects(new Ray(origin, direction), out enter);
#else
            direction = direction.GetNormalized();
            float denom = direction.Dot(plane.Normal());
            if (denom > -0.005 && denom < 0.005)
            {
                enter = 0;
                return false;
            }
            enter = (-origin.Dot(plane.Normal()) - plane.Distance()) / denom;
            if (float.IsNaN(enter))
            {
                enter = 0;
                return false;
            }
            return enter > 0;
#endif
        }

        /// <summary>
        /// Generates three points which can be used to define this <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">This <see cref="Plane"/>.</param>
        /// <param name="scalar">Scale of distance between the generated points. The points will define the same <see cref="Plane"/> but will be farther apart the larger this value is. Must not be zero.</param>
        /// <returns>Three points which define this <see cref="Plane"/>.</returns>
        public static Vector3[] GenerateThreePoints(this Plane plane, float scalar = 16)
        {
            Vector3[] points = new Vector3[3];
            // Figure out if the plane is parallel to two of the axes.
            if (plane.Normal().Y() == 0 && plane.Normal().Z() == 0)
            {
                // parallel to plane YZ
                points[0] = new Vector3(plane.Distance() / plane.Normal().X(), -scalar, scalar);
                points[1] = new Vector3(plane.Distance() / plane.Normal().X(), 0, 0);
                points[2] = new Vector3(plane.Distance() / plane.Normal().X(), scalar, scalar);
                if (plane.Normal().X() > 0)
                {
                    Array.Reverse(points);
                }
            }
            else if (plane.Normal().X() == 0 && plane.Normal().Z() == 0)
            {
                // parallel to plane XZ
                points[0] = new Vector3(scalar, plane.Distance() / plane.Normal().Y(), -scalar);
                points[1] = new Vector3(0, plane.Distance() / plane.Normal().Y(), 0);
                points[2] = new Vector3(scalar, plane.Distance() / plane.Normal().Y(), scalar);
                if (plane.Normal().Y() > 0)
                {
                    Array.Reverse(points);
                }
            }
            else if (plane.Normal().X() == 0 && plane.Normal().Y() == 0)
            {
                // parallel to plane XY
                points[0] = new Vector3(-scalar, scalar, plane.Distance() / plane.Normal().Z());
                points[1] = new Vector3(0, 0, plane.Distance() / plane.Normal().Z());
                points[2] = new Vector3(scalar, scalar, plane.Distance() / plane.Normal().Z());
                if (plane.Normal().Z() > 0)
                {
                    Array.Reverse(points);
                }
            }
            else if (plane.Normal().X() == 0)
            {
                // If you reach this point the plane is not parallel to any two-axis plane.
                // parallel to X axis
                points[0] = new Vector3(-scalar, scalar * scalar, (-(scalar * scalar * plane.Normal().Y() - plane.Distance())) / plane.Normal().Z());
                points[1] = new Vector3(0, 0, plane.Distance() / plane.Normal().Z());
                points[2] = new Vector3(scalar, scalar * scalar, (-(scalar * scalar * plane.Normal().Y() - plane.Distance())) / plane.Normal().Z());
                if (plane.Normal().Z() > 0)
                {
                    Array.Reverse(points);
                }
            }
            else if (plane.Normal().Y() == 0)
            {
                // parallel to Y axis
                points[0] = new Vector3((-(scalar * scalar * plane.Normal().Z() - plane.Distance())) / plane.Normal().X(), -scalar, scalar * scalar);
                points[1] = new Vector3(plane.Distance() / plane.Normal().X(), 0, 0);
                points[2] = new Vector3((-(scalar * scalar * plane.Normal().Z() - plane.Distance())) / plane.Normal().X(), scalar, scalar * scalar);
                if (plane.Normal().X() > 0)
                {
                    Array.Reverse(points);
                }
            }
            else if (plane.Normal().Z() == 0)
            {
                // parallel to Z axis
                points[0] = new Vector3(scalar * scalar, (-(scalar * scalar * plane.Normal().X() - plane.Distance())) / plane.Normal().Y(), -scalar);
                points[1] = new Vector3(0, plane.Distance() / plane.Normal().Y(), 0);
                points[2] = new Vector3(scalar * scalar, (-(scalar * scalar * plane.Normal().X() - plane.Distance())) / plane.Normal().Y(), scalar);
                if (plane.Normal().Y() > 0)
                {
                    Array.Reverse(points);
                }
            }
            else
            {
                // If you reach this point the plane is not parallel to any axis. Therefore, any two coordinates will give a third.
                points[0] = new Vector3(-scalar, scalar * scalar, -(-scalar * plane.Normal().X() + scalar * scalar * plane.Normal().Y() - plane.Distance()) / plane.Normal().Z());
                points[1] = new Vector3(0, 0, plane.Distance() / plane.Normal().Z());
                points[2] = new Vector3(scalar, scalar * scalar, -(scalar * plane.Normal().X() + scalar * scalar * plane.Normal().Y() - plane.Distance()) / plane.Normal().Z());
                if (plane.Normal().Z() > 0)
                {
                    Array.Reverse(points);
                }
            }
            return points;
        }

#if !UNITY
        /// <summary>
        /// Gets the signed distance from this <see cref="Plane"/> to a given point.
        /// </summary>
        /// <param name="plane">This <see cref="Plane"/>.</param>
        /// <param name="point">Point to get the distance to.</param>
        /// <returns>Signed distance from this <see cref="Plane"/> to the given point.</returns>
        public static float GetDistanceToPoint(this Plane plane, Vector3 point)
        {
#if GODOT
            return plane.Normal.Dot(point) + plane.D;
#elif NEOAXIS
            return plane.GetDistance(point);
#else
            return Plane.DotCoordinate(plane, point);
#endif
        }

        /// <summary>
        /// Is <paramref name="vector"/> on the positive side of this <see cref="Plane"/>?
        /// </summary>
        /// <param name="plane">This <see cref="Plane"/>.</param>
        /// <param name="vector">Point to get the side for.</param>
        /// <returns><c>true</c> if <paramref name="vector"/> is on the positive side of this <see cref="Plane"/>.</returns>
        public static bool GetSide(this Plane plane, Vector3 vector)
        {
            return plane.GetDistanceToPoint(vector) > 0;
        }
#endif

#if !GODOT
        /// <summary>
        /// Determines whether <paramref name="point"/> lies on this <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane">This <see cref="Plane"/>.</param>
        /// <param name="point">The point to determine whether or not it lies on the plane.</param>
        /// <returns><c>true</c> if <paramref name="point"/> lies on this <see cref="Plane"/>.</returns>
        public static bool HasPoint(this Plane plane, Vector3 point, float epsilon = 0.00001f)
        {
            float distanceTo = plane.GetDistanceToPoint(point);
            return distanceTo < epsilon && distanceTo > -epsilon;
        }
#endif

        /// <summary>
        /// Creates a <see cref="Plane"/> object that contains three specified points.
        /// </summary>
        /// <param name="point1">The first point defining the plane.</param>
        /// <param name="point2">The second point defining the plane.</param>
        /// <param name="point3">The third point defining the plane.</param>
        /// <returns>The <see cref="Plane"/> containing the three points.</returns>
        public static Plane CreateFromVertices(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            if ((point1 + point2).Cross(point1 + point3).Magnitude() == 0 ||
                float.IsNaN(point1.X()) || float.IsNaN(point1.Y()) || float.IsNaN(point1.Z()) ||
                float.IsNaN(point2.X()) || float.IsNaN(point2.Y()) || float.IsNaN(point2.Z()) ||
                float.IsNaN(point3.X()) || float.IsNaN(point3.Y()) || float.IsNaN(point3.Z()))
            {
                return new Plane(new Vector3(0, 0, 0), 0);
            }
#if UNITY
            return new Plane(point1, point2, point3);
#elif GODOT
            Plane plane = new Plane(point1, point3, point2);
            plane.D *= -1;
            return plane;
#elif NEOAXIS
            return Plane.FromPoints(point1, point2, point3);
#else
            return Plane.CreateFromVertices(point1, point2, point3);
#endif
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Lump{Plane}"/>.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Lump{Plane}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data" /> was null.</exception>
        /// <remarks>This function goes here since it can't go into Unity's Plane class, and so can't depend
        /// on having a constructor taking a byte array.</remarks>
        public static Lump<Plane> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }
            int structLength = GetStructLength(bsp.MapType, lumpInfo.version);
            int numObjects = data.Length / structLength;
            Lump<Plane> lump = new Lump<Plane>(numObjects, bsp, lumpInfo);
            for (int i = 0; i < numObjects; ++i)
            {
                Vector3 normal = Vector3Extensions.ToVector3(data, structLength * i);
                float distance = BitConverter.ToSingle(data, (structLength * i) + 12);
                lump.Add(new Plane(normal, distance));
            }
            return lump;
        }

        /// <summary>
        /// Gets this <see cref="Plane"/> as a <c>byte</c> array to be used in a BSP of type <see cref="type"/>.
        /// </summary>
        /// <param name="p">This <see cref="Plane"/>.</param>
        /// <param name="type">The <see cref="MapType"/> of BSP this <see cref="Plane"/> is from.</param>
        /// <param name="version">The version of the planes lump in the BSP.</param>
        /// <returns><c>byte</c> array representing this <see cref="Plane"/>'s components.</returns>
        public static byte[] GetBytes(this Plane p, MapType type, int version = 0)
        {
            byte[] bytes = new byte[GetStructLength(type, version)];

            if (type.IsSubtypeOf(MapType.Quake)
                || type.IsSubtypeOf(MapType.Quake2)
                || type.IsSubtypeOf(MapType.Source)
                || type == MapType.Nightfire)
            {
                BitConverter.GetBytes(p.Type()).CopyTo(bytes, 16);
            }

            p.Normal().GetBytes().CopyTo(bytes, 0);
            BitConverter.GetBytes(p.Distance()).CopyTo(bytes, 12);
            return bytes;
        }

        /// <summary>
        /// Gets the axis this <see cref="Plane"/>'s normal is closest to (the <see cref="Plane"/>'s normal
        /// and the axis have the largest dot product).
        /// 0 = Positive Z
        /// 1 = Negative Z
        /// 2 = Positive X
        /// 3 = Negative X
        /// 4 = Positive Y
        /// 5 = Negative Y
        /// </summary>
        /// <param name="p">This <see cref="Plane"/>.</param>
        /// <returns>The best-match axis for this <see cref="Plane"/>.</returns>
        public static int BestAxis(this Plane p)
        {
            int bestaxis = 0;
            float best = 0; // "Best" dot product so far
            for (int i = 0; i < 6; ++i)
            {
                // For all possible axes, positive and negative
                float dot = p.Normal().Dot(baseAxes[i * 3]);
                if (dot > best)
                {
                    best = dot;
                    bestaxis = i;
                }
            }
            return bestaxis;
        }

        /// <summary>
        /// Gets the axial type of this plane.
        /// 0 = X
        /// 1 = Y
        /// 2 = Z
        /// 3 = Closest to X
        /// 4 = Closest to Y
        /// 5 = Closest to Z
        /// </summary>
        /// <param name="p">This <see cref="Plane"/>.</param>
        /// <returns>The axial type of this plane.</returns>
        public static int Type(this Plane p)
        {
            float ax = Math.Abs(p.Normal().X());
            if (ax >= 1.0)
            {
                return 0;
            }

            float ay = Math.Abs(p.Normal().Y());
            if (ay >= 1.0)
            {
                return 1;
            }

            float az = Math.Abs(p.Normal().Z());
            if (az >= 1.0)
            {
                return 2;
            }

            if (ax >= ay && ax >= az)
            {
                return 3;
            }
            if (ay >= ax && ay >= az)
            {
                return 4;
            }
            return 5;
        }

        /// <summary>
        /// Gets the index for this lump in the BSP file for a specific map format.
        /// </summary>
        /// <param name="type">The map type.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
        public static int GetIndexForLump(MapType type)
        {
            if (type == MapType.BlueShift)
            {
                return 0;
            }
            else if (type.IsSubtypeOf(MapType.Source)
                || type.IsSubtypeOf(MapType.Quake)
                || type.IsSubtypeOf(MapType.Quake2)
                || type.IsSubtypeOf(MapType.UberTools)
                || type == MapType.Nightfire)
            {
                return 1;
            }
            else if (type == MapType.CoD2
                || type == MapType.CoD4)
            {
                return 4;
            }
            else if (type.IsSubtypeOf(MapType.Quake3))
            {
                return 2;
            }

            return -1;
        }

        /// <summary>
        /// Gets the <see cref="Plane"/> structure length for the specified <see cref="MapType"/>.
        /// </summary>
        /// <param name="type">The version of BSP this plane came from.</param>
        /// <param name="version">The version of the planes lump this plane came from.</param>
        /// <returns>The length of this structure, in bytes.</returns>
        public static int GetStructLength(MapType type, int version)
        {
            if (type == MapType.Titanfall
                || type.IsSubtypeOf(MapType.Quake3))
            {
                return 16;
            }
            else if (type == MapType.Nightfire
                || type.IsSubtypeOf(MapType.Quake)
                || type.IsSubtypeOf(MapType.Quake2)
                || type.IsSubtypeOf(MapType.Source))
            {
                return 20;
            }

            return 0;
        }
    }
}
