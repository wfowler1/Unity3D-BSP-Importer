using System;
// BrushSide class

public class BrushSide:LumpObject {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	private int plane = - 1;
	private float dist = System.Single.NaN;
	private int texture = - 1; // This is a valid texture index in Quake 2. However it means "unused" there
	private int face = - 1;
	private int displacement = - 1; // In theory, this should always point to the side's displacement info. In practice, displacement brushes are removed on compile, leaving only the faces.
	private sbyte bevel = - 1;
	
	// CONSTRUCTORS
	public BrushSide():base(new byte[0]) { }

	public BrushSide(LumpObject copy, mapType type):base(copy.Data) {
		new BrushSide(copy.Data, type);
	}
	
	public BrushSide(byte[] data, mapType type):base(data) {
		switch (type) {
			case mapType.TYPE_COD: 
			// Call of Duty's format sucks. The first field is either a float or an int
			// depending on whether or not it's one of the first six sides in a brush.
			case mapType.TYPE_COD2: 
			case mapType.TYPE_COD4: 
				dist = DataReader.readFloat(data[0], data[1], data[2], data[3]);
				goto case mapType.TYPE_QUAKE3;
			case mapType.TYPE_QUAKE3: 
			case mapType.TYPE_FAKK: 
			case mapType.TYPE_STEF2DEMO: 
			case mapType.TYPE_MOHAA: 
				plane = DataReader.readInt(data[0], data[1], data[2], data[3]);
				texture = DataReader.readInt(data[4], data[5], data[6], data[7]);
				break;
			case mapType.TYPE_STEF2: 
				texture = DataReader.readInt(data[0], data[1], data[2], data[3]);
				plane = DataReader.readInt(data[4], data[5], data[6], data[7]);
				break;
			case mapType.TYPE_RAVEN: 
				plane = DataReader.readInt(data[0], data[1], data[2], data[3]);
				texture = DataReader.readInt(data[4], data[5], data[6], data[7]);
				face = DataReader.readInt(data[8], data[9], data[10], data[11]);
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
				this.displacement = DataReader.readShort(data[4], data[5]);
				this.bevel = (sbyte)data[6]; // In little endian format, this byte takes the least significant bits of a short
				// and can therefore be used for all Source engine formats, including Portal 2.
				goto case mapType.TYPE_SIN;
			case mapType.TYPE_SIN: 
			case mapType.TYPE_QUAKE2: 
			case mapType.TYPE_DAIKATANA: 
			case mapType.TYPE_SOF: 
				plane = DataReader.readUShort(data[0], data[1]);
				texture = (int) DataReader.readShort(data[2], data[3]);
				break;
			case mapType.TYPE_VINDICTUS: 
				plane = DataReader.readInt(data[0], data[1], data[2], data[3]);
				texture = DataReader.readInt(data[4], data[5], data[6], data[7]);
				displacement = DataReader.readInt(data[8], data[9], data[10], data[11]);
				bevel = (sbyte)data[12];
				break;
			case mapType.TYPE_NIGHTFIRE: 
				face = DataReader.readInt(data[0], data[1], data[2], data[3]);
				plane = DataReader.readInt(data[4], data[5], data[6], data[7]);
				break;
		}
	}
	
	// METHODS

	public static Lump<BrushSide> createLump(byte[] data, mapType type) {
		int structLength = 0;
		switch (type) {
			case mapType.TYPE_QUAKE2: 
			case mapType.TYPE_DAIKATANA: 
			case mapType.TYPE_SOF: 
				structLength = 4;
				break;
			case mapType.TYPE_COD: 
			case mapType.TYPE_COD2: 
			case mapType.TYPE_COD4: 
			case mapType.TYPE_SIN: 
			case mapType.TYPE_NIGHTFIRE: 
			case mapType.TYPE_QUAKE3: 
			case mapType.TYPE_STEF2: 
			case mapType.TYPE_STEF2DEMO: 
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
			case mapType.TYPE_FAKK: 
				structLength = 8;
				break;
			case mapType.TYPE_MOHAA: 
			case mapType.TYPE_RAVEN: 
				structLength = 12;
				break;
			case mapType.TYPE_VINDICTUS: 
				structLength = 16;
				break;
			default: 
				structLength = 0; // This will cause the shit to hit the fan.
				break;
		}
		int offset = 0;
		Lump<BrushSide> lump = new Lump<BrushSide>(data.Length, structLength, data.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new BrushSide(bytes, type));
			offset += structLength;
		}
		return lump;
	}
	
	public virtual sbyte isBevel() {
		return bevel;
	}

	// ACCESSORS/MUTATORS

	virtual public float Dist {
		get {
			return dist;
		}
	}

	virtual public int Plane {
		get {
			return plane;
		}
	}

	virtual public int Texture {
		get {
			return texture;
		}
	}

	virtual public int Face {
		get {
			return face;
		}
	}

	virtual public int Displacement {
		get {
			return displacement;
		}
	}
}