using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigureTool
{
    class DescriptionHelper
    {
        Dictionary<string, string> item_Desc = new Dictionary<string, string>();
        public DescriptionHelper()
        {
            item_Desc.Add("EVOModel","EVO 型号，可以是75,100,150,或者200，用于计算可用的Grid数量。");
            item_Desc.Add("MeasureName", "测量液面装置名称，可以是TIU，也可以是BoundaryID。");
            item_Desc.Add("reportPath", "液面测量结果文件路径,类似：D:\\TIU\\data.txt");
            item_Desc.Add("DstBarcodeFolder", "目标条码文件夹，可以是底部二维码扫描仪扫出来的，也可以是POSID扫出来的。");
            
        }

        public string this[string key]
        {
            get
            {
                return item_Desc[key];
            }
        }
            
    }
}
