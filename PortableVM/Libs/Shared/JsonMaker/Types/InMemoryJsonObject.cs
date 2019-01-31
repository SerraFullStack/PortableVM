using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonMaker
{
    public class InMemoryJsonObject : IJSONObject
    {
        protected Dictionary<string, IJSONObject> childs = new Dictionary<string, IJSONObject>();

        private SOType type = SOType.Null;
        private string singleValue;
        public string relativeName = "";

        public override void Initialize(IJSONObject pParent, string relativeName, IJSONObject modelObject)
        {
            this.relativeName = relativeName;
            this.parent = pParent;
        }
        public override void setChild(string name, IJSONObject child)
        {
            childs[name] = child;
        }

        public override void delete(string name)
        {
            childs.Remove(name);
        }


        public override void clear()
        {
            foreach (var current in childs.Values)
                current.clear();
            childs.Clear();
        }

        protected override string serializeSingleValue()
        {
            if (this.type == SOType.Null)
                return "null";
            else if (this.type == SOType.Boolean)
                return ((this.singleValue.ToLower() == "true") || (this.singleValue == "1")) ? "true" : "false";
            else if ((this.type == SOType.String) || (this.type == SOType.DateTime))
            {
                if ((this.singleValue.Length > 0) && (this.singleValue[0] != '"'))
                    return '"' + this.singleValue + '"';
                else
                    return this.singleValue;
            }
            else if (this.type == SOType.Double)
            {
                return this.singleValue.Replace(',', '.');
            }
            else
                return this.singleValue;


        }

        public override SOType getJSONType()
        {
            if (childs.Count > 0)
            {
                if (this.isArray())
                    return SOType.__Array;
                else
                    return SOType.__Object;
            }
            return this.type;
        }

        public override void setSingleValue(string value)
        {
            this.type = this.__determineSoType(value);

            if (this.type != SOType.Null)
                this.singleValue = value;
        }


        public override List<string> __getChildsNames()
        {
            List<string> result = new List<string>();
            foreach (var c in this.childs)
                result.Add(c.Key);

            return result;
        }

        public override IJSONObject __getChild(string name, bool caseSensitive = true)
        {
            if (caseSensitive)
            {
                if (childs.ContainsKey(name))
                    return childs[name];
            }
            else
            {
                name = name.ToLower();
                foreach (var c in childs)
                    if (c.Key.ToLower() == name)
                        return c.Value;
            }
            
            return null;
        }

        public override bool __containsChild(string name, bool caseSensitive = false)
        {
            if (caseSensitive)
            {
                return this.childs.ContainsKey(name);
            }
            else
            {
                name = name.ToLower();
                foreach (var c in childs)
                    if (c.Key.ToLower() == name)
                        return true;

                return false;
            }
        }

        public override string getRelativeName()
        {
            return this.relativeName;
        }

        public override bool isDeletable()
        {
            return false;
        }
    }
}