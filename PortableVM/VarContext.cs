using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableVM
{
    public class VarContext: IDisposable
    {
        public VarContext Next = null;
        public VarContext Prev = null;

        private Dictionary<string, DynamicValue> _vars = new Dictionary<string, DynamicValue>();

        public void Set(string name, object value)
        {
            name = name.ToLower();
            if (!(value is DynamicValue))
                value = new DynamicValue(value);

            _vars[name] = (DynamicValue)value;

        }

        public DynamicValue Get(string name, object defaultValue)
        {
            if (_vars.ContainsKey(name))
                return _vars[name.ToLower()];

            if (defaultValue != null)
            {
                if (!(defaultValue is DynamicValue))
                    defaultValue = new DynamicValue(defaultValue);

                return (DynamicValue)defaultValue;
            }
            return null;
        }

        public void Del(string varName)
        {
            if (_vars.ContainsKey(varName.ToLower()))
                _vars.Remove(varName.ToLower());
        }

        public void Dispose()
        {
            foreach (var c in _vars)
                c.Value.AsString = "";

            _vars.Clear();
            _vars = null;
        }
    }
}
