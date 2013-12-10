// DecompilerThread class
// Multithreads the decompiler, allowing queueing of decompilation jobs while
// preventing the program GUI from freezing during operation
using System;
using System.IO;
using System.Threading;
	
public delegate void MessageReceivedHandler(Object sender, MessageEventArgs e);
public delegate void ThreadFinishedHandler(Object sender, FinishEventArgs e);
public delegate void ErrorHandler(Object sender, MessageEventArgs e);
public delegate void ProgressEventHandler(Object sender, ProgressEventArgs e);

public class DecompilerThread {
	public static event MessageReceivedHandler print;
	public static event ThreadFinishedHandler done;
	public event ProgressEventHandler reportProgress;
	public event ErrorHandler error;
	private FileInfo BSPFile;
	private DoomMap doomMap;
	private BSP BSPObject;
	private int jobnum;
	private mapType openAs = mapType.TYPE_UNDEFINED;
	
	public DecompilerThread(FileInfo BSPFile, int jobnum, mapType openAs) {
		// Set up global variables
		this.BSPFile = BSPFile;
		this.jobnum = jobnum;
		this.openAs = openAs;
	}
	
	public DecompilerThread(DoomMap doomMap, int jobNum, mapType openAs) {
		this.doomMap = doomMap;
		this.jobnum = jobNum;
	}

	public void OnProgress(Object sender, double percentage) {
		if(reportProgress != null) {
			reportProgress(sender, new ProgressEventArgs(percentage));
		}
	}

	public void OnError(Object sender, string message) {
		if(error != null) {
			OnMessage(sender, message);
			error(sender, new MessageEventArgs(message));
		}
	}
	
	public static void OnMessage(Object sender, string message) {
		if(print != null) {
			print(sender, new MessageEventArgs(message));
		} else {
			Console.WriteLine(message);
		}
	}

	public static void OnFinish(Object sender, int joibnum) {
		if(done != null) {
			done(sender, new FinishEventArgs(joibnum));
		}
	}

	public virtual void Run() {
		DateTime begin = DateTime.Now;
		try {
			Entities output = null;
			if (doomMap != null) {
				// If this is a Doom map extracted from a WAD
				//Window.setProgress(jobnum, 0, doomMap.getSubSectors().size(), "Decompiling...");
				WADDecompiler decompiler = new WADDecompiler(doomMap, jobnum, this);
				output = decompiler.decompile();
			} else {
				DecompilerThread.OnMessage(this, "Opening file " + BSPFile.FullName);
				//Window.setProgress(jobnum, 0, 1, "Reading...");
				BSPReader reader = new BSPReader(BSPFile, openAs);
				reader.readBSP();
				//if (!reader.WAD) {
					BSPObject = reader.BSPData;
					/*try {
						Window.setProgress(jobnum, 0, reader.BSPData.getBrushes().size() + reader.BSPData.getEntities().size(), "Decompiling...");
					} catch (System.NullReferenceException e) {
						try {
							Window.setProgress(jobnum, 0, reader.BSPData.getLeaves().size() + reader.BSPData.getEntities().size(), "Decompiling...");
						} catch (System.NullReferenceException f) {
							Window.setProgress(jobnum, 0, 1, "Decompiling..."); // What's going on here? Put in a failsafe progress bar for now
						}
					}*/
					
					switch (reader.Version){
						case mapType.TYPE_QUAKE: 
							//DecompilerThread.OnMessage(this, "ERROR: Algorithm for decompiling Quake BSPs not written yet.",Window.VERBOSITY_ALWAYS);
							//throw new java.lang.Exception(); // Throw an exception to the exception handler to indicate it didn't work
							QuakeDecompiler decompiler29 = new QuakeDecompiler(reader.BSPData, jobnum, this);
							output = decompiler29.decompile();
							break;
						case mapType.TYPE_NIGHTFIRE: 
							BSP42Decompiler decompiler42 = new BSP42Decompiler(reader.BSPData, jobnum, this);
							output = decompiler42.decompile();
							break;
						case mapType.TYPE_QUAKE2: 
						case mapType.TYPE_SIN: 
						case mapType.TYPE_SOF: 
						case mapType.TYPE_DAIKATANA: 
							BSP38Decompiler decompiler38 = new BSP38Decompiler(reader.BSPData, jobnum, this);
							output = decompiler38.decompile();
							break;
						case mapType.TYPE_SOURCE17: 
						case mapType.TYPE_SOURCE18: 
						case mapType.TYPE_SOURCE19: 
						case mapType.TYPE_SOURCE20: 
						case mapType.TYPE_SOURCE21: 
						case mapType.TYPE_SOURCE22: 
						case mapType.TYPE_SOURCE23: 
						case mapType.TYPE_SOURCE27: 
						case mapType.TYPE_DMOMAM: 
						case mapType.TYPE_VINDICTUS: 
						case mapType.TYPE_TACTICALINTERVENTION: 
							SourceBSPDecompiler sourceDecompiler = new SourceBSPDecompiler(reader.BSPData, jobnum, this);
							output = sourceDecompiler.decompile();
							break;
						case mapType.TYPE_QUAKE3: 
						case mapType.TYPE_RAVEN: 
						case mapType.TYPE_COD: 
						case mapType.TYPE_COD2: 
						case mapType.TYPE_COD4: 
						case mapType.TYPE_STEF2: 
						case mapType.TYPE_STEF2DEMO: 
						case mapType.TYPE_MOHAA: 
						case mapType.TYPE_FAKK: 
							BSP46Decompiler decompiler46 = new BSP46Decompiler(reader.BSPData, jobnum, this);
							output = decompiler46.decompile();
							break;
						case mapType.TYPE_DOOM:
						case mapType.TYPE_HEXEN:
							foreach (DoomMap map in reader.DoomMaps) {
								WADDecompiler wadDecompiler = new WADDecompiler(map, jobnum, this);
								Entities holyshit = wadDecompiler.decompile();
								MAPMaker.outputMaps(holyshit, map.MapName, map.Folder + map.WadName + "\\", map.Version);
							}
							break;
						default: 
							DecompilerThread.OnMessage(this, "ERROR: Unknown BSP version: " + reader.Version);
							throw new System.Exception(); // Throw an exception to the exception handler to indicate it didn't work
					}
				//}
			}
			if (output != null) {
				//Window.setProgress(jobnum, 1, 1, "Saving...");
				if (doomMap == null) {
					MAPMaker.outputMaps(output, BSPObject.MapNameNoExtension, BSPObject.Folder, BSPObject.Version);
				} else {
					MAPMaker.outputMaps(output, doomMap.MapName, doomMap.Folder + doomMap.WadName + "\\", doomMap.Version);
				}
			}
			//Window.setProgress(jobnum, 1, 1, "Done!");
			//System.Drawing.Color tempAux = System.Drawing.Color.FromArgb(64, 192, 64);
			//Window.setProgressColor(jobnum, ref tempAux);
			DateTime end = DateTime.Now;
			DecompilerThread.OnMessage(this, "Time taken: " + (end - begin).ToString() + (char) 0x0D + (char) 0x0A);
			OnFinish(this, jobnum);
		} catch (System.OutOfMemoryException) {
			string st = "";
			if (openAs != mapType.TYPE_UNDEFINED) {
				st = "VM ran out of memory on job " + (jobnum + 1) + ". Are you using \"Open as...\" with the wrong game?" + (char) 0x0D + (char) 0x0A + "If not, please let me know on the issue tracker!" + (char) 0x0D + (char) 0x0A + "http://code.google.com/p/jbn-bsp-lump-tools/issues/entry";
			} else {
				st = "VM ran out of memory on job " + (jobnum + 1) + "." + (char) 0x0D + (char) 0x0A + "Please let me know on the issue tracker!" + (char) 0x0D + (char) 0x0A + "http://code.google.com/p/jbn-bsp-lump-tools/issues/entry";
			}
			//Window.setProgress(jobnum, 1, 1, "ERROR! See log!");
			//System.Drawing.Color tempAux4 = System.Drawing.Color.FromArgb(255, 128, 128);
			//UPGRADE_NOTE: ref keyword was added to struct-type parameters. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1303'"
			//Window.setProgressColor(jobnum, ref tempAux4);
			OnError(this, st);
		} catch (Exception e) {
			string st;
			if (openAs != mapType.TYPE_UNDEFINED)
			{
				//UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Throwable.toString' may return a different value. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1043'"
				st="" + (char) 0x0D + (char) 0x0A + "Exception caught in job " + (jobnum + 1) + ": " + e + (char) 0x0D + (char) 0x0A + "Are you using \"Open as...\" with the wrong game?" + (char) 0x0D + (char) 0x0A + "If not, please let me know on the issue tracker!" + (char) 0x0D + (char) 0x0A + "http://code.google.com/p/jbn-bsp-lump-tools/issues/entry";
			}
			else
			{
				//UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Throwable.toString' may return a different value. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1043'"
				st="" + (char) 0x0D + (char) 0x0A + "Exception caught in job " + (jobnum + 1) + ": " + e + (char) 0x0D + (char) 0x0A + "Please let me know on the issue tracker!" + (char) 0x0D + (char) 0x0A + "http://code.google.com/p/jbn-bsp-lump-tools/issues/entry";
			}
			/*System.String stackTrace = "Stack Trace: " + (char) 0x0D + (char) 0x0A;
			StackTraceElement[] trace = e.getStackTrace();
			for (int i = 0; i < trace.length; i++)
			{
				stackTrace += (trace[i].toString() + Window.LF);
			}
			//UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Throwable.getMessage' may return a different value. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1043'"
			DecompilerThread.OnMessage(this, e.Message + Window.LF + stackTrace);
			DecompilerThread.OnMessage(this, );
			Window.setProgress(jobnum, 1, 1, "ERROR! See log!");
			System.Drawing.Color tempAux3 = System.Drawing.Color.FromArgb(255, 128, 128);
			//UPGRADE_NOTE: ref keyword was added to struct-type parameters. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1303'"
			Window.setProgressColor(jobnum, ref tempAux3);*/
			OnError(this, st);
		} finally {
			doomMap = null;
			BSPObject = null;
			BSPFile = null;
			Thread.CurrentThread.Abort();
		}
		/*else
		{
			Window.print("Job " + (jobnum + 1) + " aborted by user.");
			Window.print(" When: While initializing job.");
			DecompilerThread.OnMessage(this, );
			Window.setProgress(jobnum, 1, 1, "Aborted!");
			System.Drawing.Color tempAux5 = System.Drawing.Color.FromArgb(255, 128, 128);
			//UPGRADE_NOTE: ref keyword was added to struct-type parameters. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1303'"
			Window.setProgressColor(jobnum, ref tempAux5);
			SupportClass.ThreadClass.Current().Interrupt();
		}*/
		//Window.setAbortButtonEnabled(jobnum, false);
	}
}

public class MessageEventArgs : EventArgs {
	private string message;
	public MessageEventArgs(string message) {
		this.message = message;
	}
	public string Message {
		get {
			return message;
		}
	}
}

public class FinishEventArgs : EventArgs {
	private int jobnum;
	public FinishEventArgs(int jobnum) {
		this.jobnum = jobnum;
	}
	public int Jobnum {
		get {
			return jobnum;
		}
	}
}

public class ProgressEventArgs : EventArgs {
	private double progress;
	public ProgressEventArgs(double progress) {
		this.progress = progress;
	}
	public double Progress {
		get {
			return progress;
		}
	}
}