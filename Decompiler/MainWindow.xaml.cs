using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Shell;

namespace Decompiler {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		
		// INITIAL DATA DECLARATION AND DEFINITION OF CONSTANTS
		ObservableCollection<Job> jobs = new ObservableCollection<Job>();
		Queue<DecompilerThread> jobQueue = new Queue<DecompilerThread>();
		Dictionary<DecompilerThread, Thread> active = new Dictionary<DecompilerThread, Thread>();
		private int finished = 0;

		// CONSTRUCTORS
		public MainWindow() {
			InitializeComponent();
			DecompilerThread.print += new MessageReceivedHandler(print);
			DecompilerThread.done += new ThreadFinishedHandler(threadFinished);
			jobListView.ItemsSource = jobs;
			taskBarItemInfo1.ProgressState = TaskbarItemProgressState.Normal;
			if(Revision.versionString == "Unversioned directory") {
				this.Title = "Decompiler v4 by 005";
			} else {
				this.Title = "Decompiler r"+Revision.versionString+" by 005";
			}
		}

		// METHODS
		private void StartNextIfAble() {
			if(jobQueue.Count > 0) {
				if (active.Count < Settings.numThreads) {
					DecompilerThread next = jobQueue.Dequeue();
					active.Add(next, new Thread(next.Run));
					active[next].Start();
					StartNextIfAble(); // Recursively call this until either the queue is empty or the active jobs is full
				}
			} else {
				System.GC.Collect(); // No jobs currently processing; use as little memory as possible ;)
			}
		}

		private void FileOpen_Click(Object sender, RoutedEventArgs e) {
			OpenFileDialog fileOpener = new OpenFileDialog();
			fileOpener.Filter = "All Supported Files|*.bsp;*.d3dbsp;*.wad|BSP Files|*.bsp;*.d3dbsp|WAD Files|*.wad|All Files|*.*";
			fileOpener.Multiselect = true;

			// Process open file dialog box results 
			if (fileOpener.ShowDialog() == true) {
				string[] filesToOpen = fileOpener.FileNames;
				for(int i=0;i<filesToOpen.Length;i++) {
					DecompilerThread thread = new DecompilerThread(new FileInfo(filesToOpen[i]), finished + active.Count + jobQueue.Count, Settings.openAs);
					Job theJob = new Job(finished + active.Count + jobQueue.Count, filesToOpen[i], thread);
					theJob.PropertyChanged += new PropertyChangedEventHandler(UpdateTaskbar);
					thread.error += new ErrorHandler(Error);
					jobQueue.Enqueue(thread);
					jobs.Add(theJob);
				}
				StartNextIfAble();
			}
		}

		private void UpdateTaskbar(object sender, EventArgs e) {
			double cumulativeProgress = 0.0;
			foreach (Job job in jobs) {
				cumulativeProgress += job.ProgressValue;
			}
			this.Dispatcher.Invoke((Action)(() => {
				taskBarItemInfo1.ProgressValue = (cumulativeProgress/(double)jobs.Count);
			}));
		}

		private void Error(object sender, EventArgs e) {
			this.Dispatcher.Invoke((Action)(() => {
				Console.WriteLine("ERROR CHANGING THINGIEE TO RED");
				taskBarItemInfo1.ProgressState = TaskbarItemProgressState.Error;
			}));
		}

		private MenuItem _OpenAsChecked = null;
		private void OpenAs_Click(object sender, RoutedEventArgs e) {
			if (_OpenAsChecked != null) {
				_OpenAsChecked.IsChecked = false;
			} else {
				miOpenAsAuto.IsChecked = false;
			}
			_OpenAsChecked = e.Source as MenuItem;
			if (_OpenAsChecked != null) {
				_OpenAsChecked.IsChecked = true;
				Settings.openAs = (mapType)Int32.Parse(_OpenAsChecked.Tag.ToString());
			}
		}

		private MenuItem _RoundingModeChecked = null;
		private void RoundingMode_Click(object sender, RoutedEventArgs e) {
			if (_RoundingModeChecked != null) {
				_RoundingModeChecked.IsChecked = false;
			} else {
				miRoundUp.IsChecked = false;
			}
			_RoundingModeChecked = e.Source as MenuItem;
			if (_RoundingModeChecked != null) {
				_RoundingModeChecked.IsChecked = true;
				Settings.roundingMode = (Settings.MidpointRounding)Int32.Parse(_RoundingModeChecked.Tag.ToString());
			}
		}

		private void OutputAuto_Click(object sender, RoutedEventArgs e) {
			if(!miSaveAsAuto.IsChecked && !miSaveAsVMF.IsChecked && !miSaveAsMOH.IsChecked && !miSaveAsGC.IsChecked && !miSaveAsGTK.IsChecked && !miSaveAsDE.IsChecked) {
				miSaveAsAuto.IsChecked = true;
				Settings.toAuto = true;
			}
			miSaveAsAuto.IsChecked = true;
			miSaveAsVMF.IsChecked = false;
			miSaveAsMOH.IsChecked = false;
			miSaveAsGC.IsChecked = false;
			miSaveAsGTK.IsChecked = false;
			miSaveAsDE.IsChecked = false;
			Settings.toAuto = true;
			Settings.toDoomEdit = false;
			Settings.toGTK = false;
			Settings.toM510 = false;
			Settings.toMoH = false;
			Settings.toVMF = false;
		}

		private void OutputVMF_Click(object sender, RoutedEventArgs e) {
			if(!miSaveAsAuto.IsChecked && !miSaveAsVMF.IsChecked && !miSaveAsMOH.IsChecked && !miSaveAsGC.IsChecked && !miSaveAsGTK.IsChecked && !miSaveAsDE.IsChecked) {
				miSaveAsAuto.IsChecked = true;
				Settings.toAuto = true;
				Settings.toVMF = false;
			} else {
				miSaveAsAuto.IsChecked = false;
				Settings.toAuto = false;
				Settings.toVMF = miSaveAsVMF.IsChecked;
			}
		}

		private void OutputDE_Click(object sender, RoutedEventArgs e) {
			if(!miSaveAsAuto.IsChecked && !miSaveAsVMF.IsChecked && !miSaveAsMOH.IsChecked && !miSaveAsGC.IsChecked && !miSaveAsGTK.IsChecked && !miSaveAsDE.IsChecked) {
				miSaveAsAuto.IsChecked = true;
				Settings.toAuto = true;
				Settings.toDoomEdit = false;
			} else {
				miSaveAsAuto.IsChecked = false;
				Settings.toAuto = false;
				Settings.toDoomEdit = miSaveAsDE.IsChecked;
			}
		}

		private void OutputGC_Click(object sender, RoutedEventArgs e) {
			if(!miSaveAsAuto.IsChecked && !miSaveAsVMF.IsChecked && !miSaveAsMOH.IsChecked && !miSaveAsGC.IsChecked && !miSaveAsGTK.IsChecked && !miSaveAsDE.IsChecked) {
				miSaveAsAuto.IsChecked = true;
				Settings.toAuto = true;
				Settings.toM510 = false;
			} else {
				miSaveAsAuto.IsChecked = false;
				Settings.toAuto = false;
				Settings.toM510 = miSaveAsGC.IsChecked;
			}
		}

		private void OutputMOH_Click(object sender, RoutedEventArgs e) {
			if(!miSaveAsAuto.IsChecked && !miSaveAsVMF.IsChecked && !miSaveAsMOH.IsChecked && !miSaveAsGC.IsChecked && !miSaveAsGTK.IsChecked && !miSaveAsDE.IsChecked) {
				miSaveAsAuto.IsChecked = true;
				Settings.toAuto = true;
				Settings.toMoH = false;
			} else {
				miSaveAsAuto.IsChecked = false;
				Settings.toAuto = false;
				Settings.toMoH = miSaveAsMOH.IsChecked;
			}
		}

		private void OutputGTK_Click(object sender, RoutedEventArgs e) {
			if(!miSaveAsAuto.IsChecked && !miSaveAsVMF.IsChecked && !miSaveAsMOH.IsChecked && !miSaveAsGC.IsChecked && !miSaveAsGTK.IsChecked && !miSaveAsDE.IsChecked) {
				miSaveAsAuto.IsChecked = true;
				Settings.toAuto = true;
				Settings.toGTK = false;
			} else {
				miSaveAsAuto.IsChecked = false;
				Settings.toAuto = false;
				Settings.toGTK = miSaveAsGTK.IsChecked;
			}
		}

		private void Ppts_Click(object sender, RoutedEventArgs e) {
			try {
				double input = Double.Parse(Microsoft.VisualBasic.Interaction.InputBox("Please enter plane point coefficient.\nCurrent Value: "+Settings.planePointCoef, "Enter new coefficient", Settings.planePointCoef.ToString(), -1, -1));
				if(input == 0) {
					throw new Exception("fuck");
				}
				Settings.planePointCoef = input;
			} catch {
				DecompilerThread.OnMessage(this, "Invalid plane point coefficient! Please enter a nonzero number!");
			}
		}

		private void Planar_Click(object sender, RoutedEventArgs e) {
			Settings.planarDecomp = miPlanar.IsChecked;
		}

		private void PlaneFlip_Click(object sender, RoutedEventArgs e) {
			Settings.skipPlaneFlip = miSkipFlip.IsChecked;
		}

		private void BrushCorns_Click(object sender, RoutedEventArgs e) {
			Settings.calcVerts = miCalcCorners.IsChecked;
		}

		private void ESD_Click(object sender, RoutedEventArgs e) {
			Settings.roundNums = miESD.IsChecked;
		}

		private void Pak_Click(object sender, RoutedEventArgs e) {
			Settings.extractZip = miPak.IsChecked;
		}

		private void NumThreads_Click(object sender, RoutedEventArgs e) {
			try {
				int input = Int32.Parse(Microsoft.VisualBasic.Interaction.InputBox("Please enter number of concurrent decompiles allowed.\nCurrent value: "+Settings.numThreads, "Enter new thread amount", Settings.numThreads.ToString(), -1, -1));
				if(input < 1) {
					throw new Exception("fuck");
				}
				Settings.numThreads = input;
			} catch {
				DecompilerThread.OnMessage(this, "Please enter a whole number greater than 0!");
			}
		}

		private void OutFolder_Click(object sender, RoutedEventArgs e) {
			try {
				System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
				if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
					Settings.outputFolder = dialog.SelectedPath + "\\";
					DecompilerThread.OnMessage(this, Settings.outputFolder);
				} else {
					Settings.outputFolder = "default";
				}
			} catch {
				Settings.outputFolder = "default"; 
			}
		}

		private void ToWorld_Click(object sender, RoutedEventArgs e) {
			Settings.brushesToWorld = miToWorld.IsChecked;
		}

		private void NoOrigin_Click(object sender, RoutedEventArgs e) {
			Settings.noOriginBrushes = miNoOrigin.IsChecked;
		}

		private void IgnoreDetail_Click(object sender, RoutedEventArgs e) {
			Settings.noDetail = miIgnoreDetail.IsChecked;
		}

		private void IgnoreWater_Click(object sender, RoutedEventArgs e) {
			Settings.noWater = miIgnoreWater.IsChecked;
		}

		private void IgnoreFaceFlag_Click(object sender, RoutedEventArgs e) {
			Settings.noFaceFlags = miIgnoreFaceFlags.IsChecked;
		}

		private void DontCorrectEntity_Click(object sender, RoutedEventArgs e) {
			Settings.noEntCorrection = miNoEntityCorrect.IsChecked;
		}

		private void DontCorrectTexture_Click(object sender, RoutedEventArgs e) {
			Settings.noTexCorrection = miNoTextureCorrect.IsChecked;
		}

		private void SpecialNull_Click(object sender, RoutedEventArgs e) {
			Settings.replaceWithNull = miSpecialNull.IsChecked;
		}

		private void DumpLump_Click(object sender, RoutedEventArgs e) {
			Settings.dumpCrashLump = miDumpLump.IsChecked;
		}

		private void DontCull_Click(object sender, RoutedEventArgs e) {
			Settings.dontCull = miDontCull.IsChecked;
		}

		private void SetMMSS_Click(object sender, RoutedEventArgs e) {
			try {
				int input = Int32.Parse(Microsoft.VisualBasic.Interaction.InputBox("Please enter a new multimanager stack size.\n"+
			          "When converting other entity systems to Source Engine entity I/O, you must recurse through\n"+
			          "multi_managers. If they reference each other in a cycle, it will loop forever. The stack prevents\n"+
			          "this from happening. Increase this to recurse further. Current value: "+Settings.mmStack, "Enter new stack size", Settings.mmStack.ToString(), -1, -1));
				if(input < 0) {
					throw new Exception("fuck");
				}
				Settings.mmStack = input;
			} catch {
				DecompilerThread.OnMessage(this, "Invalid stack size! Please enter a whole positive number or 0.");
			}
		}

		private void SetEpsilon_Click(object sender, RoutedEventArgs e) {
			try {
				double input = Double.Parse(Microsoft.VisualBasic.Interaction.InputBox("Please enter a new error tolerance value.\n"+
			          "This value is used to compensate for error propagation in double precision calculations.\n"+
			          "Typical values are between 0.0001 and 0.5. Current value: "+Settings.precision, "Enter new error tolerance", Settings.precision.ToString(), -1, -1));
				if(input < 0) {
					throw new Exception("fuck");
				}
				Settings.precision = input;
			} catch {
				DecompilerThread.OnMessage(this, "Invalid error tolerance! Please enter a positive number or 0.");
			}
		}

		private void SetOriginSize_Click(object sender, RoutedEventArgs e) {
			try {
				double input = Double.Parse(Microsoft.VisualBasic.Interaction.InputBox("Please enter a new origin brush size.\nCurrent value: "+Settings.originBrushSize, "Enter new origin brush size", Settings.originBrushSize.ToString(), -1, -1));
				if(input <= 0) {
					throw new Exception("fuck");
				}
				Settings.precision = input;
			} catch {
				DecompilerThread.OnMessage(this, "Invalid origin brush size! Please enter a positive number.");
			}
		}

		private void SaveLog_Click(object sender, RoutedEventArgs e) {
			try {
				SaveFileDialog dialog = new SaveFileDialog();
				dialog.Filter = "Text file|*.txt";
				if (dialog.ShowDialog() == true) {
					FileStream stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write);
					BinaryWriter bw = new BinaryWriter(stream);
					stream.Seek(0, SeekOrigin.Begin);
					bw.Write(txtConsole.Text);
					bw.Close();
				}
			} catch {
				DecompilerThread.OnMessage(this, "Unable to write file! Make sure the file is not read-only and that you have access to it.");
			}
		}

		private void ClearLog_Click(object sender, RoutedEventArgs e) {
			txtConsole.Text = "";
		}

		/*private MenuItem _VerbosityChecked = null;
		private string _Verbosity = "Status only";
		private void Verbosity_Click(object sender, RoutedEventArgs e) {
			if (_VerbosityChecked != null)
				_VerbosityChecked.IsChecked = false;
			else
				miVerbosityStatusOnly.IsChecked = false;
			_VerbosityChecked = e.Source as MenuItem;
			if (_VerbosityChecked != null) {
				_VerbosityChecked.IsChecked = true;
				_Verbosity = _VerbosityChecked.Header.ToString();
			}
		}*/

		private void Quit_Click(object sender, RoutedEventArgs e) {
			Application.Current.Shutdown();
		}

		private void print(object sender, MessageEventArgs e) {
			this.Dispatcher.Invoke((Action)(() => {
				txtConsole.AppendText(e.Message+"\n");
				if(txtConsole.SelectionLength==0) {
					txtConsole.ScrollToEnd();
				}
			}));
		}

		private void threadFinished(object sender, FinishEventArgs e) {
			this.Dispatcher.Invoke((Action)(() => {
				active.Remove((DecompilerThread)sender);
				sender = null;
				finished++;
				StartNextIfAble();
			}));
		}
	}

	public class Job : INotifyPropertyChanged {
		private int id = 0;
		private string map = "";
		private double percentage = 0.0;
		private DecompilerThread runnable;
		public event PropertyChangedEventHandler PropertyChanged;

		public Job(int id, string map, DecompilerThread runnable) {
			this.id = id;
			this.map = map;
			this.runnable = runnable;
			runnable.reportProgress += new ProgressEventHandler(updateProgress);
		}

		public void updateProgress(object sender, ProgressEventArgs e) {
			ProgressValue = e.Progress;
		}

		public int Id {
			get {
				return id+1;
			}
		}

		public string Name {
			get {
				return map;
			}
			set {
				map=value;
			}
		}

		public double ProgressValue {
			get {
				return percentage;
			}
			set {
				percentage = value;
				PropertyChanged(this, new PropertyChangedEventArgs("ProgressValue"));
				PropertyChanged(this, new PropertyChangedEventArgs("ProgressTooltip"));
			}
		}

		public string ProgressToolTip {
			get {
				return (Math.Round(percentage*100)).ToString()+"%";
			}
		}

		public DecompilerThread Runnable {
			get {
				return runnable;
			}
			set {
				runnable = value;
			}
		}
	}
}
