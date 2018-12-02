using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

using LibBSP;

namespace Decompiler {

	/// <summary>
	/// Delegate type for handling debug print messages.
	/// </summary>
	/// <param name="sender">Sender of this event.</param>
	/// <param name="e"><see cref="MessageEventArgs"/> containing the debug message and an error flag.</param>
	public delegate void MessageEventHandler(object sender, MessageEventArgs e);
	/// <summary>
	/// Delegate type for notifying others when a job has finished.
	/// </summary>
	/// <param name="sender">Sender of this event.</param>
	/// <param name="e">An <c>EventArgs</c> object. May be <c>EventArgs.Empty</c>.</param>
	public delegate void JobFinishedEventHandler(object sender, EventArgs e);

	/// <summary>
	/// Class orchestrating the steps for decompiling and providing information about the progress of the operation.
	/// </summary>
	public class Job : INotifyPropertyChanged {

		/// <summary>
		/// A class containing settings modifying how the decompile will be performed.
		/// </summary>
		public class Settings {
			public bool replace512WithNull = false;
			public bool noFaceFlags = false;
			public bool brushesToWorld = false;
			public bool noTexCorrection = false;
			public bool noEntCorrection = false;
			public string outputFolder = "";

			public MapType openAs = MapType.Undefined;
			public bool toAuto = true;
			public bool toM510 = false;
			public bool toVMF = false;
			public bool toGTK = false;
			public bool toDoomEdit = false;
			public bool toMoH = false;
			public bool toCoD = false;
		}

		/// <summary>
		/// Event that is fired when a job triggers a debug message.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><see cref="MessageEventArgs"/> containing the debug message and an error flag.</param>
		public static event MessageEventHandler MessageEvent;
		/// <summary>
		/// Event that is fired when a job successfully finishes.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e">An <c>EventArgs</c> object. May be <c>EventArgs.Empty</c>.</param>
		public static event JobFinishedEventHandler JobFinishedEvent;

		private int _id;
		private string _path;
		public Settings settings;
		public event PropertyChangedEventHandler PropertyChanged;

		private string _name = "";
		private double _progress = 0.0;

		/// <summary>
		/// The number of this job in the list.
		/// </summary>
		public int num {
			get {
				return _id + 1;
			}
		}

		/// <summary>
		/// The name of the map being decompiled.
		/// </summary>
		public string name {
			get {
				return _name;
			}
			set {
				_name = value;
				PropertyChanged(this, new PropertyChangedEventArgs("name"));
			}
		}

		/// <summary>
		/// The progress of this decompile operation.
		/// </summary>
		public double progress {
			get {
				return _progress;
			}
			set {
				_progress = value;
				PropertyChanged(this, new PropertyChangedEventArgs("progress"));
				PropertyChanged(this, new PropertyChangedEventArgs("percentage"));
			}
		}

		/// <summary>
		/// The progress of this decompile operation, expressed as a percentage.
		/// </summary>
		public string percentage {
			get {
				return (Math.Round(_progress * 100)).ToString() + "%";
			}
		}

		/// <summary>
		/// Creates a new instance of a <see cref="Job"/> object.
		/// </summary>
		/// <param name="id">Index of this job.</param>
		/// <param name="path">The path to the BSP file this job will operate on.</param>
		/// <param name="settings">A <see cref="Job.Settings"/> object specifying options for changing decompile output.</param>
		public Job(int id, string path, Settings settings) {
			this._id = id;
			this._path = path;
			this.settings = settings;

			_name = _path.Substring(_path.LastIndexOf('\\') + 1);
		}

		/// <summary>
		/// Shortcut to the <see cref="MessageEvent"/> delegate using this <see cref="Job"/> object as the sender.
		/// </summary>
		/// <param name="st">The <c>string</c> to be printed.</param>
		public void Print(string st) {
			if (MessageEvent != null) {
				MessageEvent(this, new MessageEventArgs(st));
			}
		}
		
		/// <summary>
		/// Runs the decompile process using the given instance data.
		/// </summary>
		public void Run() {
			DateTime begin = DateTime.Now;
			BSP bsp = null;
#if !DEBUG
			try {
#endif
				Entities output = null;
				string mapDirectory = "";
				string mapName = "";
				MapType version = MapType.Undefined;
				bsp = new BSP(_path);
				bsp.version = settings.openAs;
				BSPDecompiler decompiler = new BSPDecompiler(bsp, this);
				output = decompiler.Decompile();
				mapDirectory = bsp.Folder;
				mapName = bsp.MapNameNoExtension;
				version = bsp.version;
				MAPWriter writer = new MAPWriter(output, mapDirectory, mapName, version, this);
				writer.WriteAll();
				DateTime end = DateTime.Now;
				Print("Time taken: " + (end - begin).ToString() + (char)0x0D + (char)0x0A);
				progress = 1;
				if (JobFinishedEvent != null) {
					JobFinishedEvent(this, EventArgs.Empty);
				}
#if !DEBUG
			} catch (Exception e) {
				if (MessageEvent != null) {
					MessageEvent(this, new MessageEventArgs("Exception caught in Job " + num + ": " + e.ToString(), true));
				}
			}
#endif
		}
	}

	/// <summary>
	/// <c>EventArgs</c> class to be sent along with a <c>MessageEvent</c>.
	/// </summary>
	public class MessageEventArgs : EventArgs {
		public string message { get; private set; }
		public bool error { get; private set; }

		/// <summary>
		/// Creates a new instance of a <see cref="MessageEventArgs"/> class.
		/// </summary>
		/// <param name="message">A <c>string</c> to print.</param>
		/// <param name="error">Optional <c>bool</c> value determining whether this message signals an error.</param>
		public MessageEventArgs(string message, bool error = false) {
			this.message = message;
			this.error = error;
		}
	}
}
