using System;
using System.Collections.Generic;
using UnityEngine;
// SourceDispVertex class

// Holds all the data for a displacement in a Source map.

public class SourceDispVertex:LumpObject {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	// At this point, screw it, I'm just copying names from the Valve developer wiki Source BSP documentation page
	private Vector3 normal; // The normalized vector direction this vertex points from "flat"
	private float dist; // Magnitude of normal, before normalization
	private float alpha; // Alpha value of texture at this vertex
	
	// CONSTRUCTORS
	
	// This constructor takes 20 bytes in a byte array, as though
	// it had just been read by a FileInputStream.
	public SourceDispVertex(byte[] data):base(data) {
		this.normal = DataReader.readPoint3F(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11]);
		this.dist = DataReader.readFloat(data[12], data[13], data[14], data[15]);
		this.alpha = DataReader.readFloat(data[16], data[17], data[18], data[19]);
	}
	
	// METHODS
	public static SourceDispVertices createLump(byte[] data) {
		int offset = 0;
		int structLength = 20;
		SourceDispVertices lump = new SourceDispVertices(new List<SourceDispVertex>(data.Length / structLength), data.Length);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new SourceDispVertex(bytes));
			offset += structLength;
		}
		return lump;
	}

	// ACCESSORS/MUTATORS
	virtual public Vector3 Normal {
		get {
			return normal;
		}
		set {
			normal = value;
		}
	}

	virtual public float Dist {
		get {
			return dist;
		}
		set {
			dist = value;
		}
	}

	virtual public float Alpha {
		get {
			return alpha;
		}
		set {
			alpha = value;
		}
	}
}