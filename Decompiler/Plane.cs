using System;
// Plane class
// Holds the A, B, C and D components for a plane, which can be read from any given BSP.
// Assumes the plane equation is of format Ax+By+Cz=D, or Ax+By+Cz-D=0.
[Serializable]
public class Plane:LumpObject, IEquatable<Plane> {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	private Vector3D normal;
	private double dist = System.Double.NaN;
	
	// CONSTRUCTORS

	public Plane():base(new byte[0]) { }
	
	// This one takes the components separate and in the correct data type
	public Plane(float inA, float inB, float inC, float inDist):this((double)inA, (double)inB, (double)inC, (double)inDist) {
	}
	
	public Plane(double inA, double inB, double inC, double inDist):base(new byte[0]) {
		normal = new Vector3D(inA, inB, inC);
		dist = inDist;
	}
	
	public Plane(Vector3D normal, double dist):base(new byte[0]) {
		this.normal = new Vector3D(normal);
		this.dist = dist;
	}
	
	public Plane(Plane copy):base(new byte[0]) {
		normal = new Vector3D(copy.Normal);
		dist = copy.Dist;
	}

	// Takes 3 vertices, which define the plane.
	public Plane(Vector3D a, Vector3D b, Vector3D c):base(new byte[0]) {
		normal = ((a-c)^(a-b)).normalized();
		dist = a*normal;
	}

	public Plane(Vector3D[] points):this(points[0], points[1], points[2]) {
	}

	public Plane(float[] inNormal, float inDist):base(new byte[0]) {
		normal = new Vector3D(inNormal[0], inNormal[1], inNormal[2]);
		dist = (double)inDist;
	}
	
	public Plane(Vector3D normal, float dist):base(new byte[0]) {
		this.normal = normal;
		this.dist = (double)dist;
	}
	
	public Plane(LumpObject data, mapType type):base(data.Data) {
		new Plane(data.Data, type);
	}
	
	public Plane(byte[] data, mapType type):base(data) {
		normal = DataReader.readPoint3F(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11]);
		dist = (double)DataReader.readFloat(data[12], data[13], data[14], data[15]);
	}
	
	// METHODS
	
	/// Returns:	Whether this plane is parallel to, faces the same direction, and has the same distance as, the given plane.
	public static bool operator ==(Plane p1, Plane p2) {
		if(Object.ReferenceEquals(p1, null) ^ Object.ReferenceEquals(p2, null)) { return false; }
		if(Object.ReferenceEquals(p1, null) && Object.ReferenceEquals(p2, null)) { return true; }
		return p1.Equals(p2);
	}
	public static bool operator !=(Plane p1, Plane p2) {
		if(Object.ReferenceEquals(p1, null) ^ Object.ReferenceEquals(p2, null)) { return true; }
		if(Object.ReferenceEquals(p1, null) && Object.ReferenceEquals(p2, null)) { return false; }
		return !p1.Equals(p2);
	}

	public bool Equals(Plane other) {
		// Use Cross-Product; if 0, parallel. Must face same direction, have parallel normals, and identical distances.
		Vector3D inNorm = other.Normal;
		return (normal==inNorm && dist + Settings.precision >= other.Dist && dist - Settings.precision <= other.Dist);
	}
	
	/// Returns:	Signed distance from this plane to given vertex.
	public virtual double distance(Vector3D to) {
		return distance(to.Point);
	}

	public virtual double distance(double[] to) {
		// Ax + By + Cz - d = DISTANCE = normDOTpoint - d
		double normLength = System.Math.Pow(normal.X, 2) + System.Math.Pow(normal.Y, 2) + System.Math.Pow(normal.Z, 2);
		if (System.Math.Abs(normLength - 1.00) > 0.01) {
			normLength = System.Math.Sqrt(normLength);
		}
		return (normal.X * to[0] + normal.Y * to[1] + normal.Z * to[2] - dist) / normLength;
	}
	
	/// Returns:	Point where this plane intersects 2 planes given. Gives Vector3D.Undefined if any planes are parallel.
	public virtual Vector3D trisect(Plane p2, Plane p3) {
		Vector3D bN = p2.Normal;
		Vector3D cN = p3.Normal;
		/* Math:
		*  x1*x y1*y z1*z	 d1		x1 y1 z1  *  x	 d1		x	 x1 y1 z1 ^-1  *  d1	 (d1*(y2*z3-z2*y3) + d2*(y3*z1-z3*y1) + d3*(y1*z2-z1*y2)) / (x1*(y2*z3-z2*y3) + y1*(x3*z2-z3*x2) + z1*(x2*y3-y2*x3))
		*  x2*x y2*y z2*z  =  d2   =>   x2 y2 z2	 y  =  d2   =>   y  =  x2 y2 z2		 d2  =  (d1*(x3*z2-z3*x2) + d2*(x1*z3-z1*x3) + d3*(x2*z1-z2*x1)) / (x1*(y2*z3-z2*y3) + y1*(x3*z2-z3*x2) + z1*(x2*y3-y2*x3))
		*  x3*x y3*y z3*z	 d3		x3 y3 z3	 z	 d3		z	 x3 y3 z3		 d3	 (d1*(x2*y3-y2*x3) + d2*(x3*y1-y3*x1) + d3*(x1*y2-y1*x2)) / (x1*(y2*z3-z2*y3) + y1*(x3*z2-z3*x2) + z1*(x2*y3-y2*x3))
		*  -> Note that the 3 sets of brackets used in the determinant (the denominator) are also used in some cases before the division (these are the first row of the inverted matrix).
		*  --> Fastest method: calc once and use twice.
		*/
		double PartSolx1 = (bN.Y * cN.Z) - (bN.Z * cN.Y);
		double PartSoly1 = (bN.Z * cN.X) - (bN.X * cN.Z);
		double PartSolz1 = (bN.X * cN.Y) - (bN.Y * cN.X);
		double det = (normal.X * PartSolx1) + (normal.Y * PartSoly1) + (normal.Z * PartSolz1); // Determinant
		if (det == 0) {
			// If 0, 2 or more planes are parallel.
			return Vector3D.UNDEFINED;
		}
		// Divide by determinant to get final matrix solution, and multiply by matrix of distances to get final position.
		return new Vector3D((dist * PartSolx1 + p2.Dist * (cN.Y * normal.Z - cN.Z * normal.Y) + p3.Dist * (normal.Y * bN.Z - normal.Z * bN.Y)) / det, (dist * PartSoly1 + p2.Dist * (normal.X * cN.Z - normal.Z * cN.X) + p3.Dist * (bN.X * normal.Z - bN.Z * normal.X)) / det, (dist * PartSolz1 + p2.Dist * (cN.X * normal.Y - cN.Y * normal.X) + p3.Dist * (normal.X * bN.Y - normal.Y * bN.X)) / det);
	}
	
	// Flips plane to face opposite direction.
	public virtual void flip() {
		normal = -normal;
		dist = -dist;
	}
	
	// Takes a Plane and flips it (static method)
	[Obsolete("Plane.flip() is deprecated, use - operator instead.")]
	public static Plane flip(Plane flipMe) {
		return new Plane(-flipMe.Normal, - flipMe.Dist);
	}

	public static Plane operator -(Plane flipMe) {
		return new Plane(-flipMe.Normal, - flipMe.Dist);
	}
	
	// Takes a plane as an array of vertices and flips it over.
	public static Vector3D[] flip(Vector3D[] flipMe) {
		return new Vector3D[]{flipMe[0], flipMe[2], flipMe[1]};
	}
	
	// Returns this plane, flipped
	[Obsolete("Plane.negate() is deprecated, use - operator instead.")]
	public virtual Plane negate() {
		return new Plane(-normal, - dist);
	}
	
	public override string ToString() {
		return "(" + normal.ToString() + ") " + dist;
	}
	
	public static Vector3D[] generatePlanePoints(Plane in_Renamed) {
		//DecompilerThread.OnMessage(this, "Calculating arbitrary plane points");
		double planePointCoef = Settings.planePointCoef;
		Vector3D[] plane = new Vector3D[3];
		// Figure out if the plane is parallel to two of the axes. If so it can be reproduced easily
		if (in_Renamed.B == 0 && in_Renamed.C == 0) {
			// parallel to plane YZ
			plane[0] = new Vector3D(in_Renamed.Dist / in_Renamed.A, - planePointCoef, planePointCoef);
			plane[1] = new Vector3D(in_Renamed.Dist / in_Renamed.A, 0, 0);
			plane[2] = new Vector3D(in_Renamed.Dist / in_Renamed.A, planePointCoef, planePointCoef);
			if (in_Renamed.A > 0) {
				plane = Plane.flip(plane);
			}
		} else {
			if (in_Renamed.A == 0 && in_Renamed.C == 0) {
				// parallel to plane XZ
				plane[0] = new Vector3D(planePointCoef, in_Renamed.Dist / in_Renamed.B, - planePointCoef);
				plane[1] = new Vector3D(0, in_Renamed.Dist / in_Renamed.B, 0);
				plane[2] = new Vector3D(planePointCoef, in_Renamed.Dist / in_Renamed.B, planePointCoef);
				if (in_Renamed.B > 0) {
					plane = Plane.flip(plane);
				}
			} else {
				if (in_Renamed.A == 0 && in_Renamed.B == 0) {
					// parallel to plane XY
					plane[0] = new Vector3D(- planePointCoef, planePointCoef, in_Renamed.Dist / in_Renamed.C);
					plane[1] = new Vector3D(0, 0, in_Renamed.Dist / in_Renamed.C);
					plane[2] = new Vector3D(planePointCoef, planePointCoef, in_Renamed.Dist / in_Renamed.C);
					if (in_Renamed.C > 0) {
						plane = Plane.flip(plane);
					}
				} else {
					// If you reach this point the plane is not parallel to any two-axis plane.
					if (in_Renamed.A == 0) {
						// parallel to X axis
						plane[0] = new Vector3D(- planePointCoef, planePointCoef * planePointCoef, (- (planePointCoef * planePointCoef * in_Renamed.B - in_Renamed.Dist)) / in_Renamed.C);
						plane[1] = new Vector3D(0, 0, in_Renamed.Dist / in_Renamed.C);
						plane[2] = new Vector3D(planePointCoef, planePointCoef * planePointCoef, (- (planePointCoef * planePointCoef * in_Renamed.B - in_Renamed.Dist)) / in_Renamed.C);
						if (in_Renamed.C > 0) {
							plane = Plane.flip(plane);
						}
					} else {
						if (in_Renamed.B == 0) {
							// parallel to Y axis
							plane[0] = new Vector3D((- (planePointCoef * planePointCoef * in_Renamed.C - in_Renamed.Dist)) / in_Renamed.A, - planePointCoef, planePointCoef * planePointCoef);
							plane[1] = new Vector3D(in_Renamed.Dist / in_Renamed.A, 0, 0);
							plane[2] = new Vector3D((- (planePointCoef * planePointCoef * in_Renamed.C - in_Renamed.Dist)) / in_Renamed.A, planePointCoef, planePointCoef * planePointCoef);
							if (in_Renamed.A > 0) {
								plane = Plane.flip(plane);
							}
						} else {
							if (in_Renamed.C == 0) {
								// parallel to Z axis
								plane[0] = new Vector3D(planePointCoef * planePointCoef, (- (planePointCoef * planePointCoef * in_Renamed.A - in_Renamed.Dist)) / in_Renamed.B, - planePointCoef);
								plane[1] = new Vector3D(0, in_Renamed.Dist / in_Renamed.B, 0);
								plane[2] = new Vector3D(planePointCoef * planePointCoef, (- (planePointCoef * planePointCoef * in_Renamed.A - in_Renamed.Dist)) / in_Renamed.B, planePointCoef);
								if (in_Renamed.B > 0) {
									plane = Plane.flip(plane);
								}
							} else {
								// If you reach this point the plane is not parallel to any axis. Therefore, any two coordinates will give a third.
								plane[0]=new Vector3D(-planePointCoef, planePointCoef*planePointCoef, -(-planePointCoef*in_Renamed.A+planePointCoef*planePointCoef*in_Renamed.B-in_Renamed.Dist)/in_Renamed.C);
								plane[1] = new Vector3D(0, 0, in_Renamed.Dist / in_Renamed.C);
								plane[2]=new Vector3D(planePointCoef, planePointCoef*planePointCoef, -(planePointCoef*in_Renamed.A+planePointCoef*planePointCoef*in_Renamed.B-in_Renamed.Dist)/in_Renamed.C);
								if (in_Renamed.C > 0) {
									plane = Plane.flip(plane);
								}
							}
						}
					}
				}
			}
		}
		return plane;
	}

	public static Lump<Plane> createLump(byte[] data, mapType type) {
		int structLength = 0;
		switch (type) {
			case mapType.TYPE_QUAKE: 
			case mapType.TYPE_NIGHTFIRE: 
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
			case mapType.TYPE_DMOMAM: 
			case mapType.TYPE_VINDICTUS: 
			case mapType.TYPE_QUAKE2: 
			case mapType.TYPE_DAIKATANA: 
			case mapType.TYPE_TACTICALINTERVENTION: 
				structLength = 20;
				break;
			case mapType.TYPE_STEF2: 
			case mapType.TYPE_MOHAA: 
			case mapType.TYPE_STEF2DEMO: 
			case mapType.TYPE_RAVEN: 
			case mapType.TYPE_QUAKE3: 
			case mapType.TYPE_FAKK: 
			case mapType.TYPE_COD: 
			case mapType.TYPE_COD2: 
			case mapType.TYPE_COD4: 
				structLength = 16;
				break;
			default: 
				structLength = 0; // This will cause the shit to hit the fan.
				break;
		}
		int offset = 0;
		Lump<Plane> lump = new Lump<Plane>(data.Length, structLength, data.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new Plane(bytes, type));
			offset += structLength;
		}
		return lump;
	}
	
	// ACCESSORS/MUTATORS
	virtual public Vector3D Normal {
		get {
			return normal;
		}
	}

	virtual public double Dist {
		get {
			return dist;
		}
	}

	public virtual double A {
		get {
			return normal.X;
		}
	}
	
	public virtual double B {
		get {
			return normal.Y;
		}
	}
	
	public virtual double C {
		get {
			return normal.Z;
		}
	}
}