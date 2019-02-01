using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
                evalLine += c.AsString + ' ';
            
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

        public object EvalAsync(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            string namedCode = (string)this.GetNewId(arguments, solvedArgs, ref nextIp);

            string onDone = solvedArgs[0].AsString;
            string evalLine = "";
            foreach (var c in solvedArgs)
                evalLine += c.AsString;

            evalLine = evalLine.Replace("\r", "\n").Replace("\n\n", "\n");


            List<string> newCode = new List<string>();
            newCode.Add("_nc_ " + namedCode);
            newCode.AddRange(evalLine.Split('\n'));
            newCode.Add("ParentThreadCall \"" + onDone + "\"");
            newCode.Add("return");

            vm.addCode(newCode);


            List<DynamicValue> callArgs = new List<DynamicValue>(){
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
            vm.curretContext.Next = new VarContext();
            vm.curretContext.Next.Prev = vm.curretContext;
            vm.curretContext = vm.curretContext.Next;
            
            
            stack.Push(nextIp);
            this.Goto(arguments, solvedArgs, ref nextIp);
            return null;
        }
        
        public object Return(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {

            //remove last memory table

            if (vm.curretContext.Prev != null)
            {
                vm.curretContext = vm.curretContext.Prev;

                //currentContext never can be rootContext
                if (vm.curretContext == vm.rootContext)
                    vm.curretContext = new VarContext();
                else
                    vm.curretContext.Next.Dispose();


            }
            
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
            object operand1;
            object operand2;
            string _operator = solvedArgs[1].AsString;

            if (IsDigitsOnly(solvedArgs[0].AsString) && (IsDigitsOnly(solvedArgs[2].AsString)))
            {
                operand1 = solvedArgs[0].AsDouble;
                operand2 = solvedArgs[2].AsDouble;
            }
            else
            {
                operand1 = solvedArgs[0].AsString;
                operand2 = solvedArgs[2].AsString;
            }

            
            bool result = false;
            switch (_operator)
            {
                case "==":
                case "=":
                    result = operand1 == operand2;
                    break;
				case "!=":
                case "!":
                    result = operand1 != operand2;
                    break;
                case ">":
                    if (operand1 is double)
                        result = ((double)operand1).CompareTo((double)operand2) > 0;
                    else
                        result = ((string)operand1).CompareTo((string)operand2) > 0;
                    break;
                case ">=":
                    if (operand1 is double)
                        result = ((double)operand1).CompareTo((double)operand2) >= 0;
                    else
                        result = ((string)operand1).CompareTo((string)operand2) >= 0;
                    break;
                case "<":
                    if (operand1 is double)
                        result = ((double)operand1).CompareTo((double)operand2) < 0;
                    else
                        result = ((string)operand1).CompareTo((string)operand2) < 0;
                    break;
                case "<=":
                    if (operand1 is double)
                        result = ((double)operand1).CompareTo((double)operand2) <= 0;
                    else
                        result = ((string)operand1).CompareTo((string)operand2) <= 0;
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
        
        public object Cmp(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
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
            if (solvedArgs.Count > 0)
                vm.SetVar(Consts._VMRUNRESULT, solvedArgs[0]);

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
            nVm.rootContext = vm.rootContext;

            //defining the root context as same of vm.rootContext, the threads can share global vars
            //but
            //definig the root context as same of vm.currentContext, the treads share firsts contexts (the new threads will contains a fork of the main thread context)
            nVm.curretContext = new VarContext();
            //nVm.curretContext.Prev = vm.rootContext;
            nVm.curretContext.Prev = vm.curretContext;

            nVm.Tags["parentVM"] = vm;
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

        public object SyncThreadCall(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            VM destThread = VM.vmsPointers[solvedArgs[0].AsInt];

            var lockCurrVm = true;
            int parentStackSize = -1;
            Stack<int> parentStack = null;
            destThread.pause(() => {

                ((Standard)destThread.GetLibs()["standard"]).Call(arguments, solvedArgs, ref destThread.ip);

                //wait for return
                parentStack = ((Standard)destThread.GetLibs()["standard"]).stack;
                parentStackSize = parentStack.Count;

                lockCurrVm = false;
            });

            destThread.resume();

            while ((lockCurrVm) || (parentStack.Count >= parentStackSize))
                Thread.Sleep(1);

            return null;
        }

        public object ParentThreadCall(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            VM destThread = (VM)vm.Tags["parentVM"];

            var lockCurrVm = true;
            int parentStackSize = -1;
            Stack<int> parentStack = null;
            destThread.pause(() => {

                ((Standard)destThread.GetLibs()["standard"]).Call(arguments, solvedArgs, ref destThread.ip);

                //wait for return
                parentStack = ((Standard)destThread.GetLibs()["standard"]).stack;
                parentStackSize = parentStack.Count;

                lockCurrVm = false;
                destThread.resume();
            });


            while ((lockCurrVm) || (parentStack.Count >= parentStackSize))
                Thread.Sleep(1);

            return null;
        }


        public object Sleep(List<DynamicValue> arguments, List <DynamicValue> solvedArgs, ref int nextIp)
        {
            int delay = solvedArgs[0].AsInt;

            Thread.Sleep(delay);

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

        private bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (!"0123456789-+,.".Contains(c))
                    return false;
            }

            return true;
        }



    }
}
