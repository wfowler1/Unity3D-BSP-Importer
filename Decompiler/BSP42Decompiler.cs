using System;
// BSP42Decompiler
// Decompiles a Nightfire BSP

public class BSP42Decompiler {
	
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
	private DecompilerThread parent;
	
	private BSP BSPObject;
	
	// CONSTRUCTORS
	
	// This constructor sets everything according to specified settings.
	public BSP42Decompiler(BSP BSPObject, int jobnum, DecompilerThread parent)
	{
		// Set up global variables
		this.BSPObject = BSPObject;
		this.jobnum = jobnum;
		this.parent = parent;
	}
	
	// METHODS
	
	// -decompile()
	// Attempts to convert the Nightfire BSP file back into a .MAP file.
	//
	// This is another one of the most complex things I've ever had to code. I've
	// never nested for loops four deep before.
	// Iterators:
	// i: Current entity in the list
	//  j: Current leaf, referenced in a list by the model referenced by the current entity
	//   k: Current brush, referenced in a list by the current leaf.
	//	l: Current side of the current brush.
	//	 m: When attempting vertex decompilation, the current vertex.
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
			// getModelNumber() returns 0 for worldspawn, the *# for brush based entities, and -1 for everything else
			int currentModel = mapFile[i].ModelNumber;
			if (currentModel > - 1)
			{
				// If this is still -1 then it's strictly a point-based entity. Move on to the next one.
				int firstLeaf = BSPObject.Models[currentModel].FirstLeaf;
				int numLeaves = BSPObject.Models[currentModel].NumLeaves;
				bool[] brushesUsed = new bool[BSPObject.Brushes.Count]; // Keep a list of brushes already in the model, since sometimes the leaves lump references one brush several times
				numBrshs = 0;
				for (int j = 0; j < numLeaves; j++)
				{
					// For each leaf in the bunch
					Leaf currentLeaf = BSPObject.Leaves[j + firstLeaf];
					int firstBrushIndex = currentLeaf.FirstMarkBrush;
					int numBrushIndices = currentLeaf.NumMarkBrushes;
					if (numBrushIndices > 0)
					{
						// A lot of leaves reference no brushes. If this is one, this iteration of the j loop is finished
						for (int k = 0; k < numBrushIndices; k++)
						{
							// For each brush referenced
							if (!brushesUsed[(int) BSPObject.MarkBrushes[firstBrushIndex + k]])
							{
								// If the current brush has NOT been used in this entity
								//Console.Write("Brush " + numBrshs);
								brushesUsed[(int) BSPObject.MarkBrushes[firstBrushIndex + k]] = true;
								decompileBrush(BSPObject.Brushes[(int) BSPObject.MarkBrushes[firstBrushIndex + k]], i); // Decompile the brush
								numBrshs++;
								numTotalItems++;
								if(numTotalItems%onePercent == 0) {
									parent.OnProgress(this, numTotalItems/(double)(BSPObject.Brushes.Count + BSPObject.Entities.Count));
								}
							}
						}
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
	
	// -decompileBrush(Brush, int, boolean)
	// Decompiles the Brush and adds it to entitiy #currentEntity as .MAP data.
	private void  decompileBrush(Brush brush, int currentEntity)
	{
		Vector3D origin = mapFile[currentEntity].Origin;
		int firstSide = brush.FirstSide;
		int numSides = brush.NumSides;
		MAPBrushSide[] brushSides = new MAPBrushSide[0];
		bool isDetail = false;
		if (!Settings.noDetail && (brush.Contents[1] & ((sbyte) 1 << 1)) != 0)
		{
			isDetail = true;
		}
		MAPBrush mapBrush = new MAPBrush(numBrshs, currentEntity, isDetail);
		int numRealFaces = 0;
		Plane[] brushPlanes = new Plane[0];
		//DecompilerThread.OnMessage(this, ": " + numSides + " sides");
		if (mapFile[currentEntity]["classname"]=="func_water")
		{
			mapBrush.Water = true;
		}
		for (int l = 0; l < numSides; l++)
		{
			// For each side of the brush
			BrushSide currentSide = BSPObject.BrushSides[firstSide + l];
			Face currentFace = BSPObject.Faces[currentSide.Face]; // To find those three points, I can use vertices referenced by faces.
			string texture = BSPObject.Textures[currentFace.Texture].Name;
			if ((currentFace.Flags[1] & ((sbyte) 1 << 0)) == 0)
			{
				// Surfaceflags 512 + 256 + 32 are set only by the compiler, on faces that need to be thrown out.
				if (!texture.ToUpper().Equals("special/clip".ToUpper()) && !texture.ToUpper().Equals("special/playerclip".ToUpper()) && !texture.ToUpper().Equals("special/enemyclip".ToUpper()))
				{
					if (Settings.replaceWithNull && ((currentFace.Flags[1] & ((byte) 1 << 1)) != 0) && !texture.ToUpper().Equals("special/trigger".ToUpper()))
					{
						texture = "special/null";
						currentFace.Flags = new byte[4];
					}
				}
				int firstVertex = currentFace.FirstVertex;
				int numVertices = currentFace.NumVertices;
				Plane currentPlane;
				try
				{
					// I've only ever come across this error once or twice, but something causes it very rarely
					currentPlane = BSPObject.Planes[currentSide.Plane];
				}
				catch (System.IndexOutOfRangeException)
				{
					try
					{
						// So try to get the plane index from somewhere else
						currentPlane = BSPObject.Planes[currentFace.Plane];
					}
					catch (System.IndexOutOfRangeException f)
					{
						// If that fails, BS something
						DecompilerThread.OnMessage(this, "WARNING: BSP has error, references nonexistant plane " + currentSide.Plane + ", bad side " + (l) + " of brush " + numBrshs + " Entity " + currentEntity);
						currentPlane = new Plane((double) 1, (double) 0, (double) 0, (double) 0);
					}
				}
				Vector3D[] triangle = new Vector3D[0];
				bool pointsWorked = false;
				if (numVertices != 0 && !Settings.planarDecomp)
				{
					// If the face actually references a set of vertices
					triangle = new Vector3D[3]; // Three points define a plane. All I have to do is find three points on that plane.
					triangle[0] = new Vector3D(BSPObject.Vertices[firstVertex].Vector); // Grab and store the first one
					int m = 1;
					for (m = 1; m < numVertices; m++)
					{
						// For each point after the first one
						triangle[1] = new Vector3D(BSPObject.Vertices[firstVertex + m].Vector);
						if (triangle[0]!=triangle[1])
						{
							// Make sure the point isn't the same as the first one
							break; // If it isn't the same, this point is good
						}
					}
					for (m = m + 1; m < numVertices; m++)
					{
						// For each point after the previous one used
						triangle[2] = new Vector3D(BSPObject.Vertices[firstVertex + m].Vector);
						if (triangle[2]!=triangle[0] && triangle[2]!=triangle[1])
						{
							// Make sure no point is equal to the third one
							// Make sure all three points are non collinear
							Vector3D cr = (triangle[0]-triangle[1])^(triangle[0]-triangle[2]);
							if (cr.magnitude() > Settings.precision)
							{
								// vector length is never negative.
								pointsWorked = true;
								break;
							}
						}
					}
				}
				double[] textureU = new double[3];
				double[] textureV = new double[3];
				TexInfo currentTexInfo = BSPObject.TexInfo[currentFace.TextureScale];
				// Get the lengths of the axis vectors
				double SAxisLength = System.Math.Sqrt(System.Math.Pow((double) currentTexInfo.SAxis.X, 2) + System.Math.Pow((double) currentTexInfo.SAxis.Y, 2) + System.Math.Pow((double) currentTexInfo.SAxis.Z, 2));
				double TAxisLength = System.Math.Sqrt(System.Math.Pow((double) currentTexInfo.TAxis.X, 2) + System.Math.Pow((double) currentTexInfo.TAxis.Y, 2) + System.Math.Pow((double) currentTexInfo.TAxis.Z, 2));
				// In compiled maps, shorter vectors=longer textures and vice versa. This will convert their lengths back to 1. We'll use the actual scale values for length.
				double texScaleU = (1 / SAxisLength); // Let's use these values using the lengths of the U and V axes we found above.
				double texScaleV = (1 / TAxisLength);
				textureU[0] = ((double) currentTexInfo.SAxis.X / SAxisLength);
				textureU[1] = ((double) currentTexInfo.SAxis.Y / SAxisLength);
				textureU[2] = ((double) currentTexInfo.SAxis.Z / SAxisLength);
				double originShiftU = (textureU[0] * origin[X] + textureU[1] * origin[Y] + textureU[2] * origin[Z]) / texScaleU;
				double textureUhiftU = (double) currentTexInfo.SShift - originShiftU;
				textureV[0] = ((double) currentTexInfo.TAxis.X / TAxisLength);
				textureV[1] = ((double) currentTexInfo.TAxis.Y / TAxisLength);
				textureV[2] = ((double) currentTexInfo.TAxis.Z / TAxisLength);
				double originShiftV = (textureV[0] * origin[X] + textureV[1] * origin[Y] + textureV[2] * origin[Z]) / texScaleV;
				double textureUhiftV = (double) currentTexInfo.TShift - originShiftV;
				float texRot = 0; // In compiled maps this is calculated into the U and V axes, so set it to 0 until I can figure out a good way to determine a better value.
				int flags = DataReader.readInt(currentFace.Flags[0], currentFace.Flags[1], currentFace.Flags[2], currentFace.Flags[3]); // This is actually a set of flags. Whatever.
				string material;
				try
				{
					material = BSPObject.Materials[currentFace.Material].Name;
				}
				catch (System.IndexOutOfRangeException)
				{
					// In case the BSP has some strange error making it reference nonexistant materials
					DecompilerThread.OnMessage(this, "WARNING: Map referenced nonexistant material #" + currentFace.Material + ", using wld_lightmap instead!");
					material = "wld_lightmap";
				}
				double lgtScale = 16; // These values are impossible to get from a compiled map since they
				double lgtRot = 0; // are used by RAD for generating lightmaps, then are discarded, I believe.
				MAPBrushSide[] newList = new MAPBrushSide[brushSides.Length + 1];
				for (int i = 0; i < brushSides.Length; i++)
				{
					newList[i] = brushSides[i];
				}
				if (Settings.noFaceFlags)
				{
					flags = 0;
				}
				if (pointsWorked)
				{
					newList[brushSides.Length] = new MAPBrushSide(currentPlane, triangle, texture, textureU, textureUhiftU, textureV, textureUhiftV, texRot, texScaleU, texScaleV, flags, material, lgtScale, lgtRot);
				}
				else
				{
					newList[brushSides.Length] = new MAPBrushSide(currentPlane, texture, textureU, textureUhiftU, textureV, textureUhiftV, texRot, texScaleU, texScaleV, flags, material, lgtScale, lgtRot);
				}
				brushSides = newList;
				numRealFaces++;
			}
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