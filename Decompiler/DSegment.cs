using System;
// DSegment class

// This class holds data of a single Doom line Segment
// Not entirely sure why this structure exists. It's quite superfluous.

public class DSegment:LumpObject {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	private short startVertex;
	private short endVertex;
	private short angle;
	private short lineDef;
	private short direction; // 0 for right side of linedef, 1 for left
	private short dist;
	
	// CONSTRUCTORS
	
	// This one takes the components separate and in the correct data type
	/*public DSegment(short startVertex, short endVertex, short angle, short lineDef, short direction, short offset) {
	this.startVertex=startVertex;
	this.endVertex=endVertex;
	this.angle=angle;
	this.lineDef=lineDef;
	this.direction=direction;
	this.offset=offset;
	}*/

	public DSegment():base(new byte[0]) { }
	
	// This one takes an array of bytes (as if read directly from a file) and reads them
	// directly into the proper data types.
	public DSegment(byte[] data):base(data) {
		this.startVertex = DataReader.readShort(data[0], data[1]);
		this.endVertex = DataReader.readShort(data[2], data[3]);
		this.angle = DataReader.readShort(data[4], data[5]);
		this.lineDef = DataReader.readShort(data[6], data[7]);
		this.direction = DataReader.readShort(data[8], data[9]);
		this.dist = DataReader.readShort(data[10], data[11]);
	}
	
	// METHODS
	public static Lump<DSegment> createLump(byte[] data) {
		int structLength = 12;
		int offset = 0;
		Lump<DSegment> lump = new Lump<DSegment>(data.Length, structLength, data.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new DSegment(bytes));
			offset += structLength;
		}
		return lump;
	}

	// ACCESSORS/MUTATORS

	virtual public short StartVertex {
		get {
			return startVertex;
		}
		set {
			startVertex = value;
		}
	}
	virtual public short EndVertex {
		get {
			return endVertex;
		}
		set {
			endVertex = value;
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
	virtual public short Linedef {
		get {
			return lineDef;
		}
		set {
			lineDef = value;
		}
	}
	virtual public short Direction {
		get {
			return direction;
		}
		set {
			direction = value;
		}
	}
	virtual public short Dist {
		get {
			return dist;
		}
		set {
			dist = value;
		}
	}
}