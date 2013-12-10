using System;
using System.Collections.Generic;
// SourceStaticProp class
// Handles the data needed for one static prop.
// This is the lump object with the most wild changes between different versions
// and different game implementations. More research needed

public class SourceStaticProp:LumpObject {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	private Vector3D origin;
	private Vector3D angles;
	private short dictionaryEntry;
	private byte solidity;
	private byte flags;
	private int skin;
	private float minFadeDist;
	private float maxFadeDist;
	private float forcedFadeScale = 1;
	internal string targetname = null;
	
	// CONSTRUCTORS
	public SourceStaticProp(LumpObject data, mapType type, int version):base(data.Data) {
		new SourceStaticProp(data.Data, type, version);
	}
	
	public SourceStaticProp(byte[] data, mapType type, int version):base(data) {
		switch (type) {
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
			case mapType.TYPE_DMOMAM: 
				switch (version) {
					case 5:
						if (data.Length == 188) {
							// This is only for The Ship or Bloody Good Time.
							byte[] targetnameBytes = new byte[128];
							for (int i = 0; i < 128; i++) {
								targetnameBytes[i] = data[60 + i];
							}
							targetname = DataReader.readNullTerminatedString(targetnameBytes);
							if (targetname.Length == 0) {
								targetname = null;
							}
						}
						goto case 6;
					case 6: 
					case 7: 
					case 8: 
					case 9: 
					case 10: 
						forcedFadeScale = DataReader.readFloat(data[56], data[57], data[58], data[59]);
						goto case 4;
					case 4: 
						origin = DataReader.readPoint3F(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11]);
						angles = DataReader.readPoint3F(data[12], data[13], data[14], data[15], data[16], data[17], data[18], data[19], data[20], data[21], data[22], data[23]);
						dictionaryEntry = DataReader.readShort(data[24], data[25]);
						solidity = data[30];
						flags = data[31];
						skin = DataReader.readInt(data[32], data[33], data[34], data[35]);
						minFadeDist = DataReader.readFloat(data[36], data[37], data[38], data[39]);
						maxFadeDist = DataReader.readFloat(data[40], data[41], data[42], data[43]);
						break;
				}
				break;
		}
	}
	
	// METHODS
	public static SourceStaticProps createLump(byte[] data, mapType type, int version) {
		int structLength = 0;
		string[] dictionary = new string[0];
		SourceStaticProps lump;
		if (data.Length > 0) {
			/*switch(type) { // It's possible to determine structlength using arithmetic rather than version numbering
			case mapType.TYPE_SOURCE17:
			case mapType.TYPE_SOURCE18:
			case mapType.TYPE_SOURCE19:
			case mapType.TYPE_SOURCE20:
			case mapType.TYPE_SOURCE21:
			case mapType.TYPE_SOURCE22:
			case mapType.TYPE_SOURCE23:
				switch(version) {
					case 4:
						structLength=56;
						break;
					case 5:
						structLength=60;
						break;
					case 6:
						structLength=64;
						break;
					case 7:
						structLength=68;
						break;
					case 8:
						structLength=72;
						break;
					case 9:
						structLength=73; // ??? The last entry is a boolean, is it stored as a byte?
						break;
					default:
						structLength=0;
						break;
					default:
						structLength=0;
				}*/
			int offset = 0;
			dictionary = new string[DataReader.readInt(data[offset++], data[offset++], data[offset++], data[offset++])];
			for (int i = 0; i < dictionary.Length; i++) {
				byte[] temp = new byte[128];
				for (int j = 0; j < 128; j++) {
					temp[j] = data[offset++];
				}
				dictionary[i] = DataReader.readNullTerminatedString(temp);
			}
			int numLeafDefinitions = DataReader.readInt(data[offset++], data[offset++], data[offset++], data[offset++]);
			for (int i = 0; i < numLeafDefinitions; i++) {
				offset += 2; // Each leaf index is an unsigned short, which i just want to skip
			}
			int numProps = DataReader.readInt(data[offset++], data[offset++], data[offset++], data[offset++]);
			lump = new SourceStaticProps(new List<SourceStaticProp>(numProps), dictionary, data.Length);
			if (numProps > 0) {
				structLength = (data.Length - offset) / numProps;
				byte[] bytes = new byte[structLength];
				for (int i = 0; i < numProps; i++) {
					for (int j = 0; j < structLength; j++) {
						bytes[j] = data[offset + j];
					}
					lump.Add(new SourceStaticProp(bytes, type, version));
					offset += structLength;
				}
			}
		} else {
			lump = new SourceStaticProps(new List<SourceStaticProp>(), dictionary, data.Length);
		}
		return lump;
	}
	
	// ACCESSORS/MUTATORS
	virtual public Vector3D Origin {
		get {
			return origin;
		}
	}

	virtual public Vector3D Angles {
		get {
			return angles;
		}
	}

	virtual public int DictionaryEntry {
		get {
			return dictionaryEntry;
		}
	}

	virtual public byte Solidity {
		get {
			return solidity;
		}
	}

	virtual public byte Flags {
		get {
			return flags;
		}
	}

	virtual public int Skin {
		get {
			return skin;
		}
	}

	virtual public float MinFadeDist {
		get {
			return minFadeDist;
		}
	}

	virtual public float MaxFadeDist {
		get {
			return maxFadeDist;
		}
	}

	virtual public float ForcedFadeScale {
		get {
			return forcedFadeScale;
		}
	}

	virtual public string Targetname {
		get {
			return targetname;
		}
	}
}