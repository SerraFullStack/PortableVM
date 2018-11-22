using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortableVM
{
    //default registers
    public abstract class Consts
    {
        public const string _RESULT = "_return";
    }
    
    public delegate object OnUnknownInstruction(VM sender, string instruction, List<DynamicValue> rawArguments, List<DynamicValue> solvedArgs, ref int nextIp, out bool allowContinue);
    
    public class VM
    {
        //this event will be executed when an instruction is not recognized by the VM. This can be used
        //to extendends the instruction set with function in the application.
        public event OnUnknownInstruction onUnknownInstruction;
        
        //Contains the processed code
        public List<Instruction> code = new List<Instruction>();
        
        //Contains a list of labels (named codes) and theis positions in the 'code' List. This is used by
        //the Standart.goto instruction to locate positions of labels
        public Dictionary<string, List<NamedCodeRef>> namedCodePointers = new Dictionary<string, List<NamedCodeRef>>();
        
        //contains a list of library pointers. When a new library is developed, you must to add this to
        //StartLibs method of this class.
        private Dictionary<string, Lib> libs = new Dictionary<string, Lib>();
        
        //the vars memory contains a list of memory tables. Each "call" instruction is called, a new memory table is
        //added to the VarsMemory to create a contextual variables. This allow local variables.
        public Stack<Dictionary<string, DynamicValue>> VarsMemory = new Stack<Dictionary<string, DynamicValue>>();
        
        //must contains extra class properties.
        public Dictionary<string, object> Tags = new Dictionary<string, object>();
        
        public VM(OnUnknownInstruction onUnknownInstruction = null)
        {
            if (onUnknownInstruction != null)
                this.onUnknownInstruction = onUnknownInstruction;

            startLibs();
        }
        
        public VM(string filename, OnUnknownInstruction onUnknownInstruction = null)
        {
            if (onUnknownInstruction != null)
                this.onUnknownInstruction = onUnknownInstruction;
            
            startLibs();
            string[] lines = File.ReadAllLines(filename);
            this.addCode(lines.ToList());
            this.Run();
        }

        public VM(string[] lines, OnUnknownInstruction onUnknownInstruction = null)
        {
            if (onUnknownInstruction != null)
                this.onUnknownInstruction = onUnknownInstruction;
            
            startLibs();
            this.addCode(lines.ToList());
            this.Run();
        }

        private void startLibs()
        {
            this.libs["standard"] = new Libs.Standard() { vm = this };
            this.libs["math"] = new Libs.Math() { vm = this };
        }

        public void addCode(List<string> lines)
        {
            List<string> currLine;
            Instruction currInstruction;
            
            //scrools through the lines
            foreach (var curr in lines)
            {
                //split the line by ' ' (space)
                currLine = Split(curr.TrimStart().TrimEnd().Trim(), ' ');
                if (currLine.Count > 0)
                {
                    //checks if current line is a named code (label)
                    if ((currLine[0].ToLower() == "_nc_") || (currLine[0].ToLower() == ":"))
                    {
                        //extract the name of code
                        string nc = currLine[1].ToLower();
                        
                        //create a namedCodeRef to contains the named code information
                        NamedCodeRef temp = new NamedCodeRef();
                        temp.name = nc;
                        temp.address = code.Count();
                        
                        //parameters can be used with named code. To do this, programmers just add the parameters names after
                        //the code name (Ex.: _nc_ myLabel parameter1 parameter2). The code bellow casts the parameters passed to
                        //"goto" instruction to these parameters names
                        for (int ca = 2; ca < currLine.Count; ca++)
                            temp.argumentsNames.Add(currLine[ca]);
                        
                        if (!namedCodePointers.ContainsKey(nc))
                            namedCodePointers[nc] = new List<NamedCodeRef>();
                        
                        namedCodePointers[nc].Add(temp);
                        
                        
                    }
                    //checks if the line is a comment or just strutural code
                    else if ((currLine[0] != "") && (!@"#/\;".Contains(currLine[0][0])))
                    {
                        //create a new Instruction object to contains the instructin information
                        currInstruction = new Instruction();
                        currInstruction.instruction = currLine[0].ToLower();
                        //set the default library as "standart" and check if the programmer was informed the library. In true case,
                        //just identify and replace the libraryName.
                        currInstruction.lib = "standard";
                        if (currInstruction.instruction.Contains("."))
                        {
                            currInstruction.lib = currInstruction.instruction.Substring(0, currInstruction.instruction.IndexOf('.'));
                            currInstruction.instruction = currInstruction.instruction.Substring(currInstruction.instruction.IndexOf('.') + 1);
                        }
                        
                        //take the instruction arguments
                        for (int cont = 1; cont < currLine.Count; cont++)
                            currInstruction.arguments.Add(new DynamicValue(currLine[cont]));

                        //added the parsed instruction to "code" list.
                        code.Add(currInstruction);
                    }
                }
            }
        }
        
        private List<string> Split(string data, char splitBy)
        {
            List<string> result = new List<string>();
            bool opened = false;
            string currData = "";
            char oldCur = ' ';
            foreach (var curr in data)
            {
                if (opened)
                {
                    if ((curr != '"') || (oldCur == '\\'))
                    {
                        if ((curr == '\\') && (oldCur != '\\'))
                        {
                            oldCur = curr;
                            continue;
                        }
                        
                        currData += curr;
                    }
                    else
                        opened = false;
                }
                else
                {
                    
                    if ((curr != '"') || (oldCur == '\\'))
                    {
                        if ((curr == '\\') && (oldCur != '\\'))
                        {
                            oldCur = curr;
                            continue;
                        }
                        
                        if (curr != splitBy)
                        {
                            currData += curr;
                        }
                        else
                        {
                            result.Add(currData);
                            currData = "";
                        }
                    }
                    else
                        opened = true;
                }

                oldCur = curr;
            }

            if (currData != "")
            {
                result.Add(currData);
            }

            return result;

        }

        //current instruction pointer
        public int ip = 0;
        
        //contains the amount of runned instructions
        public UInt64 totalRunnedInstructions = 0;
        
        
        public void Run(bool waitEnd = false)
        {
            bool ended = false;
            Thread th = new Thread(delegate ()
           {
               int nextIp;
               this.totalRunnedInstructions = 0;
               while (ip < code.Count)
               {
                   this.totalRunnedInstructions++;
                   nextIp = ip + 1;

                   if (RunInstruction(code[ip], ref nextIp))
                   {
                       ip = nextIp;
                   }
                   else
                   {
                       throw new Exception("The instruction "+code[ip].lib+"."+code[ip].instruction+" could not be executed");
                       break;
                   }
               }
               ended = true;
           });

            th.Start();
            
            while (waitEnd && !ended)
            {
                Thread.Sleep(1);
            }
        }


        public Dictionary<string, Lib>GetLibs(){return this.libs; }
        
        /// <summary>
        /// Run an instruction
        /// </summary>
        /// <param name="instruction">The instruction to be executed</param>
        /// <param name="nextIp">The reference to a variable that will receive a next value to Instruction Pointer. This parameter exists why some instruction may change the current position of execution</param>
        /// <returns></returns>
        private bool RunInstruction(Instruction instruction, ref int nextIp)
        {

            object result = null;
            bool ok = false;
            //try to solve instruction arguments values
            List<DynamicValue> solvedArgs = new List<DynamicValue>();
            solvedArgs.Clear();
            foreach (var c in instruction.arguments)
            {
                solvedArgs.Add(this.GetVar(c.AsString, new DynamicValue(c.AsString)));
            }
            
            
            //try to identify the libray
            if (libs.ContainsKey(instruction.lib))
            {
                //try to identify the instruction
                if (libs[instruction.lib].instructions.ContainsKey(instruction.instruction.ToLower()))
                {
                    //run the instruction
                    result = libs[instruction.lib].instructions[instruction.instruction.ToLower()](instruction.arguments, solvedArgs, ref nextIp);
                    ok =  true;
                }
                else
                    //try run the instruction in the application
                    result = this.InvokeOnUnknownFunction(this, instruction.lib + "." + instruction.instruction, instruction.arguments, solvedArgs, ref nextIp, out ok);
            }
            else
                //try run the instruction in the application
                result = this.InvokeOnUnknownFunction(this, instruction.lib + "." + instruction.instruction, instruction.arguments, solvedArgs, ref nextIp, out ok);
            
            //checks if instruction was returned some value. If true, put this value in _return global variable
            if (result != null)
            {
                if (result is DynamicValue)
                    SetVar(Consts._RESULT, (DynamicValue)result);
                else
                    SetVar(Consts._RESULT, new DynamicValue(result));
            }
            
            return ok;
        }

        public object InvokeOnUnknownFunction(VM sender, string instruction, List<DynamicValue> rawArguments, List<DynamicValue> solvedArgs, ref int nextIp, out bool allowContinue)
        {
            if (this.onUnknownInstruction != null)
                return onUnknownInstruction.Invoke(this, instruction, rawArguments, solvedArgs, ref nextIp, out allowContinue);
            
            allowContinue = false;
            return null;
        }
        
        
        public void SetVar(string varName, DynamicValue value)
        {
            varName = varName.ToLower();
            if (VarsMemory.Count == 0)
                VarsMemory.Push(new Dictionary<string, DynamicValue>());
            
            if (varName[0] == '_')
            {
                //uses the root memory table
                VarsMemory.ElementAt(0)[varName] = value;
            }
            else
            {;
                //add the variable to last memoryTable
                VarsMemory.Last()[varName] = value;
            }
        }

        public DynamicValue GetVar(string varName, DynamicValue def = null)
        {
            varName = varName.ToLower();
            
            if (varName.StartsWith("\""))
            {
                string result = varName.Substring(1);
                if (result.EndsWith("\""))
                    result = result.Substring(0, result.Length-1);
                
                result = result.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\b", " ");
                
                return new DynamicValue(result);
            }
            
            if (VarsMemory.Count == 0)
                return def;
            
            if (varName[0] == '_')
            {
                //uses the root memory table
                if (VarsMemory.ElementAt(0).ContainsKey(varName))
                    return VarsMemory.ElementAt(0)[varName];
                else
                    return def;
            }
            else
            {
                int currVarsMemoryPos = VarsMemory.Count-1;
                while (currVarsMemoryPos >= 0)
                {
                    //loking by variable in all memory tables (in descent order)
                    if (VarsMemory.ElementAt(currVarsMemoryPos).ContainsKey(varName))
                        return VarsMemory.ElementAt(currVarsMemoryPos)[varName];
                    
                    currVarsMemoryPos--;
                }
                
                return def;
            }
            
        }
        
        
        
    }

    public class Instruction
    {
        public string lib;
        public string instruction;
        public List<DynamicValue> arguments = new List<DynamicValue>();

    }

    public class DynamicValue
    {
        private string _value = "";
        public DynamicValue(object value)
        {
            this.set(value);
        }

        public void set(object value)
        {
            if (!(value is string))
                this._value = value.ToString();
            else
                this._value = (string)value;
        }

        public object get()
        {
            return _value;
        }

        public string AsString {
            get { return _value; }
            set { _value = value; }
        }

        public int AsInt
        {
            get { return int.Parse(_value); }
            set { _value = value.ToString(); }
        }

        public double AsDouble
        {
            get { return double.Parse(_value); }
            set { _value = value.ToString(); }
        }

        public bool AsBool
        {
            get { return "1true".Contains(_value.ToLower()); }
            set { _value = value.ToString(); }
        }


    }
    
    
    public class NamedCodeRef
    {
        public string name;
        public int address;
        public List<string> argumentsNames = new List<string>();
        
    }
}
