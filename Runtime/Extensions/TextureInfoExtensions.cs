using UnityEngine;
using LibBSP;

namespace BSPImporter {

	/// <summary>
	/// Static class with extension methods for <see cref="TextureInfo"/> objects.
	/// </summary>
	public static class TextureInfoExtensions {
		
		/// <summary>
		/// Builds a the texture transform matrix for <paramref name="textureInfo"/>.
		/// </summary>
		/// <param name="textureInfo">This <see cref="TextureInfo"/>.</param>
		/// <returns>The texture transform matrix for <paramref name="textureInfo"/>.</returns>
		public static Matrix4x4 BuildTexMatrix(this TextureInfo textureInfo) {
			Vector3 scaledUAxis = textureInfo.UAxis.SwizzleYZ().ScaleInch2Meter();
			Vector3 scaledVAxis = textureInfo.VAxis.SwizzleYZ().ScaleInch2Meter();
			Vector3 STNormal = Vector3.Cross(scaledUAxis, scaledVAxis);
			Matrix4x4 texmatrix = Matrix4x4.identity;
			texmatrix[0, 0] = scaledUAxis.x;
			texmatrix[0, 1] = scaledVAxis.x;
			texmatrix[0, 2] = STNormal.x;
			texmatrix[1, 0] = scaledUAxis.y;
			texmatrix[1, 1] = scaledVAxis.y;
			texmatrix[1, 2] = STNormal.y;
			texmatrix[2, 0] = scaledUAxis.z;
			texmatrix[2, 1] = scaledVAxis.z;
			texmatrix[2, 2] = STNormal.z;
			return texmatrix;
		}

		/// <summary>
		/// Calculates the UV at the given <paramref name="transformVertex"/> using this <see cref="TextureInfo"/>.
		/// </summary>
		/// <param name="textureInfo">This <see cref="TextureInfo"/>.</param>
		/// <param name="transformVertex">The vertex to calculate the UV coordinate for.</param>
		/// <param name="dims">The width and height of the <see cref="Texture2D"/> to be used on this face.</param>
		/// <returns>The UV coordinates at <paramref name="transformVertex"/> projected from this <see cref="TextureInfo"/>.</returns>
		public static Vector2 CalculateUV(this TextureInfo textureInfo, Vector3 transformVertex, Vector2 dims) {
			return new Vector2(
				(textureInfo.UAxis.sqrMagnitude * transformVertex.x + textureInfo.Translation.x) / dims.x,
				(textureInfo.VAxis.sqrMagnitude * transformVertex.y + textureInfo.Translation.y) / dims.y
			);
		}

	}
}
