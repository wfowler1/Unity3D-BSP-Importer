using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
// Entity class

// This class holds data on ONE entity. It's only really useful when
// used in an array along with many others. Each value is stored as
// a separate attribute, in a dictionary.

[Serializable]
public class Entity:LumpObject {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	private Dictionary<string, string> attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
	//private List<KeyValuePair<string, string>> connections = new List<KeyValuePair<string, string>>();
	private List<string> connections = new List<string>();
	
	// CONSTRUCTORS
	
	public Entity(byte[] data):this(Encoding.ASCII.GetString(data).Split((char)0x0A)) {
	}
	
	public Entity(string classname):base(new byte[0]) {
		attributes.Add("classname", classname);
	}
	
	public Entity(string[] atts):base(new byte[0]) {
		attributes = new Dictionary<string, string>(atts.Length, StringComparer.InvariantCultureIgnoreCase);
		for (int i = 0; i < atts.Length; i++) {
			addAttribute(atts[i]);
		}
	}
	
	public Entity():base(new byte[0]) {
	}
	
	public Entity(Entity copy):base(copy.Data) {
		attributes = new Dictionary<string, string>(copy.Attributes);
		connections = new List<string>(copy.connections);
	}

	public Entity(Dictionary<string, string> atts, List<string> connects):base(new byte[0]) {
		attributes = new Dictionary<string,string>(atts);
		connections = new List<string>(connects);
	}

	public Entity(Dictionary<string, string> atts):this(atts, new List<string>()) {
	}
	
	// METHODS
	
	// renameAttribute(String, String)
	// Renames the specified attribute to the second String.
	public virtual void renameAttribute(string attribute, string to) {
		if(attributes.ContainsKey(attribute)) {
			string val = attributes[attribute];
			attributes.Remove(attribute);
			if(attributes.ContainsKey(to)) { attributes.Remove(to); Debug.Log("WARNING: Attribute "+to+" already existed in entity, overwritten!"); }
			attributes.Add(to, val);
		}
	}
	
	// addAttribute(String)
	// Simply adds the input String to the attribute list. This String can be anything,
	// even containing newlines or curly braces. BE CAREFUL.
	public virtual void addAttribute(string st) {
		string key = "";
		string val = "";
		bool inQuotes = false;
		bool isVal = false;
		int numCommas = 0;
		for(int i = 0; i < st.Length; i++) {
			// Some entity values in Source can use escape sequenced quotes. Need to make sure not to parse those.
			if(st[i]=='\"' && (i==0 || st[i-1]!='\\')) {
				if(inQuotes) {
					if(isVal) {
						break;
					}
					isVal=true;
				}
				inQuotes = !inQuotes;
			} else {
				if(inQuotes) {
					if(!isVal) {
						key+=st[i];
					} else {
						val+=st[i];
						if(st[i]==',' || st[i]==(char)0x1B) { numCommas++; }
					}
				}
			}
		}
		val.Replace("\\\"", "\"");
		if(key != null && key != "") {
			if((numCommas == 4 || numCommas == 6)/* && ((type >= BSP.TYPE_SOURCE17 && type <= BSP.TYPE_SOURCE23) || type == BSP.TYPE_VINDICTUS || type == BSP.TYPE_TACTICALINTERVENTION || type == BSP.DMOMAM)*/) {
				st = st.Replace((char)0x1B, ',');
				connections.Add(st);
			} else {
				if(!attributes.ContainsKey(key)) {
					attributes.Add(key, val);
				}
			}
		}
	}

	public bool Remove(string target) {
		return attributes.Remove(target);
	}

	public void Add(string key, string val) {
		if(attributes.ContainsKey(key)) {
			attributes.Remove(key);
		}
		attributes.Add(key, val);
	}
	
	// +toString()
	// Returns a string representation of this entity.
	// Pretty much the entity exactly as it was loaded.
	public override string ToString() {
		string output = "{\n";
		foreach (string st in attributes.Keys) {
			output += "\""+st+"\" \""+attributes[st]+"\"\n";
		}
		foreach (string connection in connections) {
			output += connection;
		}
		return output+"}";
	}
	
	// attributeIs(String, String)
	// Returns true if the attribute String1 exists and is equivalent to String2
	public virtual bool attributeIs(string attribute, string check) {
		return check.Equals(this[attribute], StringComparison.InvariantCultureIgnoreCase);
	}
	
	// Returns true if the bits in "spawnflags" corresponding to the set bits in 'check' are set
	public virtual bool spawnflagsSet(int check) {
		return ((Spawnflags & check) == check);
	}
	
	// Toggles the bits in "spawnflags" which are set in "check"
	public virtual void toggleSpawnflags(int toggle) {
		this["spawnflags"]=(Spawnflags ^ toggle).ToString();
	}
	
	// Disables the bits in "spawnflags" which are set in "check"
	// Alternate method: spawnflags = (disable ^ 0xFFFFFFFF) & spawnflags
	public virtual void disableSpawnflags(int disable) {
		toggleSpawnflags(Spawnflags & disable);
	}
	
	// Enables the bits in "spawnflags" which are set in "check"
	public virtual void enableSpawnflags(int enable) {
		this["spawnflags"]=(Spawnflags | enable).ToString();
	}
	
	// Try to determine what Source engine input this entity would perform when "fired".
	// "Firing" an entity is used in practically all other engines for entity I/O, but
	// Source replaced it with the input/output system which, while more powerful, makes
	// my job that much harder. There is no generic "Fire" input, so I need to give a
	// best guess as to the action that will actually be performed.
	public virtual string onFire()
	{
		if (attributeIs("classname", "func_door") || attributeIs("classname", "func_door_rotating") || attributeIs("classname", "trigger_hurt") || attributeIs("classname", "func_brush") || attributeIs("classname", "light") || attributeIs("classname", "light_spot"))
		{
			return "Toggle";
		}
		if (attributeIs("classname", "ambient_generic"))
		{
			return "ToggleSound";
		}
		if (attributeIs("classname", "env_message"))
		{
			return "ShowMessage";
		}
		if (attributeIs("classname", "trigger_changelevel"))
		{
			return "ChangeLevel";
		}
		if (attributeIs("classname", "env_global"))
		{
			if (attributeIs("triggermode", "1"))
			{
				return "TurnOn";
			}
			else
			{
				if (attributeIs("triggermode", "3"))
				{
					return "Toggle";
				}
				else
				{
					return "TurnOff";
				}
			}
		}
		if (attributeIs("classname", "func_breakable"))
		{
			return "Break";
		}
		if (attributeIs("classname", "func_button"))
		{
			return "Press";
		}
		if (attributeIs("classname", "env_shake"))
		{
			return "StartShake";
		}
		if (attributeIs("classname", "env_fade"))
		{
			return "Fade";
		}
		if (attributeIs("classname", "env_sprite"))
		{
			return "ToggleSprite";
		}
		if (attributeIs("classname", "logic_relay"))
		{
			return "Trigger";
		}
		if (attributeIs("classname", "math_counter"))
		{
			return "Add,1";
		}
		return "Toggle";
	}
	
	public virtual string onEnable()
	{
		if (attributeIs("classname", "trigger_hurt") || attributeIs("classname", "func_brush") || attributeIs("classname", "logic_relay") || attributeIs("classname", "math_counter"))
		{
			return "Enable";
		}
		if (attributeIs("classname", "func_door") || attributeIs("classname", "func_door_rotating"))
		{
			return "Open";
		}
		if (attributeIs("classname", "ambient_generic"))
		{
			return "PlaySound";
		}
		if (attributeIs("classname", "env_message"))
		{
			return "ShowMessage";
		}
		if (attributeIs("classname", "trigger_changelevel"))
		{
			return "ChangeLevel";
		}
		if (attributeIs("classname", "light") || attributeIs("classname", "light_spot"))
		{
			return "TurnOn";
		}
		if (attributeIs("classname", "func_breakable"))
		{
			return "Break";
		}
		if (attributeIs("classname", "env_shake"))
		{
			return "StartShake";
		}
		if (attributeIs("classname", "env_fade"))
		{
			return "Fade";
		}
		if (attributeIs("classname", "env_sprite"))
		{
			return "ShowSprite";
		}
		if (attributeIs("classname", "func_button"))
		{
			return "PressIn";
		}
		return "Enable";
	}
	
	public virtual string onDisable()
	{
		if (attributeIs("classname", "trigger_hurt") || attributeIs("classname", "func_brush") || attributeIs("classname", "logic_relay") || attributeIs("classname", "math_counter"))
		{
			return "Disable";
		}
		if (attributeIs("classname", "func_door") || attributeIs("classname", "func_door_rotating"))
		{
			return "Close";
		}
		if (attributeIs("classname", "ambient_generic"))
		{
			return "StopSound";
		}
		if (attributeIs("classname", "env_message"))
		{
			return "ShowMessage";
		}
		if (attributeIs("classname", "trigger_changelevel"))
		{
			return "ChangeLevel";
		}
		if (attributeIs("classname", "light") || attributeIs("classname", "light_spot"))
		{
			return "TurnOff";
		}
		if (attributeIs("classname", "func_breakable"))
		{
			return "Break";
		}
		if (attributeIs("classname", "env_shake"))
		{
			return "StopShake";
		}
		if (attributeIs("classname", "env_fade"))
		{
			return "Fade";
		}
		if (attributeIs("classname", "env_sprite"))
		{
			return "HideSprite";
		}
		if (attributeIs("classname", "func_button"))
		{
			return "PressOut";
		}
		return "Disable";
	}
	
	// Try to determine which "Output" normally causes this entity to "fire" its target.
	public virtual string fireAction()
	{
		if (attributeIs("classname", "func_button") || attributeIs("classname", "func_rot_button") || attributeIs("classname", "momentary_rot_button"))
		{
			return "OnPressed";
		}
		if (attributeIs("classname", "trigger_multiple") || attributeIs("classname", "trigger_once") || attributeIs("classname", "logic_relay"))
		{
			return "OnTrigger";
		}
		if (attributeIs("classname", "logic_auto"))
		{
			return "OnNewGame";
		}
		if (attributeIs("classname", "func_door") || attributeIs("classname", "func_door_rotating"))
		{
			return "OnOpen";
		}
		if (attributeIs("classname", "func_breakable"))
		{
			return "OnBreak";
		}
		if (attributeIs("classname", "math_counter"))
		{
			return "OnHitMax";
		}
		return "OnTrigger";
	}
	
	public static Entity cloneNoBrushes(Entity copy) {
		return new Entity(copy.Attributes, copy.Connections);
	}
	
	public virtual Entity cloneNoBrushes() {
		return new Entity(attributes, connections);
	}

	public static Entity parseString(string data) {
		return new Entity(data.Split((char)0x0A));
	}
	
	public static Entities createLump(byte[] data) {
		int count = 0;
		bool inQuotes = false; // Keep track of whether or not we're currently in a set of quotation marks.
		// I came across a map where the idiot map maker used { and } within a value. This broke the code prior to revision 55.
		for (int i = 0; i < data.Length; i++) {
			if (inQuotes) {
				if (data[i] == '\"' && inQuotes) {
					inQuotes = false;
				}
			} else {
				if (data[i] == '\"') {
					inQuotes = true;
				} else {
					if (data[i] == '{') {
						count++;
					}
				}
			}
		}
		Entities lump = new Entities(data.Length, count);
		char currentChar; // The current character being read in the file. This is necessary because
		// we need to know exactly when the { and } characters occur and capture
		// all text between them.
		int offset = 0;
		for (int i = 0; i < count; i++) {
			// For every entity
			string current = ""; // This will be the resulting entity, fed into the Entity class
			currentChar = (char)data[offset]; // begin reading the file
			while (currentChar != '{') {
				// Eat bytes until we find the beginning of an entity structure
				offset++;
				currentChar = (char)data[offset];
			}
			inQuotes = false;
			do {
				if (currentChar == '\"') {
					inQuotes = !inQuotes;
				}
				current += (currentChar + ""); // adds characters to the current string
				offset++;
				currentChar = (char)data[offset];
			} while (currentChar != '}' || inQuotes); // Read bytes until we find the end of the current entity structure
			current += (currentChar + ""); // adds the '}' to the current string
			lump.Add(Entity.parseString(current));
		}
		return lump;
	}
	
	public bool hasAttribute(string att) {
		return attributes.ContainsKey(att);
	}
	
	// ACCESSORS/MUTATORS
	virtual public bool BrushBased {
		get {
			return (ModelNumber >= 0);
		}
	}

	virtual public int Spawnflags {
		get {
			try {
				return System.Int32.Parse(attributes["spawnflags"]);
			} catch {
				return 0;
			}
		}
	}

	virtual public Dictionary<string, string> Attributes {
		get {
			return attributes;
		}
	}

	virtual public List<string> Connections {
		get {
			return connections;
		}
	}

	virtual public int ModelNumber {
		// If there's a model number in the attributes list, this method fetches it
		// and returns it. If there is no model defined, or it's not a numerical 
		// value, then -1 is returned. If it's the worldspawn then a 0 is returned.
		get {
			try {
				if(attributes["classname"] == "worldspawn") {
					return 0;
				} else {
					if(attributes.ContainsKey("model")) {
						string st = attributes["model"];
						if(st[0]=='*') {
							try {
								return System.Int32.Parse(st.Substring(1));
							} catch(System.FormatException) {
								return -1;
							}
						} else {
							return -1;
						}
					} else {
						return -1;
					}
				}
			} catch {
				return -1;
			}
		}
	}

	virtual public Vector3 Origin {
		// Returns the three components of the entity's "origin" attribute as a Vector3
		get {
			float[] point = new float[3];
			//if (attributes.ContainsKey("origin") && !string.IsNullOrWhiteSpace(attributes["origin"])) {
			if(attributes.ContainsKey("origin") && attributes["origin"] != null && attributes["origin"] != "") {
				string[] origin = attributes["origin"].Split(' ');
				for (int i = 0; i < 3 && i < origin.Length; i++) {
					try {
						point[i] = System.Single.Parse(origin[i]);
					} catch {
						return Vector3.zero;
					}
				}
			}
			return new Vector3(point[0], point[1], point[2]);
		}
		set {
			this["origin"] = value.x+" "+value.y+" "+value.z;
		}
	}

	virtual public Vector3 Angles {
		// Returns the three components of the entity's "angles" attribute as a Vector3
		get {
			float[] euler = new float[3];
			//if (attributes.ContainsKey("angles") && !string.IsNullOrWhiteSpace(attributes["angles"])) {
			if(attributes.ContainsKey("angles") && attributes["angles"] != null && attributes["angles"] != "") {
				string[] angles = attributes["angles"].Split(' ');
				for (int i = 0; i < 3 && i < angles.Length; i++) {
					try {
						euler[i] = System.Single.Parse(angles[i]);
					} catch {
						return Vector3.zero;
					}
				}
			}
			return new Vector3(euler[0], euler[1], euler[2]);
		}
		set {
			this["angles"] = value.x+" "+value.y+" "+value.z;
		}
	}
	
	// Set an attribute. If it doesn't exist, it is added. If it does, it is
	// overwritten with the new one, since that's much easier to do than edit
	// the preexisting one.
	public virtual string this[string attribute] {
		set {
			if(attributes.ContainsKey(attribute)) {
				attributes[attribute] = value;
			} else {
				attributes.Add(attribute, value);
			}
		}
		get {
			if(attributes.ContainsKey(attribute)) {
				return attributes[attribute];
			} else {
				return "";
			}
		}
	}
}