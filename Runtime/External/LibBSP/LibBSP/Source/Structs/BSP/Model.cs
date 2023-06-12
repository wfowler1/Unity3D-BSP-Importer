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
    /// A class containing all data needed for the models lump in any given BSP.
    /// </summary>
    public struct Model : ILumpObject
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
        /// Gets the head <see cref="Node"/> referenced by this <see cref="Model"/>.
        /// </summary>
        public Node HeadNode
        {
            get
            {
                return Parent.Bsp.Nodes[HeadNodeIndex];
            }
        }

        /// <summary>
        /// Gets or sets the index of the head <see cref="Node"/> used by this <see cref="Model"/>.
        /// </summary>
        [Index("Nodes")]
        public int HeadNodeIndex
        {
            get
            {
                if (MapType == MapType.DMoMaM)
                {
                    return BitConverter.ToInt32(Data, 40);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                      || MapType.IsSubtypeOf(MapType.Quake2)
                      || MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt32(Data, 36);
                }
                else if (MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 24);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.DMoMaM)
                {
                    bytes.CopyTo(Data, 40);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                      || MapType.IsSubtypeOf(MapType.Quake2)
                      || MapType.IsSubtypeOf(MapType.Source))
                {
                    bytes.CopyTo(Data, 36);
                }
                else if (MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 24);
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first head ClipNode used by this <see cref="Model"/>.
        /// </summary>
        public int HeadClipNode1Index
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return BitConverter.ToInt32(Data, 40);
                }
                else if (MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 28);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    bytes.CopyTo(Data, 40);
                }
                else if (MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 28);
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the second head ClipNode used by this <see cref="Model"/>.
        /// </summary>
        public int HeadClipNode2Index
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return BitConverter.ToInt32(Data, 44);
                }
                else if (MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 32);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    bytes.CopyTo(Data, 44);
                }
                else if (MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 32);
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the third head ClipNode used by this <see cref="Model"/>.
        /// </summary>
        public int HeadClipNode3Index
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return BitConverter.ToInt32(Data, 48);
                }
                else if (MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 36);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    bytes.CopyTo(Data, 48);
                }
                else if (MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 36);
                }
            }
        }

        /// <summary>
        /// Enumerates the <see cref="Leaf"/>s used by this <see cref="Model"/>.
        /// </summary>
        public IEnumerable<Leaf> Leaves
        {
            get
            {
                for (int i = 0; i < NumLeaves; ++i)
                {
                    yield return Parent.Bsp.Leaves[FirstLeafIndex + i];
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first <see cref="Leaf"/> used by this <see cref="Model"/>.
        /// </summary>
        [Index("Leaves")]
        public int FirstLeafIndex
        {
            get
            {
                if (MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 40);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 40);
                }
            }
        }

        /// <summary>
        /// Gets or sets the count of <see cref="Leaf"/> objects used by this <see cref="Model"/>.
        /// </summary>
        [Count("Leaves")]
        public int NumLeaves
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return BitConverter.ToInt32(Data, 52);
                }
                else if (MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 44);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    bytes.CopyTo(Data, 52);
                }
                else if (MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 44);
                }
            }
        }

        /// <summary>
        /// Enumerates the <see cref="Brush"/>es used by this <see cref="Model"/>.
        /// </summary>
        public IEnumerable<Brush> Brushes
        {
            get
            {
                for (int i = 0; i < NumBrushes; ++i)
                {
                    yield return Parent.Bsp.Brushes[FirstBrushIndex + i];
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first <see cref="Brush"/> used by this <see cref="Model"/>.
        /// </summary>
        [Index("Brushes")]
        public int FirstBrushIndex
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.CoD))
                {
                    return BitConverter.ToInt32(Data, 40);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3))
                {
                    return BitConverter.ToInt32(Data, 32);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.CoD))
                {
                    bytes.CopyTo(Data, 40);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3))
                {
                    bytes.CopyTo(Data, 32);
                }
            }
        }

        /// <summary>
        /// Gets or sets the count of <see cref="Brush"/> objects referenced by this <see cref="Leaf"/>.
        /// </summary>
        [Count("Brushes")]
        public int NumBrushes
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.CoD))
                {
                    return BitConverter.ToInt32(Data, 44);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3))
                {
                    return BitConverter.ToInt32(Data, 36);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.CoD))
                {
                    bytes.CopyTo(Data, 44);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3))
                {
                    bytes.CopyTo(Data, 36);
                }
            }
        }

        /// <summary>
        /// Enumerates the <see cref="Face"/>s used by this <see cref="Model"/>.
        /// </summary>
        public IEnumerable<Face> Faces
        {
            get
            {
                for (int i = 0; i < NumFaces; ++i)
                {
                    yield return Parent.Bsp.Faces[FirstFaceIndex + i];
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first <see cref="Face"/> used by this <see cref="Model"/>.
        /// </summary>
        [Index("Faces")]
        public int FirstFaceIndex
        {
            get
            {
                if (MapType == MapType.CoD4)
                {
                    return BitConverter.ToInt16(Data, 24);
                }
                else if (MapType == MapType.DMoMaM)
                {
                    return BitConverter.ToInt32(Data, 44);
                }
                else if (MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 48);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return BitConverter.ToInt32(Data, 56);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt32(Data, 40);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Titanfall)
                {
                    return BitConverter.ToInt32(Data, 24);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.CoD4)
                {
                    Data[24] = bytes[0];
                    Data[25] = bytes[1];
                }
                else if (MapType == MapType.DMoMaM)
                {
                    bytes.CopyTo(Data, 44);
                }
                else if (MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 48);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    bytes.CopyTo(Data, 56);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    bytes.CopyTo(Data, 40);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Titanfall)
                {
                    bytes.CopyTo(Data, 24);
                }
            }
        }

        /// <summary>
        /// Gets or sets the count of <see cref="Face"/> objects referenced by this <see cref="Leaf"/>.
        /// </summary>
        [Count("Faces")]
        public int NumFaces
        {
            get
            {
                if (MapType == MapType.CoD4)
                {
                    return BitConverter.ToInt16(Data, 28);
                }
                else if (MapType == MapType.DMoMaM)
                {
                    return BitConverter.ToInt32(Data, 48);
                }
                else if (MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 52);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return BitConverter.ToInt32(Data, 60);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt32(Data, 44);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Titanfall)
                {
                    return BitConverter.ToInt32(Data, 28);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.CoD4)
                {
                    Data[28] = bytes[0];
                    Data[29] = bytes[1];
                }
                else if (MapType == MapType.DMoMaM)
                {
                    bytes.CopyTo(Data, 48);
                }
                else if (MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 52);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    bytes.CopyTo(Data, 60);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    bytes.CopyTo(Data, 44);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Titanfall)
                {
                    bytes.CopyTo(Data, 28);
                }
            }
        }

        /// <summary>
        /// Enumerates the patch indices used by this <see cref="Model"/>.
        /// </summary>
        public IEnumerable<int> PatchIndices
        {
            get
            {
                for (int i = 0; i < NumPatchIndices; ++i)
                {
                    yield return (int)Parent.Bsp.PatchIndices[FirstPatchIndicesIndex + i];
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first patch index for this <see cref="Model"/>.
        /// </summary>
        [Index("PatchIndices")]
        public int FirstPatchIndicesIndex
        {
            get
            {
                if (MapType == MapType.CoD
                    || MapType == MapType.CoDDemo)
                {
                    return BitConverter.ToInt32(Data, 32);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.CoD
                    || MapType == MapType.CoDDemo)
                {
                    bytes.CopyTo(Data, 32);
                }
            }
        }

        /// <summary>
        /// Gets or sets the count of patch indices referenced by this <see cref="Model"/>.
        /// </summary>
        [Count("PatchIndices")]
        public int NumPatchIndices
        {
            get
            {
                if (MapType == MapType.CoD
                    || MapType == MapType.CoDDemo)
                {
                    return BitConverter.ToInt32(Data, 36);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.CoD
                    || MapType == MapType.CoDDemo)
                {
                    bytes.CopyTo(Data, 36);
                }
            }
        }

        /// <summary>
        /// Gets or sets the bounding box minimums for this model.
        /// </summary>
        public Vector3 Minimums
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Nightfire
                    || MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    return Vector3Extensions.ToVector3(Data);
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Nightfire
                    || MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    value.GetBytes().CopyTo(Data, 0);
                }
            }
        }

        /// <summary>
        /// Gets or sets the bounding box maximums for this model.
        /// </summary>
        public Vector3 Maximums
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Nightfire
                    || MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    return Vector3Extensions.ToVector3(Data, 12);
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Nightfire
                    || MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Titanfall)
                {
                    value.GetBytes().CopyTo(Data, 12);
                }
            }
        }

        /// <summary>
        /// Gets or sets the origin for this model.
        /// </summary>
        public Vector3 Origin
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return Vector3Extensions.ToVector3(Data, 24);
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    value.GetBytes().CopyTo(Data, 24);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Model"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="Model"/> came from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public Model(byte[] data, ILump parent = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Parent = parent;
        }

        /// <summary>
        /// Creates a new <see cref="Model"/> by copying the fields in <paramref name="source"/>, using
        /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
        /// to use when creating the new <see cref="Model"/>.
        /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
        /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
        /// </summary>
        /// <param name="source">The <see cref="Model"/> to copy.</param>
        /// <param name="parent">
        /// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="Model"/>.
        /// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
        /// </param>
        public Model(Model source, ILump parent = null)
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

            HeadNodeIndex = source.HeadNodeIndex;
            HeadClipNode1Index = source.HeadClipNode1Index;
            HeadClipNode2Index = source.HeadClipNode2Index;
            HeadClipNode3Index = source.HeadClipNode3Index;
            FirstLeafIndex = source.FirstLeafIndex;
            NumLeaves = source.NumLeaves;
            FirstBrushIndex = source.FirstBrushIndex;
            NumBrushes = source.NumBrushes;
            FirstFaceIndex = source.FirstFaceIndex;
            NumFaces = source.NumFaces;
            FirstPatchIndicesIndex = source.FirstPatchIndicesIndex;
            NumPatchIndices = source.NumPatchIndices;
            Minimums = source.Minimums;
            Maximums = source.Maximums;
            Origin = source.Origin;
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Lump{Model}"/>.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Lump{Model}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
        public static Lump<Model> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            return new Lump<Model>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
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
                return 52;
            }
            else if (mapType == MapType.Titanfall)
            {
                return 32;
            }
            else if (mapType.IsSubtypeOf(MapType.Quake2)
                || mapType.IsSubtypeOf(MapType.CoD)
                || mapType.IsSubtypeOf(MapType.Source))
            {
                return 48;
            }
            else if (mapType.IsSubtypeOf(MapType.Quake))
            {
                return 64;
            }
            else if (mapType.IsSubtypeOf(MapType.Quake3))
            {
                return 40;
            }
            else if (mapType == MapType.Nightfire)
            {
                return 56;
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
                || type.IsSubtypeOf(MapType.Source)
                || type == MapType.Nightfire
                || type == MapType.Titanfall
                || type == MapType.MOHAADemo)
            {
                return 14;
            }
            else if (type.IsSubtypeOf(MapType.MOHAA)
                || type.IsSubtypeOf(MapType.FAKK2)
                || type.IsSubtypeOf(MapType.Quake2))
            {
                return 13;
            }
            else if (type.IsSubtypeOf(MapType.STEF2))
            {
                return 15;
            }
            else if (type == MapType.CoD
                || type == MapType.CoDDemo)
            {
                return 27;
            }
            else if (type == MapType.CoD2)
            {
                return 35;
            }
            else if (type == MapType.CoD4)
            {
                return 37;
            }
            else if (type.IsSubtypeOf(MapType.Quake3))
            {
                return 7;
            }

            return -1;
        }

    }
}
