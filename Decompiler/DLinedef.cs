using System;
// DLinedef class
// Contains all necessary information for a Doom LINEDEF object.
// The linedef is a strange animal. It roughly equates to the Planes of other
// BSP formats, but also defines what sectors are on what side.

public class DLinedef:LumpObject {
	
	// INITIAL DATA DEFINITION AND DECLARATION OF CONSTANTS
	
	private short start;
	private short end;
	private byte[] flags;
	private short action;
	private short tag;
	private short right;
	private short left;
	private short[] arguments = new short[5];
	
	// CONSTRUCTORS
	
	public DLinedef():base(new byte[0]) { }

	public DLinedef(byte[] data, mapType type):base(data) {
		start = DataReader.readShort(data[0], data[1]);
		end = DataReader.readShort(data[2], data[3]);
		flags = new byte[]{data[4], data[5]};
		switch (type) {
			case mapType.TYPE_DOOM: 
				action = DataReader.readShort(data[6], data[7]);
				tag = DataReader.readShort(data[8], data[9]);
				right = DataReader.readShort(data[10], data[11]);
				left = DataReader.readShort(data[12], data[13]);
				break;
			case mapType.TYPE_HEXEN: 
				action = data[6];
				arguments[0] = data[7];
				arguments[1] = data[8];
				arguments[2] = data[9];
				arguments[3] = data[10];
				arguments[4] = data[11];
				right = DataReader.readShort(data[12], data[13]);
				left = DataReader.readShort(data[14], data[15]);
				break;
		}
	}

	// METHODS

	public static Lump<DLinedef> createLump(byte[] data, mapType type) {
		int structLength = 0;
		switch (type) {
			case mapType.TYPE_DOOM: 
				structLength = 14;
				break;
			case mapType.TYPE_HEXEN: 
				structLength = 16;
				break;
		}
		int offset = 0;
		Lump<DLinedef> lump = new Lump<DLinedef>(data.Length, structLength, data.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new DLinedef(bytes, type));
			offset += structLength;
		}
		return lump;
	}

	// ACCESSORS AND MUTATORS

	virtual public bool OneSided {
		get {
			return (right == -1 || left == -1);
		}
	}
	virtual public short Start {
		get {
			return start;
		}
		set {
			start = value;
		}
	}
	virtual public short End {
		get {
			return end;
		}
		set {
			end = value;
		}
	}
	virtual public byte[] Flags {
		get {
			return flags;
		}
		set {
			flags = value;
		}
	}
	virtual public short Action {
		get {
			return action;
		}
	}
	virtual public short Tag {
		get {
			return tag;
		}
		set {
			tag = value;
		}
	}
	virtual public short Right {
		get {
			return right;
		}
		set {
			right = value;
		}
	}
	virtual public short Left {
		get {
			return left;
		}
		set {
			left = value;
		}
	}
	virtual public short[] Arguments {
		get {
			return arguments;
		}
	}
}