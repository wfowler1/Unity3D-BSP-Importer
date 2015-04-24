using System;
using UnityEngine;
// SourceTexData class

// Contains all the information for a single SourceTexData object

namespace BSPImporter {
	public class SourceTexData : LumpObject {

		// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS

		private Vector3 reflectivity;
		private int stringTableIndex;
		private int width;
		private int height;
		private int view_width;
		private int view_height;

		// CONSTRUCTORS

		// Takes everything exactly as it is stored
		public SourceTexData(Vector3 reflectivity, int stringTableIndex, int width, int height, int view_width, int view_height)
			: base(new byte[0]) {
			this.reflectivity = reflectivity;
			this.stringTableIndex = stringTableIndex;
			this.width = width;
			this.height = height;
			this.view_width = view_width;
			this.view_height = view_height;
		}

		// This constructor takes bytes in a byte array, as though
		// it had just been read by a FileInputStream.
		public SourceTexData(byte[] data)
			: base(data) {
			this.reflectivity = DataReader.readPoint3F(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11]);
			this.stringTableIndex = DataReader.readInt(data[12], data[13], data[14], data[15]);
			this.width = DataReader.readInt(data[16], data[17], data[18], data[19]);
			this.height = DataReader.readInt(data[20], data[21], data[22], data[23]);
			this.view_width = DataReader.readInt(data[24], data[25], data[26], data[27]);
			this.view_height = DataReader.readInt(data[28], data[29], data[30], data[31]);
		}

		// METHODS
		public static Lump<SourceTexData> createLump(byte[] data) {
			int structLength = 32;
			int offset = 0;
			Lump<SourceTexData> lump = new Lump<SourceTexData>(data.Length, structLength, data.Length / structLength);
			byte[] bytes = new byte[structLength];
			for(int i = 0; i < data.Length / structLength; i++) {
				for(int j = 0; j < structLength; j++) {
					bytes[j] = data[offset + j];
				}
				lump.Add(new SourceTexData(bytes));
				offset += structLength;
			}
			return lump;
		}

		// ACCESSORS/MUTATORS

		virtual public Vector3 Reflectivity {
			get {
				return reflectivity;
			}
			set {
				reflectivity = value;
			}
		}

		virtual public int StringTableIndex {
			get {
				return stringTableIndex;
			}
			set {
				stringTableIndex = value;
			}
		}

		virtual public int Width {
			get {
				return width;
			}
			set {
				width = value;
			}
		}

		virtual public int Height {
			get {
				return height;
			}
			set {
				height = value;
			}
		}

		virtual public int ViewWidth {
			get {
				return view_width;
			}
			set {
				view_width = value;
			}
		}

		virtual public int ViewHeight {
			get {
				return view_height;
			}
			set {
				view_height = value;
			}
		}
	}
}