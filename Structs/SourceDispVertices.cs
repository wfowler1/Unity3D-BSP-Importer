using System;
using System.Collections.Generic;
// SourceDispVertices class

// Extends Lump class, and contains methods only useful for Displacement vertices.
// Only one method in this class, can it go somewhere else?

public class SourceDispVertices:Lump<SourceDispVertex> {
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	// CONSTRUCTORS
	public SourceDispVertices(List<SourceDispVertex> elements, int length):base(elements, length, 20) {
	}
	
	// METHODS
	
	// ACCESSORS/MUTATORS
	
	public virtual SourceDispVertex[] getVertsInDisp(int first, int power) {
		int numVerts = 0;
		switch (power) {
			case 2: 
				numVerts = 25;
				break;
			case 3: 
				numVerts = 81;
				break;
			case 4: 
				numVerts = 289;
				break;
		}
		SourceDispVertex[] output = new SourceDispVertex[numVerts];
		for (int i = 0; i < numVerts; i++) {
			output[i] = this[first + i];
		}
		return output;
	}
}