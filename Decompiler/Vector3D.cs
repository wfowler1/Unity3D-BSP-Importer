using System;
// Vector3D class

// Holds a double3, for a point.
// Incidentally, I'd LOVE to use 128-bit quads for this, but no such thing exists in Java.
// Would take waaaayy too much time to process decimals...
// With help from Alex "UltimateSniper"
[Serializable]
public class Vector3D:IEquatable<Vector3D> {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	public static readonly Vector3D UNDEFINED = new Vector3D(System.Double.NaN, System.Double.NaN, System.Double.NaN);
	public static readonly Vector3D RIGHT = new Vector3D(1, 0, 0);
	public static readonly Vector3D FORWARD = new Vector3D(0, 1, 0);
	public static readonly Vector3D UP = new Vector3D(0, 0, 1);
	public static readonly Vector3D ZERO = new Vector3D(0, 0, 0);
	
	private double[] point = new double[3];
	
	// CONSTRUCTORS
	
	// Takes X, Y and Z separate.
	public Vector3D(float inX, float inY, float inZ) {
		//X = (double)inX;
		//Y = (double)inY;
		//Z = (double)inZ;
		point[0] = Convert.ToDouble(inX);
		point[1] = Convert.ToDouble(inY);
		point[2] = Convert.ToDouble(inZ);
	}

	public Vector3D(double inX, double inY, double inZ) {
		//X = inX;
		//Y = inY;
		//Z = inZ;
		point[0] = inX;
		point[1] = inY;
		point[2] = inZ;
	}

	// Takes one array of length 3, containing X, Y and Z.
	public Vector3D(float[] point) {
		try {
			//X = (double)point[0];
			//Y = (double)point[1];
			//Z = (double)point[2];
			this.point[0] = Convert.ToDouble(point[0]);
			this.point[1] = Convert.ToDouble(point[1]);
			this.point[2] = Convert.ToDouble(point[2]);
		} catch (System.IndexOutOfRangeException) {
			;
		}
	}

	public Vector3D(double[] point) {
		try {
			//X = point[0];
			//Y = point[1];
			//Z = point[2];
			this.point = point;
		} catch (System.IndexOutOfRangeException) {
			;
		}
	}

	// Takes a Vector3D.
	public Vector3D(Vector3D copy) {
		point = new double[]{copy.X, copy.Y, copy.Z};
	}

	// Takes bytes in a byte array, as though it had just been read by a FileInputStream.
	public Vector3D(byte[] data) {
		if (data.Length >= 12 && data.Length < 24) {
			X = (double) DataReader.readFloat(data[0], data[1], data[2], data[3]);
			Y = (double) DataReader.readFloat(data[4], data[5], data[6], data[7]);
			Z = (double) DataReader.readFloat(data[8], data[9], data[10], data[11]);
		}
		else if (data.Length >= 24) {
			X = DataReader.readDouble(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7]);
			Y = DataReader.readDouble(data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15]);
			Z = DataReader.readDouble(data[16], data[17], data[18], data[19], data[20], data[21], data[22], data[23]);
		} else {
			throw new System.IndexOutOfRangeException();
		}
	}
	
	// Takes two shorts, and assumes they are X and Y.
	// Useful for turning 2D data from a Doom Map into 3D coordinates
	public Vector3D(short x, short y) {
		point[0] = Convert.ToDouble(x);
		point[1] = Convert.ToDouble(y);
		point[2] = 0.0;
	}
	
	// Takes three shorts.
	public Vector3D(short x, short y, short z) {
		point[0] = Convert.ToDouble(x);
		point[1] = Convert.ToDouble(y);
		point[2] = Convert.ToDouble(z);
	}

	public Vector3D(short[] point) {
		try {
			this.point[0] = Convert.ToDouble(point[0]);
			this.point[1] = Convert.ToDouble(point[1]);
			this.point[2] = Convert.ToDouble(point[2]);
		} catch (System.IndexOutOfRangeException) {
			;
		}
	}
	
	// METHODS
	
	// Operators
	public static Vector3D operator +(Vector3D v1, Vector3D v2) {
		return new Vector3D(v1.X+v2.X, v1.Y+v2.Y, v1.Z+v2.Z);
	}

	public static Vector3D operator -(Vector3D v1, Vector3D v2) {
		return new Vector3D(v1.X-v2.X, v1.Y-v2.Y, v1.Z-v2.Z);
	}

	public static Vector3D operator -(Vector3D v1) {
		return new Vector3D(-v1.X, -v1.Y, -v1.Z);
	}

	public static Vector3D operator *(Vector3D v1, double scalar) {
		return new Vector3D(v1.X*scalar, v1.Y*scalar, v1.Z*scalar);
	}

	public static Vector3D operator *(double scalar, Vector3D v1) {
		return v1*scalar;
	}

	public static double operator *(Vector3D v1, Vector3D v2) {
		return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
	}

	public static Vector3D operator ^(Vector3D v1, Vector3D v2) {
		return new Vector3D(v1.Y * v2.Z - v2.Y * v1.Z, v2.X * v1.Z - v1.X * v2.Z, v1.X * v2.Y - v2.X * v1.Y);
	}

	public static Vector3D operator /(Vector3D v1, double divisor) {
		return new Vector3D(v1.X/divisor, v1.Y/divisor, v1.Z/divisor);
	}

	public static bool operator ==(Vector3D v1, Vector3D v2) {
		if(Object.ReferenceEquals(v1, null) ^ Object.ReferenceEquals(v2, null)) { return false; }
		if(Object.ReferenceEquals(v1, null) && Object.ReferenceEquals(v2, null)) { return true; }
		return v1.Equals(v2);
	}
	public static bool operator !=(Vector3D v1, Vector3D v2) {
		if(Object.ReferenceEquals(v1, null) ^ Object.ReferenceEquals(v2, null)) { return true; }
		if(Object.ReferenceEquals(v1, null) && Object.ReferenceEquals(v2, null)) { return false; }
		return !v1.Equals(v2);
	}

	/// Returns:	Vector3D of the components of this vertex, plus the respective components of the input vertex.
	[Obsolete("Vector3D.add() is deprecated, use + operator instead.")]
	public virtual Vector3D add(Vector3D data) {
		return new Vector3D(point[0] + data.X, point[1] + data.Y, point[2] + data.Z);
	}
	
	/// Returns:	Vector3D of the components of this vertex, minus the respective components of the input vertex.
	[Obsolete("Vector3D.subtract() is deprecated, use - operator instead.")]
	public virtual Vector3D subtract(Vector3D data) {
		return new Vector3D(point[0] - data.X, point[1] - data.Y, point[2] - data.Z);
	}
	
	/// Returns:	Negative of this vertex.
	[Obsolete("Vector3D.negate() is deprecated, use - operator instead.")]
	public virtual Vector3D negate() {
		return new Vector3D(- point[0], - point[1], - point[2]);
	}
	
	/// Returns:	Whether or not the vertex is identical to this one.
	// Behavior identical to ==
	public bool Equals(Vector3D data) {
		return (point[0] + Settings.precision >= data.X && point[0] - Settings.precision <= data.X && point[1] + Settings.precision >= data.Y && point[1] - Settings.precision <= data.Y && point[2] + Settings.precision >= data.Z && point[2] - Settings.precision <= data.Z);
	}
	
	// Scalar product
	/// Returns:	Vector3D of the components of this vertex, multiplied by the scalar value.
	[Obsolete("Vector3D.scale() is deprecated, use * operator instead.")]
	public virtual Vector3D scale(double scalar) {
		return new Vector3D(point[0] * scalar, point[1] * scalar, point[2] * scalar);
	}
	
	// Vector Products
	// Dot
	[Obsolete("Vector3D.dotProduct() is deprecated, use * operator instead.")]
	public static double dotProduct(Vector3D vec1, Vector3D vec2) {
		return dotProduct(vec1.Point, vec2.Point);
	}
	
	[Obsolete("Vector3D.dotProduct() is deprecated, use * operator instead.")]
	public static double dotProduct(double[] vec1, Vector3D vec2) {
		return dotProduct(vec1, vec2.Point);
	}
	
	[Obsolete("Vector3D.dotProduct() is deprecated, use * operator instead.")]
	public static double dotProduct(Vector3D vec1, double[] vec2) {
		return dotProduct(vec1.Point, vec2);
	}
	
	[Obsolete("Vector3D.dotProduct() is deprecated, use * operator instead.")]
	public static double dotProduct(double[] vec1, double[] vec2) {
		return vec1[0] * vec2[0] + vec1[1] * vec2[1] + vec1[2] * vec2[2];
	}
	
	[Obsolete("Vector3D.dot() is deprecated, use * operator instead.")]
	public virtual double dot(Vector3D vec) {
		return dot(vec.Point);
	}
	
	[Obsolete("Vector3D.dot() is deprecated, use * operator instead.")]
	public virtual double dot(double[] vec) {
		return point[0] * vec[0] + point[1] * vec[1] + point[2] * vec[2];
	}

	// Cross
	[Obsolete("Vector3D.crossProduct() is deprecated, use ^ operator instead.")]
	public static Vector3D crossProduct(Vector3D vec1, Vector3D vec2)
	{
		return crossProduct(vec1.Point, vec2.Point);
	}
	
	[Obsolete("Vector3D.crossProduct() is deprecated, use ^ operator instead.")]
	public static Vector3D crossProduct(double[] vec1, Vector3D vec2)
	{
		return crossProduct(vec1, vec2.Point);
	}
	
	[Obsolete("Vector3D.crossProduct() is deprecated, use ^ operator instead.")]
	public static Vector3D crossProduct(Vector3D vec1, double[] vec2)
	{
		return crossProduct(vec1.Point, vec2);
	}
	
	[Obsolete("Vector3D.crossProduct() is deprecated, use ^ operator instead.")]
	public static Vector3D crossProduct(double[] vec1, double[] vec2)
	{
		return new Vector3D(vec1[1] * vec2[2] - vec2[1] * vec1[2], vec2[0] * vec1[2] - vec1[0] * vec2[2], vec1[0] * vec2[1] - vec2[0] * vec1[1]);
	}

	[Obsolete("Vector3D.cross() is deprecated, use ^ operator instead.")]
	public virtual Vector3D cross(Vector3D vec)
	{
		return cross(vec.Point);
	}
	
	[Obsolete("Vector3D.cross() is deprecated, use ^ operator instead.")]
	public virtual Vector3D cross(double[] vec)
	{
		return new Vector3D(point[1] * vec[2] - vec[1] * point[2], vec[0] * point[2] - point[0] * vec[2], point[0] * vec[1] - vec[0] * point[1]);
	}
	
	// Generic
	/// Returns:	Distance from the vertex to the origin.
	public virtual double magnitude()
	{
		return System.Math.Sqrt(System.Math.Pow(point[0], 2) + System.Math.Pow(point[1], 2) + System.Math.Pow(point[2], 2));
	}
	
	/// Returns:	Distance from this point to another one.
	public virtual double distance(Vector3D to)
	{
		return System.Math.Sqrt(System.Math.Pow((point[0] - to.X), 2) + System.Math.Pow((point[1] - to.Y), 2) + System.Math.Pow((point[2] - to.Z), 2));
	}
	
	/// Returns:	String describing the vertex in the form x , y , z.
	public override string ToString()
	{
		return point[0].ToString() + " , " + point[1].ToString() + " , " + point[2].ToString();
	}
	
	// Modifies this vector to have length 1, with same direction
	public virtual void normalize()
	{
		Vector3D newVector = this / magnitude();
		point[0] = newVector.X;
		point[1] = newVector.Y;
		point[2] = newVector.Z;
	}
	
	public virtual Vector3D normalized()
	{
		return this / magnitude();
	}
	
	// ACCESSORS/MUTATORS
	
	public virtual double X {
		get {
			return point[0];
		}
		set {
			if (value + Settings.precision >= Math.Floor(value + 0.5) && value - Settings.precision <= Math.Floor(value + 0.5)) {
				point[0] = Math.Floor(value + 0.5);
			} else {
				point[0] = value;
			}
		}
	}
	
	public virtual double Y {
		get {
			return point[1];
		}
		set {
			if (value + Settings.precision >= Math.Floor(value + 0.5) && value - Settings.precision <= Math.Floor(value + 0.5)) {
				point[1] = Math.Floor(value + 0.5);
			} else {
				point[1] = value;
			}
		}
	}
	
	public virtual double Z {
		get {
			return point[2];
		}
		set {
			if (value + Settings.precision >= Math.Floor(value + 0.5) && value - Settings.precision <= Math.Floor(value + 0.5)) {
				point[2] = Math.Floor(value + 0.5);
			} else {
				point[2] = value;
			}
		}
	}

	virtual public double[] Point {
		get {
			return point;
		}
		set {
			try {
				X = value[0];
				Y = value[1];
				Z = value[2];
			} catch (System.IndexOutOfRangeException) {
				;
			}
		}
	}

	virtual public double this[int index] {
		get {
			if(index>=0 && index<=2) {
				return point[index];
			}
			return System.Double.NaN;
		}
		set {
			if(index>=0 && index<=2) {
				if (value + Settings.precision >= Math.Floor(value + 0.5) && value - Settings.precision <= Math.Floor(value + 0.5)) {
					point[index] = Math.Floor(value + 0.5);
				} else {
					point[index]=value;
				}
			}
		}
	}
}