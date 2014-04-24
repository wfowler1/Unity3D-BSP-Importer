using System;
// DSector class
// Contains all necessary information for a Doom SECTOR object.
// The sector defines an area, the heights of the floor and cieling
// of the area, the floor and cieling textures, the light level, the
// type of sector and a tag number.

public class DSector:LumpObject {
	
	// INITIAL DATA DEFINITION AND DECLARATION OF CONSTANTS
	
	private short floor;
	private short cieling;
	
	private string floorTexture;
	private string cielingTexture;
	
	private short light;
	private short type;
	private short tag;
	
	// CONSTRUCTORS
	
	/*public DSector(short floor, short cieling, String floorTexture, String cielingTexture, short light, short type, short tag) {
	this.floor=floor;
	this.cieling=cieling;
	this.floorTexture=floorTexture;
	this.cielingTexture=cielingTexture;
	this.light=light;
	this.type=type;
	this.tag=tag;
	}*/
	
	public DSector():base(new byte[0]) { }

	public DSector(byte[] data):base(data) {
		floor = DataReader.readShort(data[0], data[1]);
		cieling = DataReader.readShort(data[2], data[3]);
		floorTexture = DataReader.readNullTerminatedString(new byte[]{data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11]});
		cielingTexture = DataReader.readNullTerminatedString(new byte[]{data[12], data[13], data[14], data[15], data[16], data[17], data[18], data[19]});
		light = DataReader.readShort(data[20], data[21]);
		type = DataReader.readShort(data[22], data[23]);
		tag = DataReader.readShort(data[24], data[25]);
	}
	
	// METHODS
	
	public static Lump<DSector> createLump(byte [] data) {
		int offset = 0;
		int structLength = 26;
		Lump<DSector> lump = new Lump<DSector>(data.Length, structLength, data.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new DSector(bytes));
			offset += structLength;
		}
		return lump;
	}
		
	// ACCESSORS AND MUTATORS

	public virtual short FloorHeight {
		get {
			return floor;
		}
		set {
			floor = value;
		}
	}
	public virtual short CielingHeight {
		get {
			return cieling;
		}
		set {
			cieling = value;
		}
	}
	public virtual string FloorTexture {
		get {
			return floorTexture;
		}
		set {
			floorTexture = value;
		}
	}
	public virtual string CielingTexture {
		get {
			return cielingTexture;
		}
		set {
			cielingTexture = value;
		}
	}
	public virtual short LightLevel {
		get {
			return light;
		}
		set {
			light = value;
		}
	}
	public virtual short Type {
		get {
			return type;
		}
		set {
			type = value;
		}
	}
	public virtual short Tag {
		get {
			return tag;
		}
		set {
			tag = value;
		}
	}
}