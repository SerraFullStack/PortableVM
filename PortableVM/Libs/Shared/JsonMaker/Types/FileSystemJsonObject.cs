using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonMaker
{
    class FileSystemJsonObject : IJSONObject
    {
        private IJSONObject modelObject;
        public string baseFolder = "";
        string fileName = "";
        string relativeName;

        public override void Initialize(IJSONObject pParent, string relativeName, IJSONObject modelObject)
        {
            this.modelObject = modelObject;
            this.baseFolder = ((FileSystemJsonObject)modelObject).baseFolder;
            this.baseFolder = this.baseFolder.Replace("\\", "/");

            if (this.baseFolder[this.baseFolder.Length - 1] == '/')
                this.baseFolder = this.baseFolder.Substring(0, this.baseFolder.Length - 1);

            this.fileName = this.baseFolder + relativeName.Replace(".", "/");

            if (fileName != "" && fileName[fileName.Length - 1] == '/')
                fileName = fileName.Substring(0, fileName.Length - 1);

            this.relativeName = relativeName;
        }
        public override void clear()
        {
            //as the system works with files, its does't invoke the "clear" of childs, just uses the Operating System to delete the children recursively

            /*var childsNames = this.__getChildsNames();
            foreach (var current in childsNames)
                __getChild(current).clear();*/

            if (Directory.Exists(this.fileName))
            {
                var directories = Directory.GetDirectories(this.fileName);
                foreach (var c in directories)
                    Directory.Delete(c, true);

                var files = Directory.GetFiles(this.fileName);
                foreach (var c in files)
                    File.Delete(c);


            }
            else if (File.Exists(this.fileName))
                File.Delete(this.fileName);
        }

        public override void delete(string name)
        {
            if (Directory.Exists(this.fileName))
            {
                name = this.fileName + '/' + name;

                if (File.Exists(name))
                    File.Delete(name);
                else if (Directory.Exists(name))
                    Directory.Delete(name, true);
            }
        }

        public override SOType getJSONType()
        {
            return __determineSoType(this.serializeSingleValue());


        }

        public override void setChild(string name, IJSONObject child)
        {
            //checks if exists a file
            if (File.Exists(this.fileName))
                File.Delete(this.fileName);

        }

        public override void setSingleValue(string value)
        {
            //if this is a object (with childs), remove all childs data
            if (Directory.Exists(this.fileName))
            {
                Directory.Delete(this.fileName, true);
            }

            if (!Directory.Exists(Path.GetDirectoryName(this.fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(this.fileName));
            }
            //write a file with the value
            File.WriteAllText(this.fileName, value);
        }

        public override bool __containsChild(string name, bool caseSensitive = false)
        {
            name = this.fileName + "/" + name;
            return Directory.Exists(name) || File.Exists(name);

        }

        public override List<string> __getChildsNames()
        {
            if (File.Exists(this.fileName))
            {
                return new List<string>();
            }
            else
            {
                List<string> result = Directory.GetDirectories(this.fileName).ToList();
                result.AddRange(Directory.GetFiles(this.fileName));

                //refactor childs names
                for (int cont = 0; cont < result.Count; cont++)
                    result[cont] = result[cont].Substring(this.baseFolder.Length + this.relativeName.Length + 1).Replace("\\", ".").Replace("/", ".");

                return result;
            }
        }

        public string __getBaseFolder()
        {
            return this.baseFolder;
        }

        protected override string serializeSingleValue()
        {
            if (File.Exists(this.fileName))
                return File.ReadAllText(this.fileName);
            else
                return "";
        }

        public override IJSONObject __getChild(string name, bool caseSensitive = true)
        {
            var ret = new FileSystemJsonObject();
            ret.Initialize(this, this.relativeName + "." + name, this.modelObject);
            return ret;
        }

        public override string getRelativeName()
        {
            return this.relativeName;
        }

        public override bool isDeletable()
        {
            return true;
        }
    }
}
