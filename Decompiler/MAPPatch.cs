using System;
using System.Collections.Generic;
// MAPPatch class
// Quake 3 maps have bezier patches defined in their face structure, as a list of control points.
// Not sure about the particulars of how it works, but hopefully I can extract them by just moving
// numbers around, like I did with displacement surfaces.
[Serializable]
public class MAPPatch : List<Vertex> {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	private Vertex dims;
	private string texture;
	
	// CONSTRUCTORS
	
	public MAPPatch(int x, int y, string texture):this(x, y, texture, new List<Vertex>(x*y)) {
	}
	
	public MAPPatch(int x, int y, string texture, List<Vertex> points):base(points) {
		dims = new Vertex(new Vector3D(x, y, 0.0), 0.0f, 0.0f);
		this.texture = texture;
	}
	
	// METHODS
	
	// toString()
	public override string ToString() {
		string temp = texture;
		try {
			if (temp.Substring(0, (9) - (0)).ToUpper().Equals("textures/".ToUpper())) {
				temp = temp.Substring(9);
			}
		} catch (System.ArgumentOutOfRangeException) {
			;
		}
		string output = "{"+(char)0x0D+(char)0x0A+"patchDef2"+(char)0x0D+(char)0x0A+"{"+(char)0x0D+(char)0x0A+temp+(char)0x0D+(char)0x0A+dims.ToString()+(char)0x0D+(char)0x0A+"("+(char)0x0D+(char)0x0A;
		for(int i = 0; i<dims.Vector.X; i++) {
			output += "( ";
			for(int j=0; j<dims.Vector.Y; j++) {
				output += this[i+(int)(j*dims.Vector.X)].ToString()+" ";
			}
			output += ")"+(char)0x0D+(char)0x0A;
		}
		return output + ")"+(char)0x0D+(char)0x0A+"}"+(char)0x0D+(char)0x0A+"}";
	}
	
	// ACCESSORS/MUTATORS

	public Vertex Dims {
		get { return dims; }
	}

	public string Texture {
		get { return texture; }
	}
}