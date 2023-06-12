#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#if !UNITY_5_6_OR_NEWER
#define OLDUNITY
#endif
#endif

using System;
using System.Collections.Generic;

namespace LibBSP
{
#if UNITY
    using Plane = UnityEngine.Plane;
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;
    using Vector4 = UnityEngine.Vector4;
#if !OLDUNITY
    using Vertex = UnityEngine.UIVertex;
#endif
#elif GODOT
    using Plane = Godot.Plane;
    using Vector2 = Godot.Vector2;
    using Vector3 = Godot.Vector3;
    using Vector4 = Godot.Quat;
#elif NEOAXIS
    using Plane = NeoAxis.PlaneF;
    using Vector2 = NeoAxis.Vector2F;
    using Vector3 = NeoAxis.Vector3F;
    using Vector4 = NeoAxis.Vector4F;
    using Vertex = NeoAxis.StandardVertex;
#else
    using Plane = System.Numerics.Plane;
    using Vector2 = System.Numerics.Vector2;
    using Vector3 = System.Numerics.Vector3;
    using Vector4 = System.Numerics.Vector4;
#endif

    public class Lump<T> : List<T>, ILump
    {

        /// <summary>
        /// The <see cref="BSP"/> this <see cref="ILump"/> came from.
        /// </summary>
        public BSP Bsp { get; set; }

        /// <summary>
        /// The <see cref="LibBSP.LumpInfo"/> associated with this <see cref="Lump{T}"/>.
        /// </summary>
        public LumpInfo LumpInfo { get; protected set; }

        /// <summary>
        /// Gets the length of this lump in bytes.
        /// </summary>
        public virtual int Length
        {
            get
            {
                if (Count > 0)
                {
                    Type type = typeof(T);
                    if (typeof(ILumpObject).IsAssignableFrom(type))
                    {
                        return ((ILumpObject)this[0]).Data.Length * Count;
                    }
                    else if (type == typeof(Vertex))
                    {
                        return VertexExtensions.GetStructLength(Bsp.MapType, LumpInfo.version) * Count;
                    }
                    else if (type == typeof(Plane))
                    {
                        return PlaneExtensions.GetStructLength(Bsp.MapType, LumpInfo.version) * Count;
                    }
                    else if (type == typeof(Vector2))
                    {
                        return 8 * Count;
                    }
                    else if (type == typeof(Vector3))
                    {
                        return 12 * Count;
                    }
                    else if (type == typeof(Vector4))
                    {
                        return 16 * Count;
                    }
                    else if (type == typeof(byte))
                    {
                        return Count;
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// Creates an empty <c>Lump</c> of <typeparamref name="T"/> objects.
        /// </summary>
        /// <param name="bsp">The <see cref="BSP"/> which <paramref name="data"/> came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> object for this <c>Lump</c>.</param>
        public Lump(BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo))
        {
            Bsp = bsp;
            LumpInfo = lumpInfo;
        }

        /// <summary>
        /// Creates a new <c>Lump</c> that contains elements copied from the passed <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="items">The elements to copy into this <c>Lump</c>.</param>
        /// <param name="bsp">The <see cref="BSP"/> which <paramref name="data"/> came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> object for this <c>Lump</c>.</param>
        public Lump(IEnumerable<T> items, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(items)
        {
            Bsp = bsp;
            LumpInfo = lumpInfo;
        }

        /// <summary>
        /// Creates an empty <c>Lump</c> of <typeparamref name="T"/> objects with the specified initial capactiy.
        /// </summary>
        /// <param name="capacity">The number of elements that can initially be stored.</param>
        /// <param name="bsp">The <see cref="BSP"/> which <paramref name="data"/> came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> object for this <c>Lump</c>.</param>
        public Lump(int capacity, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(capacity)
        {
            Bsp = bsp;
            LumpInfo = lumpInfo;
        }

        /// <summary>
        /// Parses the passed <c>byte</c> array into a <c>Lump</c> of <typeparamref name="T"/> objects.
        /// </summary>
        /// <param name="data">Array of <c>byte</c>s to parse.</param>
        /// <param name="structLength">Number of <c>byte</c>s to copy into the elements. Negative values indicate a variable length, which is not supported by this constructor.</param>
        /// <param name="bsp">The <see cref="BSP"/> which <paramref name="data"/> came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> object for this <c>Lump</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data" /> was <c>null</c>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="structLength"/> is negative.</exception>
        public Lump(byte[] data, int structLength, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(data.Length / structLength)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }
            if (structLength <= 0)
            {
                throw new NotSupportedException("Cannot use the base Lump constructor for variable length lumps (structLength was negative). Create a derived class with a new constructor instead.");
            }

            Bsp = bsp;
            LumpInfo = lumpInfo;
            for (int i = 0; i < data.Length / structLength; ++i)
            {
                byte[] bytes = new byte[structLength];
                Array.Copy(data, (i * structLength), bytes, 0, structLength);
                Add((T)Activator.CreateInstance(typeof(T), new object[] { bytes, this }));
            }
        }

        /// <summary>
        /// Gets all the data in this lump as a byte array.
        /// </summary>
        /// <param name="lumpOffset">The offset of the beginning of this lump.</param>
        /// <returns>The data.</returns>
        public virtual byte[] GetBytes(int lumpOffset = 0)
        {
            byte[] data;

            if (Count > 0)
            {
                Type type = typeof(T);
                if (typeof(ILumpObject).IsAssignableFrom(type))
                {
                    int length = ((ILumpObject)this[0]).Data.Length;
                    data = new byte[length * Count];
                    for (int i = 0; i < Count; ++i)
                    {
                        ((ILumpObject)this[i]).Data.CopyTo(data, length * i);
                    }
                }
                else if (type == typeof(Vertex))
                {
                    int length = VertexExtensions.GetStructLength(Bsp.MapType, LumpInfo.version);
                    data = new byte[length * Count];
                    for (int i = 0; i < Count; ++i)
                    {
                        ((Vertex)(object)this[i]).GetBytes(Bsp.MapType, LumpInfo.version).CopyTo(data, length * i);
                    }
                }
                else if (type == typeof(Plane))
                {
                    int length = PlaneExtensions.GetStructLength(Bsp.MapType, LumpInfo.version);
                    data = new byte[length * Count];
                    for (int i = 0; i < Count; ++i)
                    {
                        ((Plane)(object)this[i]).GetBytes(Bsp.MapType, LumpInfo.version).CopyTo(data, length * i);
                    }
                }
                else if (type == typeof(Vector2))
                {
                    data = new byte[8 * Count];
                    for (int i = 0; i < Count; ++i)
                    {
                        ((Vector2)(object)this[i]).GetBytes().CopyTo(data, 8 * i);
                    }
                }
                else if (type == typeof(Vector3))
                {
                    data = new byte[12 * Count];
                    for (int i = 0; i < Count; ++i)
                    {
                        ((Vector3)(object)this[i]).GetBytes().CopyTo(data, 12 * i);
                    }
                }
                else if (type == typeof(Vector4))
                {
                    data = new byte[16 * Count];
                    for (int i = 0; i < Count; ++i)
                    {
                        ((Vector4)(object)this[i]).GetBytes().CopyTo(data, 16 * i);
                    }
                }
                else if (type == typeof(byte))
                {
                    data = (byte[])(object)ToArray();
                }
                else
                {
                    data = new byte[0];
                }
            }
            else
            {
                data = new byte[0];
            }

            return data;
        }

    }
}
