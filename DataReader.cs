using System;
using UnityEngine;
// DataReader class

// Static class
// Contains methods for reading certain data types from byte
// arrays. Useful for:
// Reading data from a bytesteam
// Avoiding confusion between big and little endian values (all are assumed little endian)
// Cleaning up code

public class DataReader {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	// CONSTRUCTORS
	
	// METHODS
	
	public static short readShort(byte first, byte second) {
		return (short)((second << 8) | (first & 0xff));
	}
	
	public static short readShort(byte[] data) {
		return readShort(data[0], data[1]);
	}
	
	public static ushort readUShort(byte first, byte second) {
		unchecked {
			return (ushort)((second << 8) | first);
		}
	}
	
	public static ushort readUShort(byte[] data) {
		return readUShort(data[0], data[1]);
	}
	
	public static int readInt(byte first, byte second, byte third, byte fourth) {
		return ((fourth << 24) | (third << 16) | (second << 8) | first);
	}
	
	public static int readInt(byte[] data) {
		return readInt(data[0], data[1], data[2], data[3]);
	}
	
	public static uint readUInt(byte first, byte second, byte third, byte fourth) {
		unchecked {
			return (uint)((fourth << 24) | (third << 16) | (second << 8) | first);
		}
	}
	
	public static uint readUInt(byte[] data) {
		return readUInt(data[0], data[1], data[2], data[3]);
	}
	
	public static long readLong(byte first, byte second, byte third, byte fourth, byte fifth, byte sixth, byte seventh, byte eighth) {
		return ((eighth << 56) | (seventh << 48) | (sixth << 40) | (fifth << 32) | (fourth << 24) | (third << 16) | (second << 8) | first);
	}
	
	public static long readLong(byte[] data) {
		return readLong(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7]);
	}
	
	public static ulong readULong(byte first, byte second, byte third, byte fourth, byte fifth, byte sixth, byte seventh, byte eighth) {
		unchecked {
			return (ulong)((eighth << 56) | (seventh << 48) | (sixth << 40) | (fifth << 32) | (fourth << 24) | (third << 16) | (second << 8) | first);
		}
	}
	
	public static ulong readULong(byte[] data) {
		return readULong(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7]);
	}
	
	public static float readFloat(byte first, byte second, byte third, byte fourth) {
		return readFloat(new byte[] { first, second, third, fourth });
	}
	
	public static float readFloat(byte[] data) {
		return System.BitConverter.ToSingle(data, 0);
	}
	
	public static Vector3 readPoint3F(byte first, byte second, byte third, byte fourth, byte fifth, byte sixth, byte seventh, byte eighth, byte ninth, byte tenth, byte eleventh, byte twelfth) {
		return new Vector3(readFloat(first, second, third, fourth), readFloat(fifth, sixth, seventh, eighth), readFloat(ninth, tenth, eleventh, twelfth));
	}
	
	public static Vector3 readPoint3F(byte[] data) {
		return new Vector3(readFloat(data[0], data[1], data[2], data[3]), readFloat(data[4], data[5], data[6], data[7]), readFloat(data[8], data[9], data[10], data[11]));
	}
	
	public static double readDouble(byte first, byte second, byte third, byte fourth, byte fifth, byte sixth, byte seventh, byte eighth) {
		return readDouble(new byte[] {first, second, third, fourth, fifth, sixth, seventh, eighth});
	}
	
	public static double readDouble(byte[] data) {
		return System.BitConverter.ToDouble(data, 0);
	}
	
	public static string readNullTerminatedString(byte[] data) {
		return System.Text.Encoding.ASCII.GetString(data).TrimEnd('\0');
	}
	
	public static string readString(byte[] data) {
		return System.Text.Encoding.ASCII.GetString(data);
	}

	public static short swapEndian(short data) {
		byte[] temp = BitConverter.GetBytes(data);
		return readShort(temp[1], temp[0]);
	}

	public static ushort swapEndian(ushort data) {
		byte[] temp = BitConverter.GetBytes(data);
		return readUShort(temp[1], temp[0]);
	}

	public static int swapEndian(int data) {
		byte[] temp = BitConverter.GetBytes(data);
		return readInt(temp[3], temp[2], temp[1], temp[0]);
	}

	public static uint swapEndian(uint data) {
		byte[] temp = BitConverter.GetBytes(data);
		return readUInt(temp[3], temp[2], temp[1], temp[0]);
	}

	public static long swapEndian(long data) {
		byte[] temp = BitConverter.GetBytes(data);
		return readLong(temp[7], temp[6], temp[5], temp[4], temp[3], temp[2], temp[1], temp[0]);
	}

	public static ulong swapEndian(ulong data) {
		byte[] temp = BitConverter.GetBytes(data);
		return readULong(temp[7], temp[6], temp[5], temp[4], temp[3], temp[2], temp[1], temp[0]);
	}

	public static float swapEndian(float data) {
		byte[] temp = BitConverter.GetBytes(data);
		return readFloat(temp[3], temp[2], temp[1], temp[0]);
	}

	public static double swapEndian(double data) {
		byte[] temp = BitConverter.GetBytes(data);
		return readDouble(temp[7], temp[6], temp[5], temp[4], temp[3], temp[2], temp[1], temp[0]);
	}
	// ACCESSORS/MUTATORS
}