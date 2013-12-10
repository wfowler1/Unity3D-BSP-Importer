using System;
// BSP46Decompiler
// Decompiles a v46 BSP

public class BSP46Decompiler {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	public const int A = 0;
	public const int B = 1;
	public const int C = 2;
	
	public const int X = 0;
	public const int Y = 1;
	public const int Z = 2;
	
	private int jobnum;
	
	private Entities mapFile; // Most MAP file formats (including GearCraft) are simply a bunch of nested entities
	private int numBrshs;
	private int numSimpleCorrects = 0;
	private int numAdvancedCorrects = 0;
	private int numGoodBrushes = 0;
	private int currentSideIndex = 0;
	private bool isCoD = false;
	private DecompilerThread parent;
	
	private BSP BSPObject;
	
	// CONSTRUCTORS
	
	// This constructor sets everything according to specified settings.
	public BSP46Decompiler(BSP BSPObject, int jobnum, DecompilerThread parent)
	{
		// Set up global variables
		this.BSPObject = BSPObject;
		this.jobnum = jobnum;
		this.parent = parent;
	}
	
	// METHODS
	
	// +decompile()
	// Attempts to convert the BSP file back into a .MAP file.
	public virtual Entities decompile()
	{
		DecompilerThread.OnMessage(this, "Decompiling...");
		// In the decompiler, it is not necessary to copy all entities to a new object, since
		// no writing is ever done back to the BSP file.
		mapFile = BSPObject.Entities;
		int numTotalItems = 0;
		int onePercent = (int)((BSPObject.Brushes.Count + BSPObject.Entities.Count)/100);
		// I need to go through each entity and see if it's brush-based.
		// Worldspawn is brush-based as well as any entity with model *#.
		for (int i = 0; i < BSPObject.Entities.Count; i++)
		{
			// For each entity
			//DecompilerThread.OnMessage(this, "Entity " + i + ": " + mapFile[i]["classname"]);
			numBrshs = 0; // Reset the brush count for each entity
			// getModelNumber() returns 0 for worldspawn, the *# for brush based entities, and -1 for everything else
			int currentModel = mapFile[i].ModelNumber;
			if (currentModel > - 1)
			{
				// If this is still -1 then it's strictly a point-based entity. Move on to the next one.
				int firstBrush = BSPObject.Models[currentModel].FirstBrush;
				int numBrushes = BSPObject.Models[currentModel].NumBrushes;
				numBrshs = 0;
				for (int j = 0; j < numBrushes; j++)
				{
					// For each brush
					//Console.Write("Brush " + j);
					decompileBrush(BSPObject.Brushes[j + firstBrush], i); // Decompile the brush
					numBrshs++;
					numTotalItems++;
					if(numTotalItems%onePercent == 0) {
						parent.OnProgress(this, numTotalItems/(double)(BSPObject.Brushes.Count + BSPObject.Entities.Count));
					}
				}
			}
			numTotalItems++;
			if(numTotalItems%onePercent == 0) {
				parent.OnProgress(this, numTotalItems/(double)(BSPObject.Brushes.Count + BSPObject.Entities.Count));
			}
		}
		if (!Settings.skipPlaneFlip)
		{
			DecompilerThread.OnMessage(this, "Num simple corrected brushes: " + numSimpleCorrects);
			DecompilerThread.OnMessage(this, "Num advanced corrected brushes: " + numAdvancedCorrects);
			DecompilerThread.OnMessage(this, "Num good brushes: " + numGoodBrushes);
		}
		parent.OnProgress(this, 1.0);
		return mapFile;
	}
	
	// -decompileBrush(Brush, int)
	// Decompiles the Brush and adds it to entitiy #currentEntity as MAPBrush classes.
	private void  decompileBrush(Brush brush, int currentEntity)
	{
		Vector3D origin = mapFile[currentEntity].Origin;
		int firstSide = brush.FirstSide;
		int numSides = brush.NumSides;
		if (firstSide < 0)
		{
			isCoD = true;
			firstSide = currentSideIndex;
			currentSideIndex += numSides;
		}
		MAPBrushSide[] brushSides = new MAPBrushSide[0];
		bool isDetail = false;
		int brushTextureIndex = brush.Texture;
		byte[] contents = new byte[4];
		if (brushTextureIndex >= 0)
		{
			contents = BSPObject.Textures[brushTextureIndex].Contents;
		}
		if (!Settings.noDetail && (contents[3] & ((byte) 1 << 3)) != 0)
		{
			// This is the flag according to q3 source
			isDetail = true; // it's the same as Q2 (and Source), but I haven't found any Q3 maps that use it, so far
		}
		MAPBrush mapBrush = new MAPBrush(numBrshs, currentEntity, isDetail);
		int numRealFaces = 0;
		Plane[] brushPlanes = new Plane[0];
		//DecompilerThread.OnMessage(this, ": " + numSides + " sides");
		if (!Settings.noWater && (contents[0] & ((byte) 1 << 5)) != 0)
		{
			mapBrush.Water = true;
		}
		bool isVisBrush = false;
		for (int i = 0; i < numSides; i++)
		{
			// For each side of the brush
			BrushSide currentSide = BSPObject.BrushSides[firstSide + i];
			int currentFaceIndex = currentSide.Face;
			Plane currentPlane;
			if (isCoD)
			{
				if (i == 0)
				{
					// XMin
					currentPlane = new Plane((double) (- 1), (double) 0, (double) 0, (double) (- currentSide.Dist));
				}
				else
				{
					if (i == 1)
					{
						// XMax
						currentPlane = new Plane((double) 1, (double) 0, (double) 0, (double) currentSide.Dist);
					}
					else
					{
						if (i == 2)
						{
							// YMin
							currentPlane = new Plane((double) 0, (double) (- 1), (double) 0, (double) (- currentSide.Dist));
						}
						else
						{
							if (i == 3)
							{
								// YMax
								currentPlane = new Plane((double) 0, (double) 1, (double) 0, (double) currentSide.Dist);
							}
							else
							{
								if (i == 4)
								{
									// ZMin
									currentPlane = new Plane((double) 0, (double) 0, (double) (- 1), (double) (- currentSide.Dist));
								}
								else
								{
									if (i == 5)
									{
										// ZMax
										currentPlane = new Plane((double) 0, (double) 0, (double) 1, (double) currentSide.Dist);
									}
									else
									{
										currentPlane = BSPObject.Planes[currentSide.Plane];
									}
								}
							}
						}
					}
				}
			}
			else
			{
				currentPlane = BSPObject.Planes[currentSide.Plane];
			}
			Vector3D[] triangle = new Vector3D[0];
			bool pointsWorked = false;
			int firstVertex = - 1;
			int numVertices = 0;
			string texture = "noshader";
			bool masked = false;
			if (currentFaceIndex > - 1)
			{
				Face currentFace = BSPObject.Faces[currentFaceIndex];
				int currentTextureIndex = currentFace.Texture;
				firstVertex = currentFace.FirstVertex;
				numVertices = currentFace.NumVertices;
				string mask = BSPObject.Textures[currentTextureIndex].Mask;
				if (mask.ToUpper().Equals("ignore".ToUpper()) || mask.Length == 0)
				{
					texture = BSPObject.Textures[currentTextureIndex].Name;
				}
				else
				{
					texture = mask.Substring(0, (mask.Length - 4) - (0)); // Because mask includes file extensions
					masked = true;
				}
				if (numVertices != 0 && !Settings.planarDecomp)
				{
					// If the face actually references a set of vertices
					triangle = new Vector3D[3]; // Three points define a plane. All I have to do is find three points on that plane.
					triangle[0] = new Vector3D(BSPObject.Vertices[firstVertex].Vector); // Grab and store the first one
					int j = 1;
					for (; j < numVertices; j++)
					{
						// For each point after the first one
						triangle[1] = new Vector3D(BSPObject.Vertices[firstVertex + j].Vector);
						if (triangle[0]!=triangle[1])
						{
							// Make sure the point isn't the same as the first one
							break; // If it isn't the same, this point is good
						}
					}
					for (j = j + 1; j < numVertices; j++)
					{
						// For each point after the previous one used
						triangle[2] = new Vector3D(BSPObject.Vertices[firstVertex + j].Vector);
						if (triangle[2]!=triangle[0] && triangle[2]!=triangle[1])
						{
							// Make sure no point is equal to the third one
							// Make sure all three points are non collinear
							Vector3D cr = Vector3D.crossProduct(triangle[0].subtract(triangle[1]), triangle[0].subtract(triangle[2]));
							if (cr.magnitude() > Settings.precision)
							{
								// vector length is never negative.
								pointsWorked = true;
								break;
							}
						}
					}
				}
			}
			else
			{
				// If face information is not available, use the brush side's info instead
				int currentTextureIndex = currentSide.Texture;
				if (currentTextureIndex >= 0)
				{
					string mask = BSPObject.Textures[currentTextureIndex].Mask;
					if (mask.ToUpper().Equals("ignore".ToUpper()) || mask.Length == 0)
					{
						texture = BSPObject.Textures[currentTextureIndex].Name;
					}
					else
					{
						texture = mask.Substring(0, (mask.Length - 4) - (0)); // Because mask includes file extensions
						masked = true;
					}
				}
				else
				{
					// If neither face or brush side has texture info, fall all the way back to brush. I don't know if this ever happens.
					if (brushTextureIndex >= 0)
					{
						// If none of them have any info, noshader
						string mask = BSPObject.Textures[brushTextureIndex].Mask;
						if (mask.ToUpper().Equals("ignore".ToUpper()) || mask.Length == 0)
						{
							texture = BSPObject.Textures[brushTextureIndex].Name;
						}
						else
						{
							texture = mask.Substring(0, (mask.Length - 4) - (0)); // Because mask includes file extensions
							masked = true;
						}
					}
				}
			}
			if (texture.ToUpper().Equals("textures/common/vis".ToUpper()))
			{
				isVisBrush = true;
				break;
			}
			// Get the lengths of the axis vectors.
			// TODO: This information seems to be contained in Q3's vertex structure. But there doesn't seem
			// to be a way to directly link faces to brush sides.
			double UAxisLength = 1;
			double VAxisLength = 1;
			double texScaleS = 1;
			double texScaleT = 1;
			Vector3D[] textureAxes = TexInfo.textureAxisFromPlane(currentPlane);
			double originShiftS = (textureAxes[0].X * origin[X]) + (textureAxes[0].Y * origin[Y]) + (textureAxes[0].Z * origin[Z]);
			double originShiftT = (textureAxes[1].X * origin[X]) + (textureAxes[1].Y * origin[Y]) + (textureAxes[1].Z * origin[Z]);
			double textureShiftS;
			double textureShiftT;
			if (firstVertex >= 0)
			{
				textureShiftS = (double) BSPObject.Vertices[firstVertex].TexCoordX - originShiftS;
				textureShiftT = (double) BSPObject.Vertices[firstVertex].TexCoordY - originShiftT;
			}
			else
			{
				textureShiftS = 0 - originShiftS;
				textureShiftT = 0 - originShiftT;
			}
			float texRot = 0;
			string material;
			if (masked)
			{
				material = "wld_masked";
			}
			else
			{
				material = "wld_lightmap";
			}
			double lgtScale = 16;
			double lgtRot = 0;
			MAPBrushSide[] newList = new MAPBrushSide[brushSides.Length + 1];
			for (int j = 0; j < brushSides.Length; j++)
			{
				newList[j] = brushSides[j];
			}
			int flags;
			//if(Settings.noFaceFlags) {
			flags = 0;
			//}
			if (pointsWorked)
			{
				newList[brushSides.Length] = new MAPBrushSide(currentPlane, triangle, texture, textureAxes[0].Point, textureShiftS, textureAxes[1].Point, textureShiftT, texRot, texScaleS, texScaleT, flags, material, lgtScale, lgtRot);
			}
			else
			{
				newList[brushSides.Length] = new MAPBrushSide(currentPlane, texture, textureAxes[0].Point, textureShiftS, textureAxes[1].Point, textureShiftT, texRot, texScaleS, texScaleT, flags, material, lgtScale, lgtRot);
			}
			brushSides = newList;
			numRealFaces++;
		}
		
		for (int i = 0; i < brushSides.Length; i++)
		{
			mapBrush.add(brushSides[i]);
		}
		
		brushPlanes = new Plane[mapBrush.NumSides];
		for (int i = 0; i < brushPlanes.Length; i++)
		{
			brushPlanes[i] = mapBrush[i].Plane;
		}
		
		if (isCoD && mapBrush.NumSides > 6)
		{
			// Now we need to get rid of all the sides that aren't used. Get a list of
			// the useless sides from one brush, and delete those sides from all of them,
			// since they all have the same sides.
			if (!Settings.dontCull && numSides > 6)
			{
				int[] badSides = MAPBrush.findUnusedPlanes(mapBrush);
				// Need to iterate backward, since these lists go from low indices to high, and
				// the index of all subsequent items changes when something before it is removed.
				if (mapBrush.NumSides - badSides.Length < 4)
				{
					DecompilerThread.OnMessage(this, "WARNING: Plane cull returned less than 4 sides for entity " + currentEntity + " brush " + numBrshs);
				}
				else
				{
					for (int i = badSides.Length - 1; i > - 1; i--)
					{
						mapBrush.delete(badSides[i]);
					}
				}
			}
		}
		
		if (!Settings.skipPlaneFlip)
		{
			if (mapBrush.hasBadSide())
			{
				// If there's a side that might be backward
				if (mapBrush.hasGoodSide())
				{
					// If there's a side that is forward
					mapBrush = MAPBrush.SimpleCorrectPlanes(mapBrush);
					numSimpleCorrects++;
					if (Settings.calcVerts)
					{
						// This is performed in advancedcorrect, so don't use it if that's happening
						try
						{
							mapBrush = MAPBrush.CalcBrushVertices(mapBrush);
						}
						catch (System.NullReferenceException)
						{
							DecompilerThread.OnMessage(this, "WARNING: Brush vertex calculation failed on entity " + mapBrush.Entnum + " brush " + mapBrush.Brushnum + "");
						}
					}
				}
				else
				{
					// If no forward side exists
					try
					{
						mapBrush = MAPBrush.AdvancedCorrectPlanes(mapBrush);
						numAdvancedCorrects++;
					}
					catch (System.ArithmeticException)
					{
						DecompilerThread.OnMessage(this, "WARNING: Plane correct returned 0 triangles for entity " + mapBrush.Entnum + " brush " + mapBrush.Brushnum + "");
					}
				}
			}
			else
			{
				numGoodBrushes++;
			}
		}
		else
		{
			if (Settings.calcVerts)
			{
				// This is performed in advancedcorrect, so don't use it if that's happening
				try
				{
					mapBrush = MAPBrush.CalcBrushVertices(mapBrush);
				}
				catch (System.NullReferenceException)
				{
					DecompilerThread.OnMessage(this, "WARNING: Brush vertex calculation failed on entity " + mapBrush.Entnum + " brush " + mapBrush.Brushnum + "");
				}
			}
		}
		
		// This adds the brush we've been finding and creating to
		// the current entity as an attribute. The way I've coded
		// this whole program and the entities parser, this shouldn't
		// cause any issues at all.
		if (!isVisBrush)
		{
			if (Settings.brushesToWorld)
			{
				mapBrush.Water = false;
				mapFile[0].Brushes.Add(mapBrush);
			}
			else
			{
				mapFile[currentEntity].Brushes.Add(mapBrush);
			}
		}
	}
}