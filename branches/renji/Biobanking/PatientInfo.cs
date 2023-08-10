namespace Biobanking
{
	public struct PatientInfo
	{
		public string id;

		public string name;

		public string seqNo;

		public string age;

		public PatientInfo(string id, string name, string seqNo, string age = "")
		{
			this.id = id;
			this.name = name;
			this.seqNo = seqNo;
			this.age = age;
		}

		public PatientInfo(string id)
		{
			this.id = id;
			name = "";
			seqNo = "";
			age = "";
		}
	}
}
