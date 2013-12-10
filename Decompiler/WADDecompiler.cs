using System;
// WADDecompiler class

// Handles the actual decompilation.

public class WADDecompiler
{
	
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
	
	private int jobnum;
	
	private Entities mapFile; // Most MAP file formats (including GearCraft) are simply a bunch of nested entities
	private int numBrshs;
	private DecompilerThread parent;
	
	private DoomMap doomMap;
	
	// CONSTRUCTORS
	
	// This constructor sets up everything to convert a Doom map into brushes compatible with modern map editors.
	// I don't know if this is decompiling, per se. I don't know if Doom maps were ever compiled or if they just had nodes built.
	public WADDecompiler(DoomMap doomMap, int jobnum, DecompilerThread parent)
	{
		this.doomMap = doomMap;
		this.jobnum = jobnum;
		this.parent = parent;
	}
	
	// METHODS
	
	// +decompile()
	// Attempts to convert a map in a Doom WAD into a usable .MAP file. This has many
	// challenges, not the least of which is the fact that the Doom engine didn't use
	// brushes (at least, not in any sane way).
	public virtual Entities decompile()
	{
		DecompilerThread.OnMessage(this, "Decompiling...");
		DecompilerThread.OnMessage(this, doomMap.MapName);
		
		mapFile = new Entities();
		Entity world = new Entity("worldspawn");
		mapFile.Add(world);
		
		string[] lowerWallTextures = new string[doomMap.Sidedefs.Count];
		string[] midWallTextures = new string[doomMap.Sidedefs.Count];
		string[] higherWallTextures = new string[doomMap.Sidedefs.Count];
		
		short[] sectorTag = new short[doomMap.Sectors.Count];
		string playerStartOrigin = "";
		
		// Since Doom relied on sectors to define a cieling and floor height, and nothing else,
		// need to find the minimum and maximum used Z values. This is because the Doom engine
		// is only a pseudo-3D engine. For all it cares, the cieling and floor extend to their
		// respective infinities. For a GC/Hammer map, however, this cannot be the case.
		int ZMin = 32767; // Even though the values in the map will never exceed these, use ints here to avoid
		int ZMax = - 32768; // overflows, in case the map DOES go within 32 units of these values.
		for (int i = 0; i < doomMap.Sectors.Count; i++)
		{
			DSector currentSector = doomMap.Sectors[i];
			sectorTag[i] = currentSector.Tag;
			if (currentSector.FloorHeight < ZMin + 32)
			{
				ZMin = currentSector.FloorHeight - 32; // Can't use the actual value, because that IS the floor
			}
			else
			{
				if (currentSector.CielingHeight > ZMax - 32)
				{
					ZMax = currentSector.CielingHeight + 32; // or the cieling. Subtract or add a sane value to it.
				}
			}
		}
		
		// I need to analyze the binary tree and get more information, particularly the
		// parent nodes of each subsector and node, as well as whether it's the right or
		// left child of that node. These are extremely important, as the parent defines
		// boundaries for the children, as well as inheriting further boundaries from its
		// parents. These boundaries are invaluable for forming brushes.
		int[] nodeparents = new int[doomMap.Nodes.Count];
		bool[] nodeIsLeft = new bool[doomMap.Nodes.Count];
		
		for (int i = 0; i < doomMap.Nodes.Count; i++)
		{
			nodeparents[i] = - 1; // There should only be one node left with -1 as a parent. This SHOULD be the root.
			for (int j = 0; j < doomMap.Nodes.Count; j++)
			{
				if (doomMap.Nodes[j].Child1 == i)
				{
					nodeparents[i] = j;
					break;
				}
				else
				{
					if (doomMap.Nodes[j].Child2 == i)
					{
						nodeparents[i] = j;
						nodeIsLeft[i] = true;
						break;
					}
				}
			}
		}
		
		// Keep a list of what subsectors belong to which sector
		int[] subsectorSectors = new int[doomMap.SubSectors.Count];
		// Keep a list of what sidedefs belong to what subsector as well
		int[][] subsectorSidedefs = new int[doomMap.SubSectors.Count][];
		
		short[][] sideDefShifts = new short[2][];
		for (int i2 = 0; i2 < 2; i2++)
		{
			sideDefShifts[i2] = new short[doomMap.Sidedefs.Count];
		}
		
		// Figure out what sector each subsector belongs to, and what node is its parent.
		// Depending on sector "tags" this will help greatly in creation of brushbased entities,
		// and also helps in finding subsector floor and cieling heights.
		int[] ssparents = new int[doomMap.SubSectors.Count];
		bool[] ssIsLeft = new bool[doomMap.SubSectors.Count];
		for (int i = 0; i < doomMap.SubSectors.Count; i++)
		{
			//DecompilerThread.OnMessage(this, "Populating texture lists for subsector " + i);
			// First, find the subsector's parent and whether it is the left or right child.
			ssparents[i] = - 1; // No subsector should have a -1 in here
			for (int j = 0; j < doomMap.Nodes.Count; j++)
			{
				// When a node references a subsector, it is not referenced by negative
				// index, as future BSP versions do. The bits 0-14 ARE the index, and
				// bit 15 (which is the sign bit in two's compliment math) determines
				// whether or not it is a node or subsector. Therefore, we need to add
				// 2^15 to the number to produce the actual index.
				if (doomMap.Nodes[j].Child1 + 32768 == i)
				{
					ssparents[i] = j;
					break;
				}
				else
				{
					if (doomMap.Nodes[j].Child2 + 32768 == i)
					{
						ssparents[i] = j;
						ssIsLeft[i] = true;
						break;
					}
				}
			}
			
			// Second, figure out what sector a subsector belongs to, and the type of sector it is.
			subsectorSectors[i] = - 1;
			Edge currentsubsector = doomMap.SubSectors[i];
			subsectorSidedefs[i] = new int[currentsubsector.NumSegs];
			for (int j = 0; j < currentsubsector.NumSegs; j++)
			{
				// For each segment the subsector references
				DSegment currentsegment = doomMap.Segments[currentsubsector.FirstSeg + j];
				DLinedef currentlinedef = doomMap.Linedefs[currentsegment.Linedef];
				int currentsidedefIndex;
				int othersideIndex;
				if (currentsegment.Direction == 0)
				{
					currentsidedefIndex = currentlinedef.Right;
					othersideIndex = currentlinedef.Left;
				}
				else
				{
					currentsidedefIndex = currentlinedef.Left;
					othersideIndex = currentlinedef.Right;
				}
				subsectorSidedefs[i][j] = currentsidedefIndex;
				DSidedef currentSidedef = doomMap.Sidedefs[currentsidedefIndex];
				if (currentlinedef.OneSided)
				{
					// A one-sided linedef should always be like this
					midWallTextures[currentsidedefIndex] = doomMap.WadName + "/" + currentSidedef.MidTexture;
					higherWallTextures[currentsidedefIndex] = "special/nodraw";
					lowerWallTextures[currentsidedefIndex] = "special/nodraw";
					sideDefShifts[X][currentsidedefIndex] = currentSidedef.OffsetX;
					sideDefShifts[Y][currentsidedefIndex] = currentSidedef.OffsetY;
				}
				else
				{
					// I don't really get why I need to apply these textures to the other side. But if it works I won't argue...
					if (!currentSidedef.MidTexture.Equals("-"))
					{
						midWallTextures[othersideIndex] = doomMap.WadName + "/" + currentSidedef.MidTexture;
					}
					else
					{
						midWallTextures[othersideIndex] = "special/nodraw";
					}
					if (!currentSidedef.HighTexture.Equals("-"))
					{
						higherWallTextures[othersideIndex] = doomMap.WadName + "/" + currentSidedef.HighTexture;
					}
					else
					{
						higherWallTextures[othersideIndex] = "special/nodraw";
					}
					if (!currentSidedef.LowTexture.Equals("-"))
					{
						lowerWallTextures[othersideIndex] = doomMap.WadName + "/" + currentSidedef.LowTexture;
					}
					else
					{
						lowerWallTextures[othersideIndex] = "special/nodraw";
					}
					sideDefShifts[X][othersideIndex] = currentSidedef.OffsetX;
					sideDefShifts[Y][othersideIndex] = currentSidedef.OffsetY;
				}
				// Sometimes a subsector seems to belong to more than one sector. Only the reference in the first seg is true.
				if (j == 0)
				{
					subsectorSectors[i] = currentSidedef.Sector;
				}
			}
		}
		bool[] linedefFlagsDealtWith = new bool[doomMap.Linedefs.Count];
		bool[] linedefSpecialsDealtWith = new bool[doomMap.Linedefs.Count];
		
		MAPBrush[][] sectorFloorBrushes = new MAPBrush[doomMap.Sectors.Count][];
		for (int i3 = 0; i3 < doomMap.Sectors.Count; i3++)
		{
			sectorFloorBrushes[i3] = new MAPBrush[0];
		}
		MAPBrush[][] sectorCielingBrushes = new MAPBrush[doomMap.Sectors.Count][];
		for (int i4 = 0; i4 < doomMap.Sectors.Count; i4++)
		{
			sectorCielingBrushes[i4] = new MAPBrush[0];
		}
		
		// For one-sided linedefs referenced by more than one subsector
		bool[] outsideBrushAlreadyCreated = new bool[doomMap.Linedefs.Count];
		
		int onePercent = (int)((doomMap.SubSectors.Count)/100);
		for (int i = 0; i < doomMap.SubSectors.Count; i++)
		{
			//DecompilerThread.OnMessage(this, "Creating brushes for subsector " + i);
			
			Edge currentsubsector = doomMap.SubSectors[i];
			
			// Third, create a few brushes out of the geometry.
			MAPBrush cielingBrush = new MAPBrush(numBrshs++, 0, false);
			MAPBrush floorBrush = new MAPBrush(numBrshs++, 0, false);
			MAPBrush midBrush = new MAPBrush(numBrshs++, 0, false);
			MAPBrush sectorTriggerBrush = new MAPBrush(numBrshs++, 0, false);
			DSector currentSector = doomMap.Sectors[subsectorSectors[i]];
			
			Vector3D[] roofPlane = new Vector3D[3];
			double[] roofTexS = new double[3];
			double[] roofTexT = new double[3];
			roofPlane[0] = new Vector3D(0, Settings.planePointCoef, ZMax);
			roofPlane[1] = new Vector3D(Settings.planePointCoef, Settings.planePointCoef, ZMax);
			roofPlane[2] = new Vector3D(Settings.planePointCoef, 0, ZMax);
			roofTexS[0] = 1;
			roofTexT[1] = - 1;
			
			Vector3D[] cileingPlane = new Vector3D[3];
			double[] cileingTexS = new double[3];
			double[] cileingTexT = new double[3];
			cileingPlane[0] = new Vector3D(0, 0, currentSector.CielingHeight);
			cileingPlane[1] = new Vector3D(Settings.planePointCoef, 0, currentSector.CielingHeight);
			cileingPlane[2] = new Vector3D(Settings.planePointCoef, Settings.planePointCoef, currentSector.CielingHeight);
			cileingTexS[0] = 1;
			cileingTexT[1] = - 1;
			
			Vector3D[] floorPlane = new Vector3D[3];
			double[] floorTexS = new double[3];
			double[] floorTexT = new double[3];
			floorPlane[0] = new Vector3D(0, Settings.planePointCoef, currentSector.FloorHeight);
			floorPlane[1] = new Vector3D(Settings.planePointCoef, Settings.planePointCoef, currentSector.FloorHeight);
			floorPlane[2] = new Vector3D(Settings.planePointCoef, 0, currentSector.FloorHeight);
			floorTexS[0] = 1;
			floorTexT[1] = - 1;
			
			Vector3D[] foundationPlane = new Vector3D[3];
			double[] foundationTexS = new double[3];
			double[] foundationTexT = new double[3];
			foundationPlane[0] = new Vector3D(0, 0, ZMin);
			foundationPlane[1] = new Vector3D(Settings.planePointCoef, 0, ZMin);
			foundationPlane[2] = new Vector3D(Settings.planePointCoef, Settings.planePointCoef, ZMin);
			foundationTexS[0] = 1;
			foundationTexT[1] = - 1;
			
			Vector3D[] topPlane = new Vector3D[3];
			double[] topTexS = new double[3];
			double[] topTexT = new double[3];
			topPlane[0] = new Vector3D(0, Settings.planePointCoef, currentSector.CielingHeight);
			topPlane[1] = new Vector3D(Settings.planePointCoef, Settings.planePointCoef, currentSector.CielingHeight);
			topPlane[2] = new Vector3D(Settings.planePointCoef, 0, currentSector.CielingHeight);
			topTexS[0] = 1;
			topTexT[1] = - 1;
			
			Vector3D[] bottomPlane = new Vector3D[3];
			double[] bottomTexS = new double[3];
			double[] bottomTexT = new double[3];
			bottomPlane[0] = new Vector3D(0, 0, currentSector.FloorHeight);
			bottomPlane[1] = new Vector3D(Settings.planePointCoef, 0, currentSector.FloorHeight);
			bottomPlane[2] = new Vector3D(Settings.planePointCoef, Settings.planePointCoef, currentSector.FloorHeight);
			bottomTexS[0] = 1;
			bottomTexT[1] = - 1;
			
			int nextNode = ssparents[i];
			bool leftSide = ssIsLeft[i];
			
			for (int j = 0; j < currentsubsector.NumSegs; j++)
			{
				// Iterate through the sidedefs defined by segments of this subsector
				DSegment currentseg = doomMap.Segments[currentsubsector.FirstSeg + j];
				Vector3D start = doomMap.Vertices[currentseg.StartVertex].Vector;
				Vector3D end = doomMap.Vertices[currentseg.EndVertex].Vector;
				DLinedef currentLinedef = doomMap.Linedefs[(int) currentseg.Linedef];
				
				Vector3D[] plane = new Vector3D[3];
				double[] texS = new double[3];
				double[] texT = new double[3];
				plane[0] = new Vector3D(start.X, start.Y, ZMin);
				plane[1] = new Vector3D(end.X, end.Y, ZMin);
				plane[2] = new Vector3D(end.X, end.Y, ZMax);
				
				Vector3D linestart = new Vector3D(doomMap.Vertices[currentLinedef.Start].Vector.X, doomMap.Vertices[currentLinedef.Start].Vector.Y, ZMin);
				Vector3D lineend = new Vector3D(doomMap.Vertices[currentLinedef.End].Vector.X, doomMap.Vertices[currentLinedef.End].Vector.Y, ZMax);
				
				double sideLength = System.Math.Sqrt(System.Math.Pow(start.X - end.X, 2) + System.Math.Pow(start.Y - end.Y, 2));
				
				bool upperUnpegged = !((currentLinedef.Flags[0] & ((sbyte) 1 << 3)) == 0);
				bool lowerUnpegged = !((currentLinedef.Flags[0] & ((sbyte) 1 << 4)) == 0);
				
				texS[0] = (start.X - end.X) / sideLength;
				texS[1] = (start.Y - end.Y) / sideLength;
				texS[2] = 0;
				texT[0] = 0;
				texT[1] = 0;
				texT[2] = - 1;
				
				double SShift = sideDefShifts[X][subsectorSidedefs[i][j]] - (texS[0] * end.X) - (texS[1] * end.Y);
				double lowTShift = 0;
				double highTShift = 0;
				if (!currentLinedef.OneSided)
				{
					DSector otherSideSector;
					if (doomMap.Sectors[doomMap.Sidedefs[currentLinedef.Left].Sector] == currentSector)
					{
						otherSideSector = doomMap.Sectors[doomMap.Sidedefs[currentLinedef.Right].Sector];
					}
					else
					{
						otherSideSector = doomMap.Sectors[doomMap.Sidedefs[currentLinedef.Left].Sector];
					}
					if (lowerUnpegged)
					{
						lowTShift = otherSideSector.CielingHeight;
					}
					else
					{
						lowTShift = currentSector.FloorHeight;
					}
					if (upperUnpegged)
					{
						highTShift = otherSideSector.CielingHeight;
					}
					else
					{
						highTShift = currentSector.CielingHeight;
					}
					lowTShift += sideDefShifts[Y][subsectorSidedefs[i][j]];
					highTShift += sideDefShifts[Y][subsectorSidedefs[i][j]];
				}
				MAPBrushSide low = new MAPBrushSide(plane, lowerWallTextures[subsectorSidedefs[i][j]], texS, SShift, texT, lowTShift, 0, 1, 1, 0, "wld_lightmap", 16, 0);
				MAPBrushSide high = new MAPBrushSide(plane, higherWallTextures[subsectorSidedefs[i][j]], texS, SShift, texT, highTShift, 0, 1, 1, 0, "wld_lightmap", 16, 0);
				MAPBrushSide mid;
				MAPBrushSide damage = new MAPBrushSide(plane, "special/trigger", texS, 0, texT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
				
				if (currentLinedef.OneSided)
				{
					if (!outsideBrushAlreadyCreated[currentseg.Linedef])
					{
						outsideBrushAlreadyCreated[currentseg.Linedef] = true;
						double highestCieling = currentSector.CielingHeight;
						double lowestFloor = currentSector.FloorHeight;
						if (currentSector.Tag != 0)
						{
							double temp = getHighestNeighborCielingHeight(subsectorSectors[i]);
							if (temp > highestCieling)
							{
								highestCieling = temp;
							}
							temp = getLowestNeighborFloorHeight(subsectorSectors[i]);
							if (temp < lowestFloor)
							{
								lowestFloor = temp;
							}
						}
						MAPBrush outsideBrush = null;
						if (lowestFloor <= highestCieling)
						{
							outsideBrush = MAPBrush.createFaceBrush(midWallTextures[subsectorSidedefs[i][j]], "special/nodraw", new Vector3D(linestart.X, linestart.Y, ZMin), new Vector3D(lineend.X, lineend.Y, ZMax), sideDefShifts[X][subsectorSidedefs[i][j]], sideDefShifts[Y][subsectorSidedefs[i][j]], lowerUnpegged, currentSector.CielingHeight, currentSector.FloorHeight);
						}
						else
						{
							outsideBrush = MAPBrush.createFaceBrush(midWallTextures[subsectorSidedefs[i][j]], "special/nodraw", new Vector3D(linestart.X, linestart.Y, lowestFloor), new Vector3D(lineend.X, lineend.Y, highestCieling), sideDefShifts[X][subsectorSidedefs[i][j]], sideDefShifts[Y][subsectorSidedefs[i][j]], lowerUnpegged, currentSector.CielingHeight, currentSector.FloorHeight);
						}
						world.Brushes.Add(outsideBrush);
					}
					mid = new MAPBrushSide(plane, "special/nodraw", texS, 0, texT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
				}
				else
				{
					double midTShift = sideDefShifts[Y][subsectorSidedefs[i][j]];
					if (lowerUnpegged)
					{
						midTShift += currentSector.FloorHeight;
					}
					else
					{
						midTShift += currentSector.CielingHeight;
					}
					mid = new MAPBrushSide(plane, midWallTextures[subsectorSidedefs[i][j]], texS, SShift, texT, midTShift, 0, 1, 1, 0, "wld_masked", 16, 0);
					if (!linedefFlagsDealtWith[currentseg.Linedef])
					{
						linedefFlagsDealtWith[currentseg.Linedef] = true;
						if (!((currentLinedef.Flags[0] & ((sbyte) 1 << 0)) == 0))
						{
							// Flag 0x0001 indicates "solid" but doesn't block bullets. It is assumed for all one-sided.
							MAPBrush solidBrush = MAPBrush.createFaceBrush("special/clip", "special/clip", linestart, lineend, 0, 0, false, 0, 0);
							world.Brushes.Add(solidBrush);
						}
						else
						{
							if (!((currentLinedef.Flags[0] & ((sbyte) 1 << 1)) == 0))
							{
								// Flag 0x0002 indicates "monster clip".
								MAPBrush solidBrush = MAPBrush.createFaceBrush("special/enemyclip", "special/enemyclip", linestart, lineend, 0, 0, false, 0, 0);
								world.Brushes.Add(solidBrush);
							}
						}
					}
					DSidedef otherside = doomMap.Sidedefs[currentLinedef.Left];
					if (currentLinedef.Action != 0 && !linedefSpecialsDealtWith[currentseg.Linedef])
					{
						linedefSpecialsDealtWith[currentseg.Linedef] = true;
						Entity trigger = null;
						MAPBrush triggerBrush = MAPBrush.createFaceBrush("special/trigger", "special/trigger", linestart, lineend, 0, 0, false, 0, 0);
						if (doomMap.Version == mapType.TYPE_HEXEN)
						{
							bool[] bitset = new bool[16];
							for (int k = 0; k < 8; k++)
							{
								bitset[k] = !((currentLinedef.Flags[0] & ((sbyte) k << 1)) == 0);
							}
							for (int k = 0; k < 8; k++)
							{
								bitset[k + 8] = !((currentLinedef.Flags[1] & ((sbyte) k << 1)) == 0);
							}
							if (bitset[10] && bitset[11] && !bitset[12])
							{
								// Triggered when "Used" by player
								trigger = new Entity("func_button");
								trigger["spawnflags"] = "1";
								if (bitset[9])
								{
									trigger["wait"] = "1";
								}
								else
								{
									trigger["wait"] = "-1";
								}
							}
							else
							{
								if (bitset[9])
								{
									// Can be activated more than once
									trigger = new Entity("trigger_multiple");
									trigger["wait"] = "1";
								}
								else
								{
									trigger = new Entity("trigger_once");
								}
							}
							switch (currentLinedef.Action)
							{
								case 21: 
								// Floor lower to lowest surrounding floor
								case 22:  // Floor lower to next lowest surrounding floor
									if (currentLinedef.Arguments[0] != 0)
									{
										trigger["target"] = "sector" + currentLinedef.Arguments[0] + "lowerfloor";
									}
									else
									{
										trigger["target"] = "sectornum" + otherside.Sector + "lowerfloor";
									}
									break;
								case 24: 
								// Floor raise to highest surrounding floor
								case 25:  // Floor raise to next highest surrounding floor
									if (currentLinedef.Arguments[0] != 0)
									{
										trigger["target"] = "sector" + currentLinedef.Arguments[0] + "raisefloor";
									}
									else
									{
										trigger["target"] = "sectornum" + otherside.Sector + "raisefloor";
									}
									break;
								case 70:  // Teleport
									trigger = new Entity("trigger_teleport");
									if (currentLinedef.Arguments[0] != 0)
									{
										trigger["target"] = "teledest" + currentLinedef.Arguments[0];
									}
									else
									{
										trigger["target"] = "sector" + currentLinedef.Tag + "teledest";
									}
									break;
								case 80:  // Exec script
									// This is a toughie. I can't write a script-to-entity converter.
									trigger["target"] = "script" + currentLinedef.Arguments[0];
									trigger["arg0"] = "" + currentLinedef.Arguments[2];
									trigger["arg1"] = "" + currentLinedef.Arguments[3];
									trigger["arg2"] = "" + currentLinedef.Arguments[4];
									break;
								case 181:  // PLANE_ALIGN
									trigger = null;
									if (!leftSide)
									{
										DSidedef getsector = doomMap.Sidedefs[currentLinedef.Left];
										DSector copyheight = doomMap.Sectors[getsector.Sector];
										short newHeight = copyheight.FloorHeight;
										//floorPlane[0]=new Vector3D(0, Settings.getPlanePointCoef(), 2000);
										//floorPlane[1]=new Vector3D(Settings.getPlanePointCoef(), Settings.getPlanePointCoef(), currentSector.getFloorHeight());
										//floorPlane[2]=new Vector3D(Settings.getPlanePointCoef(), 0, currentSector.getFloorHeight());
									}
									else
									{
										linedefSpecialsDealtWith[currentseg.Linedef] = false;
									}
									break;
								default: 
									trigger = null;
									break;
							}
						}
						else
						{
							switch (currentLinedef.Action)
							{
								
								case 1: 
								// Use Door. open, wait, close
								case 31:  // Use Door. Open, stay.
									trigger = new Entity("func_button");
									trigger["wait"] = "1";
									if (currentLinedef.Action == 31)
									{
										trigger["wait"] = "-1";
									}
									trigger["spawnflags"] = "1";
									if (doomMap.Sectors[otherside.Sector].Tag == 0)
									{
										trigger["target"] = "sectornum" + otherside.Sector + "door";
										if (currentLinedef.Action == 1)
										{
											sectorTag[otherside.Sector] = - 1;
										}
										if (currentLinedef.Action == 31)
										{
											sectorTag[otherside.Sector] = - 2;
										}
									}
									else
									{
										trigger["target"] = "sector" + doomMap.Sectors[otherside.Sector].Tag + "door";
									}
									break;
								
								case 36: 
								// Floor lower to 8 above next lowest neighboring sector
								case 38:  // Floor lower to next lowest neighboring sector
									trigger = new Entity("trigger_once");
									trigger["target"] = "sector" + currentLinedef.Tag + "lowerfloor";
									break;
								
								case 62:  // Floor lower to next lowest neighboring sector, wait 4s, goes back up
									trigger = new Entity("func_button");
									trigger["target"] = "sector" + currentLinedef.Tag + "vator";
									trigger["wait"] = "1";
									trigger["spawnflags"] = "1";
									break;
								
								case 63: 
								// Door with button, retriggerable
								case 103:  // Push button, one-time door open stay open
									trigger = new Entity("func_button");
									trigger["target"] = "sector" + currentLinedef.Tag + "door";
									trigger["wait"] = "1";
									if (currentLinedef.Action == 103)
									{
										trigger["wait"] = "-1";
									}
									trigger["spawnflags"] = "1";
									break;
								
								case 88:  // Walkover retriggerable elevator trigger
									trigger = new Entity("trigger_multiple");
									trigger["target"] = "sector" + currentLinedef.Tag + "vator";
									break;
								
								case 97:  // Walkover retriggerable Teleport
									trigger = new Entity("trigger_teleport");
									trigger["target"] = "sector" + currentLinedef.Tag + "teledest";
									break;
								
								case 109:  // Walkover one-time door open stay open
									trigger = new Entity("trigger_once");
									trigger["target"] = "sector" + currentLinedef.Tag + "door";
									break;
							}
						}
						if (trigger != null)
						{
							trigger.Brushes.Add(triggerBrush);
							mapFile.Add(trigger);
						}
					}
				}
				
				cielingBrush.add(high);
				midBrush.add(mid);
				floorBrush.add(low);
				sectorTriggerBrush.add(damage);
			}
			
			MAPBrushSide roof = new MAPBrushSide(roofPlane, "special/nodraw", roofTexS, 0, roofTexT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
			MAPBrushSide cieling = new MAPBrushSide(cileingPlane, doomMap.WadName + "/" + currentSector.CielingTexture, cileingTexS, 0, cileingTexT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
			MAPBrushSide floor = new MAPBrushSide(floorPlane, doomMap.WadName + "/" + currentSector.FloorTexture, floorTexS, 0, floorTexT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
			MAPBrushSide foundation = new MAPBrushSide(foundationPlane, "special/nodraw", foundationTexS, 0, foundationTexT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
			MAPBrushSide top = new MAPBrushSide(topPlane, "special/nodraw", topTexS, 0, topTexT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
			MAPBrushSide bottom = new MAPBrushSide(bottomPlane, "special/nodraw", bottomTexS, 0, bottomTexT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
			MAPBrushSide invertedFloor = new MAPBrushSide(Plane.flip(floorPlane), "special/trigger", floorTexS, 0, floorTexT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
			MAPBrushSide damageTop = new MAPBrushSide(new Vector3D[]{floorPlane[0]+new Vector3D(0, 0, 1), floorPlane[1]+new Vector3D(0, 0, 1), floorPlane[2]+new Vector3D(0, 0, 1)}, "special/trigger", floorTexS, 0, floorTexT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
			sectorTriggerBrush.add(damageTop);
			sectorTriggerBrush.add(invertedFloor);
			
			midBrush.add(top);
			midBrush.add(bottom);
			
			cielingBrush.add(cieling);
			cielingBrush.add(roof);
			
			floorBrush.add(floor);
			floorBrush.add(foundation);
			
			// Now need to add the data from node subdivisions. Neither segments nor nodes
			// will completely define a usable brush, but both of them together will.
			do 
			{
				DNode currentNode = doomMap.Nodes[nextNode];
				Vector3D start;
				Vector3D end;
				if (leftSide)
				{
					start = currentNode.VecHead+currentNode.VecTail;
					end = currentNode.VecHead;
				}
				else
				{
					start = currentNode.VecHead;
					end = currentNode.VecHead+currentNode.VecTail;
				}
				
				Vector3D[] plane = new Vector3D[3];
				double[] texS = new double[3];
				double[] texT = new double[3];
				// This is somehow always correct. And I'm okay with that.
				plane[0] = new Vector3D(start.X, start.Y, ZMin);
				plane[1] = new Vector3D(end.X, end.Y, ZMin);
				plane[2] = new Vector3D(start.X, start.Y, ZMax);
				
				double sideLength = System.Math.Sqrt(System.Math.Pow(start.X - end.X, 2) + System.Math.Pow(start.Y - end.Y, 2));
				
				texS[0] = (start.X - end.X) / sideLength;
				texS[1] = (start.Y - end.Y) / sideLength;
				texS[2] = 0;
				texT[0] = 0;
				texT[1] = 0;
				texT[2] = 1;
				MAPBrushSide low = new MAPBrushSide(plane, "special/nodraw", texS, 0, texT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
				MAPBrushSide high = new MAPBrushSide(plane, "special/nodraw", texS, 0, texT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
				MAPBrushSide mid = new MAPBrushSide(plane, "special/nodraw", texS, 0, texT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
				MAPBrushSide damage = new MAPBrushSide(plane, "special/trigger", texS, 0, texT, 0, 0, 1, 1, 0, "wld_lightmap", 16, 0);
				
				cielingBrush.add(high);
				midBrush.add(mid);
				floorBrush.add(low);
				sectorTriggerBrush.add(damage);
				
				leftSide = nodeIsLeft[nextNode];
				nextNode = nodeparents[nextNode];
			}
			while (nextNode != - 1);
			// Now we need to get rid of all the sides that aren't used. Get a list of
			// the useless sides from one brush, and delete those sides from all of them,
			// since they all have the same sides.
			int[] badSides = new int[0];
			if (!Settings.dontCull)
			{
				badSides = MAPBrush.findUnusedPlanes(cielingBrush);
				// Need to iterate backward, since these lists go from low indices to high, and
				// the index of all subsequent items changes when something before it is removed.
				if (cielingBrush.NumSides - badSides.Length < 4)
				{
					DecompilerThread.OnMessage(this, "WARNING: Plane cull returned less than 4 sides for subsector " + i);
					badSides = new int[0];
				}
				else
				{
					for (int j = badSides.Length - 1; j > - 1; j--)
					{
						cielingBrush.delete(badSides[j]);
						floorBrush.delete(badSides[j]);
					}
				}
			}
			
			MAPBrush[] newFloorList = new MAPBrush[sectorFloorBrushes[subsectorSectors[i]].Length + 1];
			MAPBrush[] newCielingList = new MAPBrush[sectorCielingBrushes[subsectorSectors[i]].Length + 1];
			for (int j = 0; j < sectorFloorBrushes[subsectorSectors[i]].Length; j++)
			{
				newFloorList[j] = sectorFloorBrushes[subsectorSectors[i]][j];
				newCielingList[j] = sectorCielingBrushes[subsectorSectors[i]][j];
			}
			newFloorList[newFloorList.Length - 1] = floorBrush;
			newCielingList[newCielingList.Length - 1] = cielingBrush;
			sectorFloorBrushes[subsectorSectors[i]] = newFloorList;
			sectorCielingBrushes[subsectorSectors[i]] = newCielingList;
			
			bool containsMiddle = false;
			for (int j = 0; j < midBrush.NumSides; j++)
			{
				if (!midBrush[j].Texture.ToUpper().Equals("special/nodraw".ToUpper()))
				{
					containsMiddle = true;
					break;
				}
			}
			if (containsMiddle && currentSector.CielingHeight > currentSector.FloorHeight)
			{
				Entity middleEnt = new Entity("func_illusionary");
				if (midBrush.NumSides - badSides.Length >= 4)
				{
					for (int j = badSides.Length - 1; j > - 1; j--)
					{
						midBrush.delete(badSides[j]);
					}
				}
				
				middleEnt.Brushes.Add(midBrush);
				mapFile.Add(middleEnt);
			}
			Entity hurtMe = new Entity("trigger_hurt");
			Entity triggerSecret = new Entity("trigger_bondsecret");
			switch (currentSector.Type)
			{
				case 4: 
				// 20% damage/s with lighting properties
				case 11: 
				// 20% damage/s
				case 16:  // 20% damage/s plus end level when player is almost dead
					hurtMe["dmg"] = "40";
					if (!Settings.dontCull)
					{
						if (sectorTriggerBrush.NumSides - badSides.Length >= 4)
						{
							for (int j = badSides.Length - 1; j > - 1; j--)
							{
								sectorTriggerBrush.delete(badSides[j]);
							}
						}
					}
					hurtMe.Brushes.Add(sectorTriggerBrush);
					mapFile.Add(hurtMe);
					break;
				case 5:  // 10% damage/s
					hurtMe["dmg"] = "20";
					if (!Settings.dontCull)
					{
						if (sectorTriggerBrush.NumSides - badSides.Length >= 4)
						{
							for (int j = badSides.Length - 1; j > - 1; j--)
							{
								sectorTriggerBrush.delete(badSides[j]);
							}
						}
					}
					hurtMe.Brushes.Add(sectorTriggerBrush);
					mapFile.Add(hurtMe);
					break;
				case 7:  // 5% damage/s
					hurtMe["dmg"] = "10";
					if (!Settings.dontCull)
					{
						if (sectorTriggerBrush.NumSides - badSides.Length >= 4)
						{
							for (int j = badSides.Length - 1; j > - 1; j--)
							{
								sectorTriggerBrush.delete(badSides[j]);
							}
						}
					}
					hurtMe.Brushes.Add(sectorTriggerBrush);
					mapFile.Add(hurtMe);
					break;
				case 9:  // "secret"
					triggerSecret["Sound"] = "common/mission_success.wav";
					if (!Settings.dontCull)
					{
						if (sectorTriggerBrush.NumSides - badSides.Length >= 4)
						{
							for (int j = badSides.Length - 1; j > - 1; j--)
							{
								sectorTriggerBrush.delete(badSides[j]);
							}
						}
					}
					triggerSecret.Brushes.Add(sectorTriggerBrush);
					mapFile.Add(triggerSecret);
					break;
				case 10:  // 30 seconds after level start, "close" like a door
					if (currentSector.Tag == 0)
					{
						sectorTag[subsectorSectors[i]] = - 3;
					}
					break;
				case 14:  // 300 seconds after level start, "open" like a door
					if (currentSector.Tag == 0)
					{
						sectorTag[subsectorSectors[i]] = - 4;
					}
					break;
				default: 
					if (!Settings.dontCull)
					{
						if (sectorTriggerBrush.NumSides - badSides.Length >= 4)
						{
							for (int j = badSides.Length - 1; j > - 1; j--)
							{
								sectorTriggerBrush.delete(badSides[j]);
							}
						}
					}
					if ((currentSector.Type & 96) != 0)
					{
						// "Generalized" pain sectors
						if ((currentSector.Type & 96) == 32)
						{
							hurtMe["dmg"] = "10";
						}
						else
						{
							if ((currentSector.Type & 96) == 64)
							{
								hurtMe["dmg"] = "20";
							}
							else
							{
								if ((currentSector.Type & 96) == 96)
								{
									hurtMe["dmg"] = "40";
								}
							}
						}
						hurtMe.Brushes.Add(sectorTriggerBrush);
						mapFile.Add(hurtMe);
					}
					if ((currentSector.Type & 128) == 128)
					{
						// "Generalized" secret trigger
						triggerSecret["Sound"] = "common/mission_success.wav";
						triggerSecret.Brushes.Add(sectorTriggerBrush);
						mapFile.Add(triggerSecret);
					}
					break;
			}
			if((i+1)%onePercent == 0) {
				parent.OnProgress(this, (i+1)/(double)(doomMap.SubSectors.Count));
			}
			//Settings.setProgress(jobnum, i + 1, doomMap.SubSectors.Count, "Decompiling...");
		}
		
		// Add the brushes to the map, as world by default, or entities if they are supported.
		for (int i = 0; i < doomMap.Sectors.Count; i++)
		{
			bool[] floorsUsed = new bool[sectorFloorBrushes[i].Length];
			bool[] cielingsUsed = new bool[sectorCielingBrushes[i].Length];
			if (sectorTag[i] == 0)
			{
				for (int j = 0; j < sectorFloorBrushes[i].Length; j++)
				{
					world.Brushes.Add(sectorFloorBrushes[i][j]);
					floorsUsed[j] = true;
					world.Brushes.Add(sectorCielingBrushes[i][j]);
					cielingsUsed[j] = true;
				}
			}
			else
			{
				if (sectorTag[i] == - 1 || sectorTag[i] == - 2 || sectorTag[i] == - 3 || sectorTag[i] == - 4)
				{
					// I'm using this to mean a door with no tag number
					Entity newDoor = new Entity("func_door");
					newDoor["speed"] = "60";
					newDoor["angles"] = "270 0 0";
					newDoor["spawnflags"] = "256";
					newDoor["targetname"] = "sectornum" + i + "door";
					if (sectorTag[i] == - 1)
					{
						newDoor["wait"] = "4";
					}
					else
					{
						if (sectorTag[i] == - 2)
						{
							newDoor["wait"] = "-1";
						}
					}
					if (sectorTag[i] == - 3)
					{
						Entity triggerAuto = new Entity("trigger_auto");
						triggerAuto["target"] = "sectornum" + i + "door_mm";
						Entity multiManager = new Entity("multi_manager");
						multiManager["sectornum" + i + "door"] = "30";
						mapFile.Add(triggerAuto);
						mapFile.Add(multiManager);
					}
					if (sectorTag[i] == - 4)
					{
						newDoor["Spawnflags"] = "1";
						Entity triggerAuto = new Entity("trigger_auto");
						triggerAuto["target"] = "sectornum" + i + "door_mm";
						Entity multiManager = new Entity("multi_manager");
						multiManager["sectornum" + i + "door"] = "300";
						mapFile.Add(triggerAuto);
						mapFile.Add(multiManager);
					}
					int lowestNeighborCielingHeight = getLowestNeighborCielingHeight(i);
					int lip = ZMax - lowestNeighborCielingHeight + 4;
					newDoor["lip"] = "" + lip;
					for (int j = 0; j < sectorFloorBrushes[i].Length; j++)
					{
						cielingsUsed[j] = true;
						newDoor.Brushes.Add(sectorCielingBrushes[i][j]);
					}
					mapFile.Add(newDoor);
				}
				else
				{
					for (int j = 0; j < doomMap.Linedefs.Count; j++)
					{
						DLinedef currentLinedef = doomMap.Linedefs[j];
						int linedefTriggerType = currentLinedef.Action;
						if (doomMap.Version == mapType.TYPE_HEXEN)
						{
							switch (linedefTriggerType)
							{
								
								case 21: 
								// Floor lower to lowest neighbor
								case 22:  // Floor lower to nearest lower neighbor
									// I don't know where retriggerability is determined, or whether or not it goes back up.
									if (currentLinedef.Arguments[0] == sectorTag[i])
									{
										Entity newFloor = new Entity("func_door");
										newFloor["angles"] = "90 0 0";
										newFloor["wait"] = "-1";
										newFloor["speed"] = "" + currentLinedef.Arguments[1];
										if (currentLinedef.Arguments[0] == 0)
										{
											newFloor["targetname"] = "sectornum" + i + "lowerfloor";
										}
										else
										{
											newFloor["targetname"] = "sector" + currentLinedef.Arguments[0] + "lowerfloor";
										}
										int lowestNeighborFloorHeight;
										if (linedefTriggerType == 21)
										{
											lowestNeighborFloorHeight = getLowestNeighborFloorHeight(i);
										}
										else
										{
											lowestNeighborFloorHeight = getNextLowestNeighborFloorHeight(i);
										}
										if (lowestNeighborFloorHeight == 32768)
										{
											lowestNeighborFloorHeight = doomMap.Sectors[i].FloorHeight;
										}
										int lip = ZMin - lowestNeighborFloorHeight;
										newFloor["lip"] = "" + System.Math.Abs(lip);
										for (int k = 0; k < sectorFloorBrushes[i].Length; k++)
										{
											if (!floorsUsed[k])
											{
												floorsUsed[k] = true;
												newFloor.Brushes.Add(sectorFloorBrushes[i][k]);
											}
										}
										mapFile.Add(newFloor);
									}
									break;
								case 24: 
								// Floor raise to highest neighbor
								case 25:  // Floor raise to nearest higher neighbor
									// I don't know where retriggerability is determined, or whether or not it goes back up.
									if (currentLinedef.Arguments[0] == sectorTag[i])
									{
										Entity newFloor = new Entity("func_door");
										newFloor["angles"] = "270 0 0";
										newFloor["wait"] = "-1";
										newFloor["speed"] = "" + currentLinedef.Arguments[1];
										if (currentLinedef.Arguments[0] == 0)
										{
											newFloor["targetname"] = "sectornum" + i + "raisefloor";
										}
										else
										{
											newFloor["targetname"] = "sector" + currentLinedef.Arguments[0] + "raisefloor";
										}
										int highestNeighborFloorHeight;
										if (linedefTriggerType == 24)
										{
											highestNeighborFloorHeight = getHighestNeighborFloorHeight(i);
										}
										else
										{
											highestNeighborFloorHeight = getNextHighestNeighborFloorHeight(i);
										}
										if (highestNeighborFloorHeight == - 32768)
										{
											highestNeighborFloorHeight = doomMap.Sectors[i].FloorHeight;
										}
										int lip = ZMin - highestNeighborFloorHeight;
										newFloor["lip"] = "" + System.Math.Abs(lip);
										for (int k = 0; k < sectorFloorBrushes[i].Length; k++)
										{
											if (!floorsUsed[k])
											{
												floorsUsed[k] = true;
												newFloor.Brushes.Add(sectorFloorBrushes[i][k]);
											}
										}
										mapFile.Add(newFloor);
									}
									break;
								}
						}
						else
						{
							if (currentLinedef.Tag == sectorTag[i])
							{
								switch (linedefTriggerType)
								{
									
									case 36: 
									// Line crossed, floor lowers, stays 8 above next lowest
									case 38:  // Line crossed, floor lowers, stays at next lowest
										Entity newFloor = new Entity("func_door");
										newFloor["speed"] = "120";
										newFloor["angles"] = "90 0 0";
										newFloor["targetname"] = "sector" + sectorTag[i] + "lowerfloor";
										newFloor["wait"] = "-1";
										int lowestNeighborFloorHeight = getLowestNeighborFloorHeight(i);
										int lip = ZMin - lowestNeighborFloorHeight;
										if (linedefTriggerType == 36)
										{
											lip -= 8;
										}
										newFloor["lip"] = "" + System.Math.Abs(lip);
										for (int k = 0; k < sectorFloorBrushes[i].Length; k++)
										{
											if (!floorsUsed[k])
											{
												floorsUsed[k] = true;
												newFloor.Brushes.Add(sectorFloorBrushes[i][k]);
											}
										}
										mapFile.Add(newFloor);
										break;
									case 63: 
									// Push button, door opens, waits 4s, closes
									case 103: 
									// Push button, door opens, stays
									case 109:  // Cross line, door opens, stays
										Entity newDoor = new Entity("func_door");
										newDoor["speed"] = "60";
										newDoor["angles"] = "270 0 0";
										newDoor["targetname"] = "sector" + sectorTag[i] + "door";
										newDoor["wait"] = "-1";
										if (sectorTag[i] == 63)
										{
											newDoor["wait"] = "4";
										}
										int lowestNeighborCielingHeight = getLowestNeighborCielingHeight(i);
										lip = ZMax - lowestNeighborCielingHeight + 4;
										newDoor["lip"] = "" + lip;
										for (int k = 0; k < sectorFloorBrushes[i].Length; k++)
										{
											if (!cielingsUsed[k])
											{
												cielingsUsed[k] = true;
												newDoor.Brushes.Add(sectorCielingBrushes[i][k]);
											}
										}
										mapFile.Add(newDoor);
										break;
									case 62: 
									// Push button, Elevator goes down to lowest, wait 4s, goes up
									case 88:  // Elevator goes down to lowest, wait 4s, goes up
										Entity newVator = new Entity("func_door");
										newVator["speed"] = "120";
										newVator["angles"] = "90 0 0";
										newVator["targetname"] = "sector" + sectorTag[i] + "vator";
										newVator["wait"] = "4";
										lowestNeighborFloorHeight = getLowestNeighborFloorHeight(i);
										lip = System.Math.Abs(ZMin - lowestNeighborFloorHeight);
										newVator["lip"] = "" + lip;
										for (int k = 0; k < sectorFloorBrushes[i].Length; k++)
										{
											if (!floorsUsed[k])
											{
												newVator.Brushes.Add(sectorFloorBrushes[i][k]);
												floorsUsed[k] = true;
											}
										}
										mapFile.Add(newVator);
										break;
									default:  // I'd like to not use this evenutally, all the trigger types ought to be handled
										DecompilerThread.OnMessage(this, "WARNING: Unimplemented linedef trigger type " + linedefTriggerType + " for sector " + i + " tagged " + sectorTag[i]);
										for (int k = 0; k < sectorFloorBrushes[i].Length; k++)
										{
											if (!floorsUsed[k])
											{
												world.Brushes.Add(sectorFloorBrushes[i][k]);
												floorsUsed[k] = true;
											}
											if (!cielingsUsed[k])
											{
												world.Brushes.Add(sectorCielingBrushes[i][k]);
												cielingsUsed[k] = true;
											}
										}
										break;
								}
							}
						}
					}
				}
			}
			for (int j = 0; j < sectorFloorBrushes[i].Length; j++)
			{
				if (!cielingsUsed[j])
				{
					world.Brushes.Add(sectorCielingBrushes[i][j]);
				}
				if (!floorsUsed[j])
				{
					world.Brushes.Add(sectorFloorBrushes[i][j]);
				}
			}
		}
		
		// Convert THINGS
		for (int i = 0; i < doomMap.Things.Count; i++)
		{
			DThing currentThing = doomMap.Things[i];
			// To find the true height of a thing, I need to iterate through nodes until I come to a subsector
			// definition. Then I need to use the floor height of the sector that subsector belongs to.
			Vector3D origin = currentThing.Origin;
			int subsectorIndex = doomMap.Nodes.Count - 1;
			while (subsectorIndex >= 0)
			{
				// Once child is negative, subsector is found
				DNode currentNode = doomMap.Nodes[subsectorIndex];
				Vector3D start = currentNode.VecHead;
				Vector3D end = currentNode.VecHead+currentNode.VecTail;
				Plane currentPlane = new Plane(start, end, new Vector3D(start.X, start.Y, 1));
				if (currentPlane.distance(origin) < 0)
				{
					subsectorIndex = currentNode.Child1;
				}
				else
				{
					subsectorIndex = currentNode.Child2;
				}
			}
			subsectorIndex += 32768;
			int sectorIndex = subsectorSectors[subsectorIndex];
			DSector thingSector = doomMap.Sectors[sectorIndex];
			if (origin.Z == 0)
			{
				origin.Z=thingSector.FloorHeight;
			}
			
			Entity thing = null;
			// Things from both Doom. Currently converting to appropriate Doom 3 entities.
			switch (currentThing.ClassNum)
			{
				case 1: 
				// Single player spawn
				case 2: 
				// coop
				case 3: 
				// coop
				case 4:  // coop
					thing = new Entity("info_player_start");
					if (currentThing.ClassNum > 1)
					{
						thing["targetname"] = "coopspawn" + currentThing.ClassNum;
					}
					playerStartOrigin = origin.X + " " + origin.Y + " " + (origin.Z + 36);
					thing["origin"] = playerStartOrigin;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 11:  // Deathmatch spawn
					thing = new Entity("info_player_deathmatch");
					thing["origin"] = origin.X + " " + origin.Y + " " + (origin.Z + 36);
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 14:  // Teleport destination
					thing = new Entity("info_teleport_destination");
					if (currentThing.ID != 0)
					{
						thing["targetname"] = "teledest" + currentThing.ID;
					}
					else
					{
						thing["targetname"] = "sector" + thingSector.Tag + "teledest";
					}
					thing["origin"] = origin.X + " " + origin.Y + " " + (origin.Z + 36);
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 17:  // Big cell pack
					thing = new Entity("ammo_cells_large");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 82:  // Super shotgun
					thing = new Entity("weapon_shotgun_double");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2001:  // Shotgun
					thing = new Entity("weapon_shotgun");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2002:  // Chaingun
					thing = new Entity("weapon_chaingun");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2003:  // Rocket launcher
					thing = new Entity("weapon_rocketlauncher");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2004:  // Plasma gun
					thing = new Entity("weapon_plasmagun");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2005:  // Chainsaw
					thing = new Entity("weapon_chainsaw");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2006:  // BFG9000
					thing = new Entity("weapon_bfg");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2007:  // Ammo clip
					thing = new Entity("ammo_clip_small");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2008:  // Shotgun shells
					thing = new Entity("ammo_shells_small");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2010:  // Rocket
					thing = new Entity("ammo_rockets_small");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2046:  // Box of Rockets
					thing = new Entity("ammo_rockets_large");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2047:  // Cell pack
					thing = new Entity("ammo_cells_small");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2048:  // Box of ammo
					thing = new Entity("ammo_bullets_large");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				
				case 2049:  // Box of shells
					thing = new Entity("ammo_shells_large");
					thing["origin"] = origin.X + " " + origin.Y + " " + origin.Z;
					thing["angles"] = "0 " + currentThing.Angle + " 0";
					break;
				}
			
			if (thing != null)
			{
				mapFile.Add(thing);
			}
		}
		
		Entity playerequip = new Entity("game_player_equip");
		playerequip["weapon_pistol"] = "1";
		playerequip["origin"] = playerStartOrigin;
		mapFile.Add(playerequip);
		parent.OnProgress(this, 1.0);
		return mapFile;
	}
	
	private int getLowestNeighborCielingHeight(int sector)
	{
		int lowestNeighborCielingHeight = 32768;
		for (int j = 0; j < doomMap.Linedefs.Count; j++)
		{
			DLinedef currentLinedef = doomMap.Linedefs[j];
			if (!currentLinedef.OneSided)
			{
				DSector neighbor = null;
				if (doomMap.Sidedefs[currentLinedef.Left].Sector == sector)
				{
					neighbor = doomMap.Sectors[doomMap.Sidedefs[currentLinedef.Right].Sector];
				}
				else
				{
					if (doomMap.Sidedefs[currentLinedef.Right].Sector == sector)
					{
						neighbor = doomMap.Sectors[doomMap.Sidedefs[currentLinedef.Left].Sector];
					}
				}
				if (neighbor != null && neighbor.CielingHeight < lowestNeighborCielingHeight)
				{
					lowestNeighborCielingHeight = neighbor.CielingHeight;
				}
			}
		}
		return lowestNeighborCielingHeight;
	}
	
	private int getLowestNeighborFloorHeight(int sector)
	{
		int lowestNeighborFloorHeight = 32768;
		for (int k = 0; k < doomMap.Linedefs.Count; k++)
		{
			DLinedef currentSearchLinedef = doomMap.Linedefs[k];
			if (!currentSearchLinedef.OneSided)
			{
				DSector neighbor = null;
				if (doomMap.Sidedefs[currentSearchLinedef.Left].Sector == sector)
				{
					neighbor = doomMap.Sectors[doomMap.Sidedefs[currentSearchLinedef.Right].Sector];
				}
				else
				{
					if (doomMap.Sidedefs[currentSearchLinedef.Right].Sector == sector)
					{
						neighbor = doomMap.Sectors[doomMap.Sidedefs[currentSearchLinedef.Left].Sector];
					}
				}
				if (neighbor != null && neighbor.FloorHeight < lowestNeighborFloorHeight)
				{
					lowestNeighborFloorHeight = neighbor.FloorHeight;
				}
			}
		}
		return lowestNeighborFloorHeight;
	}
	
	private int getHighestNeighborCielingHeight(int sector)
	{
		int highestNeighborCielingHeight = - 32768;
		for (int j = 0; j < doomMap.Linedefs.Count; j++)
		{
			DLinedef currentLinedef = doomMap.Linedefs[j];
			if (!currentLinedef.OneSided)
			{
				DSector neighbor = null;
				if (doomMap.Sidedefs[currentLinedef.Left].Sector == sector)
				{
					neighbor = doomMap.Sectors[doomMap.Sidedefs[currentLinedef.Right].Sector];
				}
				else
				{
					if (doomMap.Sidedefs[currentLinedef.Right].Sector == sector)
					{
						neighbor = doomMap.Sectors[doomMap.Sidedefs[currentLinedef.Left].Sector];
					}
				}
				if (neighbor != null && neighbor.CielingHeight > highestNeighborCielingHeight)
				{
					highestNeighborCielingHeight = neighbor.CielingHeight;
				}
			}
		}
		return highestNeighborCielingHeight;
	}
	
	private int getHighestNeighborFloorHeight(int sector)
	{
		int highestNeighborFloorHeight = - 32768;
		for (int k = 0; k < doomMap.Linedefs.Count; k++)
		{
			DLinedef currentSearchLinedef = doomMap.Linedefs[k];
			if (!currentSearchLinedef.OneSided)
			{
				DSector neighbor = null;
				if (doomMap.Sidedefs[currentSearchLinedef.Left].Sector == sector)
				{
					neighbor = doomMap.Sectors[doomMap.Sidedefs[currentSearchLinedef.Right].Sector];
				}
				else
				{
					if (doomMap.Sidedefs[currentSearchLinedef.Right].Sector == sector)
					{
						neighbor = doomMap.Sectors[doomMap.Sidedefs[currentSearchLinedef.Left].Sector];
					}
				}
				if (neighbor != null && neighbor.FloorHeight > highestNeighborFloorHeight)
				{
					highestNeighborFloorHeight = neighbor.FloorHeight;
				}
			}
		}
		return highestNeighborFloorHeight;
	}
	
	private int getNextLowestNeighborCielingHeight(int sector)
	{
		int nextLowestNeighborCielingHeight = 32768;
		int current = doomMap.Sectors[sector].CielingHeight;
		for (int j = 0; j < doomMap.Linedefs.Count; j++)
		{
			DLinedef currentLinedef = doomMap.Linedefs[j];
			if (!currentLinedef.OneSided)
			{
				DSector neighbor = null;
				if (doomMap.Sidedefs[currentLinedef.Left].Sector == sector)
				{
					neighbor = doomMap.Sectors[doomMap.Sidedefs[currentLinedef.Right].Sector];
				}
				else
				{
					if (doomMap.Sidedefs[currentLinedef.Right].Sector == sector)
					{
						neighbor = doomMap.Sectors[doomMap.Sidedefs[currentLinedef.Left].Sector];
					}
				}
				if (neighbor != null && neighbor.CielingHeight > nextLowestNeighborCielingHeight && neighbor.CielingHeight < current)
				{
					nextLowestNeighborCielingHeight = neighbor.CielingHeight;
				}
			}
		}
		return nextLowestNeighborCielingHeight;
	}
	
	private int getNextLowestNeighborFloorHeight(int sector)
	{
		int nextLowestNeighborFloorHeight = 32768;
		int current = doomMap.Sectors[sector].FloorHeight;
		for (int k = 0; k < doomMap.Linedefs.Count; k++)
		{
			DLinedef currentSearchLinedef = doomMap.Linedefs[k];
			if (!currentSearchLinedef.OneSided)
			{
				DSector neighbor = null;
				if (doomMap.Sidedefs[currentSearchLinedef.Left].Sector == sector)
				{
					neighbor = doomMap.Sectors[doomMap.Sidedefs[currentSearchLinedef.Right].Sector];
				}
				else
				{
					if (doomMap.Sidedefs[currentSearchLinedef.Right].Sector == sector)
					{
						neighbor = doomMap.Sectors[doomMap.Sidedefs[currentSearchLinedef.Left].Sector];
					}
				}
				if (neighbor != null && neighbor.FloorHeight > nextLowestNeighborFloorHeight && neighbor.FloorHeight < current)
				{
					nextLowestNeighborFloorHeight = neighbor.FloorHeight;
				}
			}
		}
		return nextLowestNeighborFloorHeight;
	}
	
	private int getNextHighestNeighborCielingHeight(int sector)
	{
		int nextHighestNeighborCielingHeight = - 32768;
		int current = doomMap.Sectors[sector].CielingHeight;
		for (int j = 0; j < doomMap.Linedefs.Count; j++)
		{
			DLinedef currentLinedef = doomMap.Linedefs[j];
			if (!currentLinedef.OneSided)
			{
				DSector neighbor = null;
				if (doomMap.Sidedefs[currentLinedef.Left].Sector == sector)
				{
					neighbor = doomMap.Sectors[doomMap.Sidedefs[currentLinedef.Right].Sector];
				}
				else
				{
					if (doomMap.Sidedefs[currentLinedef.Right].Sector == sector)
					{
						neighbor = doomMap.Sectors[doomMap.Sidedefs[currentLinedef.Left].Sector];
					}
				}
				if (neighbor != null && neighbor.CielingHeight < nextHighestNeighborCielingHeight && neighbor.CielingHeight > current)
				{
					nextHighestNeighborCielingHeight = neighbor.CielingHeight;
				}
			}
		}
		return nextHighestNeighborCielingHeight;
	}
	
	private int getNextHighestNeighborFloorHeight(int sector)
	{
		int nextHighestNeighborFloorHeight = - 32768;
		int current = doomMap.Sectors[sector].FloorHeight;
		for (int k = 0; k < doomMap.Linedefs.Count; k++)
		{
			DLinedef currentSearchLinedef = doomMap.Linedefs[k];
			if (!currentSearchLinedef.OneSided)
			{
				DSector neighbor = null;
				if (doomMap.Sidedefs[currentSearchLinedef.Left].Sector == sector)
				{
					neighbor = doomMap.Sectors[doomMap.Sidedefs[currentSearchLinedef.Right].Sector];
				}
				else
				{
					if (doomMap.Sidedefs[currentSearchLinedef.Right].Sector == sector)
					{
						neighbor = doomMap.Sectors[doomMap.Sidedefs[currentSearchLinedef.Left].Sector];
					}
				}
				if (neighbor != null && neighbor.FloorHeight < nextHighestNeighborFloorHeight && neighbor.FloorHeight > current)
				{
					nextHighestNeighborFloorHeight = neighbor.FloorHeight;
				}
			}
		}
		return nextHighestNeighborFloorHeight;
	}
}