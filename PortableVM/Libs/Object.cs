/*
 * Created by SharpDevelop.
 * User: rafael.tonello
 * Date: 22/11/2018
 * Time: 09:19
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace PortableVM.Libs
{
    /// <summary>
    /// Description of Object.
    /// </summary>
    public class Object: Lib
    {
        public Object()
        {
            Standard.autoParseCSharpLib(this, this.instructions);
        }
        
        //create ClassName variableName constructor_arg1 constructor_ar2
        public object Create(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            //add some empty variables to garant that argument and solveArgs will contains two firsts values and prevent 
            //long codes to treat args missing
            arguments.Add(new DynamicValue(""));
            arguments.Add(new DynamicValue(""));
            solvedArgs.Add(new DynamicValue(""));
            solvedArgs.Add(new DynamicValue(""));


            string className = arguments[0].AsString;
            string objectId = (string)((Standard)vm.GetLibs()["standard"]).GetNewId(arguments, solvedArgs, ref nextIp);

            //store the object id in the sugested variable (argument 1)
            if (arguments[1].AsString != "")
                vm.SetVar(arguments[1].AsString, new DynamicValue(objectId));

            //store metadata about object
            //store the object className
            vm.SetVar(objectId + ".className", new DynamicValue(className), vm.rootContext);
                
            //prepare the constructor arguments (reuse the arguments and solvedArgs)
                
            //add reference to this as first constructor argument
            arguments[0].AsString = objectId;
            solvedArgs[0].AsString = objectId;
                
                
            //checks if exists a named code called className + ".constructor" or (classname + "." + className)
            if (vm.namedCodePointers.ContainsKey(className + ".constructor"))
            {
                //replace seconds value of arguments with the named code indentification
                arguments[1].AsString = "constructor";
                solvedArgs[1].AsString = "constructor";
                    
                //Call the "call" of this class
                this.Call(arguments, solvedArgs, ref nextIp);
            }
            else if (vm.namedCodePointers.ContainsKey(className + "."+className))
            {
                //replace seconds value of arguments with the named code indentification
                arguments[1].AsString = className;
                solvedArgs[1].AsString = className;
                    
                //Call the "call" of this class
                this.Call(arguments, solvedArgs, ref nextIp);
            }
                
            return objectId;
        }
        
        public object SetProperty(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            //get the object identification
            string objectId = solvedArgs[0].AsString;
            
            //set a property named objectId + "." + arguments[0] (use the same context level of object)
            vm.SetVar(objectId + "." + arguments[1].AsString, solvedArgs[2], vm.rootContext);
            
            return null;
        }

        public object Set(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            return this.SetProperty(arguments, solvedArgs, ref nextIp);

        }

        public object GetProperty(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            //get the object identification
            string objectId = solvedArgs[0].AsString;
            
            //set a property named objectId + "." + arguments[0] (use the same context level of object)
            return vm.GetVar(objectId + "." + arguments[1].AsString, new DynamicValue(null), vm.rootContext);
            
        }

        public object Get(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            return this.GetProperty(arguments, solvedArgs, ref nextIp);
        }

        public object DeleteProperty(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            //get the object identification
            string objectId = solvedArgs[0].AsString;
            
            //set a property named objectId + "." + arguments[0] (use the same context level of object)
            vm.DelVar(objectId + "." + arguments[1].AsString, vm.rootContext);
            
            return null;
        }
        
        public object Call(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            //get the object identification
            string objectId = solvedArgs[0].AsString;
            string methodName = solvedArgs[1].AsString;

            //get the object className
            string className = vm.GetVar(objectId + ".className", new DynamicValue("")).AsString;


            //parape parameters to run "standard.call" instruction
            {
                //determine the real namedCode (class name + namedCode) to be sented to instruction call
                string NamedCodeToCall = className + "." + methodName;
                solvedArgs[0].AsString = NamedCodeToCall;
                arguments[0].AsString = NamedCodeToCall;

                //Turn parameter 1 to an pointer to object (to make second argument of call a pointer to 
                //object. With this, user can use keyword like 'this' or 'self' to refer to the object)
                solvedArgs[1].AsString = objectId;
                arguments[1].AsString = objectId;
            }
                
            return ((Standard)vm.GetLibs()["standard"]).Call(arguments, solvedArgs, ref nextIp);
        }
        
        public object Serialize(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            return null;
            
        }
        
        public object Deserialize(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            return null;
        }
        
        public object Run(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            string objectId = solvedArgs[0].AsString;
            //in all arguments, replace the "this" keyword by current objectId
            
            for (int count = 0; count < arguments.Count; count++)
                arguments[count].AsString = arguments[count].AsString.Replace("this", objectId).Replace("self", objectId);
            
            //create a new instruction
            Instruction nI = new Instruction();
            
            //create a new Instruction object to contains the instructin information
            nI.instruction = arguments[0].AsString.ToLower();
            nI.lib = "standard";
                                                
            if (nI.instruction.Contains("."))
            {
                nI.lib = nI.instruction.Substring(0, nI.instruction.IndexOf('.'));
                nI.instruction = nI.instruction.Substring(nI.instruction.IndexOf('.') + 1);
            }
            nI.arguments = arguments;
            nI.arguments.RemoveAt(0);
            
            //run the instruction in the vm
            vm.RunInstruction(nI, ref nextIp);
            
            return vm.GetVar("_return", new DynamicValue(null));
        }
        
    }
}
