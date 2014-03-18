using System;

public static class Settings {
	public enum MidpointRounding {
		Up=0,
		Down=1,
		AwayFromZero=2,
		TowardZero=3,
		ToEven=4,
		ToOdd=5,
	}

	public static int numThreads = Environment.ProcessorCount;
	public static double planePointCoef = 32;
	public static bool skipPlaneFlip = true;
	public static bool noDetail = false;
	public static bool noWater = false;
	public static bool replaceWithNull = false;
	public static bool planarDecomp = false;
	public static bool noFaceFlags = false;
	public static bool calcVerts = false;
	public static bool brushesToWorld = false;
	public static bool dontCull = true;
	public static bool noTexCorrection = false;
	public static bool noEntCorrection = false;
	public static bool roundNums = true;
	public static bool noOriginBrushes = false;
	public static bool dumpCrashLump = false;
	public static bool extractZip = false;
	public static double originBrushSize=16;
	public static int verbosity=0;
	public static string outputFolder="default";
	public static double precision=0.05;
	public static int mmStack=8;
	public static mapType openAs = mapType.TYPE_UNDEFINED;
	public static bool toAuto = true;
	public static bool toM510 = false;
	public static bool toVMF = false;
	public static bool toGTK = false;
	public static bool toDoomEdit = false;
	public static bool toMoH = false;
	public static int MMStackSize = 8;
	public static MidpointRounding roundingMode = MidpointRounding.Up;
}