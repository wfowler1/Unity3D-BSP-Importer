using System;
using System.Collections.Generic;
// SourceStaticProps class

// Extends the Lump class, contains data only relevant to Static Props, like
// the dictionary of actual model paths.

public class SourceStaticProps:Lump<SourceStaticProp>
{
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	private string[] dictionary;
	
	// CONSTRUCTORS
	
	// Takes a byte array, as if read from a FileInputStream
	public SourceStaticProps(List<SourceStaticProp> elements, string[] dictionary, int length):base(elements, length, 0) {
		this.dictionary = dictionary;
	}
	
	// METHODS
	
	// ACCESSORS/MUTATORS
	virtual public string[] Dictionary {
		get {
			return dictionary;
		}
	}
}