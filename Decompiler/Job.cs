using System;

namespace decompileronly
{
	public class Job
	{
		private int _Id = -1;
		public int Id
		{
			get { return _Id; }
			set { _Id = value; }
		}

		private string _Name = "";
		public string Name
		{
			get { return _Name; }
			set { _Name = value; }
		}

		private double _ProgressValue = 0;
		public double ProgressValue
		{
			get { return _ProgressValue; }
			set { _ProgressValue = value; }
		}
		private double _ProgressMaxValue = 100;
		public double ProgressMaxValue
		{
			get { return _ProgressMaxValue; }
			set { _ProgressMaxValue = value; }
		}
		private string _ProgressToolTip = "";
		public string ProgressToolTip
		{
			get { return _ProgressToolTip; }
			set { _ProgressToolTip = value; }
		}

		private string _ButtonText = "";
		public string ButtonText
		{
			get { return _ButtonText; }
			set { _ButtonText = value; }
		}
	}
}
