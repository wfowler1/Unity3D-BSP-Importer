using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibBSP;

namespace BSPImporter {
#if UNITY_5_6_OR_NEWER
	using Vertex = UIVertex;
#endif

	/// <summary>
	/// Static class with helper utilities and extension methods for <see cref="BSP"/> objects.
	/// </summary>
	public static class BSPExtensions {

		/// <summary>
		/// Gets all <see cref="Leaf"/> objects referenced by this <see cref="Model"/>.
		/// </summary>
		/// <param name="bsp">This <see cref="BSP"/>.</param>
		/// <param name="model">The <see cref="Model"/> to get all <see cref="Leaf"/> objects from.</param>
		/// <returns>A <see cref="List{T}"/>&lt;<see cref="Leaf"/>&gt; containing all <see cref="Leaf"/> objects referenced by <paramref name="model"/>.</returns>
		public static List<Leaf> GetLeavesInModel(this BSP bsp, Model model) {
			List<Leaf> result = null;
			if (model.FirstLeafIndex < 0) {
				if (model.HeadNodeIndex >= 0) {
					result = bsp.GetLeavesInNode(bsp.nodes[model.HeadNodeIndex]);
				}
			} else {
				result = new List<Leaf>(model.NumLeaves);
				for (int i = 0; i < model.NumLeaves; i++) {
					result.Add(bsp.leaves[model.FirstLeafIndex + i]);
				}
			}
			return result;
		}

		/// <summary>
		/// Gets all <see cref="Leaf"/> objects referenced by this <see cref="Node"/>, recursively.
		/// </summary>
		/// <param name="bsp">This <see cref="BSP"/>.</param>
		/// <param name="node">The <see cref="Node"/> to get all <see cref="Leaf"/> descendants from, recursing through the BSP tree.</param>
		/// <returns>A <see cref="List{T}"/>&lt;<see cref="Leaf"/>&gt; containing all <see cref="Leaf"/> objects descended from <paramref name="node"/>.</returns>
		public static List<Leaf> GetLeavesInNode(this BSP bsp, Node node) {
			List<Leaf> nodeLeaves = new List<Leaf>();
			Stack<Node> nodestack = new Stack<Node>();
			nodestack.Push(node);

			Node currentNode;

			while (!(nodestack.Count == 0)) {
				currentNode = nodestack.Pop();
				int right = currentNode.Child2Index;
				if (right >= 0) {
					nodestack.Push(bsp.nodes[right]);
				} else {
					nodeLeaves.Add(bsp.leaves[(right * (-1)) - 1]);
				}
				int left = currentNode.Child1Index;
				if (left >= 0) {
					nodestack.Push(bsp.nodes[left]);
				} else {
					nodeLeaves.Add(bsp.leaves[(left * (-1)) - 1]);
				}
			}
			return nodeLeaves;
		}

		/// <summary>
		/// Gets all <see cref="Face"/> objects referenced by this <see cref="Model"/>, recursively.
		/// </summary>
		/// <param name="bsp">This <see cref="BSP"/>.</param>
		/// <param name="model">The <see cref="Model"/> to get all <see cref="Face"/> objects from. Depending on <see cref="BSP.version"/> this may need to traverse through <see cref="BSP.leaves"/>.</param>
		/// <returns>A <see cref="List{T}"/>&lt;<see cref="Face"/>&gt; containing all <see cref="Face"/> objects descended from <paramref name="model"/>.</returns>
		public static List<Face> GetFacesInModel(this BSP bsp, Model model) {
			List<Face> result = null;
			if (model.FirstFaceIndex >= 0) {
				if (result == null) {
					result = new List<Face>();
				}
				for (int i = 0; i < model.NumFaces; i++) {
					result.Add(bsp.faces[model.FirstFaceIndex + i]);
				}
			} else {
				bool[] faceUsed = new bool[bsp.faces.Count];
				List<Leaf> leaves = bsp.GetLeavesInModel(model);
				foreach (Leaf leaf in leaves) {
					if (leaf.FirstMarkFaceIndex >= 0) {
						if (result == null) {
							result = new List<Face>();
						}
						for (int i = 0; i < leaf.NumMarkFaceIndices; i++) {
							int currentFace = (int)bsp.markSurfaces[leaf.FirstMarkFaceIndex + i];
							if (!faceUsed[currentFace]) {
								faceUsed[currentFace] = true;
								result.Add(bsp.faces[currentFace]);
							}
						}
					}
				}
			}
			return result;
		}
		
		/// <summary>
		/// Gets the index of the <see cref="TextureData"/> which uses <paramref name="texture"/>, for Source engine only.
		/// </summary>
		/// <param name="bsp">This <see cref="BSP"/>.</param>
		/// <param name="texture">Name of the texture to get the <see cref="TextureData"/> index for.</param>
		/// <returns>Index of the <see cref="TextureData"/> used for <paramref name="texture"/>, or <c>-1</c> if it was not found.</returns>
		public static int FindTexDataWithTexture(this BSP bsp, string texture) {
			for (int i = 0; i < bsp.texDatas.Count; i++) {
				string temp = bsp.textures.GetTextureAtOffset((uint)bsp.texTable[bsp.texDatas[i].TextureStringOffsetIndex]);
				if (temp.Equals(texture)) {
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Gets the index of the <see cref="Texture"/> used by <paramref name="face"/>.
		/// </summary>
		/// <param name="bsp">This <see cref="BSP"/>.</param>
		/// <param name="face">The <see cref="Face"/> to get the texture index for.</param>
		/// <returns>Index of the <see cref="Texture"/> used for <paramref name="texture"/>, or <c>-1</c> if it was not found.</returns>
		public static int GetTextureIndex(this BSP bspObject, Face face) {
			if (face.TextureIndex >= 0) {
				return face.TextureIndex;
			} else {
				if (face.TextureInfoIndex > 0) {
					if (bspObject.texDatas != null) {
						return bspObject.texDatas[bspObject.texInfo[face.TextureInfoIndex].TextureIndex].TextureStringOffsetIndex;
					} else {
						return bspObject.texInfo[face.TextureInfoIndex].TextureIndex;
					}
				}
			}
			return -1;
		}

		/// <summary>
		/// Gets the <see cref="TextureInfo"/> for the passed <see cref="Face"/>.
		/// </summary>
		/// <param name="bsp">This <see cref="BSP"/>.</param>
		/// <param name="face">The <see cref="Face"/> object to get the appropriate <see cref="TextureInfo"/> for.</param>
		/// <returns>The appropriate <see cref="TextureInfo"/> for <paramref name="face"/>.</returns>
		public static TextureInfo GetTextureInfo(this BSP bsp, Face face) {
			if (face.TextureIndex >= 0 && bsp.textures[face.TextureIndex].TextureInfo.Data != null && bsp.textures[face.TextureIndex].TextureInfo.Data.Length > 0) {
				return bsp.textures[face.TextureIndex].TextureInfo;
			}

			if (face.TextureInfoIndex >= 0) {
				return bsp.texInfo[face.TextureInfoIndex];
			}

			return new TextureInfo();
		}

	}
}
