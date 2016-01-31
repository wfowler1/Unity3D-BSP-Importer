using System;
using System.Collections.Generic;
using System.Linq;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Static class containing helper methods for working with <see cref="Cubemap"/> objects.
	/// </summary>
	public static class CubemapExtensions {

		/// <summary>
		/// Parse the properties of this <see cref="Cubemap"/> into an <see cref="Entity"/> object.
		/// </summary>
		/// <param name="prop">This <see cref="Cubemap"/>.</param>
		/// <returns><see cref="Entity"/> representation of this <see cref="Cubemap"/>.</returns>
		public static Entity ToEntity(this Cubemap cubemap) {
			Entity entity = new Entity("env_cubemap");
			entity["origin"] = cubemap.origin.x + " " + cubemap.origin.y + " " + cubemap.origin.z;
			entity["cubemapsize"] = cubemap.size + "";
			return entity;
		}

	}
}
