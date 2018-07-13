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
			double sShift = texInfo.shifts[0] - (texInfo.axes[0] * worldPosition);
			double tShift = texInfo.shifts[1] - (texInfo.axes[1] * worldPosition);
			return new TextureInfo(sAxis, (float)sShift, (float)SScale, tAxis, (float)tShift, (float)TScale, 0, -1);
		}

	}
}
