using System;
// LumpObject class
// A base class for any given lump object. Holds the data as a byte array.
[Serializable]
public class LumpObject:IEquatable<LumpObject> {
	
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
	public static bool operator ==(LumpObject o1, LumpObject o2) {
		if(Object.ReferenceEquals(o1, null) ^ Object.ReferenceEquals(o2, null)) { return false; }
		if(Object.ReferenceEquals(o1, null) && Object.ReferenceEquals(o2, null)) { return true; }
		if(o1.Length == o2.Length && o1.Length != 0) {
			for(int i=0;i<o1.Length;i++) {
				if(o1.Data[i] != o2.Data[i]) {
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public static bool operator !=(LumpObject o1, LumpObject o2) {
		if(Object.ReferenceEquals(o1, null) ^ Object.ReferenceEquals(o2, null)) { return true; }
		if(Object.ReferenceEquals(o1, null) && Object.ReferenceEquals(o2, null)) { return false; }
		if(o1.Length == o2.Length) {
			for(int i=0;i<o1.Length;i++) {
				if(o1.Data[i] != o2.Data[i]) {
					return true;
				}
			}
			return false;
		}
		return true;
	}

	public bool Equals(LumpObject o2) {
		return this==o2;
	}
	
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