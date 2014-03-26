// GTKRadiantMapWriter class
//
// Writes a GTKRadiant .MAP file from a passed Entities object
using System;

public class GTKRadiantMapWriter {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	private string path;
	private Entities data;
	private mapType BSPVersion;
	
	private int currentEntity;
	
	// CONSTRUCTORS
	
	public GTKRadiantMapWriter(Entities from, string to, mapType BSPVersion) {
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
	public virtual void write() {
		// Preprocessing entity corrections
		if (!Settings.noEntCorrection) {
			if (BSPVersion == mapType.TYPE_NIGHTFIRE || BSPVersion == mapType.TYPE_QUAKE) {
				for (int i = 1; i < data.Count; i++) {
					for (int j = 0; j < data[i].Brushes.Count; j++) {
						if (data[i].Brushes[j].Water) {
							MAPBrush currentBrush = data[i].Brushes[j];
							for (int k = 0; k < currentBrush.NumSides; k++) {
								MAPBrushSide currentBrushSide = currentBrush[k];
								currentBrushSide.Texture = "Shaders/liquids/clear_calm1";
							}
							data[0].Brushes.Add(currentBrush);
						}
					}
					if (data[i]["classname"].Equals("func_water", StringComparison.InvariantCultureIgnoreCase)) {
						data.RemoveAt(i);
						i--;
					}
				}
			}
			// Correct some attributes of entities
			for (int i = 0; i < data.Count; i++) {
				Entity current = data[i];
				switch (BSPVersion) {
					case mapType.TYPE_NIGHTFIRE:  // Nightfire
						current = ent42ToEntRad(current);
						break;
					case mapType.TYPE_QUAKE2: 
						current = ent38ToEntRad(current);
						break;
					case mapType.TYPE_DOOM: 
					case mapType.TYPE_HEXEN: 
						break;
				}
			}
		}
		
		byte[][] entityBytes = new byte[data.Count][];
		int totalLength = 0;
		for (currentEntity = 0; currentEntity < data.Count; currentEntity++) {
			try {
				entityBytes[currentEntity] = entityToByteArray(data[currentEntity], currentEntity);
			} catch (System.IndexOutOfRangeException) {
				// This happens when entities are added after the array is made
				byte[][] newList = new byte[data.Count][]; // Create a new array with the new length
				for (int j = 0; j < entityBytes.Length; j++) {
					newList[j] = entityBytes[j];
				}
				newList[currentEntity] = entityToByteArray(data[currentEntity], currentEntity);
				entityBytes = newList;
			}
			totalLength += entityBytes[currentEntity].Length;
		}
		byte[] allEnts = new byte[totalLength];
		int offset = 0;
		for (int i = 0; i < data.Count; i++) {
			for (int j = 0; j < entityBytes[i].Length; j++)
			{
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
	private byte[] entityToByteArray(Entity inputData, int num) {
		byte[] outputData;
		Vector3D origin;
		if (inputData.BrushBased) {
			origin = inputData.Origin;
			inputData.Remove("origin");
			inputData.Remove("model");
			if (origin[0] != 0 || origin[1] != 0 || origin[2] != 0)
			{
				// If this entity uses the "origin" attribute
				MAPBrush newOriginBrush = MAPBrush.createBrush(new Vector3D(- Settings.originBrushSize, - Settings.originBrushSize, - Settings.originBrushSize), new Vector3D(Settings.originBrushSize, Settings.originBrushSize, Settings.originBrushSize), "special/origin");
				inputData.Brushes.Add(newOriginBrush);
			}
		} else {
			origin = Vector3D.ZERO;
		}
		string temp = "// entity "+num+(char)0x0D+(char)0x0A;
		temp += "{";
		int len = temp.Length+5;
		// Get the lengths of all attributes together
		foreach (string key in inputData.Attributes.Keys) {
			len += key.Length + inputData[key].Length + 7; // Four quotes, a space and a newline
		}
		outputData = new byte[len];
		int offset = 0;
		for (int i = 0; i < temp.Length; i++) {
			outputData[offset++] = (byte)temp[i];
		}
		outputData[offset++] = (byte)0x0D;
		outputData[offset++] = (byte)0x0A;
		foreach (string key in inputData.Attributes.Keys) {
			// For each attribute
			outputData[offset++] = (byte)'\"'; // 1
			for (int j = 0; j < key.Length; j++) {
				// Then for each byte in the attribute
				outputData[offset++] = (byte) key[j]; // add it to the output array
			}
			outputData[offset++] = (byte)'\"'; // 2
			outputData[offset++] = (byte)' '; // 3
			outputData[offset++] = (byte)'\"'; // 4
			for (int j = 0; j < inputData.Attributes[key].Length; j++) {
				// Then for each byte in the attribute
				outputData[offset++] = (byte) inputData.Attributes[key][j]; // add it to the output array
			}
			outputData[offset++] = (byte)'\"'; // 5
			outputData[offset++] = (byte)0x0D; // 6
			outputData[offset++] = (byte)0x0A; // 7
		}
		int brushArraySize = 0;
		byte[][] brushes = new byte[inputData.Brushes.Count][];
		for (int j = 0; j < inputData.Brushes.Count; j++)
		{
			brushes[j] = brushToByteArray(inputData.Brushes[j], j);
			brushArraySize += brushes[j].Length;
		}
		int brushoffset = 0;
		byte[] brushArray = new byte[brushArraySize];
		for (int j = 0; j < inputData.Brushes.Count; j++) {
			// For each brush in the entity
			for (int k = 0; k < brushes[j].Length; k++)
			{
				brushArray[brushoffset + k] = brushes[j][k];
			}
			brushoffset += brushes[j].Length;
		}
		if (brushArray.Length != 0)
		{
			len += brushArray.Length;
			byte[] newOut = new byte[len];
			for (int j = 0; j < outputData.Length; j++)
			{
				newOut[j] = outputData[j];
			}
			for (int j = 0; j < brushArray.Length; j++)
			{
				newOut[j + outputData.Length - 3] = brushArray[j];
			}
			offset += brushArray.Length;
			outputData = newOut;
		}
		outputData[offset++] = (byte)'}';
		outputData[offset++] = (byte)0x0D;
		outputData[offset++] = (byte)0x0A;
		return outputData;
	}
	
	private byte[] brushToByteArray(MAPBrush inData, int num) {
		if (inData.Patch != null) {
			return patchToByteArray(inData.Patch, num);
		}
		if (inData.NumSides < 4) {
			// Can't create a brush with less than 4 sides
			DecompilerThread.OnMessage(this, "WARNING: Tried to create brush from " + inData.NumSides + " sides!");
			return new byte[0];
		}
		string brush = "// brush " + num + (char) 0x0D + (char) 0x0A + "{" + (char) 0x0D + (char) 0x0A;
		for (int i = 0; i < inData.NumSides; i++) {
			brush += (brushSideToString(inData[i], (inData.Detail || inData[0].Displacement != null)) + (char) 0x0D + (char) 0x0A);
		}
		brush += ("}" + (char) 0x0D + (char) 0x0A);
		if (brush.Length < 45) {
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
	
	private string brushSideToString(MAPBrushSide inData, bool isDetail) {
		try {
			Vector3D[] triangle = inData.Triangle;
			string texture = inData.Texture;
			Vector3D textureS = inData.TextureS;
			Vector3D textureT = inData.TextureT;
			double textureShiftS = inData.TextureShiftS;
			double textureShiftT = inData.TextureShiftT;
			float texRot = inData.TexRot;
			double texScaleX = inData.TexScaleX;
			double texScaleY = inData.TexScaleY;
			int flags = inData.Flags;
			string material = inData.Material;
			double lgtScale = inData.LgtScale;
			double lgtRot = inData.LgtRot;
			string temp = "";
			// Correct textures here
			try {
				if (texture.Substring(0, (9) - (0)).ToUpper().Equals("textures/".ToUpper())) {
					texture = texture.Substring(9);
				}
			} catch (System.ArgumentOutOfRangeException) {
				;
			}
			if (BSPVersion == mapType.TYPE_NIGHTFIRE || BSPVersion == mapType.TYPE_DOOM || BSPVersion == mapType.TYPE_HEXEN) {
				if (texture.ToUpper().Equals("special/nodraw".ToUpper()) || texture.ToUpper().Equals("special/null".ToUpper())) {
					texture = "common/nodraw";
				} else {
					if (texture.ToUpper().Equals("special/clip".ToUpper())) {
						texture = "common/clip";
					} else {
						if (texture.ToUpper().Equals("special/sky".ToUpper())) {
							texture = "common/skyportal";
						} else {
							if (texture.ToUpper().Equals("special/trigger".ToUpper())) {
								texture = "common/trigger";
							} else {
								if (texture.ToUpper().Equals("special/playerclip".ToUpper())) {
									texture = "common/playerclip";
								} else {
									if (texture.ToUpper().Equals("special/npcclip".ToUpper()) || texture.ToUpper().Equals("special/enemyclip".ToUpper())) {
										texture = "common/tankclip";
									}
								}
							}
						}
					}
				}
			} else {
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
					} catch (System.ArgumentOutOfRangeException) {
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
			}
			// There might be other flags, detail was the only one I found though.
			if (isDetail) {
				flags = flags | 134217728;
			}
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
				textureS = TexInfo.textureAxisFromPlane(inData.Plane)[0];
			}
			if(Double.IsInfinity(textureT.X) || Double.IsNaN(textureT.X) || Double.IsInfinity(textureT.Y) || Double.IsNaN(textureT.Y) || Double.IsInfinity(textureT.Z) || Double.IsNaN(textureT.Z)) {
				textureT = TexInfo.textureAxisFromPlane(inData.Plane)[1];
			}
			if (Settings.roundNums) {
				temp = "( " + MAPMaker.Round(triangle[0].X, 6) + 
				       " " + MAPMaker.Round(triangle[0].Y, 6) + 
				       " " + MAPMaker.Round(triangle[0].Z, 6) + " ) " + 
				       "( " + MAPMaker.Round(triangle[1].X, 6) + 
				       " " + MAPMaker.Round(triangle[1].Y, 6) + 
				       " " + MAPMaker.Round(triangle[1].Z, 6) + " ) " + 
				       "( " + MAPMaker.Round(triangle[2].X, 6) + 
				       " " + MAPMaker.Round(triangle[2].Y, 6) + 
				       " " + MAPMaker.Round(triangle[2].Z, 6) + " ) " + 
				       texture + " " + System.Math.Floor(textureShiftS) + " " + System.Math.Floor(textureShiftT) + " " + 
				       MAPMaker.FormattedRound(texRot, 2, "######0.00") + " " + 
				       MAPMaker.Round(texScaleX, 6) + " " + 
				       MAPMaker.Round(texScaleY, 6) + " " + flags + " 0 0 ";
			} else {
				temp = "( " + triangle[0].X + " " + triangle[0].Y + " " + triangle[0].Z + " ) " + "( " + triangle[1].X + " " + triangle[1].Y + " " + triangle[1].Z + " ) " + "( " + triangle[2].X + " " + triangle[2].Y + " " + triangle[2].Z + " ) " + texture + " " + textureShiftS + " " + textureShiftT + " " + texRot + " " + texScaleX + " " + texScaleY + " " + flags + " 0 0 ";
			}
			return temp;
		}
		catch (System.NullReferenceException e)
		{
			DecompilerThread.OnMessage(this, "WARNING: Side with bad data! Not exported!");
			return "";
		}
	}

	public byte[] patchToByteArray(MAPPatch inData, int num) {
		string patch = "// brush " + num + (char) 0x0D + (char) 0x0A + inData.ToString() + (char) 0x0D + (char) 0x0A;
		byte[] patchbytes = new byte[patch.Length];
		for (int i = 0; i < patch.Length; i++) {
			patchbytes[i] = (byte) patch[i];
		}
		return patchbytes;
	}
	
	// These methods are TODO
	public virtual Entity ent42ToEntRad(Entity inEnt) {
		if (inEnt["classname"].Equals("func_door_rotating", StringComparison.InvariantCultureIgnoreCase)) {
			inEnt["classname"] = "func_rotatingdoor";
		} else {
			if (inEnt["classname"].Equals("worldspawn", StringComparison.InvariantCultureIgnoreCase)) {
				inEnt.Remove("mapversion");
			}
		}
		return inEnt;
	}
	
	public virtual Entity ent38ToEntRad(Entity inData) {
		return inData;
	}
	
	// ACCESSORS/MUTATORS
}