using JsonMaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortableVM.Libs
{
    class Json : Lib
    {
        public Dictionary<string, JSON> instances = new Dictionary<string, JSON>();
        public Json()
        {
            Standard.autoParseCSharpLib(this, this.instructions);
        }

        //create ClassName variableName constructor_arg1 constructor_ar2
        public object Create(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            string objectId = (string)((Standard)vm.GetLibs()["standard"]).GetNewId(arguments, solvedArgs, ref nextIp);
            this.instances[objectId] = new JSON();
            //store the object id in the sugested variable (argument 1)
            if (arguments.Count > 0)
            {
                vm.SetVar(arguments[0].AsString, new DynamicValue(objectId), vm.rootContext);

                //store the level of object (in context)

                //add reference to this as first constructor argument
                arguments[0].AsString = objectId;
                solvedArgs[0].AsString = objectId;
            }

            return objectId;
        }

        public object SetProperty(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            //get the object identification
            string objectId = solvedArgs[0].AsString;
            string property = solvedArgs[1].AsString;
            string value = solvedArgs[2].AsString;

            this.instances[objectId].set(property, value);

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
            string property = solvedArgs[1].AsString;

            string value = this.instances[objectId].getString(property);


            return value;
        }

        public object Get(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            return this.GetProperty(arguments, solvedArgs, ref nextIp);
        }


        public object DeleteProperty(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            //get the object identification
            string objectId = solvedArgs[0].AsString;
            string property = solvedArgs[1].AsString;

            this.instances[objectId].del(property);

            return null;
        }

        public object GetChildsNames(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            //get the object identification
            string objectId = solvedArgs[0].AsString;
            string property = solvedArgs[1].AsString;

            var childNames = this.instances[objectId].getChildsNames(property);

            //create new Array
            string arrayId = (string)((Array)vm.GetLibs()["array"]).Create(new List<DynamicValue>(), new List<DynamicValue>(), ref nextIp);

            //create array data (each position will contains a childName)
            foreach (var c in childNames)
            {
                var addArgs = (new DynamicValue[] { new DynamicValue(arrayId), new DynamicValue(c) }).ToList();
                ((Array)vm.GetLibs()["array"]).Add(
                    addArgs,
                    addArgs, 
                    ref nextIp);
            }

            return arrayId;
        }

        public object Serialize(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            string objectId = solvedArgs[0].AsString;
            return this.instances[objectId].ToJson();

        }

        public object Deserialize(List<DynamicValue> arguments, List<DynamicValue> solvedArgs, ref int nextIp)
        {
            string objectId = "";
            if (solvedArgs.Count > 1)
                objectId = solvedArgs[0].AsString;
            else
                objectId = (string)this.Create(new List<DynamicValue>(), new List<DynamicValue>(), ref nextIp);

            this.instances[objectId].parseJson(solvedArgs[1].AsString);

            return objectId;
        }
    }
}
