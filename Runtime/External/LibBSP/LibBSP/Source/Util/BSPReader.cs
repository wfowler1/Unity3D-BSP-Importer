using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace LibBSP
{
    /// <summary>
    /// Handles reading of a BSP file on-demand.
    /// </summary>
    public class BSPReader
    {

        /// <summary>
        /// A short and simple string that will always occur in the entities lump, but is extremely
        /// unlikely to show up in any kind of binary data.
        /// </summary>
        private const string entityPattern = "\"classname\"";

        private Dictionary<int, LumpInfo> lumpFiles = null;

        /// <summary>
        /// Gets the <see cref="FileInfo"/> for this <see cref="BSPReader"/>, if used.
        /// </summary>
        public FileInfo BspFile { get; set; }

        /// <summary>
        /// An XOr encryption key for encrypted map formats. Must be read and set.
        /// </summary>
        private byte[] key = new byte[0];

        /// <summary>
        /// Creates a <see cref="BSPReader"/> not pointing to a file.
        /// </summary>
        public BSPReader() { }

        /// <summary>
        /// Creates a new instance of a <see cref="BSPReader"/> class to read the specified file.
        /// </summary>
        /// <param name="file">The <c>FileInfo</c> representing the file this <see cref="BSPReader"/> should read.</param>
        public BSPReader(FileInfo file)
        {
            if (!File.Exists(file.FullName))
            {
                throw new FileNotFoundException("Unable to open BSP file: File " + file.FullName + " was not found.");
            }
            else
            {
                BspFile = file;
            }
        }

        /// <summary>
        /// Gets the data for the <see cref="BSP"/>'s header.
        /// </summary>
        /// <param name="mapType">The <see cref="MapType"/> this is.</param>
        /// <returns><c>byte</c> array containing the header data.</returns>
        public byte[] GetHeader(MapType mapType)
        {
            if (BspFile == null)
            {
                return null;
            }
            if (mapType == MapType.CoD4)
            {
                int lumpInfoLength = BSPHeader.GetLumpInfoLength(mapType);
                int magicLength = BSPHeader.GetMagic(mapType).Length;

                byte[] bytes;
                using (FileStream stream = new FileStream(BspFile.FullName, FileMode.Open, FileAccess.Read))
                {
                    BinaryReader binaryReader = new BinaryReader(stream);

                    stream.Seek(magicLength, SeekOrigin.Begin);
                    int numLumps = binaryReader.ReadInt32();
                    int length = magicLength + 4 + (lumpInfoLength * numLumps);

                    stream.Seek(0, SeekOrigin.Begin);
                    bytes = binaryReader.ReadBytes(length);
                    binaryReader.Close();
                }

                return bytes;
            }
            else
            {
                int lumpInfoLength = BSPHeader.GetLumpInfoLength(mapType);
                int numLumps = BSP.GetNumLumps(mapType);
                int magicLength = BSPHeader.GetMagic(mapType).Length;

                int length = magicLength + (lumpInfoLength * numLumps);
                if (mapType.IsSubtypeOf(MapType.UberTools))
                {
                    length += 4;
                }

                byte[] bytes;
                using (FileStream stream = new FileStream(BspFile.FullName, FileMode.Open, FileAccess.Read))
                {
                    BinaryReader binaryReader = new BinaryReader(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    bytes = binaryReader.ReadBytes(length);
                    binaryReader.Close();
                }

                if (mapType == MapType.TacticalInterventionEncrypted)
                {
                    bytes = XorWithKeyStartingAtIndex(bytes);
                }

                return bytes;
            }
        }

        /// <summary>
        /// Reads the lump in the BSP file or lump file using the information in "<paramref name="info"/>".
        /// </summary>
        /// <param name="info">The <see cref="LumpInfo"/> object representing the lump's information.</param>
        /// <returns>
        /// A <c>byte</c> array containing the data from the file for the lump at the offset with the length from "<paramref name="info"/>".
        /// </returns>
        public byte[] ReadLump(LumpInfo info)
        {
            if (info.length == 0)
            {
                return new byte[0];
            }
            byte[] output;

            if (info.lumpFile != null)
            {
                output = ReadLump(info.offset, info.length, info.lumpFile.FullName);
            }
            else
            {
                output = ReadLump(info.offset, info.length);
            }

            if (key.Length != 0)
            {
                output = XorWithKeyStartingAtIndex(output, info.offset);
            }

            return output;
        }

        /// <summary>
        /// Reads the data in the specified file at <paramref name="offset"/> and length <paramref name="length"/>.
        /// </summary>
        /// <param name="offset">The offset to begin reading from.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <param name="fileName">The path of the file to read from, or <c>null</c> to read from the BSP file instead.</param>
        /// <returns>
        /// A <c>byte</c> array containing the data from the BSP or <paramref name="fileName"/> at <paramref name="offset"/> with <paramref name="length"/>.
        /// </returns>
        public byte[] ReadLump(int offset, int length, string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                if (BspFile == null)
                {
                    return null;
                }
                fileName = BspFile.FullName;
            }

            byte[] output;
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(stream);
                stream.Seek(offset, SeekOrigin.Begin);
                output = binaryReader.ReadBytes(length);
                binaryReader.Close();
            }
            return output;
        }

        /// <summary>
        /// Loads any lump files associated with the BSP.
        /// </summary>
        private void LoadLumpFiles()
        {
            lumpFiles = new Dictionary<int, LumpInfo>();
            if (BspFile == null)
            {
                return;
            }

            // Scan the BSP's directory for lump files
            DirectoryInfo dir = BspFile.Directory;
            List<FileInfo> files = dir.GetFiles(BspFile.Name.Substring(0, BspFile.Name.Length - 4) + "_?_*.lmp").ToList();
            // Sort the list by the number on the file
            files.Sort((f1, f2) =>
            {
                int startIndex = BspFile.Name.Length - 1;
                int f1EndIndex = f1.Name.LastIndexOf('.');
                int f2EndIndex = f2.Name.LastIndexOf('.');
                int f1Position = int.Parse(f1.Name.Substring(startIndex, f1EndIndex - startIndex));
                int f2Position = int.Parse(f2.Name.Substring(startIndex, f2EndIndex - startIndex));
                return f1Position - f2Position;
            });

            // Read the files in order. The last file in the list for a specific lump will replace that lump.
            foreach (FileInfo file in files)
            {
                using (FileStream fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                {
                    BinaryReader br = new BinaryReader(fs);
                    fs.Seek(0, SeekOrigin.Begin);
                    byte[] input = br.ReadBytes(20);
                    int offset = BitConverter.ToInt32(input, 0);
                    int lumpIndex = BitConverter.ToInt32(input, 4);
                    int version = BitConverter.ToInt32(input, 8);
                    int length = BitConverter.ToInt32(input, 12);
                    lumpFiles[lumpIndex] = new LumpInfo()
                    {
                        offset = offset,
                        version = version,
                        length = length,
                        lumpFile = file
                    };
                    br.Close();
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="LumpInfo"/> for the lump file for lump <paramref name="index"/>,
        /// if it exists.
        /// </summary>
        /// <param name="index">Index of the lump to get lump file <see cref="LumpInfo"/> for.</param>
        /// <returns><see cref="LumpInfo"/> for lump <paramref name="index"/> if it exists.</returns>
        public LumpInfo GetLumpFileLumpInfo(int index)
        {
            if (lumpFiles == null)
            {
                LoadLumpFiles();
            }

            if (lumpFiles.ContainsKey(index))
            {
                return lumpFiles[index];
            }

            return default(LumpInfo);
        }

        /// <summary>
        /// Xors the <paramref name="data"/> <c>byte</c> array with the locally stored key <c>byte</c> array, starting at a certain <paramref name="index"/> in the key.
        /// </summary>
        /// <param name="data">The byte array to Xor.</param>
        /// <param name="index">The index in the key byte array to start reading from.</param>
        /// <returns>The input <c>byte</c> array Xored with the key <c>byte</c> array.</returns>
        /// <exception cref="ArgumentNullException">The passed <paramref name="data"/> parameter was <c>null</c>.</exception>
        private byte[] XorWithKeyStartingAtIndex(byte[] data, int index = 0)
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }
            if (key.Length == 0 || data.Length == 0)
            {
                return data;
            }
            byte[] output = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                output[i] = (byte)(data[i] ^ key[(i + index) % key.Length]);
            }
            return output;
        }

        /// <summary>
        /// Tries to get the <see cref="MapType"/> member most closely represented by the referenced file. If the file is 
        /// found to be big-endian, this will set <see cref="BSPReader.bigEndian"/> to <c>true</c>.
        /// </summary>
        /// <returns>The <see cref="MapType"/> of this BSP, <see cref="MapType.Undefined"/> if it could not be determined.</returns>
        public MapType GetVersion()
        {
            MapType ret = GetVersion(false);
            //if (ret == MapType.Undefined) {
            //	ret = GetVersion(true);
            //	if (ret != MapType.Undefined) {
            //		_bigEndian = true;
            //	}
            //}
            return ret;
        }

        /// <summary>
        /// Tries to get the <see cref="MapType"/> member most closely represented by the referenced file.
        /// </summary>
        /// <param name="bigEndian">Set to <c>true</c> to attempt reading the data in big-endian byte order.</param>
        /// <returns>The <see cref="MapType"/> of this BSP, <see cref="MapType.Undefined"/> if it could not be determined.</returns>
        private MapType GetVersion(bool bigEndian)
        {
            MapType current = MapType.Undefined;

            if (BspFile != null)
            {
                using (FileStream stream = new FileStream(BspFile.FullName, FileMode.Open, FileAccess.Read))
                {
                    if (stream.Length < 4)
                    {
                        return current;
                    }
                    BinaryReader binaryReader = new BinaryReader(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                    int num = binaryReader.ReadInt32();
                    if (bigEndian)
                    {
                        byte[] bytes = BitConverter.GetBytes(num);
                        Array.Reverse(bytes);
                        num = BitConverter.ToInt32(bytes, 0);
                    }
                    switch (num)
                    {
                        case BSPHeader.IBSPHeader:
                        {
                            // Versions: CoD, CoD2, CoD4, Quake 2, Daikatana, Quake 3 (RtCW), Soldier of Fortune
                            int num2 = binaryReader.ReadInt32();
                            if (bigEndian)
                            {
                                byte[] bytes = BitConverter.GetBytes(num2);
                                Array.Reverse(bytes);
                                num2 = BitConverter.ToInt32(bytes, 0);
                            }
                            switch (num2)
                            {
                                case 4:
                                {
                                    current = MapType.CoD2;
                                    break;
                                }
                                case 22:
                                {
                                    current = MapType.CoD4;
                                    break;
                                }
                                case 38:
                                {
                                    current = MapType.Quake2;
                                    break;
                                }
                                case 41:
                                {
                                    current = MapType.Daikatana;
                                    break;
                                }
                                case 46:
                                {
                                    current = MapType.Quake3;
                                    // This version number is both Quake 3 and Soldier of Fortune. Find out the length of the
                                    // header, based on offsets.
                                    for (int i = 0; i < 17; i++)
                                    {
                                        stream.Seek((i + 1) * 8, SeekOrigin.Begin);
                                        int temp = binaryReader.ReadInt32();
                                        if (bigEndian)
                                        {
                                            byte[] bytes = BitConverter.GetBytes(temp);
                                            Array.Reverse(bytes);
                                            temp = BitConverter.ToInt32(bytes, 0);
                                        }
                                        if (temp == 184)
                                        {
                                            current = MapType.SoF;
                                            break;
                                        }
                                        else
                                        {
                                            if (temp == 144)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    break;
                                }
                                case 47:
                                {
                                    current = MapType.ET;
                                    break;
                                }
                                case 58:
                                {
                                    current = MapType.CoDDemo;
                                    break;
                                }
                                case 59:
                                {
                                    current = MapType.CoD;
                                    break;
                                }
                            }
                            break;
                        }
                        case BSPHeader.MOHAAHeader:
                        {
                            // MoHAA
                            int num2 = binaryReader.ReadInt32();
                            if (num2 == 18)
                            {
                                current = MapType.MOHAADemo;
                            }
                            else
                            {
                                current = MapType.MOHAA;
                            }
                            break;
                        }
                        case BSPHeader.EALAHeader:
                        {
                            // MoHAA Spearhead and Breakthrough
                            current = MapType.MOHAABT;
                            break;
                        }
                        case BSPHeader.VBSPHeader:
                        {
                            // Source engine.
                            // Some source games handle this as 2 shorts.
                            // TODO: Big endian?
                            // Formats: Source 17-23 and 27, DMoMaM, Vindictus
                            int num2 = (int)binaryReader.ReadUInt16();
                            switch (num2)
                            {
                                case 17:
                                {
                                    current = MapType.Source17;
                                    break;
                                }
                                case 18:
                                {
                                    current = MapType.Source18;
                                    break;
                                }
                                case 19:
                                {
                                    current = MapType.Source19;
                                    break;
                                }
                                case 20:
                                {
                                    int version2 = (int)binaryReader.ReadUInt16();
                                    if (version2 == 4)
                                    {
                                        // TODO: This doesn't necessarily mean the whole map should be read as DMoMaM.
                                        current = MapType.DMoMaM;
                                    }
                                    else
                                    {
                                        current = MapType.Source20;
                                        // Hack for detecting Vindictus: Look in the GameLump for offset/length/flags outside of ranges we'd expect
                                        stream.Seek(568, SeekOrigin.Begin);
                                        int gameLumpOffset = binaryReader.ReadInt32();
                                        stream.Seek(gameLumpOffset, SeekOrigin.Begin);
                                        int numGameLumps = binaryReader.ReadInt32();
                                        if (numGameLumps > 0)
                                        {
                                            // Normally this would be the offset and length for the first game lump.
                                            // But in Vindictus it's the version indicator for it instead.
                                            stream.Seek(gameLumpOffset + 12, SeekOrigin.Begin);
                                            int testOffset = binaryReader.ReadInt32();
                                            if (numGameLumps > 0)
                                            {
                                                if (testOffset < 24)
                                                {
                                                    current = MapType.Vindictus;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                                case 21:
                                {
                                    current = MapType.Source21;
                                    // Hack to determine if this is a L4D2 map. Read what would normally be the offset of
                                    // a lump. If it is less than the header length it's probably not an offset, indicating L4D2.
                                    stream.Seek(8, SeekOrigin.Begin);
                                    int test = binaryReader.ReadInt32();
                                    if (bigEndian)
                                    {
                                        byte[] bytes = BitConverter.GetBytes(test);
                                        Array.Reverse(bytes);
                                        test = BitConverter.ToInt32(bytes, 0);
                                    }
                                    if (test < 1032)
                                    {
                                        current = MapType.L4D2;
                                    }
                                    break;
                                }
                                case 22:
                                {
                                    current = MapType.Source22;
                                    break;
                                }
                                case 23:
                                {
                                    current = MapType.Source23;
                                    break;
                                }
                                case 27:
                                {
                                    current = MapType.Source27;
                                    break;
                                }
                            }
                            break;
                        }
                        case BSPHeader.RBSPHeader:
                        {
                            // Raven software's modification of Q3BSP, or Ritual's modification of Q2.
                            // Formats: Raven, SiN
                            current = MapType.Raven;
                            for (int i = 0; i < 17; i++)
                            {
                                // Find out where the first lump starts, based on offsets.
                                stream.Seek((i + 1) * 8, SeekOrigin.Begin);
                                int temp = binaryReader.ReadInt32();
                                if (bigEndian)
                                {
                                    byte[] bytes = BitConverter.GetBytes(temp);
                                    Array.Reverse(bytes);
                                    temp = BitConverter.ToInt32(bytes, 0);
                                }
                                if (temp == 168)
                                {
                                    current = MapType.SiN;
                                    break;
                                }
                                else
                                {
                                    if (temp == 152)
                                    {
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                        case BSPHeader.EF2Header:
                        {
                            // "EF2!"
                            current = MapType.STEF2;
                            break;
                        }
                        case BSPHeader.rBSPHeader:
                        {
                            // Titanfall
                            current = MapType.Titanfall;
                            break;
                        }
                        case BSPHeader.FAKKHeader:
                        {
                            // Formats: STEF2 demo, Heavy Metal FAKK2, American McGee's Alice
                            int num2 = binaryReader.ReadInt32();
                            if (bigEndian)
                            {
                                byte[] bytes = BitConverter.GetBytes(num2);
                                Array.Reverse(bytes);
                                num2 = BitConverter.ToInt32(bytes, 0);
                            }
                            switch (num2)
                            {
                                case 12:
                                {
                                    current = MapType.FAKK2;
                                    break;
                                }
                                case 19:
                                {
                                    current = MapType.STEF2Demo;
                                    break;
                                }
                                case 42:
                                {// American McGee's Alice
                                    current = MapType.Alice;
                                    break;
                                }
                            }
                            break;
                        }
                        // Various numbers not representing a string
                        // Formats: HL1, Quake, Nightfire, or perhaps Tactical Intervention's encrypted format
                        case 29:
                        {
                            current = MapType.Quake;
                            break;
                        }
                        case 30:
                        {
                            current = MapType.BlueShift;
                            stream.Seek(4, SeekOrigin.Begin);
                            int lump0offset = binaryReader.ReadInt32();
                            int lump0length = binaryReader.ReadInt32();
                            stream.Seek(lump0offset, SeekOrigin.Begin);
                            char currentChar;
                            int patternMatch = 0;
                            for (int i = 0; i < lump0length - entityPattern.Length; ++i)
                            {
                                currentChar = (char)stream.ReadByte();
                                if (currentChar == entityPattern[patternMatch])
                                {
                                    ++patternMatch;
                                    if (patternMatch == entityPattern.Length)
                                    {
                                        current = MapType.GoldSrc;
                                        break;
                                    }
                                }
                                else
                                {
                                    patternMatch = 0;
                                }
                            }
                            break;
                        }
                        case 42:
                        {
                            current = MapType.Nightfire;
                            break;
                        }
                        default:
                        {
                            // Hack to get Tactical Intervention's encryption key. At offset 384, there are two unused lumps whose
                            // values in the header are always 0s. Grab these 32 bytes (256 bits) and see if they match an expected value.
                            stream.Seek(384, SeekOrigin.Begin);
                            key = binaryReader.ReadBytes(32);
                            stream.Seek(0, SeekOrigin.Begin);
                            int num2 = BitConverter.ToInt32(XorWithKeyStartingAtIndex(binaryReader.ReadBytes(4)), 0);
                            if (num2 == BSPHeader.VBSPHeader)
                            {
                                current = MapType.TacticalInterventionEncrypted;
                            }
                            else
                            {
                                current = MapType.Undefined;
                            }
                            break;
                        }
                    }
                    binaryReader.Close();
                }
            }
            return current;
        }
    }
}
