using System;
// QuakeDecompiler
// Attempts to decompile a Quake BSP

public class QuakeDecompiler
{
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	public const int A = 0;
	public const int B = 1;
	public const int C = 2;
	
	public const int X = 0;
	public const int Y = 1;
	public const int Z = 2;
	
	private int jobnum;
	
	private Entities mapFile; // Most MAP file formats are simply a bunch of nested entities
	private int numBrshs;
	private int numSimpleCorrects = 0;
	private int numAdvancedCorrects = 0;
	private int numGoodBrushes = 0;
	private DecompilerThread parent;
	
	private BSP BSPObject;
	
	// CONSTRUCTORS
	
	// This constructor sets everything according to specified settings.
	public QuakeDecompiler(BSP BSPObject, int jobnum, DecompilerThread parent)
	{
		// Set up global variables
		this.BSPObject = BSPObject;
		this.jobnum = jobnum;
		this.parent = parent;
	}
	
	// METHODS
	
	// -decompile()
	// Attempts to convert the Quake/Half-life BSP file back into a .MAP file.
	public virtual Entities decompile()
	{
		DecompilerThread.OnMessage(this, "Decompiling...");
		// In the decompiler, it is not necessary to copy all entities to a new object, since
		// no writing is ever done back to the BSP file.
		mapFile = BSPObject.Entities;
		int numTotalItems = 0;
		int onePercent = (int)((BSPObject.Entities.Count)/100);
		// I need to go through each entity and see if it's brush-based.
		// Worldspawn is brush-based as well as any entity with model *#.
		for (int i = 0; i < BSPObject.Entities.Count; i++)
		{
			// For each entity
			DecompilerThread.OnMessage(this, "Entity " + i + ": " + mapFile[i]["classname"]);
			// getModelNumber() returns 0 for worldspawn, the *# for brush based entities, and -1 for everything else
			int currentModel = mapFile[i].ModelNumber;
			
			if (currentModel > - 1)
			{
				// If this is still -1 then it's strictly a point-based entity. Move on to the next one.
				Vector3D origin = mapFile[i].Origin;
				Model currentModelObject = BSPObject.Models[currentModel];
				int firstFace = currentModelObject.FirstFace;
				int numFaces = currentModelObject.NumFaces;
				for (int j = 0; j < numFaces; j++)
				{
					Face face = BSPObject.Faces[firstFace + j];
					// Turn vertices and edges into arrays of vectors
					Vector3D[] froms = new Vector3D[face.NumEdges];
					Vector3D[] tos = new Vector3D[face.NumEdges];
					for (int k = 0; k < face.NumEdges; k++)
					{
						if (BSPObject.SurfEdges[face.FirstEdge + k] > 0)
						{
							froms[k] = BSPObject.Vertices[BSPObject.Edges[(int) BSPObject.SurfEdges[face.FirstEdge + k]].FirstVertex].Vector;
							tos[k] = BSPObject.Vertices[BSPObject.Edges[(int) BSPObject.SurfEdges[face.FirstEdge + k]].SecondVertex].Vector;
						}
						else
						{
							tos[k] = BSPObject.Vertices[BSPObject.Edges[(int) BSPObject.SurfEdges[face.FirstEdge + k] * (- 1)].FirstVertex].Vector;
							froms[k] = BSPObject.Vertices[BSPObject.Edges[(int) BSPObject.SurfEdges[face.FirstEdge + k] * (- 1)].SecondVertex].Vector;
						}
					}
					
					TexInfo currentTexInfo = BSPObject.TexInfo[face.Texture];
					Texture currentTexture = BSPObject.Textures[currentTexInfo.Texture];
					string texture = currentTexture.Name;
					
					MAPBrush faceBrush = MAPBrush.createBrushFromWind(froms, tos, texture, "special/nodraw", currentTexInfo);
					mapFile[i].Brushes.Add(faceBrush);
				}
			}
			numTotalItems++;
			if(numTotalItems%onePercent == 0) {
				parent.OnProgress(this, numTotalItems/(double)(BSPObject.Brushes.Count));
			}
		}
		/*if(!Settings.skipPlaneFlip) {
		DecompilerThread.OnMessage(this, "Num simple corrected brushes: "+numSimpleCorrects,Settings.VERBOSITY_MAPSTATS); 
		DecompilerThread.OnMessage(this, "Num advanced corrected brushes: "+numAdvancedCorrects,Settings.VERBOSITY_MAPSTATS); 
		DecompilerThread.OnMessage(this, "Num good brushes: "+numGoodBrushes,Settings.VERBOSITY_MAPSTATS); 
		}*/
		parent.OnProgress(this, 1.0);
		return mapFile;
	}
}