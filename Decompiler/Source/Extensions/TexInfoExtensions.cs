using System;
using System.Collections.Generic;
using System.Linq;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Helper class containing methods for working with <see cref="TextureInfo"/> objects.
	/// </summary>
	public static class TexInfoExtensions {

		/// <summary>
		/// Converts a <see cref="TextureInfo"/> object from a BSP file into one that is usable in MAP files. 
		/// Texture axes will be normalized, and scaling will be stored separately.
		/// </summary>
		/// <param name="texInfo">This <see cref="TextureInfo"/>.</param>
		/// <param name="worldPosition">The world coordinates of the entity using this <see cref="TextureInfo"/>. Usually <c>Vector3d.zero</c>.</param>
		/// <returns>A <see cref="TextureInfo"/> object for use in MAP output.</returns>
		public static TextureInfo BSP2MAPTexInfo(this TextureInfo texInfo, Vector3d worldPosition) {
			// There's a lot of weird vector math going on here, don't try to understand it.
			// Suffice it to say, this is the tried-and-true method of getting what we need.
			double uScale = 1.0 / texInfo.uAxis.magnitude;
			double vScale = 1.0 / texInfo.vAxis.magnitude;
			Vector3d uAxis = texInfo.uAxis.normalized;
			Vector3d vAxis = texInfo.vAxis.normalized;
			double uTranslate = texInfo.translation.x - (texInfo.uAxis * worldPosition);
			double vTranslate = texInfo.translation.y - (texInfo.vAxis * worldPosition);
			return new TextureInfo(uAxis, vAxis, new Vector2d(uTranslate, vTranslate), new Vector2d(uScale, vScale), 0, -1, 0);
		}

		/// <summary>
		/// Validates this <see cref="TextureInfo"/>. This will replace any <c>infinity</c> or <c>NaN</c>
		/// values with valid values to use.
		/// </summary>
		/// <param name="texInfo">The <see cref="TextureInfo"/> to validate.</param>
		/// <param name="plane">The <see cref="Plane"/> of the surface this <see cref="TextureInfo"/> is applied to.</param>
		public static void Validate(this TextureInfo texInfo, Plane plane) {
			// Validate texture scaling
			if (double.IsInfinity(texInfo.scale.x) || double.IsNaN(texInfo.scale.x) || texInfo.scale.x == 0) {
				texInfo.scale = new Vector2d(1, texInfo.scale.y);
			}
			if (double.IsInfinity(texInfo.scale.y) || double.IsNaN(texInfo.scale.y) || texInfo.scale.y == 0) {
				texInfo.scale = new Vector2d(texInfo.scale.x, 1);
			}
			// Validate translations
			if (double.IsInfinity(texInfo.translation.x) || double.IsNaN(texInfo.translation.x)) {
				texInfo.translation = new Vector2d(0, texInfo.translation.y);
			}
			if (double.IsInfinity(texInfo.translation.y) || double.IsNaN(texInfo.translation.y)) {
				texInfo.translation = new Vector2d(texInfo.translation.x, 0);
			}
			// Validate axis components
			if (double.IsInfinity(texInfo.uAxis.x) || double.IsNaN(texInfo.uAxis.x) || double.IsInfinity(texInfo.uAxis.y) || double.IsNaN(texInfo.uAxis.y) || double.IsInfinity(texInfo.uAxis.z) || double.IsNaN(texInfo.uAxis.z) || texInfo.uAxis == Vector3d.zero) {
				texInfo.uAxis = TextureInfo.TextureAxisFromPlane(plane)[0];
			}
			if (double.IsInfinity(texInfo.vAxis.x) || double.IsNaN(texInfo.vAxis.x) || double.IsInfinity(texInfo.vAxis.y) || double.IsNaN(texInfo.vAxis.y) || double.IsInfinity(texInfo.vAxis.z) || double.IsNaN(texInfo.vAxis.z) || texInfo.vAxis == Vector3d.zero) {
				texInfo.vAxis = TextureInfo.TextureAxisFromPlane(plane)[1];
			}
			// Validate axes relative to plane ("Texture axis perpendicular to face")
			if (Math.Abs((texInfo.uAxis ^ texInfo.vAxis) * plane.normal) < 0.01) {
				Vector3d[] newAxes = TextureInfo.TextureAxisFromPlane(plane);
				texInfo.uAxis = newAxes[0];
				texInfo.vAxis = newAxes[1];
			}
		}

	}
}
