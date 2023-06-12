using System;
using System.Collections;
using System.Collections.Generic;

namespace LibBSP
{
    /// <summary>
    /// List class for numbers. Can handle any integer data type except <c>ulong</c>.
    /// </summary>
    public class NumList : IList<long>, ICollection<long>, IEnumerable<long>, IList, ICollection, IEnumerable, ILump
    {

        /// <summary>
        /// The <see cref="BSP"/> this <see cref="ILump"/> came from.
        /// </summary>
        public BSP Bsp { get; set; }

        /// <summary>
        /// The <see cref="LumpInfo"/> associated with this <see cref="ILump"/>.
        /// </summary>
        public LumpInfo LumpInfo { get; protected set; }

        /// <summary>
        /// Enum of the types that may be used in this class.
        /// </summary>
        public enum DataType : int
        {
            Invalid = 0,
            SByte = 1,
            Byte = 2,
            Int16 = 3,
            UInt16 = 4,
            Int32 = 5,
            UInt32 = 6,
            Int64 = 7,
        }

        /// <summary>
        /// Array of <c>byte</c>s used as the data source for this <see cref="NumList"/>.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets the length of this lump in bytes.
        /// </summary>
        public int Length
        {
            get
            {
                return Data.Length;
            }
        }

        /// <summary>
        /// The <see cref="DataType"/> this <see cref="NumList"/> stores.
        /// </summary>
        public DataType Type { get; private set; }

        /// <summary>
        /// Creates a new <see cref="NumList"/> object from a <c>byte</c> array.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="type">The type of number to store.</param>
        /// <param name="bsp">The parent <see cref="BSP"/> of this <see cref="NumList"/>.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> for this lump.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public NumList(byte[] data, DataType type, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo))
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }

            Bsp = bsp;
            LumpInfo = lumpInfo;
            Data = data;
            Type = type;
        }

        /// <summary>
        /// Creates a new <see cref="NumList"/> object using another <see cref="NumList"/> to copy.
        /// </summary>
        /// <param name="original">The <see cref="NumList"/> to copy.</param>
        /// <param name="type">The type of number to store.</param>
        /// <param name="bsp">The parent <see cref="BSP"/> of this <see cref="NumList"/>.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> for this lump.</param>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
        public NumList(NumList original, DataType type, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo))
        {
            Bsp = bsp;
            LumpInfo = lumpInfo;
            Type = type;
            Data = new byte[original.Count * StructLength];
            for (int i = 0; i < original.Count; ++i)
            {
                this[i] = original[i];
            }
        }

        /// <summary>
        /// Creates an empty <see cref="NumList"/> object.
        /// </summary>
        /// <param name="type">The type of number to store.</param>
        /// <param name="bsp">The parent <see cref="BSP"/> of this <see cref="NumList"/>.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> for this lump.</param>
        public NumList(DataType type, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo))
        {
            Bsp = bsp;
            Type = type;
            Data = new byte[0];
        }

        /// <summary>
        /// Creates a new <see cref="NumList"/> object from a <c>byte</c> array and returns it.
        /// </summary>
        /// <param name="data"><c>byte</c> array to parse.</param>
        /// <param name="type">The type of number to store.</param>
        /// <param name="bsp">The parent <see cref="BSP"/> of this <see cref="NumList"/>.</param>
        /// <param name="lumpInfo">The <see cref="LumpInfo"/> for this lump.</param>
        /// <returns>The resulting <see cref="NumList"/>.</returns>
        public static NumList LumpFactory(byte[] data, DataType type, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo))
        {
            return new NumList(data, type, bsp, lumpInfo);
        }

        /// <summary>
        /// Gets the length, in bytes, of the numerical primitive used by this instance of this class.
        /// </summary>
        public int StructLength
        {
            get
            {
                switch (Type)
                {
                    case DataType.Byte:
                    case DataType.SByte:
                    {
                        return sizeof(sbyte);
                    }
                    case DataType.UInt16:
                    case DataType.Int16:
                    {
                        return sizeof(short);
                    }
                    case DataType.UInt32:
                    case DataType.Int32:
                    {
                        return sizeof(int);
                    }
                    case DataType.Int64:
                    {
                        return sizeof(long);
                    }
                    default:
                    {
                        return 0;
                    }
                }
            }
        }

        /// <summary>
        /// Gets all the data in this lump as a byte array.
        /// </summary>
        /// <param name="lumpOffset">The offset of the beginning of this lump.</param>
        /// <returns>The data.</returns>
        public byte[] GetBytes(int lumpOffset = 0)
        {
            return Data;
        }

        #region IndicesForLumps
        /// <summary>
        /// Gets the index for the Leaf Faces lump in the BSP file for a specific map format, and the type of data the format uses.
        /// </summary>
        /// <param name="version">The map type.</param>
        /// <param name="dataType"><c>out</c> parameter that will contain the data type this version uses.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
        public static int GetIndexForLeafFacesLump(MapType version, out DataType dataType)
        {
            if (version == MapType.Nightfire)
            {
                dataType = DataType.UInt32;
                return 12;
            }
            else if (version == MapType.Vindictus)
            {
                dataType = DataType.UInt32;
                return 16;
            }
            else if (version.IsSubtypeOf(MapType.Quake))
            {
                dataType = DataType.UInt16;
                return 11;
            }
            else if (version == MapType.CoD
                || version == MapType.CoDDemo)
            {
                dataType = DataType.UInt32;
                return 23;
            }
            else if (version.IsSubtypeOf(MapType.Quake2))
            {
                dataType = DataType.UInt16;
                return 9;
            }
            else if (version.IsSubtypeOf(MapType.STEF2))
            {
                dataType = DataType.UInt32;
                return 9;
            }
            else if (version.IsSubtypeOf(MapType.FAKK2)
                || version.IsSubtypeOf(MapType.MOHAA))
            {
                dataType = DataType.Int32;
                return 7;
            }
            else if (version.IsSubtypeOf(MapType.Source))
            {
                dataType = DataType.UInt16;
                return 16;
            }
            else if (version.IsSubtypeOf(MapType.Quake3))
            {
                dataType = DataType.Int32;
                return 5;
            }

            dataType = DataType.Invalid;
            return -1;
        }

        /// <summary>
        /// Gets the index for the Face Edges lump in the BSP file for a specific map format, and the type of data the format uses.
        /// </summary>
        /// <param name="version">The map type.</param>
        /// <param name="dataType"><c>out</c> parameter that will contain data type this version uses.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
        public static int GetIndexForFaceEdgesLump(MapType version, out DataType dataType)
        {
            if (version.IsSubtypeOf(MapType.Quake2))
            {
                dataType = DataType.Int32;
                return 12;
            }
            else if (version.IsSubtypeOf(MapType.Quake)
                || version.IsSubtypeOf(MapType.Source))
            {
                dataType = DataType.Int32;
                return 13;
            }

            dataType = DataType.Invalid;
            return -1;
        }

        /// <summary>
        /// Gets the index for the Leaf Brushes lump in the BSP file for a specific map format, and the type of data the format uses.
        /// </summary>
        /// <param name="version">The map type.</param>
        /// <param name="dataType"><c>out</c> parameter that will contain the data type this version uses.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
        public static int GetIndexForLeafBrushesLump(MapType version, out DataType dataType)
        {
            if (version == MapType.Nightfire)
            {
                dataType = DataType.UInt32;
                return 13;
            }
            else if (version.IsSubtypeOf(MapType.STEF2))
            {
                dataType = DataType.UInt32;
                return 8;
            }
            else if (version.IsSubtypeOf(MapType.Quake2))
            {
                dataType = DataType.UInt16;
                return 10;
            }
            else if (version == MapType.Vindictus)
            {
                dataType = DataType.UInt32;
                return 17;
            }
            else if (version == MapType.CoD
                || version == MapType.CoDDemo)
            {
                dataType = DataType.UInt32;
                return 22;
            }
            else if (version == MapType.CoD2)
            {
                dataType = DataType.UInt32;
                return 27;
            }
            else if (version == MapType.CoD4)
            {
                dataType = DataType.UInt32;
                return 29;
            }
            else if (version.IsSubtypeOf(MapType.Source))
            {
                dataType = DataType.UInt16;
                return 17;
            }
            else if (version.IsSubtypeOf(MapType.Quake3))
            {
                dataType = DataType.UInt32;
                return 6;
            }

            dataType = DataType.Invalid;
            return -1;
        }

        /// <summary>
        /// Gets the index for the indices lump in the BSP file for a specific map format, and the type of data the format uses.
        /// </summary>
        /// <param name="version">The map type.</param>
        /// <param name="dataType"><c>out</c> parameter that will contain the data type this version uses.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
        public static int GetIndexForIndicesLump(MapType version, out DataType dataType)
        {
            if (version == MapType.Nightfire)
            {
                dataType = DataType.UInt32;
                return 6;
            }
            else if (version == MapType.CoD
                || version == MapType.CoDDemo)
            {
                dataType = DataType.UInt16;
                return 8;
            }
            else if (version == MapType.CoD2)
            {
                dataType = DataType.UInt16;
                return 9;
            }
            else if (version == MapType.CoD4)
            {
                dataType = DataType.UInt16;
                return 11;
            }
            else if (version.IsSubtypeOf(MapType.FAKK2)
                || version.IsSubtypeOf(MapType.MOHAA))
            {
                dataType = DataType.UInt32;
                return 5;
            }
            else if (version.IsSubtypeOf(MapType.STEF2))
            {
                dataType = DataType.UInt32;
                return 7;
            }
            else if (version.IsSubtypeOf(MapType.Quake3))
            {
                dataType = DataType.UInt32;
                return 11;
            }

            dataType = DataType.Invalid;
            return -1;
        }

        /// <summary>
        /// Gets the index for the patch indices lump in the BSP file for a specific map format, and the type of data the format uses.
        /// </summary>
        /// <param name="version">The map type.</param>
        /// <param name="dataType"><c>out</c> parameter that will contain the data type this version uses.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
        public static int GetIndexForPatchIndicesLump(MapType version, out DataType dataType)
        {
            if (version == MapType.CoD
                || version == MapType.CoDDemo)
            {
                dataType = DataType.UInt32;
                return 23;
            }

            dataType = DataType.Invalid;
            return -1;
        }

        /// <summary>
        /// Gets the index for the leaf patch indices lump in the BSP file for a specific map format, and the type of data the format uses.
        /// </summary>
        /// <param name="version">The map type.</param>
        /// <param name="dataType"><c>out</c> parameter that will contain the data type this version uses.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
        public static int GetIndexForLeafPatchesLump(MapType version, out DataType dataType)
        {
            if (version == MapType.CoD
                || version == MapType.CoDDemo)
            {
                dataType = DataType.UInt32;
                return 26;
            }

            dataType = DataType.Invalid;
            return -1;
        }

        /// <summary>
        /// Gets the index for the leaf static models indices lump in the BSP file for a specific map format, and the type of data the format uses.
        /// </summary>
        /// <param name="version">The map type.</param>
        /// <param name="dataType"><c>out</c> parameter that will contain the data type this version uses.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
        public static int GetIndexForLeafStaticModelsLump(MapType version, out DataType dataType)
        {
            if (version == MapType.MOHAADemo)
            {
                dataType = DataType.UInt16;
                return 27;
            }
            else if (version.IsSubtypeOf(MapType.MOHAA))
            {
                dataType = DataType.UInt16;
                return 26;
            }

            dataType = DataType.Invalid;
            return -1;
        }

        /// <summary>
        /// Gets the index for the texture table lump in the BSP file for a specific map format, and the type of data the format uses.
        /// </summary>
        /// <param name="version">The map type.</param>
        /// <param name="dataType"><c>out</c> parameter that will contain the data type this version uses.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
        public static int GetIndexForTexTableLump(MapType version, out DataType dataType)
        {
            if (version.IsSubtypeOf(MapType.Source)
                || version == MapType.Titanfall)
            {
                dataType = DataType.Int32;
                return 44;
            }

            dataType = DataType.Invalid;
            return -1;
        }

        /// <summary>
        /// Gets the index for the displacement triangles lump in the BSP file for a specific map format, and the type of data the format uses.
        /// </summary>
        /// <param name="version">The map type.</param>
        /// <param name="dataType"><c>out</c> parameter that will contain the data type this version uses.</param>
        /// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
        public static int GetIndexForDisplacementTrianglesLump(MapType version, out DataType dataType)
        {
            if (version.IsSubtypeOf(MapType.Source))
            {
                dataType = DataType.UInt16;
                return 48;
            }

            dataType = DataType.Invalid;
            return -1;
        }
        #endregion

        #region ICollection
        public void Add(long value)
        {
            byte[] temp = new byte[Data.Length + StructLength];
            Array.Copy(Data, 0, temp, 0, Data.Length);
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, temp, Data.Length, StructLength);
            Data = temp;
        }

        public void Clear()
        {
            Data = new byte[0];
        }

        public bool Contains(long value)
        {
            foreach (long curr in this)
            {
                if (curr == value)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(long[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; ++i)
            {
                array[i + arrayIndex] = this[i];
            }
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            for (int i = 0; i < Count; ++i)
            {
                array.SetValue(this[i], i + arrayIndex);
            }
        }

        public bool Remove(long value)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (this[i] == value)
                {
                    RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public int Count
        {
            get
            {
                return Data.Length / StructLength;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public object SyncRoot
        {
            get
            {
                return Data.SyncRoot;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return Data.IsSynchronized;
            }
        }
        #endregion

        #region IEnumerable
        public IEnumerator<long> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
            {
                yield return this[i];
            }
        }
        #endregion

        #region IList
        public int Add(object obj)
        {
            if (obj is byte || obj is sbyte || obj is short || obj is ushort || obj is int || obj is uint || obj is long)
            {
                Add((long)obj);
                return Count - 1;
            }

            return -1;
        }

        public bool Contains(object obj)
        {
            if (obj is byte || obj is sbyte || obj is short || obj is ushort || obj is int || obj is uint || obj is long)
            {
                return Contains((long)obj);
            }

            return false;
        }

        public int IndexOf(object obj)
        {
            if (obj is byte || obj is sbyte || obj is short || obj is ushort || obj is int || obj is uint || obj is long)
            {
                return IndexOf((long)obj);
            }

            return -1;
        }

        public void Insert(int index, object obj)
        {
            if (obj is byte || obj is sbyte || obj is short || obj is ushort || obj is int || obj is uint || obj is long)
            {
                Insert(index, (long)obj);
            }
        }

        public void Remove(object obj)
        {
            if (obj is byte || obj is sbyte || obj is short || obj is ushort || obj is int || obj is uint || obj is long)
            {
                Remove((long)obj);
            }
        }

        public int IndexOf(long value)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (this[i] == value)
                {
                    return i;
                }
            }

            return -1;
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long)
                {
                    this[index] = (long)value;
                }
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public void Insert(int index, long value)
        {
            byte[] temp = new byte[Data.Length + StructLength];
            Array.Copy(Data, 0, temp, 0, StructLength * index);
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, temp, StructLength * index, StructLength);
            Array.Copy(Data, StructLength * index, temp, StructLength * (index + 1), Data.Length - StructLength * index);
            Data = temp;
        }

        public void RemoveAt(int index)
        {
            byte[] temp = new byte[Data.Length - StructLength];
            Array.Copy(Data, 0, temp, 0, StructLength * index);
            Array.Copy(Data, StructLength * (index + 1), temp, StructLength * index, Data.Length - (StructLength * (index + 1)));
            Data = temp;
        }

        public long this[int index]
        {
            get
            {
                switch (Type)
                {
                    case DataType.SByte:
                    {
                        return (sbyte)Data[index];
                    }
                    case DataType.Byte:
                    {
                        return Data[index];
                    }
                    case DataType.Int16:
                    {
                        return BitConverter.ToInt16(Data, index * 2);
                    }
                    case DataType.UInt16:
                    {
                        return BitConverter.ToUInt16(Data, index * 2);
                    }
                    case DataType.Int32:
                    {
                        return BitConverter.ToInt32(Data, index * 4);
                    }
                    case DataType.UInt32:
                    {
                        return BitConverter.ToUInt32(Data, index * 4);
                    }
                    case DataType.Int64:
                    {
                        return BitConverter.ToInt64(Data, index * 8);
                    }
                    default:
                    {
                        return 0;
                    }
                }
            }
            set
            {
                Array.Copy(BitConverter.GetBytes(value), 0, Data, index * StructLength, StructLength);
            }
        }
        #endregion

        /// <summary>
        /// Removes all items from the list.
        /// </summary>
        public void RemoveAll()
        {
            Data = new byte[0];
        }
    }
}
