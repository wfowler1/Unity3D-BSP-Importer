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
		/// Converts a <see cref="TextureInfo"/> object from a BSP file into one that is usable in MAP files. The <c>out</c>
		/// parameters are the texture scalars, since the axes will be normalized.
		/// </summary>
		/// <param name="texInfo">This <see cref="TextureInfo"/>.</param>
		/// <param name="worldPosition">The world coordinates of the entity using this <see cref="TextureInfo"/>. Usually <c>Vector3d.zero</c>.</param>
		/// <param name="SScale"><c>out</c> parameter that will contain the scale of the texture along the S axis.</param>
		/// <param name="TScale"><c>out</c> parameter that will contain the scale of the texture along the T axis.</param>
		/// <returns>A <see cref="TextureInfo"/> object for use in MAP output.</returns>
		public static TextureInfo BSP2MAPTexInfo(this TextureInfo texInfo, Vector3d worldPosition, out double SScale, out double TScale) {
			// There's a lot of weird vector math going on here, don't try to understand it.
			// Suffice it to say, this is the tried-and-true method of getting what we need.
			SScale = 1.0 / texInfo.axes[0].magnitude;
			TScale = 1.0 / texInfo.axes[1].magnitude;
			Vector3d sAxis = texInfo.axes[0].normalized;
			Vector3d tAxis = texInfo.axes[1].normalized;
			double sShift = texInfo.shifts[0] - (texInfo.axes[0] * worldPosition);
			double tShift = texInfo.shifts[1] - (texInfo.axes[1] * worldPosition);
			return new TextureInfo(sAxis, (float)sShift, tAxis, (float)tShift, 0, -1);
		}

	}
}
