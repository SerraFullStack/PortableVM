using System;
using System.Collections.Generic;
using System.Text;

namespace JsonMaker
{
    public enum SOType { Null, String, DateTime, Int, Double, Boolean, __Object, __Array, Undefined}
    public abstract class IJSONObject
    {
        public IJSONObject parent;
        public string name;
        
        protected SOType forcedType = SOType.Undefined;

        
        public abstract void clear();
        
        //ModelObject is the IJsonObject used to initialize the JSON library (can be used to get types and first object arguments)
        public abstract void Initialize(IJSONObject pParent, string relativeName, IJSONObject modelObject);
        
        //delete a child
        public abstract void delete(string name);
        public abstract SOType getJSONType();
        public abstract string getRelativeName();
        public virtual bool isArray()
        {
            int temp = 0;
            var childsNames = this.__getChildsNames();
            if (childsNames.Count == 0)
                return false;

            int cont = 0;
            while (cont < childsNames.Count)
            {
                if (!int.TryParse(childsNames[cont], out temp))
                    return false;
                cont++;
            }
            return true;
        }
        public abstract void setChild(string name, IJSONObject child);
        public abstract void setSingleValue(string value);
        protected abstract string serializeSingleValue();


        public virtual void forceType(SOType forcedType)
        {
            this.forcedType = forcedType;
        }
        public virtual string ToJson(bool quotesOnNames, bool format = false, int level = 0)
        {
            StringBuilder result = new StringBuilder();

            var childsNames = this.__getChildsNames();
            if (childsNames.Count > 0)
            {
                bool array = this.isArray();
                if (array)
                    result.Append("[");
                else
                    result.Append("{");

                if (format)
                    result.Append("\r\n");

                level++;

                for (int cont = 0; cont < childsNames.Count; cont++)
                {
                    if (format)
                    {
                        for (int a = 0; a < level; a++)
                            result.Append("    ");
                    }

                    var current = this.__getChild(childsNames[cont]);
                    if (array)
                        result.Append(current.ToJson(quotesOnNames, format, level));
                    else
                    {
                        if (quotesOnNames)
                            result.Append('"' + childsNames[cont]+ "\":" + current.ToJson(quotesOnNames, format, level));
                        else
                            result.Append(childsNames[cont] + ":" + current.ToJson(quotesOnNames, format, level));
                    }

                    if (cont < childsNames.Count - 1)
                    {
                        result.Append(',');
                        if (format)
                            result.Append("\r\n");
                    }
                }

                level--;
                if (format)
                {
                    result.Append("\r\n");
                    for (int a = 0; a < level; a++)
                        result.Append("    ");
                }

                if (array)
                    result.Append("]");
                else
                    result.Append("}");
                return result.ToString();
            }
            else
                return serializeSingleValue();
        }
        public abstract bool __containsChild(string name, bool caseSensitive = false);
        public abstract IJSONObject __getChild(string name, bool caseSensitive = true);
        public abstract List<string> __getChildsNames();

        //indicates to JSON if the object can be deleted after use. In Memory, for example, JSONObject could not be deleted, because its is used in a 
        //threaded list. In the fileSystem, for other example, the JSONObjects can be deleted, because its is created in each "getChild" call.
        //This function is very important in the C++ version of library. In C# version, the garbage collector auto remove orphans JsonObjects.
        public abstract bool isDeletable();

        protected SOType __determineSoType(string value)
        {
            int sucess;
            double sucess2;
            DateTime sucess3;

            if (forcedType != SOType.Undefined)
                return forcedType;

            //trye as null
            if ((value == null) || (value == "null") || (value == ""))
                return SOType.Null;
            else
            {

                if ((value == "true") || (value == "false"))
                    return SOType.Boolean;
                else
                {
                    //try as int
                    //Note: In json, numbers can'r start with 0
                    if ((value != "") && ("123456789+-".Contains(value[0]+"")) && (int.TryParse(value, out sucess)))
                        return SOType.Int;
                    else
                    {
                        //try as double
                        //Note: In json, numbers can'r start with 0
                        if ((value != "") && ("123456789+-.,".Contains(value[0] + "")) && (double.TryParse(value.Replace(",", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator).Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator), out sucess2)))
                            return SOType.Double;
                        else if ((value.Contains(":")) && (DateTime.TryParse(value.Replace("\"", ""), out sucess3)))
                        {
                            return SOType.DateTime;
                        }
                        else
                        {
                            //is a string
                            return SOType.String;
                        }
                    }
                }
            }
        }
    }
}