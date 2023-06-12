#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;
using System.Reflection;

namespace LibBSP
{
#if UNITY
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;
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
    /// Holds all data for an Overlay from Source engine.
    /// </summary>
    public struct Overlay : ILumpObject
    {

        /// <summary>
        /// Number of <see cref="Face"/>s referenced by an <see cref="Overlay"/>.
        /// </summary>
        public const int NumOverlayFaces = 64;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public ILump Parent { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// <inheritdoc/>
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
        /// <inheritdoc/>
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
        /// Gets or sets the ID of this <see cref="Overlay"/>.
        /// </summary>
        public int ID
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt32(Data, 0);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    bytes.CopyTo(Data, 0);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="LibBSP.TextureInfo"/> referenced by this <see cref="Overlay"/>.
        /// </summary>
        public TextureInfo TextureInfo
        {
            get
            {
                return Parent.Bsp.TextureInfo[TextureInfoIndex];
            }
        }

        /// <summary>
        /// Gets or sets the index of the <see cref="LibBSP.TextureInfo"/> used by this <see cref="Overlay"/>.
        /// </summary>
        public int TextureInfoIndex
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 4);
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
                    bytes.CopyTo(Data, 4);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[4] = bytes[0];
                    Data[5] = bytes[1];
                }
            }
        }

        /// <summary>
        /// Gets or sets the face count and render order of this <see cref="Overlay"/>.
        /// </summary>
        public uint FaceCountAndRenderOrder
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return BitConverter.ToUInt32(Data, 8);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToUInt16(Data, 6);
                }

                return 0;
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
                    Data[6] = bytes[0];
                    Data[7] = bytes[1];
                }
            }
        }

        /// <summary>
        /// Gets or sets the face count of this <see cref="Overlay"/>.
        /// </summary>
        public uint FaceCount
        {
            get
            {
                return FaceCountAndRenderOrder & 0x00003FFF;
            }
            set
            {
                FaceCountAndRenderOrder = (FaceCountAndRenderOrder & 0x0000C000) | value;
            }
        }

        /// <summary>
        /// Gets or sets the render order of this <see cref="Overlay"/>.
        /// </summary>
        public uint RenderOrder
        {
            get
            {
                return FaceCountAndRenderOrder >> 14;
            }
            set
            {
                FaceCountAndRenderOrder = (FaceCountAndRenderOrder & 0x00003FFF) | (value << 14);
            }
        }

        /// <summary>
        /// Gets or sets the face index array for this <see cref="Overlay"/>.
        /// </summary>
        public int[] FaceIndices
        {
            get
            {
                int offset;
                if (MapType == MapType.Vindictus)
                {
                    offset = 12;
                }
                else
                {
                    offset = 8;
                }

                int[] result = new int[NumOverlayFaces];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = BitConverter.ToInt32(Data, offset);
                    offset += 4;
                }

                return result;
            }
            set
            {
                int offset;
                if (MapType == MapType.Vindictus)
                {
                    offset = 12;
                }
                else
                {
                    offset = 8;
                }

                for (int i = 0; i < value.Length && i < NumOverlayFaces; i++)
                {
                    byte[] bytes = BitConverter.GetBytes(value[i]);
                    bytes.CopyTo(Data, offset);
                    offset += 4;
                }
            }
        }

        /// <summary>
        /// Gets or sets the U for this <see cref="Overlay"/>.
        /// </summary>
        public Vector2 U
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return Vector2Extensions.ToVector2(Data, 268);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Vector2Extensions.ToVector2(Data, 264);
                }

                return new Vector2(0, 0);
            }
            set
            {
                if (MapType == MapType.Vindictus)
                {
                    value.GetBytes().CopyTo(Data, 268);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    value.GetBytes().CopyTo(Data, 264);
                }
            }
        }

        /// <summary>
        /// Gets or sets the V for this <see cref="Overlay"/>.
        /// </summary>
        public Vector2 V
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return Vector2Extensions.ToVector2(Data, 276);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Vector2Extensions.ToVector2(Data, 272);
                }

                return new Vector2(0, 0);
            }
            set
            {
                if (MapType == MapType.Vindictus)
                {
                    value.GetBytes().CopyTo(Data, 276);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    value.GetBytes().CopyTo(Data, 272);
                }
            }
        }

        /// <summary>
        /// Gets or sets the first UV point for this <see cref="Overlay"/>.
        /// </summary>
        public Vector3 UVPoint0
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return Vector3Extensions.ToVector3(Data, 284);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Vector3Extensions.ToVector3(Data, 280);
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType == MapType.Vindictus)
                {
                    value.GetBytes().CopyTo(Data, 284);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    value.GetBytes().CopyTo(Data, 280);
                }
            }
        }

        /// <summary>
        /// Gets or sets the second UV point for this <see cref="Overlay"/>.
        /// </summary>
        public Vector3 UVPoint1
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return Vector3Extensions.ToVector3(Data, 296);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Vector3Extensions.ToVector3(Data, 292);
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType == MapType.Vindictus)
                {
                    value.GetBytes().CopyTo(Data, 296);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    value.GetBytes().CopyTo(Data, 292);
                }
            }
        }

        /// <summary>
        /// Gets or sets the third UV point for this <see cref="Overlay"/>.
        /// </summary>
        public Vector3 UVPoint2
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return Vector3Extensions.ToVector3(Data, 308);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Vector3Extensions.ToVector3(Data, 304);
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType == MapType.Vindictus)
                {
                    value.GetBytes().CopyTo(Data, 308);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    value.GetBytes().CopyTo(Data, 304);
                }
            }
        }

        /// <summary>
        /// Gets or sets the fourth UV point for this <see cref="Overlay"/>.
        /// </summary>
        public Vector3 UVPoint3
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return Vector3Extensions.ToVector3(Data, 320);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Vector3Extensions.ToVector3(Data, 316);
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType == MapType.Vindictus)
                {
                    value.GetBytes().CopyTo(Data, 320);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    value.GetBytes().CopyTo(Data, 316);
                }
            }
        }

        /// <summary>
        /// Gets or sets the Origin for this <see cref="Overlay"/>.
        /// </summary>
        public Vector3 Origin
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return Vector3Extensions.ToVector3(Data, 332);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Vector3Extensions.ToVector3(Data, 328);
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType == MapType.Vindictus)
                {
                    value.GetBytes().CopyTo(Data, 332);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    value.GetBytes().CopyTo(Data, 328);
                }
            }
        }

        /// <summary>
        /// Gets or sets the basis normal for this <see cref="Overlay"/>.
        /// </summary>
        public Vector3 BasisNormal
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return Vector3Extensions.ToVector3(Data, 344);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Vector3Extensions.ToVector3(Data, 340);
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType == MapType.Vindictus)
                {
                    value.GetBytes().CopyTo(Data, 344);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    value.GetBytes().CopyTo(Data, 340);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Overlay"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="Overlay"/> came from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public Overlay(byte[] data, ILump parent = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Parent = parent;
        }

        /// <summary>
        /// Creates a new <see cref="Overlay"/> by copying the fields in <paramref name="source"/>, using
        /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
        /// to use when creating the new <see cref="Overlay"/>.
        /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
        /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
        /// </summary>
        /// <param name="source">The <see cref="Overlay"/> to copy.</param>
        /// <param name="parent">
        /// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="Overlay"/>.
        /// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
        /// </param>
        public Overlay(Overlay source, ILump parent)
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

            ID = source.ID;
            TextureInfoIndex = source.TextureInfoIndex;
            FaceCountAndRenderOrder = source.FaceCountAndRenderOrder;
            FaceIndices = source.FaceIndices;
            U = source.U;
            V = source.V;
            UVPoint0 = source.UVPoint0;
            UVPoint1 = source.UVPoint1;
            UVPoint2 = source.UVPoint2;
            UVPoint3 = source.UVPoint3;
            Origin = source.Origin;
            BasisNormal = source.BasisNormal;
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Lump{Overlay}"/>.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Lump{Overlay}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
        public static Lump<Overlay> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            return new Lump<Overlay>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
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
                return 356;
            }
            else if (mapType.IsSubtypeOf(MapType.Source))
            {
                return 352;
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
            if (type.IsSubtypeOf(MapType.Source))
            {
                return 45;
            }

            return -1;
        }

    }
}
