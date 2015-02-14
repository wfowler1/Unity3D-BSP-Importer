using System;
using System.Collections.Generic;
// Lump class
// If special treatment is needed for a list, another class can be made to extend this one.
[Serializable] public class Lump<T> : List<T> {
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS

	private int lumpLength = 0;
	private int structLength = 0;

	// CONSTRUCTORS

	public Lump(int lumpLength, int structLength) {
		this.structLength = structLength;
		this.lumpLength = lumpLength;
	}

	public Lump(List<T> data, int lumpLength, int structLength) : base(data) {
		this.structLength = structLength;
		this.lumpLength = lumpLength;
	}

	public Lump(int lumpLength, int structLength, int initialCapacity) : base(initialCapacity) {
		this.structLength = structLength;
		this.lumpLength = lumpLength;
	}

	// METHODS
	public virtual bool hasFunnySize() {
		if(Count == 0 || structLength < 1) {
			return false;
		}
		return lumpLength % Count != 0;
	}

	// ACCESSORS/MUTATORS

	public int Length {
		get {
			return lumpLength;
		}
		set {
			lumpLength = value;
		}
	}

	public int StructLength {
		get { return structLength; }
	}
}