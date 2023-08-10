using System;
using System.Collections.Generic;

namespace Biobanking
{
	internal class BaseReader : IResultReader
	{
		public List<PatientInfo> ReadPatientInfos()
		{
			return new PatientInfoReader().Read();
		}

		public virtual List<DetectedInfo> Read()
		{
			throw new NotImplementedException();
		}
	}
}
