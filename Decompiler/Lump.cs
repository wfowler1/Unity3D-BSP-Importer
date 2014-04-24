using System;
using System.Collections.Generic;
// Lump class
// If special treatment is needed for a list, another class can be made to extend this one.
[Serializable]
public class Lump<T>:List<T> where T:LumpObject, new() {
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS

	private int lumpLength = 0;
	private int structLength = 0;
	
	// CONSTRUCTORS

	public Lump(int lumpLength, int structLength) {
		this.structLength = structLength;
		this.lumpLength = lumpLength;
	}

	public Lump(List<T> data, int lumpLength, int structLength):base(data) {
		this.structLength = structLength;
		this.lumpLength = lumpLength;
	}

	public Lump(int lumpLength, int structLength, int initialCapacity):base(initialCapacity) {
		this.structLength = structLength;
		this.lumpLength = lumpLength;
	}

	// DO NOT USE THIS CONSTRUCTOR WITH A STRUCTLENGTH OF 0
	public Lump(byte[] data, int structLength):base(data.Length / structLength) {
		for(int i=0; i<data.Length / structLength; i++) {
			byte[] objectData = new byte[structLength];
			for(int j=0;j<structLength;j++) {
				objectData[j] = data[(i*structLength) + j];
			}
			T newObject = new T();
			newObject.Data = objectData;
			this.Add(newObject);
		}
	}
	
	// METHODS
	public virtual bool hasFunnySize() {
		if (Count == 0 || structLength < 1) {
			return false;
		}
		return lumpLength % Count != 0;
	}
	
	// ACCESSORS/MUTATORS
	
	public virtual int Length {
		get {
			return lumpLength;
		}
		set {
			lumpLength = value;
		}
	}
}