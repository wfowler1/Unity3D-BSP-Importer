using System;
using System.Collections.Generic;
using System.Linq;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Class containing methods for parsing entities from various BSP formats into those for Hammer.
	/// </summary>
	public class EntityToHammer {

		private Job _master;

		private Entities _entities;
		private MapType _version;

		private int _mmStackLength = 0;
		private List<string> _numeralizedTargetnames = new List<string>();
		private List<int> _numTargets = new List<int>();

		/// <summary>
		/// Creates a new instance of an <see cref="EntityToHammer"/> object which will operate on the passed <see cref="Entities"/>.
		/// </summary>
		/// <param name="entities"The <see cref="Entities"/> to postprocess.</param>
		/// <param name="version">The <see cref="MapType"/> of the BSP the entities are from.</param>
		/// <param name="master">The parent <see cref="Job"/> object for this instance.</param>
		public EntityToHammer(Entities entities, MapType version, Job master) {
			_entities = entities;
			_version = version;
			_master = master;
		}

		/// <summary>
		/// Processes every <see cref="Entity"/> in an <see cref="Entities"/> object to be used in a Hammer map.
		/// </summary>
		public void PostProcessEntities() {
			// There should really only be one of these. But someone might have screwed with the map...
			List<Entity> worldspawns = _entities.FindAll(entity => { return entity.className.Equals("worldspawn", StringComparison.InvariantCultureIgnoreCase); });

			// TODO: This is awful. Let's rework the enum to have internal ways to check engine forks.
			if (_version != MapType.Source17 &&
				 _version != MapType.Source18 &&
				 _version != MapType.Source19 &&
				 _version != MapType.Source20 &&
				 _version != MapType.Source21 &&
				 _version != MapType.Source22 &&
				 _version != MapType.Source23 &&
				 _version != MapType.Source27 &&
				 _version != MapType.L4D2 &&
				 _version != MapType.DMoMaM &&
				 _version != MapType.Vindictus &&
				 _version != MapType.TacticalInterventionEncrypted) {
				bool hasWater = false;
				// Make sure all water brushes currently in the worldspawn get converted to Source.
				foreach (Entity worldspawn in worldspawns) {
					for (int i = 0; i < worldspawn.brushes.Count; ++i) {
						MAPBrush brush = worldspawn.brushes[i];
						if (brush.isWater) {
							hasWater = true;
							ConvertToWater(brush);
						}
					}
				}
				// Make sure all func_water entities get converted to Source.
				List<Entity> waters = _entities.FindAll(entity => { return entity.className.Equals("func_water", StringComparison.InvariantCultureIgnoreCase); });
				if (waters.Any()) {
					hasWater = true;
					// Parse water entities into just water brushes
					foreach (Entity water in waters) {
						ParseWaterIntoWorld(worldspawns[0], water);
						_entities.Remove(water);
					}
				}

				if (hasWater && !_entities.Any(entity => { return entity.className.Equals("water_lod_control", StringComparison.InvariantCultureIgnoreCase); })) {
					Entity pointEntity = _entities.Find(entity => { return !entity.brushBased; });
					Vector3d origin = Vector3d.zero;
					if (pointEntity != null) {
						origin = pointEntity.origin;
					}
					Entity lodControl = new Entity("water_lod_control");
					lodControl["cheapwaterenddistance"] = "2000";
					lodControl["cheapwaterstartdistance"] = "1000";
					lodControl.origin = origin;
				}

				if (_version == MapType.MOHAA) {
					foreach (Entity worldspawn in worldspawns) {
						for (int i = 0; i < worldspawn.brushes.Count; ++i) {
							MAPBrush brush = worldspawn.brushes[i];
							MAPTerrainMoHAA terrain = brush.mohTerrain;
							if (terrain != null && terrain.size == new Vector2d(9, 9)) {
								MAPTerrainMoHAA.Partition partition = terrain.partitions[0];
								Plane p = new Plane(new Vector3d(0, 0, 1), terrain.origin.z);
								Vector3d[] froms = new Vector3d[] {
									terrain.origin,
									new Vector3d(terrain.origin.x, terrain.origin.y + 512, terrain.origin.z),
									new Vector3d(terrain.origin.x + 512, terrain.origin.y + 512, terrain.origin.z),
									new Vector3d(terrain.origin.x + 512, terrain.origin.y, terrain.origin.z),
								};
								Vector3d[] tos = new Vector3d[] {
									new Vector3d(terrain.origin.x, terrain.origin.y + 512, terrain.origin.z),
									new Vector3d(terrain.origin.x + 512, terrain.origin.y + 512, terrain.origin.z),
									new Vector3d(terrain.origin.x + 512, terrain.origin.y, terrain.origin.z),
									terrain.origin,
								};
								if (terrain.flags > 0) {
									p.Flip();
									Vector3d temp = froms[1];
									froms[1] = froms[3];
									froms[3] = temp;
									temp = tos[1];
									tos[1] = tos[3];
									tos[3] = temp;
								}
								Vector3d[] axes = TextureInfo.TextureAxisFromPlane(p);
								TextureInfo newTextureInfo = new TextureInfo(axes[0], partition.textureShift[0], (float)partition.textureScale[0],
								                                             axes[1], partition.textureShift[1], (float)partition.textureScale[1],
								                                             0, 0);
								MAPBrush newBrush = MAPBrushExtensions.CreateBrushFromWind(froms, tos, partition.shader, "tools/toolsnodraw", newTextureInfo, 32);
								MAPBrushSide newSide = newBrush.sides[0];
								MAPDisplacement newDisplacement = new MAPDisplacement() {
									power = 3,
									start = terrain.origin,
									normals = new Vector3d[9][],
									distances = new float[9][],
									alphas = new float[9][],
								};
								for (int y = 0; y < terrain.size.y; ++y) {
									newDisplacement.normals[y] = new Vector3d[9];
									newDisplacement.distances[y] = new float[9];
									newDisplacement.alphas[y] = new float[9];
									for (int x = 0; x < terrain.size.x; ++x) {
										newDisplacement.normals[y][x] = Vector3d.up;
										newDisplacement.distances[y][x] = terrain.vertices[(y * (int)terrain.size.y) + x].height;
									}
								}
								newSide.displacement = newDisplacement;
								worldspawn.brushes.RemoveAt(i--);
								worldspawn.brushes.Add(newBrush);
							}
						}
					}
				}
			}

			foreach (Entity worldspawn in worldspawns) {
				for (int i = 0; i < worldspawn.brushes.Count; ++i) {
					MAPBrush brush = worldspawn.brushes[i];
					if (brush.isDetail) {
						Entity newEntity = new Entity("func_detail");
						newEntity.brushes.Add(brush);
						_entities.Add(newEntity);
						worldspawn.brushes.RemoveAt(i);
						--i;
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

			if (!_master.settings.noEntCorrection) {
				// TODO: This is awful. Let's rework the enum to have internal ways to check engine forks.
				if (_version != MapType.Source17 &&
					 _version != MapType.Source18 &&
					 _version != MapType.Source19 &&
					 _version != MapType.Source20 &&
					 _version != MapType.Source21 &&
					 _version != MapType.Source22 &&
					 _version != MapType.Source23 &&
					 _version != MapType.Source27 &&
					 _version != MapType.L4D2 &&
					 _version != MapType.DMoMaM &&
					 _version != MapType.Vindictus &&
					 _version != MapType.TacticalInterventionEncrypted) {
					for (int i = 0; i < _entities.Count; ++i) {
						ParseEntityIO(_entities[i]);
					}
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
				if (side.plane.normal == Vector3d.up) {
					side.texture = "dev/dev_water2";
				} else {
					side.texture = "TOOLS/TOOLSNODRAW";
				}
			}
		}

		/// <summary>
		/// Sends <paramref name="entity"/> to be postprocessed into the appropriate method based on version.
		/// </summary>
		/// <param name="entity"><see cref="Entity"/> to postprocess.</param>
		private void PostProcessEntity(Entity entity) {
			switch (_version) {
				case MapType.Quake2:
				case MapType.SiN:
				case MapType.SoF: {
					PostProcessQuake2Entity(entity);
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
				case MapType.L4D2:
				case MapType.DMoMaM:
				case MapType.Vindictus:
				case MapType.TacticalInterventionEncrypted: {
					PostProcessSourceEntity(entity);
					break;
				}
				case MapType.Quake3:
				case MapType.FAKK:
				case MapType.Raven:
				case MapType.MOHAA:
				case MapType.STEF2:
				case MapType.STEF2Demo:
				case MapType.CoD:
				case MapType.CoD2:
				case MapType.CoD4: {
					// TODO
					break;
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert an <see cref="Entity"/> from a Nightfire BSP to one for Hammer.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to parse.</param>
		private void PostProcessNightfireEntity(Entity entity) {
			if (entity.brushBased) {
				Vector3d origin = entity.origin;
				entity.Remove("model");
				foreach (MAPBrush brush in entity.brushes) {
					brush.Translate(origin);
				}
			}
			if (entity.angles.x != 0) {
				entity.angles = new Vector3d(-entity.angles.x, entity.angles.y, entity.angles.z);
			}
			if (!entity["body"].Equals("")) {
				entity.RenameKey("body", "SetBodyGroup");
			}
			if ((Vector3d)entity.GetVector("rendercolor") == Vector3d.zero) {
				entity["rendercolor"] = "255 255 255";
			}
			if (entity.angles == Vector3d.back) {
				entity.angles = new Vector3d(-90, 0, 0);
			}
			string modelName = entity["model"];
			if (modelName.Length >= 4 && modelName.Substring(modelName.Length - 4).Equals(".spz", StringComparison.InvariantCultureIgnoreCase)) {
				entity["model"] = modelName.Substring(0, modelName.Length - 4) + ".spr";
			}

			switch (entity.className.ToLower()) {
				case "light_spot": {
					entity["pitch"] = (entity.angles.x + entity.GetFloat("pitch", 0)).ToString();
					float cone = entity.GetFloat("_cone", 0);
					if (cone > 90) { cone = 90; }
					if (cone < 0) { cone = 0; }
					entity["_cone"] = cone.ToString();
					float cone2 = entity.GetFloat("_cone2", 0);
					if (cone2 > 90) { cone2 = 90; }
					if (cone2 < 0) { cone2 = 0; }
					entity["_cone2"] = cone2.ToString();
					entity.RenameKey("_cone", "_inner_cone");
					entity.RenameKey("_cone2", "_cone");
					break;
				}
				case "func_wall": {
					entity["classname"] = "func_brush";
					entity["solidity"] = "2";
					entity["disableshadows"] = "1";
					entity.Remove("angles");
					entity.Remove("rendermode");
					break;
				}
				case "func_wall_toggle": {
					entity["classname"] = "func_brush";
					entity["solidity"] = "0";
					entity["disableshadows"] = "1";
					entity.Remove("angles");
					if (entity.SpawnflagsSet(1)) {
						entity["StartDisabled"] = "1";
						entity.ClearSpawnflags(1);
					} else {
						entity["StartDisabled"] = "0";
					}
					break;
				}
				case "func_illusionary": {
					entity["classname"] = "func_brush";
					entity["solidity"] = "1";
					entity["disableshadows"] = "1";
					entity.Remove("angles");
					break;
				}
				case "item_generic": {
					entity["classname"] = "prop_dynamic";
					entity["solid"] = "0";
					entity.Remove("effects");
					entity.Remove("fixedlight");
					break;
				}
				case "env_glow": {
					entity["classname"] = "env_sprite";
					break;
				}
				case "info_teleport_destination": {
					entity["classname"] = "info_target";
					break;
				}
				case "info_ctfspawn": {
					if (entity["team_no"].Equals("1")) {
						entity["classname"] = "ctf_combine_player_spawn";
						entity.Remove("team_no");
					} else if (entity["team_no"].Equals("2")) {
						entity["classname"] = "ctf_rebel_player_spawn";
						entity.Remove("team_no");
					}
					goto case "info_player_start";
				}
				case "info_player_deathmatch":
				case "info_player_start": {
					Vector3d origin = entity.origin;
					entity.origin = new Vector3d(origin.x, origin.y, (origin.z - 40));
					break;
				}
				case "item_ctfflag": {
					entity.Remove("skin");
					entity.Remove("goal_min");
					entity.Remove("goal_max");
					entity.Remove("model");
					entity["SpawnWithCaptureEnabled"] = "1";
					if (entity["goal_no"].Equals("1")) {
						entity["classname"] = "ctf_combine_flag";
						entity["targetname"] = "combine_flag";
						entity.Remove("goal_no");
					} else if (entity["goal_no"].Equals("2")) {
						entity["classname"] = "ctf_rebel_flag";
						entity["targetname"] = "rebel_flag";
						entity.Remove("goal_no");
					}
					break;
				}
				case "func_ladder": {
					foreach (MAPBrush brush in entity.brushes) {
						foreach (MAPBrushSide side in brush.sides) {
							side.texture = "TOOLS/TOOLSINVISIBLELADDER";
						}
					}
					break;
				}
				case "func_door": {
					entity["movedir"] = entity["angles"];
					entity["noise1"] = entity["movement_noise"];
					entity.Remove("movement_noise");
					entity.Remove("angles");
					if (entity.SpawnflagsSet(1)) {
						entity["spawnpos"] = "1";
						entity.ClearSpawnflags(1);
					}
					entity["renderamt"] = "255";
					break;
				}
				case "func_button": {
					entity["movedir"] = entity["angles"];
					goto case "func_rot_button";
				}
				case "func_rot_button": {
					entity.Remove("angles");
					foreach (MAPBrush brush in entity.brushes) {
						foreach (MAPBrushSide side in brush.sides) {
							// If we want this to be an invisible, non-colliding button that's "+use"-able
							if (side.texture.Equals("special/TRIGGER", StringComparison.InvariantCultureIgnoreCase)) {
								side.texture = "TOOLS/TOOLSHINT"; // Hint is the only thing that still works that doesn't collide with the player
							}
						}
					}
					if (!entity.SpawnflagsSet(256)) {
						// Nightfire's "touch activates" flag, same as source!
						if (entity.GetFloat("health", 0) != 0) {
							entity.SetSpawnflags(512);
						} else {
							entity.SetSpawnflags(1024);
						}
					}
					break;
				}
				case "trigger_hurt": {
					if (entity.SpawnflagsSet(2)) {
						entity["StartDisabled"] = "1";
					}
					if (!entity.SpawnflagsSet(8)) {
						entity["spawnflags"] = "1";
					} else {
						entity["spawnflags"] = "0";
					}
					entity.RenameKey("dmg", "damage");
					break;
				}
				case "trigger_auto": {
					entity["classname"] = "logic_auto";
					break;
				}
				case "trigger_once":
				case "trigger_multiple": {
					if (entity.SpawnflagsSet(8) || entity.SpawnflagsSet(1)) {
						entity.ClearSpawnflags(1);
						entity.ClearSpawnflags(8);
						entity.SetSpawnflags(2);
					}
					if (entity.SpawnflagsSet(2)) {
						entity.ClearSpawnflags(1);
					} else {
						entity.SetSpawnflags(1);
					}
					break;
				}
				case "func_door_rotating": {
					if (entity.SpawnflagsSet(1)) {
						entity["spawnpos"] = "1";
						entity.ClearSpawnflags(1);
					}
					entity["noise1"] = entity["movement_noise"];
					entity.Remove("movement_noise");
					break;
				}
				case "trigger_push": {
					entity["pushdir"] = entity["angles"];
					entity.Remove("angles");
					break;
				}
				case "light_environment": {
					Entity newShadowControl = new Entity("shadow_control");
					Entity newEnvSun = new Entity("env_sun");
					newShadowControl["angles"] = entity["angles"];
					newEnvSun["angles"] = entity["angles"];
					newShadowControl["origin"] = entity["origin"];
					newEnvSun["origin"] = entity["origin"];
					newShadowControl["color"] = "128 128 128";
					_entities.Add(newShadowControl);
					_entities.Add(newEnvSun);
					break;
				}
				case "func_tracktrain": {
					entity.RenameKey("movesnd", "MoveSound");
					entity.RenameKey("stopsnd", "StopSound");
					break;
				}
				case "path_track": {
					if (entity.SpawnflagsSet(1)) {
						entity.Remove("targetname");
					}
					break;
				}
				case "trigger_relay": {
					entity["classname"] = "logic_relay";
					break;
				}
				case "trigger_counter": {
					entity["classname"] = "math_counter";
					entity["max"] = entity["count"];
					entity["min"] = "0";
					entity["startvalue"] = "0";
					entity.Remove("count");
					break;
				}
				case "worldspawn": {
					entity.Remove("mapversion");
					break;
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert an <see cref="Entity"/> from a Source engine BSP to one for Hammer.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to parse.</param>
		private void PostProcessSourceEntity(Entity entity) {
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
			entity.Remove("hammerid");

		}

		/// <summary>
		/// Postprocesser to convert an <see cref="Entity"/> from a Quake 2-based BSP to one for Hammer.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to parse.</param>
		private void PostProcessQuake2Entity(Entity entity) {
			if (!entity["angle"].Equals("")) {
				entity["angles"] = "0 " + entity["angle"] + " 0";
				entity.Remove("angle");
			}
			if (entity.brushBased) {
				Vector3d origin = entity.origin;
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
					entity["classname"] = "func_brush";
					// 2 I believe is "Start enabled" and 4 is "toggleable", or the other way around. Not sure.
					if (entity.SpawnflagsSet(2) || entity.SpawnflagsSet(4)) {
						entity["solidity"] = "0";
					} else {
						entity["solidity"] = "2";
					}
					break;
				}
				case "info_player_start":
				case "info_player_deathmatch": {
					Vector3d origin = entity.origin;
					entity.origin = new Vector3d(origin.x, origin.y, (origin.z + 18));
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
					entity.brushes.Add(MAPBrushExtensions.CreateCube(mins, maxs, "tools/toolstrigger"));
					entity.Remove("origin");
					entity["classname"] = "trigger_teleport";
					break;
				}
				case "misc_teleporter_dest": {
					entity["classname"] = "info_target";
					break;
				}
			}
		}

		/// <summary>
		/// Turn a triggering entity (like a func_button or trigger_multiple) into a Source
		/// engine trigger using entity I/O. There's a few complications to this: There's
		/// no generic output which always acts like the triggers in other engines, and there's
		/// no "Fire" input. I try to figure out which ones are best based on their classnames
		/// but it's not 100% foolproof, and I have to add a case for every specific class.
		/// </summary>
		/// <param name="entity">The <see cref="Entity"/> to parse I/O connections for.</param>
		public void ParseEntityIO(Entity entity) {
			if (!(entity["target"] == "")) {
				double delay = entity.GetFloat("delay", 0.0f);
				if (!entity["target"].Equals("")) {
					Entity[] targets = GetTargets(entity["target"]);
					foreach (Entity target in targets) {
						if (target.ValueIs("classname", "multi_manager") || target.ValueIs("classname", "multi_kill_manager")) {
							Entity mm = ParseMultimanager(target);
							foreach (Entity.EntityConnection connection in mm.connections) {
								if (entity.ValueIs("classname", "logic_relay") && entity.ContainsKey("delay")) {
									entity.connections.Add(new Entity.EntityConnection() { name = "OnTrigger", target = connection.target, action = connection.action, param = connection.param, delay = connection.delay + delay, fireOnce = connection.fireOnce, unknown0 = "", unknown1 = "" });
								} else {
									entity.connections.Add(new Entity.EntityConnection() { name = entity.FireAction(), target = connection.target, action = connection.action, param = connection.param, delay = connection.delay, fireOnce = connection.fireOnce, unknown0 = "", unknown1 = "" });
								}
							}
						} else {
							string outputAction = target.OnFire();
							if (entity.ValueIs("triggerstate", "0")) {
								outputAction = target.OnDisable();
							} else {
								if (entity.ValueIs("triggerstate", "1")) {
									outputAction = target.OnEnable();
								}
							}
							entity.connections.Add(new Entity.EntityConnection() { name = entity.FireAction(), target = target["targetname"], action = outputAction, param = "", delay = delay, fireOnce = -1, unknown0 = "", unknown1 = "" });
						}
					}
				}
				if (!entity["killtarget"].Equals("")) {
					entity.connections.Add(new Entity.EntityConnection() { name = entity.FireAction(), target = entity["killtarget"], action = "Kill", param = "", delay = delay, fireOnce = -1, unknown0 = "", unknown1 = "" });
				}
				entity.Remove("target");
				entity.Remove("killtarget");
				entity.Remove("triggerstate");
				entity.Remove("delay");
			}
		}

		/// <summary>
		/// Multimanagers are also a special case. There are none in Source. Instead, I
		/// need to add EVERY targetted entity in a multimanager to the original trigger
		/// entity as an output with the specified delay. Things get even more complicated
		/// when a multi_manager fires another multi_manager. In this case, this method will
		/// recurse on itself until all the complexity is worked out.
		/// One potential problem is if two multi_managers continuously call each other, this
		/// method will recurse infinitely until there is a stack overflow. This might happen
		/// when there is some sort of cycle going on in the map and multi_managers call each
		/// other recursively to run the cycle with a delay. I solve this with an atrificial
		/// limit of 8 multimanager recursions.
		/// TODO: It would be better to detect this problem when it happens.
		/// TODO: Instead of adding more attributes, parse into connections.
		/// </summary>
		/// <param name="entity">The multi_manager to parse.</param>
		/// <returns>The parsed multi_manager. This will have all targets as <see cref="Entity.EntityConnection"/> objects.</returns>
		private Entity ParseMultimanager(Entity entity) {
			++_mmStackLength;
			Entity dummy = new Entity(entity);
			dummy.Remove("classname");
			dummy.Remove("origin");
			dummy.Remove("angles");
			dummy.Remove("targetname");
			List<string> delete = new List<string>();
			foreach (KeyValuePair<string, string> kvp in dummy) {
				string targetname = kvp.Key;
				double delay = dummy.GetFloat(kvp.Key, 0.0f);
				for (int i = targetname.Length - 1; i >= 0; --i) {
					if (targetname[i] == '#') {
						targetname = targetname.Substring(0, i);
						break;
					}
				}
				Entity[] targets = GetTargets(targetname);
				delete.Add(kvp.Key);
				for (int i = 0; i < targets.Length; i++) {
					if (entity.ValueIs("classname", "multi_kill_manager")) {
						if (targets.Length > 1) {
							dummy.connections.Add(new Entity.EntityConnection() { name = "condition", target = targetname + i, action = "Kill", param = "", delay = delay, fireOnce = -1, unknown0 = "", unknown1 = "" });
						} else {
							dummy.connections.Add(new Entity.EntityConnection() { name = "condition", target = targetname, action = "Kill", param = "", delay = delay, fireOnce = -1, unknown0 = "", unknown1 = "" });
						}
					} else {
						if (targets[i].ValueIs("classname", "multi_manager") || targets[i].ValueIs("classname", "multi_kill_manager")) {
							if (_mmStackLength <= 8) {
							//if (_mmStackLength <= Settings.MMStackSize) {
								Entity mm = ParseMultimanager(targets[i]);
								foreach (Entity.EntityConnection connection in mm.connections) {
									dummy.connections.Add(new Entity.EntityConnection() { name = connection.name, target = connection.target, action = connection.action, param = connection.param, delay = connection.delay + delay, fireOnce = connection.fireOnce, unknown0 = connection.unknown0, unknown1 = connection.unknown1 });
								}
							} else {
								_master.Print("WARNING: Multimanager stack overflow on entity " + entity["targetname"] + " calling " + targets[i]["targetname"] + "!");
								_master.Print("This is probably because of multi_managers repeatedly calling eachother.");
							}
						} else {
							if (targets.Length > 1) {
								string outputAction = targets[i].OnFire();
								if (entity.ValueIs("triggerstate", "0")) {
									outputAction = targets[i].OnDisable();
								} else {
									if (entity.ValueIs("triggerstate", "1")) {
										outputAction = targets[i].OnEnable();
									}
								}
								dummy.connections.Add(new Entity.EntityConnection() { name = "condition", target = targetname + i, action = outputAction, param = "", delay = delay, fireOnce = -1, unknown0 = "", unknown1 = "" });
							} else if (targets.Length == 1) {
								string outputAction = targets[0].OnFire();
								if (entity.ValueIs("triggerstate", "0")) {
									outputAction = targets[0].OnDisable();
								} else {
									if (entity.ValueIs("triggerstate", "1")) {
										outputAction = targets[0].OnEnable();
									}
								}
								dummy.connections.Add(new Entity.EntityConnection() { name = "condition", target = targetname, action = outputAction, param = "", delay = delay, fireOnce = -1, unknown0 = "", unknown1 = "" });
							} else {
								dummy.connections.Add(new Entity.EntityConnection() { name = "condition", target = targetname, action = "Toggle", param = "", delay = delay, fireOnce = -1, unknown0 = "", unknown1 = "" });
							}
						}
					}
				}
			}
			foreach (string st in delete) {
				dummy.Remove(st);
			}
			--_mmStackLength;
			return dummy;
		}

		/// <summary>
		/// Since Source also requires explicit enable/disable on/off events (and many
		/// entities don't support the "Toggle" input) I can't have multiple entities
		/// with the same targetname. So these need to be distinguished and tracked.
		/// </summary>
		/// <param name="name">The targetname of entities to get.</param>
		/// <returns>An array of all <see cref="Entity"/> objects with targetname set to <paramref name="name"/> if they have unique FireActions, or an array of one <see cref="Entity"/> if all the FireAcitons are the same.</returns>
		private Entity[] GetTargets(string name) {
			bool numeralized = false;
			List<Entity> targets = new List<Entity>();
			int numNumeralized = 0;
			//foreach (string numeralizedTargetname in _numeralizedTargetnames) {
			for (int i = 0; i < _numeralizedTargetnames.Count; ++i) {
				if (_numeralizedTargetnames[i].Equals(name)) {
					numeralized = true;
					numNumeralized = _numTargets[i];
					break;
				}
			}
			if (numeralized) {
				targets = new List<Entity>(numNumeralized);
				for (int i = 0; i < numNumeralized; ++i) {
					targets.Add(_entities.GetWithName(name + i));
				}
			} else {
				targets = _entities.GetAllWithName(name);
				if (targets.Count > 1) {
					// Make sure each target needs its own Fire action and name
					bool unique = false;
					for (int i = 1; i < targets.Count; ++i) {
						if (!targets[0].OnFire().Equals(targets[i].OnFire())) {
							unique = true;
							break;
						}
					}
					if (!unique) {
						return new Entity[] { targets[0] };
					}
					_numeralizedTargetnames.Add(name);
					_numTargets.Add(targets.Count);
					for (int i = 0; i < targets.Count; ++i) {
						targets[i]["targetname"] = name + i;
					}
				}
			}
			return targets.ToArray<Entity>();
		}

		/// <summary>
		/// Every <see cref="MAPBrushSide"/> contained in <paramref name="brushes"/> will have its texture examined,
		/// and, if necessary, replaced with the equivalent for Hammer.
		/// </summary>
		/// <param name="brushes">The collection of <see cref="MAPBrush"/> objects to have textures parsed.</param>
		/// <param name="version">The <see cref="MapType"/> of the BSP this entity came from.</param>
		private void PostProcessTextures(IEnumerable<MAPBrush> brushes) {
			foreach (MAPBrush brush in brushes) {
				foreach (MAPBrushSide brushSide in brush.sides) {
					ValidateTexInfo(brushSide);
					PostProcessSpecialTexture(brushSide);
					switch (_version) {
						case MapType.Nightfire: {
							PostProcessNightfireTexture(brushSide);
							break;
						}
						case MapType.Quake2: {
							PostProcessQuake2Texture(brushSide);
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
						case MapType.L4D2:
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
				case "**nulltexture**":
				case "**nodrawtexture**": {
					brushSide.texture = "tools/toolsnodraw";
					break;
				}
				case "**skiptexture**": {
					brushSide.texture = "tools/toolsskip";
					break;
				}
				case "**skytexture**": {
					brushSide.texture = "tools/toolsskybox";
					break;
				}
				case "**hinttexture**": {
					brushSide.texture = "tools/toolshint";
					break;
				}
				case "**cliptexture**": {
					brushSide.texture = "tools/toolsclip";
					break;
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by Hammer, if necessary.
		/// </summary>
		/// <param name="brushSide">The <see cref="MAPBrushSide"/> to have its texture parsed.</param>
		private void PostProcessNightfireTexture(MAPBrushSide brushSide) {
			switch (brushSide.texture.ToLower()) {
				case "special/nodraw":
				case "special/null": {
					brushSide.texture = "tools/toolsnodraw";
					break;
				}
				case "special/clip": {
					brushSide.texture = "tools/toolsclip";
					break;
				}
				case "special/sky": {
					brushSide.texture = "tools/toolsskybox";
					break;
				}
				case "special/trigger": {
					brushSide.texture = "tools/toolstrigger";
					break;
				}
				case "special/playerclip": {
					brushSide.texture = "tools/toolsplayerclip";
					break;
				}
				case "special/npcclip":
				case "special/enemyclip": {
					brushSide.texture = "tools/toolsnpcclip";
					break;
				}
			}
		}

		/// <summary>
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by Hammer, if necessary.
		/// </summary>
		/// <param name="brushSide">The <see cref="MAPBrushSide"/> to have its texture parsed.</param>
		private void PostProcessQuake2Texture(MAPBrushSide brushSide) {
			if (brushSide.texture.Length >= 5 && brushSide.texture.Substring(brushSide.texture.Length - 5).Equals("/clip", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = "tools/toolsclip";
			} else if (brushSide.texture.Length >= 5 && brushSide.texture.Substring(brushSide.texture.Length - 5).Equals("/hint", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = "tools/toolshint";
			} else if (brushSide.texture.Length >= 8 && brushSide.texture.Substring(brushSide.texture.Length - 8).Equals("/trigger", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = "tools/toolstrigger";
			} else if (brushSide.texture.Equals("*** unsused_texinfo ***", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = "tools/toolsnodraw";
			}
		}

		/// <summary>
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by Hammer, if necessary.
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
					brushSide.texture = "tools/toolsclip";
					break;
				}
				case "common/nodrawnonsolid":
				case "system/trigger":
				case "common/trigger": {
					brushSide.texture = "tools/toolstrigger";
					break;
				}
				case "common/nodraw":
				case "common/caulkshadow":
				case "common/caulk":
				case "system/caulk":
				case "noshader": {
					brushSide.texture = "tools/toolsnodraw";
					break;
				}
				case "common/do_not_enter":
				case "common/donotenter":
				case "common/monsterclip": {
					brushSide.texture = "tools/toolsnpcclip";
					break;
				}
				case "common/caulksky":
				case "common/skyportal": {
					brushSide.texture = "tools/toolsskybox";
					break;
				}
				case "common/hint": {
					brushSide.texture = "tools/toolshint";
					break;
				}
				case "common/waterskip": {
					brushSide.texture = "liquids/!water";
					break;
				}
				case "system/do_not_enter":
				case "common/playerclip": {
					brushSide.texture = "tools/toolsplayerclip";
					break;
				}
			}
			if (brushSide.texture.Length >= 4 && brushSide.texture.Substring(0, 4).Equals("sky/", StringComparison.InvariantCultureIgnoreCase)) {
				brushSide.texture = "tools/toolsskybox";
			}
		}

		/// <summary>
		/// Postprocesser to convert the texture referenced by <paramref name="brushSide"/> into one used by Hammer, if necessary.
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
