using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Biobanking
{
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
	[DebuggerNonUserCode]
	[CompilerGenerated]
	internal class stringRes
	{
		private static ResourceManager resourceMan;

		private static CultureInfo resourceCulture;

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (resourceMan == null)
				{
					resourceMan = new ResourceManager("Biobanking.stringRes", typeof(stringRes).Assembly);
				}
				return resourceMan;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return resourceCulture;
			}
			set
			{
				resourceCulture = value;
			}
		}

		internal static string expression => ResourceManager.GetString("expression", resourceCulture);

		internal static string f => ResourceManager.GetString("f", resourceCulture);

		internal static string labwareSettingFileName => ResourceManager.GetString("labwareSettingFileName", resourceCulture);

		internal static string MeasureName => ResourceManager.GetString("MeasureName", resourceCulture);

		internal static string pipettingSettingFileName => ResourceManager.GetString("pipettingSettingFileName", resourceCulture);

		internal static string reportPath => ResourceManager.GetString("reportPath", resourceCulture);

		internal static string version => ResourceManager.GetString("version", resourceCulture);

		internal stringRes()
		{
		}
	}
}
