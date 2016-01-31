using System;
using System.Collections.Generic;
using System.Linq;

using LibBSP;

namespace Decompiler {
	/// <summary>
	/// Static class containing helper functions for operating with <see cref="Entity"/> objects.
	/// </summary>
	public static class EntityExtensions {

		/// <summary>
		/// Tries to determine what Source engine input this entity would perform when "fired".
		/// "Firing" an entity is used in practically all other engines for entity I/O, but
		/// Source replaced it with the input/output system which is more powerful. A best match
		/// action must be found for the action the entity would have taken in other enignes.
		/// </summary>
		/// <param name="entity">This <see cref="Entity"/> object.</param>
		/// <returns>The best match action for what this entity would do when fired in other engines.</returns>
		public static string OnFire(this Entity entity) {
			switch (entity["classname"]) {
				case "env_shake": {
					return "StartShake";
				}
				case "env_fade": {
					return "Fade";
				}
				case "env_sprite": {
					return "ToggleSprite";
				}
				case "logic_relay": {
					return "Trigger";
				}
				case "math_counter": {
					return "Add,1";
				}
				case "func_button": {
					return "Press";
				}
				case "func_breakable": {
					return "Break";
				}
				case "env_global": {
					switch (entity["triggermode"]) {
						case "1": {
							return "TurnOn";
						}
						case "3": {
							return "Toggle";
						}
						default: {
							return "TurnOff";
						}
					}
				}
				case "trigger_changelevel": {
					return "ChangeLevel";
				}
				case "env_message": {
					return "ShowMessage";
				}
				case "ambient_generic": {
					return "ToggleSound";
				}
				case "func_door":
				case "func_door_rotating":
				case "trigger_hurt":
				case "func_brush":
				case "light":
				case "light_spot":
				default: {
					return "Toggle";
				}
			}

		}

		/// <summary>
		/// Tries to determine what action in Source Engine's Entity I/O would be equivalent
		/// to "enabling" the entity in prior engines.
		/// </summary>
		/// <param name="entity">This <see cref="Entity"/> object.</param>
		/// <returns>The best match action for what this entity would do when enabled in other engines.</returns>
		public static string OnEnable(this Entity entity) {
			switch (entity["classname"]) {
				case "func_door":
				case "func_door_rotating": {
					return "Open";
				}
				case "ambient_generic": {
					return "PlaySound";
				}
				case "env_message": {
					return "ShowMessage";
				}
				case "trigger_changelevel": {
					return "ChangeLevel";
				}
				case "light":
				case "light_spot": {
					return "TurnOn";
				}
				case "func_breakable": {
					return "Break";
				}
				case "env_shake": {
					return "StartShake";
				}
				case "env_fade": {
					return "Fade";
				}
				case "env_sprite": {
					return "ShowSprite";
				}
				case "func_button": {
					return "PressIn";
				}
				case "trigger_hurt":
				case "func_brush":
				case "logic_relay":
				case "math_counter":
				default: {
					return "Enable";
				}
			}
		}

		/// <summary>
		/// Tries to determine what action in Source Engine's Entity I/O would be equivalent
		/// to "disabling" the entity in prior engines.
		/// </summary>
		/// <param name="entity">This <see cref="Entity"/> object.</param>
		/// <returns>The best match action for what this entity would do when disabled in other engines.</returns>
		public static string OnDisable(this Entity entity) {
			switch (entity["classname"]) {
				case "func_door":
				case "func_door_rotating": {
					return "Close";
				}
				case "ambient_generic": {
					return "StopSound";
				}
				case "env_message": {
					return "ShowMessage";
				}
				case "trigger_changelevel": {
					return "ChangeLevel";
				}
				case "light":
				case "light_spot": {
					return "TurnOff";
				}
				case "func_breakable": {
					return "Break";
				}
				case "env_shake": {
					return "StopShake";
				}
				case "env_fade": {
					return "Fade";
				}
				case "env_sprite": {
					return "HideSprite";
				}
				case "func_button": {
					return "PressOut";
				}
				case "trigger_hurt":
				case "func_brush":
				case "logic_relay":
				case "math_counter":
				default: {
					return "Disable";
				}
			}
		}

		/// <summary>
		/// Tries to determine which "Output" in Source Engine an <c>Entity</c> would use 
		/// to "fire" its targets in prior engines.
		/// </summary>
		/// <param name="entity">This <see cref="Entity"/> object.</param>
		/// <returns>The best match "Output" for this <see cref="Entity"/>.</returns>
		public static string FireAction(this Entity entity) {
			switch (entity["classname"]) {
				case "func_button":
				case "func_rot_button":
				case "momentary_rot_button": {
					return "OnPressed";
				}
				case "logic_auto": {
					return "OnNewGame";
				}
				case "func_door":
				case "func_door_rotating": {
					return "OnOpen";
				}
				case "func_breakable": {
					return "OnBreak";
				}
				case "math_counter": {
					return "OnHitMax";
				}
				case "trigger_multiple":
				case "trigger_once":
				case "logic_relay":
				default: {
					return "OnTrigger";
				}
			}
		}

	}
}
