using System;
// DSidedef class
// Contains all necessary information for a Doom SIDEDEF object.
// The sidedef is roughly equivalent to the Face (or surface)
// object in later BSP versions.
// This is one of the cleverest, dumbest, craziest structures
// I've ever seen in a game map format. It contains three texture
// references, and how they are used depends on adjacent sectors.

public class DSidedef:LumpObject {
	
	// INITIAL DATA DEFINITION AND DECLARATION OF CONSTANTS
	
	private short[] offsets;
	private string[] textures;
	private short sector;
	
	public const int HIGH = 0;
	public const int MID = 2;
	public const int LOW = 1;
	
	public const int X = 0;
	public const int Y = 1;
	
	// CONSTRUCTORS

	public DSidedef():base(new byte[0]) { }
	
	public DSidedef(byte[] data):base(data) {
		offsets = new short[2];
		offsets[X] = DataReader.readShort(data[0], data[1]);
		offsets[Y] = DataReader.readShort(data[2], data[3]);
		textures = new string[3];
		for (int i = 0; i < 3; i++) {
			textures[i] = "";
			for (int j = 0; j < 8; j++) {
				if (data[(i * 8) + j + 4] != (sbyte) 0x00) {
					textures[i] += (char) data[(i * 8) + j + 4];
				} else {
					break;
				}
			}
		}
		sector = DataReader.readShort(data[28], data[29]);
	}
	
	// METHODS

	public static Lump<DSidedef> createLump(byte[] data) {
		int offset = 0;
		int structLength = 30;
		Lump<DSidedef> lump = new Lump<DSidedef>(data.Length, structLength, data.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new DSidedef(bytes));
			offset += structLength;
		}
		return lump;
	}
	
	// ACCESSORS AND MUTATORS

	virtual public short OffsetX {
		get {
			return offsets[X];
		}
		set {
			offsets[X] = value;
		}
	}
	virtual public short OffsetY {
		get {
			return offsets[Y];
		}
		set {
			offsets[Y] = value;
		}
	}
	virtual public string HighTexture {
		get {
			return textures[HIGH];
		}
		set {
			textures[HIGH] = value;
		}
	}
	virtual public string MidTexture {
		get {
			return textures[MID];
		}
		set {
			textures[MID] = value;
		}
	}
	virtual public string LowTexture {
		get {
			return textures[LOW];
		}
		set {
			textures[LOW] = value;
		}
	}
	virtual public short Sector {
		get {
			return sector;
		}
		set {
			sector = value;
		}
	}
}