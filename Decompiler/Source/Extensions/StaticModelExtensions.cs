using System;
using System.Collections.Generic;
using System.Linq;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Helper class containing methods for working with <see cref="StaticModel"/> objects.
	/// </summary>
	public static class StaticModelExtensions {

		/// <summary>
		/// Parse the properties of this <see cref="StaticModel"/> into an <see cref="Entity"/> object.
		/// </summary>
		/// <param name="model">This <see cref="StaticModel"/>.</param>
		/// <returns><see cref="Entity"/> representation of this <see cref="StaticModel"/>.</returns>
		public static Entity ToEntity(this StaticModel model) {
			Entity entity = new Entity("static_model");
			entity["model"] = model.name;
			entity.origin = model.origin;
			entity.angles = model.angles;
			entity["scale"] = model.scale.ToString();
			entity["angle"] = "0";
			return entity;
		}

	}
}
