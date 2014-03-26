using System;
// BSP38Decompiler class
// Decompile BSP v38

public class BSP38Decompiler {
	
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
	public BSP38Decompiler(BSP BSPObject, int jobnum, DecompilerThread parent) {
		// Set up global variables
		this.BSPObject = BSPObject;
		this.jobnum = jobnum;
		this.parent = parent;
	}
	
	// METHODS
	
	// Attempt to turn the Quake 2 BSP into a .MAP file
	public virtual Entities decompile()
	{
		DecompilerThread.OnMessage(this, "Decompiling...");
		// In the decompiler, it is not necessary to copy all entities to a new object, since
		// no writing is ever done back to the BSP file.
		mapFile = BSPObject.Entities;
		//int numAreaPortals=0;
		int numTotalItems = 0;
		int onePercent = (int)((BSPObject.Brushes.Count + BSPObject.Entities.Count)/100);
		if(onePercent < 1) {
			onePercent = 1;
		}
		bool containsAreaPortals = false;
		for (int i = 0; i < BSPObject.Entities.Count; i++)
		{
			// For each entity
			//DecompilerThread.OnMessage(this, "Entity " + i + ": " + mapFile[i]["classname"]);
			// Deal with area portals.
			if (mapFile[i]["classname"].Equals("func_areaportal", StringComparison.CurrentCultureIgnoreCase))
			{
				mapFile[i].Attributes.Remove("style");
				containsAreaPortals = true;
			}
			// getModelNumber() returns 0 for worldspawn, the *# for brush based entities, and -1 for everything else
			int currentModel = mapFile[i].ModelNumber;
			if (currentModel > - 1)
			{
				// If this is still -1 then it's strictly a point-based entity. Move on to the next one.
				Leaf[] leaves = BSPObject.getLeavesInModel(currentModel);
				int numLeaves = leaves.Length;
				bool[] brushesUsed = new bool[BSPObject.Brushes.Count]; // Keep a list of brushes already in the model, since sometimes the leaves lump references one brush several times
				numBrshs = 0; // Reset the brush count for each entity
				for (int j = 0; j < numLeaves; j++)
				{
					// For each leaf in the bunch
					Leaf currentLeaf = leaves[j];
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
								Brush brush = BSPObject.Brushes[(int) BSPObject.MarkBrushes[firstBrushIndex + k]];
								if ((brush.Contents[1] & ((sbyte) 1 << 7)) == 0)
								{
									decompileBrush(brush, i); // Decompile the brush
								}
								else
								{
									containsAreaPortals = true;
								}
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
			numTotalItems++; // This entity
			if(numTotalItems%onePercent == 0) {
				parent.OnProgress(this, numTotalItems/(double)(BSPObject.Brushes.Count + BSPObject.Entities.Count));
			}
		}
		if (containsAreaPortals)
		{
			// If this map was found to have area portals
			int j = 0;
			for (int i = 0; i < BSPObject.Brushes.Count; i++)
			{
				// For each brush in this map
				if ((BSPObject.Brushes[i].Contents[1] & ((sbyte) 1 << 7)) != 0)
				{
					// If the brush is an area portal brush
					for (j++; j < BSPObject.Entities.Count; j++)
					{
						// Find an areaportal entity
						if (BSPObject.Entities[j]["classname"].Equals("func_areaportal", StringComparison.CurrentCultureIgnoreCase))
						{
							decompileBrush(BSPObject.Brushes[i], j); // Add the brush to that entity
							break; // And break out of the inner loop, but remember your place.
						}
					}
					if (j == BSPObject.Entities.Count)
					{
						// If we're out of entities, stop this whole thing.
						break;
					}
				}
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
	
	// -decompileBrush38(Brush, int, boolean)
	// Decompiles the Brush and adds it to entitiy #currentEntity as .MAP data.
	private void decompileBrush(Brush brush, int currentEntity)
	{
		Vector3D origin = mapFile[currentEntity].Origin;
		int firstSide = brush.FirstSide;
		int numSides = brush.NumSides;
		bool isDetail = false;
		MAPBrushSide[] brushSides = new MAPBrushSide[numSides];
		if (!Settings.noDetail && (brush.Contents[3] & ((sbyte) 1 << 3)) != 0)
		{
			// According to Q2's source, this is the detail flag
			isDetail = true;
		}
		MAPBrush mapBrush = new MAPBrush(numBrshs, currentEntity, isDetail);
		//DecompilerThread.OnMessage(this, ": " + numSides + " sides");
		if (!Settings.noWater && (brush.Contents[0] & ((sbyte) 1 << 5)) != 0)
		{
			mapBrush.Water = true;
		}
		for (int i = 0; i < numSides; i++)
		{
			// For each side of the brush
			Vector3D[] plane = new Vector3D[3]; // Three points define a plane. All I have to do is find three points on that plane.
			BrushSide currentSide = BSPObject.BrushSides[firstSide + i];
			Plane currentPlane = BSPObject.Planes[currentSide.Plane]; // To find those three points, I must extrapolate from planes until I find a way to associate faces with brushes
			Texture currentTexture;
			bool isDuplicate = false;
			for (int j = i + 1; j < numSides; j++)
			{
				// For each subsequent side of the brush
				// For some reason, QUAKE 2 MAKES COPLANAR SIDES OF BRUSHES. I don't know why but it's stupid.
				if (currentPlane.Equals(BSPObject.Planes[BSPObject.BrushSides[firstSide + j].Plane]))
				{
					DecompilerThread.OnMessage(this, "WARNING: Duplicate planes in entity " + currentEntity + " brush " + numBrshs + ", sides " + i + " and " + j + " (BSP planes " + currentSide.Plane + " and " + BSPObject.BrushSides[firstSide + j].Plane);
					isDuplicate = true;
				}
			}
			if (!isDuplicate)
			{
				/*
				if(!Settings.planarDecomp) {
				// Find a face whose plane and texture information corresponds to the current side
				// It doesn't really matter if it's the actual brush's face, just as long as it provides vertices.
				SiNFace currentFace=null;
				boolean faceFound=false;
				for(int j=0;j<BSP.getSFaces().size();j++) {
				currentFace=BSP.getSFaces().getFace(j);
				if(currentFace.getPlane()==currentSide.getPlane() && currentFace.getTexInfo()==currentSide.getTexInfo() && currentFace.getNumEdges()>1) {
				faceFound=true;
				break;
				}
				}
				if(faceFound) {
				int markEdge=BSP.getMarkEdges().getInt(currentFace.getFirstEdge());
				int currentMarkEdge=0;
				int firstVertex;
				int secondVertex;
				if(markEdge>0) {
				firstVertex=BSP.getEdges().getEdge(markEdge).getFirstVertex();
				secondVertex=BSP.getEdges().getEdge(markEdge).getSecondVertex();
				} else {
				firstVertex=BSP.getEdges().getEdge(-markEdge).getSecondVertex();
				secondVertex=BSP.getEdges().getEdge(-markEdge).getFirstVertex();
				}
				int numVertices=currentFace.getNumEdges()+1;
				boolean pointsWorked=false;
				plane[0]=new Vector3D(BSP.getVertices().getVertex(firstVertex)); // Grab and store the first one
				plane[1]=new Vector3D(BSP.getVertices().getVertex(secondVertex)); // The second should be unique from the first
				boolean second=false;
				if(plane[0].equals(plane[1])) { // If for some messed up reason they are the same
				for(currentMarkEdge=1;currentMarkEdge<currentFace.getNumEdges();currentMarkEdge++) { // For each edge after the first one
				markEdge=BSP.getMarkEdges().getInt(currentFace.getFirstEdge()+currentMarkEdge);
				if(markEdge>0) {
				plane[1]=new Vector3D(BSP.getVertices().getVertex(BSP.getEdges().getEdge(markEdge).getFirstVertex()));
				} else {
				plane[1]=new Vector3D(BSP.getVertices().getVertex(BSP.getEdges().getEdge(-markEdge).getSecondVertex()));
				}
				if(!plane[0].equals(plane[1])) { // Make sure the point isn't the same as the first one
				second=false;
				break; // If it isn't the same, this point is good
				} else {
				if(markEdge>0) {
				plane[1]=new Vector3D(BSP.getVertices().getVertex(BSP.getEdges().getEdge(markEdge).getSecondVertex()));
				} else {
				plane[1]=new Vector3D(BSP.getVertices().getVertex(BSP.getEdges().getEdge(-markEdge).getFirstVertex()));
				}
				if(!plane[0].equals(plane[1])) {
				second=true;
				break;
				}
				}
				}
				}
				if(second) {
				currentMarkEdge++;
				}
				for(;currentMarkEdge<currentFace.getNumEdges();currentMarkEdge++) {
				markEdge=BSP.getMarkEdges().getInt(currentFace.getFirstEdge()+currentMarkEdge);
				if(second) {
				if(markEdge>0) {
				plane[2]=new Vector3D(BSP.getVertices().getVertex(BSP.getEdges().getEdge(markEdge).getFirstVertex()));
				} else {
				plane[2]=new Vector3D(BSP.getVertices().getVertex(BSP.getEdges().getEdge(-markEdge).getSecondVertex()));
				}
				if(!plane[2].equals(plane[0]) && !plane[2].equals(plane[1])) { // Make sure no point is equal to the third one
				if((Vector3D.crossProduct(plane[0].subtract(plane[1]), plane[0].subtract(plane[2])).X!=0) || // Make sure all
				(Vector3D.crossProduct(plane[0].subtract(plane[1]), plane[0].subtract(plane[2])).Y!=0) || // three points 
				(Vector3D.crossProduct(plane[0].subtract(plane[1]), plane[0].subtract(plane[2])).Z!=0)) { // are not collinear
				pointsWorked=true;
				break;
				}
				}
				}
				// if we get to here, the first vertex of the edge failed, or was already used
				if(markEdge>0) { // use the second vertex
				plane[2]=new Vector3D(BSP.getVertices().getVertex(BSP.getEdges().getEdge(markEdge).getSecondVertex()));
				} else {
				plane[2]=new Vector3D(BSP.getVertices().getVertex(BSP.getEdges().getEdge(-markEdge).getFirstVertex()));
				}
				if(!plane[2].equals(plane[0]) && !plane[2].equals(plane[1])) { // Make sure no point is equal to the third one
				if((Vector3D.crossProduct(plane[0].subtract(plane[1]), plane[0].subtract(plane[2])).X!=0) || // Make sure all
				(Vector3D.crossProduct(plane[0].subtract(plane[1]), plane[0].subtract(plane[2])).Y!=0) || // three points 
				(Vector3D.crossProduct(plane[0].subtract(plane[1]), plane[0].subtract(plane[2])).Z!=0)) { // are not collinear
				pointsWorked=true;
				break;
				}
				}
				// If we get here, neither point worked and we need to try the next edge.
				second=true;
				}
				if(!pointsWorked) {
				plane=Plane.generatePlanePoints(currentPlane);
				}
				} else { // Face not found
				plane=Plane.generatePlanePoints(currentPlane);
				}
				} else { // Planar decomp only */
				plane = Plane.generatePlanePoints(currentPlane);
				// }
				string texture = "special/clip";
				double[] textureU = new double[3];
				double[] textureV = new double[3];
				double UShift = 0;
				double VShift = 0;
				double texScaleU = 1;
				double texScaleV = 1;
				if (currentSide.Texture > - 1)
				{
					currentTexture = BSPObject.Textures[currentSide.Texture];
					if ((currentTexture.Flags[0] & ((sbyte) 1 << 2)) != 0)
					{
						texture = "special/sky";
					}
					else
					{
						if ((currentTexture.Flags[1] & ((sbyte) 1 << 1)) != 0)
						{
							texture = "special/skip";
						}
						else
						{
							if ((currentTexture.Flags[1] & ((sbyte) 1 << 0)) != 0)
							{
								if (currentEntity == 0)
								{
									texture = "special/hint"; // Hint was not used the same way in Quake 2 as other games.
								}
								else
								{
									// For example, a Hint brush CAN be used for a trigger in Q2 and is used as such a lot.
									texture = "special/trigger";
								}
							}
							else
							{
								texture = currentTexture.Name;
							}
						}
					}
					// Get the lengths of the axis vectors
					double SAxisLength = System.Math.Sqrt(System.Math.Pow((double) currentTexture.TexAxes.SAxis.X, 2) + System.Math.Pow((double) currentTexture.TexAxes.SAxis.Y, 2) + System.Math.Pow((double) currentTexture.TexAxes.SAxis.Z, 2));
					double TAxisLength = System.Math.Sqrt(System.Math.Pow((double) currentTexture.TexAxes.TAxis.X, 2) + System.Math.Pow((double) currentTexture.TexAxes.TAxis.Y, 2) + System.Math.Pow((double) currentTexture.TexAxes.TAxis.Z, 2));
					// In compiled maps, shorter vectors=longer textures and vice versa. This will convert their lengths back to 1. We'll use the actual scale values for length.
					texScaleU = (1 / SAxisLength); // Let's use these values using the lengths of the U and V axes we found above.
					texScaleV = (1 / TAxisLength);
					textureU[0] = ((double) currentTexture.TexAxes.SAxis.X / SAxisLength);
					textureU[1] = ((double) currentTexture.TexAxes.SAxis.Y / SAxisLength);
					textureU[2] = ((double) currentTexture.TexAxes.SAxis.Z / SAxisLength);
					textureV[0] = ((double) currentTexture.TexAxes.TAxis.X / TAxisLength);
					textureV[1] = ((double) currentTexture.TexAxes.TAxis.Y / TAxisLength);
					textureV[2] = ((double) currentTexture.TexAxes.TAxis.Z / TAxisLength);
					UShift = (double) currentTexture.TexAxes.SShift;
					VShift = (double) currentTexture.TexAxes.TShift;
				}
				else
				{
					Vector3D[] axes = TexInfo.textureAxisFromPlane(currentPlane);
					textureU = axes[0].Point;
					textureV = axes[1].Point;
				}
				double originShiftU = (textureU[0] * origin[X] + textureU[1] * origin[Y] + textureU[2] * origin[Z]) / texScaleU;
				double textureShiftU = UShift - originShiftU;
				double originShiftV = (textureV[0] * origin[X] + textureV[1] * origin[Y] + textureV[2] * origin[Z]) / texScaleV;
				double textureShiftV = VShift - originShiftV;
				float texRot = 0; // In compiled maps this is calculated into the U and V axes, so set it to 0 until I can figure out a good way to determine a better value.
				int flags = 0; // Set this to 0 until we can somehow associate faces with brushes
				string material = "wld_lightmap"; // Since materials are a NightFire only thing, set this to a good default
				double lgtScale = 16; // These values are impossible to get from a compiled map since they
				double lgtRot = 0; // are used by RAD for generating lightmaps, then are discarded, I believe.
				brushSides[i] = new MAPBrushSide(plane, texture, textureU, textureShiftU, textureV, textureShiftV, texRot, texScaleU, texScaleV, flags, material, lgtScale, lgtRot);
				mapBrush.add(brushSides[i]);
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