using System.IO;
using System.Text;

namespace Biobanking
{
	public sealed class StringWriterWithEncoding : StringWriter
	{
		private readonly Encoding encoding;

		public override Encoding Encoding => encoding;

		public StringWriterWithEncoding(Encoding encoding)
		{
			this.encoding = encoding;
		}
	}
}
