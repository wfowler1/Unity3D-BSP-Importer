using System;
// SourceBSPDecompiler class
// Decompile BSP v38

public class SourceBSPDecompiler {
	
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
	public SourceBSPDecompiler(BSP BSPObject, int jobnum, DecompilerThread parent) {
		// Set up global variables
		this.BSPObject = BSPObject;
		this.jobnum = jobnum;
		this.parent = parent;
	}
	
	// METHODS
	
	// Attempt to turn the BSP into a .MAP file
	public virtual Entities decompile() {
		DecompilerThread.OnMessage(this, "Decompiling...");
		// In the decompiler, it is not necessary to copy all entities to a new object, since
		// no writing is ever done back to the BSP file.
		mapFile = BSPObject.Entities;
		//int numAreaPortals=0;
		int numTotalItems = 0;
		int onePercent = (int)((BSPObject.Brushes.Count + BSPObject.Entities.Count)/100);
		int originalNumEntities = BSPObject.Entities.Count; // Need to keep track of this in this algorithm, since I create more entities on the fly
		for (int i = 0; i < originalNumEntities; i++) {
			// For each entity
			//DecompilerThread.OnMessage(this, "Entity " + i + ": " + mapFile[i]["classname"]);
			// getModelNumber() returns 0 for worldspawn, the *# for brush based entities, and -1 for everything else
			int currentModel = mapFile[i].ModelNumber;
			if (currentModel > - 1) { // If this is still -1 then it's strictly a point-based entity. Move on to the next one.
				Leaf[] leaves = BSPObject.getLeavesInModel(currentModel);
				int numLeaves = leaves.Length;
				bool[] brushesUsed = new bool[BSPObject.Brushes.Count]; // Keep a list of brushes already in the model, since sometimes the leaves lump references one brush several times
				numBrshs = 0; // Reset the brush count for each entity
				for (int j = 0; j < numLeaves; j++) {
					// For each leaf in the bunch
					Leaf currentLeaf = leaves[j];
					int firstMarkBrushIndex = currentLeaf.FirstMarkBrush;
					int numBrushIndices = currentLeaf.NumMarkBrushes;
					if (numBrushIndices > 0) {
						// A lot of leaves reference no brushes. If this is one, this iteration of the j loop is finished
						for (int k = 0; k < numBrushIndices; k++) {
							// For each brush referenced
							long currentBrushIndex = BSPObject.MarkBrushes[firstMarkBrushIndex + k];
							if (!brushesUsed[(int) currentBrushIndex]) {
								// If the current brush has NOT been used in this entity
								//Console.Write("Brush " + numBrshs);
								brushesUsed[(int) currentBrushIndex] = true;
								Brush brush = BSPObject.Brushes[(int) currentBrushIndex];
								decompileBrush(brush, i); // Decompile the brush
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
		// Find displacement faces and generate brushes for them
		for (int i = 0; i < BSPObject.Faces.Count; i++) {
			Face face = BSPObject.Faces[i];
			if (face.Displacement > - 1)
			{
				SourceDispInfo disp = BSPObject.DispInfos[face.Displacement];
				TexInfo currentTexInfo;
				if (face.Texture > - 1)
				{
					currentTexInfo = BSPObject.TexInfo[face.Texture];
				}
				else
				{
					Vector3D[] axes = TexInfo.textureAxisFromPlane(BSPObject.Planes[face.Plane]);
					currentTexInfo = new TexInfo(axes[0], 0, axes[1], 0, 0, BSPObject.findTexDataWithTexture("tools/toolsclip"));
				}
				SourceTexData currentTexData = BSPObject.TexDatas[currentTexInfo.Texture];
				string texture = BSPObject.Textures.getTextureAtOffset((uint)BSPObject.TexTable[currentTexData.StringTableIndex]);
				double[] textureU = new double[3];
				double[] textureV = new double[3];
				// Get the lengths of the axis vectors
				double SAxisLength = System.Math.Sqrt(System.Math.Pow((double) currentTexInfo.SAxis.X, 2) + System.Math.Pow((double) currentTexInfo.SAxis.Y, 2) + System.Math.Pow((double) currentTexInfo.SAxis.Z, 2));
				double TAxisLength = System.Math.Sqrt(System.Math.Pow((double) currentTexInfo.TAxis.X, 2) + System.Math.Pow((double) currentTexInfo.TAxis.Y, 2) + System.Math.Pow((double) currentTexInfo.TAxis.Z, 2));
				// In compiled maps, shorter vectors=longer textures and vice versa. This will convert their lengths back to 1. We'll use the actual scale values for length.
				double texScaleU = (1 / SAxisLength); // Let's use these values using the lengths of the U and V axes we found above.
				double texScaleV = (1 / TAxisLength);
				textureU[0] = ((double) currentTexInfo.SAxis.X / SAxisLength);
				textureU[1] = ((double) currentTexInfo.SAxis.Y / SAxisLength);
				textureU[2] = ((double) currentTexInfo.SAxis.Z / SAxisLength);
				//UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextSettingsIndex'&keyword='jlca1042'"
				double textureShiftU = (double) currentTexInfo.SShift;
				textureV[0] = ((double) currentTexInfo.TAxis.X / TAxisLength);
				textureV[1] = ((double) currentTexInfo.TAxis.Y / TAxisLength);
				textureV[2] = ((double) currentTexInfo.TAxis.Z / TAxisLength);
				//UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextSettingsIndex'&keyword='jlca1042'"
				double textureShiftV = (double) currentTexInfo.TShift;
				
				if (face.NumEdges != 4)
				{
					DecompilerThread.OnMessage(this, "Displacement face with " + face.NumEdges + " edges!");
				}
				
				// Turn vertices and edges into arrays of vectors
				Vector3D[] froms = new Vector3D[face.NumEdges];
				Vector3D[] tos = new Vector3D[face.NumEdges];
				for (int j = 0; j < face.NumEdges; j++)
				{
					if (BSPObject.SurfEdges[face.FirstEdge + j] > 0)
					{
						froms[j] = BSPObject.Vertices[BSPObject.Edges[(int)BSPObject.SurfEdges[face.FirstEdge + j]].FirstVertex].Vector;
						tos[j] = BSPObject.Vertices[BSPObject.Edges[(int)BSPObject.SurfEdges[face.FirstEdge + j]].SecondVertex].Vector;
					}
					else
					{
						tos[j] = BSPObject.Vertices[BSPObject.Edges[(int) BSPObject.SurfEdges[face.FirstEdge + j] * (- 1)].FirstVertex].Vector;
						froms[j] = BSPObject.Vertices[BSPObject.Edges[(int) BSPObject.SurfEdges[face.FirstEdge + j] * (- 1)].SecondVertex].Vector;
					}
				}
				
				MAPBrush displacementBrush = MAPBrush.createBrushFromWind(froms, tos, texture, "TOOLS/TOOLSNODRAW", currentTexInfo);
				
				MAPDisplacement mapdisp = new MAPDisplacement(disp, BSPObject.DispVerts.getVertsInDisp(disp.DispVertStart, disp.Power));
				displacementBrush[0].Displacement = mapdisp;
				mapFile[0].Brushes.Add(displacementBrush);
			}
		}
		for (int i = 0; i < BSPObject.StaticProps.Count; i++)
		{
			Entity newStaticProp = new Entity("prop_static");
			SourceStaticProp currentProp = BSPObject.StaticProps[i];
			newStaticProp["model"] = BSPObject.StaticProps.Dictionary[currentProp.DictionaryEntry];
			newStaticProp["skin"] = currentProp.Skin + "";
			newStaticProp["origin"] = currentProp.Origin.X + " " + currentProp.Origin.Y + " " + currentProp.Origin.Z;
			newStaticProp["angles"] = currentProp.Angles.X + " " + currentProp.Angles.Y + " " + currentProp.Angles.Z;
			newStaticProp["solid"] = currentProp.Solidity + "";
			newStaticProp["fademindist"] = currentProp.MinFadeDist + "";
			newStaticProp["fademaxdist"] = currentProp.MaxFadeDist + "";
			newStaticProp["fadescale"] = currentProp.ForcedFadeScale + "";
			if (currentProp.Targetname != null)
			{
				newStaticProp["targetname"] = currentProp.Targetname;
			}
			mapFile.Add(newStaticProp);
		}
		for (int i = 0; i < BSPObject.Cubemaps.Count; i++)
		{
			Entity newCubemap = new Entity("env_cubemap");
			SourceCubemap currentCube = BSPObject.Cubemaps[i];
			newCubemap["origin"] = currentCube.Origin.X + " " + currentCube.Origin.Y + " " + currentCube.Origin.Z;
			newCubemap["cubemapsize"] = currentCube.Size + "";
			mapFile.Add(newCubemap);
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
		MAPBrushSide[] brushSides = new MAPBrushSide[numSides];
		bool isDetail = false;
		if (currentEntity == 0 && !Settings.noDetail && (brush.Contents[3] & ((sbyte) 1 << 3)) != 0)
		{
			isDetail = true;
		}
		MAPBrush mapBrush = new MAPBrush(numBrshs, currentEntity, isDetail);
		if (currentEntity == 0 && !Settings.noWater && (brush.Contents[0] & ((sbyte) 1 << 5)) != 0)
		{
			mapBrush.Water = true;
		}
		//DecompilerThread.OnMessage(this, ": " + numSides + " sides, detail: " + isDetail);
		for (int i = 0; i < numSides; i++)
		{
			// For each side of the brush
			BrushSide currentSide = BSPObject.BrushSides[firstSide + i];
			if (currentSide.isBevel() == 0)
			{
				// Bevel sides are evil
				Vector3D[] plane = new Vector3D[3]; // Three points define a plane. All I have to do is find three points on that plane.
				Plane currentPlane = BSPObject.Planes[currentSide.Plane]; // To find those three points, I must extrapolate from planes until I find a way to associate faces with brushes
				bool isDuplicate = false; /* TODO: We sure don't want duplicate planes (though this is already handled by the MAPBrush class). Make sure neither checked side is bevel.
				for(int j=i+1;j<numSides;j++) { // For each subsequent side of the brush
				if(currentPlane.equals(BSPObject.Planes.getPlane(BSPObject.getBrushSides()[firstSide+j).getPlane()))) {
				DecompilerThread.OnMessage(this, "WARNING: Duplicate planes in a brush, sides "+i+" and "+j,Settings.VERBOSITY_WARNINGS);
				isDuplicate=true;
				}
				}*/
				if (!isDuplicate)
				{
					TexInfo currentTexInfo = null;
					string texture = "tools/toolsclip";
					if (currentSide.Texture > - 1)
					{
						currentTexInfo = BSPObject.TexInfo[currentSide.Texture];
					}
					else
					{
						int dataIndex = BSPObject.findTexDataWithTexture("tools/toolsclip");
						if (dataIndex >= 0)
						{
							currentTexInfo = new TexInfo(new Vector3D(0, 0, 0), 0, new Vector3D(0, 0, 0), 0, 0, dataIndex);
						}
					}
					if (currentTexInfo != null)
					{
						SourceTexData currentTexData;
						if (currentTexInfo.Texture >= 0)
						{
							// I've only found one case where this is a problem: c2a3a in HL Source. Don't know why.
							currentTexData = BSPObject.TexDatas[currentTexInfo.Texture];
							texture = BSPObject.Textures.getTextureAtOffset((uint)BSPObject.TexTable[currentTexData.StringTableIndex]);
						}
						else
						{
							texture = "tools/toolsskip";
						}
					}
					double[] textureU = new double[3];
					double[] textureV = new double[3];
					double textureShiftU = 0;
					double textureShiftV = 0;
					double texScaleU = 1;
					double texScaleV = 1;
					// Get the lengths of the axis vectors
					if ((texture.Length > 6 && texture.Substring(0, (6) - (0)).ToUpper().Equals("tools/".ToUpper())) || currentTexInfo == null)
					{
						// Tools textured faces do not maintain their own texture axes. Therefore, an arbitrary axis is
						// used in the compiled map. When decompiled, these axes might smear the texture on the face. Fix that.
						Vector3D[] axes = TexInfo.textureAxisFromPlane(currentPlane);
						textureU = axes[0].Point;
						textureV = axes[1].Point;
					}
					else
					{
						double SAxisLength = System.Math.Sqrt(System.Math.Pow((double) currentTexInfo.SAxis.X, 2) + System.Math.Pow((double) currentTexInfo.SAxis.Y, 2) + System.Math.Pow((double) currentTexInfo.SAxis.Z, 2));
						double TAxisLength = System.Math.Sqrt(System.Math.Pow((double) currentTexInfo.TAxis.X, 2) + System.Math.Pow((double) currentTexInfo.TAxis.Y, 2) + System.Math.Pow((double) currentTexInfo.TAxis.Z, 2));
						// In compiled maps, shorter vectors=longer textures and vice versa. This will convert their lengths back to 1. We'll use the actual scale values for length.
						texScaleU = (1 / SAxisLength); // Let's use these values using the lengths of the U and V axes we found above.
						texScaleV = (1 / TAxisLength);
						textureU[0] = ((double) currentTexInfo.SAxis.X / SAxisLength);
						textureU[1] = ((double) currentTexInfo.SAxis.Y / SAxisLength);
						textureU[2] = ((double) currentTexInfo.SAxis.Z / SAxisLength);
						double originShiftU = (((double) currentTexInfo.SAxis.X / SAxisLength) * origin[X] + ((double) currentTexInfo.SAxis.Y / SAxisLength) * origin[Y] + ((double) currentTexInfo.SAxis.Z / SAxisLength) * origin[Z]) / texScaleU;
						//UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextSettingsIndex'&keyword='jlca1042'"
						textureShiftU = (double) currentTexInfo.SShift - originShiftU;
						textureV[0] = ((double) currentTexInfo.TAxis.X / TAxisLength);
						textureV[1] = ((double) currentTexInfo.TAxis.Y / TAxisLength);
						textureV[2] = ((double) currentTexInfo.TAxis.Z / TAxisLength);
						double originShiftV = (((double) currentTexInfo.TAxis.X / TAxisLength) * origin[X] + ((double) currentTexInfo.TAxis.Y / TAxisLength) * origin[Y] + ((double) currentTexInfo.TAxis.Z / TAxisLength) * origin[Z]) / texScaleV;
						//UPGRADE_WARNING: Data types in Visual C# might be different.  Verify the accuracy of narrowing conversions. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextSettingsIndex'&keyword='jlca1042'"
						textureShiftV = (double) currentTexInfo.TShift - originShiftV;
					}
					float texRot = 0; // In compiled maps this is calculated into the U and V axes, so set it to 0 until I can figure out a good way to determine a better value.
					int flags = 0; // Set this to 0 until we can somehow associate faces with brushes
					string material = "wld_lightmap"; // Since materials are a NightFire only thing, set this to a good default
					double lgtScale = 16; // These values are impossible to get from a compiled map since they
					double lgtRot = 0; // are used by RAD for generating lightmaps, then are discarded, I believe.
					brushSides[i] = new MAPBrushSide(currentPlane, texture, textureU, textureShiftU, textureV, textureShiftV, texRot, texScaleU, texScaleV, flags, material, lgtScale, lgtRot);
					mapBrush.add(brushSides[i]);
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
	
	public virtual TexInfo createPerpTexInfo(Plane in_Renamed)
	{
		Vector3D[] axes = TexInfo.textureAxisFromPlane(in_Renamed);
		return new TexInfo(axes[0], 0, axes[1], 0, 0, BSPObject.findTexDataWithTexture("tools/toolsclip"));
	}
}