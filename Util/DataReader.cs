using System;
using UnityEngine;
// DataReader class

// Static class
// Contains methods for reading certain data types from byte
// arrays. Useful for:
// Reading data from a bytesteam
// Avoiding confusion between big and little endian values (all are assumed little endian)
// Cleaning up code

public static class DataReader {

	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS

	// CONSTRUCTORS

	// METHODS

	public static short readShort(params byte[] data) {
		return (short)((data[1] << 8) | (data[0] & 0xff));
	}

	public static ushort readUShort(params byte[] data) {
		unchecked {
			return (ushort)((data[1] << 8) | data[0]);
		}
	}

	public static int readInt(params byte[] data) {
		return ((data[3] << 24) | (data[2] << 16) | (data[1] << 8) | data[0]);
	}

	public static uint readUInt(params byte[] data) {
		unchecked {
			return (uint)((data[3] << 24) | (data[2] << 16) | (data[1] << 8) | data[0]);
		}
	}

	public static long readLong(params byte[] data) {
		return ((data[7] << 56) | (data[6] << 48) | (data[5] << 40) | (data[4] << 32) | (data[3] << 24) | (data[2] << 16) | (data[1] << 8) | data[0]);
	}

	public static ulong readULong(params byte[] data) {
		unchecked {
			return (ulong)((data[7] << 56) | (data[6] << 48) | (data[5] << 40) | (data[4] << 32) | (data[3] << 24) | (data[2] << 16) | (data[1] << 8) | data[0]);
		}
	}

	public static float readFloat(params byte[] data) {
		return BitConverter.ToSingle(data, 0);
	}

	public static Vector3 readPoint3F(params byte[] data) {
		return new Vector3(readFloat(data[0], data[1], data[2], data[3]), readFloat(data[4], data[5], data[6], data[7]), readFloat(data[8], data[9], data[10], data[11]));
	}

	public static double readDouble(params byte[] data) {
		return BitConverter.ToDouble(data, 0);
	}

	public static string readNullTerminatedString(params byte[] data) {
		return System.Text.Encoding.ASCII.GetString(data).TrimEnd('\0');
	}

	public static string readString(params byte[] data) {
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