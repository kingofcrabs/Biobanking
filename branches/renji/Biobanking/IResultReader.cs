using System.Collections.Generic;

namespace Biobanking
{
	public interface IResultReader
	{
		List<DetectedInfo> Read();

		List<PatientInfo> ReadPatientInfos();
	}
}
