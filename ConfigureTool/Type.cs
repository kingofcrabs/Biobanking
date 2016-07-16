using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigureTool
{
    public enum MType
    {
        TString,
        TInt,
        TBool,
        TDouble
    }

    class TypeConstrain
    {
        static public Dictionary<string, MType> ExtractTypeInfo(Dictionary<string, string> dict)
        {
            Dictionary<string, MType> key_type = new Dictionary<string, MType>();
            foreach(var pair in dict)
            {
                bool result = false;
                double dVal = 0;
                int val = 0;
                if ( bool.TryParse(pair.Value,out result))
                {
                    key_type.Add(pair.Key, MType.TBool);
                }
                else if (int.TryParse(pair.Value, out val))
                {
                    key_type.Add(pair.Key, MType.TInt);
                }
                else if (double.TryParse(pair.Value, out dVal))
                {
                    key_type.Add(pair.Key, MType.TDouble);
                }
                else
                {
                    key_type.Add(pair.Key, MType.TString);
                }
            }
            return key_type;
        }

        static public bool IsExpectedType(string val,MType type)
        {
            switch(type)
            {
                case MType.TBool:
                    bool res = false;
                    return bool.TryParse(val, out res);
                case MType.TDouble:
                    double dRes = 0;
                    return double.TryParse(val, out dRes);
                case MType.TInt:
                    int iRes = 0;
                    return int.TryParse(val, out iRes);
                case MType.TString:
                default:
                    return true;

            }
            
            
        }

    }
}
