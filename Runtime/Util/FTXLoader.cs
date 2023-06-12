using System;
using System.IO;
using UnityEngine;

namespace BSPImporter
{
    /// <summary>
    /// Static class for loading uncompressed R8G8B8A8 FTX texture files (Alice, FAKK2).
    /// </summary>
    public static class FTXLoader
    {
        /// <summary>
        /// Length of FTX file header.
        /// </summary>
        public const int HeaderLength = 12;

        /// <summary>
        /// Loads the FTX texture file at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Path to the FTX texture.</param>
        /// <returns><see cref="Texture2D"/> imported from the FTX texture, or <c>null</c> if file cannot be read.</returns>
        public static Texture2D LoadFTX(string path)
        {
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                return LoadFTX(stream);
            }
        }

        /// <summary>
        /// Loads FTX texture from <see cref="Stream"/> <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Stream to FTX texture.</param>
        /// <returns><see cref="Texture2D"/> imported from the FTX texture, or <c>null</c> if <paramref name="stream"/> cannot be read.</returns>
        public static Texture2D LoadFTX(Stream stream)
        {
            if (!stream.CanRead)
            {
                return null;
            }

            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);

            return LoadFTX(data);
        }

        /// <summary>
        /// Loads FTX texture from <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Bytes for the FTX texture.</param>
        /// <returns><see cref="Texture2D"/> imported from the FTX texture, or <c>null</c> if data is invalid.</returns>
        public static Texture2D LoadFTX(byte[] data)
        {
            if (data.Length < HeaderLength)
            {
                return null;
            }

            int width = BitConverter.ToInt32(data, 0);
            int height = BitConverter.ToInt32(data, 4);

            if (data.Length < (width * height * 4) + HeaderLength)
            {
                return null;
            }

            Texture2D texture = new Texture2D(width, height);

            int offset = HeaderLength;
            for (int i = texture.height - 1; i >= 0; --i)
            {
                for (int j = 0; j < texture.width; ++j)
                {
                    texture.SetPixel(j, i, new Color32(data[offset], data[offset + 1], data[offset + 2], data[offset + 3]));
                    offset += 4;
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
