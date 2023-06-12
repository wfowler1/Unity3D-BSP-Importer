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
    using Plane = UnityEngine.Plane;
#elif GODOT
    using Vector2 = Godot.Vector2;
    using Vector3 = Godot.Vector3;
    using Plane = Godot.Plane;
#elif NEOAXIS
    using Vector2 = NeoAxis.Vector2F;
    using Vector3 = NeoAxis.Vector3F;
    using Plane = NeoAxis.PlaneF;
#else
    using Vector2 = System.Numerics.Vector2;
    using Vector3 = System.Numerics.Vector3;
    using Plane = System.Numerics.Plane;
#endif

    /// <summary>
    /// This struct contains the texture scaling information for certain formats.
    /// Some BSP formats lack this lump (or the information is contained in a
    /// different lump) so their cases will be left out.
    /// </summary>
    [Serializable]
    public struct TextureInfo : ILumpObject
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

        // No BSP format uses these so they are fields.
        public Vector2 scale;
        public float rotation;

        /// <summary>
        /// Gets or sets the U axis, used to calculate the U coordinates for each <see cref="Vertex"/>.
        /// </summary>
        public Vector3 UAxis
        {
            get
            {
                return Vector3Extensions.ToVector3(Data);
            }
            set
            {
                value.GetBytes().CopyTo(Data, 0);
            }
        }

        /// <summary>
        /// Gets or sets the V axis, used to calculate the V coordinates for each <see cref="Vertex"/>.
        /// </summary>
        public Vector3 VAxis
        {
            get
            {
                return Vector3Extensions.ToVector3(Data, 16);
            }
            set
            {
                value.GetBytes().CopyTo(Data, 16);
            }
        }

        /// <summary>
        /// Gets or sets the translation of the texture along the U and V axes.
        /// </summary>
        public Vector2 Translation
        {
            get
            {
                return new Vector2(BitConverter.ToSingle(Data, 12), BitConverter.ToSingle(Data, 28));
            }
            set
            {
                BitConverter.GetBytes(value.X()).CopyTo(Data, 12);
                BitConverter.GetBytes(value.Y()).CopyTo(Data, 28);
            }
        }

        /// <summary>
        /// Gets or sets the lightmap U axis.
        /// </summary>
        public Vector3 LightmapUAxis
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Vector3Extensions.ToVector3(Data, 32);
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    value.GetBytes().CopyTo(Data, 32);
                }
            }
        }

        /// <summary>
        /// Gets or sets the lightmap V axis.
        /// </summary>
        public Vector3 LightmapVAxis
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Vector3Extensions.ToVector3(Data, 48);
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    value.GetBytes().CopyTo(Data, 48);
                }
            }
        }

        /// <summary>
        /// Gets or sets the lightmap translation of along the U and V axes.
        /// </summary>
        public Vector2 LightmapTranslation
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return new Vector2(BitConverter.ToSingle(Data, 44), BitConverter.ToSingle(Data, 60));
                }

                return new Vector2(0, 0);
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    BitConverter.GetBytes(value.X()).CopyTo(Data, 44);
                    BitConverter.GetBytes(value.Y()).CopyTo(Data, 60);
                }
            }
        }

        /// <summary>
        /// Gets or sets the flags for this <see cref="TextureInfo"/>.
        /// </summary>
        public int Flags
        {
            get
            {
                if (MapType == MapType.DMoMaM)
                {
                    return BitConverter.ToInt32(Data, 88);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt32(Data, 64);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake) || MapType == MapType.Undefined)
                {
                    return BitConverter.ToInt32(Data, 36);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.DMoMaM)
                {
                    bytes.CopyTo(Data, 88);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    bytes.CopyTo(Data, 64);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake) || MapType == MapType.Undefined)
                {
                    bytes.CopyTo(Data, 36);
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the <see cref="Texture"/> (or <see cref="TextureData"/> in Source engine)
        /// used by this <see cref="TextureInfo"/>.
        /// </summary>
        /// <remarks>
        /// Using one field for data access in different lumps because of the similarity between Source and
        /// Quake II engines. Both engines reference texture data in this structure, Source just goes through
        /// a few more steps.
        /// </remarks>
        public int TextureIndex
        {
            get
            {
                if (MapType == MapType.DMoMaM)
                {
                    return BitConverter.ToInt32(Data, 92);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt32(Data, 68);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake) || MapType == MapType.Undefined)
                {
                    return BitConverter.ToInt32(Data, 32);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.DMoMaM)
                {
                    bytes.CopyTo(Data, 92);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    bytes.CopyTo(Data, 68);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake) || MapType == MapType.Undefined)
                {
                    bytes.CopyTo(Data, 32);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="TextureInfo"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="TextureInfo"/> came from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public TextureInfo(byte[] data, ILump parent = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Parent = parent;
            scale = new Vector2(1, 1);
            rotation = 0;
        }

        /// <summary>
        /// Creates a new <see cref="TextureInfo"/> object using the passed data.
        /// </summary>
        /// <param name="uAxis">The U texture axis.</param>
        /// <param name="vAxis">The V texture axis.</param>
        /// <param name="translation">Texture translation along both axes (in pixels).</param>
        /// <param name="scale">Texture scale along both axes.</param>
        /// <param name="flags">The flags for this <see cref="TextureInfo"/>.</param>
        /// <param name="texture">Index into the texture list for the texture this <see cref="TextureInfo"/> uses.</param>
        /// <param name="rotation">Rotation of the texutre axes.</param>
        public TextureInfo(Vector3 uAxis, Vector3 vAxis, Vector2 translation, Vector2 scale, int flags, int texture, float rotation)
        {
            Data = new byte[40];
            Parent = null;

            this.scale = scale;
            this.rotation = rotation;
            UAxis = uAxis;
            VAxis = vAxis;
            Translation = translation;
            Flags = flags;
            TextureIndex = texture;
        }

        /// <summary>
        /// Creates a new <see cref="TextureInfo"/> by copying the fields in <paramref name="source"/>, using
        /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
        /// to use when creating the new <see cref="TextureInfo"/>.
        /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
        /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
        /// </summary>
        /// <param name="source">The <see cref="TextureInfo"/> to copy.</param>
        /// <param name="parent">
        /// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="TextureInfo"/>.
        /// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
        /// </param>
        public TextureInfo(TextureInfo source, ILump parent)
        {
            Parent = parent;
            scale = source.scale;
            rotation = source.rotation;

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

            UAxis = source.UAxis;
            VAxis = source.VAxis;
            Translation = source.Translation;
            LightmapUAxis = source.LightmapUAxis;
            LightmapVAxis = source.LightmapVAxis;
            LightmapTranslation = source.LightmapTranslation;
            Flags = source.Flags;
            TextureIndex = source.TextureIndex;
        }

        /// <summary>
        /// Given a <see cref="Plane"/> <c>p</c>, return an optimal set of texture axes for it.
        /// </summary>
        /// <param name="p"><see cref="Plane"/> of the surface.</param>
        /// <returns>The best matching texture axes for the given <see cref="Plane"/>.</returns>
        public static Vector3[] TextureAxisFromPlane(Plane p)
        {
            int bestaxis = p.BestAxis();
            Vector3[] newAxes = new Vector3[2];
            newAxes[0] = PlaneExtensions.baseAxes[bestaxis * 3 + 1];
            newAxes[1] = PlaneExtensions.baseAxes[bestaxis * 3 + 2];
            return newAxes;
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Lump{TextureInfo}"/>.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Lump{TextureInfo}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
        public static Lump<TextureInfo> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            return new Lump<TextureInfo>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
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
            if (mapType == MapType.DMoMaM)
            {
                return 96;
            }
            else if (mapType.IsSubtypeOf(MapType.Source))
            {
                return 72;
            }
            else if (mapType == MapType.Nightfire)
            {
                return 32;
            }
            else if (mapType.IsSubtypeOf(MapType.Quake) || mapType == MapType.Undefined)
            {
                return 40;
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
            if (type.IsSubtypeOf(MapType.Quake)
                || type.IsSubtypeOf(MapType.Source))
            {
                return 6;
            }
            else if (type == MapType.Nightfire)
            {
                return 17;
            }

            return -1;
        }

    }
}
