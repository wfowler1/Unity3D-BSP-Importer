using System.Collections.Generic;

// MAP510Writer class
//
// Writes a Gearcraft .MAP file from a passed Entities object
using System;

public class MAP510Writer {
	
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
	
	private static bool ctfEnts = false;
	
	// CONSTRUCTORS
	
	public MAP510Writer(Entities from, string to, mapType BSPVersion) {
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
	public virtual void write()
	{
		// Preprocessing entity corrections
		if (!Settings.noEntCorrection) {
			if (BSPVersion != mapType.TYPE_NIGHTFIRE)
			{
				Entity waterEntity = null;
				bool newEnt = false;
				for (int i = 0; i < data.Count; i++)
				{
					if (data[i]["classname"].Equals("func_water", StringComparison.InvariantCultureIgnoreCase))
					{
						waterEntity = data[i];
						break;
					}
					else
					{
						try
						{
							if (data[i]["classname"].Substring(0, 8).Equals("team_CTF", StringComparison.InvariantCultureIgnoreCase) || data[i]["classname"].Equals("ctf_flag_hardcorps", StringComparison.InvariantCultureIgnoreCase))
							{
								ctfEnts = true;
							}
						}
						catch (System.ArgumentOutOfRangeException)
						{
							;
						}
					}
				}
				if (waterEntity == null)
				{
					newEnt = true;
					waterEntity = new Entity("func_water");
					waterEntity["rendercolor"] = "0 0 0";
					waterEntity["speed"] = "100";
					waterEntity["wait"] = "4";
					waterEntity["skin"] = "-3";
					waterEntity["WaveHeight"] = "3.2";
				} // TODO: Y U NO WORK?!?!?
				for (int i = 0; i < data[0].Brushes.Count; i++)
				{
					if (data[0].Brushes[i].Water)
					{
						waterEntity.Brushes.Add(data[0].Brushes[i]);
						data[0].Brushes.RemoveAt(i);
						i--;
					}
				}
				if (newEnt && waterEntity.Brushes.Count != 0)
				{
					data.Add(waterEntity);
				}
			}
			// Correct some attributes of entities
			for (int i = 0; i < data.Count; i++)
			{
				Entity current = data[i];
				switch (BSPVersion)
				{
					case mapType.TYPE_QUAKE3: 
					case mapType.TYPE_FAKK: 
					case mapType.TYPE_RAVEN: 
					case mapType.TYPE_MOHAA: 
					case mapType.TYPE_STEF2: 
					case mapType.TYPE_STEF2DEMO: 
						current = ent46ToEntM510(current);
						break;
				
					case mapType.TYPE_NIGHTFIRE:  // Nightfire
						current = ent42ToEntM510(current);
						break;
				
					case mapType.TYPE_QUAKE2: 
					case mapType.TYPE_SIN: 
					case mapType.TYPE_SOF: 
						current = ent38ToEntM510(current);
						break;
				
					case mapType.TYPE_DOOM: 
					case mapType.TYPE_HEXEN: 
						current = entDoomToEntM510(current);
						break;
				
					case mapType.TYPE_COD: 
					case mapType.TYPE_COD2: 
					case mapType.TYPE_COD4: 
						current = ent59ToEntM510(current);
						break;
				
					case mapType.TYPE_SOURCE17: 
					case mapType.TYPE_SOURCE18: 
					case mapType.TYPE_SOURCE19: 
					case mapType.TYPE_SOURCE20: 
					case mapType.TYPE_SOURCE21: 
					case mapType.TYPE_SOURCE22: 
					case mapType.TYPE_SOURCE23: 
					case mapType.TYPE_DMOMAM: 
					case mapType.TYPE_VINDICTUS: 
					case mapType.TYPE_TACTICALINTERVENTION: 
						current = entSourceToEntM510(current);
						break;
					}
			}
		}
		
		byte[][] entityBytes = new byte[data.Count][];
		int totalLength = 0;
		for (currentEntity = 0; currentEntity < data.Count; currentEntity++)
		{
			try
			{
				entityBytes[currentEntity] = entityToByteArray(data[currentEntity], currentEntity);
			}
			catch (System.IndexOutOfRangeException)
			{
				// This happens when entities are added after the array is made
				byte[][] newList = new byte[data.Count][]; // Create a new array with the new length
				for (int j = 0; j < entityBytes.Length; j++)
				{
					newList[j] = entityBytes[j];
				}
				newList[currentEntity] = entityToByteArray(data[currentEntity], currentEntity);
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
	private byte[] entityToByteArray(Entity inputData, int num)
	{
		byte[] outputData;
		Vector3D origin = inputData.Origin;
		if (inputData["classname"].Equals("worldspawn", StringComparison.InvariantCultureIgnoreCase))
		{
			inputData["mapversion"] = "510";
			if (ctfEnts)
			{
				inputData["defaultctf"] = "1";
			}
		}
		string temp = "{ // Entity "+num;
		int len = temp.Length+5; // Closing curly brace and two newlines
		// Get the lengths of all attributes together
		//for (int i = 0; i < inputData.Attributes.Count; i++) {
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
				outputData[j + offset] = (byte) key[j]; // add it to the output array
			}
			offset += key.Length;
			outputData[offset++] = (byte)'\"'; // 2
			outputData[offset++] = (byte)' '; // 3
			outputData[offset++] = (byte)'\"'; // 4
			for (int j = 0; j < inputData.Attributes[key].Length; j++) {
				// Then for each byte in the attribute
				outputData[j + offset] = (byte) inputData.Attributes[key][j]; // add it to the output array
			}
			offset += inputData.Attributes[key].Length;
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
	
	private byte[] brushToByteArray(MAPBrush inputData, int num) {
		if (inputData.NumSides < 4) {
			// Can't create a brush with less than 4 sides
			DecompilerThread.OnMessage(this, "WARNING: Tried to create brush from " + inputData.NumSides + " sides!");
			return new byte[0];
		}
		string brush = "{ // Brush " + num + (char) 0x0D + (char) 0x0A;
		if ((inputData.Detail || inputData[0].Displacement != null) && currentEntity == 0)
		{
			brush += ("\"BRUSHFLAGS\" \"DETAIL\"" + (char) 0x0D + (char) 0x0A);
		}
		for (int i = 0; i < inputData.NumSides; i++)
		{
			brush += (brushSideToString(inputData[i]) + (char) 0x0D + (char) 0x0A);
		}
		brush += ("}" + (char) 0x0D + (char) 0x0A);
		if (brush.Length < 45) {
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
	
	private string brushSideToString(MAPBrushSide inputData)
	{
		try
		{
			Vector3D[] triangle = inputData.Triangle;
			string texture = inputData.Texture;
			Vector3D textureS = inputData.TextureS;
			Vector3D textureT = inputData.TextureT;
			double textureShiftS = inputData.TextureShiftS;
			double textureShiftT = inputData.TextureShiftT;
			float texRot = inputData.TexRot;
			double texScaleX = inputData.TexScaleX;
			double texScaleY = inputData.TexScaleY;
			int flags = inputData.Flags;
			string material = inputData.Material;
			double lgtScale = inputData.LgtScale;
			double lgtRot = inputData.LgtRot;
			// Correct special textures on Q2 maps
			if (!Settings.noTexCorrection)
			{
				if (BSPVersion == mapType.TYPE_QUAKE2 || BSPVersion == mapType.TYPE_SIN)
				{
					// Many of the special textures are taken care of in the decompiler method itself
					try
					{
						// using face flags, rather than texture names.
						if (texture.Substring(texture.Length - 8).ToUpper().Equals("/trigger".ToUpper()))
						{
							texture = "special/trigger";
						}
						else
						{
							if (texture.Substring(texture.Length - 5).ToUpper().Equals("/clip".ToUpper()))
							{
								texture = "special/clip";
							}
							else
							{
								if (texture.ToUpper().Equals("*** unsused_texinfo ***".ToUpper()))
								{
									texture = "special/nodraw";
								}
							}
						}
					}
					catch (System.ArgumentOutOfRangeException)
					{
						;
					}
				}
				if (BSPVersion == mapType.TYPE_SOURCE17 || BSPVersion == mapType.TYPE_SOURCE18 || BSPVersion == mapType.TYPE_SOURCE19 || BSPVersion == mapType.TYPE_SOURCE20 || BSPVersion == mapType.TYPE_SOURCE21 || BSPVersion == mapType.TYPE_SOURCE22 || BSPVersion == mapType.TYPE_SOURCE23 || BSPVersion == mapType.TYPE_DMOMAM || BSPVersion == mapType.TYPE_VINDICTUS || BSPVersion == mapType.TYPE_TACTICALINTERVENTION)
				{
					try
					{
						if (texture.ToUpper().Equals("tools/toolshint".ToUpper()))
						{
							texture = "special/hint";
						}
						else
						{
							if (texture.ToUpper().Equals("tools/toolsskip".ToUpper()))
							{
								texture = "special/skip";
							}
							else
							{
								if (texture.ToUpper().Equals("tools/toolsclip".ToUpper()))
								{
									texture = "special/clip";
								}
								else
								{
									if (texture.ToUpper().Equals("tools/toolstrigger".ToUpper()) || texture.ToUpper().Equals("TOOLS/TOOLSFOG".ToUpper()))
									{
										texture = "special/trigger";
									}
									else
									{
										if (texture.ToUpper().Equals("tools/TOOLSSKYBOX".ToUpper()))
										{
											texture = "special/sky";
										}
										else
										{
											if (texture.ToUpper().Equals("tools/toolsnodraw".ToUpper()))
											{
												texture = "special/nodraw";
											}
											else
											{
												if (texture.ToUpper().Equals("TOOLS/TOOLSPLAYERCLIP".ToUpper()))
												{
													texture = "special/playerclip";
												}
												else
												{
													if (texture.ToUpper().Equals("TOOLS/TOOLSNPCCLIP".ToUpper()))
													{
														texture = "special/enemyclip";
													}
													else
													{
														if (texture.ToUpper().Equals("TOOLS/TOOLSBLACK".ToUpper()))
														{
															texture = "special/black";
														}
														else
														{
															if (texture.ToUpper().Equals("TOOLS/TOOLSINVISIBLE".ToUpper()))
															{
																texture = "special/clip";
															}
															else
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
												}
											}
										}
									}
								}
							}
						}
					}
					catch (System.ArgumentOutOfRangeException)
					{
						;
					}
				}
				if (BSPVersion == mapType.TYPE_QUAKE3 || BSPVersion == mapType.TYPE_MOHAA || BSPVersion == mapType.TYPE_COD || BSPVersion == mapType.TYPE_STEF2 || BSPVersion == mapType.TYPE_STEF2DEMO || BSPVersion == mapType.TYPE_FAKK)
				{
					try
					{
						if (texture.Substring(0, (9) - (0)).ToUpper().Equals("textures/".ToUpper()))
						{
							texture = texture.Substring(9);
						}
					}
					catch (System.ArgumentOutOfRangeException)
					{
						;
					}
					if (texture.ToUpper().Equals("common/clip".ToUpper()))
					{
						texture = "special/clip";
					}
					else
					{
						if (texture.ToUpper().Equals("common/trigger".ToUpper()))
						{
							texture = "special/trigger";
						}
						else
						{
							if (texture.ToUpper().Equals("noshader".ToUpper()))
							{
								texture = "special/nodraw";
							}
							else
							{
								if (texture.ToUpper().Equals("common/physics_clip".ToUpper()))
								{
									texture = "special/clip";
								}
								else
								{
									if (texture.ToUpper().Equals("common/caulk".ToUpper()) || texture.ToUpper().Equals("common/caulkshadow".ToUpper()))
									{
										texture = "special/nodraw";
									}
									else
									{
										if (texture.ToUpper().Equals("common/do_not_enter".ToUpper()) || texture.ToUpper().Equals("common/donotenter".ToUpper()) || texture.ToUpper().Equals("common/monsterclip".ToUpper()))
										{
											texture = "special/npcclip";
										}
										else
										{
											if (texture.ToUpper().Equals("common/caulksky".ToUpper()) || texture.ToUpper().Equals("common/skyportal".ToUpper()))
											{
												texture = "special/sky";
											}
											else
											{
												if (texture.ToUpper().Equals("common/hint".ToUpper()))
												{
													texture = "special/hint";
												}
												else
												{
													if (texture.ToUpper().Equals("common/nodraw".ToUpper()))
													{
														texture = "special/nodraw";
													}
													else
													{
														if (texture.ToUpper().Equals("common/metalclip".ToUpper()))
														{
															texture = "special/clip";
														}
														else
														{
															if (texture.ToUpper().Equals("common/grassclip".ToUpper()))
															{
																texture = "special/clip";
															}
															else
															{
																if (texture.ToUpper().Equals("common/paperclip".ToUpper()))
																{
																	texture = "special/clip";
																}
																else
																{
																	if (texture.ToUpper().Equals("common/woodclip".ToUpper()))
																	{
																		texture = "special/clip";
																	}
																	else
																	{
																		if (texture.ToUpper().Equals("common/waterskip".ToUpper()))
																		{
																			texture = "liquids/!water";
																		}
																		else
																		{
																			if (texture.ToUpper().Equals("common/glassclip".ToUpper()))
																			{
																				texture = "special/clip";
																			}
																			else
																			{
																				if (texture.ToUpper().Equals("common/playerclip".ToUpper()))
																				{
																					texture = "special/playerclip";
																				}
																				else
																				{
																					if (texture.ToUpper().Equals("common/nodrawnonsolid".ToUpper()))
																					{
																						texture = "special/trigger";
																					}
																					else
																					{
																						if (texture.ToUpper().Equals("common/clipfoliage".ToUpper()))
																						{
																							texture = "special/clip";
																						}
																						else
																						{
																							if (texture.ToUpper().Equals("common/foliageclip".ToUpper()))
																							{
																								texture = "special/clip";
																							}
																							else
																							{
																								if (texture.ToUpper().Equals("common/carpetclip".ToUpper()))
																								{
																									texture = "special/clip";
																								}
																								else
																								{
																									if (texture.ToUpper().Equals("common/dirtclip".ToUpper()))
																									{
																										texture = "special/clip";
																									}
																									else
																									{
																										try
																										{
																											if (texture.Substring(0, (4) - (0)).ToUpper().Equals("sky/".ToUpper()))
																											{
																												texture = "special/sky";
																											}
																										}
																										catch (System.ArgumentOutOfRangeException)
																										{
																											; // I couldn't give a fuck
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
				}
				if (BSPVersion == mapType.TYPE_RAVEN)
				{
					try
					{
						if (texture.Substring(0, (9) - (0)).ToUpper().Equals("textures/".ToUpper()))
						{
							texture = texture.Substring(9);
						}
					}
					catch (System.ArgumentOutOfRangeException)
					{
						;
					}
					if (texture.ToUpper().Equals("system/clip".ToUpper()))
					{
						texture = "special/clip";
					}
					else
					{
						if (texture.ToUpper().Equals("system/trigger".ToUpper()))
						{
							texture = "special/trigger";
						}
						else
						{
							if (texture.ToUpper().Equals("noshader".ToUpper()))
							{
								texture = "special/nodraw";
							}
							else
							{
								if (texture.ToUpper().Equals("system/physics_clip".ToUpper()))
								{
									texture = "special/clip";
								}
								else
								{
									if (texture.ToUpper().Equals("system/caulk".ToUpper()))
									{
										texture = "special/nodraw";
									}
									else
									{
										if (texture.ToUpper().Equals("system/do_not_enter".ToUpper()))
										{
											texture = "special/nodraw";
										}
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
				textureS = TexInfo.textureAxisFromPlane(inputData.Plane)[0];
			}
			if(Double.IsInfinity(textureT.X) || Double.IsNaN(textureT.X) || Double.IsInfinity(textureT.Y) || Double.IsNaN(textureT.Y) || Double.IsInfinity(textureT.Z) || Double.IsNaN(textureT.Z)) {
				textureT = TexInfo.textureAxisFromPlane(inputData.Plane)[1];
			}
			if (Settings.roundNums)
			{
				return "( " + MAPMaker.FormattedRound(triangle[0].X, 6, "######0.000000") + " " + MAPMaker.FormattedRound(triangle[0].Y, 6, "######0.000000") + " " + MAPMaker.FormattedRound(triangle[0].Z, 6, "######0.000000") + " ) " + 
				       "( " + MAPMaker.FormattedRound(triangle[1].X, 6, "######0.000000") + " " + MAPMaker.FormattedRound(triangle[1].Y, 6, "######0.000000") + " " + MAPMaker.FormattedRound(triangle[1].Z, 6, "######0.000000") + " ) " + 
				       "( " + MAPMaker.FormattedRound(triangle[2].X, 6, "######0.000000") + " " + MAPMaker.FormattedRound(triangle[2].Y, 6, "######0.000000") + " " + MAPMaker.FormattedRound(triangle[2].Z, 6, "######0.000000") + " ) " + 
				       texture + 
				       " [ " + MAPMaker.FormattedRound(textureS.X, 6, "######0.000000") + " " + MAPMaker.FormattedRound(textureS.Y, 6, "######0.000000") + " " + MAPMaker.FormattedRound(textureS.Z, 6, "######0.000000") + " " + MAPMaker.Round(textureShiftS) + " ]" + 
				       " [ " + MAPMaker.FormattedRound(textureT.X, 6, "######0.000000") + " " + MAPMaker.FormattedRound(textureT.Y, 6, "######0.000000") + " " + MAPMaker.FormattedRound(textureT.Z, 6, "######0.000000") + " " + MAPMaker.Round(textureShiftT) + " ] " + 
				       MAPMaker.FormattedRound(texRot, 4, "######0.####") + " " + MAPMaker.FormattedRound(texScaleX, 4, "######0.####") + " " + MAPMaker.FormattedRound(texScaleY, 4, "######0.####") + " " + flags + " " + material + " [ " + MAPMaker.FormattedRound(lgtScale, 4, "######0.####") + " " + MAPMaker.FormattedRound(lgtRot, 4, "######0.####") + " ]";
			}
			else
			{
				return "( " + triangle[0].X + " " + triangle[0].Y + " " + triangle[0].Z + " ) " + "( " + triangle[1].X + " " + triangle[1].Y + " " + triangle[1].Z + " ) " + "( " + triangle[2].X + " " + triangle[2].Y + " " + triangle[2].Z + " ) " + texture + " [ " + textureS.X + " " + textureS.Y + " " + textureS.Z + " " + textureShiftS + " ]" + " [ " + textureT.X + " " + textureT.Y + " " + textureT.Z + " " + textureShiftT + " ] " + texRot + " " + texScaleX + " " + texScaleY + " " + flags + " " + material + " [ " + lgtScale + " " + lgtRot + " ]";
			}
		}
		catch (System.NullReferenceException)
		{
			DecompilerThread.OnMessage(this, "WARNING: Side with bad data! Not exported!");
			return "";
		}
	}
	
	public virtual Entity ent42ToEntM510(Entity inputData)
	{
		if (inputData.BrushBased)
		{
			Vector3D origin = inputData.Origin;
			inputData.Attributes.Remove("origin");
			inputData.Attributes.Remove("model");
			if ((origin[0] != 0 || origin[1] != 0 || origin[2] != 0) && !Settings.noOriginBrushes)
			{
				// If this brush uses the "origin" attribute
				MAPBrush newOriginBrush = MAPBrush.createBrush(new Vector3D(- Settings.originBrushSize, - Settings.originBrushSize, - Settings.originBrushSize), new Vector3D(Settings.originBrushSize, Settings.originBrushSize, Settings.originBrushSize), "special/origin");
				inputData.Brushes.Add(newOriginBrush);
			}
			for (int i = 0; i < inputData.Brushes.Count; i++)
			{
				MAPBrush currentBrush = inputData.Brushes[i];
				currentBrush.translate(new Vector3D(origin));
			}
		}
		return inputData;
	}
	
	// Turn a CoD entity into a Gearcraft one.
	public virtual Entity ent59ToEntM510(Entity inputData)
	{
		if (inputData.BrushBased)
		{
			Vector3D origin = inputData.Origin;
			inputData.Attributes.Remove("origin");
			inputData.Attributes.Remove("model");
			if (inputData["classname"].ToUpper().Equals("func_rotating".ToUpper()))
			{
				// TODO: What entities require origin brushes in CoD?
				if ((origin[0] != 0 || origin[1] != 0 || origin[2] != 0) && !Settings.noOriginBrushes)
				{
					// If this brush uses the "origin" attribute
					MAPBrush newOriginBrush = MAPBrush.createBrush(new Vector3D(- Settings.originBrushSize, - Settings.originBrushSize, - Settings.originBrushSize), new Vector3D(Settings.originBrushSize, Settings.originBrushSize, Settings.originBrushSize), "special/origin");
					inputData.Brushes.Add(newOriginBrush);
				}
			}
			for (int i = 0; i < inputData.Brushes.Count; i++)
			{
				MAPBrush currentBrush = inputData.Brushes[i];
				currentBrush.translate(new Vector3D(origin));
			}
		}
		else
		{
			if (inputData["classname"].ToUpper().Equals("light".ToUpper()))
			{
				inputData["_light"] = "255 255 255 " + inputData["light"];
				inputData.Attributes.Remove("light");
			}
			else
			{
				if (inputData["classname"].ToUpper().Equals("mp_deathmatch_spawn".ToUpper()))
				{
					inputData["classname"] = "info_player_deathmatch";
				}
				else
				{
					if (inputData["classname"].ToUpper().Equals("mp_teamdeathmatch_spawn".ToUpper()))
					{
						inputData["classname"] = "info_player_deathmatch";
					}
					else
					{
						if (inputData["classname"].ToUpper().Equals("mp_searchanddestroy_spawn_allied".ToUpper()))
						{
							inputData["classname"] = "info_player_ctfspawn";
							inputData["team_no"] = "1";
							inputData.Attributes.Remove("model");
						}
						else
						{
							if (inputData["classname"].ToUpper().Equals("mp_searchanddestroy_spawn_axis".ToUpper()))
							{
								inputData["classname"] = "info_player_ctfspawn";
								inputData["team_no"] = "2";
								inputData.Attributes.Remove("model");
							}
						}
					}
				}
			}
		}
		return inputData;
	}
	
	// Turn a Q3 entity into a Gearcraft one (generally for use with nightfire)
	// This won't magically fix every single thing to work in Gearcraft, for example
	// the Nightfire engine had no support for area portals. But it should save map
	// porters some time, especially when it comes to the Capture The Flag mod.
	public virtual Entity ent46ToEntM510(Entity inputData)
	{
		if (inputData.BrushBased)
		{
			Vector3D origin = inputData.Origin;
			inputData.Attributes.Remove("origin");
			inputData.Attributes.Remove("model");
			if (inputData.attributeIs("classname", "func_rotating") || inputData.attributeIs("classname", "func_rotatingdoor"))
			{
				// TODO: What entities require origin brushes in Quake 3?
				if ((origin[0] != 0 || origin[1] != 0 || origin[2] != 0) && !Settings.noOriginBrushes)
				{
					// If this brush uses the "origin" attribute
					MAPBrush newOriginBrush = MAPBrush.createBrush(new Vector3D(- Settings.originBrushSize, - Settings.originBrushSize, - Settings.originBrushSize), new Vector3D(Settings.originBrushSize, Settings.originBrushSize, Settings.originBrushSize), "special/origin");
					inputData.Brushes.Add(newOriginBrush);
				}
			}
			for (int i = 0; i < inputData.Brushes.Count; i++)
			{
				inputData.Brushes[i].translate(origin);
			}
		}
		if (inputData["classname"].ToUpper().Equals("team_CTF_blueflag".ToUpper()))
		{
			// Blue flag
			inputData["classname"] = "item_ctfflag";
			inputData["skin"] = "1"; // 0 for PHX, 1 for MI6
			inputData["goal_no"] = "1"; // 2 for PHX, 1 for MI6
			inputData["goal_max"] = "16 16 72";
			inputData["goal_min"] = "-16 -16 0";
			inputData["model"] = "models/ctf_flag.mdl";
			Entity flagBase = new Entity("item_ctfbase");
			flagBase["origin"] = inputData["origin"];
			flagBase["angles"] = inputData["angles"];
			flagBase["angle"] = inputData["angle"];
			flagBase["goal_no"] = "1";
			flagBase["model"] = "models/ctf_flag_stand_mi6.mdl";
			flagBase["goal_max"] = "16 16 72";
			flagBase["goal_min"] = "-16 -16 0";
			data.Add(flagBase);
		}
		else
		{
			if (inputData["classname"].ToUpper().Equals("team_CTF_redflag".ToUpper()))
			{
				// Red flag
				inputData["classname"] = "item_ctfflag";
				inputData["skin"] = "0"; // 0 for PHX, 1 for MI6
				inputData["goal_no"] = "2"; // 2 for PHX, 1 for MI6
				inputData["goal_max"] = "16 16 72";
				inputData["goal_min"] = "-16 -16 0";
				inputData["model"] = "models/ctf_flag.mdl";
				Entity flagBase = new Entity("item_ctfbase");
				flagBase["origin"] = inputData["origin"];
				flagBase["angles"] = inputData["angles"];
				flagBase["angle"] = inputData["angle"];
				flagBase["goal_no"] = "2";
				flagBase["model"] = "models/ctf_flag_stand_phoenix.mdl";
				flagBase["goal_max"] = "16 16 72";
				flagBase["goal_min"] = "-16 -16 0";
				data.Add(flagBase);
			}
			else
			{
				if (inputData["classname"].ToUpper().Equals("team_CTF_redspawn".ToUpper()) || inputData["classname"].ToUpper().Equals("info_player_axis".ToUpper()))
				{
					inputData["classname"] = "info_ctfspawn";
					inputData["team_no"] = "2";
					Vector3D origin = inputData.Origin;
					inputData["origin"] = origin[X] + " " + origin[Y] + " " + (origin[Z] + 24);
				}
				else
				{
					if (inputData["classname"].ToUpper().Equals("team_CTF_bluespawn".ToUpper()) || inputData["classname"].ToUpper().Equals("info_player_allied".ToUpper()))
					{
						inputData["classname"] = "info_ctfspawn";
						inputData["team_no"] = "1";
						Vector3D origin = inputData.Origin;
						inputData["origin"] = origin[X] + " " + origin[Y] + " " + (origin[Z] + 24);
					}
					else
					{
						if (inputData["classname"].ToUpper().Equals("info_player_start".ToUpper()))
						{
							Vector3D origin = inputData.Origin;
							inputData["origin"] = origin[X] + " " + origin[Y] + " " + (origin[Z] + 24);
						}
						else
						{
							if (inputData["classname"].ToUpper().Equals("info_player_coop".ToUpper()))
							{
								Vector3D origin = inputData.Origin;
								inputData["origin"] = origin[X] + " " + origin[Y] + " " + (origin[Z] + 24);
							}
							else
							{
								if (inputData["classname"].ToUpper().Equals("info_player_deathmatch".ToUpper()))
								{
									Vector3D origin = inputData.Origin;
									inputData["origin"] = origin[X] + " " + origin[Y] + " " + (origin[Z] + 24);
								}
								else
								{
									if (inputData["classname"].ToUpper().Equals("light".ToUpper()))
									{
										string color = inputData["color"];
										string intensity = inputData["light"];
										string[] nums = color.Split(' ');
										double[] lightNumbers = new double[4];
										for (int j = 0; j < 3 && j < nums.Length; j++)
										{
											try
											{
												lightNumbers[j] = System.Double.Parse(nums[j]);
												lightNumbers[j] *= 255; // Quake 3's numbers are from 0 to 1, Nightfire are from 0 to 255
											}
											catch (System.FormatException)
											{
												;
											}
										}
										try
										{
											lightNumbers[s] = System.Double.Parse(intensity);
										}
										catch (System.FormatException)
										{
											;
										}
										inputData.Attributes.Remove("_color");
										inputData.Attributes.Remove("light");
										inputData["_light"] = lightNumbers[r] + " " + lightNumbers[g] + " " + lightNumbers[b] + " " + lightNumbers[s];
									}
									else
									{
										if (inputData["classname"].ToUpper().Equals("func_rotatingdoor".ToUpper()))
										{
											inputData["classname"] = "func_door_rotating";
										}
										else
										{
											if (inputData["classname"].ToUpper().Equals("info_pathnode".ToUpper()))
											{
												inputData["classname"] = "info_node";
											}
											else
											{
												if (inputData["classname"].ToUpper().Equals("trigger_ladder".ToUpper()))
												{
													inputData["classname"] = "func_ladder";
												}
												else
												{
													if (inputData["classname"].ToUpper().Equals("worldspawn".ToUpper()))
													{
														if (!inputData["suncolor"].Equals(""))
														{
															Entity light_environment = new Entity("light_environment");
															light_environment["_light"] = inputData["suncolor"];
															light_environment["angles"] = inputData["sundirection"];
															light_environment["_fade"] = inputData["sundiffuse"];
															inputData.Attributes.Remove("suncolor");
															inputData.Attributes.Remove("sundirection");
															inputData.Attributes.Remove("sundiffuse");
															inputData.Attributes.Remove("sundiffusecolor");
															data.Add(light_environment);
														}
													}
													else
													{
														if (inputData["classname"].ToUpper().Equals("trigger_use".ToUpper()))
														{
															inputData["classname"] = "func_button";
															inputData["spawnflags"] = "1";
															inputData["wait"] = "1";
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
			}
		}
		return inputData;
	}
	
	
	// Turn a Q2 entity into a Gearcraft one (generally for use with nightfire)
	// This won't magically fix every single thing to work in Gearcraft, for example
	// the Nightfire engine had no support for area portals. But it should save map
	// porters some time, especially when it comes to the Capture The Flag mod.
	public virtual Entity ent38ToEntM510(Entity inputData)
	{
		if (!inputData["angle"].Equals(""))
		{
			inputData["angles"] = "0 " + inputData["angle"] + " 0";
			inputData.Attributes.Remove("angle");
		}
		if (inputData.BrushBased)
		{
			Vector3D origin = inputData.Origin;
			inputData.Attributes.Remove("origin");
			inputData.Attributes.Remove("model");
			if (inputData.attributeIs("classname", "func_rotating"))
			{
				// TODO: What entities require origin brushes in CoD?
				if ((origin[0] != 0 || origin[1] != 0 || origin[2] != 0) && !Settings.noOriginBrushes)
				{
					// If this brush uses the "origin" attribute
					MAPBrush newOriginBrush = MAPBrush.createBrush(new Vector3D(- Settings.originBrushSize, - Settings.originBrushSize, - Settings.originBrushSize), new Vector3D(Settings.originBrushSize, Settings.originBrushSize, Settings.originBrushSize), "special/origin");
					inputData.Brushes.Add(newOriginBrush);
				}
			}
			for (int i = 0; i < inputData.Brushes.Count; i++)
			{
				MAPBrush currentBrush = inputData.Brushes[i];
				//currentBrush.translate(new Vector3D(origin));
			}
		}
		if (inputData["classname"].ToUpper().Equals("func_wall".ToUpper()))
		{
			if (!inputData["targetname"].Equals(""))
			{
				// Really this should depend on spawnflag 2 or 4
				inputData["classname"] = "func_wall_toggle";
			} // 2 I believe is "Start enabled" and 4 is "toggleable", or the other way around. Not sure. Could use an OR.
		}
		else
		{
			if (inputData["classname"].ToUpper().Equals("item_flag_team2".ToUpper()) || inputData["classname"].ToUpper().Equals("ctf_flag_hardcorps".ToUpper()))
			{
				// Blue flag
				inputData["classname"] = "item_ctfflag";
				inputData["skin"] = "1"; // 0 for PHX, 1 for MI6
				inputData["goal_no"] = "1"; // 2 for PHX, 1 for MI6
				inputData["goal_max"] = "16 16 72";
				inputData["goal_min"] = "-16 -16 0";
				Entity flagBase = new Entity("item_ctfbase");
				flagBase["origin"] = inputData["origin"];
				flagBase["angles"] = inputData["angles"];
				flagBase["angle"] = inputData["angle"];
				flagBase["goal_no"] = "1";
				flagBase["model"] = "models/ctf_flag_stand_mi6.mdl";
				flagBase["goal_max"] = "16 16 72";
				flagBase["goal_min"] = "-16 -16 0";
				data.Add(flagBase);
			}
			else
			{
				if (inputData["classname"].ToUpper().Equals("item_flag_team1".ToUpper()) || inputData["classname"].ToUpper().Equals("ctf_flag_sintek".ToUpper()))
				{
					// Red flag
					inputData["classname"] = "item_ctfflag";
					inputData["skin"] = "0"; // 0 for PHX, 1 for MI6
					inputData["goal_no"] = "2"; // 2 for PHX, 1 for MI6
					inputData["goal_max"] = "16 16 72";
					inputData["goal_min"] = "-16 -16 0";
					Entity flagBase = new Entity("item_ctfbase");
					flagBase["origin"] = inputData["origin"];
					flagBase["angles"] = inputData["angles"];
					flagBase["angle"] = inputData["angle"];
					flagBase["goal_no"] = "2";
					flagBase["model"] = "models/ctf_flag_stand_phoenix.mdl";
					flagBase["goal_max"] = "16 16 72";
					flagBase["goal_min"] = "-16 -16 0";
					data.Add(flagBase);
				}
				else
				{
					if (inputData["classname"].ToUpper().Equals("info_player_team1".ToUpper()) || inputData["classname"].ToUpper().Equals("info_player_sintek".ToUpper()))
					{
						inputData["classname"] = "info_ctfspawn";
						inputData["team_no"] = "2";
					}
					else
					{
						if (inputData["classname"].ToUpper().Equals("info_player_team2".ToUpper()) || inputData["classname"].ToUpper().Equals("info_player_hardcorps".ToUpper()))
						{
							inputData["classname"] = "info_ctfspawn";
							inputData["team_no"] = "1";
						}
						else
						{
							if (inputData["classname"].ToUpper().Equals("info_player_start".ToUpper()))
							{
								Vector3D origin = inputData.Origin;
								inputData["origin"] = origin[X] + " " + origin[Y] + " " + (origin[Z] + 18);
							}
							else
							{
								if (inputData["classname"].ToUpper().Equals("info_player_coop".ToUpper()))
								{
									Vector3D origin = inputData.Origin;
									inputData["origin"] = origin[X] + " " + origin[Y] + " " + (origin[Z] + 18);
								}
								else
								{
									if (inputData["classname"].ToUpper().Equals("info_player_deathmatch".ToUpper()))
									{
										Vector3D origin = inputData.Origin;
										inputData["origin"] = origin[X] + " " + origin[Y] + " " + (origin[Z] + 18);
									}
									else
									{
										if (inputData["classname"].ToUpper().Equals("light".ToUpper()))
										{
											string color = inputData["color"];
											string intensity = inputData["light"];
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
												lightNumbers[s] = System.Double.Parse(intensity);
											}
											catch (System.FormatException)
											{
												;
											}
											inputData.Attributes.Remove("_color");
											inputData.Attributes.Remove("light");
											inputData["_light"] = lightNumbers[r] + " " + lightNumbers[g] + " " + lightNumbers[b] + " " + lightNumbers[s];
										}
										else
										{
											if (inputData["classname"].ToUpper().Equals("misc_teleporter".ToUpper()))
											{
												Vector3D origin = inputData.Origin;
												Vector3D mins = new Vector3D(origin[X] - 24, origin[Y] - 24, origin[Z] - 24);
												Vector3D maxs = new Vector3D(origin[X] + 24, origin[Y] + 24, origin[Z] + 48);
												inputData.Brushes.Add(MAPBrush.createBrush(mins, maxs, "special/trigger"));
												inputData.Attributes.Remove("origin");
												inputData["classname"] = "trigger_teleport";
											}
											else
											{
												if (inputData["classname"].ToUpper().Equals("misc_teleporter_dest".ToUpper()))
												{
													inputData["classname"] = "info_teleport_destination";
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
		return inputData;
	}
	
	private Entity entSourceToEntM510(Entity inputData)
	{
		if (inputData.BrushBased)
		{
			Vector3D origin = inputData.Origin;
			inputData.Attributes.Remove("origin");
			inputData.Attributes.Remove("model");
			if (inputData.attributeIs("classname", "func_door_rotating"))
			{
				// TODO: What entities require origin brushes?
				if ((origin[0] != 0 || origin[1] != 0 || origin[2] != 0) && !Settings.noOriginBrushes)
				{
					// If this brush uses the "origin" attribute
					MAPBrush newOriginBrush = MAPBrush.createBrush(new Vector3D(- Settings.originBrushSize, - Settings.originBrushSize, - Settings.originBrushSize), new Vector3D(Settings.originBrushSize, Settings.originBrushSize, Settings.originBrushSize), "special/origin");
					inputData.Brushes.Add(newOriginBrush);
				}
			}
			for (int i = 0; i < inputData.Brushes.Count; i++)
			{
				MAPBrush currentBrush = inputData.Brushes[i];
				currentBrush.translate(new Vector3D(origin));
			}
		}
		if (inputData["classname"].ToUpper().Equals("func_breakable_surf".ToUpper()))
		{
			inputData["classname"] = "func_breakable";
		}
		else
		{
			if (inputData["classname"].ToUpper().Equals("func_brush".ToUpper()))
			{
				if (inputData["solidity"].Equals("0"))
				{
					inputData["classname"] = "func_wall_toggle";
					if (inputData["StartDisabled"].Equals("1"))
					{
						inputData["spawnflags"] = "1";
					}
					else
					{
						inputData["spawnflags"] = "0";
					}
					inputData.Attributes.Remove("StartDisabled");
				}
				else
				{
					if (inputData["solidity"].Equals("1"))
					{
						inputData["classname"] = "func_illusionary";
					}
					else
					{
						inputData["classname"] = "func_wall";
					}
				}
				inputData.Attributes.Remove("solidity");
			}
			else
			{
				if (inputData["classname"].ToUpper().Equals("env_fog_controller".ToUpper()))
				{
					inputData["classname"] = "env_fog";
					inputData["rendercolor"] = inputData["fogcolor"];
					inputData.Attributes.Remove("fogcolor");
				}
				else
				{
					if (inputData["classname"].ToUpper().Equals("prop_static".ToUpper()))
					{
						inputData["classname"] = "item_generic";
					}
					else
					{
						if (inputData["classname"].ToUpper().Equals("info_player_rebel".ToUpper()) || inputData["classname"].ToUpper().Equals("info_player_janus".ToUpper()) || inputData["classname"].ToUpper().Equals("ctf_rebel_player_spawn".ToUpper()))
						{
							inputData["classname"] = "info_ctfspawn";
							inputData["team_no"] = "2";
							Vector3D origin = inputData.Origin;
							inputData["origin"] = origin[X] + " " + origin[Y] + " " + (origin[Z] + 40);
						}
						else
						{
							if (inputData["classname"].ToUpper().Equals("info_player_combine".ToUpper()) || inputData["classname"].ToUpper().Equals("info_player_mi6".ToUpper()) || inputData["classname"].ToUpper().Equals("ctf_combine_player_spawn".ToUpper()))
							{
								inputData["classname"] = "info_ctfspawn";
								inputData["team_no"] = "1";
								Vector3D origin = inputData.Origin;
								inputData["origin"] = origin[X] + " " + origin[Y] + " " + (origin[Z] + 40);
							}
							else
							{
								if (inputData["classname"].ToUpper().Equals("info_player_deathmatch".ToUpper()))
								{
									Vector3D origin = inputData.Origin;
									inputData["origin"] = origin[X] + " " + origin[Y] + " " + (origin[Z] + 40);
								}
								else
								{
									if (inputData["classname"].ToUpper().Equals("ctf_combine_flag".ToUpper()))
									{
										inputData.Attributes.Remove("targetname");
										inputData.Attributes.Remove("SpawnWithCaptureEnabled");
										inputData["skin"] = "1";
										inputData["goal_max"] = "16 16 72";
										inputData["goal_min"] = "-16 -16 0";
										inputData["goal_no"] = "1";
										inputData["model"] = "models/ctf_flag.mdl";
										inputData["classname"] = "item_ctfflag";
										Entity newFlagBase = new Entity("item_ctfbase");
										newFlagBase["origin"] = inputData["origin"];
										newFlagBase["angles"] = inputData["angles"];
										newFlagBase["goal_max"] = "16 16 72";
										newFlagBase["goal_min"] = "-16 -16 0";
										newFlagBase["goal_no"] = "1";
										newFlagBase["model"] = "models/ctf_flag_stand_mi6.mdl";
										data.Add(newFlagBase);
									}
									else
									{
										if (inputData["classname"].ToUpper().Equals("ctf_rebel_flag".ToUpper()))
										{
											inputData.Attributes.Remove("targetname");
											inputData.Attributes.Remove("SpawnWithCaptureEnabled");
											inputData["skin"] = "0";
											inputData["goal_max"] = "16 16 72";
											inputData["goal_min"] = "-16 -16 0";
											inputData["goal_no"] = "2";
											inputData["model"] = "models/ctf_flag.mdl";
											inputData["classname"] = "item_ctfflag";
											Entity newFlagBase = new Entity("item_ctfbase");
											newFlagBase["origin"] = inputData["origin"];
											newFlagBase["angles"] = inputData["angles"];
											newFlagBase["goal_max"] = "16 16 72";
											newFlagBase["goal_min"] = "-16 -16 0";
											newFlagBase["goal_no"] = "2";
											newFlagBase["model"] = "models/ctf_flag_stand_phoenix.mdl";
											data.Add(newFlagBase);
										}
									}
								}
							}
						}
					}
				}
			}
		}
		return inputData;
	}
	
	private Entity entDoomToEntM510(Entity inputData)
	{
		if (inputData.attributeIs("classname", "weapon_pistol"))
		{
			inputData["classname"] = "weapon_p99";
		}
		else
		{
			if (inputData.attributeIs("classname", "ammo_cells_large"))
			{
				inputData["classname"] = "ammo_bondmine";
			}
			else
			{
				if (inputData.attributeIs("classname", "weapon_shotgun_double"))
				{
					inputData["classname"] = "weapon_pdw90";
				}
				else
				{
					if (inputData.attributeIs("classname", "weapon_shotgun"))
					{
						inputData["classname"] = "weapon_frinesi";
					}
					else
					{
						if (inputData.attributeIs("classname", "weapon_chaingun"))
						{
							inputData["classname"] = "weapon_minigun";
						}
						else
						{
							if (inputData.attributeIs("classname", "weapon_plasmagun"))
							{
								inputData["classname"] = "weapon_grenadelauncher";
							}
							else
							{
								if (inputData.attributeIs("classname", "weapon_chainsaw"))
								{
									inputData["classname"] = "weapon_ronin";
								}
								else
								{
									if (inputData.attributeIs("classname", "weapon_bfg"))
									{
										inputData["classname"] = "weapon_laserrifle";
									}
									else
									{
										if (inputData.attributeIs("classname", "ammo_clip_small"))
										{
											inputData["classname"] = "ammo_p99";
										}
										else
										{
											if (inputData.attributeIs("classname", "ammo_shells_small"))
											{
												inputData["classname"] = "ammo_mini";
											}
											else
											{
												if (inputData.attributeIs("classname", "ammo_rockets_small"))
												{
													inputData["classname"] = "ammo_darts";
												}
												else
												{
													if (inputData.attributeIs("classname", "ammo_rockets_large"))
													{
														inputData["classname"] = "ammo_rocketlauncher";
													}
													else
													{
														if (inputData.attributeIs("classname", "ammo_cells_small"))
														{
															inputData["classname"] = "ammo_grenadelauncher";
														}
														else
														{
															if (inputData.attributeIs("classname", "ammo_bullets_large"))
															{
																inputData["classname"] = "ammo_mp9";
															}
															else
															{
																if (inputData.attributeIs("classname", "ammo_shells_large"))
																{
																	inputData["classname"] = "ammo_shotgun";
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
					}
				}
			}
		}
		return inputData;
	}
	
	// ACCESSORS/MUTATORS
}