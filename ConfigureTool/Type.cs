using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigureTool
{
    public enum Type
    {
        TString,
        TInt,
        TBool,
        TDouble
    }

    class TypeConstrain
    {
        static public Dictionary<string, Type> ExtractTypeInfo(Dictionary<string, string> dict)
        {
            Dictionary<string, Type> key_type = new Dictionary<string, Type>();
            foreach(var pair in dict)
            {
                bool result = false;
                double dVal = 0;
                int val = 0;
                if ( bool.TryParse(pair.Value,out result))
                {
                    key_type.Add(pair.Key, Type.TBool);
                }
                else if (double.TryParse(pair.Value, out dVal))
                {
                    key_type.Add(pair.Key, Type.TDouble);
                }
                else if(int.TryParse(pair.Value,out val))
                {
                    key_type.Add(pair.Key, Type.TInt);
                }
                else
                {
                    key_type.Add(pair.Key, Type.TString);
                }
            }
            return key_type;
        }

    }
}
