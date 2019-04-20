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

using LibBSP;

namespace Decompiler.GUI {
	using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		private JobQueueManager jobs = new JobQueueManager();

		private string outputFolder = "";

		private MapType openAs = MapType.Undefined;
		private MenuItem _OpenAsChecked = null;

		/// <summary>
		/// Initializes a new <see cref="MainWindow"/> object.
		/// </summary>
		public MainWindow() {
			InitializeComponent();
			jobListView.ItemsSource = jobs;
			taskBarItemInfo1.ProgressState = TaskbarItemProgressState.Normal;
			Job.MessageEvent += Print;
			Job.JobFinishedEvent += JobFinished;
#pragma warning disable 0162
			if (Revision.version == "To be replaced on build") {
				this.Title = "BSP Decompiler v5";
			} else {
				this.Title = "BSP Decompiler v5 r" + Revision.version;
			}
#pragma warning restore 0162
		}

		/// <summary>
		/// Handler for File -&gt; Open BSP menu option.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void FileOpen_Click(object sender, RoutedEventArgs e) {
			ShowOpenDialog(false);
		}

		/// <summary>
		/// Hanler for File -&gt; Open all in folder menu option.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void FileOpenFolder_Click(object sender, RoutedEventArgs e) {
			ShowOpenDialog(true, false);
		}
		
		/// <summary>
		/// Hanler for File -&gt; Open all in folder recursively menu option.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void FileOpenFolderRecursive_Click(object sender, RoutedEventArgs e) {
			ShowOpenDialog(true, true);
		}

		/// <summary>
		/// Handler for all File -&gt; Open As menu options.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void OpenAs_Click(object sender, RoutedEventArgs e) {
			if (_OpenAsChecked != null) {
				_OpenAsChecked.IsChecked = false;
			} else {
				miOpenAsAuto.IsChecked = false;
			}
			_OpenAsChecked = e.Source as MenuItem;
			if (_OpenAsChecked != null) {
				_OpenAsChecked.IsChecked = true;
				openAs = (MapType)Int32.Parse(_OpenAsChecked.Tag.ToString());
			}
		}

		/// <summary>
		/// Handler for File -&gt; Output Format -&gt; Auto menu option.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void OutputAuto_Click(object sender, RoutedEventArgs e) {
			miSaveAsAuto.IsChecked = true;
			miSaveAsVMF.IsChecked = false;
			miSaveAsMOH.IsChecked = false;
			miSaveAsCoD.IsChecked = false;
			miSaveAsGC.IsChecked = false;
			miSaveAsGTK.IsChecked = false;
			miSaveAsDE.IsChecked = false;
		}

		/// <summary>
		/// Handler for File -&gt; Open MAP or VMF menu option.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void FileOpenMAP_Click(object sender, RoutedEventArgs e) {
			ShowOpenDialog(true);
		}

		/// <summary>
		/// Shows the Open dialog, for compiled or uncompiled maps.
		/// </summary>
		/// <param name="folder">Should we be looking for a folder instead?</param>
		/// <param name="recursive">If we're looking for a folder, should we recurse through all subdirectories looking for maps?</param>
		private void ShowOpenDialog(bool folder = false, bool recursive = false) {
			if (folder) {
				FolderBrowserDialog folderBrowser = new FolderBrowserDialog();

				if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
					string[] files = Directory.GetFiles(folderBrowser.SelectedPath, "*.*bsp", (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
					AddJobs(files);
				}
			} else {
				OpenFileDialog fileOpener = new OpenFileDialog();
				fileOpener.Filter = "BSP Files|*.bsp;*.d3dbsp|All Files|*.*";
				fileOpener.Multiselect = true;

				// Process open file dialog box results 
				if (fileOpener.ShowDialog() == true) {
					AddJobs(fileOpener.FileNames);
				}
			}
		}

		/// <summary>
		/// Starts decompile jobs for all files in <paramref name="filesToOpen"/>.
		/// </summary>
		/// <param name="filesToOpen">The BSP files to decompile.</param>
		private void AddJobs(string[] filesToOpen) {
			for (int i = 0; i < filesToOpen.Length; ++i) {
				Job.Settings settings = new Job.Settings() {
					replace512WithNull = miSpecialNull.IsChecked,
					noFaceFlags = miIgnoreFaceFlags.IsChecked,
					brushesToWorld = miToWorld.IsChecked,
					noTexCorrection = miNoTextureCorrect.IsChecked,
					noEntCorrection = miNoEntityCorrect.IsChecked,
					outputFolder = outputFolder,
					openAs = openAs,
					toAuto = miSaveAsAuto.IsChecked,
					toM510 = miSaveAsGC.IsChecked,
					toVMF = miSaveAsVMF.IsChecked,
					toGTK = miSaveAsGTK.IsChecked,
					toDoomEdit = miSaveAsDE.IsChecked,
					toMoH = miSaveAsMOH.IsChecked,
					toCoD = miSaveAsCoD.IsChecked
				};
				Job theJob = new Job(jobs.Count, filesToOpen[i], settings);
				theJob.PropertyChanged += new PropertyChangedEventHandler(UpdateTaskbar);
				jobs.Add(theJob);
			}
			jobs.StartNextIfAble();
		}

		/// <summary>
		/// Handler for all File -&gt; Output Format menu options except Auto.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void OutputSpecific_Click(object sender, RoutedEventArgs e) {
			if (!miSaveAsAuto.IsChecked && !miSaveAsVMF.IsChecked && !miSaveAsMOH.IsChecked && !miSaveAsGC.IsChecked && !miSaveAsGTK.IsChecked && !miSaveAsDE.IsChecked && !miSaveAsCoD.IsChecked) {
				miSaveAsAuto.IsChecked = true;
			} else {
				miSaveAsAuto.IsChecked = false;
			}
		}

		/// <summary>
		/// Handler for Options -&gt; Set number of threads menu option.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void NumThreads_Click(object sender, RoutedEventArgs e) {
			try {
				int input = Int32.Parse(Microsoft.VisualBasic.Interaction.InputBox("Please enter number of concurrent decompiles allowed.\nCurrent value: " + jobs.numThreads, "Enter new thread amount", jobs.numThreads.ToString(), -1, -1));
				if (input >= 1) {
					jobs.numThreads = input;
				} else {
					Print(this, new MessageEventArgs("Please enter a whole number greater than 0!"));
				}
			} catch {
				Print(this, new MessageEventArgs("Please enter a whole number greater than 0!"));
			}
		}

		/// <summary>
		/// Handler for Options -&gt; Set output folder menu option.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void OutFolder_Click(object sender, RoutedEventArgs e) {
			try {
				System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
				if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
					outputFolder = dialog.SelectedPath + "\\";
					Print(this, new MessageEventArgs(outputFolder));
				} else {
					outputFolder = "";
				}
			} catch {
				outputFolder = "";
			}
		}

		/// <summary>
		/// Handler for Debug -&gt; Save log menu option.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
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
				Print(this, new MessageEventArgs("Unable to write file! Make sure you have access to the directory."));
			}
		}

		/// <summary>
		/// Handler for Debug -&gt; Clear log menu option.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void ClearLog_Click(object sender, RoutedEventArgs e) {
			txtConsole.Text = "";
		}

		/// <summary>
		/// Handler for Help -&gt; About menu option.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void About_Click(object sender, RoutedEventArgs e) {
			if (MessageBox.Show(this,
				"BSP Decompiler v5 revision " + Revision.version + " " + Revision.configuration + "\nBuilt on " + LibBSP.Revision.dateTime + "\nWritten by William Fowler\n\nFull source code available on GitHub at https://github.com/wfowler1/bsp-decompiler\nWould you like to go there now?",
				"About BSP Decompiler",
				MessageBoxButton.YesNo,
				MessageBoxImage.Information,
				MessageBoxResult.No) == MessageBoxResult.Yes) {
				System.Diagnostics.Process.Start("https://github.com/wfowler1/bsp-decompiler");
			}
		}

		/// <summary>
		/// Handler for Help -&gt; About LibBSP log menu option.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void AboutLibBSP_Click(object sender, RoutedEventArgs e) {
			if (MessageBox.Show(this,
				"LibBSP revision " + LibBSP.Revision.version + " " + LibBSP.Revision.configuration + "\nBuilt on " + LibBSP.Revision.dateTime + "\nWritten by William Fowler\n\nFull source code available on GitHub at https://github.com/wfowler1/LibBSP\nWould you like to go there now?",
				"About LibBSP",
				MessageBoxButton.YesNo,
				MessageBoxImage.Information,
				MessageBoxResult.No) == MessageBoxResult.Yes) {
				System.Diagnostics.Process.Start("https://github.com/wfowler1/LibBSP");
			}
		}

		/// <summary>
		/// Handler for File -&gt; Quit menu option.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>RoutedEventArgs</c> for this event.</param>
		private void Quit_Click(object sender, RoutedEventArgs e) {
			Application.Current.Shutdown();
		}

		/// <summary>
		/// Handles an error in a <see cref="Job"/> and tells the <see cref="JobQueueManager"/> to start another.
		/// </summary>
		/// <param name="job"><see cref="Job"/> that has a problem.</param>
		private void Error(Job job) {
			taskBarItemInfo1.ProgressState = TaskbarItemProgressState.Error;
			jobs.RemoveActive(job);
			jobs.StartNextIfAble();
		}

		/// <summary>
		/// Appends a <c>string</c> to the log output.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><see cref="MessageEventArgs"/> containing the <c>string</c> to append and an optional error flag.</param>
		private void Print(object sender, MessageEventArgs e) {
			Dispatcher.Invoke(() => {
				txtConsole.AppendText(e.message + "\n");
				if (txtConsole.SelectionLength == 0) {
					txtConsole.ScrollToEnd();
				}
				if (e.error) {
					Error(sender as Job);
				}
			});
		}

		/// <summary>
		/// Handler for when a <see cref="Job"/> finishes, tells the <see cref="JobQueueManager"/> to start another.
		/// </summary>
		/// <param name="sender">Sender of this event, should be the <see cref="Job"/> that was completed.</param>
		/// <param name="e"><c>EventArgs</c> for this event. Can be <c>EventArgs.Empty</c>.</param>
		private void JobFinished(object sender, EventArgs e) {
			Dispatcher.Invoke(() => {
				jobs.RemoveActive((Job)sender);
				jobs.StartNextIfAble();
			});
		}

		/// <summary>
		/// Handler to update the taskbar progress indicator.
		/// </summary>
		/// <param name="sender">Sender of this event.</param>
		/// <param name="e"><c>EventArgs</c> for this event.</param>
		private void UpdateTaskbar(object sender, EventArgs e) {
			Dispatcher.Invoke(() => {
				double cumulativeProgress = 0.0;
				foreach (Job job in jobs) {
					cumulativeProgress += job.progress;
				}
				double val = (cumulativeProgress / (double)jobs.Count);
				if (val != 1) {
					taskBarItemInfo1.ProgressValue = val;
				} else {
					taskBarItemInfo1.ProgressValue = 0;
				}
			});
		}
	}
}
