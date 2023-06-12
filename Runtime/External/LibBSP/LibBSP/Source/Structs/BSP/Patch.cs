#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#if !UNITY_5_6_OR_NEWER
// UIVertex was introduced in Unity 4.5 but it only had color, position and one UV.
// From 4.6.0 until 5.5.6 it was missing two sets of UVs.
#define OLDUNITY
#endif
#endif

using System;
using System.Collections.Generic;
using System.Reflection;

namespace LibBSP
{
#if UNITY
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;
#if !OLDUNITY
    using Vertex = UnityEngine.UIVertex;
#endif
#elif GODOT
    using Vector2 = Godot.Vector2;
    using Vector3 = Godot.Vector3;
#elif NEOAXIS
    using Vector2 = NeoAxis.Vector2F;
    using Vector3 = NeoAxis.Vector3F;
#else
    using Vector2 = System.Numerics.Vector2;
    using Vector3 = System.Numerics.Vector3;
#endif

    /// <summary>
    /// Holds the data for a patch in a CoD BSP.
    /// </summary>
    /// <remarks>
    /// This lump only seems to be used for patch collision data, the visual parts of patch and terrain
    /// meshes are still stored in the <see cref="Face"/>s lump. Since patches are stored this way and the
    /// CoD <see cref="Face"/> structure does away with Quake 3's type field, there's no easy reliable way
    /// of getting UVs for the vertices.
    /// </remarks>
    public struct Patch : ILumpObject
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
        /// Gets the <see cref="LibBSP.Texture"/> referenced by this <see cref="Patch"/>.
        /// </summary>
        public Texture Shader
        {
            get
            {
                return Parent.Bsp.Textures[ShaderIndex];
            }
        }

        /// <summary>
        /// Gets or sets the index of the <see cref="LibBSP.Texture"/> used by this <see cref="Patch"/>.
        /// </summary>
        public int ShaderIndex
        {
            get
            {
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        return BitConverter.ToInt16(Data, 0);
                    }
                    default:
                    {
                        return -1;
                    }
                }
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        bytes.CopyTo(Data, 0);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the type of <see cref="Patch"/> this is. 0 is a typical patch, 1 is a terrain.
        /// </summary>
        public short Type
        {
            get
            {
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        return BitConverter.ToInt16(Data, 2);
                    }
                    default:
                    {
                        return -1;
                    }
                }
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        bytes.CopyTo(Data, 2);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// If this is a typical patch, gets or sets the dimensions of this patch.
        /// </summary>
        public Vector2 Dimensions
        {
            get
            {
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        if (Type == 0)
                        {
                            return new Vector2(BitConverter.ToInt16(Data, 4), BitConverter.ToInt16(Data, 6));
                        }
                        else
                        {
                            return new Vector2(0, 0);
                        }
                    }
                    default:
                    {
                        return new Vector2(0, 0);
                    }
                }
            }
            set
            {
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        if (Type == 0)
                        {
                            BitConverter.GetBytes((short)value.X()).CopyTo(Data, 4);
                            BitConverter.GetBytes((short)value.Y()).CopyTo(Data, 6);
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// If this is a typical patch, gets or sets the flags on this patch.
        /// </summary>
        public int Flags
        {
            get
            {
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        if (Type == 0)
                        {
                            return BitConverter.ToInt32(Data, 8);
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    default:
                    {
                        return -1;
                    }
                }
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        if (Type == 0)
                        {
                            bytes.CopyTo(Data, 8);
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the Patch <see cref="Vector3"/>s referenced by this <see cref="Patch"/>.
        /// </summary>
        public IEnumerable<Vector3> Vertices
        {
            get
            {
                for (int i = 0; i < NumVertices; ++i)
                {
                    yield return Parent.Bsp.PatchVertices[FirstVertex + i];
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first Patch <see cref="Vertex"/> used by this <see cref="Patch"/>.
        /// </summary>
        [Index("PatchVertices")]
        public int FirstVertex
        {
            get
            {
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        if (Type == 0)
                        {
                            return BitConverter.ToInt32(Data, 12);
                        }
                        else if (Type == 1)
                        {
                            return BitConverter.ToInt32(Data, 8);
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    default:
                    {
                        return -1;
                    }
                }
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        if (Type == 0)
                        {
                            bytes.CopyTo(Data, 12);
                        }
                        else if (Type == 1)
                        {
                            bytes.CopyTo(Data, 8);
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the count of Patch <see cref="Vertex"/>es used by this <see cref="Patch"/>.
        /// </summary>
        [Count("PatchVertices")]
        public int NumVertices
        {
            get
            {
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        if (Type == 0)
                        {
                            return BitConverter.ToInt16(Data, 4) * BitConverter.ToInt16(Data, 6);
                        }
                        else if (Type == 1)
                        {
                            return BitConverter.ToInt16(Data, 4);
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    default:
                    {
                        return -1;
                    }
                }
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        if (Type == 1)
                        {
                            Data[4] = bytes[0];
                            Data[5] = bytes[1];
                        }
                        else
                        {
                            throw new NotSupportedException("Cannot set count of Patch Vertices on a pure patch. Set Patch.Dimensions instead.");
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// If this <see cref="Patch"/> is a terrain, enumerates the Patch <see cref="Vertex"/> indices
        /// referenced by this <see cref="Patch"/>.
        /// </summary>
        public IEnumerable<short> VertexIndices
        {
            get
            {
                for (int i = 0; i < NumVertexIndices; ++i)
                {
                    yield return (short)Parent.Bsp.PatchIndices[FirstVertexIndex + i];
                }
            }
        }

        /// <summary>
        /// If this <see cref="Patch"/> is a terrain, gets the count of Patch <see cref="Vertex"/> indices
        /// used by this <see cref="Patch"/>.
        /// </summary>
        [Count("PatchIndices")]
        public int NumVertexIndices
        {
            get
            {
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        if (Type == 1)
                        {
                            return BitConverter.ToInt16(Data, 6);
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    default:
                    {
                        return -1;
                    }
                }
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        if (Type == 1)
                        {
                            Data[6] = bytes[0];
                            Data[7] = bytes[1];
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// If this <see cref="Patch"/> is a terrain, gets or sets the index of the first Patch <see cref="Vertex"/>
        /// index used by this <see cref="Patch"/>.
        /// </summary>
        [Index("PatchIndices")]
        public int FirstVertexIndex
        {
            get
            {
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        if (Type == 1)
                        {
                            return BitConverter.ToInt32(Data, 12);
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    default:
                    {
                        return -1;
                    }
                }
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);
                switch (MapType)
                {
                    case MapType.CoD:
                    {
                        if (Type == 1)
                        {
                            bytes.CopyTo(Data, 12);
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Patch"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="Patch"/> came from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public Patch(byte[] data, ILump parent = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Parent = parent;
        }

        /// <summary>
        /// Creates a new <see cref="Patch"/> by copying the fields in <paramref name="source"/>, using
        /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
        /// to use when creating the new <see cref="Patch"/>.
        /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
        /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
        /// </summary>
        /// <param name="source">The <see cref="Patch"/> to copy.</param>
        /// <param name="parent">
        /// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="Patch"/>.
        /// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
        /// </param>
        public Patch(Patch source, ILump parent) : this()
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

            ShaderIndex = source.ShaderIndex;
            Type = source.Type;

            if (Type == 0)
            {
                Dimensions = source.Dimensions;
                Flags = source.Flags;
            }
            else if (Type == 1)
            {
                NumVertices = source.NumVertices;
                NumVertexIndices = source.NumVertexIndices;
                FirstVertexIndex = source.FirstVertexIndex;
            }

            FirstVertex = source.FirstVertex;
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Lump{Patch}"/>.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Lump{Patch}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
        public static Lump<Patch> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            return new Lump<Patch>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
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
            switch (mapType)
            {
                case MapType.CoD:
                {
                    return 16;
                }
                default:
                {
                    throw new ArgumentException("Lump object " + MethodBase.GetCurrentMethod().DeclaringType.Name + " does not exist in map type " + mapType + " or has not been implemented.");
                }
            }
        }

        /// <summary>
        /// Gets the index for this lump in the BSP file for a specific map format.
        /// </summary>
        /// <param name="type">The map type.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
        public static int GetIndexForLump(MapType type)
        {
            switch (type)
            {
                case MapType.CoD:
                {
                    return 24;
                }
                default:
                {
                    return -1;
                }
            }
        }

    }
}
