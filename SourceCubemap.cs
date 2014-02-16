using System;
using UnityEngine;
// SourceCubemap class

public class SourceCubemap:LumpObject {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	private Vector3 origin;
	private int size;
	
	// CONSTRUCTORS
	public SourceCubemap(LumpObject data, mapType type):base(data.Data) {
		new SourceCubemap(data.Data, type);
	}
	
	public SourceCubemap(byte[] data, mapType type):base(data) {
		switch (type) {
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
				origin = new Vector3(DataReader.readInt(data[0], data[1], data[2], data[3]), DataReader.readInt(data[4], data[5], data[6], data[7]), DataReader.readInt(data[8], data[9], data[10], data[11]));
				size = DataReader.readInt(data[12], data[13], data[14], data[15]);
				break;
		}
	}
	
	// METHODS
	public static Lump<SourceCubemap> createLump(byte[] data, mapType type) {
		int structLength = 0;
		switch (type) {
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
				structLength = 16;
				break;
			default: 
				structLength = 0; // This will cause the shit to hit the fan.
				break;
		}
		int offset = 0;
		Lump<SourceCubemap> lump = new Lump<SourceCubemap>(data.Length, structLength, data.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new SourceCubemap(bytes, type));
			offset += structLength;
		}
		return lump;
	}

	// ACCESSORS/MUTATORS
	virtual public Vector3 Origin {
		get {
			return origin;
		}
	}

	virtual public int Size {
		get {
			return size;
		}
	}
}