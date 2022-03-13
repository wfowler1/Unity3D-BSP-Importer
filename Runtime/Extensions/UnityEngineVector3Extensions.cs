using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BSPImporter {

	/// <summary>
	/// Static class with extension methods for <see cref="Vector3"/> objects.
	/// </summary>
	public static class UnityEngineVector3Extensions {

		/// <summary>
		/// Swaps the Y and Z coordinates of a Vector, converting between Quake's Z-Up coordinates to Unity's Y-Up.
		/// </summary>
		/// <param name="v">This <see cref="Vector3"/>.</param>
		/// <returns>The "swizzled" Vector.</returns>
		public static Vector3 SwizzleYZ(this Vector3 v) {
			return new Vector3(v.x, v.z, v.y);
		}

		/// <summary>
		/// Scales this <see cref="Vector3"/> from inches to meters.
		/// </summary>
		/// <param name="v">This <see cref="Vector3"/>.</param>
		/// <returns>This <see cref="Vector3"/> scaled from inches to meters.</returns>
		public static Vector3 ScaleInch2Meter(this Vector3 v) {
			return v * MeshUtils.inch2MeterScale;
		}

	}
}
