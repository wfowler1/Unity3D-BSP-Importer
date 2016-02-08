using System;
using System.Collections.Generic;
using System.Linq;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Class containing methods for parsing entities from various BSP formats into those for MOHRadiant.
	/// </summary>
	public class EntityToMoHRadiant {

		private Job _master;

		private Entities _entities;
		private MapType _version;

		/// <summary>
		/// Creates a new instance of an <see cref="EntityToMOHRadiant"/> object which will operate on the passed <see cref="Entities"/>.
		/// </summary>
		/// <param name="entities"The <see cref="Entities"/> to postprocess.</param>
		/// <param name="version">The <see cref="MapType"/> of the BSP the entities are from.</param>
		/// <param name="master">The parent <see cref="Job"/> object for this instance.</param>
		public EntityToMoHRadiant(Entities entities, MapType version, Job master) {
			_entities = entities;
			_version = version;
			_master = master;
		}

		/// <summary>
		/// Processes every <see cref="Entity"/> in an <see cref="Entities"/> object to be used in a MOHRadiant map.
		/// </summary>
		public void PostProcessEntities() {
			// There should really only be one of these. But someone might have screwed with the map...
			List<Entity> worldspawns = _entities.FindAll(entity => { return entity.className.Equals("worldspawn", StringComparison.InvariantCultureIgnoreCase); });

			// TODO: This is awful. Let's rework the enum to have internal ways to check engine forks.
			if (_version != MapType.MOHAA) {
				// Make sure all water brushes currently in the worldspawn get converted to Source.
				foreach (Entity worldspawn in worldspawns) {
					foreach (MAPBrush brush in worldspawn.brushes) {
						if (brush.isWater) {
							ConvertToWater(brush);
						}
					}
				}
				// Make sure all func_water entities get converted to Source.
				List<Entity> waters = _entities.FindAll(entity => { return entity.className.Equals("func_water", StringComparison.InvariantCultureIgnoreCase); });
				if (waters.Any()) {
					// Parse water entities into just water brushes
					foreach (Entity water in waters) {
						ParseWaterIntoWorld(worldspawns[0], water);
						_entities.Remove(water);
					}
				}
			}

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
		/// Goes through each <see cref="MAPBrush"/> in <paramref name="water"/>, converts it to a water brush for Source,
		/// and adds the <see cref="MAPBrush"/> to <paramref name="world"/>.
		/// </summary>
		/// <param name="world">The world <see cref="Entity"/>.</param>
		/// <param name="water">A water <see cref="Entity"/>.</param>
		private void ParseWaterIntoWorld(Entity world, Entity water) {
			foreach (MAPBrush brush in water.brushes) {
				ConvertToWater(brush);
				world.brushes.Add(brush);
			}
		}

		/// <summary>
		/// For <paramref name="brush"/>, sets the top <see cref="MAPBrushSide"/>'s texture
		/// to a water texture and sets all others to nodraw.
		/// </summary>
		/// <param name="brush">The <see cref="MAPBrush"/> to make into a water brush.</param>
		private void ConvertToWater(MAPBrush brush) {
			foreach (MAPBrushSide side in brush.sides) {
				side.texture = "test/omaha_pjspick5";
			}
		}

		/// <summary>
		/// Sends <paramref name="entity"/> to be postprocessed into the appropriate method based on version.
		/// </summary>
		/// <param name="entity"><see cref="Entity"/> to postprocess.</param>
		private void PostProcessEntity(Entity entity) {
			switch (_version) {
				case MapType.Nightfire: {
					PostProcessNightfireEntity(entity);
					break;
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert an <see cref="Entity"/> from a Nightfire BSP to one for MOHRadiant.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to parse.</param>
		private void PostProcessNightfireEntity(Entity entity) {
			switch (entity.className.ToLower()) {
				case "func_door_rotating": {
					entity["classname"] = "func_rotatingdoor";
					break;
				}
				case "worldspawn": {
					entity.Remove("mapversion");
					break;
				}
			}
		}

		/// <summary>
		/// Every <see cref="MAPBrushSide"/> contained in <paramref name="brushes"/> will have its texture examined,
		/// and, if necessary, replaced with the equivalent for MOHRadiant.
		/// </summary>
		/// <param name="brushes">The collection of <see cref="MAPBrush"/> objects to have textures parsed.</param>
		/// <param name="version">The <see cref="MapType"/> of the BSP this entity came from.</param>
		private void PostProcessTextures(IEnumerable<MAPBrush> brushes) {
			foreach (MAPBrush brush in brushes) {
				foreach (MAPBrushSide brushSide in brush.sides) {
					ValidateTexInfo(brushSide);
					switch (_version) {
						case MapType.Nightfire: {
							PostProcessNightfireTexture(brushSide);
							break;
						}
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
						case MapType.TacticalInterventionEncrypted: {
							PostProcessSourceTexture(brushSide);
							break;
						}
						case MapType.Quake3:
						case MapType.MOHAA:
						case MapType.CoD:
						case MapType.STEF2:
						case MapType.STEF2Demo:
						case MapType.Raven:
						case MapType.FAKK: {
							PostProcessQuake3Texture(brushSide);
							break;
						}
					}
				}
				if (brush.patch != null) {
					PostProcessQuake3Texture(brush.patch);
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
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by MOHRadiant, if necessary.
		/// </summary>
		/// <param name="brushSide">The <see cref="MAPBrushSide"/> to have its texture parsed.</param>
		private void PostProcessNightfireTexture(MAPBrushSide brushSide) {
			switch (brushSide.texture.ToLower()) {
				case "special/nodraw":
				case "special/null": {
					brushSide.texture = "common/nodraw";
					break;
				}
				case "special/clip": {
					brushSide.texture = "common/clip";
					break;
				}
				case "special/sky": {
					brushSide.texture = "common/skyportal";
					break;
				}
				case "special/trigger": {
					brushSide.texture = "common/trigger";
					break;
				}
				case "special/playerclip": {
					brushSide.texture = "common/playerclip";
					break;
				}
				case "special/hint": {
					brushSide.texture = "common/hint";
					break;
				}
				case "special/skip": {
					brushSide.texture = "common/skip";
					break;
				}
				case "special/npcclip":
				case "special/enemyclip": {
					brushSide.texture = "common/tankclip";
					break;
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by MOHRadiant, if necessary.
		/// </summary>
		/// <param name="brushSide">The <see cref="MAPBrushSide"/> to have its texture parsed.</param>
		private void PostProcessQuake3Texture(MAPBrushSide brushSide) {
			if (brushSide.texture.Length >= 9 && brushSide.texture.Substring(0, 9).Equals("textures/", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = brushSide.texture.Substring(9);
			}
		}

		/// <summary>
		/// Postprocesser to convert the texture referenced by <paramref name="patch"/> into one used by MOHRadiant, if necessary.
		/// </summary>
		/// <param name="patch">The <see cref="MAPPatch"/> to have its texture parsed.</param>
		private void PostProcessQuake3Texture(MAPPatch patch) {
			if (patch.texture.Length >= 9 && patch.texture.Substring(0, 9).Equals("textures/", StringComparison.InvariantCultureIgnoreCase)) {
				patch.texture = patch.texture.Substring(9);
			}
		}

		/// <summary>
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by MOHRadiant, if necessary.
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
				case "tools/toolshint": {
					brushSide.texture = "common/hint";
					break;
				}
				case "tools/toolsskip": {
					brushSide.texture = "common/skip";
					break;
				}
				case "tools/toolsinvisible":
				case "tools/toolsplayerclip":
				case "tools/toolsclip": {
					brushSide.texture = "common/clip";
					break;
				}
				case "tools/toolstrigger":
				case "tools/toolsfog": {
					brushSide.texture = "common/trigger";
					break;
				}
				case "tools/toolsskybox": {
					brushSide.texture = "common/skyportal";
					break;
				}
				case "tools/toolsnodraw": {
					brushSide.texture = "common/nodraw";
					break;
				}
				case "tools/toolsnpcclip": {
					brushSide.texture = "common/tankclip";
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
