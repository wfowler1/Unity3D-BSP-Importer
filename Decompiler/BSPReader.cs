using System.IO;
using System.Collections.Generic;
// BSPReader class

// Does the actual reading of the BSP file and takes appropriate
// action based primarily on BSP version number. It also feeds all
// appropriate data to the different BSP version classes. This
// does not actually do any data processing or analysis, it simply
// reads from the hard drive and sends the data where it needs to go.
// Deprecates the LS class, and doesn't create a file for every lump!
using System;

public class BSPReader {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	private FileInfo BSPFile;
	private FileStream stream;
	private BinaryReader br;
	public const int OFFSET = 0;
	public const int LENGTH = 1;
	// These are only used in Source BSPs, which have a lot of different structures
	public const int LUMPVERSION = 2;
	public const int FOURCC = 3;
	
	private int lumpOffset = 0;
	private int lumpLength = 0;

	private mapType version = mapType.TYPE_UNDEFINED;
	private bool bigEndian = false;
	private mapType readAs = mapType.TYPE_UNDEFINED; // An override for version number detection, in case the user wishes to use a specific algorithm
	// Left 4 Dead 2, for some reason, made the order "version, offset, length" for lump header structure,
	// rather than the usual "offset, length, version". I guess someone at Valve got bored.
	private bool isL4D2 = false;
	
	private bool wad = false;
	
	// Declare all kinds of maps here, the one actually used will be determined by constructor
	private List<DoomMap> doomMaps = new List<DoomMap>();
	private BSP BSPObject;
	
	// Decryption key for Tactical Intervention; should be 32 bytes
	public byte[] key;
	
	// CONSTRUCTORS
	
	// Takes a String in and assumes it is a path. That path is the path to the file
	// that is the BSP and its name minus the .BSP extension is assumed to be the folder.
	// See comments below for clarification. Case does not matter on the extension, so it
	// could be .BSP, .bsp, etc.
	
	public BSPReader(string path, mapType readAs) : this(new FileInfo(path), readAs) {
	}
	
	public BSPReader(FileInfo file, mapType readAs) {
		this.readAs = readAs;
		BSPFile = file;
		if (!File.Exists(BSPFile.FullName)) {
			DecompilerThread.OnMessage(this, "Unable to open BSP file; file "+BSPFile.FullName+" not found.");
		} else {
			this.stream = new FileStream(BSPFile.FullName, FileMode.Open, FileAccess.Read);
			this.br = new BinaryReader(this.stream);
		}
	}
	
	// METHODS

	public void readBSP() {
		try {
			version = Version;
			byte[] theLump = new byte[0];
			BSPObject = new BSP(BSPFile.FullName, version);
			switch(version) {
				case mapType.TYPE_VINDICTUS:
				case mapType.TYPE_TACTICALINTERVENTION:
				case mapType.TYPE_SOURCE17:
				case mapType.TYPE_SOURCE18:
				case mapType.TYPE_SOURCE19:
				case mapType.TYPE_SOURCE20:
				case mapType.TYPE_SOURCE21:
				case mapType.TYPE_SOURCE22:
				case mapType.TYPE_SOURCE23:
				case mapType.TYPE_SOURCE27:
				case mapType.TYPE_DMOMAM:
					DecompilerThread.OnMessage(this, "Source BSP");
					stream.Seek(8, SeekOrigin.Begin);
					int test = br.ReadInt32();
					if(bigEndian) { test = DataReader.swapEndian(test); }
					if(test < 1032) { // If what's usually the offset is less than the length of the header and directory
						isL4D2 = true; // This is Left 4 Dead 2
					}
					
					// Lump 35, Game lump
					// Need to handle this here in order to detect Vindictus maps.
					// This lump SUCKS. It's a lump containing nested lumps for game specific data.
					// What we need out of it is the static prop lump.
					theLump = readLumpNum(35);
					try {
						readGameLump(theLump, lumpOffset);
					} catch {
						dumpLump(theLump);
					}

					for(int i=0;i<64;i++) {
						try {
							switch(i) {
								case 0:
									theLump = readLumpNum(i);
									BSPObject.Entities = Entity.createLump(theLump);
									break;
								case 1:
									theLump = readLumpNum(i);
									BSPObject.Planes = Plane.createLump(theLump, version);
									break;
								case 2:
									theLump = readLumpNum(i);
									BSPObject.TexDatas = SourceTexData.createLump(theLump);
									break;
								case 3:
									theLump = readLumpNum(i);
									BSPObject.Vertices = Vertex.createLump(theLump, version);
									break;
								case 5:
									theLump = readLumpNum(i);
									BSPObject.Nodes = Node.createLump(theLump, version);
									break;
								case 6:
									theLump = readLumpNum(i);
									BSPObject.TexInfo = TexInfo.createLump(theLump, version);
									break;
								case 7:
									theLump = readLumpNum(i);
									BSPObject.Faces = Face.createLump(theLump, version);
									break;
								case 10:
									theLump = readLumpNum(i);
									BSPObject.Leaves = Leaf.createLump(theLump, version);
									break;
								case 12:
									theLump = readLumpNum(i);
									BSPObject.Edges = Edge.createLump(theLump, version);
									break;
								case 13:
									theLump = readLumpNum(i);
									BSPObject.SurfEdges = new NumList(theLump, NumList.dataType.INT);
									break;
								case 14:
									theLump = readLumpNum(i);
									BSPObject.Models = Model.createLump(theLump, version);
									break;
								case 17:
									theLump = readLumpNum(i);
									if(version == mapType.TYPE_VINDICTUS) {
										BSPObject.MarkBrushes = new NumList(theLump, NumList.dataType.UINT);
									} else {
										BSPObject.MarkBrushes = new NumList(theLump, NumList.dataType.USHORT);
									}
									break;
								case 18:
									theLump = readLumpNum(i);
									BSPObject.Brushes = Brush.createLump(theLump, version);
									break;
								case 19:
									theLump = readLumpNum(i);
									BSPObject.BrushSides = BrushSide.createLump(theLump, version);
									break;
								case 26:
									theLump = readLumpNum(i);
									BSPObject.DispInfos = SourceDispInfo.createLump(theLump, version);
									break;
								case 27:
									theLump = readLumpNum(i);
									BSPObject.OriginalFaces = Face.createLump(theLump, version);
									break;
								case 33:
									theLump = readLumpNum(i);
									BSPObject.DispVerts = SourceDispVertex.createLump(theLump);
									break;
								case 40:
									theLump = readLumpNum(i);
									if (Settings.extractZip) {
										Console.Write("Extracting internal PAK file... ");
										writeLump(BSPObject.MapName+".zip", theLump);
									}
									break;
								case 42:
									theLump = readLumpNum(i);
									BSPObject.Cubemaps = SourceCubemap.createLump(theLump, version);
									break;
								case 43:
									theLump = readLumpNum(i);
									BSPObject.Textures = Texture.createLump(theLump, version);
									break;
								case 44:
									theLump = readLumpNum(i);
									BSPObject.TexTable = new NumList(theLump, NumList.dataType.INT);
									break;
								case 48:
									theLump = readLumpNum(i);
									BSPObject.DispTris = new NumList(theLump, NumList.dataType.USHORT);
									break;
							}
						} catch {
							dumpLump(theLump);
						}
					}
					break;
				case mapType.TYPE_NIGHTFIRE:
					DecompilerThread.OnMessage(this, "BSP v42 (Nightfire)");
					for(int i=0;i<18;i++) {
						try {
							switch(i) {
								case 0:
									theLump = readLumpNum(i);
									BSPObject.Entities = Entity.createLump(theLump);
									break;
								case 1:
									theLump = readLumpNum(i);
									BSPObject.Planes = Plane.createLump(theLump, version);
									break;
								case 2:
									theLump = readLumpNum(i);
									BSPObject.Textures = Texture.createLump(theLump, version);
									break;
								case 3:
									theLump = readLumpNum(i);
									BSPObject.Materials = Texture.createLump(theLump, version);
									break;
								case 4:
									theLump = readLumpNum(i);
									BSPObject.Vertices = Vertex.createLump(theLump, version);
									break;
								case 9:
									theLump = readLumpNum(i);
									BSPObject.Faces = Face.createLump(theLump, version);
									break;
								case 11:
									theLump = readLumpNum(i);
									BSPObject.Leaves = Leaf.createLump(theLump, version);
									break;
								case 13:
									theLump = readLumpNum(i);
									BSPObject.MarkBrushes = new NumList(theLump, NumList.dataType.UINT);
									break;
								case 14:
									theLump = readLumpNum(i);
									BSPObject.Models = Model.createLump(theLump, version);
									break;
								case 15:
									theLump = readLumpNum(i);
									BSPObject.Brushes = Brush.createLump(theLump, version);
									break;
								case 16:
									theLump = readLumpNum(i);
									BSPObject.BrushSides = BrushSide.createLump(theLump, version);
									break;
								case 17:
									theLump = readLumpNum(i);
									BSPObject.TexInfo = TexInfo.createLump(theLump, version);
									break;
							}
						} catch {
							dumpLump(theLump);
						}
					}
					break;
				case mapType.TYPE_QUAKE2:
				case mapType.TYPE_SIN: 
				case mapType.TYPE_DAIKATANA: 
				case mapType.TYPE_SOF:
					DecompilerThread.OnMessage(this, "BSP v38 (Quake 2/SiN/Daikatana/Soldier of Fortune)");
					for(int i=0;i<19;i++) {
						try {
							switch(i) {
								case 0:
									theLump = readLumpNum(i);
									BSPObject.Entities = Entity.createLump(theLump);
									break;
								case 1:
									theLump = readLumpNum(i);
									BSPObject.Planes = Plane.createLump(theLump, version);
									break;
								case 2:
									theLump = readLumpNum(i);
									BSPObject.Vertices = Vertex.createLump(theLump, version);
									break;
								case 4:
									theLump = readLumpNum(i);
									BSPObject.Nodes = Node.createLump(theLump, version);
									break;
								case 5:
									theLump = readLumpNum(i);
									BSPObject.Textures = Texture.createLump(theLump, version);
									break;
								case 6:
									theLump = readLumpNum(i);
									BSPObject.Faces = Face.createLump(theLump, version);
									break;
								case 8:
									theLump = readLumpNum(i);
									BSPObject.Leaves = Leaf.createLump(theLump, version);
									break;
								case 10:
									theLump = readLumpNum(i);
									BSPObject.MarkBrushes = new NumList(theLump, NumList.dataType.USHORT);
									break;
								case 11:
									theLump = readLumpNum(i);
									BSPObject.Edges = Edge.createLump(theLump, version);
									break;
								case 12:
									theLump = readLumpNum(i);
									BSPObject.SurfEdges = new NumList(theLump, NumList.dataType.INT);
									break;
								case 13:
									theLump = readLumpNum(i);
									BSPObject.Models = Model.createLump(theLump, version);
									break;
								case 14:
									theLump = readLumpNum(i);
									BSPObject.Brushes = Brush.createLump(theLump, version);
									break;
								case 15:
									theLump = readLumpNum(i);
									BSPObject.BrushSides = BrushSide.createLump(theLump, version);
									break;
								/*case 18:
									theLump = readLumpNum(i);
									BSPObject.AreaPortals = AreaPortal.createLump(theLump, version);
									break;*/
							}
						} catch {
							dumpLump(theLump);
						}
					}
					break;
				case mapType.TYPE_QUAKE:
					DecompilerThread.OnMessage(this, "Quake 1/Half-life BSP");
					for (int i = 0; i < 15; i++) {
						try {
							switch (i) {
								case 0:
									theLump = readLumpNum(i);
									BSPObject.Entities = Entity.createLump(theLump);
									break;
								case 1:
									theLump = readLumpNum(i);
									BSPObject.Planes = Plane.createLump(theLump, version);
									break;
								case 2:
									theLump = readLumpNum(i);
									BSPObject.Textures = Texture.createLump(theLump, version);
									break;
								case 3:
									theLump = readLumpNum(i);
									BSPObject.Vertices = Vertex.createLump(theLump, version);
									break;
								case 5:
									theLump = readLumpNum(i);
									BSPObject.Nodes = Node.createLump(theLump, version);
									break;
								case 6:
									theLump = readLumpNum(i);
									BSPObject.TexInfo = TexInfo.createLump(theLump, version);
									break;
								case 7:
									theLump = readLumpNum(i);
									BSPObject.Faces = Face.createLump(theLump, version);
									break;
								case 10:
									theLump = readLumpNum(i);
									BSPObject.Leaves = Leaf.createLump(theLump, version);
									break;
								case 11:
									theLump = readLumpNum(i);
									BSPObject.MarkSurfaces = new NumList(theLump, NumList.dataType.USHORT);
									break;
								case 12:
									theLump = readLumpNum(i);
									BSPObject.Edges = Edge.createLump(theLump, version);
									break;
								case 13:
									theLump = readLumpNum(i);
									BSPObject.SurfEdges = new NumList(theLump, NumList.dataType.INT);
									break;
								case 14:
									theLump = readLumpNum(i);
									BSPObject.Models = Model.createLump(theLump, version);
									break;
							}
						}
						catch
						{
							dumpLump(theLump);
						}
					}
					break;
				case mapType.TYPE_STEF2:
				case mapType.TYPE_STEF2DEMO:
					DecompilerThread.OnMessage(this, "Star Trek Elite Force 2 BSP");
					for (int i = 0; i < 30; i++) {
						try {
							switch (i) {
								case 0:
									theLump = readLumpNum(i);
									BSPObject.Textures = Texture.createLump(theLump, version);
									break;
								case 1:
									theLump = readLumpNum(i);
									BSPObject.Planes = Plane.createLump(theLump, version);
									break;
								case 5:
									theLump = readLumpNum(i);
									BSPObject.Faces = Face.createLump(theLump, version);
									break;
								case 6:
									theLump = readLumpNum(i);
									BSPObject.Vertices = Vertex.createLump(theLump, version);
									break;
								case 12:
									theLump = readLumpNum(i);
									BSPObject.BrushSides = BrushSide.createLump(theLump, version);
									break;
								case 13:
									theLump = readLumpNum(i);
									BSPObject.Brushes = Brush.createLump(theLump, version);
									break;
								case 15:
									theLump = readLumpNum(i);
									BSPObject.Models = Model.createLump(theLump, version);
									break;
								case 16:
									theLump = readLumpNum(i);
									BSPObject.Entities = Entity.createLump(theLump);
									break;
							}
						}
						catch
						{
							dumpLump(theLump);
						}
					}
					break;
				case mapType.TYPE_MOHAA: 
					DecompilerThread.OnMessage(this, "MOHAA BSP (modified id Tech 3)");
					for(int i=0;i<28;i++) {
						try {
							switch (i) {
								case 0:
									theLump = readLumpNum(i);
									BSPObject.Textures = Texture.createLump(theLump, version);
									break;
								case 1:
									theLump = readLumpNum(i);
									BSPObject.Planes = Plane.createLump(theLump, version);
									break;
								case 3:
									theLump = readLumpNum(i);
									BSPObject.Faces = Face.createLump(theLump, version);
									break;
								case 4:
									theLump = readLumpNum(i);
									BSPObject.Vertices = Vertex.createLump(theLump, version);
									break;
								case 11:
									theLump = readLumpNum(i);
									BSPObject.BrushSides = BrushSide.createLump(theLump, version);
									break;
								case 12:
									theLump = readLumpNum(i);
									BSPObject.Brushes = Brush.createLump(theLump, version);
									break;
								case 13:
									theLump = readLumpNum(i);
									BSPObject.Models = Model.createLump(theLump, version);
									break;
								case 14:
									theLump = readLumpNum(i);
									BSPObject.Entities = Entity.createLump(theLump);
									break;
								//case 24:
								//	theLump = readLumpNum(i);
								//	BSPObject.StaticProps = StaticProp.createLump(theLump);
								//	break;
							}
						}
						catch
						{
							dumpLump(theLump);
						}
					}
					break;
				case mapType.TYPE_FAKK: 
					DecompilerThread.OnMessage(this, "Heavy Metal FAKK² BSP");
					for(int i=0;i<20;i++) {
						try {
							switch (i) {
								case 0:
									theLump = readLumpNum(i);
									BSPObject.Textures = Texture.createLump(theLump, version);
									break;
								case 1:
									theLump = readLumpNum(i);
									BSPObject.Planes = Plane.createLump(theLump, version);
									break;
								case 10:
									theLump = readLumpNum(i);
									BSPObject.BrushSides = BrushSide.createLump(theLump, version);
									break;
								case 11:
									theLump = readLumpNum(i);
									BSPObject.Brushes = Brush.createLump(theLump, version);
									break;
								case 13:
									theLump = readLumpNum(i);
									BSPObject.Models = Model.createLump(theLump, version);
									break;
								case 14:
									theLump = readLumpNum(i);
									BSPObject.Entities = Entity.createLump(theLump);
									break;
							}
						}
						catch
						{
							dumpLump(theLump);
						}
					}
					break;
				case mapType.TYPE_RAVEN: 
				case mapType.TYPE_QUAKE3: 
					DecompilerThread.OnMessage(this, "BSP v46 (id Tech 3)");
					for(int i=0;i<18;i++) {
						try {
							switch (i) {
								case 0:
									theLump = readLumpNum(i);
									BSPObject.Entities = Entity.createLump(theLump);
									break;
								case 1:
									theLump = readLumpNum(i);
									BSPObject.Textures = Texture.createLump(theLump, version);
									break;
								case 2:
									theLump = readLumpNum(i);
									BSPObject.Planes = Plane.createLump(theLump, version);
									break;
								case 7:
									theLump = readLumpNum(i);
									BSPObject.Models = Model.createLump(theLump, version);
									break;
								case 8:
									theLump = readLumpNum(i);
									BSPObject.Brushes = Brush.createLump(theLump, version);
									break;
								case 9:
									theLump = readLumpNum(i);
									BSPObject.BrushSides = BrushSide.createLump(theLump, version);
									break;
								case 10:
									theLump = readLumpNum(i);
									BSPObject.Vertices = Vertex.createLump(theLump, version);
									break;
								case 13:
									theLump = readLumpNum(i);
									BSPObject.Faces = Face.createLump(theLump, version);
									break;
							}
						}
						catch
						{
							dumpLump(theLump);
						}
					}
					break;
				case mapType.TYPE_COD: 
					DecompilerThread.OnMessage(this, "BSP v59 (Call of Duty)");
					for(int i=0;i<33;i++) {
						try {
							switch (i) {
								case 0:
									theLump = readLumpNum(i);
									BSPObject.Textures = Texture.createLump(theLump, version);
									break;
								case 2:
									theLump = readLumpNum(i);
									BSPObject.Planes = Plane.createLump(theLump, version);
									break;
								case 3:
									theLump = readLumpNum(i);
									BSPObject.BrushSides = BrushSide.createLump(theLump, version);
									break;
								case 4:
									theLump = readLumpNum(i);
									BSPObject.Brushes = Brush.createLump(theLump, version);
									break;
								case 27:
									theLump = readLumpNum(i);
									BSPObject.Models = Model.createLump(theLump, version);
									break;
								case 29:
									theLump = readLumpNum(i);
									BSPObject.Entities = Entity.createLump(theLump);
									break;
							}
						}
						catch
						{
							dumpLump(theLump);
						}
					}
					break;
					
				case mapType.TYPE_COD2: 
					DecompilerThread.OnMessage(this, "Call of Duty 2 BSP");
					for(int i=0;i<40;i++) {
						try {
							switch (i) {
								case 0:
									theLump = readLumpNum(i);
									BSPObject.Textures = Texture.createLump(theLump, version);
									break;
								case 4:
									theLump = readLumpNum(i);
									BSPObject.Planes = Plane.createLump(theLump, version);
									break;
								case 5:
									theLump = readLumpNum(i);
									BSPObject.BrushSides = BrushSide.createLump(theLump, version);
									break;
								case 6:
									theLump = readLumpNum(i);
									BSPObject.Brushes = Brush.createLump(theLump, version);
									break;
								case 35:
									theLump = readLumpNum(i);
									BSPObject.Models = Model.createLump(theLump, version);
									break;
								case 37:
									theLump = readLumpNum(i);
									BSPObject.Entities = Entity.createLump(theLump);
									break;
							}
						}
						catch
						{
							dumpLump(theLump);
						}
					}
					break;
				case mapType.TYPE_COD4:
					DecompilerThread.OnMessage(this, "Call of Duty 4 BSP");
					for(int i=0;i<55;i++) {
						try {
							switch (i) {
								case 0:
									theLump = readLumpNum(i);
									BSPObject.Textures = Texture.createLump(theLump, version);
									break;
								case 4:
									theLump = readLumpNum(i);
									BSPObject.Planes = Plane.createLump(theLump, version);
									break;
								case 5:
									theLump = readLumpNum(i);
									BSPObject.BrushSides = BrushSide.createLump(theLump, version);
									break;
								case 8:
									theLump = readLumpNum(i);
									BSPObject.Brushes = Brush.createLump(theLump, version);
									break;
								case 37:
									theLump = readLumpNum(i);
									BSPObject.Models = Model.createLump(theLump, version);
									break;
								case 39:
									theLump = readLumpNum(i);
									BSPObject.Entities = Entity.createLump(theLump);
									break;
							}
						}
						catch
						{
							dumpLump(theLump);
						}
					}
					break;
				case mapType.TYPE_DOOM:
				case mapType.TYPE_HEXEN:
					DecompilerThread.OnMessage(this, "WAD file found");
					stream.Seek(4, SeekOrigin.Begin);
					int numLumps = br.ReadInt32();
					for(int i=0;i<numLumps;i++) {
						string name = getLumpName(i);
						if((name.Length == 5 && name[0] == 'M' && name[1] == 'A' && name[2] == 'P' && name[3] >= '0' && name[3] <= '9' && name[4] >= '0' && name[4] <= '9')
						|| ((name.Length == 4 && name[0] == 'E' && name[1] >= '0' && name[1] <= '9' && name[2] == 'M' && name[3] >= '0' && name[3] <= '9'))) {
							if(getLumpName(i+11) == "BEHAVIOR") {
								version = mapType.TYPE_HEXEN;
							}
							DecompilerThread.OnMessage(this, "Map: " + name);
							DoomMap currentMap = new DoomMap(BSPFile.FullName, name, version);
							int[] headerInfo = getLumpInfo(i);
							if (headerInfo[1] > 0 && Settings.extractZip) {
								Console.Write("Extracting Map Header ");
								writeLump(name+".txt", readLumpNum(i));
							}
							
							currentMap.Things = DThing.createLump(readLumpNum(i+1), version);
							currentMap.Linedefs = DLinedef.createLump(readLumpNum(i+2), version);
							currentMap.Sidedefs = DSidedef.createLump(readLumpNum(i+3));
							currentMap.Vertices = Vertex.createLump(readLumpNum(i+4), version);
							currentMap.Segments = DSegment.createLump(readLumpNum(i+5));
							currentMap.SubSectors = Edge.createLump(readLumpNum(i+6), version);
							currentMap.Nodes = DNode.createLump(readLumpNum(i+7));
							currentMap.Sectors = DSector.createLump(readLumpNum(i+8));

							doomMaps.Add(currentMap);
							currentMap.printBSPReport();
						}
					}
					break;
				default:
					DecompilerThread.OnMessage(this, "Tried to populate structures for a format not implemented yet!");
					break;
			}
			BSPObject.printBSPReport();
		}
		catch (System.IO.IOException) {
			DecompilerThread.OnMessage(this, "Unable to access BSP file! Is it open in another program?");
		}
		br.Close();
	}

	public void readGameLump(byte[] gamelumpData, int gamelumpFileOffset) {
		int numGamelumps = DataReader.readInt(gamelumpData[0], gamelumpData[1], gamelumpData[2], gamelumpData[3]);
		int gamelumpOffset = 4;
		if (version == mapType.TYPE_DMOMAM) {
			if(readAs == mapType.TYPE_UNDEFINED) {
				int next4 = DataReader.readInt(gamelumpData[4], gamelumpData[5], gamelumpData[6], gamelumpData[7]);
				if (next4 == 1) {
					gamelumpOffset += 4;
				} else {
					version = mapType.TYPE_SOURCE20;
					BSPObject.Version = mapType.TYPE_SOURCE20;
				}
			} else {
				gamelumpOffset += 4;
			}
		}
		if (numGamelumps > 1) {
			byte[] staticPropLump = new byte[0];
			int staticPropLumpVersion = 0;
			for (int i = 0; i < numGamelumps; i++) {
				string gamelumpID = DataReader.readString(new byte[] { gamelumpData[gamelumpOffset], gamelumpData[gamelumpOffset + 1], gamelumpData[gamelumpOffset + 2], gamelumpData[gamelumpOffset + 3] } );
				int gamelumpVersion = (int) DataReader.readShort(gamelumpData[gamelumpOffset + 6], gamelumpData[gamelumpOffset + 7]);
				int internalOffset = DataReader.readInt(gamelumpData[gamelumpOffset + 8], gamelumpData[gamelumpOffset + 9], gamelumpData[gamelumpOffset + 10], gamelumpData[gamelumpOffset + 11]);
				int internalLength = DataReader.readInt(gamelumpData[gamelumpOffset + 12], gamelumpData[gamelumpOffset + 13], gamelumpData[gamelumpOffset + 14], gamelumpData[gamelumpOffset + 15]);
				if ((internalOffset < 4 + (16 * numGamelumps) && version == mapType.TYPE_SOURCE20 && readAs == mapType.TYPE_UNDEFINED) || version == mapType.TYPE_VINDICTUS || readAs == mapType.TYPE_VINDICTUS) {
					// Even if the offset is relative to start of game lump, it will never be below this. If it is, it uses this format instead.
					BSPObject.Version = mapType.TYPE_VINDICTUS;
					version = mapType.TYPE_VINDICTUS;
					gamelumpVersion = DataReader.readInt(gamelumpData[gamelumpOffset + 8], gamelumpData[gamelumpOffset + 9], gamelumpData[gamelumpOffset + 10], gamelumpData[gamelumpOffset + 11]);
					internalOffset = DataReader.readInt(gamelumpData[gamelumpOffset + 12], gamelumpData[gamelumpOffset + 13], gamelumpData[gamelumpOffset + 14], gamelumpData[gamelumpOffset + 15]);
					internalLength = DataReader.readInt(gamelumpData[gamelumpOffset + 16], gamelumpData[gamelumpOffset + 17], gamelumpData[gamelumpOffset + 18], gamelumpData[gamelumpOffset + 19]);
					gamelumpOffset += 20;
				} else {
					gamelumpOffset += 16;
					if (version == mapType.TYPE_DMOMAM) {
						gamelumpOffset += 4;
					}
				}
				if (internalOffset < gamelumpFileOffset) {
					internalOffset += gamelumpFileOffset;
				}
				if (gamelumpID.Equals("prps", StringComparison.InvariantCultureIgnoreCase)) {
					staticPropLumpVersion = gamelumpVersion;
					staticPropLump = readLump(internalOffset, internalLength);
					BSPObject.StaticProps = SourceStaticProp.createLump(staticPropLump, version,  staticPropLumpVersion);
				}
				// Other game lumps would go here
			}
		}
	}

	// For Doom maps only, return the name of the lump at the specified index
	public string getLumpName(int index) {
		stream.Seek(4, SeekOrigin.Begin);
		int numLumps = br.ReadInt32();
		if(index > numLumps - 1) {
			return "";
		} //else
		int directoryOffset = br.ReadInt32();
		stream.Seek(directoryOffset + (index*16) + 8, SeekOrigin.Begin);
		byte[] name = br.ReadBytes(8);
		return DataReader.readNullTerminatedString(name);
	}

	// Doom maps only. Return the offset/length pair of the lump at the specified index
	public int[] getLumpInfo(int index) {
		stream.Seek(4, SeekOrigin.Begin);
		int numLumps = br.ReadInt32();
		if(index > numLumps - 1) {
			return new int[] {0,0};
		} //else
		int directoryOffset = br.ReadInt32();
		stream.Seek(directoryOffset + (index*16), SeekOrigin.Begin);
		int temp = br.ReadInt32();
		return new int[] {temp, br.ReadInt32()};
	}

	public byte[] readLumpNum(int index) {
		switch(version) {
			case mapType.TYPE_QUAKE: 
			case mapType.TYPE_NIGHTFIRE: 
				return readLumpFromHeader(4 + (8*index));
				break;
			case mapType.TYPE_QUAKE2: 
			case mapType.TYPE_DAIKATANA: 
			case mapType.TYPE_SIN: 
			case mapType.TYPE_SOF:  
			case mapType.TYPE_QUAKE3: 
			case mapType.TYPE_RAVEN: 
			case mapType.TYPE_COD: 
				return readLumpFromHeader(8 + (8*index));
				break;
			case mapType.TYPE_COD2: 
				stream.Seek(8 + (8*index), SeekOrigin.Begin);
				int tmep = br.ReadInt32();
				return readLump(br.ReadInt32(), tmep);
				break;
			case mapType.TYPE_STEF2: 
			case mapType.TYPE_STEF2DEMO:
			case mapType.TYPE_MOHAA: 
			case mapType.TYPE_FAKK: 
				return readLumpFromHeader(12 + (8*index));
				break;
			case mapType.TYPE_COD4: 
				//TODO
				// CoD4 is somewhat unique, it calls for a different kind of reader.
				stream.Seek(8, SeekOrigin.Begin);// IBSP version 22
				int numlumps = br.ReadInt32();
				int offset = (numlumps * 8) + 12;
				for (int i = 0; i < numlumps; i++) {
					int id = br.ReadInt32();
					int length = br.ReadInt32();
					if(id == index) {
						return readLump(offset, length);
						break;
					} else {
						offset += length;
						while(offset % 4 != 0) { offset ++; }
					}
				}
				break;
			case mapType.TYPE_SOURCE17: 
			case mapType.TYPE_SOURCE18: 
			case mapType.TYPE_SOURCE19: 
			case mapType.TYPE_SOURCE20: 
			case mapType.TYPE_SOURCE21: 
			case mapType.TYPE_SOURCE22: 
			case mapType.TYPE_SOURCE23: 
			case mapType.TYPE_SOURCE27: 
			case mapType.TYPE_TACTICALINTERVENTION: 
			case mapType.TYPE_VINDICTUS: 
			case mapType.TYPE_DMOMAM: 
				return readLumpFromHeader(8 + (16*index));
				break;
			case mapType.TYPE_DOOM:
			case mapType.TYPE_HEXEN:
				int[] ol = getLumpInfo(index);
				return readLump(ol[0], ol[1]);
				break;
		}
		return new byte[0];
	}
	
	// Returns the lump referenced by the offset/length pair at the specified offset
	public byte[] readLumpFromHeader(int offset) {
		try {
			stream.Seek(offset, SeekOrigin.Begin);
			byte[] input = br.ReadBytes(12);
			if (version == mapType.TYPE_TACTICALINTERVENTION) {
				input = encrypt(input, offset);
			}
			if(isL4D2) {
				lumpOffset = DataReader.readInt(input[4], input[5], input[6], input[7]);
				lumpLength = DataReader.readInt(input[8], input[9], input[10], input[11]);
			} else {
				lumpOffset = DataReader.readInt(input[0], input[1], input[2], input[3]);
				lumpLength = DataReader.readInt(input[4], input[5], input[6], input[7]);
			}
			if(bigEndian) { lumpOffset = DataReader.swapEndian(lumpLength); lumpOffset = DataReader.swapEndian(lumpLength); }
			return readLump(lumpOffset, lumpLength);
		}
		catch (System.IO.IOException) {
			DecompilerThread.OnMessage(this, "Unknown error reading BSP, it was working before!");
		}
		return new byte[0];
	}

	// +readLump(int, int)
	// Reads the lump length bytes long at offset in the file
	public byte[] readLump(int offset, int length) {
		try {
			stream.Seek(offset, SeekOrigin.Begin);
			byte[] input = br.ReadBytes(length);
			if (version == mapType.TYPE_TACTICALINTERVENTION) {
				input = encrypt(input, offset);
			}
			return input;
		}
		catch (System.IO.IOException) {
			DecompilerThread.OnMessage(this, "Unknown error reading BSP, it was working before!");
		}
		return new byte[0];
	}

	// +readLump(int, int, int)
	// Reads the lump length bytes long at offset in a source engine BSP
	public byte[] readLump(int offset, int length, int lumpversion) {
		try {
			stream.Seek(offset, SeekOrigin.Begin);
			byte[] input = br.ReadBytes(length);
			if (version == mapType.TYPE_TACTICALINTERVENTION) {
				input = encrypt(input, offset);
			}
			return input;
		}
		catch (System.IO.IOException) {
			DecompilerThread.OnMessage(this, "Unknown error reading BSP, it was working before!");
		}
		return new byte[0];
	}

	// XOR encrypter functions, for Tactical Intervention
	public byte[] encrypt(byte[] data) {
		return encrypt(data, 0);
	}

	public void dumpLump(byte[] data) {
		if(Settings.dumpCrashLump) {
			DecompilerThread.OnMessage(this, "Error reading a lump, dumping to crashlump.lmp!");
			writeLump("crashlump.lmp", data);
		}
	}

	public void writeLump(string name, byte[] data) {
		try {
			FileStream debugstream;
			if(Settings.outputFolder=="default") {
				debugstream = new FileStream(BSPObject.Folder +"\\"+ name, FileMode.Create, FileAccess.Write);
			} else {
				debugstream = new FileStream(Settings.outputFolder + name, FileMode.Create, FileAccess.Write);
			}
			BinaryWriter bw = new BinaryWriter(debugstream);
			debugstream.Seek(0, SeekOrigin.Begin);
			bw.Write(data);
		} catch(System.IO.IOException e) {
			if(Settings.outputFolder=="default") {
				DecompilerThread.OnMessage(this, "ERROR: Could not save "+BSPObject.Folder +"\\"+ name+", ensure the file is not open in another program.");
			} else {
				DecompilerThread.OnMessage(this, "ERROR: Could not save "+Settings.outputFolder + name+", ensure the file is not open in another program.");
			}
			throw e;
		}
	}
	
	public byte[] encrypt(byte[] data, int offset) {
		byte[] output = new byte[data.Length];
		for (int i = 0; i < data.Length; i++) {
			output[i] = (byte)(data[i] ^ key[(i + offset) % key.Length]);
		}
		return output;
	}

	// Tries to determine what version BSP this is. Returns a member of the mapType enum, as an int.
	public mapType findVersionNumber() {
		mapType current = mapType.TYPE_UNDEFINED;
		stream.Seek(0, SeekOrigin.Begin);
		int data = br.ReadInt32();
		if(bigEndian) { DataReader.swapEndian(data); }
		if (data == 1347633737) {
			// 1347633737 reads in ASCII as "IBSP"
			data = br.ReadInt32();
			if(bigEndian) { DataReader.swapEndian(data); }
			switch (data) {
				case 4: 
					current = mapType.TYPE_COD2;
					break;
				case 22: 
					current = mapType.TYPE_COD4;
					break;
				case 59: 
					current = mapType.TYPE_COD;
					break;
				case 38: 
					current = mapType.TYPE_QUAKE2;
					break;
				case 41: 
					current = mapType.TYPE_DAIKATANA;
					break;
				case 46: 
					current = mapType.TYPE_QUAKE3;
					for (int i = 0; i < 17; i++) {
						// Find out where the first lump starts, based on offsets.
						// This process assumes the file header has not been tampered with in any way.
						// Unfortunately it could inadvertantly lead to a method of decompile protection.
						stream.Seek((i+1) * 8, SeekOrigin.Begin);
						int temp = br.ReadInt32();
						if(bigEndian) { DataReader.swapEndian(temp); }
						if (temp == 184) {
							current = mapType.TYPE_SOF;
							break;
						} else {
							if (temp == 144) {
								break;
							}
						}
					}
					break;
				case 47: 
					current = mapType.TYPE_QUAKE3;
					break;
			}
		} else {
			if (data == 892416050) {
				// 892416050 reads in ASCII as "2015," the game studio which developed MoHAA
				current = mapType.TYPE_MOHAA;
			} else {
				if (data == 1095516485) {
					// 1095516485 reads in ASCII as "EALA," the ones who developed MoHAA Spearhead and Breakthrough
					current = mapType.TYPE_MOHAA;
				} else {
					if (data == 1347633750) {
						// 1347633750 reads in ASCII as "VBSP." Indicates Source engine.
						// Some source games handle this as 2 shorts. Since most version numbers
						// are below 65536, I can always read the first number as a short, because
						// the least significant bits come first.
						data = (int)br.ReadUInt16();
						switch (data) {
							case 17: 
								current = mapType.TYPE_SOURCE17;
								break;
							case 18: 
								current = mapType.TYPE_SOURCE18;
								break;
							case 19: 
								current = mapType.TYPE_SOURCE19;
								break;
							case 20: 
								int version2 = (int)br.ReadUInt16();
								if (version2 == 4) {
									current = mapType.TYPE_DMOMAM;
								} else {
									current = mapType.TYPE_SOURCE20;
								}
								break;
							case 21: 
								current = mapType.TYPE_SOURCE21;
								break;
							case 22: 
								current = mapType.TYPE_SOURCE22;
								break;
							case 23: 
								current = mapType.TYPE_SOURCE23;
								break;
							case 27: 
								current = mapType.TYPE_SOURCE27;
								break;
						}
					} else {
						if (data == 1347633746) {
							// Reads in ASCII as "RBSP". Raven software's modification of Q3BSP, or Ritual's modification of Q2.
							current = mapType.TYPE_RAVEN;
							for (int i = 0; i < 17; i++) {
								// Find out where the first lump starts, based on offsets.
								// This process assumes the file header has not been tampered with in any way.
								// Unfortunately it could inadvertantly lead to a method of decompile protection.
								stream.Seek((i+1) * 8, SeekOrigin.Begin);
								int temp = br.ReadInt32();
								if(bigEndian) { DataReader.swapEndian(temp); }
								if (temp == 168) {
									current = mapType.TYPE_SIN;
									break;
								} else {
									if (temp == 152) {
										break;
									}
								}
							}
						} else {
							if (data == 556942917) {
								// "EF2!"
								current = mapType.TYPE_STEF2;
							} else {
								if (data == 1145132873 || data == 1145132880) {
									// "IWAD" or "PWAD"
									wad = true;
									current = mapType.TYPE_DOOM;
								} else {
									if (data == 1263223110) {
										// "FAKK"
										data = br.ReadInt32();
										if(bigEndian) { DataReader.swapEndian(data); }
										switch (data) {
											case 19: 
												current = mapType.TYPE_STEF2DEMO;
												break;
											case 12: 
											case 42: // American McGee's Alice
												current = mapType.TYPE_FAKK;
												break;
										}
									} else {
										switch (data) {
											case 29: 
											case 30: 
												current = mapType.TYPE_QUAKE;
												break;
											case 42: 
												current = mapType.TYPE_NIGHTFIRE;
												break;
											default:
												stream.Seek(384, SeekOrigin.Begin);
												key = br.ReadBytes(32);
												stream.Seek(0, SeekOrigin.Begin);
												data = DataReader.readInt(encrypt(br.ReadBytes(4)));
												if(data == 1347633750) {
													current = mapType.TYPE_TACTICALINTERVENTION;
												} else {
													current = mapType.TYPE_UNDEFINED;
												}
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
		return current;
	}
	
	// ACCESSORS/MUTATORS
	
	public BSP BSPData {
		get {
			return BSPObject;
		}
	}

	public List<DoomMap> DoomMaps {
		get {
			return doomMaps;
		}
	}

	virtual public bool WAD {
		get {
			return wad;
		}
	}

	virtual public mapType Version {
		get {
			if (readAs != mapType.TYPE_UNDEFINED) {
				// If forcing to read as Tactical Intervention map, attempt to grab the encryption key.
				if(readAs == mapType.TYPE_TACTICALINTERVENTION) {
					stream.Seek(384, SeekOrigin.Begin);
					key = br.ReadBytes(32);
				}
				return readAs;
			} // else
			if(version != mapType.TYPE_UNDEFINED) { return version; } // else
			mapType current = findVersionNumber(); // Attempt to get the current version.
			if(current == mapType.TYPE_UNDEFINED) {
				bigEndian = true; // If it fails, try reading things as big endian.
				current = findVersionNumber();
				if(current == mapType.TYPE_UNDEFINED) {
					bigEndian = false; // If that still fails, then it probably isn't big endian either.
				}
			}
			return current;
		}
	}
}