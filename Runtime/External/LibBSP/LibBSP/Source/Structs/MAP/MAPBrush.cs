using System;
using System.Collections.Generic;

namespace LibBSP {

	/// <summary>
	/// Class containing all data for a single brush, including side definitions or a patch definition.
	/// </summary>
	[Serializable] public class MAPBrush {

		public List<MAPBrushSide> sides = new List<MAPBrushSide>(6);
		public MAPPatch patch;
		public MAPTerrainEF2 ef2Terrain;
		public MAPTerrainMoHAA mohTerrain;

		public bool isDetail = false;
		public bool isWater = false;
		public bool isManVis = false;

		/// <summary>
		/// Creates a new empty <see cref="MAPBrush"/> object. Internal data will have to be set manually.
		/// </summary>
		public MAPBrush() { }

		/// <summary>
		/// Creates a new <see cref="MAPBrush"/> object using the supplied <c>string</c> array as data.
		/// </summary>
		/// <param name="lines">Data to parse.</param>
		public MAPBrush(IList<string> lines) {
			int braceCount = 0;
			bool brushDef3 = false;
			bool inPatch = false;
			bool inTerrain = false;
			List<string> child = new List<string>();
			for (int i = 0; i < lines.Count; ++i) {
				string line = lines[i];

				if (line[0] == '{') {
					braceCount++;
					if (braceCount == 1 || brushDef3) { continue; }
				} else if (line[0] == '}') {
					braceCount--;
					if (braceCount == 0 || brushDef3) { continue; }
				}

				if (braceCount == 1 || brushDef3) {
					// Source engine
					if (line.Length >= "side".Length && line.Substring(0, "side".Length) == "side") {
						continue;
					}
					// id Tech does this kinda thing
					else if (line.Length >= "patch".Length && line.Substring(0, "patch".Length) == "patch") {
						inPatch = true;
						// Gonna need this line too. We can switch on the type of patch definition, make things much easier.
						child.Add(line);
						continue;
					} else if (inPatch) {
						child.Add(line);
						inPatch = false;
						patch = new MAPPatch(child.ToArray());
						child = new List<string>();
						continue;
					} else if (line.Length >= "terrainDef".Length && line.Substring(0, "terrainDef".Length) == "terrainDef") {
						inTerrain = true;
						child.Add(line);
						continue;
					} else if (inTerrain) {
						child.Add(line);
						inTerrain = false;
						// TODO: MoHRadiant terrain
						ef2Terrain = new MAPTerrainEF2(child.ToArray());
						child = new List<string>();
						continue;
					} else if (line.Length >= "brushDef3".Length && line.Substring(0, "brushDef3".Length) == "brushDef3") {
						brushDef3 = true;
						continue;
					} else if (line == "\"BRUSHFLAGS\" \"DETAIL\"") {
						isDetail = true;
						continue;
					} else if (line.Length >= "\"id\"".Length && line.Substring(0, "\"id\"".Length) == "\"id\"") {
						continue;
					} else {
						child.Add(line);
						sides.Add(new MAPBrushSide(child.ToArray()));
						child = new List<string>();
					}
				} else if (braceCount > 1) {
					child.Add(line);
				}
			}
		}

	}
}
