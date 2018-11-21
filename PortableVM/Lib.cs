using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableVM
{
    public delegate object LibInstruction(List<DynamicValue> rawArguments, List<DynamicValue> solvedArgs, ref int nextIp);
    public abstract class Lib
    {
        public VM vm;
        public Dictionary<string, LibInstruction> instructions = new Dictionary<string, LibInstruction>();

    }
}
