/*
 * Created by SharpDevelop.
 * User: rafael.tonello
 * Date: 26/11/2018
 * Time: 09:08
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace PortableVM.Libs
{
    /// <summary>
    /// Description of Array.
    /// </summary>
    public class Array: Lib
    {
        public Array()
        {
            Standard.autoParseCSharpLib(this, this.instructions);
        }
        
        public object Create(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            //add some empty variables to garant that argument and solveArgs will contains two firsts values and prevent 
            //long codes to treat args missing
            arguments.Add(new DynamicValue(""));
            arguments.Add(new DynamicValue(""));
            solvedArgs.Add(new DynamicValue(""));
            solvedArgs.Add(new DynamicValue(""));

            List<DynamicValue> objectCreateArgs = new List<DynamicValue>();
            objectCreateArgs.Add(new DynamicValue("")); //no className

            if (arguments.Count > 0)
                objectCreateArgs.Add(new DynamicValue(arguments[0].AsString)); //array variable name
            

            //create the object
            string objectId = (string)((Libs.Object)vm.GetLibs()["object"]).Create(objectCreateArgs, objectCreateArgs, ref nextIp);
            
            //create the "length" property with value '0'
            List<DynamicValue> propArgs = new List<DynamicValue>{
                new DynamicValue(objectId),
                new DynamicValue("length"),
                new DynamicValue(0)
            };
            ((Libs.Object)vm.GetLibs()["object"]).SetProperty(propArgs, propArgs, ref nextIp);
            
                
            return objectId;
        }
        
        public object GetLength(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            string objectRef = solvedArgs[0].AsString;
            
            //get the "length" property
            List<DynamicValue> propArgs = new List<DynamicValue>{
                new DynamicValue(objectRef),
                new DynamicValue("length")
            };
            var length = (DynamicValue)((Libs.Object)vm.GetLibs()["object"]).GetProperty(propArgs, propArgs, ref nextIp);
            
            return length;
        }
        
        public object GetAt(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            string objectRef = solvedArgs[0].AsString;
            int index = solvedArgs[1].AsInt;
            
            //get the "length" property
            var length = ((DynamicValue)this.GetLength(arguments, solvedArgs, ref nextIp)).AsInt;
            
            if (index < length)
            {
                var propArgs = new List<DynamicValue>{
                    new DynamicValue(objectRef),
                    new DynamicValue(index)
                };
                var result = (DynamicValue)((Libs.Object)vm.GetLibs()["object"]).GetProperty(propArgs, propArgs, ref nextIp);
                if (result._value != null)
                    return result;
                else
                    return "";
            }
            else
            {
                return "";
            }
        }
        
        public object SetAt(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            string objectRef = solvedArgs[0].AsString;
            int index = solvedArgs[1].AsInt;
            var value = solvedArgs[2];
            
            //get the "length" property
            var length = ((DynamicValue)this.GetLength(arguments, solvedArgs, ref nextIp)).AsInt;
            
            if (index >= length)
            {
                //update array length
                var propArgs = new List<DynamicValue>{
                    new DynamicValue(objectRef),
                    new DynamicValue("length"),
                    new DynamicValue(index+1)
                };
                ((Libs.Object)vm.GetLibs()["object"]).SetProperty(propArgs, propArgs, ref nextIp);
                
            }

            return null;
        }
        
        public object RemoveAt(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            string objectRef = solvedArgs[0].AsString;
            int index = solvedArgs[1].AsInt;
            
            var length = ((DynamicValue)this.GetLength(arguments, solvedArgs, ref nextIp)).AsInt;
            
            for (int cont = index; cont < length-1; cont++)
            {
                //get next position data
                var propArgs = new List<DynamicValue>{
                    new DynamicValue(objectRef),
                    new DynamicValue(cont+1)
                };
                
                var curr = (DynamicValue)this.GetAt(propArgs, propArgs, ref nextIp);
                
                //set current position with data of next position
                propArgs = new List<DynamicValue>{
                    new DynamicValue(objectRef),
                    new DynamicValue(cont),
                    curr
                };
                this.SetAt(propArgs, propArgs, ref nextIp);
            }
            
            //delete the last element
            var propArgs2 = new List<DynamicValue>{
                new DynamicValue(objectRef),
                new DynamicValue(length -1)
            };
            ((Libs.Object)vm.GetLibs()["object"]).DeleteProperty(new List<DynamicValue> {
                   new DynamicValue(objectRef),
                   new DynamicValue(length-1)
            }, new List<DynamicValue> {
                   new DynamicValue(objectRef),
                   new DynamicValue(length-1)
            }, ref nextIp);

            return null;
            
            
            
        }
        
        public object Add(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            string objectRef = solvedArgs[0].AsString;
            var value = solvedArgs[1];
            
            //get the "length" property
            var length = ((DynamicValue)this.GetLength(arguments, solvedArgs, ref nextIp)).AsInt;
            
            var propArgs = new List<DynamicValue>{
                new DynamicValue(objectRef),
                new DynamicValue(length),
                value
            };
            ((Libs.Object)vm.GetLibs()["object"]).SetProperty(propArgs, propArgs, ref nextIp);


            //increase length property
            propArgs = new List<DynamicValue>{
                new DynamicValue(objectRef),
                new DynamicValue("length"),
                new DynamicValue(length+1)
            };
            ((Libs.Object)vm.GetLibs()["object"]).SetProperty(propArgs, propArgs, ref nextIp);

            return null;
        }
        
        
    }
}
