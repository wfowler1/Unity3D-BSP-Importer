using System;
// MAPDisplacement class
// Holds the information for a displacement, ideally for a VMF file.
[Serializable]
public class MAPDisplacement {

	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	private int power;
	private Vector3D start;
	private Vector3D[][] normals;
	private float[][] distances;
	private float[][] alphas;
	private uint[] allowedVerts;
	
	// CONSTRUCTORS
	
	public MAPDisplacement(SourceDispInfo disp, SourceDispVertex[] vertices) {
		power = disp.Power;
		start = disp.StartPosition;
		int numVertsInRow = 0;
		switch (power) {
			case 2: 
				numVertsInRow = 5;
				break;
			case 3: 
				numVertsInRow = 9;
				break;
			case 4: 
				numVertsInRow = 17;
				break;
		}
		normals = new Vector3D[numVertsInRow][];
		distances = new float[numVertsInRow][];
		alphas = new float[numVertsInRow][];
		for (int i = 0; i < numVertsInRow; i++) {
			normals[i] = new Vector3D[numVertsInRow];
			distances[i] = new float[numVertsInRow];
			alphas[i] = new float[numVertsInRow];
			for (int j = 0; j < numVertsInRow; j++) {
				normals[i][j] = vertices[(i * numVertsInRow) + j].Normal;
				distances[i][j] = vertices[(i * numVertsInRow) + j].Dist;
				alphas[i][j] = vertices[(i * numVertsInRow) + j].Alpha;
			}
		}
		allowedVerts = disp.AllowedVerts;
	}
	
	// METHODS
	
	// ACCESSORS/MUTATORS
	public virtual Vector3D getNormal(int row, int column)
	{
		return normals[row][column];
	}
	
	public virtual float getDist(int row, int column)
	{
		return distances[row][column];
	}
	
	public virtual float getAlpha(int row, int column)
	{
		return alphas[row][column];
	}

	virtual public int Power {
		get {
			return power;
		}
	}

	virtual public Vector3D Start {
		get {
			return start;
		}
	}

	virtual public uint[] AllowedVerts {
		get {
			return allowedVerts;
		}
	}
}