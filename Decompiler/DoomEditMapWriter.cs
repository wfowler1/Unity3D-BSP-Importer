// DoomEditMapWriter class
//
// Writes a DoomEdit (or other id tech 5 editor) file from a passed Entities object
using System;

public class DoomEditMapWriter {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	public const int A = 0;
	public const int B = 1;
	public const int C = 2;
	
	public const int X = 0;
	public const int Y = 1;
	public const int Z = 2;
	
	// These are lowercase so as not to conflict with A B and C
	// Light entity attributes; red, green, blue, strength (can't use i for intensity :P)
	public const int r = 0;
	public const int g = 1;
	public const int b = 2;
	public const int s = 3;
	
	private string path;
	private Entities data;
	private mapType BSPVersion;
	
	private int currentEntity;
	
	// CONSTRUCTORS
	
	public DoomEditMapWriter(Entities from, string to, mapType BSPVersion) {
		this.data = from;
		this.path = to;
		this.BSPVersion = BSPVersion;
	}
	
	// METHODS
	
	// write()
	// Saves the lump to the specified path.
	// Handling file I/O with Strings is generally a bad idea. If you have maybe a couple hundred
	// Strings to write then it'll probably be okay, but when you have on the order of 10,000 Strings
	// it gets VERY slow, even if you concatenate them all before writing.
	public virtual void  write() {
		// Preprocessing entity corrections
		if (!Settings.noEntCorrection) {
			if (BSPVersion == mapType.TYPE_NIGHTFIRE) {
				for (int i = 1; i < data.Count; i++) {
					for (int j = 0; j < data[i].Brushes.Count; j++) {
						if (data[i].Brushes[j].Water) {
							data[0].Brushes.Add(data[i].Brushes[j]);
							// TODO: Textures on this brush
						}
					}
					if (data[i]["classname"].Equals("func_water", StringComparison.InvariantCultureIgnoreCase)) {
						data.RemoveAt(i);
						i--;
					}
				}
			}
			// Correct some attributes of entities
			// TODO
			/*switch(BSPVersion) {
			default:
			break;
			}*/
		}
		
		string temp = "Version 2"+(char)0x0A;
		
		byte[][] entityBytes = new byte[data.Count][];
		int totalLength = 0;
		for (currentEntity = 0; currentEntity < data.Count; currentEntity++) {
			try {
				entityBytes[currentEntity] = entityToByteArray(data[currentEntity]);
			} catch (System.IndexOutOfRangeException) {
				// This happens when entities are added after the array is made
				byte[][] newList = new byte[data.Count][]; // Create a new array with the new length
				for (int j = 0; j < entityBytes.Length; j++) {
					newList[j] = entityBytes[j];
				}
				newList[currentEntity] = entityToByteArray(data[currentEntity]);
				entityBytes = newList;
			}
			totalLength += entityBytes[currentEntity].Length;
		}
		byte[] allEnts = new byte[totalLength + temp.Length];
		int offset = 0;
		for (int i = 0; i < temp.Length; i++) {
			allEnts[offset++] = (byte)temp[i];
		}
		for (int i = 0; i < data.Count; i++) {
			for (int j = 0; j < entityBytes[i].Length; j++) {
				allEnts[offset + j] = entityBytes[i][j];
			}
			offset += entityBytes[i].Length;
		}
		MAPMaker.write(allEnts, path, false);
	}
	
	// -entityToByteArray()
	// Converts the entity and its brushes into byte arrays rather than Strings,
	// which can then be written to a file much faster. Concatenating Strings is
	// a costly operation, especially when hundreds of thousands of Strings are
	// in play. This is one of two parts to writing a file quickly. The second
	// part is to call the FileOutputStream.write() method only once, with a
	// gigantic array, rather than several times with many small arrays. File I/O
	// from a hard drive is another costly operation, best done by handling
	// massive amounts of data in one go, rather than tiny amounts of data thousands
	// of times.
	private byte[] entityToByteArray(Entity inEnt) {
		byte[] output;
		Vector3D origin = Vector3D.ZERO;
		if (inEnt.BrushBased) {
			inEnt.Remove("model");
		}
		if (inEnt.Brushes.Count > 0) {
			origin = inEnt.Origin;
		}
		string temp = "// entity "+currentEntity+(char)0x0A+"{"+(char)0x0A;
		int len = temp.Length + 2; // Closing brace and a newline
		// Get the lengths of all attributes together
		foreach (string key in inEnt.Attributes.Keys) {
			len += key.Length + inEnt.Attributes[key].Length + 6; // Four quotes, a space and a newline
		}
		output = new byte[len];
		int offset = 0;
		for(int i=0;i<temp.Length;i++) {
			output[offset++] = (byte)temp[i];
		}
		foreach (string key in inEnt.Attributes.Keys) {
			// For each attribute
			output[offset++] = (byte)'\"'; // 1
			for (int j = 0; j < key.Length; j++) {
				// Then for each byte in the attribute
				output[j + offset] = (byte) key[j]; // add it to the output array
			}
			offset += key.Length;
			output[offset++] = (byte)'\"'; // 2
			output[offset++] = (byte)' '; // 3
			output[offset++] = (byte)'\"'; // 4
			for (int j = 0; j < inEnt.Attributes[key].Length; j++) {
				// Then for each byte in the attribute
				output[j + offset] = (byte) inEnt.Attributes[key][j]; // add it to the output array
			}
			offset += inEnt.Attributes[key].Length;
			output[offset++] = (byte)'\"'; // 5
			output[offset++] = (byte)0x0D; // 6
		}
		int brushArraySize = 0;
		byte[][] brushes = new byte[inEnt.Brushes.Count][];
		for (int j = 0; j < inEnt.Brushes.Count; j++) {
			// For each brush in the entity
			// models with origin brushes need to be offset into their in-use position
			inEnt.Brushes[j].translate(origin);
			brushes[j] = brushToByteArray(inEnt.Brushes[j], j);
			brushArraySize += brushes[j].Length;
		}
		int brushoffset = 0;
		byte[] brushArray = new byte[brushArraySize];
		for (int j = 0; j < inEnt.Brushes.Count; j++) {
			// For each brush in the entity
			for (int k = 0; k < brushes[j].Length; k++) {
				brushArray[brushoffset + k] = brushes[j][k];
			}
			brushoffset += brushes[j].Length;
		}
		if (brushArray.Length != 0) {
			len += brushArray.Length;
			byte[] newOut = new byte[len];
			for (int j = 0; j < output.Length; j++) {
				newOut[j] = output[j];
			}
			for (int j = 0; j < brushArray.Length; j++) {
				newOut[j + output.Length - 2] = brushArray[j];
			}
			offset += brushArray.Length;
			output = newOut;
		}
		output[offset++] = (byte)'}';
		output[offset++] = (byte)0x0A;
		return output;
	}
	
	private byte[] brushToByteArray(MAPBrush inBrush, int num) {
		if (inBrush.NumSides < 4) {
			// Can't create a brush with less than 4 sides
			DecompilerThread.OnMessage(this, "WARNING: Tried to create brush from " + inBrush.NumSides + " sides!");
			return new byte[0];
		}
		string brush = "// primitive " + num + (char) 0x0A + "{" + (char) 0x0A + " brushDef3" + (char) 0x0A + " {" + (char) 0x0A;
		for (int i = 0; i < inBrush.NumSides; i++) {
			brush += ("  " + brushSideToString(inBrush[i]) + (char) 0x0A);
		}
		brush += (" }" + (char) 0x0A + "}" + (char) 0x0A);
		if (brush.Length < 58) {
			// Any brush this short contains no sides.
			DecompilerThread.OnMessage(this, "WARNING: Brush with no sides being written! Oh no!");
			return new byte[0];
		} else {
			byte[] brushbytes = new byte[brush.Length];
			for (int i = 0; i < brush.Length; i++) {
				brushbytes[i] = (byte) brush[i];
			}
			return brushbytes;
		}
	}
	
	private string brushSideToString(MAPBrushSide inputData) {
		try {
			string texture = inputData.Texture;
			if (BSPVersion == mapType.TYPE_SOURCE17 || BSPVersion == mapType.TYPE_SOURCE18 || BSPVersion == mapType.TYPE_SOURCE19 || BSPVersion == mapType.TYPE_SOURCE20 || BSPVersion == mapType.TYPE_SOURCE21 || BSPVersion == mapType.TYPE_SOURCE22 || BSPVersion == mapType.TYPE_SOURCE23 || BSPVersion == mapType.TYPE_DMOMAM || BSPVersion == mapType.TYPE_VINDICTUS || BSPVersion == mapType.TYPE_TACTICALINTERVENTION) {
				try {
					if (texture.Substring(0, (5) - (0)).ToUpper().Equals("maps/".ToUpper())) {
						texture = texture.Substring(5);
						for (int i = 0; i < texture.Length; i++) {
							if (texture[i] == '/') {
								texture = texture.Substring(i + 1);
								break;
							}
						}
					}
				}
				catch (System.ArgumentOutOfRangeException) {
					;
				}
				// Find cubemap textures
				int numUnderscores = 0;
				bool validnumber = false;
				for (int i = texture.Length - 1; i > 0; i--) {
					if (texture[i] <= '9' && texture[i] >= '0') {
						// Current is a number, start building string
						validnumber = true;
					} else {
						if (texture[i] == '-') {
							// Current is a minus sign (-).
							if (!validnumber) {
								break; // Make sure there's a number to add the minus sign to. If not, kill the loop.
							}
						} else {
							if (texture[i] == '_') {
								// Current is an underscore (_)
								if (validnumber) {
									// Make sure there is a number in the current string
									numUnderscores++; // before moving on to the next one.
									validnumber = false;
									if (numUnderscores == 3) {
										// If we've got all our numbers
										texture = texture.Substring(0, (i) - (0)); // Cut the texture string
										break; // Kill the loop, we're done
									}
								} else {
									// No number after the underscore
									break;
								}
							} else {
								// Not an acceptable character
								break;
							}
						}
					}
				}
			}
			Plane plane = inputData.Plane;
			Vector3D textureS = inputData.TextureS;
			Vector3D textureT = inputData.TextureT;
			double textureShiftS = inputData.TextureShiftS;
			double textureShiftT = inputData.TextureShiftT;
			double texScaleX = inputData.TexScaleX;
			double texScaleY = inputData.TexScaleY;
			if(Double.IsInfinity(texScaleX) || Double.IsNaN(texScaleX)) {
				texScaleX = 1;
			}
			if(Double.IsInfinity(texScaleY) || Double.IsNaN(texScaleY)) {
				texScaleY = 1;
			}
			if(Double.IsInfinity(textureShiftS) || Double.IsNaN(textureShiftS)) {
				textureShiftS = 0;
			}
			if(Double.IsInfinity(textureShiftT) || Double.IsNaN(textureShiftT)) {
				textureShiftT = 0;
			}
			if(Double.IsInfinity(textureS.X) || Double.IsNaN(textureS.X) || Double.IsInfinity(textureS.Y) || Double.IsNaN(textureS.Y) || Double.IsInfinity(textureS.Z) || Double.IsNaN(textureS.Z)) {
				textureS = TexInfo.textureAxisFromPlane(inputData.Plane)[0];
			}
			if(Double.IsInfinity(textureT.X) || Double.IsNaN(textureT.X) || Double.IsInfinity(textureT.Y) || Double.IsNaN(textureT.Y) || Double.IsInfinity(textureT.Z) || Double.IsNaN(textureT.Z)) {
				textureT = TexInfo.textureAxisFromPlane(inputData.Plane)[1];
			}
			if (Settings.roundNums)
			{
				return "( " + Math.Round(plane.A) + " " + Math.Round(plane.B) + " " + Math.Round(plane.C) + " " + Math.Round(plane.Dist) + " ) " + "( ( 1 0 " + Math.Round(textureShiftS) + " ) ( 0 1 " + Math.Round(textureShiftT) + " ) ) " + "\"" + texture + "\" 0 0 0";
			}
			else
			{
				return "( " + plane.A + " " + plane.B + " " + plane.C + " " + plane.Dist + " ) " + "( ( 1 0 " + textureShiftS + " ) ( 0 1 " + textureShiftT + " ) ) " + "\"" + texture + "\" 0 0 0";
			}
		} catch (System.NullReferenceException e) {
			DecompilerThread.OnMessage(this, "WARNING: Side with bad data! Not exported!");
			return null;
		}
	}
	
	// ACCESSORS/MUTATORS
}