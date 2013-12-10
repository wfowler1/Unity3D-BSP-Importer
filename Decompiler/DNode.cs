using System;
// DNode class

// Holds all the data for a node in a Doom map.
// This is the one lump that has a structure similar to future BSPs.

// Though all the coordinates are handled as 2D 16-bit shortwords, I'm going to
// automatically convert everything to work with my established Vector3D.
// This simplifies my code by not needing another vector class for 2D coordinates
// defined by signed 16-bit numbers while still using the values.

public class DNode:LumpObject {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	private Vector3D vecHead; // This format uses a vector head and tail for partitioning, rather
	private Vector3D vecTail; // than the 3D plane conventionally used by more modern engines.
	// The "tail" is actually a change in X and Y, rather than an explicitly defined point.
	private Vector3D[] RRectangle = new Vector3D[2]; // The stupid thing is, these rectangles are defined by
	private Vector3D[] LRectangle = new Vector3D[2]; // top, bottom, left, right. That's YMax, YMin, XMin, XMax, in that order
	private short RChild; // Since partitioning is done using a vector, there is still the idea of
	private short LChild; // directionality. Therefore, two subnodes can consistently be called "right" or "left"
	
	// XYZ, PDQ
	public static int X = 0;
	public static int Y = 1;
	public static int Z = 2;
	
	public static int MINS = 0;
	public static int MAXS = 1;
	
	// CONSTRUCTORS
	
	// This constructor takes all data in their proper data types
	/*public DNode(short segHeadX, short segHeadY, short segTailX, short segTailY, short RRectTop,
	short RRectBottom, short RRectLeft, short RRectRight, short LRectTop, short LRectBottom,
	short LRectLeft, short LRectRight, short RChild, short LChild) {
	vecHead=new Vector3D(segHeadX, segHeadY);
	vecTail=new Vector3D(segTailX, segTailY);
	Vector3D RMins=new Vector3D(RRectLeft, RRectBottom);
	Vector3D RMaxs=new Vector3D(RRectRight, RRectTop);
	Vector3D LMins=new Vector3D(LRectLeft, LRectBottom);
	Vector3D LMaxs=new Vector3D(LRectRight, LRectTop);
	RRectangle= new Vector3D[2];
	RRectangle[0] = RMins;
	RRectangle[1] = RMaxs;
	LRectangle= new Vector3D[2];
	LRectangle[0] = LMins;
	LRectangle[1] = LMaxs;
	this.RChild=RChild;
	this.LChild=LChild;
	}*/
	
	// This constructor takes in a byte array, as though
	// it had just been read by a FileInputStream.
	public DNode(byte[] data):base(data) {
		vecHead = new Vector3D(DataReader.readShort(data[0], data[1]), DataReader.readShort(data[2], data[3]));
		vecTail = new Vector3D(DataReader.readShort(data[4], data[5]), DataReader.readShort(data[6], data[7]));
		Vector3D RMins = new Vector3D(DataReader.readShort(data[12], data[13]), DataReader.readShort(data[10], data[11]));
		Vector3D RMaxs = new Vector3D(DataReader.readShort(data[14], data[15]), DataReader.readShort(data[8], data[9]));
		Vector3D LMins = new Vector3D(DataReader.readShort(data[20], data[21]), DataReader.readShort(data[18], data[19]));
		Vector3D LMaxs = new Vector3D(DataReader.readShort(data[22], data[23]), DataReader.readShort(data[16], data[17]));
		RRectangle = new Vector3D[2];
		RRectangle[0] = RMins;
		RRectangle[1] = RMaxs;
		LRectangle = new Vector3D[2];
		LRectangle[0] = LMins;
		LRectangle[1] = LMaxs;
		this.RChild = DataReader.readShort(data[24], data[25]);
		this.LChild = DataReader.readShort(data[26], data[27]);
	}
	
	// METHODS
	
	// intersectsBox(Vertex mins, Vertex maxs)
	// Determines if this node's partition vector (as a line segment) intersects the passed box.
	// Seems rather esoteric, no? But it's needed. Algorithm adapted from top answer at 
	// http://stackoverflow.com/questions/99353/how-to-test-if-a-line-segment-intersects-an-axis-aligned-rectange-in-2d
	public virtual bool intersectsBox(Vector3D mins, Vector3D maxs) {
		// Compute the signed distance from the line to each corner of the box
		double[] dist = new double[4];
		double x1 = vecHead.X;
		double x2 = vecHead.X + vecTail.X;
		double y1 = vecHead.Y;
		double y2 = vecHead.Y + vecTail.Y;
		dist[0] = vecTail.Y * mins.X + vecTail.X * mins.Y + (x2 * y1 - x1 * y2);
		dist[1] = vecTail.Y * mins.X + vecTail.X * maxs.Y + (x2 * y1 - x1 * y2);
		dist[2] = vecTail.Y * maxs.X + vecTail.X * mins.Y + (x2 * y1 - x1 * y2);
		dist[3] = vecTail.Y * maxs.X + vecTail.X * maxs.Y + (x2 * y1 - x1 * y2);
		if (dist[0] >= 0 && dist[1] >= 0 && dist[2] >= 0 && dist[3] >= 0) {
			return false;
		} else {
			if (dist[0] <= 0 && dist[1] <= 0 && dist[2] <= 0 && dist[3] <= 0) {
				return false;
			} else {
				// If we get to this point, the line intersects the box. Figure out if the line SEGMENT actually cuts it.
				if ((x1 > maxs.X && x2 > maxs.X) || (x1 < mins.X && x2 < mins.X) || (y1 > maxs.Y && y2 > maxs.Y) || (y1 < mins.Y && y2 < mins.Y)) {
					return false;
				} else {
					return true;
				}
			}
		}
	}

	public static Lump<DNode> createLump(byte[] data) {
		int structLength = 28;
		int offset = 0;
		Lump<DNode> lump = new Lump<DNode>(data.Length, structLength, data.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new DNode(bytes));
			offset += structLength;
		}
		return lump;
	}

	// ACCESSORS/MUTATORS

	virtual public Vector3D VecHead {
		get {
			return vecHead;
		}
		set {
			vecHead = value;
		}
	}
	virtual public Vector3D VecTail {
		get {
			return vecTail;
		}
		set {
			vecTail = value;
		}
	}
	virtual public Vector3D RMins {
		get {
			return RRectangle[MINS];
		}
		set {
			RRectangle[MINS] = value;
		}
	}
	virtual public Vector3D LMins {
		get {
			return LRectangle[MINS];
		}
		set {
			LRectangle[MINS] = value;
		}
	}
	virtual public Vector3D RMaxs {
		get {
			return RRectangle[MAXS];
		}
		set {
			RRectangle[MAXS] = value;
		}
	}
	virtual public Vector3D LMaxs {
		get {
			return LRectangle[MAXS];
		}
		set {
			LRectangle[MAXS] = value;
		}
	}
	virtual public short Child1 {
		get {
			return RChild;
		}
		set {
			RChild = value;
		}
	}
	virtual public short Child2 {
		get {
			return LChild;
		}
		set {
			LChild = value;
		}
	}
}