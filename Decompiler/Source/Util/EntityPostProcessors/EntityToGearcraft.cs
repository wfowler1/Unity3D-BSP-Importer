using System;
using System.Collections.Generic;
using System.Linq;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Class containing methods for parsing entities from various BSP formats into those for Gearcraft.
	/// </summary>
	public class EntityToGearcraft {

		private Job _master;

		private Entities _entities;
		private MapType _version;

		/// <summary>
		/// Creates a new instance of an <see cref="EntityToGrarcraft"/> object which will operate on the passed <see cref="Entities"/>.
		/// </summary>
		/// <param name="entities"The <see cref="Entities"/> to postprocess.</param>
		/// <param name="version">The <see cref="MapType"/> of the BSP the entities are from.</param>
		/// <param name="master">The parent <see cref="Job"/> object for this instance.</param>
		public EntityToGearcraft(Entities entities, MapType version, Job master) {
			_entities = entities;
			_version = version;
			_master = master;
		}

		/// <summary>
		/// Processes every <see cref="Entity"/> in an <see cref="Entities"/> object to be used in a Gearcraft map.
		/// </summary>
		public void PostProcessEntities() {
			// There should really only be one of these. But someone might have screwed with the map...
			List<Entity> worldspawns = _entities.FindAll(entity => { return entity.className.Equals("worldspawn", StringComparison.InvariantCultureIgnoreCase); });
			foreach (Entity entity in worldspawns) {
				entity["mapversion"] = "510";
			}

			// Detect and parse water
			Entity waterEntitiy = PostProcessWater(worldspawns);
			if (waterEntitiy != null) {
				_entities.Add(waterEntitiy);
			}

			// Detect Capture the Flag entities and enforce the gametype
			bool isCTF = DetectCTF(_entities);
			if (isCTF) {
				foreach (Entity entity in worldspawns) {
					entity["defaultctf"] = "1";
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
		/// Moves any <see cref="MAPBrush"/> with <c>isWater</c> <c>true</c> from the passed <see cref="Entity"/> objects into a
		/// new <see cref="Entity"/> object and returns it.
		/// </summary>
		/// <param name="entities">An enumerable object of <see cref="Entity"/> objects to strip water <see cref="MAPBrush"/> objects from.</param>
		/// <returns>A new <see cref="Entity"/> object containing all the stripped water <see cref="MAPBrush"/> objects.</returns>
		private static Entity PostProcessWater(IEnumerable<Entity> entities) {
			Entity waterEntity = null;
			foreach (Entity entity in entities) {
				for (int i = 0; i < entity.brushes.Count; ++i) {
					if (entity.brushes[i].isWater) {
						// Don't create a new entity for the water unless we need it, but only create one
						if (waterEntity == null) {
							waterEntity = CreateNewWaterEntity();
						}
						waterEntity.brushes.Add(entity.brushes[i]);
						entity.brushes.RemoveAt(i);
						--i;
					}
				}
			}

			return waterEntity;
		}

		/// <summary>
		/// Creates a new empty water <see cref="Entity"/>.
		/// </summary>
		/// <returns>A new empty water <see cref="Entity"/> with good defaults.</returns>
		private static Entity CreateNewWaterEntity() {
			Entity waterEntity = new Entity("func_water");
			waterEntity["rendercolor"] = "0 0 0";
			waterEntity["speed"] = "100";
			waterEntity["wait"] = "4";
			waterEntity["skin"] = "-3";
			waterEntity["WaveHeight"] = "3.2";
			return waterEntity;
		}

		/// <summary>
		/// Detects Capture the Flag entities in a collection of <see cref="Entity"/> objects.
		/// </summary>
		/// <param name="entities">An enumerable list of <see cref="Entity"/> objects to search through.</param>
		/// <returns>Returns <c>true</c> if there exist Capture the Flag entities in <paramref name="entities"/>.</returns>
		private static bool DetectCTF(IEnumerable<Entity> entities) {
			return entities.Any<Entity>(entity => (entity.className == "team_ctf" || entity.className == "ctf_flag_hardcorps"));
		}

		/// <summary>
		/// Sends <paramref name="entity"/> to be postprocessed into the appropriate method based on version.
		/// </summary>
		/// <param name="entity"><see cref="Entity"/> to postprocess.</param>
		private void PostProcessEntity(Entity entity) {
			switch (_version) {
				case MapType.CoD:
				case MapType.CoD2:
				case MapType.CoD4: {
					PostProcessCoDEntity(entity);
					break;
				}
				case MapType.Quake2:
				case MapType.SiN:
				case MapType.SoF: {
					PostProcessQuake2Entity(entity);
					break;
				}
				case MapType.Quake3:
				case MapType.FAKK:
				case MapType.Raven:
				case MapType.MOHAA:
				case MapType.STEF2:
				case MapType.STEF2Demo: {
					PostProcessQuake3Entity(entity);
					break;
				}
				case MapType.Nightfire: {
					PostProcessNightfireEntity(entity);
					break;
				}
				case MapType.Source17:
				case MapType.Source18:
				case MapType.Source19:
				case MapType.Source20:
				case MapType.Source21:
				case MapType.Source22:
				case MapType.Source23:
				case MapType.DMoMaM:
				case MapType.L4D2:
				case MapType.Vindictus:
				case MapType.TacticalInterventionEncrypted: {
					PostProcessSourceEntity(entity);
					break;
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert an <see cref="Entity"/> from a Nightfire BSP to one for Gearcraft.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to parse.</param>
		private void PostProcessNightfireEntity(Entity entity) {
			if (entity.brushBased) {
				Vector3d origin = entity.origin;
				entity.Remove("origin");
				entity.Remove("model");
				if (origin != Vector3d.zero) {
					// If this brush has an origin
					MAPBrush neworiginBrush = MAPBrushExtensions.CreateCube(new Vector3d(-16, -16, -16), new Vector3d(16, 16, 16), "special/origin");
					entity.brushes.Add(neworiginBrush);
				}
				foreach (MAPBrush brush in entity.brushes) {
					brush.Translate(origin);
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert an <see cref="Entity"/> from a Source engine BSP to one for Gearcraft.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to parse.</param>
		private void PostProcessSourceEntity(Entity entity) {
			entity.Remove("hammerid");
			if (entity.brushBased) {
				Vector3d origin = entity.origin;
				entity.Remove("origin");
				entity.Remove("model");
				if (entity.ValueIs("classname", "func_door_rotating")) {
					// TODO: What entities require origin brushes?
					if (origin != Vector3d.zero) {
						MAPBrush neworiginBrush = MAPBrushExtensions.CreateCube(new Vector3d(-16, -16, -16), new Vector3d(16, 16, 16), "special/origin");
						entity.brushes.Add(neworiginBrush);
					}
				}
				foreach (MAPBrush brush in entity.brushes) {
					brush.Translate(origin);
				}
			}

			switch (entity["classname"].ToLower()) {
				case "func_breakable_surf": {
					entity["classname"] = "func_breakable";
					break;
				}
				case "func_brush": {
					if (entity["solidity"] == "0") {
						entity["classname"] = "func_wall_toggle";
						if (entity["StartDisabled"] == "1") {
							entity["spawnflags"] = "1";
						} else {
							entity["spawnflags"] = "0";
						}
						entity.Remove("StartDisabled");
					} else {
						if (entity["solidity"] == "1") {
							entity["classname"] = "func_illusionary";
						} else {
							entity["classname"] = "func_wall";
						}
					}
					entity.Remove("solidity");
					break;
				}
				case "env_fog_controller": {
					entity["classname"] = "env_fog";
					entity["rendercolor"] = entity["fogcolor"];
					entity.Remove("fogcolor");
					break;
				}
				case "prop_static": {
					entity["classname"] = "item_generic";
					break;
				}
				case "info_player_rebel":
				case "info_player_janus": // GoldenEye Source :3
				case "ctf_rebel_player_spawn": {
					entity["classname"] = "info_ctfspawn";
					entity["team_no"] = "2";
					goto case "info_player_deathmatch";
				}
				case "info_player_combine":
				case "info_player_mi6":
				case "ctf_combine_player_spawn": {
					entity["classname"] = "info_ctfspawn";
					entity["team_no"] = "1";
					goto case "info_player_deathmatch";
				}
				case "info_player_deathmatch": {
					Vector3d origin = entity.origin;
					entity["origin"] = origin.x + " " + origin.y + " " + (origin.z + 40);
					break;
				}
				case "ctf_combine_flag": {
					entity.Remove("targetname");
					entity.Remove("SpawnWithCaptureEnabled");
					entity["skin"] = "1";
					entity["goal_max"] = "16 16 72";
					entity["goal_min"] = "-16 -16 0";
					entity["goal_no"] = "1";
					entity["model"] = "models/ctf_flag.mdl";
					entity["classname"] = "item_ctfflag";
					Entity newFlagBase = new Entity("item_ctfbase");
					newFlagBase["origin"] = entity["origin"];
					newFlagBase["angles"] = entity["angles"];
					newFlagBase["goal_max"] = "16 16 72";
					newFlagBase["goal_min"] = "-16 -16 0";
					newFlagBase["goal_no"] = "1";
					newFlagBase["model"] = "models/ctf_flag_stand_mi6.mdl";
					_entities.Add(newFlagBase);
					break;
				}
				case "ctf_rebel_flag": {
					entity.Remove("targetname");
					entity.Remove("SpawnWithCaptureEnabled");
					entity["skin"] = "0";
					entity["goal_max"] = "16 16 72";
					entity["goal_min"] = "-16 -16 0";
					entity["goal_no"] = "2";
					entity["model"] = "models/ctf_flag.mdl";
					entity["classname"] = "item_ctfflag";
					Entity newFlagBase = new Entity("item_ctfbase");
					newFlagBase["origin"] = entity["origin"];
					newFlagBase["angles"] = entity["angles"];
					newFlagBase["goal_max"] = "16 16 72";
					newFlagBase["goal_min"] = "-16 -16 0";
					newFlagBase["goal_no"] = "2";
					newFlagBase["model"] = "models/ctf_flag_stand_phoenix.mdl";
					_entities.Add(newFlagBase);
					break;
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert an <see cref="Entity"/> from a Call of Duty BSP to one for Gearcraft.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to parse.</param>
		private void PostProcessCoDEntity(Entity entity) {
			if (entity.brushBased) {
				Vector3d origin = entity.origin;
				entity.Remove("origin");
				entity.Remove("model");
				if (entity["classname"].ToUpper().Equals("func_rotating".ToUpper())) {
					// TODO: What entities require origin brushes in CoD?
					if (origin == Vector3d.zero) {
						// If this brush uses the "origin" attribute
						MAPBrush neworiginBrush = MAPBrushExtensions.CreateCube(new Vector3d(-16, -16, -16), new Vector3d(16, 16, 16), "special/origin");
						entity.brushes.Add(neworiginBrush);
					}
				}
				foreach (MAPBrush brush in entity.brushes) {
					brush.Translate(origin);
				}
			}

			switch (entity["classname"].ToLower()) {
				case "light": {
					entity["_light"] = "255 255 255 " + entity["light"];
					entity.Remove("light");
					break;
				}
				case "mp_teamdeathmatch_spawn":
				case "mp_deathmatch_spawn": {
					entity["classname"] = "info_player_deathmatch";
					break;
				}
				case "mp_searchanddestroy_spawn_allied": {
					entity["classname"] = "info_player_ctfspawn";
					entity["team_no"] = "1";
					entity.Remove("model");
					break;
				}
				case "mp_searchanddestroy_spawn_axis": {
					entity["classname"] = "info_player_ctfspawn";
					entity["team_no"] = "2";
					entity.Remove("model");
					break;
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert an <see cref="Entity"/> from a Quake 2-based BSP to one for Gearcraft.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to parse.</param>
		private void PostProcessQuake2Entity(Entity entity) {
			if (!entity["angle"].Equals("")) {
				entity["angles"] = "0 " + entity["angle"] + " 0";
				entity.Remove("angle");
			}
			if (entity.brushBased) {
				Vector3d origin = entity.origin;
				entity.Remove("origin");
				entity.Remove("model");
				if (entity.ValueIs("classname", "func_rotating")) {
					if (origin != Vector3d.zero) {
						MAPBrush neworiginBrush = MAPBrushExtensions.CreateCube(new Vector3d(-16, -16, -16), new Vector3d(16, 16, 16), "special/origin");
						entity.brushes.Add(neworiginBrush);
					}
				}
				foreach (MAPBrush brush in entity.brushes) {
					brush.Translate(origin);
				}
			}

			switch (entity["classname"].ToLower()) {
				case "func_wall": {
					if (entity.SpawnflagsSet(2) || entity.SpawnflagsSet(4)) {
						entity["classname"] = "func_wall_toggle";
					}
					break;
				}
				case "item_flag_team2":
				case "ctf_flag_hardcorps": {
					// Blue flag
					entity["classname"] = "item_ctfflag";
					entity["skin"] = "1"; // 0 for PHX, 1 for MI6
					entity["goal_no"] = "1"; // 2 for PHX, 1 for MI6
					entity["goal_max"] = "16 16 72";
					entity["goal_min"] = "-16 -16 0";
					Entity flagBase = new Entity("item_ctfbase");
					flagBase["origin"] = entity["origin"];
					flagBase["angles"] = entity["angles"];
					flagBase["angle"] = entity["angle"];
					flagBase["goal_no"] = "1";
					flagBase["model"] = "models/ctf_flag_stand_mi6.mdl";
					flagBase["goal_max"] = "16 16 72";
					flagBase["goal_min"] = "-16 -16 0";
					_entities.Add(flagBase);
					break;
				}
				case "item_flag_team1":
				case "ctf_flag_sintek": {
					// Red flag
					entity["classname"] = "item_ctfflag";
					entity["skin"] = "0"; // 0 for PHX, 1 for MI6
					entity["goal_no"] = "2"; // 2 for PHX, 1 for MI6
					entity["goal_max"] = "16 16 72";
					entity["goal_min"] = "-16 -16 0";
					Entity flagBase = new Entity("item_ctfbase");
					flagBase["origin"] = entity["origin"];
					flagBase["angles"] = entity["angles"];
					flagBase["angle"] = entity["angle"];
					flagBase["goal_no"] = "2";
					flagBase["model"] = "models/ctf_flag_stand_phoenix.mdl";
					flagBase["goal_max"] = "16 16 72";
					flagBase["goal_min"] = "-16 -16 0";
					_entities.Add(flagBase);
					break;
				}
				case "info_player_team1":
				case "info_player_sintek": {
					entity["classname"] = "info_ctfspawn";
					entity["team_no"] = "2";
					break;
				}
				case "info_player_team2":
				case "info_player_hardcorps": {
					entity["classname"] = "info_ctfspawn";
					entity["team_no"] = "1";
					break;
				}
				case "info_player_start":
				case "info_player_coop":
				case "info_player_deathmatch": {
					Vector3d origin = entity.origin;
					entity["origin"] = origin.x + " " + origin.y + " " + (origin.z + 18);
					break;
				}
				case "light": {
					Vector3d color;
					if (entity.ContainsKey("_color")) {
						color = entity.GetVector("_color");
					} else {
						color = Vector3d.one;
					}
					color *= 255;
					float intensity = entity.GetFloat("light", 1);
					entity.Remove("_color");
					entity.Remove("light");
					entity["_light"] = color.x + " " + color.y + " " + color.z + " " + intensity;
					break;
				}
				case "misc_teleporter": {
					Vector3d origin = entity.origin;
					Vector3d mins = new Vector3d(origin.x - 24, origin.y - 24, origin.z - 24);
					Vector3d maxs = new Vector3d(origin.x + 24, origin.y + 24, origin.z + 48);
					entity.brushes.Add(MAPBrushExtensions.CreateCube(mins, maxs, "special/trigger"));
					entity.Remove("origin");
					entity["classname"] = "trigger_teleport";
					break;
				}
				case "misc_teleporter_dest": {
					entity["classname"] = "info_teleport_destination";
					break;
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert an <see cref="Entity"/> from a Quake 3-based BSP to one for Gearcraft.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to parse.</param>
		private void PostProcessQuake3Entity(Entity entity) {
			if (entity.brushBased) {
				Vector3d origin = entity.origin;
				entity.Remove("origin");
				entity.Remove("model");
				if (entity.ValueIs("classname", "func_rotating") || entity.ValueIs("classname", "func_rotatingdoor")) {
					// TODO: What entities require origin brushes in Quake 3?
					if (origin != Vector3d.zero) {
						MAPBrush neworiginBrush = MAPBrushExtensions.CreateCube(new Vector3d(-16, -16, -16), new Vector3d(16, 16, 16), "special/origin");
						entity.brushes.Add(neworiginBrush);
					}
				}
				foreach (MAPBrush brush in entity.brushes) {
					brush.Translate(origin);
				}
			}

			switch (entity["classname"].ToLower()) {
				case "worldspawn": {
					if (!entity["suncolor"].Equals("")) {
						Entity light_environment = new Entity("light_environment");
						light_environment["_light"] = entity["suncolor"];
						light_environment["angles"] = entity["sundirection"];
						light_environment["_fade"] = entity["sundiffuse"];
						entity.Remove("suncolor");
						entity.Remove("sundirection");
						entity.Remove("sundiffuse");
						entity.Remove("sundiffusecolor");
						_entities.Add(light_environment);
					}
					break;
				}
				case "team_ctf_blueflag": {
					// Blue flag
					entity["classname"] = "item_ctfflag";
					entity["skin"] = "1"; // 0 for PHX, 1 for MI6
					entity["goal_no"] = "1"; // 2 for PHX, 1 for MI6
					entity["goal_max"] = "16 16 72";
					entity["goal_min"] = "-16 -16 0";
					entity["model"] = "models/ctf_flag.mdl";
					Entity flagBase = new Entity("item_ctfbase");
					flagBase["origin"] = entity["origin"];
					flagBase["angles"] = entity["angles"];
					flagBase["angle"] = entity["angle"];
					flagBase["goal_no"] = "1";
					flagBase["model"] = "models/ctf_flag_stand_mi6.mdl";
					flagBase["goal_max"] = "16 16 72";
					flagBase["goal_min"] = "-16 -16 0";
					_entities.Add(flagBase);
					break;
				}
				case "team_ctf_redflag": {
					// Red flag
					entity["classname"] = "item_ctfflag";
					entity["skin"] = "0"; // 0 for PHX, 1 for MI6
					entity["goal_no"] = "2"; // 2 for PHX, 1 for MI6
					entity["goal_max"] = "16 16 72";
					entity["goal_min"] = "-16 -16 0";
					entity["model"] = "models/ctf_flag.mdl";
					Entity flagBase = new Entity("item_ctfbase");
					flagBase["origin"] = entity["origin"];
					flagBase["angles"] = entity["angles"];
					flagBase["angle"] = entity["angle"];
					flagBase["goal_no"] = "2";
					flagBase["model"] = "models/ctf_flag_stand_phoenix.mdl";
					flagBase["goal_max"] = "16 16 72";
					flagBase["goal_min"] = "-16 -16 0";
					_entities.Add(flagBase);
					break;
				}
				case "team_ctf_redspawn":
				case "info_player_axis": {
					entity["classname"] = "info_ctfspawn";
					entity["team_no"] = "2";
					goto case "info_player_start";
				}
				case "team_ctf_bluespawn":
				case "info_player_allied": {
					entity["classname"] = "info_ctfspawn";
					entity["team_no"] = "1";
					goto case "info_player_start";
				}
				case "info_player_start":
				case "info_player_coop":
				case "info_player_deathmatch": {
					Vector3d origin = entity.origin;
					entity["origin"] = origin.x + " " + origin.y + " " + (origin.z + 24);
					break;
				}
				case "light": {
					Vector3d color;
					if (entity.ContainsKey("_color")) {
						color = entity.GetVector("_color");
					} else {
						color = Vector3d.one;
					}
					color *= 255;
					float intensity = entity.GetFloat("light", 1);
					entity.Remove("_color");
					entity.Remove("light");
					entity["_light"] = color.x + " " + color.y + " " + color.z + " " + intensity;
					break;
				}
				case "func_rotatingdoor": {
					entity["classname"] = "func_door_rotating";
					break;
				}
				case "info_pathnode": {
					entity["classname"] = "info_node";
					break;
				}
				case "trigger_ladder": {
					entity["classname"] = "func_ladder";
					break;
				}
				case "trigger_use": {
					entity["classname"] = "func_button";
					entity["spawnflags"] = "1";
					entity["wait"] = "1";
					break;
				}
			}
		}

		/// <summary>
		/// Every <see cref="MAPBrushSide"/> contained in <paramref name="brushes"/> will have its texture examined,
		/// and, if necessary, replaced with the equivalent for Gearcraft.
		/// </summary>
		/// <param name="brushes">The collection of <see cref="MAPBrush"/> objects to have textures parsed.</param>
		/// <param name="version">The <see cref="MapType"/> of the BSP this entity came from.</param>
		private void PostProcessTextures(IEnumerable<MAPBrush> brushes) {
			foreach (MAPBrush brush in brushes) {
				foreach (MAPBrushSide brushSide in brush.sides) {
					ValidateTexInfo(brushSide);
					PostProcessSpecialTexture(brushSide);
					switch (_version) {
						case MapType.Quake2:
						case MapType.SiN: {
							PostProcessQuake2Texture(brushSide);
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
						case MapType.Source17:
						case MapType.Source18:
						case MapType.Source19:
						case MapType.Source20:
						case MapType.Source21:
						case MapType.Source22:
						case MapType.Source23:
						case MapType.Source27:
						case MapType.DMoMaM:
						case MapType.L4D2:
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
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by Hammer, if necessary.
		/// </summary>
		/// <param name="brushSide">The <see cref="MAPBrushSide"/> to have its texture parsed.</param>
		private void PostProcessQuake2Texture(MAPBrushSide brushSide) {
			if (brushSide.texture.Length >= 5 && brushSide.texture.Substring(brushSide.texture.Length - 5).Equals("/clip", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = "special/clip";
			} else if (brushSide.texture.Length >= 5 && brushSide.texture.Substring(brushSide.texture.Length - 5).Equals("/hint", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = "special/hint";
			} else if (brushSide.texture.Length >= 8 && brushSide.texture.Substring(brushSide.texture.Length - 8).Equals("/trigger", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = "special/trigger";
			} else if (brushSide.texture.Equals("*** unsused_texinfo ***", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = "special/nodraw";
			}
		}

		/// <summary>
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by GTKRadiant, if necessary.
		/// These textures are produced by the decompiler algorithm itself.
		/// </summary>
		/// <param name="brushSide">The <see cref="MAPBrushSide"/> to have its texture parsed.</param>
		private void PostProcessSpecialTexture(MAPBrushSide brushSide) {
			switch (brushSide.texture.ToLower()) {
				case "**nulltexture**": {
					brushSide.texture = "special/null";
					break;
				}
				case "**skiptexture**": {
					brushSide.texture = "special/skip";
					break;
				}
				case "**skytexture**": {
					brushSide.texture = "special/sky";
					break;
				}
				case "**hinttexture**": {
					brushSide.texture = "special/hint";
					break;
				}
				case "**cliptexture**": {
					brushSide.texture = "special/clip";
					break;
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by Gearcraft, if necessary.
		/// </summary>
		/// <param name="brushSide">The <see cref="MAPBrushSide"/> to have its texture parsed.</param>
		private void PostProcessQuake3Texture(MAPBrushSide brushSide) {
			if (brushSide.texture.Length >= 9 && brushSide.texture.Substring(0, 9).Equals("textures/", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = brushSide.texture.Substring(9);
			}
			switch (brushSide.texture.ToLower()) {
				case "common/physics_clip":
				case "common/metalclip":
				case "common/grassclip":
				case "common/paperclip":
				case "common/woodclip":
				case "common/glassclip":
				case "common/clipfoliage":
				case "common/foliageclip":
				case "common/carpetclip":
				case "common/dirtclip":
				case "system/clip":
				case "system/physics_clip":
				case "common/clip": {
					brushSide.texture = "special/clip";
					break;
				}
				case "common/nodrawnonsolid":
				case "system/trigger":
				case "common/trigger": {
					brushSide.texture = "special/trigger";
					break;
				}
				case "common/nodraw":
				case "common/caulkshadow":
				case "common/caulk":
				case "system/caulk":
				case "noshader": {
					brushSide.texture = "special/nodraw";
					break;
				}
				case "common/do_not_enter":
				case "common/donotenter":
				case "common/monsterclip": {
					brushSide.texture = "special/npcclip";
					break;
				}
				case "common/caulksky":
				case "common/skyportal": {
					brushSide.texture = "special/sky";
					break;
				}
				case "common/hint": {
					brushSide.texture = "special/hint";
					break;
				}
				case "common/waterskip": {
					brushSide.texture = "liquids/!water";
					break;
				}
				case "system/do_not_enter":
				case "common/playerclip": {
					brushSide.texture = "special/playerclip";
					break;
				}
			}
			if (brushSide.texture.Length >= 4 && brushSide.texture.Substring(0, 4).Equals("sky/", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = "special/sky";
			}
		}

		/// <summary>
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by Gearcraft, if necessary.
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
					brushSide.texture = "special/hint";
					break;
				}
				case "tools/toolsskip": {
					brushSide.texture = "special/skip";
					break;
				}
				case "tools/toolsinvisible":
				case "tools/toolsclip": {
					brushSide.texture = "special/clip";
					break;
				}
				case "tools/toolstrigger":
				case "tools/toolsfog": {
					brushSide.texture = "special/trigger";
					break;
				}
				case "tools/toolsskybox": {
					brushSide.texture = "special/sky";
					break;
				}
				case "tools/toolsnodraw": {
					brushSide.texture = "special/nodraw";
					break;
				}
				case "tools/toolsplayerclip": {
					brushSide.texture = "special/playerclip";
					break;
				}
				case "tools/toolsnpcclip": {
					brushSide.texture = "special/enemyclip";
					break;
				}
				case "tools/toolsblack": {
					brushSide.texture = "special/black";
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
