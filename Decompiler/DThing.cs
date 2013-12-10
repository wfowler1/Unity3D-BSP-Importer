using System;
// DThing class
// Contains all necessary information for a Doom THING object.
// Essentially is Doom's entities. You could make the argument that
// this takes less space than the conventional entities lump. And you
// would be correct. But it is incredibly esoteric and non-expandable.
// Some games DID expand it (such as Hexen or Strife), but they had to
// increase the data size to do it. And it was a pain in their ass.

public class DThing:LumpObject {
	
	// INITIAL DATA DEFINITION AND DECLARATION OF CONSTANTS
	
	private Vector3D origin;
	private short angle;
	private short classNum;
	private short flags;
	
	private short id;
	private short action;
	private short[] arguments = new short[5];
	
	// CONSTRUCTORS
	
	public DThing(byte[] data, mapType type):base(data) {
		switch (type) {
			case mapType.TYPE_DOOM: 
				origin = new Vector3D(DataReader.readShort(data[0], data[1]), DataReader.readShort(data[2], data[3]));
				this.angle = DataReader.readShort(data[4], data[5]);
				this.classNum = DataReader.readShort(data[6], data[7]);
				this.flags = DataReader.readShort(data[8], data[9]);
				break;
			case mapType.TYPE_HEXEN: 
				id = DataReader.readShort(data[0], data[1]);
				origin = new Vector3D(DataReader.readShort(data[2], data[3]), DataReader.readShort(data[4], data[5]), DataReader.readShort(data[6], data[7]));
				this.angle = DataReader.readShort(data[8], data[9]);
				this.classNum = DataReader.readShort(data[10], data[11]);
				this.flags = DataReader.readShort(data[12], data[13]);
				action = data[14];
				arguments[0] = data[15];
				arguments[1] = data[16];
				arguments[2] = data[17];
				arguments[3] = data[18];
				arguments[4] = data[19];
				break;
		}
	}
	
	// METHODS
	//UPGRADE_ISSUE: The following fragment of code could not be parsed and was not converted. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextSettingsIndex'&keyword='jlca1156'"
	public static Lump<DThing> createLump(byte[] data, mapType type) {
		int structLength = 0;
		switch (type) {
			case mapType.TYPE_DOOM: 
				structLength = 10;
				break;
			case mapType.TYPE_HEXEN: 
				structLength = 20;
				break;
		}
		int offset = 0;
		Lump<DThing> lump = new Lump<DThing>(data.Length, structLength, data.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new DThing(bytes, type));
			offset += structLength;
		}
		return lump;
	}
	
	// ACCESSORS AND MUTATORS

	virtual public double OriginX {
		get {
			return origin.X;
		}
		set {
			origin.X = value;
		}
	}
	virtual public double OriginY {
		get {
			return origin.Y;
		}
		set {
			origin.Y = value;
		}
	}
	virtual public double OriginZ {
		// We're gonna need to worry about the Z, particularly in a mutator, since
		// we'll have to set a Z coordinate later on.
		get {
			return origin.Z;
		}
		set {
			origin.Z = value;
		}
	}
	virtual public Vector3D Origin {
		get {
			return origin;
		}
		set {
			origin = value;
		}
	}
	virtual public short Angle {
		get {
			return angle;
		}
		set {
			angle = value;
		}
	}
	virtual public short ClassNum {
		get {
			return classNum;
		}
		set {
			classNum = value;
		}
	}
	virtual public short Flags {
		get {
			return flags;
		}
		set {
			flags = value;
		}
	}
	virtual public short ID {
		get {
			return id;
		}
	}
	virtual public short Action {
		get {
			return action;
		}
	}
	virtual public short[] Arguments {
		get {
			return arguments;
		}
	}
}