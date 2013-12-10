using System;
using System.Collections.Generic;
// Entities class

// This class extends the Lump class, providing methods which are only useful
// for a group of Entity objects (and not really anything else).
[Serializable]
public class Entities:Lump<Entity> {
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS

	// CONSTRUCTORS

	// This one accepts an Entities and copies it
	public Entities(Entities data):base(data, 0, 0) {
	}
	
	public Entities(List<Entity> data, int length):base(data, length, 0) {
	}

	public Entities(int length, int initialcapacity):base(length, 0, initialcapacity) {
	}
	
	public Entities():base(0, 0) {
	}
	
	// METHODS
	
	// +deleteAllWithAttribute(String, String)
	// Deletes all entities with attribute set to value
	public virtual void deleteAllWithAttribute(string attribute, string keyvalue) {
		deleteEnts(findAllWithAttribute(attribute, keyvalue));
	}
	
	// +deleteEnts(int[])
	// Deletes the entities specified at all indices in the int[] array.
	public virtual void deleteEnts(int[] data) {
		for (int i = 0; i < data.Length; i++) {
			// For each element in the array
			RemoveAt(data[i]); // Delete the element
			for (int j = i + 1; j < data.Length; j++) {
				// for each element that still needs to be deleted
				if (data[i] < data[j]) {
					// if the element that still needs deleting has an index higher than what was just deleted
					data[j]--; // Subtract one from that element's index to compensate for the changed list
				}
			}
		}
	}
	
	// +findAllWithAttribute(String, String)
	// Returns an array of indices of the getElements() with the specified attribute set to
	// the specified value
	public virtual int[] findAllWithAttribute(string attribute, string keyvalue) {
		int[] indices = new int[0];
		for (int i = 0; i < Count; i++) {
			if (this[i].attributeIs(attribute, keyvalue)) {
				int[] newList = new int[indices.Length + 1];
				for (int j = 0; j < indices.Length; j++) {
					newList[j] = indices[j];
				}
				newList[newList.Length - 1] = i;
				indices = newList;
			}
		}
		return indices;
	}
	
	// Returns the actual getElements() with the specified field
	public virtual Entity[] returnAllWithAttribute(string attribute, string keyvalue) {
		int[] indices = findAllWithAttribute(attribute, keyvalue);
		Entity[] ents = new Entity[indices.Length];
		for (int i = 0; i < ents.Length; i++) {
			ents[i] = this[indices[i]];
		}
		return ents;
	}
	
	// Returns all getElements() with the specified targetname
	public virtual Entity[] returnAllWithName(string targetname) {
		return returnAllWithAttribute("targetname", targetname);
	}
	
	// Returns ONE (the first) entity with the specified targetname
	public virtual Entity returnWithName(string targetname) {
		for (int i = 0; i < Count; i++) {
			if (this[i].attributeIs("targetname", targetname)) {
				return this[i];
			}
		}
		return null;
	}
	
	// ACCESSORS/MUTATORS
}