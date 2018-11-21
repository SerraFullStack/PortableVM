using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortableVM;

namespace PortableVM.Libs
{
    public class Standard : Lib
    {
        Stack<int> stack = new Stack<int>();
        public Standard()
        {
            Standard.autoParseCSharpLib(this, this.instructions);
        }
        
        public static void autoParseCSharpLib(object lib, Dictionary<string, LibInstruction> instructionsDictionary)
        {
            //this.instructions.Add("setvar", SetVar);
            var methods = lib.GetType().GetMethods();
            foreach (var c in methods)
            {
                //checks if method is a valid instruction
                var argsTypes = c.GetParameters();
                if (argsTypes.Length == 3)
                {
                    string typeOfFirstArgument = argsTypes[0].ToString();
                    string typeOfSecondArgument = argsTypes[1].ToString();
                    string typeOfThirdArgument = argsTypes[2].ToString();


                    if ((typeOfFirstArgument.StartsWith("System.Collections.Generic.List`1[PortableVM.DynamicValue]")) &&
                        (typeOfSecondArgument.StartsWith("System.Collections.Generic.List`1[PortableVM.DynamicValue]")) &&
                        (typeOfThirdArgument.StartsWith("Int32&")))
                    {
                        var deleg = c.CreateDelegate(typeof(LibInstruction), lib);
                        instructionsDictionary.Add(c.Name.ToLower(), (LibInstruction)deleg);
                    }
                }
            }
            
        }

        public object Set(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            vm.SetVar(arguments[0].AsString, solvedArgs[1]);
            return null;
        }
        
        public object Eval(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            string namedCode = (string)this.GetNewId(arguments, solvedArgs, ref nextIp);
            
            List<string> newCode = new List<string>();
            newCode.Add("_nc_ " + newCode);
            newCode.AddRange(solvedArgs[0].AsString.Replace("\r", "\n").Replace("\n\n", "\n").Split('\n'));
            newCode.Add("return");
            vm.addCode(newCode);
            
            
            List<DynamicValue>callArgs = new List<DynamicValue>(){
                new DynamicValue(newCode)
            };
            this.Call(callArgs, callArgs, ref nextIp);
            
            
            vm.SetVar(arguments[0].AsString, solvedArgs[1]);
            return null;
        }

        public object Goto(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            string gotoName = solvedArgs[0].AsString;
            
            //identify closest namedeCode from currentIp
            if (vm.namedCodePointers.ContainsKey(solvedArgs[0].AsString.ToLower()))
            {
                int closest = nextIp;
                int distance = int.MaxValue;
                foreach (var c in vm.namedCodePointers[solvedArgs[0].AsString.ToLower()])
                {
                   if (System.Math.Abs(c - nextIp) < distance)
                   {
                       closest = c;
                       distance = System.Math.Abs(c - nextIp);
                   }
                }
                
                if (distance != int.MaxValue)
                {
                    nextIp = closest;
                }
                else
                    throw(new Exception("Could not possibe determine the position of named code "+solvedArgs[0]));
            }
            else
                throw(new Exception("Could not possibe determine the position of named code "+solvedArgs[0]));
            
            return null;
        }
        
        public object Call(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            stack.Push(nextIp);
            this.Goto(arguments, solvedArgs, ref nextIp);
            return null;
        }
        
        public object Return(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            nextIp = stack.Pop();
            
            if (solvedArgs.Count > 0)
                return solvedArgs[0];
            else
                return null;
        }

        public object If(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            
            string operand1 = solvedArgs[0].AsString;
            string _operator = solvedArgs[1].AsString;
            string operand2 = solvedArgs[2].AsString;
            
            bool result = false;
            switch (_operator)
            {
                case "==":
                case "=":
                    result = operand1 == operand2;
                    break;
                case ">":
                    result = operand1.CompareTo(operand2) > 0;
                    break;
                case ">=":
                    result = operand1.CompareTo(operand2) >= 0;
                    break;
                case "<":
                    result = operand1.CompareTo(operand2) < 0;
                    break;
                case "<=":
                    result = operand1.CompareTo(operand2) <= 0;
                    break;
                case "||":
                    result = solvedArgs[0].AsBool || solvedArgs[2].AsBool;
                    break;
                case "&&":
                    result = solvedArgs[0].AsBool && solvedArgs[2].AsBool;
                    break;
            }
            
            if ((result) && (solvedArgs.Count > 3))
                this.Goto(new List<DynamicValue>{ arguments[3] }, new List<DynamicValue>{ solvedArgs[3] }, ref nextIp);
            else if (solvedArgs.Count > 4)
                this.Goto(new List<DynamicValue>{ arguments[4] }, new List<DynamicValue>{ solvedArgs[4] }, ref nextIp);
            
            return result;
        }
        
        public object cmp(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            return solvedArgs[0].AsString.CompareTo(solvedArgs[1].AsString);
        }

        public object Je(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            
            bool result = false;
            string jumpTo = "";
            
            if (solvedArgs.Count > 1)
            {
                string operand1 = solvedArgs[0].AsString;
                string operand2 = solvedArgs[1].AsString;
                result = operand1 == operand2;
                
                jumpTo = solvedArgs[2].AsString;
            }
            else
            {
                result = vm.GetVar(Consts._RESULT, new DynamicValue(false)).AsInt == 0;
                jumpTo = solvedArgs[0].AsString;
            }
            
            if (result)
                this.Goto(new List<DynamicValue>{ new DynamicValue(jumpTo) }, new List<DynamicValue>{ new DynamicValue(jumpTo) }, ref nextIp);
            
            return null;
            
        }
        
        public object Jne(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            bool result = false;
            string jumpTo = "";
            
            if (solvedArgs.Count > 1)
            {
                string operand1 = solvedArgs[0].AsString;
                string operand2 = solvedArgs[1].AsString;
                result = operand1 != operand2;
                
                jumpTo = solvedArgs[2].AsString;
            }
            else
            {
                result = vm.GetVar(Consts._RESULT, new DynamicValue(false)).AsInt != 0;
                jumpTo = solvedArgs[0].AsString;
            }
            
            if (result)
                this.Goto(new List<DynamicValue>{ new DynamicValue(jumpTo) }, new List<DynamicValue>{ new DynamicValue(jumpTo) }, ref nextIp);
            
            return null;
        }
        
        public object Jb(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            bool result = false;
            string jumpTo = "";
            
            if (solvedArgs.Count > 1)
            {
                string operand1 = solvedArgs[0].AsString;
                string operand2 = solvedArgs[1].AsString;
                result = operand1.CompareTo(operand2) > 0;
                
                jumpTo = solvedArgs[2].AsString;
            }
            else
            {
                result = vm.GetVar(Consts._RESULT, new DynamicValue(false)).AsInt > 0;
                jumpTo = solvedArgs[0].AsString;
            }
            
            if (result)
                this.Goto(new List<DynamicValue>{ new DynamicValue(jumpTo) }, new List<DynamicValue>{ new DynamicValue(jumpTo) }, ref nextIp);
            
            return null;
        }
        
        public object Jbe(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            bool result = false;
            string jumpTo = "";
            
            if (solvedArgs.Count > 1)
            {
                string operand1 = solvedArgs[0].AsString;
                string operand2 = solvedArgs[1].AsString;
                result = operand1.CompareTo(operand2) >= 0;
                
                jumpTo = solvedArgs[2].AsString;
            }
            else
            {
                result = vm.GetVar(Consts._RESULT, new DynamicValue(false)).AsInt >= 0;
                jumpTo = solvedArgs[0].AsString;
            }
            
            if (result)
                this.Goto(new List<DynamicValue>{ new DynamicValue(jumpTo) }, new List<DynamicValue>{ new DynamicValue(jumpTo) }, ref nextIp);
            
            return null;
        }
        
        public object Jl(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            bool result = false;
            string jumpTo = "";
            
            if (solvedArgs.Count > 1)
            {
                string operand1 = solvedArgs[0].AsString;
                string operand2 = solvedArgs[1].AsString;
                result = operand1.CompareTo(operand2) < 0;
                
                jumpTo = solvedArgs[2].AsString;
            }
            else
            {
                result = vm.GetVar(Consts._RESULT, new DynamicValue(false)).AsInt < 0;
                jumpTo = solvedArgs[0].AsString;
            }
            
            if (result)
                this.Goto(new List<DynamicValue>{ new DynamicValue(jumpTo) }, new List<DynamicValue>{ new DynamicValue(jumpTo) }, ref nextIp);
            
            return null;
        }
        
        public object Jle(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            bool result = false;
            string jumpTo = "";
            
            if (solvedArgs.Count > 1)
            {
                string operand1 = solvedArgs[0].AsString;
                string operand2 = solvedArgs[1].AsString;
                result = operand1.CompareTo(operand2) <= 0;
                
                jumpTo = solvedArgs[2].AsString;
            }
            else
            {
                result = vm.GetVar(Consts._RESULT, new DynamicValue(false)).AsInt <= 0;
                jumpTo = solvedArgs[0].AsString;
            }
            
            if (result)
                this.Goto(new List<DynamicValue>{ new DynamicValue(jumpTo) }, new List<DynamicValue>{ new DynamicValue(jumpTo) }, ref nextIp);
            
            return null;
        }
        
        public object Finish(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            nextIp = int.MaxValue;
            return null;
        }
        
        int idCount = 0;
        public object GetNewId(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            idCount++;
            return DateTime.Now.ToString("yyyyMMddHHmmss") + DateTime.Now.Millisecond + idCount;
        }

        

    }
}
