using System.IO;

namespace LibBSP
{

    /// <summary>
    /// Handles reading of a BSP file.
    /// </summary>
    public class BSPWriter
    {

        private BSP _bsp;
        private int _numLumps;

        /// <summary>
        /// Constructs a new <see cref="BSPWriter"/> for the given <paramref name="bsp"/>.
        /// </summary>
        /// <param name="bsp">The <see cref="BSP"/> to write.</param>
        public BSPWriter(BSP bsp)
        {
            _bsp = bsp;
            _numLumps = BSP.GetNumLumps(_bsp.MapType);
        }

        /// <summary>
        /// Writes the <see cref="BSP"/> to the file at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The file path to write the <see cref="BSP"/> to.</param>
        public void WriteBSP(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            byte[][] lumpBytes = GetLumpsBytes();
            BSPHeader header = _bsp.Header.Regenerate();
            _bsp.UpdateHeader(header);
            _bsp.MapName = Path.GetFileNameWithoutExtension(path);

            WriteAllData(path, header.Data, lumpBytes);
            _bsp.Reader.BspFile = new FileInfo(path);
        }

        /// <summary>
        /// Gets the data from each lump as byte arrays and returns the result.
        /// </summary>
        /// <returns>Each lump's data as a byte array.</returns>
        private byte[][] GetLumpsBytes()
        {
            byte[][] lumpBytes = new byte[_numLumps][];
            int currentOffset = _bsp.Header.Length;

            for (int i = 0; i < _numLumps; i++)
            {
                ILump lump;
                if (i == GameLump.GetIndexForLump(_bsp.MapType))
                {
                    // If this is the GameLump, ensure it is loaded to update its internal offsets
                    lump = _bsp.GameLump;
                }
                else
                {
                    lump = _bsp.GetLoadedLump(i);
                }

                byte[] bytes;
                if (lump != null)
                {
                    bytes = lump.GetBytes(currentOffset);
                }
                else
                {
                    if (_bsp.Reader.BspFile != null && _bsp.Reader.BspFile.Exists)
                    {
                        bytes = _bsp.Reader.ReadLump(_bsp.Header.GetLumpInfo(i));
                    }
                    else
                    {
                        bytes = new byte[0];
                    }
                }

                lumpBytes[i] = bytes;
                currentOffset += bytes.Length;
            }

            return lumpBytes;
        }

        /// <summary>
        /// Writes the header data and all the lumps to <paramref name="path"/> sequentially.
        /// </summary>
        /// <param name="path">The path to write the BSP to.</param>
        /// <param name="header">The header data for the BSP.</param>
        /// <param name="lumpBytes">The data for each lump.</param>
        private void WriteAllData(string path, byte[] header, byte[][] lumpBytes)
        {
            using (FileStream stream = File.OpenWrite(path))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.Write(header, 0, header.Length);
                int offset = header.Length;

                for (int i = 0; i < _numLumps; ++i)
                {
                    stream.Write(lumpBytes[i], 0, lumpBytes[i].Length);
                    offset += lumpBytes[i].Length;
                }
            }
        }

    }
}
