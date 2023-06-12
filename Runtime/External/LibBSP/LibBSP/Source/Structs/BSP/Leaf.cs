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
    /// Holds data for a leaf structure in a BSP map.
    /// </summary>
    public struct Leaf : ILumpObject
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
        /// Gets or sets the contents flags for this <see cref="Leaf"/>.
        /// </summary>
        public int Contents
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 0);
                }

                return -1;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source)
                    || MapType == MapType.Nightfire)
                {
                    BitConverter.GetBytes(value).CopyTo(Data, 0);
                }
            }
        }

        /// <summary>
        /// Gets or sets the offset or cluster in the visibility data used by this <see cref="Leaf"/>.
        /// </summary>
        public int Visibility
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType == MapType.Nightfire
                    || MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 4);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToInt16(Data, 4);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3))
                {
                    return BitConverter.ToInt32(Data, 0);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType == MapType.Nightfire
                    || MapType == MapType.Vindictus)
                {
                    bytes.CopyTo(Data, 4);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[4] = bytes[0];
                    Data[5] = bytes[1];
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3))
                {
                    bytes.CopyTo(Data, 0);
                }
            }
        }

        /// <summary>
        /// Gets or sets the area of this <see cref="Leaf"/> for the AreaPortals system.
        /// </summary>
        public int Area
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake2))
                {
                    return BitConverter.ToInt16(Data, 6);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3) && MapType != MapType.CoD4)
                {
                    return BitConverter.ToInt32(Data, 4);
                }
                else if (MapType.IsSubtypeOf(MapType.Source) && MapType != MapType.Vindictus)
                {
                    return Data[6];
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.Quake2))
                {
                    Data[6] = bytes[0];
                    Data[7] = bytes[1];
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3) && MapType != MapType.CoD4)
                {
                    bytes.CopyTo(Data, 4);
                }
                else if (MapType.IsSubtypeOf(MapType.Source) && MapType != MapType.Vindictus)
                {
                    Data[6] = bytes[0];
                }
            }
        }

        /// <summary>
        /// Gets or sets the flags on this <see cref="Leaf"/>.
        /// </summary>
        public int Flags
        {
            get
            {
                if (MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 8);
                }
                else if (MapType.IsSubtypeOf(MapType.Source))
                {
                    return Data[7];
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
                    Data[7] = bytes[0];
                }
            }
        }

        /// <summary>
        /// Gets or sets the bounding box minimums for this leaf.
        /// </summary>
        public Vector3 Minimums
        {
            get
            {
                if (MapType == MapType.SoF)
                {
                    return new Vector3(BitConverter.ToInt16(Data, 10), BitConverter.ToInt16(Data, 12), BitConverter.ToInt16(Data, 14));
                }
                else if (MapType == MapType.Vindictus)
                {
                    return new Vector3(BitConverter.ToInt32(Data, 12), BitConverter.ToInt32(Data, 16), BitConverter.ToInt32(Data, 20));
                }
                else if (MapType == MapType.Nightfire)
                {
                    return Vector3Extensions.ToVector3(Data, 8);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return new Vector3(BitConverter.ToInt16(Data, 8), BitConverter.ToInt16(Data, 10), BitConverter.ToInt16(Data, 12));
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD))
                {
                    return new Vector3(BitConverter.ToInt32(Data, 8), BitConverter.ToInt32(Data, 12), BitConverter.ToInt32(Data, 16));
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType == MapType.SoF)
                {
                    BitConverter.GetBytes((short)value.X()).CopyTo(Data, 10);
                    BitConverter.GetBytes((short)value.Y()).CopyTo(Data, 12);
                    BitConverter.GetBytes((short)value.Z()).CopyTo(Data, 14);
                }
                else if (MapType == MapType.Vindictus)
                {
                    BitConverter.GetBytes((int)value.X()).CopyTo(Data, 12);
                    BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 16);
                    BitConverter.GetBytes((int)value.Z()).CopyTo(Data, 20);
                }
                else if (MapType == MapType.Nightfire)
                {
                    value.GetBytes().CopyTo(Data, 8);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    BitConverter.GetBytes((short)value.X()).CopyTo(Data, 8);
                    BitConverter.GetBytes((short)value.Y()).CopyTo(Data, 10);
                    BitConverter.GetBytes((short)value.Z()).CopyTo(Data, 12);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD))
                {
                    BitConverter.GetBytes((int)value.X()).CopyTo(Data, 8);
                    BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 12);
                    BitConverter.GetBytes((int)value.Z()).CopyTo(Data, 16);
                }
            }
        }

        /// <summary>
        /// Gets or sets the bounding box maximums for this leaf.
        /// </summary>
        public Vector3 Maximums
        {
            get
            {
                if (MapType == MapType.SoF)
                {
                    return new Vector3(BitConverter.ToInt16(Data, 16), BitConverter.ToInt16(Data, 18), BitConverter.ToInt16(Data, 20));
                }
                else if (MapType == MapType.Vindictus)
                {
                    return new Vector3(BitConverter.ToInt32(Data, 24), BitConverter.ToInt32(Data, 28), BitConverter.ToInt32(Data, 32));
                }
                else if (MapType == MapType.Nightfire)
                {
                    return Vector3Extensions.ToVector3(Data, 20);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return new Vector3(BitConverter.ToInt16(Data, 14), BitConverter.ToInt16(Data, 16), BitConverter.ToInt16(Data, 18));
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD))
                {
                    return new Vector3(BitConverter.ToInt32(Data, 20), BitConverter.ToInt32(Data, 24), BitConverter.ToInt32(Data, 28));
                }

                return new Vector3(0, 0, 0);
            }
            set
            {
                if (MapType == MapType.SoF)
                {
                    BitConverter.GetBytes((short)value.X()).CopyTo(Data, 16);
                    BitConverter.GetBytes((short)value.Y()).CopyTo(Data, 18);
                    BitConverter.GetBytes((short)value.Z()).CopyTo(Data, 20);
                }
                else if (MapType == MapType.Vindictus)
                {
                    BitConverter.GetBytes((int)value.X()).CopyTo(Data, 24);
                    BitConverter.GetBytes((int)value.Y()).CopyTo(Data, 28);
                    BitConverter.GetBytes((int)value.Z()).CopyTo(Data, 32);
                }
                else if (MapType == MapType.Nightfire)
                {
                    value.GetBytes().CopyTo(Data, 20);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    BitConverter.GetBytes((short)value.X()).CopyTo(Data, 14);
                    BitConverter.GetBytes((short)value.Y()).CopyTo(Data, 16);
                    BitConverter.GetBytes((short)value.Z()).CopyTo(Data, 18);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD))
                {
                    BitConverter.GetBytes((short)value.X()).CopyTo(Data, 20);
                    BitConverter.GetBytes((short)value.Y()).CopyTo(Data, 24);
                    BitConverter.GetBytes((short)value.Z()).CopyTo(Data, 28);
                }
            }
        }

        /// <summary>
        /// Enumerates the brush indices used by this <see cref="Leaf"/>.
        /// </summary>
        public IEnumerable<int> MarkBrushes
        {
            get
            {
                for (int i = 0; i < NumMarkBrushIndices; ++i)
                {
                    yield return (int)Parent.Bsp.LeafBrushes[FirstMarkBrushIndex + i];
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first mark brush reference for this <see cref="Leaf"/>.
        /// </summary>
        [Index("LeafBrushes")]
        public int FirstMarkBrushIndex
        {
            get
            {
                if (MapType == MapType.CoD4)
                {
                    return BitConverter.ToInt32(Data, 12);
                }
                else if (MapType.IsSubtypeOf(MapType.CoD))
                {
                    return BitConverter.ToInt32(Data, 16);
                }
                else if (MapType == MapType.SoF)
                {
                    return BitConverter.ToUInt16(Data, 26);
                }
                else if (MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 44);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToUInt16(Data, 24);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 40);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.CoD4)
                {
                    bytes.CopyTo(Data, 12);
                }
                else if (MapType.IsSubtypeOf(MapType.CoD))
                {
                    bytes.CopyTo(Data, 16);
                }
                else if (MapType == MapType.SoF)
                {
                    Data[26] = bytes[0];
                    Data[27] = bytes[1];
                }
                else if (MapType == MapType.Vindictus)
                {
                    bytes.CopyTo(Data, 44);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[24] = bytes[0];
                    Data[25] = bytes[1];
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 40);
                }
            }
        }

        /// <summary>
        /// Gets or sets the count of mark brush references for this <see cref="Leaf"/>.
        /// </summary>
        [Count("LeafBrushes")]
        public int NumMarkBrushIndices
        {
            get
            {
                if (MapType == MapType.CoD4)
                {
                    return BitConverter.ToInt32(Data, 16);
                }
                else if (MapType.IsSubtypeOf(MapType.CoD))
                {
                    return BitConverter.ToInt32(Data, 20);
                }
                else if (MapType == MapType.SoF)
                {
                    return BitConverter.ToUInt16(Data, 28);
                }
                else if (MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 48);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 44);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToUInt16(Data, 26);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.CoD4)
                {
                    bytes.CopyTo(Data, 16);
                }
                else if (MapType.IsSubtypeOf(MapType.CoD))
                {
                    bytes.CopyTo(Data, 20);
                }
                else if (MapType == MapType.SoF)
                {
                    Data[28] = bytes[0];
                    Data[29] = bytes[1];
                }
                else if (MapType == MapType.Vindictus)
                {
                    bytes.CopyTo(Data, 48);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3)
                    || MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 44);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[26] = bytes[0];
                    Data[27] = bytes[1];
                }
            }
        }

        /// <summary>
        /// Enumerates the <see cref="Face"/> indices used by this <see cref="Leaf"/>.
        /// </summary>
        public IEnumerable<int> MarkFaces
        {
            get
            {
                for (int i = 0; i < NumMarkFaceIndices; ++i)
                {
                    yield return (int)Parent.Bsp.LeafFaces[FirstMarkFaceIndex + i];
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first mark face reference for this <see cref="Leaf"/>.
        /// </summary>
        [Index("LeafFaces")]
        public int FirstMarkFaceIndex
        {
            get
            {
                if (MapType == MapType.SoF)
                {
                    return BitConverter.ToUInt16(Data, 22);
                }
                else if (MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 36);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)
                    || MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 32);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToUInt16(Data, 20);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.SoF)
                {
                    Data[22] = bytes[0];
                    Data[23] = bytes[1];
                }
                else if (MapType == MapType.Vindictus)
                {
                    bytes.CopyTo(Data, 36);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)
                    || MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 32);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[20] = bytes[0];
                    Data[21] = bytes[1];
                }
            }
        }

        /// <summary>
        /// Gets or sets the count of mark face references for this <see cref="Leaf"/>.
        /// </summary>
        [Count("LeafFaces")]
        public int NumMarkFaceIndices
        {
            get
            {
                if (MapType == MapType.SoF)
                {
                    return BitConverter.ToUInt16(Data, 24);
                }
                else if (MapType == MapType.Vindictus)
                {
                    return BitConverter.ToInt32(Data, 40);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)
                    || MapType == MapType.Nightfire)
                {
                    return BitConverter.ToInt32(Data, 36);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    return BitConverter.ToUInt16(Data, 22);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.SoF)
                {
                    Data[24] = bytes[0];
                    Data[25] = bytes[1];
                }
                else if (MapType == MapType.Vindictus)
                {
                    bytes.CopyTo(Data, 40);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD)
                    || MapType == MapType.Nightfire)
                {
                    bytes.CopyTo(Data, 36);
                }
                else if (MapType.IsSubtypeOf(MapType.Quake)
                    || MapType.IsSubtypeOf(MapType.Quake2)
                    || MapType.IsSubtypeOf(MapType.Source))
                {
                    Data[22] = bytes[0];
                    Data[23] = bytes[1];
                }
            }
        }

        /// <summary>
        /// Gets or sets ambient water sound level for this <see cref="Leaf"/>.
        /// </summary>
        public byte WaterSoundLevel
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return Data[24];
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    Data[24] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets ambient sky sound level for this <see cref="Leaf"/>.
        /// </summary>
        public byte SkySoundLevel
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return Data[25];
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    Data[25] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets ambient slime sound level for this <see cref="Leaf"/>.
        /// </summary>
        public byte SlimeSoundLevel
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return Data[26];
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    Data[26] = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets ambient lava sound level for this <see cref="Leaf"/>.
        /// </summary>
        public byte LavaSoundLevel
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    return Data[27];
                }

                return 0;
            }
            set
            {
                if (MapType.IsSubtypeOf(MapType.Quake))
                {
                    Data[27] = value;
                }
            }
        }

        /// <summary>
        /// Enumerates the <see cref="StaticModel"/> indices used by this <see cref="Leaf"/>.
        /// </summary>
        public IEnumerable<int> LeafStaticModels
        {
            get
            {
                for (int i = 0; i < NumLeafStaticModelIndices; ++i)
                {
                    yield return (int)Parent.Bsp.LeafStaticModels[FirstLeafStaticModelIndex + i];
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first leaf model reference for this <see cref="Leaf"/>.
        /// </summary>
        [Index("LeafStaticModels")]
        public int FirstLeafStaticModelIndex
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    return BitConverter.ToInt32(Data, 56);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    bytes.CopyTo(Data, 56);
                }
            }
        }

        /// <summary>
        /// Gets or sets the count of leaf model references for this <see cref="Leaf"/>.
        /// </summary>
        [Count("LeafStaticModels")]
        public int NumLeafStaticModelIndices
        {
            get
            {
                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    return BitConverter.ToInt32(Data, 60);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType.IsSubtypeOf(MapType.MOHAA))
                {
                    bytes.CopyTo(Data, 60);
                }
            }
        }

        /// <summary>
        /// Enumerates the patch indices used by this <see cref="Leaf"/>.
        /// </summary>
        public IEnumerable<int> PatchIndices
        {
            get
            {
                for (int i = 0; i < NumPatchIndices; ++i)
                {
                    yield return (int)Parent.Bsp.LeafPatches[FirstPatchIndicesIndex + i];
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the first patch index for this <see cref="Leaf"/>.
        /// </summary>
        [Index("PatchIndices")]
        public int FirstPatchIndicesIndex
        {
            get
            {
                if (MapType == MapType.CoD
                    || MapType == MapType.CoDDemo
                    || MapType == MapType.CoD2)
                {
                    return BitConverter.ToInt32(Data, 8);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.CoD
                    || MapType == MapType.CoDDemo
                    || MapType == MapType.CoD2)
                {
                    bytes.CopyTo(Data, 8);
                }
            }
        }

        /// <summary>
        /// Gets or sets the count of patch indices referenced by this <see cref="Leaf"/>.
        /// </summary>
        [Count("PatchIndices")]
        public int NumPatchIndices
        {
            get
            {
                if (MapType == MapType.CoD
                    || MapType == MapType.CoDDemo
                    || MapType == MapType.CoD2)
                {
                    return BitConverter.ToInt32(Data, 12);
                }

                return -1;
            }
            set
            {
                byte[] bytes = BitConverter.GetBytes(value);

                if (MapType == MapType.CoD
                    || MapType == MapType.CoDDemo
                    || MapType == MapType.CoD2)
                {
                    bytes.CopyTo(Data, 12);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Leaf"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="parent">The <see cref="ILump"/> this <see cref="Leaf"/> came from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public Leaf(byte[] data, ILump parent = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Data = data;
            Parent = parent;
        }

        /// <summary>
        /// Creates a new <see cref="Leaf"/> by copying the fields in <paramref name="source"/>, using
        /// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
        /// to use when creating the new <see cref="Leaf"/>.
        /// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
        /// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
        /// </summary>
        /// <param name="source">The <see cref="Leaf"/> to copy.</param>
        /// <param name="parent">
        /// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="Leaf"/>.
        /// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
        /// </param>
        public Leaf(Leaf source, ILump parent)
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

            Contents = source.Contents;
            Visibility = source.Visibility;
            Area = source.Area;
            Flags = source.Flags;
            Minimums = source.Minimums;
            Maximums = source.Maximums;
            FirstMarkBrushIndex = source.FirstMarkBrushIndex;
            NumMarkBrushIndices = source.NumMarkBrushIndices;
            FirstMarkFaceIndex = source.FirstMarkFaceIndex;
            NumMarkFaceIndices = source.NumMarkFaceIndices;
            WaterSoundLevel = source.WaterSoundLevel;
            SkySoundLevel = source.SkySoundLevel;
            SlimeSoundLevel = source.SlimeSoundLevel;
            LavaSoundLevel = source.LavaSoundLevel;
            FirstLeafStaticModelIndex = source.FirstLeafStaticModelIndex;
            NumLeafStaticModelIndices = source.NumLeafStaticModelIndices;
            FirstPatchIndicesIndex = source.FirstPatchIndicesIndex;
            NumPatchIndices = source.NumPatchIndices;
        }

        /// <summary>
        /// Factory method to parse a <c>byte</c> array into a <see cref="Lump{Leaf}"/>.
        /// </summary>
        /// <param name="data">The data to parse.</param>
        /// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
        /// <returns>A <see cref="Lump{Leaf}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
        public static Lump<Leaf> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            return new Lump<Leaf>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
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
            if (mapType == MapType.CoD4)
            {
                return 24;
            }
            else if (mapType.IsSubtypeOf(MapType.CoD))
            {
                return 36;
            }
            else if (mapType.IsSubtypeOf(MapType.MOHAA))
            {
                return 64;
            }
            else if (mapType == MapType.Source18
                || mapType == MapType.Source19
                || mapType == MapType.Vindictus)
            {
                return 56;
            }
            else if (mapType.IsSubtypeOf(MapType.Source)
                || mapType == MapType.SoF
                || mapType == MapType.Daikatana)
            {
                return 32;
            }
            else if (mapType.IsSubtypeOf(MapType.Quake)
                || mapType.IsSubtypeOf(MapType.Quake2))
            {
                return 28;
            }
            else if (mapType.IsSubtypeOf(MapType.Quake3)
                || mapType == MapType.Nightfire)
            {
                return 48;
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
            if (type == MapType.Nightfire)
            {
                return 11;
            }
            else if (type == MapType.CoD
                || type == MapType.CoDDemo)
            {
                return 21;
            }
            else if (type == MapType.CoD2)
            {
                return 26;
            }
            else if (type == MapType.CoD4)
            {
                return 28;
            }
            else if (type.IsSubtypeOf(MapType.Quake2)
                || type.IsSubtypeOf(MapType.MOHAA)
                || type.IsSubtypeOf(MapType.FAKK2))
            {
                return 8;
            }
            else if (type.IsSubtypeOf(MapType.Quake)
                || type.IsSubtypeOf(MapType.Source)
                || type.IsSubtypeOf(MapType.STEF2))
            {
                return 10;
            }
            else if (type.IsSubtypeOf(MapType.Quake3))
            {
                return 4;
            }

            return -1;
        }

    }
}
