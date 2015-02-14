using System;
// Brush class
// Tries to hold the data used by all formats of brush structure

public class Brush:LumpObject {

	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	// All four brush formats use some of these in some way
	private int firstSide = -1;
	private int numSides = -1;
	private int texture = -1;
	private byte[] contents;
	
	// CONSTRUCTORS
	public Brush(LumpObject oldObject, mapType type):base(oldObject.Data) {
		new Brush(oldObject.Data, type);
	}
	
	public Brush(byte[] data, mapType type):base(data) {
		switch (type) {
			case mapType.TYPE_QUAKE2: 
			case mapType.TYPE_DAIKATANA: 
			case mapType.TYPE_SIN: 
			case mapType.TYPE_SOF: 
			case mapType.TYPE_SOURCE17: 
			case mapType.TYPE_SOURCE18: 
			case mapType.TYPE_SOURCE19: 
			case mapType.TYPE_SOURCE20: 
			case mapType.TYPE_SOURCE21: 
			case mapType.TYPE_SOURCE22: 
			case mapType.TYPE_SOURCE23: 
			case mapType.TYPE_SOURCE27: 
			case mapType.TYPE_VINDICTUS: 
			case mapType.TYPE_TACTICALINTERVENTION: 
			case mapType.TYPE_DMOMAM: 
				firstSide = DataReader.readInt(data[0], data[1], data[2], data[3]);
				numSides = DataReader.readInt(data[4], data[5], data[6], data[7]);
				contents = new byte[]{data[8], data[9], data[10], data[11]};
				break;
			case mapType.TYPE_NIGHTFIRE: 
				contents = new byte[]{data[0], data[1], data[2], data[3]};
				firstSide = DataReader.readInt(data[4], data[5], data[6], data[7]);
				numSides = DataReader.readInt(data[8], data[9], data[10], data[11]);
				break;
			case mapType.TYPE_MOHAA: 
			case mapType.TYPE_STEF2DEMO: 
			case mapType.TYPE_RAVEN: 
			case mapType.TYPE_QUAKE3: 
			case mapType.TYPE_FAKK: 
				firstSide = DataReader.readInt(data[0], data[1], data[2], data[3]);
				numSides = DataReader.readInt(data[4], data[5], data[6], data[7]);
				texture = DataReader.readInt(data[8], data[9], data[10], data[11]);
				break;
			case mapType.TYPE_STEF2: 
				numSides = DataReader.readInt(data[0], data[1], data[2], data[3]);
				firstSide = DataReader.readInt(data[4], data[5], data[6], data[7]);
				texture = DataReader.readInt(data[8], data[9], data[10], data[11]);
				break;
			case mapType.TYPE_COD: 
			case mapType.TYPE_COD2: 
			case mapType.TYPE_COD4: 
				numSides = DataReader.readUShort(data[0], data[1]);
				texture = DataReader.readUShort(data[2], data[3]);
				break;
		}
	}

	// METHODS
	// createLump(byte[], uint)
	// Parses a byte array into a Lump object containing Brushes.
	public static Lump<Brush> createLump(byte[] inBytes, mapType type) {
		int structLength = 0;
		switch (type) {
			case mapType.TYPE_QUAKE2:
			case mapType.TYPE_DAIKATANA:
			case mapType.TYPE_SIN:
			case mapType.TYPE_SOF:
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
			case mapType.TYPE_NIGHTFIRE:
			case mapType.TYPE_STEF2:
			case mapType.TYPE_MOHAA:
			case mapType.TYPE_STEF2DEMO:
			case mapType.TYPE_RAVEN:
			case mapType.TYPE_QUAKE3:
			case mapType.TYPE_FAKK:
				structLength = 12;
				break;
			case mapType.TYPE_COD:
			case mapType.TYPE_COD2:
			case mapType.TYPE_COD4:
				structLength = 4;
				break;
		}
		int offset = 0;
		Lump<Brush> lump = new Lump<Brush>(inBytes.Length, structLength, inBytes.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < inBytes.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = inBytes[offset + j];
			}
			lump.Add(new Brush(bytes, type));
			offset += structLength;
		}
		return lump;
	}

	// ACCESSORS/MUTATORS
	virtual public int FirstSide {
		get {
			return firstSide;
		}
	}
	virtual public int NumSides {
		get {
			return numSides;
		}
	}
	virtual public int Texture {
		get {
			return texture;
		}
	}
	virtual public byte[] Contents {
		get {
			return contents;
		}
	}
}