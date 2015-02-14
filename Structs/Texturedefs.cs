using System;
using System.Collections.Generic;
// Textures class

// Extends LumpObject with some useful methods for manipulating Texture objects,
// especially when handling them as a group.

public class Texturedefs:Lump<Texturedef> {
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
		
	// CONSTRUCTORS
	
	public Texturedefs(List<Texturedef> elements, int length, int structLength):base(elements, length, structLength) {
	}
	
	// METHODS
	[Obsolete("Texturedefs.printTextures() is for debug purposes only!")]
	public virtual void printTextures() {
		// FOR DEBUG PURPOSES ONLY
		for (int i = 0; i < Count; i++) {
			System.Console.Out.WriteLine(this[i].Name);
		}
	}
	
	// ACCESSORS/MUTATORS
	
	public virtual string getTextureAtOffset(uint target) {
		int offset = 0;
		for (int i = 0; i < Count; i++) {
			if (offset < target) {
				offset += this[i].Name.Length + 1; // Add 1 for the now missing null byte. I really did think of everything! :D
			} else {
				return this[i].Name;
			}
		}
		// If we get to this point, the strings ended before target offset was reached
		return null; // Perhaps this will throw an exception down the line? :trollface:
	}
	
	public virtual int getOffsetOf(string inTexture) {
		int offset = 0;
		for (int i = 0; i < Count; i++) {
			if (!this[i].Name.Equals(inTexture, StringComparison.CurrentCultureIgnoreCase)) {
				offset += this[i].Name.Length + 1;
			} else {
				return offset;
			}
		}
		// If we get to here, the requested texture didn't exist.
		return - 1; // This will PROBABLY throw an exception later.
	}
}