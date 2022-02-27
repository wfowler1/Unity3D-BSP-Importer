using System;
using System.IO;
using UnityEngine;

namespace BSPImporter {
	public class FTXLoader {

		public static Texture2D LoadFTX(string path) {
			using (FileStream stream = File.Open(path, FileMode.Open)) {
				byte[] data = new byte[stream.Length];
				stream.Read(data, 0, (int)stream.Length);

				Texture2D texture = new Texture2D(BitConverter.ToInt32(data, 0), BitConverter.ToInt32(data, 4));

				int offset = 12;
				for (int i = texture.height - 1; i >= 0; --i) {
					for (int j = 0; j < texture.width; ++j) {
						texture.SetPixel(j, i, new Color32(data[offset], data[offset + 1], data[offset + 2], data[offset + 3]));
						offset += 4;
					}
				}

				texture.Apply();
				return texture;
			}

		}
	}
}
