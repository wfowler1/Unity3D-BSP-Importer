#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;
using System.Reflection;

namespace LibBSP
{
#if UNITY
    using Color = UnityEngine.Color32;
    using Vector2 = UnityEngine.Vector2;
    using Vector3 = UnityEngine.Vector3;
#elif GODOT
    using Color = Godot.Color;
    using Vector2 = Godot.Vector2;
    using Vector3 = Godot.Vector3;
#elif NEOAXIS
    using Color = NeoAxis.ColorByte;
    using Vector2 = NeoAxis.Vector2F;
    using Vector3 = NeoAxis.Vector3F;
#else
    using Color = System.Drawing.Color;
    using Vector2 = System.Numerics.Vector2;
    using Vector3 = System.Numerics.Vector3;
#endif

    /// <summary>
    /// Contains all the information for a single Texture Data object.
    /// </summary>
    public struct TextureData : ILumpObject
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
        /// Gets or sets the reflectivity color of this <see cref="TextureData"/>.
        /// </summary>
        public Color Reflectivity
        {
            get
            {
                return ColorExtensions.FromArgb((int)(BitConverter.ToSingle(Data, 0) * 255), (int)(BitConverter.ToSingle(Data, 4) * 255), (int)(BitConverter.ToSingle(Data, 8) * 255), 255);
            }
            set
            {
                float r = value.R() / 255f;
                float g = value.G() / 255f;
                float b = value.B() / 255f;
                new Vector3(r, g, b).GetBytes().CopyTo(Data, 0);
            }
        }

        /// <summary>
        /// Gets the offset into <see cref="BSP.Textures"/> for the texture name for this <see cref="TextureData"/>.
        /// </summary>
        public uint TextureStringOffset
        {
            get
            {
                return (uint)Parent.Bsp.TextureTable[TextureStringOffsetIndex];
            }
        }

        /// <summary>
        /// Gets or sets the index into <see cref="BSP.TextureTable"/>, which is an offset into <see cref="BSP.Textures"/> for
        /// the texture name for this <see cref="TextureData"/>.
        /// </summary>
        public int TextureStringOffsetIndex
        {
            get
            {
                return BitConverter.ToInt32(Data, 12);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(Data, 12);
            }
        }

        /// <summary>
        /// Gets or sets the actual size of the <see cref="Texture"/> referenced by this <see cref="TextureData"/>.
        /// </summary>
        public Vector2 Size
        {
            get
            {
                return new Vector2(BitConverter.ToInt32(Data, 16), BitConverter.ToInt32(Data, 20));
            }
            set
            {
                int width = (int)value.X();
                int height = (int)value.Y();
                BitConverter.GetBytes(width).CopyTo(Data, 16);
                BitConverter.GetBytes(height).CopyTo(Data, 20);
            }
        }

        /// <summary>
        /// Gets or sets the internal size of the <see cref="Texture"/> referenced by this <see cref="TextureData"/>.
        /// </summary>
        public Vector2 ViewSize
        {
            get
            {
                return new Vector2(BitConverter.ToInt32(Data, 24), BitConverter.ToInt32(Data, 28));
            }
            set
            {
                int width = (int)value.X();
                int height = (int)value.Y();
                BitConverter.GetBytes(width).CopyTo(Data, 24);
                BitConverter.GetBytes(height).CopyTo(Data, 28);
            }
        }

        /// <summary>
        /// Creates a new <see cref="TextureData"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="TextureData"/> came from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public TextureData(byte[] data, ILump parent = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Parent = parent;
        }

        /// <summary>
        /// Creates a new <see cref="TextureData"/> by copying the fields in <paramref name="source"/>, using
        /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
        /// to use when creating the new <see cref="TextureData"/>.
        /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
        /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
        /// </summary>
        /// <param name="source">The <see cref="TextureData"/> to copy.</param>
        /// <param name="parent">
        /// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="TextureData"/>.
        /// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
        /// </param>
        public TextureData(TextureData source, ILump parent)
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

            Reflectivity = source.Reflectivity;
            TextureStringOffsetIndex = source.TextureStringOffsetIndex;
            Size = source.Size;
            ViewSize = source.ViewSize;
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Lump{TextureData}"/>.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Lump{TextureData}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
        public static Lump<TextureData> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            return new Lump<TextureData>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
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
            if (mapType == MapType.Titanfall)
            {
                return 36;
            }
            else if (mapType.IsSubtypeOf(MapType.Source))
            {
                return 32;
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
                return 2;
            }

            return -1;
        }

    }
}
