using System;
using System.Collections.Generic;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Static class containing helper functions for working with <see cref="Face"/> objects.
	/// </summary>
	public static class FaceExtensions {

		/// <summary>
		/// Creates a new <see cref="MAPBrush"/> object using this <see cref="Face"/>. The brush will simply have the
		/// face as its front, the edges will be extruded by <paramref name="depth"/> and will be textured with the
		/// "nodraw" texture, as well as the back.
		/// </summary>
		/// <param name="face">This <see cref="Face"/>.</param>
		/// <param name="bsp">The <see cref="BSP"/> object this <see cref="Face"/> is from.</param>
		/// <param name="depth">The desired depth of the resulting brush.</param>
		/// <returns>A <see cref="MAPBrush"/> object representing the passed <paramref name="face"/>.</returns>
		public static MAPBrush CreateBrush(this Face face, BSP bsp, float depth) {
			TextureInfo texInfo;
			string texture;
			if (face.textureInfo >= 0) {
				texInfo = bsp.texInfo[face.textureInfo];
				TextureData texData = bsp.texDatas[texInfo.texture];
				texture = bsp.textures.GetTextureAtOffset((uint)bsp.texTable[texData.stringTableIndex]);
			} else {
				Vector3d[] axes = TextureInfo.TextureAxisFromPlane(bsp.planes[face.plane]);
				texInfo = new TextureInfo(axes[0], 0, 1, axes[1], 0, 1, 0, -1);
				texture = "tools/toolsclip";
			}
			
			TextureInfo outputTexInfo = texInfo.BSP2MAPTexInfo(Vector3d.zero);

			// Turn vertices and edges into arrays of vectors
			Vector3d[] froms = new Vector3d[face.numEdges];
			Vector3d[] tos = new Vector3d[face.numEdges];
			for (int i = 0; i < face.numEdges; ++i) {
				if (bsp.surfEdges[face.firstEdge + i] > 0) {
					froms[i] = bsp.vertices[bsp.edges[(int)bsp.surfEdges[face.firstEdge + i]].firstVertex].position;
					tos[i] = bsp.vertices[bsp.edges[(int)bsp.surfEdges[face.firstEdge + i]].secondVertex].position;
				} else {
					tos[i] = bsp.vertices[bsp.edges[(int)bsp.surfEdges[face.firstEdge + i] * (-1)].firstVertex].position;
					froms[i] = bsp.vertices[bsp.edges[(int)bsp.surfEdges[face.firstEdge + i] * (-1)].secondVertex].position;
				}
			}

			return MAPBrushExtensions.CreateBrushFromWind(froms, tos, texture, "tools/toolsnodraw", outputTexInfo, depth);
		}

	}
}
