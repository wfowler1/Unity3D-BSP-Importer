// MAPBrushSide class
// Holds all the data for a brush side in the format for a .MAP file version 510.
using System;
[Serializable]
public class MAPBrushSide {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	private Vector3D[] triangle = new Vector3D[3]; // Plane defined as three points
	private Plane plane;
	private string texture;
	private Vector3D textureS;
	private double textureShiftS;
	private Vector3D textureT;
	private double textureShiftT;
	private float texRot = 0;
	private double texScaleX;
	private double texScaleY;
	private int flags;
	private string material;
	private double lgtScale;
	private double lgtRot;
	private int id = - 1;
	
	private MAPDisplacement disp = null;
	
	private bool planeDefined = false;
	private bool triangleDefined = false;
	
	public const int X = 0;
	public const int Y = 1;
	public const int Z = 2;
	
	// CONSTRUCTORS
	// Takes a triangle of points, and calculates a new standard equation for a plane with it. Not recommended.
	public MAPBrushSide(Vector3D[] inTriangle, string inTexture, double[] inTextureS, double inTextureShiftS, double[] inTextureT, double inTextureShiftT, float inTexRot, double inTexScaleX, double inTexScaleY, int inFlags, string inMaterial, double inLgtScale, double inLgtRot) {
		if (inTriangle.Length >= 3 && inTriangle[0] != null && inTriangle[1] != null && inTriangle[2] != null) {
			triangle[0] = inTriangle[0];
			triangle[1] = inTriangle[1];
			triangle[2] = inTriangle[2];
		} else {
			throw new System.ArithmeticException("Invalid point definition for a plane: \n" + inTriangle[0] + "\n" + inTriangle[1] + "\n" + inTriangle[2]);
		}
		this.plane = new Plane(triangle);
		texture = inTexture;
		textureS = new Vector3D(inTextureS);
		textureShiftS = inTextureShiftS;
		textureT = new Vector3D(inTextureT);
		textureShiftT = inTextureShiftT;
		texRot = inTexRot;
		texScaleX = inTexScaleX;
		texScaleY = inTexScaleY;
		flags = inFlags;
		material = inMaterial;
		lgtScale = inLgtScale;
		lgtRot = inLgtRot;
		planeDefined = false;
		triangleDefined = true;
		//DecompilerThread.OnMessage(this, triangle[0]+"\n"+triangle[1]+"\n"+triangle[2]+"\n\n");
	}
	
	// Takes both a plane and triangle. Recommended if at all possible.
	public MAPBrushSide(Plane plane, Vector3D[] inTriangle, string inTexture, double[] inTextureS, double inTextureShiftS, double[] inTextureT, double inTextureShiftT, float inTexRot, double inTexScaleX, double inTexScaleY, int inFlags, string inMaterial, double inLgtScale, double inLgtRot) {
		if (inTriangle.Length >= 3 && inTriangle[0] != null && inTriangle[1] != null && inTriangle[2] != null) {
			triangle[0] = inTriangle[0];
			triangle[1] = inTriangle[1];
			triangle[2] = inTriangle[2];
		} else {
			throw new System.ArithmeticException("Invalid point definition for a plane: \n" + inTriangle[0] + "\n" + inTriangle[1] + "\n" + inTriangle[2]);
		}
		this.plane = plane;
		triangle = inTriangle;
		texture = inTexture;
		textureS = new Vector3D(inTextureS);
		textureShiftS = inTextureShiftS;
		textureT = new Vector3D(inTextureT);
		textureShiftT = inTextureShiftT;
		texRot = inTexRot;
		texScaleX = inTexScaleX;
		texScaleY = inTexScaleY;
		flags = inFlags;
		material = inMaterial;
		lgtScale = inLgtScale;
		lgtRot = inLgtRot;
		planeDefined = true;
		triangleDefined = true;
		//DecompilerThread.OnMessage(this, triangle[0]+"\n"+triangle[1]+"\n"+triangle[2]+"\n\n");
	}
	
	// Takes only a plane and finds three arbitrary points on it. Recommend only if triangle is not available.
	public MAPBrushSide(Plane plane, string inTexture, double[] inTextureS, double inTextureShiftS, double[] inTextureT, double inTextureShiftT, float inTexRot, double inTexScaleX, double inTexScaleY, int inFlags, string inMaterial, double inLgtScale, double inLgtRot) {
		this.plane = plane;
		triangle = Plane.generatePlanePoints(plane);
		texture = inTexture;
		textureS = new Vector3D(inTextureS);
		textureShiftS = inTextureShiftS;
		textureT = new Vector3D(inTextureT);
		textureShiftT = inTextureShiftT;
		texRot = inTexRot;
		texScaleX = inTexScaleX;
		texScaleY = inTexScaleY;
		flags = inFlags;
		material = inMaterial;
		lgtScale = inLgtScale;
		lgtRot = inLgtRot;
		planeDefined = true;
		triangleDefined = false;
	}
	
	public MAPBrushSide(MAPBrushSide copy) {
		plane = new Plane(copy.Plane);
		triangle[0] = new Vector3D(copy.Triangle[0]);
		triangle[1] = new Vector3D(copy.Triangle[1]);
		triangle[2] = new Vector3D(copy.Triangle[2]);
		texture = copy.Texture;
		textureS = new Vector3D(copy.TextureS);
		textureT = new Vector3D(copy.TextureT);
		textureShiftS = copy.TextureShiftS;
		textureShiftT = copy.TextureShiftT;
		texRot = copy.TexRot;
		texScaleX = copy.TexScaleX;
		texScaleY = copy.TexScaleY;
		flags = copy.Flags;
		material = copy.Material;
		lgtScale = copy.LgtScale;
		lgtRot = copy.LgtRot;
		planeDefined = copy.DefinedByPlane;
		triangleDefined = copy.DefinedByTriangle;
		disp = copy.Displacement;
	}
	
	// METHODS
	
	// toString()
	// Returns the brush side exactly as it would look in a .MAP file.
	// This is on multiple lines simply for readability. the returned
	// String will have no line breaks. This isn't used anymore for
	// file output, this would be slower.
	public override string ToString() {
		try {
			return "( " + triangle[0].X + " " + triangle[0].Y + " " + triangle[0].Z + " ) " +
			"( " + triangle[1].X + " " + triangle[1].Y + " " + triangle[1].Z + " ) " + 
			"( " + triangle[2].X + " " + triangle[2].Y + " " + triangle[2].Z + " ) " + 
			texture + 
			" [ " + textureS.X + " " + textureS.Y + " " + textureS.Z + " " + textureShiftS + " ]" + 
			" [ " + textureT.X + " " + textureT.Y + " " + textureT.Z + " " + textureShiftT + " ] " + 
			texRot + " " + texScaleX + " " + texScaleY + " " + flags + " " + material + " [ " + lgtScale + " " + lgtRot + " ]";
		} catch (System.NullReferenceException) {
			DecompilerThread.OnMessage(this, "WARNING: Side with bad data! Not exported!");
			return null;
		}
	}
	
	// flipPlane()
	// Negate all definitions of the plane in the side.
	// Don't need to change the indicators of whether or not this side was defined by
	// triangle or plane, since both definitions are still valid. I'm using the same
	// information, I'm just reversing the direction.
	public virtual void  flipSide() {
		Vector3D temp = triangle[2];
		triangle[2] = triangle[1];
		triangle[1] = temp;
		plane.flip();
	}
	
	// translate(Vector3D)
	// Shifts the brush side and its points by the amounts in the input Vector
	public virtual void translate(Vector3D shift) {
		//try {
			if (shift.X != 0 || shift.Y != 0 || shift.Z != 0) {
				triangle[0] = triangle[0]+shift;
				triangle[1] = triangle[1]+shift;
				triangle[2] = triangle[2]+shift;
				plane = new Plane(triangle);
			}
		//} catch (System.Exception) {
			//UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Throwable.toString' may return a different value. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextSettingsIndex'&keyword='jlca1043'"
		//	DecompilerThread.OnMessage(this, "WARNING: Failed to translate triangle:" + e + (char) 0x0D + (char) 0x0A + triangle[0] + (char) 0x0D + (char) 0x0A + triangle[1] + (char) 0x0D + (char) 0x0A + triangle[2] + (char) 0x0D + (char) 0x0A + "Adding: " + shift);
		//}
	}
	
	public virtual void  setSide(Plane plane, Vector3D[] triangle) {
		if (triangle.Length >= 3) {
			if (triangle[0] == null) {
				DecompilerThread.OnMessage(this, "WARNING: Tried to set triangle but point 0 was null!");
			} else {
				if (triangle[1] == null) {
					DecompilerThread.OnMessage(this, "WARNING: Tried to set triangle but point 1 was null!");
				} else {
					if (triangle[2] == null) {
						DecompilerThread.OnMessage(this, "WARNING: Tried to set triangle but point 2 was null!");
					} else {
						this.triangle[0] = triangle[0];
						this.triangle[1] = triangle[1];
						this.triangle[2] = triangle[2];
						triangleDefined = true;
						this.plane = plane;
						planeDefined = true;
					}
				}
			}
		} else {
			DecompilerThread.OnMessage(this, "WARNING: Tried to define side with " + triangle.Length + " points!");
		}
	}

	// ACCESSORS/MUTATORS
	virtual public bool DefinedByPlane {
		get {
			return planeDefined;
		}
	}

	virtual public bool DefinedByTriangle {
		get {
			return triangleDefined;
		}
	}

	virtual public Vector3D[] Triangle {
		get {
			return triangle;
		}
		set {
			if (value.Length >= 3) {
				if (value[0] == null) {
					DecompilerThread.OnMessage(this, "WARNING: Tried to set triangle but point 0 was null!");
				} else {
					if (value[1] == null) {
						DecompilerThread.OnMessage(this, "WARNING: Tried to set triangle but point 1 was null!");
					} else {
						if (value[2] == null) {
							DecompilerThread.OnMessage(this, "WARNING: Tried to set triangle but point 2 was null!");
						} else{
							triangle[0] = value[0];
							triangle[1] = value[1];
							triangle[2] = value[2];
							plane = new Plane(triangle);
							planeDefined = false;
							triangleDefined = true;
						}
					}
				}
			} else {
				DecompilerThread.OnMessage(this, "WARNING: Tried to define side with " + triangle.Length + " points!");
			}
		}
	}

	virtual public Plane Plane {
		get {
			return plane;
		}
		set {
			plane = value;
			triangle = Plane.generatePlanePoints(plane);
			planeDefined = true;
			triangleDefined = false;
		}
	}

	virtual public MAPDisplacement Displacement {
		get {
			return disp;
		}
		set {
			disp = value;
		}
	}

	virtual public double LgtScale {
		get {
			return lgtScale;
		}
	}

	virtual public double LgtRot {
		get {
			return lgtRot;
		}
	}

	virtual public string Material {
		get {
			return material;
		}
	}

	virtual public int Flags {
		get {
			return flags;
		}
	}

	virtual public double TexScaleX {
		get {
			return texScaleX;
		}
	}

	virtual public double TexScaleY {
		get {
			return texScaleY;
		}
	}

	virtual public double TextureShiftS {
		get {
			return textureShiftS;
		}
	}

	virtual public double TextureShiftT {
		get {
			return textureShiftT;
		}
	}

	virtual public string Texture {
		get {
			return texture;
		}
		set {
			texture = value;
		}
	}

	virtual public Vector3D TextureS {
		get {
			return textureS;
		}
	}

	virtual public Vector3D TextureT {
		get {
			return textureT;
		}
	}

	virtual public float TexRot {
		get {
			return texRot;
		}
	}

	virtual public int ID {
		get {
			return id;
		}
	}
}