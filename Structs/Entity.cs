using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BSPImporter {
	/// <summary>
	/// Class containing all data for a single <c>Entity</c>, including attributes, Source Entity I/O connections and solids.
	/// </summary>
	public class Entity : IComparable, IComparable<Entity> {

		public const char ConnectionMemberSeparater = (char)0x1B;

		protected Dictionary<string, string> attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		protected List<string> connections = new List<string>();
		//protected List<MAPBrush> brushes = new List<MAPBrush>();

		/// <summary>
		/// Gets whether this <c>Entity</c> is brush-based or not. Typically, brush-based entities have no "origin" attribute.
		/// </summary>
		public bool brushBased { get { return modelNumber >= 0; } }

		/// <summary>
		/// Gets the bmodel index of this entity as read from the BSP
		/// </summary>
		public int modelNumber {
			get {
				if (className == "worldspawn") { return 0; }
				if (!attributes.ContainsKey("model")) { return -1; }
				string model = attributes["model"];
				if (model.Length <= 1 || model[0] != '*') { return -1; }
				int num = -1;
				System.Int32.TryParse(model.Substring(1), out num);
				return num;
			}
		}

		/// <summary>
		/// Wrapper for the "spawnflags" attribute.
		/// </summary>
		public uint spawnflags {
			get {
				try {
					if (attributes.ContainsKey("spawnflags")) {
						return System.UInt32.Parse(attributes["spawnflags"]);
					} else {
						return 0;
					}
				} catch {
					return 0;
				}
			}
			protected set { attributes["spawnflags"] = value.ToString(); }
		}

		/// <summary>
		/// Wrapper for the "origin" attribute.
		/// </summary>
		public Vector3 origin {
			get { return GetVector("origin"); }
			protected set { attributes["origin"] = value.x + " " + value.y + " " + value.z; }
		}

		/// <summary>
		/// Wrapper for the "angles" attribute.
		/// </summary>
		public Vector3 angles {
			get { return GetVector("angles"); }
			protected set { attributes["angles"] = value.x + " " + value.y + " " + value.z; }
		}

		/// <summary>
		/// Wrapper for the "targetname" attribute.
		/// </summary>
		public string name {
			get {
				if (attributes.ContainsKey("targetname")) {
					return attributes["targetname"];
				} else {
					return "";
				}
			}
			protected set { attributes["targetname"] = value; }
		}

		/// <summary>
		/// Wrapper for the "classname" attribute.
		/// </summary>
		/// <remarks>If an entity has no class, it has no behavior. It's either an error or metadata.</remarks>
		public string className {
			get {
				if (attributes.ContainsKey("classname")) {
					return attributes["classname"];
				} else {
					return "";
				}
			}
			protected set { attributes["classname"] = value; }
		}

		/// <summary>
		/// Allows an attribute to be accessed easily using <c>Entity</c>["<paramref name="key" />"] notation.
		/// </summary>
		/// <remarks>
		/// It's up to the user to ensure the empty string doesn't cause problems, rather than returning null!
		/// </remarks>
		/// <param name="key">The attribute to retrieve</param>
		/// <returns>The value of the attribute if it exists, empty <c>string</c> otherwise</returns>
		public string this[string key] {
			get {
				if (attributes.ContainsKey(key)) {
					return attributes[key];
				} else {
					return "";
				}
			}
			protected set { attributes[key] = value; }
		}

		/// <summary>
		/// Initializes a new instance of an <c>Entity</c>, parsing the given <c>byte</c> array into an <c>Entity</c> structure.
		/// </summary>
		/// <param name="data">Array to parse</param>
		public Entity(byte[] data) : this(Encoding.ASCII.GetString(data).Split('\n')) { }

		/// <summary>
		/// Initializes a new instance of an <c>Entity</c> with the given classname.
		/// </summary>
		/// <param name="className">Classname of the new <C>Entity</C></param>
		public Entity(string className) {
			attributes.Add("classname", className);
		}

		/// <summary>
		/// Initializes a new instance of an <c>Entity</c> object with no initial properties.
		/// </summary>
		public Entity() { }

		/// <summary>
		/// Initializes a new instance of an <c>Entity</c> object, copying the attributes, connections and brushes of the passed <c>Entity</c>.
		/// </summary>
		/// <param name="copy">The <c>Entity</c> to copy</param>
		public Entity(Entity copy) {
			attributes = new Dictionary<string, string>(copy.attributes);
			connections = new List<string>(copy.connections);
		}

		/// <summary>
		/// Initializes a new instance of an <c>Entity</c>, parsing the given <c>string</c> array into an <c>Entity</c> structure.
		/// </summary>
		/// <param name="lines">Array of attributes, patches, brushes, displacements etc. to parse</param>
		public Entity(string[] lines) {
			int braceCount = 0;

			bool inConnections = false;
			bool inBrush = false;

			List<string> child = new List<string>();

			foreach (string line in lines) {
				string current = line.Trim(' ', '\t', '\r');

				if (string.IsNullOrEmpty(current)) {
					continue;
				}

				// If this line is a comment line
				if (current.Length >= 2 && current.Substring(0, 2) == "//") {
					continue;
				}

				// Perhaps I should not assume these will always be the first thing on the line
				if (current[0] == '{') {
					// If we're only one brace deep, and we have no prior information, assume a brush
					if (braceCount == 1 && !inBrush && !inConnections) {
						inBrush = true;
					}
					++braceCount;
				} else if (current[0] == '}') {
					--braceCount;
					if (inBrush) {
						child.Add(current);
						//brushes.Add(new MAPBrush(child.ToArray()));
						child = new List<string>();
					}
					if (braceCount == 1) {
						inBrush = false;
						inConnections = false;
					}
					continue;
				} else if (current.Length >= 5 && current.Substring(0, 5) == "solid") {
					inBrush = true;
					continue;
				} else if (current.Length >= 11 && current.Substring(0, 11) == "connections") {
					inConnections = true;
					continue;
				}

				if (inBrush) {
					child.Add(current);
					continue;
				}

				Add(current);
			}
		}

		/// <summary>
		/// Factory method to create an <c>Entity</c> from a <c>string</c> "<paramref name="st" />" where
		/// "<paramref name="st" />" contains all lines for the entity, including attributes, brushes, etc.
		/// </summary>
		/// <remarks>
		/// This was necessary since the <c>Entity</c>(<c>string</c>) constructor was already used in a different way
		/// </remarks>
		/// <param name="st">The data to parse</param>
		/// <returns>The resulting <c>Entity</c> object</returns>
		public static Entity FromString(string st) {
			return new Entity(st.Split('\n'));
		}

		/// <summary>
		/// Renames the attribute named "<paramref name="oldName" />" to "<paramref name="newName" />"
		/// </summary>
		/// <param name="oldName">Attribute to be renamed</param>
		/// <param name="newName">New name for this attribute</param>
		protected void RenameKey(string oldName, string newName) {
			if (attributes.ContainsKey(oldName)) {
				string val = attributes[oldName];
				attributes.Remove(oldName);
				if (attributes.ContainsKey(newName)) {
					attributes.Remove(newName);
					Debug.LogWarningFormat("Attribute {0} already existed in entity, overwritten!", newName);
				}
				attributes.Add(newName, val);
			}
		}

		/// <summary>
		/// Parses the input <c>string</c> "<paramref name="st" />" into a key/value pair and adds
		/// it as an attribute to this <c>Entity</c>
		/// </summary>
		/// <param name="st">The <c>string</c> to be parsed</param>
		protected void Add(string st) {
			string key = "";
			string val = "";
			bool inQuotes = false;
			bool isVal = false;
			int numCommas = 0;
			for (int i = 0; i < st.Length; ++i) {
				// Some entity values in Source can use escape sequenced quotes. Need to make sure not to parse those.
				if (st[i] == '\"' && (i == 0 || st[i - 1] != '\\')) {
					if (inQuotes) {
						if (isVal) {
							break;
						}
						isVal = true;
					}
					inQuotes = !inQuotes;
				} else {
					if (inQuotes) {
						if (!isVal) {
							key += st[i];
						} else {
							val += st[i];
							if (st[i] == ',' || st[i] == (char)0x1B) { numCommas++; }
						}
					}
				}
			}
			val.Replace("\\\"", "\"");
			if (key != null && key != "") {
				if (numCommas == 4 || numCommas == 6) {
					st = st.Replace(',', ConnectionMemberSeparater);
					connections.Add(st);
				} else {
					if (!attributes.ContainsKey(key)) {
						attributes.Add(key, val);
					}
				}
			}
		}

		/// <summary>
		/// Removes the attribute named "<paramref name="key" />" from this <c>Entity</c>
		/// </summary>
		/// <param name="key">Name of the attribute to be removed</param>
		/// <returns><c>true</c> if the attribute was removed successfully</returns>
		protected bool Remove(string key) {
			return attributes.Remove(key);
		}

		/// <summary>
		/// Adds the key/value pair to this <c>Entity</c>
		/// </summary>
		/// <param name="key">Name of the attribute to add</param>
		/// <param name="value">Value of the attribute to add</param>
		protected void Add(string key, string value) {
			attributes[key] = value;
		}

		/// <summary>
		/// Gets a string representation of this <c>Entity</c>.
		/// </summary>
		/// <returns>String representation of this <c>Entity</c></returns>
		public override string ToString() {
			StringBuilder output = new StringBuilder();
			output.Append("{\n");
			foreach (string st in attributes.Keys) {
				output.Append(string.Format("\"{0}\" \"{1}\"\n", st, attributes[st]));
			}
			if (connections.Count > 0) {
				output.Append("connections\n{\n");
				foreach (string connection in connections) {
					output.Append(connection + "\n");
				}
				output.Append("}\n");
			}
			return output + "}";
		}

		/// <summary>
		/// Checks if the attribute named "<paramref name="key" />" has the value "<paramref name="value" />"
		/// </summary>
		/// <param name="key">The attribute to check</param>
		/// <param name="value">The value to compare</param>
		/// <returns><c>true</c> if the values match</returns>
		public bool ValueIs(string key, string value) {
			return value.Equals(this[key], StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Checks if the bits in "spawnflags" corresponding to the set bits set in <paramref name="bits"/> are set
		/// </summary>
		/// <param name="bits">The bits to compare spawnflags to</param>
		/// <returns><c>true</c> if all bits that were set in <paramref name="bits" /> were set in spawnflags</returns>
		public bool SpawnflagsSet(uint bits) {
			return ((spawnflags & bits) == bits);
		}

		/// <summary>
		/// Toggles the bits in "spawnflags" which are set in <paramref name="bits" />
		/// </summary>
		/// <param name="bits">Bitmask of bits to toggle</param>
		protected void ToggleSpawnflags(uint bits) {
			this["spawnflags"] = (spawnflags ^ bits).ToString();
		}

		/// <summary>
		/// Clears the bits in "spawnflags" which are set in <paramref name="bits" />
		/// Equivalent to spawnflags = (<paramref name="bits" /> ^ 0xFFFFFFFF) & spawnflags
		/// </summary>
		/// <param name="bits">Bitmask of bits to clear</param>
		protected void ClearSpawnflags(uint bits) {
			ToggleSpawnflags(spawnflags & bits);
		}

		/// <summary>
		/// Sets the bits in "spawnflags" which are set in <paramref name="bits" />
		/// </summary>
		/// <param name="bits">Bitmask of bits to set</param>
		protected void SetSpawnflags(uint bits) {
			this["spawnflags"] = (spawnflags | bits).ToString();
		}

		/// <summary>
		/// Tries to determine what Source engine input this entity would perform when "fired".
		/// "Firing" an entity is used in practically all other engines for entity I/O, but
		/// Source replaced it with the input/output system which, while more powerful, makes
		/// my job that much harder. There is no generic "Fire" input, so I need to give a
		/// best guess as to the action that will actually be performed.
		/// </summary>
		/// <returns>The best match action for what this entity would do when fired in other engines</returns>
		public string OnFire() {
			switch (this["classname"]) {
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
						switch (this["triggermode"]) {
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
		/// to "enabling" the entity in prior engines
		/// </summary>
		/// <returns>The best match action for what this entity would do when enabled in other engines</returns>
		public string OnEnable() {
			switch (this["classname"]) {
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
		/// to "disabling" the entity in prior engines
		/// </summary>
		/// <returns>The best match action for what this entity would do when disabled in other engines</returns>
		public string OnDisable() {
			switch (this["classname"]) {
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
		/// Tries to determine which "Output" in Source Engine this <c>Entity</c> would use 
		/// to "fire" its targets in prior engines
		/// </summary>
		/// <returns>The best match "Output" for this <c>Entity</c></returns>
		public string FireAction() {
			switch (this["classname"]) {
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

		/// <summary>
		/// Determines whether the <c>Entity</c> contains the specified key.
		/// </summary>
		/// <param name="key">The key to check for</param>
		/// <returns><c>true</c> if the key exists in the <c>Entity</c></returns>
		public bool ContainsKey(string key) {
			return attributes.ContainsKey(key);
		}

		/// <summary>
		/// Gets a numeric value as a <c>float</c>.
		/// </summary>
		/// <param name="key">Name of the attribute to retrieve</param>
		/// <param name="failDefault">Value to return if <paramref name="key" /> doesn't exist, or couldn't be converted</param>
		/// <returns>The numeric value of the value corresponding to <paramref name="key" /></returns>
		public float GetFloat(string key, float failDefault = 0) {
			try {
				return Single.Parse(attributes[key]);
			} catch {
				return failDefault;
			}
		}

		/// <summary>
		/// Gets a numeric value as an <c>int</c>.
		/// </summary>
		/// <param name="key">Name of the attribute to retrieve</param>
		/// <param name="failDefault">Value to return if <paramref name="key" /> doesn't exist, or couldn't be converted</param>
		/// <returns>The numeric value of the value corresponding to <paramref name="key" /></returns>
		public int GetInt(string key, int failDefault = 0) {
			try {
				return Int32.Parse(attributes[key]);
			} catch {
				return failDefault;
			}
		}

		/// <summary>
		/// Gets a Vector value as a <c>Vector4</c>. This will only read as many values as are in the value, and can be
		/// implicitly converted to Vector3, Vector2, or Color.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="failDefault"></param>
		/// <returns></returns>
		public Vector4 GetVector(string key, Vector4 failDefault = default(Vector4)) {
			float[] results = new float[4];
			if (attributes.ContainsKey(key) && !string.IsNullOrEmpty(attributes[key])) {
				string[] nums = attributes[key].Split(' ');
				for (int i = 0; i < results.Length && i < nums.Length; ++i) {
					try {
						results[i] = System.Single.Parse(nums[i]);
					} catch {
						results[i] = 0;
					}
				}
			}
			return new Vector4(results[0], results[1], results[2], results[3]);
		}

		/// <summary>
		/// Compares this <c>Entity</c> to another object. First "classname" attributes are compared, then "targetname".
		/// Attributes are compared alphabetically. Targetnames are only compared if classnames match.
		/// </summary>
		/// <param name="obj"><c>Object</c> to compare to</param>
		/// <returns>Less than zero if this entity is first, 0 if they occur at the same time, greater than zero otherwise</returns>
		public int CompareTo(object obj) {
			if (obj == null) { return 1; }
			Entity other = obj as Entity;
			if (other == null) { throw new ArgumentException("Object is not an Entity"); }

			int firstTry = className.CompareTo(other.className);
			return firstTry != 0 ? firstTry : name.CompareTo(other.name);
		}

		/// <summary>
		/// Compares this <c>Entity</c> to another <c>Entity</c>. First "classname" attributes are compared, then "targetname".
		/// Attributes are compared alphabetically. Targetnames are only compared if classnames match.
		/// </summary>
		/// <param name="other"><c>Entity</c> to compare to</param>
		/// <returns>Less than zero if this entity is first, 0 if they occur at the same time, greater than zero otherwise</returns>
		public int CompareTo(Entity other) {
			if (other == null) { return 1; }
			int firstTry = className.CompareTo(other.className);
			return firstTry != 0 ? firstTry : name.CompareTo(other.name);
		}

	}
}
