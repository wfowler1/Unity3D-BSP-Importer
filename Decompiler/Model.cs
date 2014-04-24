using System;
// Model class
// An attempt at an all-encompassing model class containing all data needed for any
// given models lump in any given BSP. This is accomplished by throwing out all
// unnecessary information and keeping any relevant information.
//
// Some BSP formats hold the relevant information in different ways, so this will
// will handle any given format and will always be sufficient in one way or another
// to point to all the necessary data.

public class Model:LumpObject {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	// In general, we need to use models to find one or more leaves containing the
	// information for the solids described by this model. Some formats do it by
	// referencing a head node to iterate through and find the leaves. Others
	// directly point to a set of leaves, and still others simply directly reference
	// brushes. The ideal format simply points to brush information from here (Quake
	// 3-based engines do), but most of them don't.
	private int headNode = - 1; // Quake, Half-life, Quake 2, SiN
	private int firstLeaf = - 1; // 007 nightfire
	private int numLeaves = - 1;
	private int firstBrush = - 1; // Quake 3 and derivatives
	private int numBrushes = - 1;
	private int firstFace = - 1; // Quake/GoldSrc
	private int numFaces = - 1;
	
	// CONSTRUCTORS

	public Model():base(new byte[0]) { }

	public Model(LumpObject data, mapType type):base(data.Data) {
		new Model(data.Data, type);
	}
	
	public Model(byte[] data, mapType type):base(data) {
		switch(type) {
			case mapType.TYPE_QUAKE: 
				firstFace = DataReader.readInt(data[56], data[57], data[58], data[59]);
				numFaces = DataReader.readInt(data[60], data[61], data[62], data[63]);
				goto case mapType.TYPE_QUAKE2;
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
				// In all these formats, the "head node" index comes after 9 floats.
				headNode = DataReader.readInt(data[36], data[37], data[38], data[39]);
				break;
			case mapType.TYPE_DMOMAM: 
				headNode = DataReader.readInt(data[40], data[41], data[42], data[43]);
				break;
			case mapType.TYPE_NIGHTFIRE: 
				firstLeaf = DataReader.readInt(data[40], data[41], data[42], data[43]);
				numLeaves = DataReader.readInt(data[44], data[45], data[46], data[47]);
				break;
			case mapType.TYPE_STEF2: 
			case mapType.TYPE_STEF2DEMO: 
			case mapType.TYPE_QUAKE3: 
			case mapType.TYPE_MOHAA: 
			case mapType.TYPE_RAVEN: 
			case mapType.TYPE_FAKK: 
				firstBrush = DataReader.readInt(data[32], data[33], data[34], data[35]);
				numBrushes = DataReader.readInt(data[36], data[37], data[38], data[39]);
				break;
			case mapType.TYPE_COD: 
			case mapType.TYPE_COD2: 
			case mapType.TYPE_COD4: 
				firstBrush = DataReader.readInt(data[40], data[41], data[42], data[43]);
				numBrushes = DataReader.readInt(data[44], data[45], data[46], data[47]);
				break;
		}
	}
	
	// METHODS
	public static Lump<Model> createLump(byte[] data, mapType type) {
		int structLength = 0;
		switch (type) {
			case mapType.TYPE_QUAKE3: 
			case mapType.TYPE_RAVEN: 
			case mapType.TYPE_STEF2: 
			case mapType.TYPE_STEF2DEMO: 
			case mapType.TYPE_MOHAA: 
			case mapType.TYPE_FAKK: 
				structLength = 40;
				break;
			case mapType.TYPE_QUAKE2: 
			case mapType.TYPE_DAIKATANA: 
			case mapType.TYPE_COD: 
			case mapType.TYPE_COD2: 
			case mapType.TYPE_COD4: 
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
				structLength = 48;
				break;
			case mapType.TYPE_DMOMAM: 
				structLength = 52;
				break;
			case mapType.TYPE_NIGHTFIRE: 
				structLength = 56;
				break;
			case mapType.TYPE_QUAKE: 
				structLength = 64;
				break;
		}
		int offset = 0;
		Lump<Model> lump = new Lump<Model>(data.Length, structLength, data.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new Model(bytes, type));
			offset += structLength;
		}
		return lump;
	}
	
	// ACCESSORS/MUTATORS
	virtual public int HeadNode {
		get {
			return headNode;
		}
	}
	
	virtual public int FirstLeaf {
		get {
			return firstLeaf;
		}
	}
	
	virtual public int NumLeaves {
		get {
			return numLeaves;
		}
	}
	
	virtual public int FirstBrush {
		get {
			return firstBrush;
		}
	}
	
	virtual public int NumBrushes {
		get {
			return numBrushes;
		}
	}
	
	virtual public int FirstFace {
		get {
			return firstFace;
		}
	}
	
	virtual public int NumFaces {
		get {
			return numFaces;
		}
	}
}