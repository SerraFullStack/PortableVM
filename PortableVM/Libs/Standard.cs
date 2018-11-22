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
            string evalLine = "";
            foreach (var c in solvedArgs)
                evalLine += c.AsString;
            
            evalLine = evalLine.Replace("\r", "\n").Replace("\n\n", "\n");
            
            
            List<string> newCode = new List<string>();
            newCode.Add("_nc_ " + namedCode);
            newCode.AddRange(evalLine.Split('\n'));
            newCode.Add("return");
            
            vm.addCode(newCode);
            
            
            List<DynamicValue>callArgs = new List<DynamicValue>(){
                new DynamicValue(namedCode)
            };
            this.Call(callArgs, callArgs, ref nextIp);
            
            
            return null;
        }

        public object Goto(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            string gotoName = solvedArgs[0].AsString;
            
            //identify closest namedeCode from currentIp
            NamedCodeRef newIpPointer = this.GetLabelAddress(gotoName, vm.ip);
            
            if (newIpPointer != null)
            {
                //create variables with names of the label arguments and set values with solvedArgs list
                for (int c = 1; c < solvedArgs.Count && (c-1) < newIpPointer.argumentsNames.Count ; c++)
                {
                    vm.SetVar(newIpPointer.argumentsNames[c-1], solvedArgs[c]);
                }
                
                //jump to found named code
                nextIp = newIpPointer.address;
            }
            else
                throw(new Exception("Could not possibe determine the position of named code "+solvedArgs[0]));
            
            return null;
        }
        
        public object Call(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            //create a new memory table
            vm.VarsMemory.Push(new Dictionary<string, DynamicValue>());
            
            
            stack.Push(nextIp);
            this.Goto(arguments, solvedArgs, ref nextIp);
            return null;
        }
        
        public object Return(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            
            //remove last memory table
            if (vm.VarsMemory.Count > 0)
                vm.VarsMemory.Pop();
            
            if (stack.Count == 0)
                nextIp = int.MaxValue;
            
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
        
        public object StarThread(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            //creat a new VM
            
            VM nVm = new VM();
            
            //share memory and code with new VM
            nVm.code = vm.code;
            nVm.Tags["parentVM"] = this;
            nVm.onUnknownInstruction += delegate (VM sender, string instruction, List<DynamicValue> arguments2, List<DynamicValue> solvedArgs2,ref int nextIp2, out bool allowContinue)
            {
                return ((VM)sender.Tags["parentVM"]).InvokeOnUnknownFunction(sender, instruction, arguments2, solvedArgs2, ref nextIp2, out allowContinue);
            };
            
            nVm.namedCodePointers = vm.namedCodePointers;
            
            //first argument contains the label To Run
            string labelName = solvedArgs[0].AsString;
            var temp = GetLabelAddress(labelName, 0);
            nVm.ip = 0;
            if (nVm.ip != null)
            {
                nVm.ip = temp.address;
            }
            
            //run new vm
            nVm.Run();
            
            return null;
        }
        
        public NamedCodeRef GetLabelAddress(string labelName, int currentIp)
        {
            labelName = labelName.ToLower();
            //identify closest namedeCode from currentIp
            if (vm.namedCodePointers.ContainsKey(labelName))
            {
                NamedCodeRef closest = null;
                int distance = int.MaxValue;
                foreach (var c in vm.namedCodePointers[labelName])
                {
                   if (System.Math.Abs(c.address- currentIp) < distance)
                   {
                       closest = c;
                       distance = System.Math.Abs(c.address - currentIp);
                   }
                }
                
                if (distance != int.MaxValue)
                {
                    return closest;
                }
            }
            
            return null;
        }

        

    }
}
