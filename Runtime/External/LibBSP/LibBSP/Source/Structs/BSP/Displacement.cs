#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;
using System.Collections.Generic;
using System.Reflection;

namespace LibBSP
{
#if UNITY
    using Vector3 = UnityEngine.Vector3;
#elif GODOT
    using Vector3 = Godot.Vector3;
#elif NEOAXIS
    using Vector3 = NeoAxis.Vector3F;
#else
    using Vector3 = System.Numerics.Vector3;
#endif

    /// <summary>
    /// Holds all data for a Displacement from Source engine.
    /// </summary>
    public struct Displacement : ILumpObject
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
        /// Gets or sets the starting position of this <see cref="Displacement"/>.
        /// </summary>
        public Vector3 StartPosition
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Vector3Extensions.ToVector3(Data);
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    value.GetBytes().CopyTo(Data, 0);
                }
            }
        }

        /// <summary>
        /// Enumerates the <see cref="DisplacementVertex"/>es referenced by this <see cref="Displacement"/>.
        /// </summary>
        public IEnumerable<DisplacementVertex> Vertices
        {
            get
            {
                int numVertices = NumVertices;
                for (int i = 0; i < numVertices; ++i)
                {
                    yield return Parent.Bsp.DisplacementVertices[FirstVertexIndex + i];
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first <see cref="DisplacementVertex"/> used by this <see cref="Displacement"/>.
        /// </summary>
        [Index("DisplacementVertices")]
        public int FirstVertexIndex
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt32(Data, 12);
                }

                return -1;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    BitConverter.GetBytes(value).CopyTo(Data, 12);
                }
            }
        }

        /// <summary>
        /// Enumerates the flags for the triangles in this <see cref="Displacement"/>.
        /// </summary>
        public IEnumerable<ushort> Triangles
        {
            get
            {
                for (int i = 0; i < NumTriangles; ++i)
                {
                    yield return (ushort)Parent.Bsp.DisplacementTriangles[FirstTriangleIndex + i];
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first Displacement Triangle used by this <see cref="Displacement"/>.
        /// </summary>
        [Index("DisplacementTriangles")]
        public int FirstTriangleIndex
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt32(Data, 16);
                }

                return -1;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    BitConverter.GetBytes(value).CopyTo(Data, 16);
                }
            }
        }

        /// <summary>
        /// Gets or sets the power of this <see cref="Displacement"/>.
        /// </summary>
        public int Power
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt32(Data, 20);
                }

                return -1;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    BitConverter.GetBytes(value).CopyTo(Data, 20);
                }
            }
        }

        /// <summary>
        /// Gets the number of vertices this <see cref="Displacement"/> uses, based on <see cref="Power"/>.
        /// </summary>
        [Count("DisplacementVertices")]
        public int NumVertices
        {
            get
            {
                int numSideVerts = (int)Math.Pow(2, Power) + 1;
                return numSideVerts * numSideVerts;
            }
        }

        /// <summary>
        /// Gets the number of triangles this <see cref="Displacement"/> has, based on <see cref="Power"/>.
        /// </summary>
        [Count("DisplacementTriangles")]
        public int NumTriangles
        {
            get
            {
                int side = Power * Power;
                return 2 * side * side;
            }
        }

        /// <summary>
        /// Gets or sets the minimum allowed tesselation for this <see cref="Displacement"/>.
        /// </summary>
        public int MinimumTesselation
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt32(Data, 24);
                }

                return -1;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    BitConverter.GetBytes(value).CopyTo(Data, 24);
                }
            }
        }

        /// <summary>
        /// Gets or sets the lighting smoothing angle for this <see cref="Displacement"/>.
        /// </summary>
        public float SmoothingAngle
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToSingle(Data, 28);
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    BitConverter.GetBytes(value).CopyTo(Data, 28);
                }
            }
        }

        /// <summary>
        /// Gets or sets the contents flags for this <see cref="Displacement"/>.
        /// </summary>
        public int Contents
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt32(Data, 32);
                }

                return -1;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Source))
                {
                    BitConverter.GetBytes(value).CopyTo(Data, 32);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="LibBSP.Face"/> this <see cref="Displacement"/> was made from, for texturing and other information.
        /// </summary>
        public Face Face
        {
            get
            {
                return Parent.Bsp.Faces[FaceIndex];
            }
        }

        /// <summary>
        /// Gets or sets the index of the <see cref="LibBSP.Face"/> this <see cref="Displacement"/> was made from, for texturing and other information.
        /// </summary>
        public int FaceIndex
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 36);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToUInt16(Data, 36);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.Vindictus)
                {
                    bytes.CopyTo(Data, 36);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[36] = bytes[0];
                    Data[37] = bytes[1];
                }
            }
        }

        /// <summary>
        /// Get or sets the index of the lightmap alpha for this <see cref="Displacement"/>.
        /// </summary>
        public int LightmapAlphaStart
        {
            get
            {
                return BitConverter.ToInt32(Data, 40);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(Data, 40);
            }
        }

        /// <summary>
        /// Gets or sets the index of the first lightmap sample position used by this <see cref="Displacement"/>.
        /// </summary>
        public int LightmapSamplePositionStart
        {
            get
            {
                return BitConverter.ToInt32(Data, 44);
            }
            set
            {
                BitConverter.GetBytes(value).CopyTo(Data, 44);
            }
        }

        /// <summary>
        /// The <see cref="DisplacementNeighbor"/>s in this <see cref="Displacement"/>.
        /// </summary>
        public DisplacementNeighbor[] Neighbors { get; private set; }

        /// <summary>
        /// The <see cref="DisplacementCornerNeighbor"/>s in this <see cref="Displacement"/>.
        /// </summary>
        public DisplacementCornerNeighbor[] CornerNeighbors { get; private set; }

        /// <summary>
        /// Gets or sets the allowed vertices for this <see cref="Displacement"/>.
        /// </summary>
        public uint[] AllowedVertices
        {
            get
            {
                uint[] allowedVertices = new uint[10];
                int offset = -1;

                if (MapType == MapType.Vindictus)
                {
                    offset = 192;
                }
                else if (MapType == MapType.Source22)
                {
                    offset = 140;
                }
                else if (MapType == MapType.Source23)
                {
                    offset = 144;
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    offset = 136;
                }

                if (offset >= 0)
                {
                    for (int i = 0; i < 10; ++i)
                    {
                        allowedVertices[i] = BitConverter.ToUInt32(Data, offset + (i * 4));
                    }
                }
                return allowedVertices;
            }
            set
            {
                if (value.Length != 10)
                {
                    throw new ArgumentException("AllowedVerts array must have 10 elements.");
                }
                int offset = -1;

                if (MapType == MapType.Vindictus)
                {
                    offset = 192;
                }
                else if (MapType == MapType.Source22)
                {
                    offset = 140;
                }
                else if (MapType == MapType.Source23)
                {
                    offset = 144;
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    offset = 136;
                }

                if (offset >= 0)
                {
                    for (int i = 0; i < value.Length; ++i)
                    {
                        BitConverter.GetBytes(value[i]).CopyTo(Data, offset + (i * 4));
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Displacement"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="Displacement"/> came from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public Displacement(byte[] data, ILump parent = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Parent = parent;
            Neighbors = new DisplacementNeighbor[4];
            CornerNeighbors = new DisplacementCornerNeighbor[4];

            int neighborStructLength = DisplacementNeighbor.GetStructLength(MapType, LumpVersion);
            for (int i = 0; i < Neighbors.Length; ++i)
            {
                Neighbors[i] = new DisplacementNeighbor(this, 48 + (neighborStructLength * i));
            }
            int cornerNeighborStructLength = DisplacementCornerNeighbor.GetStructLength(MapType, LumpVersion);
            for (int i = 0; i < CornerNeighbors.Length; ++i)
            {
                CornerNeighbors[i] = new DisplacementCornerNeighbor(this, 48 + (neighborStructLength * Neighbors.Length) + (cornerNeighborStructLength * i));
            }
        }

        /// <summary>
        /// Creates a new <see cref="Displacement"/> by copying the fields in <paramref name="source"/>, using
        /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
        /// to use when creating the new <see cref="Displacement"/>.
        /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
        /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
        /// </summary>
        /// <param name="source">The <see cref="Displacement"/> to copy.</param>
        /// <param name="parent">
        /// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="Displacement"/>.
        /// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
        /// </param>
        public Displacement(Displacement source, ILump parent)
        {
            Parent = parent;
            Neighbors = new DisplacementNeighbor[4];
            CornerNeighbors = new DisplacementCornerNeighbor[4];

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

            StartPosition = source.StartPosition;
            FirstVertexIndex = source.FirstVertexIndex;
            FirstTriangleIndex = source.FirstTriangleIndex;
            Power = source.Power;
            MinimumTesselation = source.MinimumTesselation;
            SmoothingAngle = source.SmoothingAngle;
            Contents = source.Contents;
            FaceIndex = source.FaceIndex;
            LightmapAlphaStart = source.LightmapAlphaStart;
            LightmapSamplePositionStart = source.LightmapSamplePositionStart;
            int neighborStructLength = DisplacementNeighbor.GetStructLength(MapType, LumpVersion);
            for (int i = 0; i < Neighbors.Length; ++i)
            {
                Neighbors[i] = new DisplacementNeighbor(source.Neighbors[i], this, 48 + (neighborStructLength * i));
            }
            int cornerNeighborStructLength = DisplacementCornerNeighbor.GetStructLength(MapType, LumpVersion);
            for (int i = 0; i < CornerNeighbors.Length; ++i)
            {
                CornerNeighbors[i] = new DisplacementCornerNeighbor(source.CornerNeighbors[i], this, 48 + (neighborStructLength * Neighbors.Length) + (cornerNeighborStructLength * i));
            }
            AllowedVertices = source.AllowedVertices;
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Lump{Displacement}"/>.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Lump{Displacement}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
        public static Lump<Displacement> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            return new Lump<Displacement>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
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
            if (mapType == MapType.Source23)
            {
                return 184;
            }
            else if (mapType == MapType.Vindictus)
            {
                return 232;
            }
            else if (mapType.IsSubtypeOf(MapType.Source))
            {
                return 176;
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
                return 26;
            }

            return -1;
        }

        /// <summary>
        /// Struct providing access to the fields in a <see cref="Displacement"/>'s neighbor data.
        /// </summary>
        public struct DisplacementNeighbor
        {

            /// <summary>
            /// The <see cref="ILumpObject"/> this <see cref="DisplacementNeighbor"/> is a part of.
            /// </summary>
            public ILumpObject parent;

            /// <summary>
            /// The offset within the <see cref="parent"/> where this <see cref="DisplacementNeighbor"/> starts from.
            /// </summary>
            private int offset;

            /// <summary>
            /// The <see cref="DisplacementSubNeighbor"/>s in this <see cref="DisplacementNeighbor"/>.
            /// </summary>
            public DisplacementSubNeighbor[] Subneighbors { get; private set; }

            /// <summary>
            /// Constructs a new <see cref="DisplacementNeighbor"/> using the <paramref name="parent"/>'s 
            /// </summary>
            /// <param name="parent">The parent <see cref="ILumpObject"/> for this <see cref="DisplacementNeighbor"/>.</param>
            /// <param name="offset">
            /// The offset within <paramref name="parent"/>'s <see cref="ILumpObject.Data"/> where this
            /// <see cref="DisplacementNeighbor"/>'s data starts.
            /// </param>
            public DisplacementNeighbor(ILumpObject parent, int offset)
            {
                this.parent = parent;
                this.offset = offset;
                Subneighbors = new DisplacementSubNeighbor[] {
                    new DisplacementSubNeighbor(parent, offset),
                    new DisplacementSubNeighbor(parent, offset + GetStructLength(parent.MapType, parent.LumpVersion))
                };
            }

            /// <summary>
            /// Creates a new <see cref="DisplacementNeighbor"/> by copying the fields in <paramref name="source"/>, using
            /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
            /// to use when creating the new <see cref="DisplacementNeighbor"/>.
            /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
            /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
            /// </summary>
            /// <param name="source">The <see cref="DisplacementNeighbor"/> to copy.</param>
            /// <param name="parent">
            /// The <see cref="ILumpObject"/> to use as the <see cref="Parent"/> of the new <see cref="DisplacementNeighbor"/>.
            /// </param>
            /// <param name="offset">The offset within the <see cref="parent"/> where this <see cref="DisplacementNeighbor"/> starts from.</param>
            public DisplacementNeighbor(DisplacementNeighbor source, ILumpObject parent, int offset)
            {
                this.parent = parent;
                this.offset = offset;
                Subneighbors = new DisplacementSubNeighbor[] {
                    new DisplacementSubNeighbor(source.Subneighbors[0], parent, this.offset),
                    new DisplacementSubNeighbor(source.Subneighbors[1], parent, this.offset + GetStructLength(parent.MapType, parent.LumpVersion))
                };
            }

            /// <summary>
            /// Gets the length of this struct's data for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.
            /// </summary>
            /// <param name="mapType">The <see cref="LibBSP.MapType"/> of the BSP.</param>
            /// <param name="lumpVersion">The version number for the lump.</param>
            /// <returns>The length, in <c>byte</c>s, of this struct.</returns>
            public static int GetStructLength(MapType mapType, int lumpVersion = 0)
            {
                return 2 * DisplacementSubNeighbor.GetStructLength(mapType, lumpVersion);
            }

            /// <summary>
            /// Struct providing access to the fields in a <see cref="Displacement"/>'s subneighbor data.
            /// </summary>
            public struct DisplacementSubNeighbor
            {

                /// <summary>
                /// The <see cref="ILumpObject"/> this <see cref="DisplacementSubNeighbor"/> is a part of.
                /// </summary>
                public ILumpObject parent;

                /// <summary>
                /// The offset within the <see cref="parent"/> where this <see cref="DisplacementSubNeighbor"/> starts from.
                /// </summary>
                public int offset;

                /// <summary>
                /// The index of the neighboring <see cref="Displacement"/>.
                /// </summary>
                public int NeighborIndex
                {
                    get
                    {
                        if (parent.MapType.IsSubtypeOf(MapType.Source))
                        {
                            return BitConverter.ToInt16(parent.Data, offset);
                        }

                        return -1;
                    }
                    set
                    {
                        if (parent.MapType.IsSubtypeOf(MapType.Source))
                        {
                            byte[] bytes = BitConverter.GetBytes(value);
                            parent.Data[offset] = bytes[0];
                            parent.Data[offset + 1] = bytes[1];
                        }
                    }
                }

                /// <summary>
                /// The orientation of the neighboring <see cref="Displacement"/>.
                /// </summary>
                public int Orientation
                {
                    get
                    {
                        if (parent.MapType == MapType.Vindictus)
                        {
                            return BitConverter.ToInt16(parent.Data, offset + 2);
                        }
                        else if (parent.MapType.IsSubtypeOf(MapType.Source))
                        {
                            return parent.Data[offset + 2];
                        }

                        return -1;
                    }
                    set
                    {
                        byte[] bytes = BitConverter.GetBytes(value);

                        if (parent.MapType == MapType.Vindictus)
                        {
                            parent.Data[offset + 2] = bytes[0];
                            parent.Data[offset + 3] = bytes[1];
                        }
                        else if (parent.MapType.IsSubtypeOf(MapType.Source))
                        {
                            parent.Data[offset + 2] = bytes[0];
                        }
                    }
                }

                /// <summary>
                /// The span of the neighboring <see cref="Displacement"/>.
                /// </summary>
                public int Span
                {
                    get
                    {
                        if (parent.MapType == MapType.Vindictus)
                        {
                            return BitConverter.ToInt16(parent.Data, offset + 4);
                        }
                        else if (parent.MapType.IsSubtypeOf(MapType.Source))
                        {
                            return parent.Data[offset + 3];
                        }

                        return -1;
                    }
                    set
                    {
                        byte[] bytes = BitConverter.GetBytes(value);

                        if (parent.MapType == MapType.Vindictus)
                        {
                            parent.Data[offset + 4] = bytes[0];
                            parent.Data[offset + 5] = bytes[1];
                        }
                        else if (parent.MapType.IsSubtypeOf(MapType.Source))
                        {
                            parent.Data[offset + 3] = bytes[0];
                        }
                    }
                }

                /// <summary>
                /// The neighbor span of the neighboring <see cref="Displacement"/>.
                /// </summary>
                public int NeighborSpan
                {
                    get
                    {
                        if (parent.MapType == MapType.Vindictus)
                        {
                            return BitConverter.ToInt16(parent.Data, offset + 6);
                        }
                        else if (parent.MapType.IsSubtypeOf(MapType.Source))
                        {
                            return parent.Data[offset + 4];
                        }

                        return -1;
                    }
                    set
                    {
                        byte[] bytes = BitConverter.GetBytes(value);

                        if (parent.MapType == MapType.Vindictus)
                        {
                            parent.Data[offset + 6] = bytes[0];
                            parent.Data[offset + 7] = bytes[1];
                        }
                        else if (parent.MapType.IsSubtypeOf(MapType.Source))
                        {
                            parent.Data[offset + 4] = bytes[0];
                        }
                    }
                }

                /// <summary>
                /// Constructs a new <see cref="DisplacementSubNeighbor"/>
                /// </summary>
                /// <param name="parent">The parent <see cref="ILumpObject"/> for this <see cref="DisplacementNeighbor"/>.</param>
                /// <param name="offset">
                /// The offset within <paramref name="parent"/>'s <see cref="ILumpObject.Data"/> where this
                /// <see cref="DisplacementSubNeighbor"/>'s data starts.
                /// </param>
                public DisplacementSubNeighbor(ILumpObject parent, int offset)
                {
                    this.parent = parent;
                    this.offset = offset;
                }

                /// <summary>
                /// Creates a new <see cref="DisplacementSubNeighbor"/> by copying the fields in <paramref name="source"/>, using
                /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
                /// to use when creating the new <see cref="DisplacementSubNeighbor"/>.
                /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
                /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
                /// </summary>
                /// <param name="source">The <see cref="DisplacementSubNeighbor"/> to copy.</param>
                /// <param name="parent">
                /// The <see cref="ILumpObject"/> to use as the <see cref="Parent"/> of the new <see cref="DisplacementSubNeighbor"/>.
                /// </param>
                /// <param name="offset">The offset within the <see cref="parent"/> where this <see cref="DisplacementSubNeighbor"/> starts from.</param>
                public DisplacementSubNeighbor(DisplacementSubNeighbor source, ILumpObject parent, int offset)
                {
                    this.parent = parent;
                    this.offset = offset;

                    NeighborIndex = source.NeighborIndex;
                    Orientation = source.Orientation;
                    Span = source.Span;
                    NeighborSpan = source.NeighborSpan;
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
                    else if (mapType.IsSubtypeOf(MapType.Source))
                    {
                        return 6;
                    }

                    throw new ArgumentException("Object " + MethodBase.GetCurrentMethod().DeclaringType.Name + " does not exist in map type " + mapType + " or has not been implemented.");
                }

            }

        }

        /// <summary>
        /// Struct providing access to the fields in a <see cref="Displacement"/>'s corner neighbor data.
        /// </summary>
        public struct DisplacementCornerNeighbor
        {

            /// <summary>
            /// The <see cref="ILumpObject"/> this <see cref="DisplacementCornerNeighbor"/> is a part of.
            /// </summary>
            public ILumpObject parent;

            /// <summary>
            /// The offset within the <see cref="parent"/> where this <see cref="DisplacementCornerNeighbor"/> starts from.
            /// </summary>
            public int offset;

            /// <summary>
            /// The indices of the neighboring <see cref="Displacement"/>s.
            /// </summary>
            public int[] NeighborIndices
            {
                get
                {
                    int[] neighborIndices = new int[10];

                    if (parent.MapType == MapType.Vindictus)
                    {
                        for (int i = 0; i < 4; ++i)
                        {
                            neighborIndices[i] = BitConverter.ToInt32(parent.Data, offset + (i * 4));
                        }
                    }
                    else if (parent.MapType.IsSubtypeOf(MapType.Source))
                    {
                        for (int i = 0; i < 4; ++i)
                        {
                            neighborIndices[i] = BitConverter.ToInt16(parent.Data, offset + (i * 2));
                        }
                    }

                    return neighborIndices;
                }
                set
                {
                    if (value.Length != 4)
                    {
                        throw new ArgumentException("NeighborIndices array must have 4 elements.");
                    }

                    if (parent.MapType == MapType.Vindictus)
                    {
                        for (int i = 0; i < value.Length; ++i)
                        {
                            BitConverter.GetBytes(value[i]).CopyTo(parent.Data, offset + (i * 4));
                        }
                    }
                    else if (parent.MapType.IsSubtypeOf(MapType.Source))
                    {
                        for (int i = 0; i < value.Length; ++i)
                        {
                            byte[] bytes = BitConverter.GetBytes(value[i]);
                            parent.Data[offset + (i * 2)] = bytes[0];
                            parent.Data[offset + (i * 2) + 1] = bytes[1];
                        }
                    }
                }
            }

            /// <summary>
            /// The amount of neighboring <see cref="Displacement"/>s.
            /// </summary>
            public int NumNeighbors
            {
                get
                {
                    if (parent.MapType == MapType.Vindictus)
                    {
                        return BitConverter.ToInt32(parent.Data, offset + 16);
                    }
                    else if (parent.MapType.IsSubtypeOf(MapType.Source))
                    {
                        return parent.Data[offset + 8];
                    }

                    return -1;
                }
                set
                {
                    byte[] bytes = BitConverter.GetBytes(value);

                    if (parent.MapType == MapType.Vindictus)
                    {
                        BitConverter.GetBytes(value).CopyTo(parent.Data, offset + 16);
                    }
                    else if (parent.MapType.IsSubtypeOf(MapType.Source))
                    {
                        parent.Data[offset + 8] = bytes[0];
                    }
                }
            }

            /// <summary>
            /// Constructs a new <see cref="DisplacementCornerNeighbor"/>
            /// </summary>
            /// <param name="parent">The parent <see cref="ILumpObject"/> for this <see cref="DisplacementCornerNeighbor"/>.</param>
            /// <param name="offset">
            /// The offset within <paramref name="parent"/>'s <see cref="ILumpObject.Data"/> where this
            /// <see cref="DisplacementCornerNeighbor"/>'s data starts.
            /// </param>
            public DisplacementCornerNeighbor(ILumpObject parent, int offset)
            {
                this.parent = parent;
                this.offset = offset;
            }

            /// <summary>
            /// Creates a new <see cref="DisplacementCornerNeighbor"/> by copying the fields in <paramref name="source"/>, using
            /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
            /// to use when creating the new <see cref="DisplacementCornerNeighbor"/>.
            /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
            /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
            /// </summary>
            /// <param name="source">The <see cref="DisplacementCornerNeighbor"/> to copy.</param>
            /// <param name="parent">
            /// The <see cref="ILumpObject"/> to use as the <see cref="Parent"/> of the new <see cref="DisplacementCornerNeighbor"/>.
            /// </param>
            /// <param name="offset">The offset within the <see cref="parent"/> where this <see cref="DisplacementCornerNeighbor"/> starts from.</param>
            public DisplacementCornerNeighbor(DisplacementCornerNeighbor source, ILumpObject parent, int offset)
            {
                this.parent = parent;
                this.offset = offset;

                NeighborIndices = source.NeighborIndices;
                NumNeighbors = source.NumNeighbors;
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
                    return 20;
                }
                else if (mapType.IsSubtypeOf(MapType.Source))
                {
                    return 10;
                }

                throw new ArgumentException("Object " + MethodBase.GetCurrentMethod().DeclaringType.Name + " does not exist in map type " + mapType + " or has not been implemented.");
            }

        }

    }
}
