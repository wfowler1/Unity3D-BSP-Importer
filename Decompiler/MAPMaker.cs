// MAPMaker class
// Takes Entities classes and uses map writer classes to output editor mapfiles.
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class MAPMaker {
	
	// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
	
	// CONSTRUCTORS
	
	// METHODS
	public static void outputMaps(Entities data, string mapname, string mapfolder, mapType version)
	{
		if (Settings.toAuto)
		{
			// If "auto" is selected, output to one format appropriate for the source game
			switch (version)
			{
				
				// Gearcraft
				case mapType.TYPE_NIGHTFIRE: 
					MAP510Writer GCMAPMaker;
					if (Settings.outputFolder.Equals("default"))
					{
						GCMAPMaker = new MAP510Writer(data, mapfolder + mapname, version);
					}
					else
					{
						GCMAPMaker = new MAP510Writer(data, Settings.outputFolder + "\\" + mapname, version);
					}
					GCMAPMaker.write();
					break;
					
				// MOHRadiant
				case mapType.TYPE_MOHAA: 
					MOHRadiantMAPWriter MOHMAPMaker;
					if (Settings.outputFolder.Equals("default"))
					{
						MOHMAPMaker = new MOHRadiantMAPWriter(data, mapfolder + mapname, version);
					}
					else
					{
						MOHMAPMaker = new MOHRadiantMAPWriter(data, Settings.outputFolder + "\\" + mapname, version);
					}
					MOHMAPMaker.write();
					break;
				
				// GTK Radiant
				case mapType.TYPE_QUAKE: 
				case mapType.TYPE_STEF2: 
				case mapType.TYPE_STEF2DEMO: 
				case mapType.TYPE_SIN: 
				case mapType.TYPE_SOF: 
				case mapType.TYPE_RAVEN: 
				case mapType.TYPE_QUAKE2: 
				case mapType.TYPE_DAIKATANA: 
				case mapType.TYPE_QUAKE3: 
				case mapType.TYPE_COD: 
				case mapType.TYPE_FAKK: 
					GTKRadiantMapWriter RadMAPMaker;
					if (Settings.outputFolder.Equals("default"))
					{
						RadMAPMaker = new GTKRadiantMapWriter(data, mapfolder + mapname, version);
					}
					else
					{
						RadMAPMaker = new GTKRadiantMapWriter(data, Settings.outputFolder + "\\" + mapname, version);
					}
					RadMAPMaker.write();
					break;
				
				// Hammer VMF
				case mapType.TYPE_SOURCE17: 
				case mapType.TYPE_SOURCE18: 
				case mapType.TYPE_SOURCE19: 
				case mapType.TYPE_SOURCE20: 
				case mapType.TYPE_SOURCE21: 
				case mapType.TYPE_SOURCE22: 
				case mapType.TYPE_SOURCE23: 
				case mapType.TYPE_SOURCE27: 
				case mapType.TYPE_VINDICTUS: 
				case mapType.TYPE_DMOMAM: 
				case mapType.TYPE_TACTICALINTERVENTION: 
					VMFWriter VMFMaker;
					if (Settings.outputFolder.Equals("default"))
					{
						VMFMaker = new VMFWriter(data, mapfolder + mapname, version);
					}
					else
					{
						VMFMaker = new VMFWriter(data, Settings.outputFolder + "\\" + mapname, version);
					}
					VMFMaker.write();
					break;
				
				// DoomEdit
				case mapType.TYPE_DOOM:  // DoomEdit seems somehow appropriate.
					DoomEditMapWriter DOOMMAPMaker;
					if (Settings.outputFolder.Equals("default"))
					{
						DOOMMAPMaker = new DoomEditMapWriter(data, mapfolder + mapname, version);
					}
					else
					{
						DOOMMAPMaker = new DoomEditMapWriter(data, Settings.outputFolder + "\\" + mapname, version);
					}
					DOOMMAPMaker.write();
					break;
				
				default: 
					DecompilerThread.OnMessage(new Object(), "WARNING: No default format specified for BSP version " + version + ", defaulting to GearCraft.");
					MAP510Writer GCMAPMaker2;
					if (Settings.outputFolder.Equals("default"))
					{
						GCMAPMaker2 = new MAP510Writer(data, mapfolder + mapname, version);
					}
					else
					{
						GCMAPMaker2 = new MAP510Writer(data, Settings.outputFolder + "\\" + mapname, version);
					}
					GCMAPMaker2.write();
					break;
				
			}
		}
		else
		{
			Entities from;
			if (Settings.toDoomEdit)
			{
				DoomEditMapWriter mapMaker;
				if (Settings.toVMF || Settings.toMoH || Settings.toM510 || Settings.toGTK)
				{
					from = DeepCopy<Entities>(data);
				}
				else
				{
					from = data;
				}
				if (Settings.outputFolder.Equals("default"))
				{
					mapMaker = new DoomEditMapWriter(from, mapfolder + mapname + "_doomEdit", version);
				}
				else
				{
					mapMaker = new DoomEditMapWriter(from, Settings.outputFolder + mapname + "_doomEdit", version);
				}
				mapMaker.write();
			}
			if (Settings.toVMF)
			{
				VMFWriter VMFMaker;
				if (Settings.toMoH || Settings.toM510 || Settings.toGTK)
				{
					from = DeepCopy<Entities>(data);
				}
				else
				{
					from = data;
				}
				if (Settings.outputFolder.Equals("default"))
				{
					VMFMaker = new VMFWriter(from, mapfolder + mapname, version);
				}
				else
				{
					VMFMaker = new VMFWriter(from, Settings.outputFolder + "\\" + mapname, version);
				}
				VMFMaker.write();
			}
			if (Settings.toMoH)
			{
				MOHRadiantMAPWriter mapMaker;
				if (Settings.toM510 || Settings.toGTK)
				{
					from = DeepCopy<Entities>(data);
				}
				else
				{
					from = data;
				}
				if (Settings.outputFolder.Equals("default"))
				{
					mapMaker = new MOHRadiantMAPWriter(from, mapfolder + mapname + "_MOH", version);
				}
				else
				{
					mapMaker = new MOHRadiantMAPWriter(from, Settings.outputFolder + mapname + "_MOH", version);
				}
				mapMaker.write();
			}
			if (Settings.toM510)
			{
				MAP510Writer mapMaker;
				if (Settings.toGTK)
				{
					from = DeepCopy<Entities>(data);
				}
				else
				{
					from = data;
				}
				if (Settings.outputFolder.Equals("default"))
				{
					mapMaker = new MAP510Writer(from, mapfolder + mapname + "_gc", version);
				}
				else
				{
					mapMaker = new MAP510Writer(from, Settings.outputFolder + mapname + "_gc", version);
				}
				mapMaker.write();
			}
			if (Settings.toGTK)
			{
				GTKRadiantMapWriter mapMaker;
				from = data;
				if (Settings.outputFolder.Equals("default"))
				{
					mapMaker = new GTKRadiantMapWriter(data, mapfolder + mapname + "_radiant", version);
				}
				else
				{
					mapMaker = new GTKRadiantMapWriter(data, Settings.outputFolder + mapname + "_radiant", version);
				}
				mapMaker.write();
			}
		}
	}
	
	// If only one thread is allowed to use new Object() method at once, only one map will be saved at once, meaning less
	// jumping hard drive seek time used.
	public static void write(byte[] data, string destinationString, bool toVMF) {
		try
		{
			if (!destinationString.Substring(destinationString.Length - 4).ToUpper().Equals(".map".ToUpper()) && !destinationString.Substring(destinationString.Length - 4).ToUpper().Equals(".vmf".ToUpper()))
			{
				if (toVMF)
				{
					destinationString = destinationString + ".vmf";
				}
				else
				{
					destinationString = destinationString + ".map";
				}
			}
		}
		catch (System.ArgumentOutOfRangeException)
		{
			if (toVMF)
			{
				destinationString = destinationString + ".vmf";
			}
			else
			{
				destinationString = destinationString + ".map";
			}
		}
		DecompilerThread.OnMessage(new Object(), "Saving " + destinationString+"...");
		try {
			FileStream stream = new FileStream(destinationString, FileMode.Create, FileAccess.Write);
			BinaryWriter bw = new BinaryWriter(stream);
			stream.Seek(0, SeekOrigin.Begin);
			bw.Write(data);
			bw.Close();
		} catch(System.IO.IOException e) {
			DecompilerThread.OnMessage(new Object(), "ERROR: Could not save "+destinationString+", ensure the file is not open in another program.");
			throw e;
		}
	}

	/// <summary>
	/// Performs a basic deep copy.
	/// 
	/// By: The Janitor
	/// From: http://weblogs.asp.net/gunnarpeipman/archive/2007/10/07/net-and-deep-copy.aspx
	/// </summary>
	public static T DeepCopy<T>(T obj) {
		MemoryStream ms = new MemoryStream();
		BinaryFormatter bf = new BinaryFormatter();
		bf.Serialize(ms, obj);
		ms.Seek(0, SeekOrigin.Begin);
		T retval = (T)bf.Deserialize(ms);
		ms.Close();
		return retval;
	}
	
	// ACCESSORS/MUTATORS
	
	// INTERNAL CLASSES
}