// DoomMap class
// This class gathers all relevant information from the lumps of a Doom Map.
// I don't know if I can call this a BSP, though. It's more of a Binary Area
// Partition, a BAP.
// Anyhow, it never had a formal BSP version number, nor was it ever referred
// to as a BSP, so it's DoomMap.
using System;
using UnityEngine;

namespace BSPImporter {
	public class DoomMap {

		// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS

		// Since all Doom engine maps were incorporated into the WAD, we need to keep
		// track of both the location of the WAD file and the internal name of the map.
		private string wadpath;
		private string mapName;
		private mapType version;

		// Each lump has its own class for handling its specific data structures.
		// These are the only lumps we need for decompilation.
		private Lump<DThing> things;
		private Lump<DLinedef> linedefs;
		private Lump<DSidedef> sidedefs;
		private Lump<UIVertex> vertices;
		private Lump<DSegment> segs;
		private Lump<Edge> subsectors;
		private Lump<DNode> nodes;
		private Lump<DSector> sectors;

		// CONSTRUCTORS
		// This accepts a folder path and looks for the BSP there.
		public DoomMap(string wadpath, string map, mapType version) {
			this.wadpath = wadpath;
			this.mapName = map;
			this.version = version;
		}

		// METHODS

		public virtual void printBSPReport() {
			try {
				Debug.Log("Things lump: " + things.Length + " bytes, " + things.Count + " items");
				if(things.hasFunnySize()) {
					Debug.Log("WARNING: Funny lump size in Things");
				}
			} catch(System.NullReferenceException) {
			}
			try {
				Debug.Log("Linedefs lump: " + linedefs.Length + " bytes, " + linedefs.Count + " items");
				if(linedefs.hasFunnySize()) {
					Debug.Log("WARNING: Funny lump size in Linedefs");
				}
			} catch(System.NullReferenceException) {
			}
			try {
				Debug.Log("Sizedefs lump: " + sidedefs.Length + " bytes, " + sidedefs.Count + " items");
				if(sidedefs.hasFunnySize()) {
					Debug.Log("WARNING: Funny lump size in Sidedefs");
				}
			} catch(System.NullReferenceException) {
			}
			try {
				Debug.Log("Vertices lump: " + vertices.Length + " bytes, " + vertices.Count + " items");
				if(vertices.hasFunnySize()) {
					Debug.Log("WARNING: Funny lump size in Vertices");
				}
			} catch(System.NullReferenceException) {
			}
			try {
				Debug.Log("Segments lump: " + segs.Length + " bytes, " + segs.Count + " items");
				if(segs.hasFunnySize()) {
					Debug.Log("WARNING: Funny lump size in Segments");
				}
			} catch(System.NullReferenceException) {
			}
			try {
				Debug.Log("Subsectors lump: " + subsectors.Length + " bytes, " + subsectors.Count + " items");
				if(subsectors.hasFunnySize()) {
					Debug.Log("WARNING: Funny lump size in Subsectors");
				}
			} catch(System.NullReferenceException) {
			}
			try {
				Debug.Log("Nodes lump: " + nodes.Length + " bytes, " + nodes.Count + " items");
				if(nodes.hasFunnySize()) {
					Debug.Log("WARNING: Funny lump size in Nodes");
				}
			} catch(System.NullReferenceException) {
			}
			try {
				Debug.Log("Sectors lump: " + sectors.Length + " bytes, " + sectors.Count + " items");
				if(sectors.hasFunnySize()) {
					Debug.Log("WARNING: Funny lump size in Sectors");
				}
			} catch(System.NullReferenceException) {
			}
		}

		// ACCESSORS/MUTATORS
		virtual public mapType Version {
			get {
				return version;
			}
		}

		virtual public string Path {
			get {
				return wadpath;
			}
		}

		virtual public string MapName {
			get {
				return mapName;
			}
		}

		virtual public string Folder {
			get {
				int i;
				for(i = 0; i < wadpath.Length; i++) {
					if(wadpath[wadpath.Length - 1 - i] == '\\') {
						break;
					}
					if(wadpath[wadpath.Length - 1 - i] == '/') {
						break;
					}
				}
				return wadpath.Substring(0, (wadpath.Length - i) - (0));
			}
		}

		virtual public string WadName {
			get {
				System.IO.FileInfo newFile = new System.IO.FileInfo(wadpath);
				return newFile.Name.Substring(0, (newFile.Name.Length - 4) - (0));
			}
		}

		virtual public Lump<DSidedef> Sidedefs {
			set {
				sidedefs = value;
			}
			get {
				return sidedefs;
			}
		}

		virtual public Lump<UIVertex> Vertices {
			set {
				vertices = value;
			}
			get {
				return vertices;
			}
		}

		virtual public Lump<DSegment> Segments {
			set {
				segs = value;
			}
			get {
				return segs;
			}
		}

		virtual public Lump<Edge> SubSectors {
			set {
				subsectors = value;
			}
			get {
				return subsectors;
			}
		}

		virtual public Lump<DNode> Nodes {
			set {
				nodes = value;
			}
			get {
				return nodes;
			}
		}

		virtual public Lump<DSector> Sectors {
			set {
				sectors = value;
			}
			get {
				return sectors;
			}
		}

		public virtual Lump<DLinedef> Linedefs {
			set {
				linedefs = value;
			}
			get {
				return linedefs;
			}
		}

		public virtual Lump<DThing> Things {
			get {
				return things;
			}
			set {
				things = value;
			}
		}
	}
}