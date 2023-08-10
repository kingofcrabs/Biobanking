using System;
using System.Collections.Generic;
using System.IO;

namespace Biobanking
{
	internal class SciRobotHelper
	{
		private int GetHeightColumn(string sContent)
		{
			string[] array = sContent.Split('\t');
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].ToLower() == "height")
				{
					return i;
				}
			}
			throw new Exception("没有找到名为height的列。");
		}

		public void ReadZValues(ref List<DetectedInfo> heights)
		{
			string path = "C:\\BuffyEx\\data\\LastReport.txt";
			int num = 0;
			using (StreamReader streamReader = new StreamReader(path))
			{
				while (streamReader.ReadLine() != null)
				{
					num++;
				}
			}
			int num2 = (num - 1) / 2;
			heights = new List<DetectedInfo>();
			for (int i = 0; i < num2; i++)
			{
				heights.Add(new DetectedInfo());
			}
			using StreamReader streamReader2 = new StreamReader(path);
			string text = "";
			bool flag = true;
			int num3 = 0;
			int num4 = 0;
			while (true)
			{
				text = streamReader2.ReadLine();
				if (text == null)
				{
					break;
				}
				if (text == "")
				{
					continue;
				}
				if (flag)
				{
					flag = false;
					num3 = GetHeightColumn(text);
				}
				else
				{
					int index = (num4 - 1) % num2;
					string[] array = text.Split('\t');
					if (num4 < (num + 1) / 2)
					{
						heights[index].Z1 = 10.0 * double.Parse(array[num3]);
					}
					else
					{
						heights[index].Z2 = 10.0 * double.Parse(array[num3]);
					}
				}
				num4++;
			}
		}
	}
}
