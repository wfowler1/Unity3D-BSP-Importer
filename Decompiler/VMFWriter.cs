// VMFWriter class
//
// Writes a Hammer .VMF file from a passed Entities object
using System;
using System.Collections.Generic;

public class VMFWriter {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	public const int A = 0;
	public const int B = 1;
	public const int C = 2;
	
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
	
	internal int nextID = 1;
	internal string[] numeralizedTargetnames = new string[0];
	internal int[] numTargets = new int[0];
	private int mmStackLength = 0;
	
	// CONSTRUCTORS
	
	public VMFWriter(Entities from, string to, mapType BSPVersion) {
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
			DecompilerThread.OnMessage(this, "Parsing entities...");
			if (BSPVersion == mapType.TYPE_NIGHTFIRE || BSPVersion == mapType.TYPE_QUAKE) {
				bool containsWater = false;
				Vector3D goodOrigin = Vector3D.ZERO;
				for (int i = 1; i < data.Count; i++) {
					for (int j = 0; j < data[i].Brushes.Count; j++) {
						MAPBrush currentBrush = data[i].Brushes[j];
						if (currentBrush.Water) {
							containsWater = true;
							currentBrush.Detail = false;
							for (int k = 0; k < currentBrush.NumSides; k++) {
								// If the normal vector of the side's plane is in the positive Z axis, it's on top of the brush.
								if (currentBrush[k].Plane.Normal.Equals(Vector3D.UP))
								{
									currentBrush[k].Texture = "dev/dev_water2"; // Better texture?
								}
								else
								{
									currentBrush[k].Texture = "TOOLS/TOOLSNODRAW";
								}
							}
							data[0].Brushes.Add(data[i].Brushes[j]);
						}
					}
					if (data[i]["classname"].Equals("func_water", StringComparison.InvariantCultureIgnoreCase))
					{
						data.RemoveAt(i);
						i--;
					}
					else
					{
						if (data[i]["classname"].Equals("item_ctfbase", StringComparison.InvariantCultureIgnoreCase))
						{
							data.RemoveAt(i);
							i--;
						}
						else
						{
							if (data[i]["classname"].Equals("info_player_start", StringComparison.InvariantCultureIgnoreCase))
							{
								goodOrigin = data[i].Origin;
							}
						}
					}
				}
				if (containsWater)
				{
					Entity lodControl = new Entity("water_lod_control");
					lodControl["cheapwaterenddistance"] = "2000";
					lodControl["cheapwaterstartdistance"] = "1000";
					lodControl["origin"] = goodOrigin[0] + " " + goodOrigin[1] + " " + goodOrigin[2];
				}
			}
		
			// Correct some more attributes of entities
			for (int i = 0; i < data.Count; i++)
			{
				Entity current = data[i];
				switch (BSPVersion)
				{
					case mapType.TYPE_NIGHTFIRE: 
					// Nightfire
					case mapType.TYPE_DOOM: 
					// When I decompile the Doom format, I output Nightfire entities. Not ideal, but it works.
					case mapType.TYPE_HEXEN: 
						current = ent42ToEntVMF(current);
						break;
					case mapType.TYPE_QUAKE2: 
						current = ent38ToEntVMF(current);
						break;
				}
			}
		
			// Parse entity I/O
			for (int i = 0; i < data.Count; i++) {
				Entity current = data[i];
				current = parseEntityIO(current);
			}
		}
		
		/*String tempString="versioninfo"+(char)0x0D+(char)0x0A+"{"+(char)0x0D+(char)0x0A+"	\"editorversion\" \"400\""+(char)0x0D+(char)0x0A+"	\"editorbuild\" \"3325\""+(char)0x0D+(char)0x0A+"	\"mapversion\" \"0\""+(char)0x0D+(char)0x0A+"	\"formatversion\" \"100\""+(char)0x0D+(char)0x0A+"	\"prefab\" \"0\""+(char)0x0D+(char)0x0A+"}"+(char)0x0D+(char)0x0A+"";
		tempString+="visgroups"+(char)0x0D+(char)0x0A+"{"+(char)0x0D+(char)0x0A+"}"+(char)0x0D+(char)0x0A+"";
		tempString+="viewsettings"+(char)0x0D+(char)0x0A+"{"+(char)0x0D+(char)0x0A+"	\"bSnapToGrid\" \"1\""+(char)0x0D+(char)0x0A+"	\"bShowGrid\" \"1\""+(char)0x0D+(char)0x0A+"	\"bShowLogicalGrid\" \"0\""+(char)0x0D+(char)0x0A+"	\"nGridSpacing\" \"64\""+(char)0x0D+(char)0x0A+"	\"bShow3DGrid\" \"0\""+(char)0x0D+(char)0x0A+"}"+(char)0x0D+(char)0x0A+"";
		
		byte[] header=tempString.getBytes();*/
		
		byte[][] entityBytes = new byte[data.Count][];
		int totalLength = 0;
		for (currentEntity = 0; currentEntity < data.Count; currentEntity++)
		{
			try
			{
				entityBytes[currentEntity] = entityToByteArray(data[currentEntity]);
			}
			catch (System.IndexOutOfRangeException e)
			{
				// This happens when entities are added after the array is made
				byte[][] newList = new byte[data.Count][]; // Create a new array with the new length
				for (int j = 0; j < entityBytes.Length; j++)
				{
					newList[j] = entityBytes[j];
				}
				newList[currentEntity] = entityToByteArray(data[currentEntity]);
				entityBytes = newList;
			}
			totalLength += entityBytes[currentEntity].Length;
		}
		byte[] allEnts = new byte[totalLength];
		int offset = 0;
		for (int i = 0; i < data.Count; i++)
		{
			for (int j = 0; j < entityBytes[i].Length; j++)
			{
				allEnts[offset + j] = entityBytes[i][j];
			}
			offset += entityBytes[i].Length;
		}
		MAPMaker.write(allEnts, path, true);
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
		inEnt["id"] = ((System.Int32) nextID++).ToString();
		byte[] outputData;
		Vector3D origin = Vector3D.ZERO;
		if (inEnt.BrushBased) {
			inEnt.Remove("model");
		}
		if (inEnt.Brushes.Count > 0)
		{
			origin = inEnt.Origin;
		}
		string temp;
		if(inEnt["classname"].Equals("worldspawn", StringComparison.InvariantCultureIgnoreCase)) {
			temp = "world"+(char)0x0D+(char)0x0A+"{"+(char)0x0D+(char)0x0A;
		} else {
			temp = "entity"+(char)0x0D+(char)0x0A+"{"+(char)0x0D+(char)0x0A;
		}
		int len = temp.Length+3; // Closing brace, newline
		// Get the lengths of all attributes together
		foreach (string key in inEnt.Attributes.Keys) {
			len += key.Length + inEnt[key].Length + 8; // Four quotes, a space, a tab and a newline
		}
		if (inEnt.Connections.Count > 0) {
			len += 22; // tab, "connections", newline, tab, "{", newline, tab, "}", newline
			foreach (Tuple<string, string, string, string, double, int, string, Tuple<string>> connection in inEnt.Connections) {
				if(connection.Item7 == "" && connection.Rest.Item1 == "") {
					len += connection.Item1.Length + connection.Item2.Length + connection.Item3.Length + connection.Item4.Length + connection.Item5.ToString().Length + connection.Item6.ToString().Length + 13; // Two tabs, four quotes, space, four commas, newline
				} else {
					len += connection.Item1.Length + connection.Item2.Length + connection.Item3.Length + connection.Item4.Length + connection.Item5.ToString().Length + connection.Item6.ToString().Length + connection.Item7.Length + connection.Rest.Item1.Length + 15; // Two tabs, four quotes, space, six commas, newline
				}
			}
		}
		outputData = new byte[len];
		int offset = 0;
		for (int i = 0; i < temp.Length; i++) {
			outputData[offset++] = (byte)temp[i];
		}
		foreach (string key in inEnt.Attributes.Keys) {
			outputData[offset++] = (byte) (0x09); // 1
			outputData[offset++] = (byte)'\"'; // 2
			for (int j = 0; j < key.Length; j++) {
				// Then for each byte in the attribute
				outputData[j + offset] = (byte) key[j]; // add it to the output array
			}
			offset += key.Length;
			outputData[offset++] = (byte)'\"'; // 3
			outputData[offset++] = (byte)' '; // 4
			outputData[offset++] = (byte)'\"'; // 5
			for (int j = 0; j < inEnt.Attributes[key].Length; j++) {
				// Then for each byte in the attribute
				outputData[j + offset] = (byte) inEnt.Attributes[key][j]; // add it to the output array
			}
			offset += inEnt.Attributes[key].Length;
			outputData[offset++] = (byte)'\"'; // 6
			outputData[offset++] = (byte)0x0D; // 7
			outputData[offset++] = (byte)0x0A; // 8
		}
		if (inEnt.Connections.Count > 0) {
			outputData[offset++] = (byte)0x09; // tab 1
			outputData[offset++] = (byte)'c'; // 2
			outputData[offset++] = (byte)'o'; // 3
			outputData[offset++] = (byte)'n'; // 4
			outputData[offset++] = (byte)'n'; // 5
			outputData[offset++] = (byte)'e'; // 6
			outputData[offset++] = (byte)'c'; // 7
			outputData[offset++] = (byte)'t'; // 8
			outputData[offset++] = (byte)'i'; // 9
			outputData[offset++] = (byte)'o'; // 10
			outputData[offset++] = (byte)'n'; // 11
			outputData[offset++] = (byte)'s'; // 12
			outputData[offset++] = (byte)0x0D; // 13
			outputData[offset++] = (byte)0x0A; // newline 14
			outputData[offset++] = (byte)0x09; //tab 15
			outputData[offset++] = (byte)'{'; // 16
			outputData[offset++] = (byte)0x0D; // 17
			outputData[offset++] = (byte)0x0A; // newline 18
			foreach (Tuple<string, string, string, string, double, int, string, Tuple<string>> connection in inEnt.Connections) {
				string str = "";
				if(connection.Item7 == "" && connection.Rest.Item1 == "") {
					str = ""+(char)0x09 + (char)0x09 + "\""+connection.Item1+"\" \""+connection.Item2+","+connection.Item3+","+connection.Item4+","+connection.Item5.ToString()+","+connection.Item6.ToString()+"\""+(char)0x0D+(char)0x0A;
				} else {
					str = ""+(char)0x09 + (char)0x09 + "\""+connection.Item1+"\" \""+connection.Item2+","+connection.Item3+","+connection.Item4+","+connection.Item5.ToString()+","+connection.Item6.ToString()+","+connection.Item7+","+connection.Rest.Item1+"\""+(char)0x0D+(char)0x0A;
				}
				for (int j = 0; j < str.Length; j++) {
					// Then for each byte in the string
					outputData[offset++] = (byte)str[j]; // add it to the output array
				}
			}
			outputData[offset++] = (byte)0x09; // tab 19
			outputData[offset++] = (byte)'}'; // 20
			outputData[offset++] = (byte)0x0D; // 21
			outputData[offset++] = (byte)0x0A; // newline 22
		}
		int brushArraySize = 0;
		byte[][] brushes = new byte[inEnt.Brushes.Count][];
		for (int j = 0; j < inEnt.Brushes.Count; j++)
		{
			// For each brush in the entity
			bool containsNonClipSide = false;
			for (int k=0; k<inEnt.Brushes[j].NumSides; k++) {
				if (!inEnt.Brushes[j][k].Texture.ToLower().Contains("clip")) {
					containsNonClipSide = true;
					break;
				}
			}
			if (inEnt.Brushes[j].Detail && inEnt.attributeIs("classname", "worldspawn") && containsNonClipSide)
			{
				inEnt.Brushes[j].Detail = false; // Otherwise it will add an infinite number of func_details to the array
				Entity newDetailEntity = new Entity("func_detail");
				for (int k = 0; k < inEnt.Brushes[j].NumSides; k++)
				{
					MAPBrushSide currentSide = inEnt.Brushes[j][k];
					if (currentSide.Texture.Equals("special/TRIGGER", StringComparison.InvariantCultureIgnoreCase))
					{
						currentSide.Texture = "TOOLS/TOOLSHINT"; // Hint is the only thing that still works that doesn't collide with the player
					}
				}
				newDetailEntity.Brushes.Add(inEnt.Brushes[j]);
				data.Add(newDetailEntity);
				brushes[j] = new byte[0]; // No data here! The brush will be output in its entity instead.
			}
			else
			{
				inEnt.Brushes[j].translate(new Vector3D(origin));
				brushes[j] = brushToByteArray(inEnt.Brushes[j]);
				brushArraySize += brushes[j].Length;
			}
		}
		int brushoffset = 0;
		byte[] brushArray = new byte[brushArraySize];
		for (int j = 0; j < inEnt.Brushes.Count; j++)
		{
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
	
	private byte[] brushToByteArray(MAPBrush inMapBrush)
	{
		if (inMapBrush.NumSides < 4)
		{
			// Can't create a brush with less than 4 sides
			DecompilerThread.OnMessage(this, "WARNING: Tried to create brush from " + inMapBrush.NumSides + " sides!");
			return new byte[0];
		}
		string brush = (char) 0x09 + "solid" + (char) 0x0D + (char) 0x0A + (char) 0x09 + "{" + (char) 0x0D + (char) 0x0A + (char) 0x09 + (char) 0x09 + "\"id\" \"" + (nextID++) + "\"" + (char) 0x0D + (char) 0x0A;
		for (int i = 0; i < inMapBrush.NumSides; i++)
		{
			brush += brushSideToString(inMapBrush[i]);
		}
		brush += ((char) 0x09 + "}" + (char) 0x0D + (char) 0x0A);
		if (brush.Length < 40)
		{
			// Any brush this short contains no sides.
			DecompilerThread.OnMessage(this, "WARNING: Brush with no sides being written! Oh no!");
			return new byte[0];
		}
		else
		{
			byte[] brushbytes = new byte[brush.Length];
			for (int i = 0; i < brush.Length; i++)
			{
				brushbytes[i] = (byte) brush[i];
			}
			return brushbytes;
		}
	}
	
	private string brushSideToString(MAPBrushSide inBrushSide)
	{
		try
		{
			Vector3D[] triangle = inBrushSide.Triangle;
			string texture = inBrushSide.Texture;
			Vector3D textureS = inBrushSide.TextureS;
			Vector3D textureT = inBrushSide.TextureT;
			double textureShiftS = inBrushSide.TextureShiftS;
			double textureShiftT = inBrushSide.TextureShiftT;
			double texScaleX = inBrushSide.TexScaleX;
			double texScaleY = inBrushSide.TexScaleY;
			float texRot = inBrushSide.TexRot;
			double lgtScale = inBrushSide.LgtScale;
			if (BSPVersion == mapType.TYPE_NIGHTFIRE || BSPVersion == mapType.TYPE_DOOM || BSPVersion == mapType.TYPE_HEXEN)
			{
				if (texture.ToUpper().Equals("special/nodraw".ToUpper()) || texture.ToUpper().Equals("special/null".ToUpper()))
				{
					texture = "tools/toolsnodraw";
				}
				else
				{
					if (texture.ToUpper().Equals("special/clip".ToUpper()))
					{
						texture = "tools/toolsclip";
					}
					else
					{
						if (texture.ToUpper().Equals("special/sky".ToUpper()))
						{
							texture = "tools/toolsskybox";
						}
						else
						{
							if (texture.ToUpper().Equals("special/trigger".ToUpper()))
							{
								texture = "tools/toolstrigger";
							}
							else
							{
								if (texture.ToUpper().Equals("special/playerclip".ToUpper()))
								{
									texture = "tools/toolsplayerclip";
								}
								else
								{
									if (texture.ToUpper().Equals("special/npcclip".ToUpper()) || texture.ToUpper().Equals("special/enemyclip".ToUpper()))
									{
										texture = "tools/toolsnpcclip";
									}
								}
							}
						}
					}
				}
			}
			else
			{
				if (BSPVersion == mapType.TYPE_QUAKE2)
				{
					try
					{
						if (texture.ToUpper().Equals("special/hint".ToUpper()))
						{
							texture = "tools/toolshint";
						}
						else
						{
							if (texture.ToUpper().Equals("special/skip".ToUpper()))
							{
								texture = "tools/toolsskip";
							}
							else
							{
								if (texture.ToUpper().Equals("special/sky".ToUpper()))
								{
									texture = "tools/toolsskybox";
								}
								else
								{
									if (texture.Substring(texture.Length - 8).ToUpper().Equals("/trigger".ToUpper()))
									{
										texture = "tools/toolstrigger";
									}
									else
									{
										if (texture.Substring(texture.Length - 5).ToUpper().Equals("/clip".ToUpper()))
										{
											texture = "tools/toolsclip";
										}
									}
								}
							}
						}
					}
					catch (System.ArgumentOutOfRangeException e)
					{
						;
					}
				}
				else
				{
					if (BSPVersion == mapType.TYPE_SOURCE17 || BSPVersion == mapType.TYPE_SOURCE18 || BSPVersion == mapType.TYPE_SOURCE19 || BSPVersion == mapType.TYPE_SOURCE20 || BSPVersion == mapType.TYPE_SOURCE21 || BSPVersion == mapType.TYPE_SOURCE22 || BSPVersion == mapType.TYPE_SOURCE23 || BSPVersion == mapType.TYPE_DMOMAM || BSPVersion == mapType.TYPE_VINDICTUS || BSPVersion == mapType.TYPE_TACTICALINTERVENTION)
					{
						try
						{
							if (texture.Substring(0, (5) - (0)).ToUpper().Equals("maps/".ToUpper()))
							{
								texture = texture.Substring(5);
								for (int i = 0; i < texture.Length; i++)
								{
									if (texture[i] == '/')
									{
										texture = texture.Substring(i + 1);
										break;
									}
								}
							}
						}
						catch (System.ArgumentOutOfRangeException)
						{
							;
						}
						// Find cubemap textures
						int numUnderscores = 0;
						bool validnumber = false;
						for (int i = texture.Length - 1; i > 0; i--)
						{
							if (texture[i] <= '9' && texture[i] >= '0')
							{
								// Current is a number, start building string
								validnumber = true;
							}
							else
							{
								if (texture[i] == '-')
								{
									// Current is a minus sign (-).
									if (!validnumber)
									{
										break; // Make sure there's a number to add the minus sign to. If not, kill the loop.
									}
								}
								else
								{
									if (texture[i] == '_')
									{
										// Current is an underscore (_)
										if (validnumber)
										{
											// Make sure there is a number in the current string
											numUnderscores++; // before moving on to the next one.
											validnumber = false;
											if (numUnderscores == 3)
											{
												// If we've got all our numbers
												texture = texture.Substring(0, (i) - (0)); // Cut the texture string
												break; // Kill the loop, we're done
											}
										}
										else
										{
											// No number after the underscore
											break;
										}
									}
									else
									{
										// Not an acceptable character
										break;
									}
								}
							}
						}
					}
				}
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
				textureS = TexInfo.textureAxisFromPlane(inBrushSide.Plane)[0];
			}
			if(Double.IsInfinity(textureT.X) || Double.IsNaN(textureT.X) || Double.IsInfinity(textureT.Y) || Double.IsNaN(textureT.Y) || Double.IsInfinity(textureT.Z) || Double.IsNaN(textureT.Z)) {
				textureT = TexInfo.textureAxisFromPlane(inBrushSide.Plane)[1];
			}
			string output = "		side" + (char) 0x0D + (char) 0x0A + "		{" + (char) 0x0D + (char) 0x0A;
			output += ("			\"id\" \"" + (nextID++) + "\"" + (char) 0x0D + (char) 0x0A);
			output += ("			\"material\" \"" + texture + "\"" + (char) 0x0D + (char) 0x0A);
			if (Settings.roundNums) {
				output += ("			\"plane\" \"(" + MAPMaker.Round(triangle[0].X, 6) + " " + MAPMaker.Round(triangle[0].Y, 6) + " " + MAPMaker.Round(triangle[0].Z, 6) + ") ");
				output += ("("                      + MAPMaker.Round(triangle[1].X, 6) + " " + MAPMaker.Round(triangle[1].Y, 6) + " " + MAPMaker.Round(triangle[1].Z, 6) + ") ");
				output += ("("                      + MAPMaker.Round(triangle[2].X, 6) + " " + MAPMaker.Round(triangle[2].Y, 6) + " " + MAPMaker.Round(triangle[2].Z, 6) + ")\"" + (char) 0x0D + (char) 0x0A);
				output += ("			\"uaxis\" \"[" + MAPMaker.Round(textureS.X, 6) + " " + MAPMaker.Round(textureS.Y, 6) + " " + MAPMaker.Round(textureS.Z, 6) + " " + MAPMaker.Round(textureShiftS) + "] " + MAPMaker.Round(texScaleX, 4) + "\"" + (char) 0x0D + (char) 0x0A);
				output += ("			\"vaxis\" \"[" + MAPMaker.Round(textureT.X, 6) + " " + MAPMaker.Round(textureT.Y, 6) + " " + MAPMaker.Round(textureT.Z, 6) + " " + MAPMaker.Round(textureShiftT) + "] " + MAPMaker.Round(texScaleY, 4) + "\"" + (char) 0x0D + (char) 0x0A);
				output += ("			\"rotation\" \"" + MAPMaker.Round(texRot, 4) + "\"" + (char) 0x0D + (char) 0x0A);
				output += ("			\"lightmapscale\" \"" + MAPMaker.Round(lgtScale, 4) + "\"" + (char) 0x0D + (char) 0x0A);
			} else {
				output += ("			\"plane\" \"(" + triangle[0].X + " " + triangle[0].Y + " " + triangle[0].Z + ") ");
				output += ("("                      + triangle[1].X + " " + triangle[1].Y + " " + triangle[1].Z + ") ");
				output += ("("                      + triangle[2].X + " " + triangle[2].Y + " " + triangle[2].Z + ")\"" + (char) 0x0D + (char) 0x0A);
				output += ("			\"uaxis\" \"[" + textureS.X + " " + textureS.Y + " " + textureS.Z + " " + textureShiftS + "] " + texScaleX + "\"" + (char) 0x0D + (char) 0x0A);
				output += ("			\"vaxis\" \"[" + textureT.X + " " + textureT.Y + " " + textureT.Z + " " + textureShiftT + "] " + texScaleY + "\"" + (char) 0x0D + (char) 0x0A);
				output += ("			\"rotation\" \"" + texRot + "\"" + (char) 0x0D + (char) 0x0A);
				output += ("			\"lightmapscale\" \"" + lgtScale + "\"" + (char) 0x0D + (char) 0x0A);
			}
			output += ("			\"smoothing_groups\" \"0\"" + (char) 0x0D + (char) 0x0A);
			if (inBrushSide.Displacement != null)
			{
				output += displacementToString(inBrushSide.Displacement);
			}
			output += ("		}" + (char) 0x0D + (char) 0x0A);
			return output;
		}
		catch (System.NullReferenceException)
		{
			DecompilerThread.OnMessage(this, "WARNING: Side with bad data! Not exported!");
			return null;
		}
	}
	
	private string displacementToString(MAPDisplacement inDisplacement)
	{
		string output = "			dispinfo" + (char) 0x0D + (char) 0x0A + "			{" + (char) 0x0D + (char) 0x0A;
		output += ("				\"power\" \"" + inDisplacement.Power + "\"" + (char) 0x0D + (char) 0x0A);
		output += ("				\"startposition\" \"[" + inDisplacement.Start.X + " " + inDisplacement.Start.Y + " " + inDisplacement.Start.Z + "]\"" + (char) 0x0D + (char) 0x0A);
		output += ("				\"elevation\" \"0\"" + (char) 0x0D + (char) 0x0A + "				\"subdiv\" \"0\"" + (char) 0x0D + (char) 0x0A);
		string normals = "				normals" + (char) 0x0D + (char) 0x0A + "				{" + (char) 0x0D + (char) 0x0A;
		string distances = "				distances" + (char) 0x0D + (char) 0x0A + "				{" + (char) 0x0D + (char) 0x0A;
		string alphas = "				alphas" + (char) 0x0D + (char) 0x0A + "				{" + (char) 0x0D + (char) 0x0A;
		for (int i = 0; i < System.Math.Pow(2, inDisplacement.Power) + 1; i++)
		{
			normals += ("					\"row" + i + "\" \"");
			distances += ("					\"row" + i + "\" \"");
			alphas += ("					\"row" + i + "\" \"");
			for (int j = 0; j < System.Math.Pow(2, inDisplacement.Power) + 1; j++)
			{
				normals += Math.Round(inDisplacement.getNormal(i, j).X, 6) + " " + Math.Round(inDisplacement.getNormal(i, j).Y, 6) + " " + Math.Round(inDisplacement.getNormal(i, j).Z, 6);
				distances += inDisplacement.getDist(i, j);
				alphas += inDisplacement.getAlpha(i, j);
				if (j < System.Math.Pow(2, inDisplacement.Power))
				{
					normals += " ";
					distances += " ";
					alphas += " ";
				}
			}
			normals += ("\"" + (char) 0x0D + (char) 0x0A);
			distances += ("\"" + (char) 0x0D + (char) 0x0A);
			alphas += ("\"" + (char) 0x0D + (char) 0x0A);
		}
		output += (normals + "				}" + (char) 0x0D + (char) 0x0A);
		output += (distances + "				}" + (char) 0x0D + (char) 0x0A);
		output += (alphas + "				}" + (char) 0x0D + (char) 0x0A);
		output += ("				triangle_tags" + (char) 0x0D + (char) 0x0A + "				{" + (char) 0x0D + (char) 0x0A + "				}" + (char) 0x0D + (char) 0x0A);
		output += ("				allowed_verts" + (char) 0x0D + (char) 0x0A + "				{" + (char) 0x0D + (char) 0x0A + "					\"10\" \"");
		for (int i = 0; i < 10; i++)
		{
			output += inDisplacement.AllowedVerts[i];
			if (i < 9)
			{
				output += " ";
			}
		}
		output += ("\"" + (char) 0x0D + (char) 0x0A + "				}" + (char) 0x0D + (char) 0x0A);
		output += ("			}" + (char) 0x0D + (char) 0x0A);
		return output;
	}
	
	// Turn a Q2 entity into a Hammer one. This won't magically fix every single
	// thing to work in Gearcraft, for example the Nightfire engine had no support
	// for area portals. But it should save map porters some time, especially when
	// it comes to the Capture The Flag mod.
	public virtual Entity ent38ToEntVMF(Entity inEnt)
	{
		if (!inEnt["angle"].Equals(""))
		{
			inEnt["angles"] = "0 " + inEnt["angle"] + " 0";
			inEnt.Remove("angle");
		}
		if (inEnt.attributeIs("classname", "func_wall"))
		{
			inEnt["classname"] = "func_brush";
			if (!inEnt["targetname"].Equals(""))
			{
				// Really this should depend on spawnflag 2 or 4
				inEnt["solidity"] = "0"; // TODO: Make sure the attribute is actually "solidity"
			}
			else
			{
				// 2 I believe is "Start enabled" and 4 is "toggleable", or the other way around. Not sure. Could use an OR.
				inEnt["solidity"] = "2";
			}
		}
		else
		{
			if (inEnt.attributeIs("classname", "info_player_start"))
			{
				Vector3D origin = inEnt.Origin;
				inEnt["origin"] = origin.X + " " + origin.Y + " " + (origin.Z + 18);
			}
			else
			{
				if (inEnt.attributeIs("classname", "info_player_deathmatch"))
				{
					Vector3D origin = inEnt.Origin;
					inEnt["origin"] = origin.X + " " + origin.Y + " " + (origin.Z + 18);
				}
				else
				{
					if (inEnt.attributeIs("classname", "light"))
					{
						string color = inEnt["_color"];
						string intensity = inEnt["light"];
						string[] nums = color.Split(' ');
						double[] lightNumbers = new double[4];
						for (int j = 0; j < 3 && j < nums.Length; j++)
						{
							try
							{
								lightNumbers[j] = Double.Parse(nums[j]);
								lightNumbers[j] *= 255; // Quake 2's numbers are from 0 to 1, Nightfire are from 0 to 255
							}
							catch (System.FormatException)
							{
								;
							}
						}
						try
						{
							lightNumbers[s] = System.Double.Parse(intensity) / 2; // Quake 2's light intensity is waaaaaay too bright
						}
						catch (System.FormatException)
						{
							;
						}
						inEnt.Remove("_color");
						inEnt.Remove("light");
						inEnt["_light"] = lightNumbers[r] + " " + lightNumbers[g] + " " + lightNumbers[b] + " " + lightNumbers[s];
					}
					else
					{
						if (inEnt.attributeIs("classname", "misc_teleporter"))
						{
							Vector3D origin = inEnt.Origin;
							Vector3D mins = new Vector3D(origin.X - 24, origin.Y - 24, origin.Z - 24);
							Vector3D maxs = new Vector3D(origin.X + 24, origin.Y + 24, origin.Z + 48);
							inEnt.Brushes.Add(MAPBrush.createBrush(mins, maxs, "tools/toolstrigger"));
							inEnt.Remove("origin");
							inEnt["classname"] = "trigger_teleport";
						}
						else
						{
							if (inEnt.attributeIs("classname", "misc_teleporter_dest"))
							{
								inEnt["classname"] = "info_target";
							}
						}
					}
				}
			}
		}
		return inEnt;
	}
	
	// Turn a Nightfire entity into a Hammer one.
	public virtual Entity ent42ToEntVMF(Entity inEnt)
	{
		if(inEnt.Angles[0] != 0) {
			inEnt["angles"] = (-inEnt.Angles[0])+" "+inEnt.Angles[1]+" "+inEnt.Angles[2];
		}
		if (!inEnt["body"].Equals(""))
		{
			inEnt.renameAttribute("body", "SetBodyGroup");
		}
		if (inEnt["rendercolor"].Equals("0 0 0"))
		{
			inEnt["rendercolor"] = "255 255 255";
		}
		if (inEnt["angles"].Equals("0 -1 0"))
		{
			inEnt["angles"] = "-90 0 0";
		}
		try
		{
			if (inEnt["model"].Substring(inEnt["model"].Length - 4).ToUpper().Equals(".spz".ToUpper()))
			{
				inEnt["model"] = inEnt["model"].Substring(0, (inEnt["model"].Length - 4) - (0)) + ".spr";
			}
		}
		catch (System.ArgumentOutOfRangeException)
		{
			;
		}
		if (inEnt.attributeIs("classname", "light_spot"))
		{
			try
			{
				inEnt["pitch"] = ((double) (inEnt.Angles[0] + System.Double.Parse(inEnt["pitch"]))).ToString();
			}
			catch (System.FormatException e)
			{
				inEnt["pitch"] = ((double) inEnt.Angles[0]).ToString();
			}
			try
			{
				if (System.Double.Parse(inEnt["_cone"]) > 90.0)
				{
					inEnt["_cone"] = "90";
				}
				else
				{
					if (System.Double.Parse(inEnt["_cone"]) < 0.0)
					{
						inEnt["_cone"] = "0";
					}
				}
			}
			catch (System.FormatException e)
			{
				;
			}
			try
			{
				if (System.Double.Parse(inEnt["_cone2"]) > 90.0)
				{
					inEnt["_cone2"] = "90";
				}
				else
				{
					if (System.Double.Parse(inEnt["_cone2"]) < 0.0)
					{
						inEnt["_cone2"] = "0";
					}
				}
			}
			catch (System.FormatException e)
			{
				;
			}
			inEnt.renameAttribute("_cone", "_inner_cone");
			inEnt.renameAttribute("_cone2", "_cone");
		}
		else
		{
			if (inEnt.attributeIs("classname", "func_wall"))
			{
				if (inEnt["rendermode"].Equals("0"))
				{
					inEnt["classname"] = "func_detail";
					for (int i = 0; i < inEnt.Brushes.Count; i++)
					{
						MAPBrush currentBrush = inEnt.Brushes[i];
						for (int j = 0; j < currentBrush.NumSides; j++)
						{
							MAPBrushSide currentSide = currentBrush[j];
							if (currentSide.Texture.Equals("special/TRIGGER", StringComparison.InvariantCultureIgnoreCase))
							{
								currentSide.Texture = "TOOLS/TOOLSHINT"; // Hint is the only thing that still works that doesn't collide with the player
							}
						}
					}
					inEnt.Remove("rendermode");
				}
				else
				{
					inEnt["classname"] = "func_brush";
					inEnt["solidity"] = "2";
					inEnt.Remove("angles");
				}
			}
			else
			{
				if (inEnt.attributeIs("classname", "func_wall_toggle"))
				{
					inEnt["classname"] = "func_brush";
					inEnt["solidity"] = "0";
					inEnt.Remove("angles");
					try
					{
						if (inEnt.spawnflagsSet(1))
						{
							inEnt["StartDisabled"] = "1";
							inEnt.disableSpawnflags(1);
						}
						else
						{
							inEnt["StartDisabled"] = "0";
						}
					}
					catch (System.FormatException)
					{
						inEnt["StartDisabled"] = "0";
					}
				}
				else
				{
					if (inEnt.attributeIs("classname", "func_illusionary"))
					{
						inEnt["classname"] = "func_brush";
						inEnt["solidity"] = "1";
						inEnt.Remove("angles");
					}
					else
					{
						if (inEnt.attributeIs("classname", "item_generic"))
						{
							inEnt["classname"] = "prop_dynamic";
							inEnt["solid"] = "0";
							inEnt.Remove("effects");
							inEnt.Remove("fixedlight");
						}
						else
						{
							if (inEnt.attributeIs("classname", "env_glow"))
							{
								inEnt["classname"] = "env_sprite";
							}
							else
							{
								if (inEnt.attributeIs("classname", "info_teleport_destination"))
								{
									inEnt["classname"] = "info_target";
								}
								else
								{
									if (inEnt.attributeIs("classname", "info_player_deathmatch") || inEnt.attributeIs("classname", "info_player_start"))
									{
										Vector3D origin = inEnt.Origin;
										inEnt["origin"] = origin.X + " " + origin.Y + " " + (origin.Z - 40);
									}
									else
									{
										if (inEnt.attributeIs("classname", "info_ctfspawn"))
										{
											if (inEnt["team_no"].Equals("1"))
											{
												inEnt["classname"] = "ctf_combine_player_spawn";
												inEnt.Remove("team_no");
											}
											else
											{
												if (inEnt["team_no"].Equals("2"))
												{
													inEnt["classname"] = "ctf_rebel_player_spawn";
													inEnt.Remove("team_no");
												}
											}
											Vector3D origin = inEnt.Origin;
											inEnt["origin"] = origin.X + " " + origin.Y + " " + (origin.Z - 40);
										}
										else
										{
											if (inEnt.attributeIs("classname", "item_ctfflag"))
											{
												inEnt.Remove("skin");
												inEnt.Remove("goal_min");
												inEnt.Remove("goal_max");
												inEnt.Remove("model");
												inEnt["SpawnWithCaptureEnabled"] = "1";
												if (inEnt["goal_no"].Equals("1"))
												{
													inEnt["classname"] = "ctf_combine_flag";
													inEnt["targetname"] = "combine_flag";
													inEnt.Remove("goal_no");
												}
												else
												{
													if (inEnt["goal_no"].Equals("2"))
													{
														inEnt["classname"] = "ctf_rebel_flag";
														inEnt["targetname"] = "rebel_flag";
														inEnt.Remove("goal_no");
													}
												}
											}
											else
											{
												if (inEnt.attributeIs("classname", "func_ladder"))
												{
													for (int i = 0; i < inEnt.Brushes.Count; i++)
													{
														MAPBrush currentBrush = inEnt.Brushes[i];
														for (int j = 0; j < currentBrush.NumSides; j++)
														{
															MAPBrushSide currentSide = currentBrush[j];
															currentSide.Texture = "TOOLS/TOOLSINVISIBLELADDER";
														}
													}
												}
												else
												{
													if (inEnt.attributeIs("classname", "func_door"))
													{
														inEnt["movedir"] = inEnt["angles"];
														inEnt["noise1"] = inEnt["movement_noise"];
														inEnt.Remove("movement_noise");
														inEnt.Remove("angles");
														if (inEnt.spawnflagsSet(1))
														{
															inEnt["spawnpos"] = "1";
															inEnt.disableSpawnflags(1);
														}
														inEnt["renderamt"] = "255";
													}
													else
													{
														if (inEnt.attributeIs("classname", "func_button"))
														{
															inEnt["movedir"] = inEnt["angles"];
															inEnt.Remove("angles");
															for (int i = 0; i < inEnt.Brushes.Count; i++)
															{
																MAPBrush currentBrush = inEnt.Brushes[i];
																for (int j = 0; j < currentBrush.NumSides; j++)
																{
																	MAPBrushSide currentSide = currentBrush[j];
																	if (currentSide.Texture.Equals("special/TRIGGER", StringComparison.InvariantCultureIgnoreCase))
																	{
																		currentSide.Texture = "TOOLS/TOOLSHINT"; // Hint is the only thing that still works that doesn't collide with the player
																	}
																}
															}
															if (!inEnt.spawnflagsSet(256))
															{
																// Nightfire's "touch activates" flag, same as source!
																if (!inEnt["health"].Equals("") && !inEnt["health"].Equals("0"))
																{
																	inEnt.enableSpawnflags(512);
																}
																else
																{
																	inEnt.enableSpawnflags(1024);
																}
															}
														}
														else
														{
															if (inEnt.attributeIs("classname", "trigger_hurt"))
															{
																if (inEnt.spawnflagsSet(2))
																{
																	inEnt["StartDisabled"] = "1";
																}
																if (!inEnt.spawnflagsSet(8))
																{
																	inEnt["spawnflags"] = "1";
																}
																else
																{
																	inEnt["spawnflags"] = "0";
																}
																inEnt.renameAttribute("dmg", "damage");
															}
															else
															{
																if (inEnt.attributeIs("classname", "trigger_auto"))
																{
																	inEnt["classname"] = "logic_auto";
																}
																else
																{
																	if (inEnt.attributeIs("classname", "trigger_once") || inEnt.attributeIs("classname", "trigger_multiple"))
																	{
																		if (inEnt.spawnflagsSet(8) || inEnt.spawnflagsSet(1))
																		{
																			inEnt.disableSpawnflags(1);
																			inEnt.disableSpawnflags(8);
																			inEnt.enableSpawnflags(2);
																		}
																		if (inEnt.spawnflagsSet(2))
																		{
																			inEnt.disableSpawnflags(1);
																		}
																		else
																		{
																			inEnt.enableSpawnflags(1);
																		}
																	}
																	else
																	{
																		if (inEnt.attributeIs("classname", "func_door_rotating"))
																		{
																			if (inEnt.spawnflagsSet(1))
																			{
																				inEnt["spawnpos"] = "1";
																				inEnt.disableSpawnflags(1);
																			}
																			inEnt["noise1"] = inEnt["movement_noise"];
																			inEnt.Remove("movement_noise");
																		}
																		else
																		{
																			if (inEnt.attributeIs("classname", "trigger_push"))
																			{
																				inEnt["pushdir"] = inEnt["angles"];
																				inEnt.Remove("angles");
																			}
																			else
																			{
																				if (inEnt.attributeIs("classname", "light_environment"))
																				{
																					Entity newShadowControl = new Entity("shadow_control");
																					Entity newEnvSun = new Entity("env_sun");
																					newShadowControl["angles"] = inEnt["angles"];
																					newEnvSun["angles"] = inEnt["angles"];
																					newShadowControl["origin"] = inEnt["origin"];
																					newEnvSun["origin"] = inEnt["origin"];
																					newShadowControl["color"] ="128 128 128";
																					data.Add(newShadowControl);
																					data.Add(newEnvSun);
																				}
																				else
																				{
																					if (inEnt.attributeIs("classname", "func_rot_button"))
																					{
																						inEnt.Remove("angles");
																						for (int i = 0; i < inEnt.Brushes.Count; i++)
																						{
																							MAPBrush currentBrush = inEnt.Brushes[i];
																							for (int j = 0; j < currentBrush.NumSides; j++)
																							{
																								MAPBrushSide currentSide = currentBrush[j];
																								if (currentSide.Texture.ToUpper().Equals("special/TRIGGER".ToUpper()))
																								{
																									currentSide.Texture = "TOOLS/TOOLSHINT"; // Hint is the only thing that still works that doesn't collide with the player
																								}
																							}
																						}
																						if (!inEnt.spawnflagsSet(256))
																						{
																							// Nightfire's "touch activates" flag, same as source!
																							if (!inEnt["health"].Equals("") && !inEnt["health"].Equals("0"))
																							{
																								inEnt.enableSpawnflags(512);
																							}
																							else
																							{
																								inEnt.enableSpawnflags(1024);
																							}
																						}
																					}
																					else
																					{
																						if (inEnt.attributeIs("classname", "func_tracktrain"))
																						{
																							inEnt.renameAttribute("movesnd", "MoveSound");
																							inEnt.renameAttribute("stopsnd", "StopSound");
																						}
																						else
																						{
																							if (inEnt.attributeIs("classname", "path_track"))
																							{
																								if (inEnt.spawnflagsSet(1))
																								{
																									inEnt.Remove("targetname");
																								}
																							}
																							else
																							{
																								if (inEnt.attributeIs("classname", "trigger_relay"))
																								{
																									inEnt["classname"] = "logic_relay";
																								}
																								else
																								{
																									if (inEnt.attributeIs("classname", "trigger_counter"))
																									{
																										inEnt["classname"] = "math_counter";
																										inEnt["max"] = inEnt["count"];
																										inEnt["min"] = "0";
																										inEnt["startvalue"] = "0";
																										inEnt.Remove("count");
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	} // Lol
																} // so
															} // many
														} // closing
													} // braces
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		return inEnt;
	}
	
	// Turn a triggering entity (like a func_button or trigger_multiple) into a Source
	// engine trigger using entity I/O. There's a few complications to this: There's
	// no generic output which always acts like the triggers in other engines, and there's
	// no "Fire" input. I try to figure out which ones are best based on their classnames
	// but it's not 100% foolproof, and I have to add a case for every specific class.
	public virtual Entity parseEntityIO(Entity inEnt) {
		if (!(inEnt["target"]=="")) {
			double delay = 0.0;
			try {
				delay = Double.Parse(inEnt["delay"]);
			} catch (System.FormatException) { ; }
			if (!inEnt["target"].Equals("")) {
				Entity[] targets = getTargets(inEnt["target"]);
				for (int i = 0; i < targets.Length; i++) {
					if (targets[i].attributeIs("classname", "multi_manager") || targets[i].attributeIs("classname", "multi_kill_manager")) {
						Entity mm = parseMultimanager(targets[i]);
						//for (int j = 0; j < mm.Attributes.Count; j++) {
						foreach (Tuple<string, string, string, string, double, int, string, Tuple<string>> connection in mm.Connections) {
							if (inEnt.attributeIs("classname", "logic_relay") && inEnt.Attributes.ContainsKey("delay")) {
								inEnt.Connections.Add(new Tuple<string, string, string, string, double, int, string, Tuple<string>>("OnTrigger", connection.Item2, connection.Item3, connection.Item4, connection.Item5 + delay, connection.Item6, "", new Tuple<string>("")));
							} else {
								inEnt.Connections.Add(new Tuple<string, string, string, string, double, int, string, Tuple<string>>(inEnt.fireAction(), connection.Item2, connection.Item3, connection.Item4, connection.Item5, connection.Item6, "", new Tuple<string>("")));
							}
						}
					} else {
						string outputAction = targets[i].onFire();
						if (inEnt.attributeIs("triggerstate", "0")) {
							outputAction = targets[i].onDisable();
						} else {
							if (inEnt.attributeIs("triggerstate", "1")) {
								outputAction = targets[i].onEnable();
							}
						}
						inEnt.Connections.Add(new Tuple<string, string, string, string, double, int, string, Tuple<string>>(inEnt.fireAction(), targets[i]["targetname"], outputAction, "", delay, -1, "", new Tuple<string>("")));
					}
				}
			}
			if (!inEnt["killtarget"].Equals(""))
			{
				inEnt.Connections.Add(new Tuple<string, string, string, string, double, int, string, Tuple<string>>(inEnt.fireAction(), inEnt["killtarget"], "Kill", "", delay, -1, "", new Tuple<string>("")));
			}
			inEnt.Remove("target");
			inEnt.Remove("killtarget");
			inEnt.Remove("triggerstate");
			inEnt.Remove("delay");
		}
		return inEnt;
	}
	
	// Multimanagers are also a special case. There are none in Source. Instead, I
	// need to add EVERY targetted entity in a multimanager to the original trigger
	// entity as an output with the specified delay. Things get even more complicated
	// when a multi_manager fires another multi_manager. In this case, this method will
	// recurse on itself until all the complexity is worked out.
	// One potential problem is if two multi_managers continuously call each other, this
	// method will recurse infinitely until there is a stack overflow. This might happen
	// when there is some sort of cycle going on in the map and multi_managers call each
	// other recursively to run the cycle with a delay. I solve this with an atrificial
	// limit of 8 multimanager recursions.
	// TODO: It would be better to detect this problem when it happens.
	// TODO: Instead of adding more attributes, parse into connections.
	private Entity parseMultimanager(Entity inEnt)
	{
		mmStackLength++;
		Entity dummy = new Entity(inEnt);
		dummy.Remove("classname");
		dummy.Remove("origin");
		dummy.Remove("angles");
		dummy.Remove("targetname");
		List<string> delete = new List<string>();
		foreach(string st in dummy.Attributes.Keys) {
			string target = st;
			double delay = 0.0;
			try {
				delay = Double.Parse(dummy[st]);
			} catch {
			}
			for (int j = target.Length - 1; j >= 0; j--)
			{
				if (target[j] == '#')
				{
					target = target.Substring(0, (j) - (0));
					//dummy.renameAttribute(st, target);
					break;
				}
			}
			Entity[] targets = getTargets(target);
			delete.Add(st);
			for (int j = 0; j < targets.Length; j++)
			{
				if (inEnt.attributeIs("classname", "multi_kill_manager"))
				{
					if (targets.Length > 1)
					{
						dummy.Connections.Add(new Tuple<string, string, string, string, double, int, string, Tuple<string>>("condition", target + j, "Kill", "", delay, -1, "", new Tuple<string>("")));
					}
					else
					{
						dummy.Connections.Add(new Tuple<string, string, string, string, double, int, string, Tuple<string>>("condition", target, "Kill", "", delay, -1, "", new Tuple<string>("")));
					}
				}
				else
				{
					if (targets[j].attributeIs("classname", "multi_manager") || targets[j].attributeIs("classname", "multi_kill_manager"))
					{
						if (mmStackLength <= Settings.MMStackSize)
						{
							Entity mm = parseMultimanager(targets[j]);
							foreach (Tuple<string, string, string, string, double, int, string, Tuple<string>> connection in mm.Connections) {
								dummy.Connections.Add(new Tuple<string, string, string, string, double, int, string, Tuple<string>>(connection.Item1, connection.Item2, connection.Item3, connection.Item4, connection.Item5 + delay, connection.Item6, connection.Item7, connection.Rest));
							}
						}
						else
						{
							DecompilerThread.OnMessage(this, "WARNING: Multimanager stack overflow on entity " + inEnt["targetname"] + " calling " + targets[j]["targetname"] + "!");
							DecompilerThread.OnMessage(this, "This is probably because of multi_managers repeatedly calling eachother. You can increase multimanager stack size in debug options.");
						}
					}
					else
					{
						if (targets.Length > 1)
						{
							//for(int k = 0; k < targets.Length; k++) {
								string outputAction = targets[j].onFire();
								if (inEnt.attributeIs("triggerstate", "0"))
								{
									outputAction = targets[j].onDisable();
								}
								else
								{
									if (inEnt.attributeIs("triggerstate", "1"))
									{
										outputAction = targets[j].onEnable();
									}
								}
								dummy.Connections.Add(new Tuple<string, string, string, string, double, int, string, Tuple<string>>("condition", target+j, outputAction, "", delay, -1, "", new Tuple<string>("")));
							//}
						}
						else if(targets.Length == 1) {
							string outputAction = targets[0].onFire();
							if (inEnt.attributeIs("triggerstate", "0"))
							{
								outputAction = targets[0].onDisable();
							}
							else
							{
								if (inEnt.attributeIs("triggerstate", "1"))
								{
									outputAction = targets[0].onEnable();
								}
							}
							dummy.Connections.Add(new Tuple<string, string, string, string, double, int, string, Tuple<string>>("condition", target, outputAction, "", delay, -1, "", new Tuple<string>("")));
						} else {
							dummy.Connections.Add(new Tuple<string, string, string, string, double, int, string, Tuple<string>>("condition", target, "Toggle", "", delay, -1, "", new Tuple<string>("")));
						}
					}
				}
			}
		}
		foreach(string st in delete) {
			dummy.Remove(st);
		}
		mmStackLength--;
		return dummy;
	}
	
	// Since Source also requires explicit enable/disable on/off events (and many
	// entities don't support the "Toggle" input) I can't have multiple entities
	// with the same targetname. So these need to be distinguished and tracked.
	private Entity[] getTargets(string name)
	{
		bool numeralized = false;
		Entity[] targets;
		int numNumeralized = 0;
		for (int i = 0; i < numeralizedTargetnames.Length; i++)
		{
			if (numeralizedTargetnames[i].Equals(name))
			{
				numeralized = true;
				numNumeralized = numTargets[i];
				break;
			}
		}
		if (numeralized)
		{
			targets = new Entity[numNumeralized];
			for (int i = 0; i < numNumeralized; i++)
			{
				targets[i] = data.returnWithName(name + i);
			}
		}
		else
		{
			targets = data.returnAllWithName(name);
			if (targets.Length > 1)
			{
				// Make sure each target needs its own Fire action and name
				bool unique = false;
				for (int i = 1; i < targets.Length; i++)
				{
					if (!targets[0].onFire().Equals(targets[i].onFire()))
					{
						unique = true;
						break;
					}
				}
				if (!unique)
				{
					return new Entity[]{targets[0]};
				}
				string[] newList = new string[numeralizedTargetnames.Length + 1];
				int[] newNumTargets = new int[newList.Length];
				for (int i = 0; i < numeralizedTargetnames.Length; i++)
				{
					newList[i] = numeralizedTargetnames[i];
					newNumTargets[i] = numTargets[i];
				}
				newList[newList.Length - 1] = name;
				newNumTargets[newList.Length - 1] = targets.Length;
				numeralizedTargetnames = newList;
				numTargets = newNumTargets;
				for (int i = 0; i < targets.Length; i++)
				{
					targets[i]["targetname"] = name + i;
				}
			}
		}
		return targets;
	}
	
	// ACCESSORS/MUTATORS
}