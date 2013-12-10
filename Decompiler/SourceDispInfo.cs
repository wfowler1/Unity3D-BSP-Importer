using System;
// SourceDispInfo class

// Holds all the data for a displacement in a Source map.

public class SourceDispInfo:LumpObject {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	// At this point, screw it, I'm just copying names from the Valve developer wiki Source BSP documentation page
	private Vector3D startPosition;
	private int dispVertStart;
	//private int dispTriStart;
	private int power;
	private uint[] allowedVerts;
	
	// CONSTRUCTORS
	
	// This constructor takes bytes in a byte array, as though
	// it had just been read by a FileInputStream.
	public SourceDispInfo(byte[] data, mapType type):base(data) {
		startPosition = DataReader.readPoint3F(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11]);
		dispVertStart = DataReader.readInt(data[12], data[13], data[14], data[15]);
		//dispTriStart=DataReader.readInt(in[16], in[17], in[18], in[19]);
		power = DataReader.readInt(data[20], data[21], data[22], data[23]);
		allowedVerts = new uint[10];
		int offset = 0;
		switch (type) {
			case mapType.TYPE_SOURCE17: 
			case mapType.TYPE_SOURCE18: 
			case mapType.TYPE_SOURCE19: 
			case mapType.TYPE_SOURCE20: 
			case mapType.TYPE_SOURCE21: 
			case mapType.TYPE_SOURCE27: 
			case mapType.TYPE_TACTICALINTERVENTION: 
			case mapType.TYPE_DMOMAM: 
				offset = 136;
				break;
			case mapType.TYPE_SOURCE22: 
				offset = 140;
				break;
			case mapType.TYPE_SOURCE23: 
				offset = 144;
				break;
			case mapType.TYPE_VINDICTUS: 
				offset = 192;
				break;
		}
		for (int i = 0; i < 10; i++) {
			allowedVerts[i] = DataReader.readUInt(data[offset + (i * 4)], data[offset + 1 + (i * 4)], data[offset + 2 + (i * 4)], data[offset + 3 + (i * 4)]);
		}
	}
	
	// METHODS
	public static Lump<SourceDispInfo> createLump(byte[] data, mapType type) {
		int structLength = 0;
		switch (type) {
			case mapType.TYPE_SOURCE17: 
			case mapType.TYPE_SOURCE18: 
			case mapType.TYPE_SOURCE19: 
			case mapType.TYPE_SOURCE20: 
			case mapType.TYPE_SOURCE21: 
			case mapType.TYPE_SOURCE27: 
			case mapType.TYPE_TACTICALINTERVENTION: 
			case mapType.TYPE_DMOMAM: 
				structLength = 176;
				break;
			case mapType.TYPE_SOURCE22: 
				structLength = 180;
				break;
			case mapType.TYPE_SOURCE23: 
				structLength = 184;
				break;
			case mapType.TYPE_VINDICTUS: 
				structLength = 232;
				break;
			default: 
				structLength = 0; // This will cause the shit to hit the fan.
				break;
		}
		int offset = 0;
		Lump<SourceDispInfo> lump = new Lump<SourceDispInfo>(data.Length, structLength, data.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new SourceDispInfo(bytes, type));
			offset += structLength;
		}
		return lump;
	}

	// ACCESSORS/MUTATORS
	
	virtual public Vector3D StartPosition {
		get {
			return startPosition;
		}
		set {
			startPosition = value;
		}
	}

	virtual public int DispVertStart {
		get {
			return dispVertStart;
		}
		set {
			dispVertStart = value;
		}
	}

	virtual public int Power {
		get {
			return power;
		}
		set {
			power = value;
		}
	}

	virtual public uint[] AllowedVerts {
		get {
			return allowedVerts;
		}
		set {
			allowedVerts = value;
		}
	}

	/*virtual public int DispTriStart {
		get {
			return dispTriStart;
		}
		set {
			dispTriStart = value;
		}
	}*/
}