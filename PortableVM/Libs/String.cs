/*
 * Created by SharpDevelop.
 * User: rafael.tonello
 * Date: 22/11/2018
 * Time: 15:42
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace PortableVM.Libs
{
    /// <summary>
    /// Description of Strings.
    /// </summary>
    public class String:Lib
    {
        public String()
        {
            Standard.autoParseCSharpLib(this, this.instructions);
        }
        
        public object Compose(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            string temp = "";
            foreach (var c in solvedArgs)
                temp += c.AsString;
            
            return temp;
        }
        
        public object Substring(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            string data = solvedArgs[0].AsString;
            int start = solvedArgs[1].AsInt;
            int count = solvedArgs.Count > 2 ? solvedArgs[2].AsInt : -1;
            
            if (count > -1)
                data = data.Substring(start, count);
            else
                data = data.Substring(start);
            
            return data;
        }
        
        public object IndexOf(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            string source = solvedArgs[0].AsString;
            string search = solvedArgs[1].AsString;
            bool caseSensitive = solvedArgs.Count > 2 ? solvedArgs[2].AsBool : false;
            
            if (caseSensitive)
            {
                source = source.ToLower();
                search = search.ToLower();
            }
            
            return source.IndexOf(search);
        }
        
        public object LastIndexOf(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            string source = solvedArgs[0].AsString;
            string search = solvedArgs[1].AsString;
            bool caseSensitive = solvedArgs.Count > 2 ? solvedArgs[2].AsBool : false;
            
            if (caseSensitive)
            {
                source = source.ToLower();
                search = search.ToLower();
            }
            
            return source.LastIndexOf(search);
        }
        
        public object Replace(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            string source = solvedArgs[0].AsString;
            string replace = solvedArgs[1].AsString;
            string By = solvedArgs[2].AsString;
            
            return source.Replace(replace, By);
        }
        
        public object Split(List<DynamicValue> arguments, List<DynamicValue> solvedArgs,ref int nextIp)
        {
            return null;
        }
        
        
        
    }
}
