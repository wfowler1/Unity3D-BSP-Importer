using System;
// Face class
// Replaces all the separate face classes for different versions of BSP.
// Or, at least the ones I need.

namespace BSPImporter {
	public class Face : LumpObject {

		// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS

		// Faces is one of the more different lumps between versions. Some of these fields
		// are only used by one format. However, there are some commonalities which make
		// it worthwhile to unify these. All formats use a plane, a texture, and vertices
		// in some way. Also (unused for the decompiler) they all use lightmaps.
		private int plane = -1;
		private int side = -1;
		private int firstEdge = -1;
		private int numEdges = -1;
		private int texture = -1;
		private int firstVertex = -1;
		private int numVertices = -1;
		private int material = -1;
		private int textureScale = -1;
		private int displacement = -1;
		private int original = -1;
		private byte[] flags;
		private int firstIndex = -1;
		private int numIndices = -1;
		private int unknown = -1;
		private int lightStyles = -1;
		private int lightMaps = -1;

		// CONSTRUCTORS

		public Face(LumpObject data, mapType type)
			: base(data.Data) {
			new Face(data.Data, type);
		}

		public Face(byte[] data, mapType type)
			: base(data) {
			switch(type) {
				case mapType.TYPE_QUAKE:
				case mapType.TYPE_QUAKE2:
				case mapType.TYPE_DAIKATANA:
				case mapType.TYPE_SIN:
				case mapType.TYPE_SOF:
					plane = DataReader.readUShort(data[0], data[1]);
					side = DataReader.readUShort(data[2], data[3]);
					firstEdge = DataReader.readInt(data[4], data[5], data[6], data[7]);
					numEdges = DataReader.readUShort(data[8], data[9]);
					texture = DataReader.readUShort(data[10], data[11]);
					break;
				case mapType.TYPE_QUAKE3:
				case mapType.TYPE_RAVEN:
				case mapType.TYPE_STEF2:
				case mapType.TYPE_STEF2DEMO:
				case mapType.TYPE_MOHAA:
				case mapType.TYPE_FAKK:
					texture = DataReader.readInt(data[0], data[1], data[2], data[3]);
					flags = new byte[] { data[8], data[9], data[10], data[11] };
					firstVertex = DataReader.readInt(data[12], data[13], data[14], data[15]);
					numVertices = DataReader.readInt(data[16], data[17], data[18], data[19]);
					firstIndex = DataReader.readInt(data[20], data[21], data[22], data[23]);
					numIndices = DataReader.readInt(data[24], data[25], data[26], data[27]);
					break;
				case mapType.TYPE_SOURCE17:
					plane = DataReader.readUShort(data[32], data[33]);
					side = (int)data[34];
					firstEdge = DataReader.readInt(data[36], data[37], data[38], data[39]);
					numEdges = DataReader.readUShort(data[40], data[41]);
					textureScale = DataReader.readUShort(data[42], data[43]);
					displacement = DataReader.readShort(data[44], data[45]);
					original = DataReader.readInt(data[96], data[97], data[98], data[99]);
					break;
				case mapType.TYPE_SOURCE18:
				case mapType.TYPE_SOURCE19:
				case mapType.TYPE_SOURCE20:
				case mapType.TYPE_SOURCE21:
				case mapType.TYPE_SOURCE22:
				case mapType.TYPE_SOURCE23:
				case mapType.TYPE_SOURCE27:
				case mapType.TYPE_TACTICALINTERVENTION:
				case mapType.TYPE_DMOMAM:
					plane = DataReader.readUShort(data[0], data[1]);
					side = (int)data[2];
					firstEdge = DataReader.readInt(data[4], data[5], data[6], data[7]);
					numEdges = DataReader.readUShort(data[8], data[9]);
					textureScale = DataReader.readUShort(data[10], data[11]);
					displacement = DataReader.readShort(data[12], data[13]);
					original = DataReader.readInt(data[44], data[45], data[46], data[47]);
					break;
				case mapType.TYPE_VINDICTUS:
					plane = DataReader.readInt(data[0], data[1], data[2], data[3]);
					side = (int)data[4];
					firstEdge = DataReader.readInt(data[8], data[9], data[10], data[11]);
					numEdges = DataReader.readInt(data[12], data[13], data[14], data[15]);
					textureScale = DataReader.readInt(data[16], data[17], data[18], data[19]);
					displacement = DataReader.readInt(data[20], data[21], data[22], data[23]);
					original = DataReader.readInt(data[56], data[57], data[58], data[59]);
					break;
				case mapType.TYPE_NIGHTFIRE:
					plane = DataReader.readInt(data[0], data[1], data[2], data[3]);
					firstVertex = DataReader.readInt(data[4], data[5], data[6], data[7]);
					numVertices = DataReader.readInt(data[8], data[9], data[10], data[11]);
					firstIndex = DataReader.readInt(data[12], data[13], data[14], data[15]);
					numIndices = DataReader.readInt(data[16], data[17], data[18], data[19]);
					flags = new byte[] { data[20], data[21], data[22], data[23] };
					texture = DataReader.readInt(data[24], data[25], data[26], data[27]);
					material = DataReader.readInt(data[28], data[29], data[30], data[31]);
					textureScale = DataReader.readInt(data[32], data[33], data[34], data[35]);
					unknown = DataReader.readInt(data[36], data[37], data[38], data[39]);
					lightStyles = DataReader.readInt(data[40], data[41], data[42], data[43]);
					lightMaps = DataReader.readInt(data[44], data[45], data[46], data[47]);
					break;
			}
		}

		// METHODS
		public static Lump<Face> createLump(byte[] data, mapType type) {
			int structLength = 0;
			switch(type) {
				case mapType.TYPE_QUAKE:
				case mapType.TYPE_QUAKE2:
				case mapType.TYPE_DAIKATANA:
					structLength = 20;
					break;
				case mapType.TYPE_SIN:
					structLength = 36;
					break;
				case mapType.TYPE_SOF:
					structLength = 40;
					break;
				case mapType.TYPE_NIGHTFIRE:
					structLength = 48;
					break;
				case mapType.TYPE_SOURCE17:
					structLength = 104;
					break;
				case mapType.TYPE_SOURCE18:
				case mapType.TYPE_SOURCE19:
				case mapType.TYPE_SOURCE20:
				case mapType.TYPE_SOURCE21:
				case mapType.TYPE_SOURCE22:
				case mapType.TYPE_SOURCE23:
				case mapType.TYPE_SOURCE27:
				case mapType.TYPE_TACTICALINTERVENTION:
				case mapType.TYPE_DMOMAM:
					structLength = 56;
					break;
				case mapType.TYPE_VINDICTUS:
					structLength = 72;
					break;
				case mapType.TYPE_QUAKE3:
					structLength = 104;
					break;
				case mapType.TYPE_MOHAA:
					structLength = 108;
					break;
				case mapType.TYPE_STEF2:
				case mapType.TYPE_STEF2DEMO:
					structLength = 132;
					break;
				case mapType.TYPE_RAVEN:
					structLength = 148;
					break;
			}
			int offset = 0;
			Lump<Face> lump = new Lump<Face>(data.Length, structLength, data.Length / structLength);
			byte[] bytes = new byte[structLength];
			for(int i = 0; i < data.Length / structLength; i++) {
				for(int j = 0; j < structLength; j++) {
					bytes[j] = data[offset + j];
				}
				lump.Add(new Face(bytes, type));
				offset += structLength;
			}
			return lump;
		}

		// ACCESSORS/MUTATORS
		virtual public int Plane {
			get {
				return plane;
			}
		}

		virtual public int Side {
			get {
				return side;
			}
		}

		virtual public int FirstEdge {
			get {
				return firstEdge;
			}
		}

		virtual public int NumEdges {
			get {
				return numEdges;
			}
		}

		virtual public int Texture {
			get {
				return texture;
			}
		}

		virtual public int FirstVertex {
			get {
				return firstVertex;
			}
		}

		virtual public int NumVertices {
			get {
				return numVertices;
			}
		}

		virtual public int Material {
			get {
				return material;
			}
		}

		virtual public int TextureScale {
			get {
				return textureScale;
			}
		}

		virtual public int Displacement {
			get {
				return displacement;
			}
		}

		virtual public int Original {
			get {
				return original;
			}
		}

		virtual public byte[] Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}

		virtual public int FirstIndex {
			get {
				return firstIndex;
			}
			set {
				firstIndex = value;
			}
		}

		virtual public int NumIndices {
			get {
				return numIndices;
			}
			set {
				numIndices = value;
			}
		}

		public int Unknown {
			get {
				return unknown;
			}
			set {
				unknown = value;
			}
		}

		public int LightStyles {
			get {
				return lightStyles;
			}
			set {
				lightStyles = value;
			}
		}

		public int LightMaps {
			get {
				return lightMaps;
			}
			set {
				lightMaps = value;
			}
		}
	}
}