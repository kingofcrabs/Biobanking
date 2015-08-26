using System;

public class Class1
{
}

enum SampleType
{
    buffy = 0,
    plasma = 1,
    rawBlood =2,
}


class TubeInfo
{
	string barcode;         //Eppendorf tube barcode
    SampleType sampleType;  //buffy, plasma or rawblood
    string sliceID;         //1 based
    string srcBarcode;      //source sample's barcode
    string volumeUL;        //volume in ul
}

class GeneralResult
{
	bool bok;
    string errDescription;
}


