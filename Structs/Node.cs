using System;
// Node class
// Contains all data needed for a node in a BSP tree. Should be usable by any format.

namespace BSPImporter {
	public class Node : LumpObject {
		// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS

		private int plane = -1;
		private int child1 = 0; // Negative values are valid here. However, the child can never be zero,
		private int child2 = 0; // since that would reference the head node causing an infinite loop.

		// CONSTRUCTORS
		public Node(LumpObject data, mapType type)
			: base(data.Data) {
			new Node(data.Data, type);
		}

		public Node(byte[] data, mapType type)
			: base(data) {
			this.plane = DataReader.readInt(data[0], data[1], data[2], data[3]); // All formats I've seen use the first 4 bytes as an int, plane index
			switch(type) {
				// I don't actually need to read or store node information for most of these formats.
				// Support for them is only provided for completeness and consistency.
				case mapType.TYPE_QUAKE:
					this.child1 = (int)DataReader.readShort(data[4], data[5]);
					this.child2 = (int)DataReader.readShort(data[6], data[7]);
					break;
				// Nightfire, Source, Quake 2 and Quake 3-based engines all use the first three ints for planenum and children
				case mapType.TYPE_SIN:
				case mapType.TYPE_SOF:
				case mapType.TYPE_QUAKE2:
				case mapType.TYPE_DAIKATANA:
				case mapType.TYPE_NIGHTFIRE:
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
				case mapType.TYPE_STEF2:
				case mapType.TYPE_MOHAA:
				case mapType.TYPE_STEF2DEMO:
				case mapType.TYPE_RAVEN:
				case mapType.TYPE_QUAKE3:
				case mapType.TYPE_FAKK:
				case mapType.TYPE_COD:
					this.child1 = DataReader.readInt(data[4], data[5], data[6], data[7]);
					this.child2 = DataReader.readInt(data[8], data[9], data[10], data[11]);
					break;
			}
		}

		// METHODS

		public static Lump<Node> createLump(byte[] data, mapType type) {
			int structLength = 0;
			switch(type) {
				case mapType.TYPE_QUAKE:
					structLength = 24;
					break;
				case mapType.TYPE_QUAKE2:
				case mapType.TYPE_SIN:
				case mapType.TYPE_SOF:
				case mapType.TYPE_DAIKATANA:
					structLength = 28;
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
				case mapType.TYPE_DMOMAM:
					structLength = 32;
					break;
				case mapType.TYPE_VINDICTUS:
					structLength = 48;
					break;
				case mapType.TYPE_QUAKE3:
				case mapType.TYPE_FAKK:
				case mapType.TYPE_COD:
				case mapType.TYPE_STEF2:
				case mapType.TYPE_STEF2DEMO:
				case mapType.TYPE_MOHAA:
				case mapType.TYPE_RAVEN:
				case mapType.TYPE_NIGHTFIRE:
					structLength = 36;
					break;
				default:
					structLength = 0; // This will cause the shit to hit the fan.
					break;
			}
			int offset = 0;
			Lump<Node> lump = new Lump<Node>(data.Length, structLength, data.Length / structLength);
			byte[] bytes = new byte[structLength];
			for(int i = 0; i < data.Length / structLength; i++) {
				for(int j = 0; j < structLength; j++) {
					bytes[j] = data[offset + j];
				}
				lump.Add(new Node(bytes, type));
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
		virtual public int Child1 {
			get {
				return child1;
			}
		}
		virtual public int Child2 {
			get {
				return child2;
			}
		}
	}
}