using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// A class that takes an <see cref="Entities"/> object and can convert it into a <c>string</c>,
	/// to output to a file.
	/// </summary>
	public class MoHRadiantMapGenerator {

		private Job _master;

		private Entities _entities;
		private IFormatProvider format = CultureInfo.CreateSpecificCulture("en-US");

		/// <summary>
		/// Creates a new instance of a <see cref="MoHRadiantMapGenerator"/> object that will operate on "<paramref name="from"/>".
		/// </summary>
		/// <param name="from">The <see cref="Entities"/> object to output to a <c>string</c>.</param>
		/// <param name="master">The parent <see cref="Job"/> object for this instance.</param>
		public MoHRadiantMapGenerator(Entities from, Job master) {
			this._entities = from;
			this._master = master;
		}

		/// <summary>
		/// Parses the <see cref="Entities"/> object pointed to by this object into a <c>string</c>, to output to a file.
		/// </summary>
		/// <returns>A <c>string</c> representation of the <see cref="Entities"/> pointed to by this object.</returns>
		public string ParseMap() {
			// This initial buffer is probably too small (512kb) but should minimize the amount of allocations needed.
			StringBuilder sb = new StringBuilder(524288);
			for (int i = 0; i < _entities.Count; ++i) {
				ParseEntity(_entities[i], i, sb);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Process the data in an <see cref="Entity"/> into the passed <see cref="StringBuilder"/>.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to process.</param>
		/// <param name="index">The index of this <see cref="Entity"/> in the map.</param>
		/// <param name="sb">A <see cref="StringBuilder"/> object to append processed data from <paramref name="entity"/> to.</param>
		private void ParseEntity(Entity entity, int index, StringBuilder sb) {
			if (index != 0) {
				sb.Append("// Entity ")
				.Append(index)
				.Append("\r\n");
			}
			sb.Append("{\r\n");
			foreach (KeyValuePair<string, string> kvp in entity) {
				sb.Append("\"")
				.Append(kvp.Key)
				.Append("\" \"")
				.Append(kvp.Value)
				.Append("\"\r\n");
			}
			for (int i = 0; i < entity.brushes.Count; ++i) {
				ParseBrush(entity.brushes[i], i, sb);
			}
			sb.Append("}\r\n");
		}

		/// <summary>
		/// Process the data in a <see cref="MAPBrush"/> into the passed <see cref="StringBuilder"/>.
		/// </summary>
		/// <param name="brush">The <see cref="MAPBrush"/> to process.</param>
		/// <param name="index">The index of <see cref="MAPBrush"/> entity in the <see cref="Entity"/>.</param>
		/// <param name="sb">A <see cref="StringBuilder"/> object to append processed data from <paramref name="brush"/> to.</param>
		private void ParseBrush(MAPBrush brush, int index, StringBuilder sb) {
			// Unsupported features. Ignore these completely.
			if (brush.ef2Terrain != null) {
				return;
			}
			if (brush.sides.Count < 4 && brush.patch == null && brush.mohTerrain == null) {
				// Can't create a brush with less than 4 sides
				_master.Print("WARNING: Tried to create brush from " + brush.sides.Count + " sides!");
				return;
			}
			sb.Append("// Brush ")
			.Append(index.ToString())
			.Append("\r\n");
			if (brush.patch != null) {
				ParsePatch(brush.patch, sb);
			} else if (brush.mohTerrain != null) {
				ParseTerrain(brush.mohTerrain, sb);
			} else {
				sb.Append("{\r\n");
				foreach (MAPBrushSide brushSide in brush.sides) {
					ParseBrushSide(brushSide, brush.isDetail, sb);
				}
				sb.Append("}\r\n");
			}
		}
		
		/// <summary>
		/// Process the data in a <see cref="MAPBrushSide"/> into the passed <see cref="StringBuilder"/>.
		/// </summary>
		/// <param name="brushside">The <see cref="MAPBrushSide"/> to process.</param>
		/// <param name="sb">A <see cref="StringBuilder"/> object to append processed data from <paramref name="brushside"/> to.</param>
		private void ParseBrushSide(MAPBrushSide brushside, bool isDetail, StringBuilder sb) {
			sb.Append("( ")
			.Append(brushside.vertices[0].x.ToString("###0.######", format))
			.Append(" ")
			.Append(brushside.vertices[0].y.ToString("###0.######", format))
			.Append(" ")
			.Append(brushside.vertices[0].z.ToString("###0.######", format))
			.Append(" ) ( ")
			.Append(brushside.vertices[1].x.ToString("###0.######", format))
			.Append(" ")
			.Append(brushside.vertices[1].y.ToString("###0.######", format))
			.Append(" ")
			.Append(brushside.vertices[1].z.ToString("###0.######", format))
			.Append(" ) ( ")
			.Append(brushside.vertices[2].x.ToString("###0.######", format))
			.Append(" ")
			.Append(brushside.vertices[2].y.ToString("###0.######", format))
			.Append(" ")
			.Append(brushside.vertices[2].z.ToString("###0.######", format))
			.Append(" ) ")
			.Append(brushside.texture)
			.Append(" ")
			.Append(brushside.textureShiftS.ToString("###0.######", format))
			.Append(" ")
			.Append(brushside.textureShiftT.ToString("###0.######", format))
			.Append(" ")
			.Append(brushside.texRot.ToString("###0.00", format))
			.Append(" ")
			.Append(brushside.texScaleX.ToString("###0.######", format))
			.Append(" ")
			.Append(brushside.texScaleY.ToString("###0.######", format))
			.Append(" ")
			.Append(brushside.flags)
			.Append(" 0 0");
			if (isDetail) {
				sb.Append(" +surfaceparm detail");
			}
			sb.Append("\r\n");
		}

		/// <summary>
		/// Process the data in a <see cref="MAPPatch"/> into the passed <see cref="StringBuilder"/>.
		/// </summary>
		/// <param name="patch">The <see cref="MAPPatch"/> to process.</param>
		/// <param name="sb">A <see cref="StringBuilder"/> object to append processed data from <paramref name="patch"/> to.</param>
		private void ParsePatch(MAPPatch patch, StringBuilder sb) {
			sb.Append(" {\r\n  patchDef2\r\n  {\r\n")
			.Append(patch.texture)
			.Append("\r\n( ")
			.Append((int)Math.Round(patch.dims.x))
			.Append(" ")
			.Append((int)Math.Round(patch.dims.y))
			.Append(" 0 0 0 )\r\n(\r\n");
			for (int i = 0; i < patch.dims.x; ++i) {
				sb.Append("( ");
				for (int j = 0; j < patch.dims.y; ++j) {
					UIVertex vertex = patch.points[((int)Math.Round(patch.dims.x) * j) + i];
					sb.Append("( ")
					.Append(vertex.position.x.ToString("###0.#####", format))
					.Append(" ")
					.Append(vertex.position.y.ToString("###0.#####", format))
					.Append(" ")
					.Append(vertex.position.z.ToString("###0.#####", format))
					.Append(" ")
					.Append(vertex.uv0.x.ToString("###0.#####", format))
					.Append(" ")
					.Append(vertex.uv0.y.ToString("###0.#####", format))
					.Append(" ) ");
				}
				sb.Append(")\r\n");
			}
			sb.Append(")\r\n  }\r\n }\r\n");
		}

		/// <summary>
		/// Process the data in a <see cref="MAPTerrainMoHAA"/> into the passed <c>StringBuilder</c>.
		/// </summary>
		/// <param name="terrain">The <see cref="MAPTerrainMoHAA"/> to process.</param>
		/// <param name="sb">A <c>StringBuilder</c> object to append processed data from <paramref name="terrain"/> to.</param>
		private void ParseTerrain(MAPTerrainMoHAA terrain, StringBuilder sb) {
			sb.Append(" {\r\n  terrainDef\r\n  {\r\n   ")
			.Append(terrain.size[0])
			.Append(" ")
			.Append(terrain.size[1])
			.Append(" ")
			.Append(terrain.flags)
			.Append("\r\n   ")
			.Append(terrain.origin.x.ToString("###0.000000"))
			.Append(" ")
			.Append(terrain.origin.y.ToString("###0.000000"))
			.Append(" ")
			.Append(terrain.origin.z.ToString("###0.000000"))
			.Append("\r\n\t\t{\r\n");
			foreach (MAPTerrainMoHAA.Partition partition in terrain.partitions) {
				sb.Append("\t\t\t")
				.Append(partition.unknown1)
				.Append(" ")
				.Append(partition.unknown2)
				.Append(" ( ")
				.Append(partition.shader)
				.Append(" ")
				.Append(partition.textureShift[0])
				.Append(" ")
				.Append(partition.textureShift[1])
				.Append(" ")
				.Append(partition.rotation.ToString("###0.00"))
				.Append(" ")
				.Append(partition.unknown3)
				.Append(" ")
				.Append(partition.textureScale[0])
				.Append(" ")
				.Append(partition.textureScale[1])
				.Append(" ")
				.Append(partition.unknown4)
				.Append(" ")
				.Append(partition.flags)
				.Append(" ")
				.Append(partition.unknown5);
				if (!string.IsNullOrEmpty(partition.properties)) {
					sb.Append(" ")
					.Append(partition.properties);
				}
				sb.Append(" )\r\n");
			}
			sb.Append("\t\t}\r\n\t\t{\r\n");
			foreach (MAPTerrainMoHAA.Vertex vertex in terrain.vertices) {
				sb.Append("\t\t\t")
				.Append(vertex.height.ToString("###0.000000"))
				.Append(" ( ");
				if (!string.IsNullOrEmpty(vertex.unknown1)) {
					sb.Append(vertex.unknown1)
					.Append(" ");
				}
				sb.Append(") ( ");
				if (!string.IsNullOrEmpty(vertex.unknown2)) {
					sb.Append(vertex.unknown2)
					.Append(" ");
				}
				sb.Append(")\r\n");
			}
			sb.Append("\t\t}\r\n  }\r\n }\r\n");
		}
	}
}
