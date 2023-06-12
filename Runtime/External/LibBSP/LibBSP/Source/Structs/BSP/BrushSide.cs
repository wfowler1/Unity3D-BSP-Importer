#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;
using System.Reflection;

namespace LibBSP
{
#if UNITY
    using Plane = UnityEngine.Plane;
#elif GODOT
    using Plane = Godot.Plane;
#elif NEOAXIS
    using Plane = NeoAxis.PlaneF;
#else
    using Plane = System.Numerics.Plane;
#endif

    /// <summary>
    /// Holds the data used by the brush side structures of all formats of BSP.
    /// </summary>
    public struct BrushSide : ILumpObject
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
        /// Gets the Plane referenced by this <see cref="BrushSide"/>.
        /// </summary>
        public Plane Plane
        {
            get
            {
                return Parent.Bsp.Planes[PlaneIndex];
            }
        }

        /// <summary>
        /// Gets or sets the index of the Plane used by this <see cref="BrushSide"/>.
        /// </summary>
        public int PlaneIndex
        {
            get
            {
                if (MapType == MapType.STEF2
                    || MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 4);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 0);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToUInt16(Data, 0);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.STEF2
                    || MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 4);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Vindictus)
                {
                    bytes.CopyTo(Data, 0);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[0] = bytes[0];
                    Data[1] = bytes[1];
                }
            }
        }

        /// <summary>
        /// In Call of Duty based maps, gets or sets the distance of this <see cref="BrushSide"/> from its axis.
        /// </summary>
        public float Distance
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.CoD))
                {
                    return BitConverter.ToSingle(Data, 0);
                }

                return 0;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);
                if (MapType.IsSubtypeOf(MapType.CoD))
                {
                    bytes.CopyTo(Data, 0);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="LibBSP.Texture"/> referenced by this <see cref="BrushSide"/>.
        /// </summary>
        public Texture Texture
        {
            get
            {
                return Parent.Bsp.Textures[TextureIndex];
            }
        }

        /// <summary>
        /// Gets or sets the index of the <see cref="LibBSP.Texture"/> used by this <see cref="BrushSide"/>.
        /// </summary>
        public int TextureIndex
        {
            get
            {
                if (MapType == MapType.STEF2)
                {
                    return BitConverter.ToInt32(Data, 0);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 4);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt16(Data, 2);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.STEF2)
                {
                    bytes.CopyTo(Data, 0);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Vindictus)
                {
                    bytes.CopyTo(Data, 4);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[2] = bytes[0];
                    Data[3] = bytes[1];
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="LibBSP.Face"/> referenced by this <see cref="BrushSide"/>.
        /// </summary>
        public Face Face
        {
            get
            {
                return Parent.Bsp.Faces[FaceIndex];
            }
        }

        /// <summary>
        /// Gets or sets the index of the <see cref="LibBSP.Face"/> used by this <see cref="BrushSide"/>.
        /// </summary>
        public int FaceIndex
        {
            get
            {
                switch (MapType)
                {
                    case MapType.Nightfire:
                    {
                        return BitConverter.ToInt32(Data, 0);
                    }
                    case MapType.Raven:
                    {
                        return BitConverter.ToInt32(Data, 8);
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
                    case MapType.Nightfire:
                    {
                        bytes.CopyTo(Data, 0);
                        break;
                    }
                    case MapType.Raven:
                    {
                        bytes.CopyTo(Data, 8);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// In Source engine, gets the <see cref="LibBSP.Displacement"/> referenced by this <see cref="BrushSide"/>.
        /// This is never used since the brushes used to create Displacements are optimized out.
        /// </summary>
        public Displacement Displacement
        {
            get
            {
                return Parent.Bsp.Displacements[DisplacementIndex];
            }
        }

        /// <summary>
        /// In Source engine, gets or sets the index of the <see cref="LibBSP.Displacement"/> used by this <see cref="BrushSide"/>.
        /// This is never used since the brushes used to create Displacements are optimized out.
        /// </summary>
        public int DisplacementIndex
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 8);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt16(Data, 4);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.Vindictus)
                {
                    bytes.CopyTo(Data, 8);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[4] = bytes[0];
                    Data[5] = bytes[1];
                }
            }
        }

        /// <summary>
        /// Is this <see cref="BrushSide"/> a bevel?
        /// </summary>
        public bool IsBevel
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return Data[12] > 0;
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Data[6] > 0;
                }

                return false;
            }
            set
            {
                if (MapType == MapType.Vindictus)
                {
                    Data[12] = (byte)(value ? 1 : 0);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[6] = (byte)(value ? 1 : 0);
                }
            }
        }

        /// <summary>
        /// Is this <see cref="BrushSide"/> thin?
        /// </summary>
        public bool IsThin
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source) && MapType != MapType.Vindictus)
                {
                    return Data[7] > 0;
                }

                return false;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source) && MapType != MapType.Vindictus)
                {
                    Data[7] = (byte)(value ? 1 : 0);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="BrushSide"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="BrushSide"/> came from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public BrushSide(byte[] data, ILump parent)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Parent = parent;
        }

        /// <summary>
        /// Creates a new <see cref="BrushSide"/> by copying the fields in <paramref name="source"/>, using
        /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
        /// to use when creating the new <see cref="BrushSide"/>.
        /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
        /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
        /// </summary>
        /// <param name="source">The <see cref="BrushSide"/> to copy.</param>
        /// <param name="parent">
        /// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="BrushSide"/>.
        /// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
        /// </param>
        public BrushSide(BrushSide source, ILump parent)
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

            PlaneIndex = source.PlaneIndex;
            TextureIndex = source.TextureIndex;
            FaceIndex = source.FaceIndex;
            DisplacementIndex = source.DisplacementIndex;
            IsBevel = source.IsBevel;
            IsThin = source.IsThin;
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Lump{BrushSide}"/>.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Lump{BrushSide}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
        public static Lump<BrushSide> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            return new Lump<BrushSide>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
        }

        /// <summary>
        /// Gets the length of this struct's data for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.
        /// </summary>
        /// <param name="mapType">The <see cref="LibBSP.MapType"/> of the BSP.</param>
        /// <param name="lumpVersion">The version number for the lump.</param>
        /// <returns>The length, in <c>byte</c>s, of this struct.</returns>
        /// <exception cref="ArgumentException">This struct is not valid or is not implemented for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.</exception>
        public static int GetStructLength(MapType type, int version = 0)
        {
            if (type == MapType.Vindictus)
            {
                return 16;
            }
            else if (type.IsSubtypeOf(MapType.MOHAA)
                || type == MapType.Raven)
            {
                return 12;
            }
            else if (type.IsSubtypeOf(MapType.Quake3)
                || type.IsSubtypeOf(MapType.Source)
                || type == MapType.SiN
                || type == MapType.Nightfire)
            {
                return 8;
            }
            else if (type.IsSubtypeOf(MapType.Quake2))
            {
                return 4;
            }

            throw new ArgumentException("Lump object " + MethodBase.GetCurrentMethod().DeclaringType.Name + " does not exist in map type " + type + " or has not been implemented.");
        }

        /// <summary>
        /// Gets the index for this lump in the BSP file for a specific map format.
        /// </summary>
        /// <param name="type">The map type.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
        public static int GetIndexForLump(MapType type)
        {
            if (type.IsSubtypeOf(MapType.Source))
            {
                return 19;
            }
            else if (type.IsSubtypeOf(MapType.Quake2))
            {
                return 15;
            }
            else if (type.IsSubtypeOf(MapType.STEF2))
            {
                return 12;
            }
            else if (type.IsSubtypeOf(MapType.MOHAA))
            {
                return 11;
            }
            else if (type == MapType.Nightfire)
            {
                return 16;
            }
            else if (type == MapType.CoD || type == MapType.CoDDemo)
            {
                return 3;
            }
            else if (type == MapType.CoD2
                || type == MapType.CoD4)
            {
                return 5;
            }
            else if (type.IsSubtypeOf(MapType.FAKK2))
            {
                return 10;
            }
            else if (type.IsSubtypeOf(MapType.Quake3))
            {
                return 9;
            }

            return -1;
        }
    }
}
