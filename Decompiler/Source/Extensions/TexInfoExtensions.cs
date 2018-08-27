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
			double SScale = 1.0 / texInfo.axes[0].magnitude;
			double TScale = 1.0 / texInfo.axes[1].magnitude;
			Vector3d sAxis = texInfo.axes[0].normalized;
			Vector3d tAxis = texInfo.axes[1].normalized;
			double sShift = texInfo.translation.x - (texInfo.axes[0] * worldPosition);
			double tShift = texInfo.translation.y - (texInfo.axes[1] * worldPosition);
			return new TextureInfo(sAxis, tAxis, new Vector2d(sShift, tShift), new Vector2d(SScale, TScale), 0, -1, 0);
		}

		/// <summary>
		/// Validates this <see cref="TextureInfo"/>. This will replace any <c>infinity</c> or <c>NaN</c>
		/// values with valid values to use.
		/// </summary>
		/// <param name="texInfo">The <see cref="TextureInfo"/> to validate.</param>
		/// <param name="plane">The <see cref="Plane"/> of the surface this <see cref="TextureInfo"/> is applied to.</param>
		public static void Validate(this TextureInfo texInfo, Plane plane) {
			if (Double.IsInfinity(texInfo.scale.x) || Double.IsNaN(texInfo.scale.x) || texInfo.scale.x == 0) {
				texInfo.scale = new Vector2d(1, texInfo.scale.y);
			}
			if (Double.IsInfinity(texInfo.scale.y) || Double.IsNaN(texInfo.scale.y) || texInfo.scale.y == 0) {
				texInfo.scale = new Vector2d(texInfo.scale.y, 1);
			}
			if (Double.IsInfinity(texInfo.translation.x) || Double.IsNaN(texInfo.translation.x)) {
				texInfo.translation = new Vector2d(0, texInfo.translation.y);
			}
			if (Double.IsInfinity(texInfo.translation.y) || Double.IsNaN(texInfo.translation.y)) {
				texInfo.translation = new Vector2d(texInfo.translation.x, 0);
			}
			if (Double.IsInfinity(texInfo.axes[0].x) || Double.IsNaN(texInfo.axes[0].x) || Double.IsInfinity(texInfo.axes[0].y) || Double.IsNaN(texInfo.axes[0].y) || Double.IsInfinity(texInfo.axes[0].z) || Double.IsNaN(texInfo.axes[0].z) || texInfo.axes[0] == Vector3d.zero) {
				texInfo.axes[0] = TextureInfo.TextureAxisFromPlane(plane)[0];
			}
			if (Double.IsInfinity(texInfo.axes[1].x) || Double.IsNaN(texInfo.axes[1].x) || Double.IsInfinity(texInfo.axes[1].y) || Double.IsNaN(texInfo.axes[1].y) || Double.IsInfinity(texInfo.axes[1].z) || Double.IsNaN(texInfo.axes[1].z) || texInfo.axes[1] == Vector3d.zero) {
				texInfo.axes[1] = TextureInfo.TextureAxisFromPlane(plane)[1];
			}
		}

	}
}
