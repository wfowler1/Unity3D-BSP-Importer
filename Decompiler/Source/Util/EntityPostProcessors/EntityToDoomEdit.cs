using System;
using System.Collections.Generic;
using System.Linq;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Class containing methods for parsing entities from various BSP formats into those for DoomEdit.
	/// Much of this is unimplemented for the moment, but this provides a framework to build upon.
	/// </summary>
	public class EntityToDoomEdit {

		private Job _master;

		private Entities _entities;
		private MapType _version;

		/// <summary>
		/// Creates a new instance of an <see cref="EntityToDoomEdit"/> object which will operate on the passed <see cref="Entities"/>.
		/// </summary>
		/// <param name="entities"The <see cref="Entities"/> to postprocess.</param>
		/// <param name="version">The <see cref="MapType"/> of the BSP the entities are from.</param>
		/// <param name="master">The parent <see cref="Job"/> object for this instance.</param>
		public EntityToDoomEdit(Entities entities, MapType version, Job master) {
			_entities = entities;
			_version = version;
			_master = master;
		}

		/// <summary>
		/// Processes every <see cref="Entity"/> in an <see cref="Entities"/> object to be used in a DoomEdit map.
		/// </summary>
		public void PostProcessEntities() {

			// We might modify the collection as we iterate over it. Can't use foreach.
			for (int i = 0; i < _entities.Count; ++i) {
				if (!_master.settings.noEntCorrection) {
					PostProcessEntity(_entities[i]);
				}
				if (!_master.settings.noTexCorrection) {
					PostProcessTextures(_entities[i].brushes);
				}
			}
		}

		/// <summary>
		/// Sends <paramref name="entity"/> to be postprocessed into the appropriate method based on version.
		/// </summary>
		/// <param name="entity"><see cref="Entity"/> to postprocess.</param>
		private void PostProcessEntity(Entity entity) {
			switch (_version) {
				default: {
					break;
				}
			}
		}

		/// <summary>
		/// Every <see cref="MAPBrushSide"/> contained in <paramref name="brushes"/> will have its texture examined,
		/// and, if necessary, replaced with the equivalent for DoomEdit.
		/// </summary>
		/// <param name="brushes">The collection of <see cref="MAPBrush"/> objects to have textures parsed.</param>
		/// <param name="version">The <see cref="MapType"/> of the BSP this entity came from.</param>
		private void PostProcessTextures(IEnumerable<MAPBrush> brushes) {
			foreach (MAPBrush brush in brushes) {
				foreach (MAPBrushSide brushSide in brush.sides) {
					ValidateTexInfo(brushSide);
					PostProcessSpecialTexture(brushSide);
					switch (_version) {
						case MapType.Source17:
						case MapType.Source18:
						case MapType.Source19:
						case MapType.Source20:
						case MapType.Source21:
						case MapType.Source22:
						case MapType.Source23:
						case MapType.Source27:
						case MapType.L4D2:
						case MapType.DMoMaM:
						case MapType.Vindictus:
						case MapType.TacticalInterventionEncrypted: {
							PostProcessSourceTexture(brushSide);
							break;
						}
					}
				}
			}
		}

		/// <summary>
		/// Validates the texture information in <paramref name="brushSide"/>. This will replace any <c>infinity</c> or <c>NaN</c>
		/// values with valid values to use.
		/// </summary>
		/// <param name="brushSide">The <see cref="MAPBrushSide"/> to validate texture information for.</param>
		private void ValidateTexInfo(MAPBrushSide brushSide) {
			if (Double.IsInfinity(brushSide.texScaleX) || Double.IsNaN(brushSide.texScaleX) || brushSide.texScaleX == 0) {
				brushSide.texScaleX = 1;
			}
			if (Double.IsInfinity(brushSide.texScaleY) || Double.IsNaN(brushSide.texScaleY) || brushSide.texScaleY == 0) {
				brushSide.texScaleY = 1;
			}
			if (Double.IsInfinity(brushSide.textureShiftS) || Double.IsNaN(brushSide.textureShiftS)) {
				brushSide.textureShiftS = 0;
			}
			if (Double.IsInfinity(brushSide.textureShiftT) || Double.IsNaN(brushSide.textureShiftT)) {
				brushSide.textureShiftT = 0;
			}
			if (Double.IsInfinity(brushSide.textureS.x) || Double.IsNaN(brushSide.textureS.x) || Double.IsInfinity(brushSide.textureS.y) || Double.IsNaN(brushSide.textureS.y) || Double.IsInfinity(brushSide.textureS.z) || Double.IsNaN(brushSide.textureS.z) || brushSide.textureS == Vector3d.zero) {
				brushSide.textureS = TextureInfo.TextureAxisFromPlane(brushSide.plane)[0];
			}
			if (Double.IsInfinity(brushSide.textureT.x) || Double.IsNaN(brushSide.textureT.x) || Double.IsInfinity(brushSide.textureT.y) || Double.IsNaN(brushSide.textureT.y) || Double.IsInfinity(brushSide.textureT.z) || Double.IsNaN(brushSide.textureT.z) || brushSide.textureT == Vector3d.zero) {
				brushSide.textureT = TextureInfo.TextureAxisFromPlane(brushSide.plane)[1];
			}
		}

		/// <summary>
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by GTKRadiant, if necessary.
		/// These textures are produced by the decompiler algorithm itself.
		/// </summary>
		/// <param name="brushSide">The <see cref="MAPBrushSide"/> to have its texture parsed.</param>
		private void PostProcessSpecialTexture(MAPBrushSide brushSide) {
			switch (brushSide.texture.ToLower()) {
				case "**skiptexture**":
				case "**nulltexture**": {
					brushSide.texture = "textures/common/nodraw";
					break;
				}
				case "**skytexture**": {
					brushSide.texture = "textures/common/caulk";
					break;
				}
				case "**hinttexture**": {
					brushSide.texture = "textures/editor/visportal";
					break;
				}
				case "**cliptexture**": {
					brushSide.texture = "textures/common/clip";
					break;
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by DoomEdit, if necessary.
		/// </summary>
		/// <param name="brushSide">The <see cref="MAPBrushSide"/> to have its texture parsed.</param>
		private void PostProcessSourceTexture(MAPBrushSide brushSide) {
			if (brushSide.texture.Length >= 5 && brushSide.texture.Substring(0, 5).Equals("maps/", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = brushSide.texture.Substring(5);
				for (int i = 0; i < brushSide.texture.Length; ++i) {
					if (brushSide.texture[i] == '/') {
						brushSide.texture = brushSide.texture.Substring(i + 1);
						break;
					}
				}
			}

			switch (brushSide.texture.ToLower()) {
				case "tools/toolsinvisible":
				case "tools/toolsplayerclip":
				case "tools/toolsclip": {
					brushSide.texture = "textures/common/clip";
					break;
				}
				case "tools/toolstrigger":
				case "tools/toolsfog": {
					brushSide.texture = "textures/common/trigonce";
					break;
				}
				case "tools/toolsskip":
				case "tools/toolsnodraw": {
					brushSide.texture = "textures/common/nodraw";
					break;
				}
				case "tools/toolshint": {
					brushSide.texture = "textures/editor/visportal";
					break;
				}
				case "tools/toolsnpcclip": {
					brushSide.texture = "textures/common/monster_clip";
					break;
				}
			}

			// Parse cubemap textures
			// I'm sure this could be done more concisely with regex, but I suck at regex.
			int numUnderscores = 0;
			bool validnumber = false;
			for (int i = brushSide.texture.Length - 1; i > 0; --i) {
				if (brushSide.texture[i] <= '9' && brushSide.texture[i] >= '0') {
					// Current is a number, this may be a cubemap reference
					validnumber = true;
				} else {
					if (brushSide.texture[i] == '-') {
						// Current is a minus sign (-).
						if (!validnumber) {
							break; // Make sure there's a number to add the minus sign to. If not, kill the loop.
						}
					} else {
						if (brushSide.texture[i] == '_') {
							// Current is an underscore (_)
							if (validnumber) {
								// Make sure there is a number in the current string
								++numUnderscores; // before moving on to the next one.
								if (numUnderscores == 3) {
									// If we've got all our numbers
									brushSide.texture = brushSide.texture.Substring(0, i); // Cut the texture string
									break; // Kill the loop, we're done
								}
								validnumber = false;
							} else {
								// No number after the underscore
								break;
							}
						} else {
							// Not an acceptable character
							break;
						}
					}
				}
			}

		}

	}
}
