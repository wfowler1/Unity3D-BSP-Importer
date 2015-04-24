using System;
// Edge class

// Holds all the data for an edge in a Quake, Half-Life, or Quake 2 map.
// Doubles as a subsector class for Doom maps, has accessors for them.

namespace BSPImporter {
	public class Edge : LumpObject {

		// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS

		private int firstVertex;
		private int secondVertex;

		// CONSTRUCTORS

		// This constructor takes all data in their proper data types
		public Edge(short inFirstVertex, short inSecondVertex)
			: base(new byte[0]) {
			firstVertex = (int)inFirstVertex;
			secondVertex = (int)inSecondVertex;
		}

		public Edge(int inFirstVertex, int inSecondVertex)
			: base(new byte[0]) {
			firstVertex = inFirstVertex;
			secondVertex = inSecondVertex;
		}

		// This constructor takes bytes in a byte array, as though
		// it had just been read by a FileInputStream.
		public Edge(byte[] data, mapType type)
			: base(data) {
			switch(type) {
				case mapType.TYPE_QUAKE:
				case mapType.TYPE_SIN:
				case mapType.TYPE_DAIKATANA:
				case mapType.TYPE_SOURCE17:
				case mapType.TYPE_SOURCE18:
				case mapType.TYPE_SOURCE19:
				case mapType.TYPE_SOURCE20:
				case mapType.TYPE_SOURCE21:
				case mapType.TYPE_SOURCE22:
				case mapType.TYPE_SOURCE23:
				case mapType.TYPE_SOURCE27:
				case mapType.TYPE_TACTICALINTERVENTION:
				case mapType.TYPE_DMOMAM:
				case mapType.TYPE_QUAKE2:
				case mapType.TYPE_SOF:
					firstVertex = DataReader.readUShort(data[0], data[1]);
					secondVertex = DataReader.readUShort(data[2], data[3]);
					break;
				case mapType.TYPE_VINDICTUS:
					firstVertex = DataReader.readInt(data[0], data[1], data[2], data[3]);
					secondVertex = DataReader.readInt(data[4], data[5], data[6], data[7]);
					break;
				case mapType.TYPE_DOOM:
				case mapType.TYPE_HEXEN:
					firstVertex = DataReader.readUShort(data[0], data[1]);
					secondVertex = DataReader.readUShort(data[2], data[3]);
					break;
			}
		}

		// METHODS
		public static Lump<Edge> createLump(byte[] data, mapType type) {
			int structLength = 0;
			switch(type) {
				case mapType.TYPE_QUAKE:
				case mapType.TYPE_SIN:
				case mapType.TYPE_DAIKATANA:
				case mapType.TYPE_SOURCE17:
				case mapType.TYPE_SOURCE18:
				case mapType.TYPE_SOURCE19:
				case mapType.TYPE_SOURCE20:
				case mapType.TYPE_SOURCE21:
				case mapType.TYPE_SOURCE22:
				case mapType.TYPE_SOURCE23:
				case mapType.TYPE_SOURCE27:
				case mapType.TYPE_TACTICALINTERVENTION:
				case mapType.TYPE_DMOMAM:
				case mapType.TYPE_QUAKE2:
				case mapType.TYPE_SOF:
					structLength = 4;
					break;
				case mapType.TYPE_VINDICTUS:
					structLength = 8;
					break;
				case mapType.TYPE_DOOM:
				case mapType.TYPE_HEXEN:
					structLength = 4;
					break;
			}
			int offset = 0;
			Lump<Edge> lump = new Lump<Edge>(data.Length, structLength, data.Length / structLength);
			byte[] bytes = new byte[structLength];
			for(int i = 0; i < data.Length / structLength; i++) {
				for(int j = 0; j < structLength; j++) {
					bytes[j] = data[offset + j];
				}
				lump.Add(new Edge(bytes, type));
				offset += structLength;
			}
			return lump;
		}

		// ACCESSORS/MUTATORS

		virtual public int FirstVertex {
			get {
				return firstVertex;
			}
			set {
				firstVertex = value;
			}
		}
		virtual public int SecondVertex {
			get {
				return secondVertex;
			}
			set {
				secondVertex = value;
			}
		}
		virtual public int NumSegs {
			get {
				return firstVertex;
			}
		}
		virtual public int FirstSeg {
			get {
				return secondVertex;
			}
		}
	}
}