using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AutomateBarcode
{
    enum SampleType
    {
        buffy = 0,
        plasma = 1,
        rawBlood = 2,
    }

    //class TubeInfo
    //{
    //    string barcode;         //Eppendorf tube barcode
    //    string sampleType;      //buffy, plasma or rawblood
    //    string sliceID;         //1 based
    //    string srcBarcode;      //source sample's barcode
    //    string volumeUL;        //volume in ul
    //    public TubeInfo(string barcode, SampleType smpType, int sliceID, string srcBarcode, int volume)
    //    {
    //        this.barcode = barcode;
    //        this.sampleType = smpType.ToString();
    //        this.sliceID = sliceID.ToString();
    //        this.srcBarcode = srcBarcode;
    //        this.volumeUL = volume.ToString();
    //        Debug.WriteLine(string.Format("barcode: {0}; SampleType: {1}; SliceID: {2}; srcBarcode: {3}; volume: {4}",barcode,
    //            smpType,sliceID,srcBarcode,volume));
    //    }
    //}
}
