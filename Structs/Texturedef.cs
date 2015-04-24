using System;
using System.Collections.Generic;
// Texture class
//
// An all-encompassing class to handle the texture information of any given BSP format.
// The way texture information is stored varies wildly between versions. As a general
// rule, this class only handles the lump containing the string of a texture's name,
// and data from the same lump associated with it.
// For example, Nightfire's texture lump only contains 64-byte null-padded strings, but
// Quake 2's has texture scaling included, which is lump 17 in Nightfire.

namespace BSPImporter {
	public class Texturedef : LumpObject {

		// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
		private string name;
		private string mask = "ignore"; // Only used by MoHAA, and "ignore" means it's unused
		private byte[] flags;
		private byte[] contents;
		private TexInfo texAxes;

		// CONSTRUCTORS
		public Texturedef(string texture)
			: base(Convert.FromBase64String(texture)) {
			this.name = texture;
		}

		public Texturedef(LumpObject data, mapType type)
			: base(data.Data) {
			new Texturedef(data.Data, type);
		}

		public Texturedef(byte[] data, mapType type)
			: base(data) {
			switch(type) {
				case mapType.TYPE_NIGHTFIRE:
					name = DataReader.readNullTerminatedString(new byte[] { data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15], data[16], data[17], data[18], data[19], data[20], data[21], data[22], data[23], data[24], data[25], data[26], data[27], data[28], data[29], data[30], data[31], data[32], data[33], data[34], data[35], data[36], data[37], data[38], data[39], data[40], data[41], data[42], data[43], data[44], data[45], data[46], data[47], data[48], data[49], data[50], data[51], data[52], data[53], data[54], data[55], data[56], data[57], data[58], data[59], data[60], data[61], data[62], data[63] });
					break;
				case mapType.TYPE_QUAKE:
					name = DataReader.readNullTerminatedString(new byte[] { data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15] });
					break;
				case mapType.TYPE_QUAKE2:
				case mapType.TYPE_SOF:
				case mapType.TYPE_DAIKATANA:
					texAxes = new TexInfo(DataReader.readPoint3F(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11]), DataReader.readFloat(data[12], data[13], data[14], data[15]), DataReader.readPoint3F(data[16], data[17], data[18], data[19], data[20], data[21], data[22], data[23], data[24], data[25], data[26], data[27]), DataReader.readFloat(data[28], data[29], data[30], data[31]), -1, -1);
					flags = new byte[] { data[32], data[33], data[34], data[35] };
					name = DataReader.readNullTerminatedString(new byte[] { data[40], data[41], data[42], data[43], data[44], data[45], data[46], data[47], data[48], data[49], data[50], data[51], data[52], data[53], data[54], data[55], data[56], data[57], data[58], data[59], data[60], data[61], data[62], data[63], data[64], data[65], data[66], data[67], data[68], data[69], data[70], data[71] });
					break;
				case mapType.TYPE_MOHAA:
					mask = DataReader.readNullTerminatedString(new byte[] { data[76], data[77], data[78], data[79], data[80], data[81], data[82], data[83], data[84], data[85], data[86], data[87], data[88], data[89], data[90], data[91], data[92], data[93], data[94], data[95], data[96], data[97], data[98], data[99], data[100], data[101], data[102], data[103], data[104], data[105], data[106], data[107], data[108], data[109], data[110], data[111], data[112], data[113], data[114], data[115], data[116], data[117], data[118], data[119], data[120], data[121], data[122], data[123], data[124], data[125], data[126], data[127], data[128], data[129], data[130], data[131], data[132], data[133], data[134], data[135], data[136], data[137], data[138], data[139] });
					goto case mapType.TYPE_STEF2;
				case mapType.TYPE_STEF2:
				case mapType.TYPE_STEF2DEMO:
				case mapType.TYPE_RAVEN:
				case mapType.TYPE_QUAKE3:
				case mapType.TYPE_COD:
				case mapType.TYPE_COD2:
				case mapType.TYPE_COD4:
				case mapType.TYPE_FAKK:
					name = DataReader.readNullTerminatedString(new byte[] { data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15], data[16], data[17], data[18], data[19], data[20], data[21], data[22], data[23], data[24], data[25], data[26], data[27], data[28], data[29], data[30], data[31], data[32], data[33], data[34], data[35], data[36], data[37], data[38], data[39], data[40], data[41], data[42], data[43], data[44], data[45], data[46], data[47], data[48], data[49], data[50], data[51], data[52], data[53], data[54], data[55], data[56], data[57], data[58], data[59], data[60], data[61], data[62], data[63] });
					flags = new byte[] { data[64], data[65], data[66], data[67] };
					contents = new byte[] { data[68], data[69], data[70], data[71] };
					break;
				case mapType.TYPE_SOURCE17:
				case mapType.TYPE_SOURCE18:
				case mapType.TYPE_SOURCE19:
				case mapType.TYPE_SOURCE20:
				case mapType.TYPE_SOURCE21:
				case mapType.TYPE_SOURCE22:
				case mapType.TYPE_SOURCE23:
				case mapType.TYPE_SOURCE27:
				case mapType.TYPE_TACTICALINTERVENTION:
				case mapType.TYPE_VINDICTUS:
				case mapType.TYPE_DMOMAM:
					name = DataReader.readString(data);
					break;
				case mapType.TYPE_SIN:
					texAxes = new TexInfo(DataReader.readPoint3F(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11]), DataReader.readFloat(data[12], data[13], data[14], data[15]), DataReader.readPoint3F(data[16], data[17], data[18], data[19], data[20], data[21], data[22], data[23], data[24], data[25], data[26], data[27]), DataReader.readFloat(data[28], data[29], data[30], data[31]), -1, -1);
					flags = new byte[] { data[32], data[33], data[34], data[35] };
					name = DataReader.readNullTerminatedString(new byte[] { data[36], data[37], data[38], data[39], data[40], data[41], data[42], data[43], data[44], data[45], data[46], data[47], data[48], data[49], data[50], data[51], data[52], data[53], data[54], data[55], data[56], data[57], data[58], data[59], data[60], data[61], data[62], data[63], data[64], data[65], data[66], data[67], data[68], data[69], data[70], data[71], data[72], data[73], data[74], data[75], data[76], data[77], data[78], data[79], data[80], data[81], data[82], data[83], data[84], data[85], data[86], data[87], data[88], data[89], data[90], data[91], data[92], data[93], data[94], data[95], data[96], data[97], data[98], data[99] });
					break;
			}
		}

		// METHODS
		public static Texturedefs createLump(byte[] data, mapType type) {
			int numElements = -1; // For Quake and Source, which use nonconstant struct lengths
			int[] offsets = new int[0]; // For Quake, which stores offsets to each texture definition structure, which IS a constant length
			int structLength = 0;
			switch(type) {
				case mapType.TYPE_NIGHTFIRE:
					structLength = 64;
					break;
				case mapType.TYPE_QUAKE3:
				case mapType.TYPE_RAVEN:
				case mapType.TYPE_COD:
				case mapType.TYPE_COD2:
				case mapType.TYPE_COD4:
					structLength = 72;
					break;
				case mapType.TYPE_QUAKE2:
				case mapType.TYPE_DAIKATANA:
				case mapType.TYPE_SOF:
				case mapType.TYPE_STEF2:
				case mapType.TYPE_STEF2DEMO:
				case mapType.TYPE_FAKK:
					structLength = 76;
					break;
				case mapType.TYPE_MOHAA:
					structLength = 140;
					break;
				case mapType.TYPE_SIN:
					structLength = 180;
					break;
				case mapType.TYPE_SOURCE17:
				case mapType.TYPE_SOURCE18:
				case mapType.TYPE_SOURCE19:
				case mapType.TYPE_SOURCE20:
				case mapType.TYPE_SOURCE21:
				case mapType.TYPE_SOURCE22:
				case mapType.TYPE_SOURCE23:
				case mapType.TYPE_SOURCE27:
				case mapType.TYPE_TACTICALINTERVENTION:
				case mapType.TYPE_VINDICTUS:
				case mapType.TYPE_DMOMAM:
					numElements = 0;
					for(int i = 0; i < data.Length; i++) {
						if(data[i] == 0x00) {
							numElements++;
						}
					}
					break;
				case mapType.TYPE_QUAKE:
					numElements = DataReader.readInt(data[0], data[1], data[2], data[3]);
					offsets = new int[numElements];
					for(int i = 0; i < numElements; i++) {
						offsets[i] = DataReader.readInt(data[((i + 1) * 4)], data[((i + 1) * 4) + 1], data[((i + 1) * 4) + 2], data[((i + 1) * 4) + 3]);
					}
					structLength = 40;
					break;
			}
			Texturedefs lump;
			if(numElements == -1) {
				int offset = 0;
				//elements = new Texture[data.Length / structLength];
				lump = new Texturedefs(new List<Texturedef>(data.Length / structLength), data.Length, structLength);
				byte[] bytes = new byte[structLength];
				for(int i = 0; i < data.Length / structLength; i++) {
					for(int j = 0; j < structLength; j++) {
						bytes[j] = data[offset + j];
					}
					lump.Add(new Texturedef(bytes, type));
					offset += structLength;
				}
			} else {
				lump = new Texturedefs(new List<Texturedef>(numElements), data.Length, structLength);
				if(offsets.Length != 0) {
					// Quake/GoldSrc
					for(int i = 0; i < numElements; i++) {
						int offset = offsets[i];
						byte[] bytes = new byte[structLength];
						for(int j = 0; j < structLength; j++) {
							bytes[j] = data[offset + j];
						}
						lump.Add(new Texturedef(bytes, type));
						offset += structLength;
					}
				} else {
					// Source
					int offset = 0;
					int current = 0;
					byte[] bytes = new byte[0];
					for(int i = 0; i < data.Length; i++) {
						if(data[i] == (byte)0x00) {
							// They are null-terminated strings, of non-constant length (not padded)
							lump.Add(new Texturedef(bytes, type));
							bytes = new byte[0];
							current++;
						} else {
							byte[] newList = new byte[bytes.Length + 1];
							for(int j = 0; j < bytes.Length; j++) {
								newList[j] = bytes[j];
							}
							newList[bytes.Length] = data[i];
							bytes = newList;
						}
						offset++;
					}
				}
			}
			return lump;
		}

		// ACCESSORS/MUTATORS
		virtual public string Name {
			get {
				return name;
			}
		}

		virtual public string Mask {
			get {
				return mask;
			}
		}

		virtual public byte[] Flags {
			get {
				return flags;
			}
		}

		virtual public byte[] Contents {
			get {
				return contents;
			}
		}

		virtual public TexInfo TexAxes {
			get {
				return texAxes;
			}
		}
	}
}