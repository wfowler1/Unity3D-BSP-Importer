using System;
// LumpObject class
// A base class for any given lump object. Holds the data as a byte array.
[Serializable]
public class LumpObject {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	private byte[] data;
	
	// CONSTRUCTORS
	public LumpObject(byte[] data) {
		this.data = data;
	}

	public LumpObject() {
		this.data = new byte[0];
	}
	
	// METHODS
	
	// ACCESSORS/MUTATORS
	public virtual byte[] Data {
		get {
			return data;
		}
		set {
			data = value;
		}
	}
	
	public virtual int Length {
		get {
			return data.Length;
		}
	}
}