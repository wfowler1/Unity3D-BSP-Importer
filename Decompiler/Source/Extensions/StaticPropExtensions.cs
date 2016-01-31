using System;
using System.Collections.Generic;
using System.Linq;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Helper class containing methods for working with <see cref="StaticProp"/> objects.
	/// </summary>
	public static class StaticPropExtensions {

		/// <summary>
		/// Parse the properties of this <see cref="StaticProp"/> into an <see cref="Entity"/> object.
		/// </summary>
		/// <param name="prop">This <see cref="StaticProp"/>.</param>
		/// <param name="dictionary">The model names dictionary from the Static Props lump.</param>
		/// <returns><see cref="Entity"/> representation of this <see cref="StaticProp"/>.</returns>
		public static Entity ToEntity(this StaticProp prop, IList<string> dictionary) {
			Entity entity = new Entity("prop_static");
			entity["model"] = dictionary[prop.dictionaryEntry];
			entity["skin"] = prop.skin.ToString();
			entity.origin = prop.origin;
			entity.angles = prop.angles;
			entity["solid"] = prop.solidity.ToString();
			entity["fademindist"] = prop.minFadeDist.ToString();
			entity["fademaxdist"] = prop.maxFadeDist.ToString();
			entity["fadescale"] = prop.forcedFadeScale.ToString();
			if (prop.targetname != null) {
				entity["targetname"] = prop.targetname;
			}
			return entity;
		}

	}
}
