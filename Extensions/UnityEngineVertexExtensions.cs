using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibBSP;

namespace BSPImporter {
#if UNITY_5_6_OR_NEWER
	using Vertex = UIVertex;
#endif

	/// <summary>
	/// Static class with extension methods for <see cref="Vertex"/> objects.
	/// </summary>
	public static class UnityEngineVertexExensions {

		/// <summary>
		/// Negates the V component of all UVs in a <see cref="Vertex"/>.
		/// </summary>
		/// <param name="vertex">The <see cref="Vertex"/> with UVs that will have V components negated.</param>
		/// <returns>The passed <see cref="Vertex"/> with UVs with negated V components.</returns>
		public static Vertex NegateVs(this Vertex vertex) {
			Vector2 uv0 = vertex.uv0;
			uv0.y = -uv0.y;
			vertex.uv0 = uv0;
			Vector2 uv1 = vertex.uv1;
			uv1.y = -uv1.y;
			vertex.uv1 = uv1;
			Vector2 uv2 = vertex.uv2;
			uv2.y = -uv2.y;
			vertex.uv2 = uv2;
			Vector2 uv3 = vertex.uv3;
			uv3.y = -uv3.y;
			vertex.uv3 = uv3;
			return vertex;
		}

		/// <summary>
		/// Scales this <see cref="Vertex"/> from inches to meters.
		/// </summary>
		/// <param name="v">This <see cref="Vertex"/>.</param>
		/// <returns>This <see cref="Vertex"/> scaled from inches to meters.</returns>
		public static Vertex ScaleInch2Meter(this Vertex v) {
			v.position = v.position.ScaleInch2Meter();
			return v;
		}

		/// <summary>
		/// Calculates the UVs of a <see cref="Vertex"/> from the data in <paramref name="texInfo"/>, <paramref name="matrix"/>
		/// and <paramref name="texture"/> and returns a <see cref="Vertex"/> with the UVs set.
		/// </summary>
		/// <param name="vertex">The <see cref="Vertex"/> to calculate UVs for.</param>
		/// <param name="texInfo">The <see cref="TextureInfo"/> defining the UV axes for this <see cref="Vertex"/>.</param>
		/// <param name="dims">The width and height of the texture applied on this <see cref="Vertex"/>'s surface.</param>
		/// <returns>The passed <see cref="Vertex"/> with UVs calculated.</returns>
		public static Vertex CalcUV(this Vertex vertex, TextureInfo texInfo, Vector2 dims) {
			Matrix4x4 matrix = texInfo.BuildTexMatrix().inverse;
			Vector3 textureCoord = matrix.MultiplyPoint3x4(vertex.position);
			vertex.uv0 = new Vector2((texInfo.UAxis.sqrMagnitude * textureCoord.x + texInfo.Translation.x) / dims.x,
			                         (texInfo.VAxis.sqrMagnitude * textureCoord.y + texInfo.Translation.y) / dims.y);
			return vertex;
		}

		/// <summary>
		/// Swaps the Y and Z components the position and normal of the passed <see cref="Vertex"/> using <see cref="Vector3Extensions.SwizzleYZ"/> and returns the result.
		/// </summary>
		/// <param name="vertex"><see cref="Vertex"/> to swizzle.</param>
		/// <returns>The passed <see cref="Vertex"/> with position and normal swizzled.</returns>
		public static Vertex SwizzleYZ(this Vertex vertex) {
			vertex.position = vertex.position.SwizzleYZ();
			vertex.normal = vertex.normal.SwizzleYZ();
			return vertex;
		}

	}
}
