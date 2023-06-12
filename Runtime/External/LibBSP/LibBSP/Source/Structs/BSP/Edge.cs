#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#if !UNITY_5_6_OR_NEWER
#define OLDUNITY
#endif
#endif

using System;
using System.Reflection;

namespace LibBSP
{
#if UNITY
    using Vector3 = UnityEngine.Vector3;
#if !OLDUNITY
    using Vertex = UnityEngine.UIVertex;
#endif
#elif NEOAXIS
    using Vertex = NeoAxis.StandardVertex;
#endif

    /// <summary>
    /// Holds all the data for an edge in a BSP map.
    /// </summary>
    public struct Edge : ILumpObject
    {

        /// <summary>
        /// The <see cref="ILump"/> this <see cref="ILumpObject"/> came from.
        /// </summary>
        public ILump Parent { get; private set; }

        /// <summary>
        /// Array of <c>byte</c>s used as the data source for this <see cref="ILumpObject"/>.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// The <see cref="LibBSP.MapType"/> to use to interpret <see cref="Data"/>.
        /// </summary>
        public MapType MapType
        {
            get
            {
                if (Parent == null || Parent.Bsp == null)
                {
                    return MapType.Undefined;
                }
                return Parent.Bsp.MapType;
            }
        }

        /// <summary>
        /// The version number of the <see cref="ILump"/> this <see cref="ILumpObject"/> came from.
        /// </summary>
        public int LumpVersion
        {
            get
            {
                if (Parent == null)
                {
                    return 0;
                }
                return Parent.LumpInfo.version;
            }
        }

        /// <summary>
        /// Gets the first <see cref="Vertex"/> in this Edge.
        /// </summary>
        public Vertex FirstVertex
        {
            get
            {
                return Parent.Bsp.Vertices[FirstVertexIndex];
            }
        }

        /// <summary>
        /// Gets or sets the index of the first <see cref="Vertex"/> in this Edge.
        /// </summary>
        public int FirstVertexIndex
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 0);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToUInt16(Data, 0);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.Vindictus)
                {
                    bytes.CopyTo(Data, 0);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[0] = bytes[0];
                    Data[1] = bytes[1];
                }
            }
        }

        /// <summary>
        /// Gets the second <see cref="Vertex"/> in this Edge.
        /// </summary>
        public Vertex SecondVertex
        {
            get
            {
                return Parent.Bsp.Vertices[SecondVertexIndex];
            }
        }

        /// <summary>
        /// Gets or sets the index of the second <see cref="Vertex"/> in this Edge.
        /// </summary>
        public int SecondVertexIndex
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 4);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToUInt16(Data, 2);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.Vindictus)
                {
                    BitConverter.GetBytes(value).CopyTo(Data, 4);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[2] = bytes[0];
                    Data[3] = bytes[1];
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Edge"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="Edge"/> came from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public Edge(byte[] data, ILump parent = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Parent = parent;
        }

        /// <summary>
        /// Creates a new <see cref="Edge"/> by copying the fields in <paramref name="source"/>, using
        /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
        /// to use when creating the new <see cref="Edge"/>.
        /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
        /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
        /// </summary>
        /// <param name="source">The <see cref="Edge"/> to copy.</param>
        /// <param name="parent">
        /// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="Edge"/>.
        /// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
        /// </param>
        public Edge(Edge source, ILump parent)
        {
            Parent = parent;

            if (parent != null && parent.Bsp != null)
            {
                if (source.Parent != null && source.Parent.Bsp != null && source.Parent.Bsp.MapType == parent.Bsp.MapType && source.LumpVersion == parent.LumpInfo.version)
                {
                    Data = new byte[source.Data.Length];
                    Array.Copy(source.Data, Data, source.Data.Length);
                    return;
                }
                else
                {
                    Data = new byte[GetStructLength(parent.Bsp.MapType, parent.LumpInfo.version)];
                }
            }
            else
            {
                if (source.Parent != null && source.Parent.Bsp != null)
                {
                    Data = new byte[GetStructLength(source.Parent.Bsp.MapType, source.Parent.LumpInfo.version)];
                }
                else
                {
                    Data = new byte[GetStructLength(MapType.Undefined, 0)];
                }
            }

            FirstVertexIndex = source.FirstVertexIndex;
            SecondVertexIndex = source.SecondVertexIndex;
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Lump{Edge}"/>.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Lump{Edge}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
        public static Lump<Edge> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            return new Lump<Edge>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
        }

        /// <summary>
        /// Gets the length of this struct's data for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.
        /// </summary>
        /// <param name="mapType">The <see cref="LibBSP.MapType"/> of the BSP.</param>
        /// <param name="lumpVersion">The version number for the lump.</param>
        /// <returns>The length, in <c>byte</c>s, of this struct.</returns>
        /// <exception cref="ArgumentException">This struct is not valid or is not implemented for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.</exception>
        public static int GetStructLength(MapType mapType, int lumpVersion = 0)
        {
            if (mapType == MapType.Vindictus)
            {
                return 8;
            }
            else if (mapType.IsSubtypeOf(MapType.Quake)
                || mapType.IsSubtypeOf(MapType.Quake2)
                || mapType.IsSubtypeOf(MapType.Source))
            {
                return 4;
            }

            throw new ArgumentException("Lump object " + MethodBase.GetCurrentMethod().DeclaringType.Name + " does not exist in map type " + mapType + " or has not been implemented.");
        }

        /// <summary>
        /// Gets the index for this lump in the BSP file for a specific map format.
        /// </summary>
        /// <param name="type">The map type.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
        public static int GetIndexForLump(MapType type)
        {
            if (type.IsSubtypeOf(MapType.Quake2))
            {
                return 11;
            }
            else if (type.IsSubtypeOf(MapType.Quake)
                || type.IsSubtypeOf(MapType.Source))
            {
                return 12;
            }

            return -1;
        }

    }
}
