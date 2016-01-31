using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Static class containing helper functions for operating with <see cref="Brush"/> objects.
	/// </summary>
	public static class BrushExtensions {

		/// <summary>
		/// Determines if the <see cref="Brush.contents"/> of the passed <see cref="Brush"/> have the "detail" flag set.
		/// </summary>
		/// <param name="brush">This <see cref="Brush"/>.</param>
		/// <param name="version">The type of <see cref="BSP"/> the <paramref name="brush"/> is from.</param>
		/// <returns><c>true</c> if the <see cref="Brush.contents"/> indicate detail, <c>false</c> otherwise.</returns>
		public static bool IsDetail(this Brush brush, MapType version) {
			switch (version) {
				case MapType.Nightfire: {
					return ((brush.contents & (1 << 9)) != 0);
				}
				case MapType.Quake3:
				case MapType.Raven:
				case MapType.CoD:
				case MapType.CoD2:
				case MapType.CoD4:
				case MapType.STEF2:
				case MapType.STEF2Demo:
				case MapType.MOHAA:
				case MapType.FAKK:
				case MapType.Source17:
				case MapType.Source18:
				case MapType.Source19:
				case MapType.Source20:
				case MapType.Source21:
				case MapType.Source22:
				case MapType.Source23:
				case MapType.Source27:
				case MapType.DMoMaM:
				case MapType.Vindictus:
				case MapType.TacticalInterventionEncrypted:
				case MapType.SiN:
				case MapType.SoF:
				case MapType.Daikatana:
				case MapType.Quake2: {
					return ((brush.contents & (1 << 27)) != 0);
				}
			}
			return false;
		}

		/// <summary>
		/// Determines if the <see cref="Brush.contents"/> of the passed <see cref="Brush"/> have the "water" flag set.
		/// </summary>
		/// <param name="brush">This <see cref="Brush"/>.</param>
		/// <param name="version">The type of <see cref="BSP"/> the <paramref name="brush"/> is from.</param>
		/// <returns><c>true</c> if the <see cref="Brush.contents"/> indicate water, <c>false</c> otherwise.</returns>
		public static bool IsWater(this Brush brush, MapType version) {
			switch (version) {
				case MapType.Quake: {
					return brush.contents == -3;
				}
				case MapType.Nightfire: {
					return ((brush.contents & (1 << 20)) != 0);
				}
				case MapType.Quake3:
				case MapType.Raven:
				case MapType.CoD:
				case MapType.CoD2:
				case MapType.CoD4:
				case MapType.STEF2:
				case MapType.STEF2Demo:
				case MapType.MOHAA:
				case MapType.FAKK:
				case MapType.Source17:
				case MapType.Source18:
				case MapType.Source19:
				case MapType.Source20:
				case MapType.Source21:
				case MapType.Source22:
				case MapType.Source23:
				case MapType.Source27:
				case MapType.DMoMaM:
				case MapType.Vindictus:
				case MapType.TacticalInterventionEncrypted:
				case MapType.SiN:
				case MapType.SoF:
				case MapType.Daikatana:
				case MapType.Quake2: {
					return ((brush.contents & (1 << 5)) != 0);
				}
			}
			return false;
		}

	}
}
