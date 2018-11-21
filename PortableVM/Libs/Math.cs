/*
 * Created by SharpDevelop.
 * User: rafael.tonello
 * Date: 21/11/2018
 * Time: 09:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace PortableVM.Libs
{
    /// <summary>
    /// Description of Int.
    /// </summary>
    public class Math: Lib
    {
        public Math()
        {
            Standard.autoParseCSharpLib(this, this.instructions);
        }
        
        public object Sum(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            return solvedArgs[0].AsDouble + solvedArgs[1].AsDouble;
        }
        
        public object Sub(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            return solvedArgs[0].AsDouble - solvedArgs[1].AsDouble;
        }
        
        public object Div(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            return solvedArgs[0].AsDouble / solvedArgs[1].AsDouble;
        }
        
        public object Mul(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            return solvedArgs[0].AsDouble * solvedArgs[1].AsDouble;
        }
    }
}
