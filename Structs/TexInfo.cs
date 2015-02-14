using System;
using UnityEngine;
// TexInfo class
// This class contains the texture scaling information for certain formats.
// Some BSP formats lack this lump (or it is contained in a different one)
// so their cases will be left out.

public class TexInfo : LumpObject {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	public const int S = 0;
	public const int T = 1;
	public static readonly Vector3[] baseAxes = new Vector3[] { new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(0, - 1, 0), new Vector3(0, 0, - 1), new Vector3(1, 0, 0), new Vector3(0, - 1, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, - 1), new Vector3(- 1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, - 1), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, - 1), new Vector3(0, - 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, - 1) };
	
	private Vector3[] axes;
	private float[] shifts = new float[] { System.Single.NaN, System.Single.NaN };
	private int flags = -1;
	private int texture = -1;
	
	// CONSTRUCTORS
	public TexInfo(LumpObject data, mapType type) : base(data.Data) {
		new TexInfo(data.Data, type);
	}
	
	public TexInfo(byte[] data, mapType type) : base(data) {
		axes = new Vector3[2];
		axes[S] = DataReader.readPoint3F(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11]);
		shifts[S] = DataReader.readFloat(data[12], data[13], data[14], data[15]);
		axes[T] = DataReader.readPoint3F(data[16], data[17], data[18], data[19], data[20], data[21], data[22], data[23], data[24], data[25], data[26], data[27]);
		shifts[T] = DataReader.readFloat(data[28], data[29], data[30], data[31]);
		switch (type) {
			// Excluded engines: Quake 2-based, Quake 3-based
			case mapType.TYPE_SOURCE17:
			case mapType.TYPE_SOURCE18:
			case mapType.TYPE_SOURCE19:
			case mapType.TYPE_SOURCE20:
			case mapType.TYPE_SOURCE21:
			case mapType.TYPE_SOURCE22:
			case mapType.TYPE_SOURCE23:
			case mapType.TYPE_SOURCE27:
			case mapType.TYPE_TACTICALINTERVENTION:
			case mapType.TYPE_VINDICTUS:
				texture = DataReader.readInt(data[68], data[69], data[70], data[71]);
				flags = DataReader.readInt(data[64], data[65], data[66], data[67]);
				break;
			case mapType.TYPE_DMOMAM:
				texture = DataReader.readInt(data[92], data[93], data[94], data[95]);
				flags = DataReader.readInt(data[88], data[89], data[90], data[91]);
				break;
			case mapType.TYPE_QUAKE:
				texture = DataReader.readInt(data[32], data[33], data[34], data[35]);
				flags = DataReader.readInt(data[36], data[37], data[38], data[39]);
				break;
			case mapType.TYPE_NIGHTFIRE:
				break;
		}
	}
	
	// Not for use in a group
	public TexInfo(Vector3 s, float SShift, Vector3 t, float TShift, int flags, int texture) : base(new byte[0]) {
		axes = new Vector3[2];
		axes[S] = s;
		axes[T] = t;
		shifts[S] = SShift;
		shifts[T] = TShift;
		this.flags = flags;
		this.texture = texture;
	}
	
	// METHODS
	
	// textureAxisFromPlane, adapted from code in the Quake III Arena source code. Stolen without
	// permission because it falls under the terms of the GPL v2 license, because I'm not making
	// any money, just awesome tools.
	public static Vector3[] textureAxisFromPlane(Plane p) {
		int bestaxis = 0;
		double dot; // Current dot product
		double best = 0; // "Best" dot product so far
		for (int i = 0; i < 6; i++) {
			// For all possible axes, positive and negative
			dot = Vector3.Dot(p.normal, new Vector3(baseAxes[i * 3][0], baseAxes[i * 3][1], baseAxes[i * 3][2]));
			if (dot > best) {
				best = dot;
				bestaxis = i;
			}
		}
		Vector3[] out_Renamed = new Vector3[2];
		out_Renamed[0] = new Vector3(baseAxes[bestaxis * 3 + 1][0], baseAxes[bestaxis * 3 + 1][1], baseAxes[bestaxis * 3 + 1][2]);
		out_Renamed[1] = new Vector3(baseAxes[bestaxis * 3 + 2][0], baseAxes[bestaxis * 3 + 2][1], baseAxes[bestaxis * 3 + 2][2]);
		return out_Renamed;
	}

	public static Lump<TexInfo> createLump(byte[] data, mapType type) {
		int structLength = 0;
		switch (type) {
			case mapType.TYPE_NIGHTFIRE:
				structLength = 32;
				break;
			case mapType.TYPE_QUAKE:
				structLength = 40;
				break;
			case mapType.TYPE_SOURCE17:
			case mapType.TYPE_SOURCE18:
			case mapType.TYPE_SOURCE19:
			case mapType.TYPE_SOURCE20:
			case mapType.TYPE_SOURCE21:
			case mapType.TYPE_SOURCE22:
			case mapType.TYPE_SOURCE23:
			case mapType.TYPE_SOURCE27:
			case mapType.TYPE_TACTICALINTERVENTION:
			case mapType.TYPE_VINDICTUS:
				structLength = 72;
				break;
			case mapType.TYPE_DMOMAM:
				structLength = 96;
				break;
		}
		int offset = 0;
		Lump<TexInfo> lump = new Lump<TexInfo>(data.Length, structLength, data.Length / structLength);
		byte[] bytes = new byte[structLength];
		for (int i = 0; i < data.Length / structLength; i++) {
			for (int j = 0; j < structLength; j++) {
				bytes[j] = data[offset + j];
			}
			lump.Add(new TexInfo(bytes, type));
			offset += structLength;
		}
		return lump;
	}

	// ACCESSORS/MUTATORS
	virtual public Vector3 SAxis {
		get {
			return axes[S];
		}
	}

	virtual public Vector3 TAxis {
		get {
			return axes[T];
		}
	}

	virtual public float SShift {
		get {
			return shifts[S];
		}
	}

	virtual public float TShift {
		get {
			return shifts[T];
		}
	}

	virtual public int Flags {
		get {
			return flags;
		}
	}

	virtual public int Texture {
		get {
			return texture;
		}
	}
}