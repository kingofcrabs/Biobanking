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
            //exe.config
            item_Desc.Add("EVOModel","EVO 型号，可以是75,100,150,或者200，用于计算可用的Grid数量。");
            item_Desc.Add("MeasureName", "测量液面装置名称，可以是TIU，也可以是BoundaryID。");
            item_Desc.Add("reportPath", "液面测量结果文件路径,类似：D:\\TIU\\data.txt");
            item_Desc.Add("DstBarcodeFolder", "目标条码文件夹，可以是底部二维码扫描仪扫出来的，也可以是POSID扫出来的。");
            item_Desc.Add("PlasmaMaxCount", "最多允许客户设置几份Plasma。");
            item_Desc.Add("BuffyMaxCount", "最多允许客户设置几份Buffy");
            item_Desc.Add("maxSampleCount", "一次分液最多允许多少个样品。");

            //labwareSettings
            item_Desc.Add("tipCount", "使用多少根枪头加样。");
            item_Desc.Add("dstLabwareRows", "目标Labware的行数，96孔板的话就是8行，16孔载架就是16。");
            item_Desc.Add("dstLabwareColumns", "目标Labware的列数，96孔板的话就是12列，16孔载架就是1。");
            item_Desc.Add("dstLabwareStartGrid", "第一个目标Labware的Grid位置。");
            item_Desc.Add("sourceWells", "源Labware孔数，永远为16位载架，所以永远是16个。");
            item_Desc.Add("sourceLabwareGrids", "样品管条数");
            item_Desc.Add("sourceLabwareStartGrid", "样品管起始Grid。");
            item_Desc.Add("wasteGrid", "waste载架所在的Grid。");
            item_Desc.Add("dstCarrierCnt", "有几个目标Carrier。");
            item_Desc.Add("gridsPerCarrier", "一个目标载架占几个Grid。");
            item_Desc.Add("sitesPerCarrier", "一个目标载架占几个Site");

            //pipettingSettings

            item_Desc.Add("buffyAspirateLayers", "白膜吸取层数。");
            item_Desc.Add("dstPlasmaSlice", "目标Plasma份数。");
            item_Desc.Add("dstbuffySlice", "目标白膜份数。");
            item_Desc.Add("deltaXYForMSD", "吸取白膜转圈时XY偏移距离，单位mm。");
            item_Desc.Add("buffyVolume", "白膜体积。");
            item_Desc.Add("safeDelta", "安全距离，吸取Plasma时，枪头离白膜层的距离。");
            item_Desc.Add("buffySpeedFactor", "吸取白膜时速度因子。");
            item_Desc.Add("plasmaGreedyVolume", "每份Plasma时的体积，如果是0，则平均吸取。");
            //item_Desc.Add("dstRedCellSlice", "红细胞份数，已经废弃。");
            //item_Desc.Add("redCellGreedyVolume", "每份红细胞体积，已经废弃。");
            //item_Desc.Add("redCellBottomHeight", "红细胞底部废弃高度，已经废弃");
            item_Desc.Add("giveUpNotEnough", "当剩余体积不够时是否放弃。");
            item_Desc.Add("msdZDistance", "msd跨越距离。");
            item_Desc.Add("msdStartPositionAboveBuffy", "msd从白膜层上方开始吸取的位置。");
            item_Desc.Add("onlyOneSlicePerLabware", "一个Labware只放一份Plasma，第二份就放到下一个Labware上");
            item_Desc.Add("msdXYTogether", "永远为false，MSD command的早期版本xy距离相等合用一个参数。");
            item_Desc.Add("airGap", "打掉白膜后吸取的AirGap。");
            item_Desc.Add("retractHeightcm", "打掉白膜后retract高度，单位为cm。");
            item_Desc.Add("maxVolumePerSlice", "单份最大体积。");
            item_Desc.Add("bottomOffset", "PAZ的偏移量，不要修改。");
            //public bool onlyOneSlicePerRegion;
            //public bool msdXYTogether;
            //public int airGap;
            //public int bottomOffsetmm;
            //public int maxVolumePerSlice;
            //public int retractHeightcm;


            //   public int buffyAspirateLayers;
            //public int dstPlasmaSlice;
            //public int dstbuffySlice;
            //public int deltaXYForMSD;
            //public int buffyVolume;
            //public int safeDelta;
            //public double buffySpeedFactor;
            //public double plasmaGreedyVolume;
            //public int dstRedCellSlice;
            //public double redCellGreedyVolume;
            //public double redCellBottomHeight;
            //public bool giveUpNotEnough;
            //public double msdZDistance;
            //public double msdStartPositionAboveBuffy;
            //public bool onlyOneSlicePerRegion;
            //public bool msdXYTogether;
            //public int airGap;
            //public int bottomOffsetmm;
            //public int maxVolumePerSlice;
            //public int retractHeightcm;

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
