using System;
// NumList class

// This class holds an array of integers. These may be read from a lump as a list of
// byte, ubyte, short, ushort, int, uint, or long.
// This provides a unified structure for any number listing lumps.

public class NumList {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	public enum dataType {
		BYTE = 0,
		UBYTE = 1,
		SHORT = 2,
		USHORT = 3,
		INT = 4,
		UINT = 5,
		LONG = 6
	}
	
	private int length;
	private long[] elements;
	private dataType type;
	
	// CONSTRUCTORS
	
	// Takes a byte array, as if read from a FileInputStream
	public NumList(byte[] data, dataType type) {
		length = data.Length;
		this.type = type;
		switch (type) {
			case dataType.BYTE: 
				elements = new long[data.Length];
				unchecked {
					for (int i = 0; i < elements.Length; i++) {
						elements[i] = (long)((sbyte)data[i]);
					}
				}
				break;
			case dataType.UBYTE: 
				elements = new long[data.Length];
				for (int i = 0; i < elements.Length; i++) {
					elements[i] = (long)data[i];
				}
				break;
			case dataType.SHORT: 
				elements = new long[data.Length / 2];
				for (int i = 0; i < elements.Length; i++) {
					elements[i] = (long) DataReader.readShort(data[i * 2], data[(i * 2) + 1]);
				}
				break;
			case dataType.USHORT: 
				elements = new long[data.Length / 2];
				for (int i = 0; i < elements.Length; i++) {
					elements[i] = (long) DataReader.readUShort(data[i * 2], data[(i * 2) + 1]);
				}
				break;
			case dataType.INT: 
				elements = new long[data.Length / 4];
				for (int i = 0; i < elements.Length; i++) {
					elements[i] = (long) DataReader.readInt(data[i * 4], data[(i * 4) + 1], data[(i * 4) + 2], data[(i * 4) + 3]);
				}
				break;
			case dataType.UINT: 
				elements = new long[data.Length / 4];
				for (int i = 0; i < elements.Length; i++) {
					elements[i] = (long) DataReader.readUInt(data[i * 4], data[(i * 4) + 1], data[(i * 4) + 2], data[(i * 4) + 3]);
				}
				break;
			case dataType.LONG: 
				elements = new long[data.Length / 8];
				for (int i = 0; i < elements.Length; i++) {
					elements[i] = DataReader.readLong(data[i * 4], data[(i * 4) + 1], data[(i * 4) + 2], data[(i * 4) + 3], data[(i * 4) + 4], data[(i * 4) + 5], data[(i * 4) + 6], data[(i * 4) + 7]);
				}
				break;
		}
	}
	
	// METHODS
	public virtual bool hasFunnySize() {
		switch (type) {
			case dataType.BYTE: 
			case dataType.UBYTE: 
				return false;
			case dataType.SHORT: 
			case dataType.USHORT: 
				return (length % 2 != 0);
			case dataType.INT: 
			case dataType.UINT: 
				return (length % 4 != 0);
			case dataType.LONG: 
				return (length % 8 != 0);
		}
		return false;
	}
	
	// ACCESSORS/MUTATORS
	virtual public long this[int index] {
		get {
			return elements[index];
		}
	}
	
	// Returns the length (in bytes) of the lump
	public virtual int Length {
		get {
			return length;
		}
	}
	
	// Returns the number of elements.
	public virtual int Count {
		get {
			return elements.Length;
		}
	}
}