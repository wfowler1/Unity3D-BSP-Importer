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
	public class RadiantMapGenerator {

		private Job _master;

		private Entities _entities;
		private IFormatProvider format = CultureInfo.CreateSpecificCulture("en-US");

		/// <summary>
		/// Creates a new instance of a <see cref="RadiantMapGenerator"/> object that will operate on <paramref name="from"/>.
		/// </summary>
		/// <param name="from">The <see cref="Entities"/> object to output to a <c>string</c>.</param>
		/// <param name="master">The parent <see cref="Job"/> object for this instance.</param>
		public RadiantMapGenerator(Entities from, Job master) {
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
			sb.Append("// entity ")
			.Append(index)
			.Append("\r\n{\r\n");
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
			if (brush.sides.Count < 4 && brush.patch == null) {
				// Can't create a brush with less than 4 sides
				_master.Print("WARNING: Tried to create brush from " + brush.sides.Count + " sides!");
				return;
			}
			sb.Append("// brush ")
			.Append(index.ToString())
			.Append("\r\n{\r\n");
			if (brush.patch != null) {
				ParsePatch(brush.patch, sb);
			} else {
				foreach (MAPBrushSide brushSide in brush.sides) {
					ParseBrushSide(brushSide, sb);
				}
			}
			sb.Append("}\r\n");
		}
		
		/// <summary>
		/// Process the data in a <see cref="MAPBrushSide"/> into the passed <see cref="StringBuilder"/>.
		/// </summary>
		/// <param name="brushside">The <see cref="MAPBrushSide"/> to process.</param>
		/// <param name="sb">A <see cref="StringBuilder"/> object to append processed data from <paramref name="brushside"/> to.</param>
		private void ParseBrushSide(MAPBrushSide brushside, StringBuilder sb) {
			sb.Append("( ")
			.Append(brushside.vertices[0].x.ToString("###0.##########", format))
			.Append(" ")
			.Append(brushside.vertices[0].y.ToString("###0.##########", format))
			.Append(" ")
			.Append(brushside.vertices[0].z.ToString("###0.##########", format))
			.Append(" ) ( ")
			.Append(brushside.vertices[1].x.ToString("###0.##########", format))
			.Append(" ")
			.Append(brushside.vertices[1].y.ToString("###0.##########", format))
			.Append(" ")
			.Append(brushside.vertices[1].z.ToString("###0.##########", format))
			.Append(" ) ( ")
			.Append(brushside.vertices[2].x.ToString("###0.##########", format))
			.Append(" ")
			.Append(brushside.vertices[2].y.ToString("###0.##########", format))
			.Append(" ")
			.Append(brushside.vertices[2].z.ToString("###0.##########", format))
			.Append(" ) ")
			.Append(brushside.texture)
			.Append(" ")
			.Append(brushside.textureShiftS)
			.Append(" ")
			.Append(brushside.textureShiftT)
			.Append(" ")
			.Append(brushside.texRot)
			.Append(" ")
			.Append(brushside.texScaleX.ToString("###0.##########", format))
			.Append(" ")
			.Append(brushside.texScaleY.ToString("###0.##########", format))
			.Append(" ")
			.Append(brushside.flags)
			.Append(" 0 0\r\n");
		}

		/// <summary>
		/// Process the data in a <see cref="MAPPatch"/> into the passed <see cref="StringBuilder"/>.
		/// </summary>
		/// <param name="patch">The <see cref="MAPPatch"/> to process.</param>
		/// <param name="sb">A <see cref="StringBuilder"/> object to append processed data from <paramref name="patch"/> to.</param>
		private void ParsePatch(MAPPatch patch, StringBuilder sb) {
			sb.Append("patchDef2\r\n{\r\n")
			.Append(patch.texture)
			.Append("\r\n( ")
			.Append(patch.dims.x)
			.Append(" ")
			.Append(patch.dims.y)
			.Append(" 0 0 0 )\r\n(\r\n");
			for (int i = 0; i < patch.dims.x; ++i) {
				sb.Append("( ");
				for (int j = 0; j < patch.dims.y; ++j) {
					UIVertex vertex = patch.points[((int)patch.dims.x * j) + i];
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
			sb.Append(")\r\n}\r\n");
		}

	}
}
