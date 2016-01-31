using System;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Decompiler.GUI {
	/// <summary>
	/// Class managing a queue of <see cref="Job"/> objects and holding all jobs in an <c>ObservableCollection</c>.
	/// </summary>
	public class JobQueueManager : ObservableCollection<Job> {

		public int numThreads = Environment.ProcessorCount;

		private Queue<Job> _queue = new Queue<Job>();
		private Dictionary<Job, Thread> _activeJobs = new Dictionary<Job, Thread>();

		/// <summary>
		/// Adds a <see cref="Job"/> to the <c>ObservableCollection</c> and to the <c>Queue</c>.
		/// </summary>
		/// <param name="job"><see cref="Job"/> to add.</param>
		new public void Add(Job job) {
			base.Add(job);
			_queue.Enqueue(job);
			StartNextIfAble();
		}

		/// <summary>
		/// Removes a <see cref="Job"/> from the <c>Dictionary</c> of active jobs.
		/// </summary>
		/// <param name="job"><see cref="Job"/> to remove.</param>
		public void RemoveActive(Job job) {
			_activeJobs.Remove(job);
		}
		
		/// <summary>
		/// Starts the next <see cref="Job"/> on a new <c>Thread</c> if there is a thread available to run on.
		/// </summary>
		public void StartNextIfAble() {
			if (_queue.Count > 0) {
				if (_activeJobs.Count < numThreads) {
					Job next = _queue.Dequeue();
					_activeJobs.Add(next, new Thread(next.Run));
					_activeJobs[next].Start();
					StartNextIfAble(); // Recursively call this until either the queue is empty or the active jobs is full
				}
			} else {
				// All job threads have finished and none are queued; good time to collect garbage
				GC.Collect();
			}
		}

	}
}
