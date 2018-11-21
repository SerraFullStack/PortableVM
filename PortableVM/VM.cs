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
    
    
    public class VM
    {
        public event LibInstruction onUnknownInstruction;
        public List<Instruction> code = new List<Instruction>();
        public Dictionary<string, List<int>> namedCodePointers = new Dictionary<string, List<int>>();
        public VM(LibInstruction onUnknownInstruction = null)
        {
            if (onUnknownInstruction != null)
                this.onUnknownInstruction = onUnknownInstruction;

            startLibs();            
        }
        
        public VM(string filename, LibInstruction onUnknownInstruction = null)
        {
            if (onUnknownInstruction != null)
                this.onUnknownInstruction = onUnknownInstruction;
            
            startLibs();
            string[] lines = File.ReadAllLines(filename);
            this.addCode(lines.ToList());
            this.Run();
        }

        public VM(string[] lines, LibInstruction onUnknownInstruction = null)
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
            foreach (var curr in lines)
            {
                currLine = Split(curr.TrimStart().TrimEnd().Trim(), ' ');
                if (currLine.Count > 0)
                {
                    if ((currLine[0].ToLower() == "_nc_") || (currLine[0].ToLower() == ":"))
                    {
                        string nc = currLine[1].ToLower();
                        if (!namedCodePointers.ContainsKey(nc))
                            namedCodePointers[nc] = new List<int>();
                        
                        namedCodePointers[nc].Add(code.Count);
                            
                        
                    }
                    else if ((currLine[0] != "") && (!@"#/\;".Contains(currLine[0][0])))
                    {
                        currInstruction = new Instruction();
                        currInstruction.instruction = currLine[0].ToLower();
                        currInstruction.lib = "standard";
                        if (currInstruction.instruction.Contains("."))
                        {
                            currInstruction.lib = currInstruction.instruction.Substring(0, currInstruction.instruction.IndexOf('.'));
                            currInstruction.instruction = currInstruction.instruction.Substring(currInstruction.instruction.IndexOf('.') + 1);
                        }
                    
                        for (int cont = 1; cont < currLine.Count; cont++)
                            currInstruction.arguments.Add(new DynamicValue(currLine[cont]));

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
                    if ((curr != '"') || (oldCur != '\\'))
                    {
                        currData += curr;
                    }
                    else
                        opened = false;
                }
                else
                {
                    if ((curr != '"') || (oldCur != '\\'))
                    {
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
                result.Add(currData);

            return result;

        }

        public int ip = 0;
        public int totalRunnedInstructions = 0;
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


        private Dictionary<string, Lib> libs = new Dictionary<string, Lib>();
        
        public Dictionary<string, Lib>GetLibs(){return this.libs; }
        
        public Dictionary<string, DynamicValue> variables = new Dictionary<string, DynamicValue>();
        private bool RunInstruction(Instruction instruction, ref int nextIp)
        {

            object result = null;
            bool ok = false;
            //resolve variables
            List<DynamicValue> solvedArgs = new List<DynamicValue>();
            solvedArgs.Clear();
            foreach (var c in instruction.arguments)
            {
                solvedArgs.Add(this.GetVar(c.AsString, new DynamicValue(c.AsString)));
            }
            
            if (libs.ContainsKey(instruction.lib))
            {
                if (libs[instruction.lib].instructions.ContainsKey(instruction.instruction.ToLower()))
                {
                    result = libs[instruction.lib].instructions[instruction.instruction.ToLower()](instruction.arguments, solvedArgs, ref nextIp);
                    
                    ok =  true;
                }
                else if (this.onUnknownInstruction != null)
                {
                    result = onUnknownInstruction.Invoke(instruction.arguments, solvedArgs, ref nextIp);
                    ok = true;
                }
            }
            else if (this.onUnknownInstruction != null)
            {
                result = onUnknownInstruction.Invoke(instruction.arguments, solvedArgs, ref nextIp);
                ok = true;
            }
            //try send current instruction back to Application
            
            if (result != null)
            {
                if (result is DynamicValue)
                    SetVar(Consts._RESULT, (DynamicValue)result);
                else
                    SetVar(Consts._RESULT, new DynamicValue(result));
            }
            return ok;
        }

        public void SetVar(string varName, DynamicValue value)
        {
            varName = varName.ToLower();
            variables[varName] = new DynamicValue(value.get());
        }

        public DynamicValue GetVar(string varName, DynamicValue def = null)
        {
            if (varName.StartsWith("\""))
            {
                string result = varName.Substring(1);
                if (result.EndsWith("\""))
                    result = result.Substring(0, result.Length-1);
                
                result = result.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\b", " ");
                
                return new DynamicValue(result);
            }
                 
            varName = varName.ToLower();
            if (variables.ContainsKey(varName))
                return variables[varName];
            else
                return def;
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
}
