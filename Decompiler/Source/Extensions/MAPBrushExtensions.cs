using System;
using System.Collections.Generic;
using System.Linq;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Static class containing helper functions for working with <see cref="MAPBrush"/> objects.
	/// </summary>
	public static class MAPBrushExtensions {

		/// <summary>
		/// Moves this <see cref="MAPBrush"/> object in the world by the vector <paramref name="v"/>.
		/// </summary>
		/// <param name="mapBrush">This <see cref="MAPBrush"/>.</param>
		/// <param name="v">Translation vector.</param>
		public static void Translate(this MAPBrush mapBrush, Vector3d v) {
			foreach (MAPBrushSide side in mapBrush.sides) {
				side.Translate(v);
			}
		}

		/// <summary>
		/// Creates a <see cref="MAPBrush"/> using <paramref name="froms"/> and <paramref name="tos"/> as a list of edges that create a "winding" in clockwise order.
		/// </summary>
		/// <param name="froms">A list of the "From" vertices. This should match one-to-one with the <paramref name="tos"/> list.</param>
		/// <param name="tos">A list of the "to" vertices. This should match one-to-one with the <paramref name="froms"/> list.</param>
		/// <param name="texture">The texture to use on the front of this brush.</param>
		/// <param name="backtex">The texture to use on the sides and back of this brush.</param>
		/// <param name="texInfo">The texture axis information to be used on the front of this brush.</param>
		/// <param name="xScale">The scale of the texture along the S axis.</param>
		/// <param name="yScale">The scale of the texture along the T axis.</param>
		/// <param name="depth">The desired depth of the brush, how far the back should extend from the front.</param>
		/// <returns>A <see cref="MAPBrush"/> object created using the passed vertices and texture information.</returns>
		public static MAPBrush CreateBrushFromWind(IList<Vector3d> froms, IList<Vector3d> tos, string texture, string backtex, TextureInfo texInfo, double xScale, double yScale, float depth) {
			Vector3d[] planepts = new Vector3d[3];
			List<MAPBrushSide> sides = new List<MAPBrushSide>(froms.Count + 2); // Each edge, plus a front and back side

			planepts[0] = froms[0];
			planepts[1] = tos[0];
			planepts[2] = tos[1];
			Plane plane = new Plane(planepts);
			sides.Add(new MAPBrushSide() {
				vertices = new Vector3d[] { planepts[0], planepts[1], planepts[2] },
				plane = plane,
				texture = texture,
				textureS = texInfo.axes[0],
				textureShiftS = texInfo.shifts[0],
				textureT = texInfo.axes[1],
				textureShiftT = texInfo.shifts[1],
				texRot = 0,
				texScaleX = xScale,
				texScaleY = yScale,
				flags = 0,
				material = "wld_lightmap",
				lgtScale = 16,
				lgtRot = 0
			});

			Vector3d reverseNormal = -plane.normal;

			planepts[0] = froms[0] + (reverseNormal * depth);
			planepts[1] = tos[1] + (reverseNormal * depth);
			planepts[2] = tos[0] + (reverseNormal * depth);
			Plane backplane = new Plane(planepts);
			Vector3d[] generatedAxes = TextureInfo.TextureAxisFromPlane(backplane);
			sides.Add(new MAPBrushSide() {
				vertices = new Vector3d[] { planepts[0], planepts[1], planepts[2] },
				plane = backplane,
				texture = backtex,
				textureS = generatedAxes[0],
				textureShiftS = 0,
				textureT = generatedAxes[1],
				textureShiftT = 0,
				texRot = 0,
				texScaleX = 1,
				texScaleY = 1,
				flags = 0,
				material = "wld_lightmap",
				lgtScale = 16,
				lgtRot = 0
			});

			// For each edge
			for (int i = 0; i < froms.Count; ++i) {
				planepts[0] = froms[i];
				planepts[1] = froms[i] + (reverseNormal * depth);
				planepts[2] = tos[i];
				Plane sideplane = new Plane(planepts);
				generatedAxes = TextureInfo.TextureAxisFromPlane(sideplane);
				sides.Add(new MAPBrushSide() {
					vertices = new Vector3d[] { planepts[0], planepts[1], planepts[2] },
					plane = sideplane,
					texture = backtex,
					textureS = generatedAxes[0],
					textureShiftS = 0,
					textureT = generatedAxes[1],
					textureShiftT = 0,
					texRot = 0,
					texScaleX = 1,
					texScaleY = 1,
					flags = 0,
					material = "wld_lightmap",
					lgtScale = 16,
					lgtRot = 0
				});
			}

			return new MAPBrush() {
				sides = sides
			};
		}

		/// <summary>
		/// Creates an axis-aligned cubic brush with bounds from <paramref name="mins"/> to <paramref name="maxs"/>.
		/// </summary>
		/// <param name="mins">The minimum extents of the new brush.</param>
		/// <param name="maxs">The maximum extents of the new brush.</param>
		/// <param name="texture">The texture to use on this brush.</param>
		/// <returns>The resulting <see cref="MAPBrush"/> object.</returns>
		public static MAPBrush CreateCube(Vector3d mins, Vector3d maxs, string texture) {
			MAPBrush newBrush = new MAPBrush();
			Vector3d[][] planes = new Vector3d[6][];
			for (int i = 0; i < 6; ++i) {
				planes[i] = new Vector3d[3];
			} // Six planes for a cube brush, three vertices for each plane
			double[][] textureS = new double[6][];
			for (int i = 0; i < 6; ++i) {
				textureS[i] = new double[3];
			}
			double[][] textureT = new double[6][];
			for (int i = 0; i < 6; ++i) {
				textureT[i] = new double[3];
			}
			// The planes and their texture scales
			// I got these from an origin brush created by Gearcraft. Don't worry where these numbers came from, they work.
			// Top
			planes[0][0] = new Vector3d(mins.x, maxs.y, maxs.z);
			planes[0][1] = new Vector3d(maxs.x, maxs.y, maxs.z);
			planes[0][2] = new Vector3d(maxs.x, mins.y, maxs.z);
			textureS[0][0] = 1;
			textureT[0][1] = -1;
			// Bottom
			planes[1][0] = new Vector3d(mins.x, mins.y, mins.z);
			planes[1][1] = new Vector3d(maxs.x, mins.y, mins.z);
			planes[1][2] = new Vector3d(maxs.x, maxs.y, mins.z);
			textureS[1][0] = 1;
			textureT[1][1] = -1;
			// Left
			planes[2][0] = new Vector3d(mins.x, maxs.y, maxs.z);
			planes[2][1] = new Vector3d(mins.x, mins.y, maxs.z);
			planes[2][2] = new Vector3d(mins.x, mins.y, mins.z);
			textureS[2][1] = 1;
			textureT[2][2] = -1;
			// Right
			planes[3][0] = new Vector3d(maxs.x, maxs.y, mins.z);
			planes[3][1] = new Vector3d(maxs.x, mins.y, mins.z);
			planes[3][2] = new Vector3d(maxs.x, mins.y, maxs.z);
			textureS[3][1] = 1;
			textureT[3][2] = -1;
			// Near
			planes[4][0] = new Vector3d(maxs.x, maxs.y, maxs.z);
			planes[4][1] = new Vector3d(mins.x, maxs.y, maxs.z);
			planes[4][2] = new Vector3d(mins.x, maxs.y, mins.z);
			textureS[4][0] = 1;
			textureT[4][2] = -1;
			// Far
			planes[5][0] = new Vector3d(maxs.x, mins.y, mins.z);
			planes[5][1] = new Vector3d(mins.x, mins.y, mins.z);
			planes[5][2] = new Vector3d(mins.x, mins.y, maxs.z);
			textureS[5][0] = 1;
			textureT[5][2] = -1;

			for (int i = 0; i < 6; i++) {
				MAPBrushSide currentSide = new MAPBrushSide() {
					vertices = planes[i],
					plane = new Plane(planes[i]),
					texture = texture,
					textureS = new Vector3d(textureS[i]),
					textureShiftS = 0,
					textureT = new Vector3d(textureT[i]),
					textureShiftT = 0,
					texRot = 0,
					texScaleX = 1,
					texScaleY = 1,
					flags = 0,
					material = "wld_lightmap",
					lgtScale = 16,
					lgtRot = 0
				};
				newBrush.sides.Add(currentSide);
			}
			return newBrush;
		}

	}
}
