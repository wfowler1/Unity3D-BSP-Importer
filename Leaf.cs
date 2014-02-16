// Leaf class
// Master class for leaf structures. Only four formats needs leaves in order to be
// decompiled; Source, Nightfire, Quake and Quake 2.

public class Leaf:LumpObject {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	// In some formats (Quake 3 is the notable exclusion), leaves must be used to find
	// a list of brushes or faces to create solids out of.
	private byte[] contents;
	private int firstMarkBrush=-1;
	private int numMarkBrushes=-1;
	private int firstMarkFace=-1;
	private int numMarkFaces=-1;
	
	// CONSTRUCTORS
	public Leaf(LumpObject data, mapType type):base(data.Data) {
		new Leaf(data.Data, type);
	}
	
	public Leaf(byte[] data, mapType type):base(data) {
		switch(type) {
			case mapType.TYPE_SOF:
				contents=new byte[] { data[0], data[1], data[2], data[3] };
				firstMarkFace=DataReader.readUShort(data[22], data[23]);
				numMarkFaces=DataReader.readUShort(data[24], data[25]);
				firstMarkBrush=DataReader.readUShort(data[26], data[27]);
				numMarkBrushes=DataReader.readUShort(data[28], data[29]);
				break;
			case mapType.TYPE_QUAKE2:
			case mapType.TYPE_SIN:
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
			case mapType.TYPE_DAIKATANA:
				contents=new byte[] { data[0], data[1], data[2], data[3] };
				firstMarkBrush=DataReader.readUShort(data[24], data[25]);
				numMarkBrushes=DataReader.readUShort(data[26], data[27]);
				goto case mapType.TYPE_QUAKE;
			case mapType.TYPE_QUAKE:
				firstMarkFace=DataReader.readUShort(data[20], data[21]);
				numMarkFaces=DataReader.readUShort(data[22], data[23]);
				break;
			case mapType.TYPE_VINDICTUS:
				contents=new byte[] { data[0], data[1], data[2], data[3] };
				firstMarkBrush=DataReader.readInt(data[44], data[45], data[46], data[47]);
				numMarkBrushes=DataReader.readInt(data[48], data[49], data[50], data[51]);
				break;
			case mapType.TYPE_NIGHTFIRE:
				contents=new byte[] { data[0], data[1], data[2], data[3] };
				goto case mapType.TYPE_RAVEN;
			case mapType.TYPE_QUAKE3:
			case mapType.TYPE_FAKK:
			case mapType.TYPE_STEF2DEMO:
			case mapType.TYPE_STEF2:
			case mapType.TYPE_MOHAA:
			case mapType.TYPE_RAVEN:
				firstMarkFace=DataReader.readInt(data[32], data[33], data[34], data[35]);
				numMarkFaces=DataReader.readInt(data[36], data[37], data[38], data[39]);
				firstMarkBrush=DataReader.readInt(data[40], data[41], data[42], data[43]);
				numMarkBrushes=DataReader.readInt(data[44], data[45], data[46], data[47]);
				break;
		}
	}
	
	// METHODS
	public static Lump<Leaf> createLump(byte[] data, mapType type) {
		int structLength=0;
		switch(type) {
			case mapType.TYPE_QUAKE:
			case mapType.TYPE_QUAKE2:
			case mapType.TYPE_SIN:
				structLength=28;
				break;
			case mapType.TYPE_SOURCE17:
			case mapType.TYPE_SOURCE20:
			case mapType.TYPE_SOURCE21:
			case mapType.TYPE_SOURCE22:
			case mapType.TYPE_SOURCE23:
			case mapType.TYPE_SOURCE27: 
			case mapType.TYPE_TACTICALINTERVENTION:
			case mapType.TYPE_SOF:
			case mapType.TYPE_DAIKATANA:
			case mapType.TYPE_DMOMAM:
				structLength=32;
				break;
			case mapType.TYPE_VINDICTUS:
				structLength=56;
				break;
			case mapType.TYPE_COD:
				structLength=36;
				break;
			case mapType.TYPE_NIGHTFIRE:
			case mapType.TYPE_QUAKE3:
			case mapType.TYPE_FAKK:
			case mapType.TYPE_STEF2DEMO:
			case mapType.TYPE_STEF2:
			case mapType.TYPE_RAVEN:
				structLength=48;
				break;
			case mapType.TYPE_SOURCE18:
			case mapType.TYPE_SOURCE19:
				structLength=56;
				break;
			case mapType.TYPE_MOHAA:
				structLength=64;
				break;
			default:
				structLength=0; // This will cause the shit to hit the fan.
				break;
		}
		int offset=0;
		Lump<Leaf> lump = new Lump<Leaf>(data.Length, structLength, data.Length / structLength);
		byte[] bytes=new byte[structLength];
		for(int i=0;i<data.Length / structLength;i++) {
			for(int j=0;j<structLength;j++) {
				bytes[j]=data[offset+j];
			}
			lump.Add(new Leaf(bytes, type));
			offset+=structLength;
		}
		return lump;
	}
	
	// ACCESSORS/MUTATORS
	public virtual byte[] Contents {
		get {
			return contents;
		}
	}
	public virtual int FirstMarkBrush {
		get {
			return firstMarkBrush;
		}
	}
	public virtual int NumMarkBrushes {
		get {
			return numMarkBrushes;
		}
	}
	public virtual int FirstMarkFace {
		get {
			return firstMarkFace;
		}
	}
	public virtual int NumMarkFaces {
		get {
			return numMarkFaces;
		}
	}
}