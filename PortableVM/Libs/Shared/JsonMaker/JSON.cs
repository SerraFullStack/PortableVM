using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JsonMaker
{
    public class JSON : IDisposable
    {
        private bool caseSensitiveToFind = true;
        private IJSONObject root;
        
        private IJSONObject modelObject;

        public void clear()
        {
            root.clear();
        }

        private void internalInitialize(IJSONObject _modelObject = null)
        {
            if (_modelObject == null)
                _modelObject = new InMemoryJsonObject();
            
            this.modelObject = _modelObject;
            
            root = (IJSONObject)Activator.CreateInstance(this.modelObject.GetType());
            
            root.Initialize(null, "", this.modelObject);
        }

        public JSON(bool caseSensitiveToFind = true, IJSONObject _modelObject = null)
        {
            this.caseSensitiveToFind = caseSensitiveToFind;
            this.internalInitialize(_modelObject);
        }
        public JSON(string JsonString, bool caseSensitiveToFind = true, IJSONObject _modelObject = null)
        {
            this.caseSensitiveToFind = caseSensitiveToFind;
            this.internalInitialize(_modelObject);
            this.parseJson(JsonString);
        }

        private IJSONObject find(string objectName, bool autoCreateTree, IJSONObject currentParent, SOType forceType = SOType.Undefined)
        {

            //quebra o nome em um array
            objectName = objectName.Replace("]", "").Replace("[", ".");
            string currentName = objectName;
            string childsNames = "";
            IJSONObject childOnParent;

            if (objectName.IndexOf('.') > -1)
            {
                currentName = objectName.Substring(0, objectName.IndexOf('.'));
                childsNames = objectName.Substring(objectName.IndexOf('.') + 1);
            }

            if (!(currentParent.__containsChild(currentName, this.caseSensitiveToFind)))
            {
                if (autoCreateTree)
                {
                    IJSONObject tempObj;
                    string currentParentRelativeName = currentParent.getRelativeName();
                    
                    /*if (currentParent is InMemoryJsonObject)
                        tempObj = new InMemoryJsonObject((InMemoryJsonObject)currentParent, currentParent.getRelativeName() + (currentParentRelativeName.Contains('.') ? "." : "") + currentName);
                    else
                        tempObj = new FileSystemJsonObject((FileSystemJsonObject)currentParent, currentParent.getRelativeName() + (currentParentRelativeName.Contains('.') ? "." : "") + currentName, (string)JsonObjectArguments);*/
                    tempObj = (IJSONObject)Activator.CreateInstance(currentParent.GetType());
                    tempObj.Initialize(currentParent, currentParent.getRelativeName(), this.modelObject);
                    
                    if (forceType != SOType.Undefined)
                        tempObj.forceType(forceType);

                    currentParent.setChild(currentName, tempObj);
                }
                else
                {
                    return null;
                }
            }


            childOnParent = currentParent.__getChild(currentName, this.caseSensitiveToFind);


            if (childsNames == "")
            {
                return childOnParent;
            }
            else
            {
                return this.find(childsNames, autoCreateTree, childOnParent);
            }
        }

        private void _set(string objectName, string value)
        {

            if (isAJson(value))
            {
                this.parseJson(value, objectName);
                return;
            }

            IJSONObject temp = this.find(objectName, true, this.root);

            /*if (value[0] == '\"')
                value = value.Substring(1, value.Length - 2);*/

            //value = value.Replace("\"", "\\\"");

            temp.setSingleValue(value);

        }

        private void del(IJSONObject node)
        {
            node.clear();
            var childs = node.__getChildsNames();
            foreach (var c in childs)
            {
                del(node.__getChild(c));
            }
            childs.Clear();

            /*
            var parentNodesNames = node.parent.__getChildsNames();
            for (int cont = 0; cont < parentNodesNames.Count; cont++)
            {
                if (parentNodesNames[cont] == node)
                {
                    //if parent is an array, pull the elements forward backwards
                    if (node.parent.isArray())
                    {
                        for (int cont2 = cont; cont2 < parentNodes.Count - 1; cont2++)
                            parentNodes[parentNodes.ElementAt(cont2).Key] = parentNodes[parentNodes.ElementAt(cont2 + 1).Key];

                        parentNodes.Remove(parentNodes.Last().Key);
                    }
                    else
                    {
                        parentNodes.Remove(parentNodes.ElementAt(cont).Key);
                    }
                    break;
                }
            }*/
        }

        Semaphore interfaceSemaphore = new Semaphore(1, 1);

        /// <summary>
        /// Removes an object from JSON three
        /// </summary>
        /// <param name="objectName">The object name</param>
        public void del(string objectName)
        {
            interfaceSemaphore.WaitOne();
            IJSONObject temp = this.find(objectName, false, this.root);
            if (temp != null)
                del(temp);

            interfaceSemaphore.Release();


        }

        public void clearChilds(string objectName)
        {
            IJSONObject temp = this.find(objectName, false, this.root);
            if (temp != null)
            {
                //var childs = temp.__getChilds();
                var names = temp.__getChildsNames();
                foreach (var c in names)
                {
                    del(temp.__getChild(c));
                }
            }



        }


        /// <summary>
        /// Insert a new json in current json three
        /// </summary>
        /// <param name="objectName">Name of the object</param>
        /// <param name="toImport">Json to be imported</param>
        public void set(string objectName, JSON toImport)
        {
            if (objectName != "")
                objectName = objectName + ":";
            this.parseJson(objectName + toImport.ToJson());

        }

        /// <summary>
        /// Serialize the Json three
        /// </summary>
        /// <param name="quotesOnNames">User '"' in name of objects</param>
        /// <returns></returns>
        public string ToJson(bool format = true)
        {
            interfaceSemaphore.WaitOne();
            string result = root.ToJson(true, format);
            interfaceSemaphore.Release();
            return result;
        }

        public override string ToString()
        {
            return this.ToJson();
        }

        /// <summary>
        /// Return true if the an object is in json three
        /// </summary>
        /// <param name="objectName">The object name</param>
        /// <returns></returns>
        public bool contains(string objectName)
        {
            interfaceSemaphore.WaitOne();
            bool result = this.find(objectName, false, this.root) != null;
            interfaceSemaphore.Release();
            return result;
        }

        /// <summary>
        /// returns the value of an json object as a json string (Serialize an object)
        /// </summary>
        /// <param name="objectName">The object name</param>
        /// <param name="quotesOnNames">User '"' in names</param>
        /// <returns></returns>
        public string get(string objectName, bool format = false, bool quotesOnNames = true, string valueOnNotFound = "undefined")
        {
            interfaceSemaphore.WaitOne();
            IJSONObject temp = this.find(objectName, false, this.root);
            interfaceSemaphore.Release();
            if (temp != null)
                return temp.ToJson(quotesOnNames, format);
            else
                return valueOnNotFound;

        }

        public IJSONObject getRaw(string objectName)
        {
            return this.find(objectName, false, this.root);
        }

        private List<string> getObjectsNames(IJSONObject currentItem = null)
        {
            List<string> retorno = new List<string>();

            if (currentItem == null)
                currentItem = this.root;


            string parentName = "";

            List<string> childsNames;

            var chieldsNames = currentItem.__getChildsNames();
            for (int cont = 0; cont < chieldsNames.Count; cont++)
            {

                childsNames = getObjectsNames(currentItem.__getChild(chieldsNames[cont]));


                parentName = chieldsNames[cont];
                //adiciona os filhos ao resultado
                //verifica se o nome atual atende ao filtro
                foreach (var att in childsNames)
                {


                    string nAtt = att;
                    if (nAtt != "")
                        nAtt = parentName + '.' + nAtt;

                    retorno.Add(nAtt);
                }
                retorno.Add(chieldsNames[cont]);
            }
            return retorno;

        }

        /// <summary>
        /// Return all names of the json three of an object
        /// </summary>
        /// <param name="objectName">The name of object</param>
        /// <returns></returns>
        public List<string> getObjectsNames(string objectName = "")
        {
            if (objectName == "")
            {
                IJSONObject nullo = null;
                return this.getObjectsNames(nullo);
            }
            else
            {
                var finded = this.find(objectName, false, this.root);
                List<string> retorno = new List<string>();
                if (finded != null)
                    retorno = this.getObjectsNames(finded);

                return retorno;
            }
        }

        private List<string> getChildsNames(IJSONObject currentItem = null)
        {
            List<string> retorno = new List<string>();

            if (currentItem == null)
                currentItem = this.root;

            var chieldsNames = currentItem.__getChildsNames();
            for (int cont = 0; cont < chieldsNames.Count; cont++)
            {
                retorno.Add(chieldsNames[cont]);
            }
            return retorno;
        }

        /// <summary>
        /// Return the childNames of an json object
        /// </summary>
        /// <param name="objectName">The name of object</param>
        /// <returns></returns>
        public List<string> getChildsNames(string objectName = "")
        {
            if (objectName == "")
            {
                IJSONObject nullo = null;
                return this.getChildsNames(nullo);
            }
            else
            {
                var finded = this.find(objectName, false, this.root);
                List<string> retorno = new List<string>();
                if (finded != null)
                    retorno = this.getChildsNames(finded);
                return retorno;
            }

        }

        #region json parser

        public void fromJson(string json, bool tryParseInvalidJson = false)
        {
            this.parseJson(json, "", tryParseInvalidJson, SOType.Undefined);
        }

        public void fromString(string json, bool tryParseInvalidJson = false)
        {
            this.parseJson(json, "", tryParseInvalidJson, SOType.Undefined);

        }

        /// <summary>
        /// Set or creates an property with an json string
        /// </summary>
        /// <param name="objectName">The json object name</param>
        /// <param name="value">The json string </param>
        public void set(string objectName, string value, SOType forceType = SOType.Undefined)
        {
            interfaceSemaphore.WaitOne();

            if (objectName != "")
            {
                if (!objectName.StartsWith("\""))
                    objectName = '"' + objectName + '"';
                objectName = "{" + objectName + ":" + value + "}";
                this.parseJson(objectName, "", false, forceType);
            }
            else
                this.parseJson(value, "", false, forceType);
            interfaceSemaphore.Release();

        }

        private enum ParseStates { findingStart, readingName, waitingKeyValueSep, findValueStart, prepareArray, readingContentString, readingContentNumber, readingContentSpecialWord }
        public void parseJson(string json, string parentName = "", bool tryParseInvalidJson = false, SOType forceType = SOType.Undefined)
        {
            var currentObject = this.root;
            if (parentName != "")
                currentObject = this.find(parentName, true, root);
            currentObject.name = parentName;

            ParseStates state = ParseStates.findValueStart;

            bool ignoreNextChar = false;
            StringBuilder currentStringContent = new StringBuilder();
            StringBuilder currentNumberContent = new StringBuilder();
            StringBuilder currentSpecialWordContent = new StringBuilder();
            StringBuilder currentChildName = new StringBuilder();

            int currLine = 1;
            int currCol = 1;

            int max = json.Length;
            char curr = ' ';

            for (int cont = 0; cont < max; cont++)
            {
                curr = json[cont];

                currCol++;
                if (curr == '\n')
                {
                    currCol = 1;
                    currLine++;
                }

                switch (state)
                {
                    case ParseStates.findingStart:
                        if (curr == '"')
                        {
                            if (currentObject.isArray())
                                state = ParseStates.prepareArray;
                            else
                                state = ParseStates.readingName;
                            currentChildName.Clear();
                        }
                        else if ((curr == ',')/* || (curr == '[') || (curr == '{')*/)
                        {
                            if (currentObject.isArray())
                                state = ParseStates.prepareArray;
                        }

                        else if ((curr == '}') || (curr == ']'))
                        {
                            if (parentName.Contains('.'))
                            {
                                parentName = parentName.Substring(0, parentName.LastIndexOf('.'));
                                if (currentObject != null)
                                    currentObject = currentObject.parent;
                            }
                            else
                            {
                                parentName = "";
                                currentObject = root;
                            }
                        }
                        break;
                    case ParseStates.readingName:
                        if (curr == '"')
                        {
                            state = ParseStates.waitingKeyValueSep;
                            currentObject = this.find(currentChildName.ToString(), true, currentObject, forceType);
                            currentObject.name = currentChildName.ToString();
                            parentName = parentName + (parentName != "" ? "." : "") + currentChildName;

                        }
                        else
                            currentChildName.Append(curr);
                        break;
                    case ParseStates.waitingKeyValueSep:
                        if (curr == ':')
                            state = ParseStates.findValueStart;
                        break;
                    case ParseStates.findValueStart:
                        if (curr == '"')
                        {
                            state = ParseStates.readingContentString;
                            currentStringContent.Clear();
                        }
                        else if (curr == '{')
                        {
                            state = ParseStates.findingStart;
                        }
                        else if (curr == '[')
                            state = ParseStates.prepareArray;
                        else if ("0123456789-+.".Contains(curr))
                        {
                            state = ParseStates.readingContentNumber;
                            currentNumberContent.Clear();
                            cont--;
                            currCol--;
                        }
                        else if ("untf".Contains(curr))
                        {
                            state = ParseStates.readingContentSpecialWord;
                            currentSpecialWordContent.Clear();
                            cont--;
                            currCol--;
                        }
                        else if (curr == ']')
                        {
                            //delete currenObject
                            var temp = currentObject;

                            if (parentName.Contains('.'))
                            {
                                parentName = parentName.Substring(0, parentName.LastIndexOf('.'));
                                currentObject = currentObject.parent;
                            }
                            else
                            {
                                parentName = "";
                                currentObject = root;
                            }

                            currentObject.delete(temp.name);

                            cont--;
                            currCol--;
                            state = ParseStates.findingStart;
                        }
                        else if (!" \t\r\n".Contains(curr))
                        {
                            if(!tryParseInvalidJson)
                                throw new Exception("SintaxError at line "+currLine + " and column "+currCol + ". Expected ' '(space), \t, \r or \n, but found "+curr+".");
                        }
                        break;

                    case ParseStates.prepareArray:
                        //state = "findingStart";
                        currentChildName.Clear();
                        currentChildName.Append(currentObject.__getChildsNames().Count.ToString());
                        currentObject = this.find(currentChildName.ToString(), true, currentObject, forceType);
                        currentObject.name = currentChildName.ToString();
                        parentName = parentName + (parentName != "" ? "." : "") + currentChildName;
                        state = ParseStates.findValueStart;
                        cont--;
                        currCol--;
                        break;
                    case ParseStates.readingContentString:
                        if (ignoreNextChar)
                        {
                            currentStringContent.Append(curr);
                            ignoreNextChar = false;
                        }
                        else if (curr == '\\')
                        {
                            ignoreNextChar = true;
                            currentStringContent.Append(curr);
                        }
                        else if (curr == '"')
                        {
                            currentObject.setSingleValue(currentStringContent.ToString());

                            //return to parent Object
                            if (parentName.Contains('.'))
                            {
                                parentName = parentName.Substring(0, parentName.LastIndexOf('.'));
                                currentObject = currentObject.parent;
                            }
                            else
                            {
                                parentName = "";
                                currentObject = root;
                            }
                            
                            state = ParseStates.findingStart;

                        }
                        else
                            currentStringContent.Append(curr);
                        break;
                    case ParseStates.readingContentNumber:
                        if ("0123456789.-+".Contains(curr))
                            currentNumberContent.Append(curr);
                        else
                        {
                            currentObject.setSingleValue(currentNumberContent.ToString());

                            //return to parent Object
                            if (parentName.Contains('.'))
                            {
                                parentName = parentName.Substring(0, parentName.LastIndexOf('.'));
                                currentObject = currentObject.parent;
                            }
                            else
                            {
                                parentName = "";
                                currentObject = root;
                            }

                            cont--;
                            state = ParseStates.findingStart;
                        }

                        break;
                    case ParseStates.readingContentSpecialWord:
                        if ("truefalseundefindednul".Contains(curr))
                            currentSpecialWordContent.Append(curr);
                        else
                        {
                            string strTemp = currentSpecialWordContent.ToString();
                            if ((strTemp == "true") ||
                                (strTemp == "false") ||
                                (strTemp == "null") ||
                                (strTemp == "undefined"))
                            {
                                currentObject.setSingleValue(strTemp);

                                //return to parent Object
                                if (parentName.Contains('.'))
                                {
                                    parentName = parentName.Substring(0, parentName.LastIndexOf('.'));
                                    currentObject = currentObject.parent;
                                }
                                else
                                {
                                    parentName = "";
                                    currentObject = root;
                                }

                                cont--;
                                state = ParseStates.findingStart;
                            }
                            else
                            {
                                if (!tryParseInvalidJson)
                                    throw new Exception("Invalid simbol at line " + currLine + " and column " + currCol + ": " + currentSpecialWordContent);
                            }
                        }

                        break;
                }
            }
        }

        private List<string> getJsonFields(string json)
        {
            int open = 0;
            List<string> fields = new List<string>();
            StringBuilder temp = new StringBuilder();
            bool quotes = false;

            bool skeepNext = false;

            for (int cont = 1; cont < json.Length - 1; cont++)
            {
                if (skeepNext)
                {
                    temp.Append(json[cont]);
                    skeepNext = false;
                    continue;
                }


                if (!quotes)
                {
                    if (json[cont] == ',')
                    {
                        if (open == 0)
                        {
                            fields.Add(temp.ToString());
                            temp.Clear();
                            continue;
                        }
                    }
                    else if ((json[cont] == '{') || (json[cont] == '['))
                        open++;
                    else if ((json[cont] == '}') || (json[cont] == ']'))
                        open--;
                }
                else
                {
                    if (json[cont] == '\\')
                    {
                        temp.Append(json[cont]);
                        skeepNext = true;
                        continue;
                    }
                }


                if (json[cont] == '"')
                {
                    //if ((json[cont - 1] != '\\') || (json[cont - 2] == '\\'))
                    quotes = !quotes;
                }

                // if ((quotes) || (temp.Length == 0) || (!"}]".Contains(temp[temp.Length - 1])))
                temp.Append(json[cont]);


            }
            if (temp.Length > 0)
                fields.Add(temp.ToString());

            return fields;

        }

        private string clearJsonString(string json)
        {
            StringBuilder result = new StringBuilder();

            bool quotes = false;
            char oldOldAtt = ' ';
            char oldAtt = ' ';
            bool skeepNext = false;
            foreach (char att in json)
            {
                if (skeepNext)
                {
                    result.Append(att);
                    skeepNext = false;
                    continue;
                }

                if (att == '\"')
                    quotes = !quotes;

                if (!quotes)
                {
                    if (!"\r\n\t\0 ".Contains(att))
                        result.Append(att);
                }
                else
                {
                    if (att == '\\')
                    {
                        skeepNext = true;
                    }

                    result.Append(att);
                }

                oldOldAtt = oldAtt;
                oldAtt = att;
            }

            try
            {
                //var result2 = __unescapeString(result.ToString());
                //return result2;
                return result.ToString();
            }
            catch
            {
                return json;
            }
        }

        private bool isAJson(string json, bool objects = true, bool arrays = true)
        {
            bool quotes = false;

            char oldAtt = (char)0;
            int cont = 0;
            json = json.TrimStart();

            foreach (char att in json)
            {
                cont++;
                if ((att == '\"') && (oldAtt != '\\'))
                    quotes = !quotes;

                if (!quotes)
                {
                    if (att == ':')
                        return true;
                    if ((objects) && ("{}".Contains(att)) && (cont == 0))
                        return true;
                    else if ((arrays) && ("[]".Contains(att) && (cont == 0)))
                        return true;
                }
                oldAtt = att;
            }

            return false;
        }

        #endregion

        public SOType getJSONType(string objectName)
        {
            interfaceSemaphore.WaitOne();
            IJSONObject temp = this.find(objectName, false, this.root);
            interfaceSemaphore.Release();
            if (temp != null)
            {
                return temp.getJSONType();
            }
            else
                return SOType.Undefined;
        }

        /// <summary>
        /// Get a json property as string
        /// </summary>
        /// <param name="name">Object name of the property</param>
        /// <param name="defaultValue">Value to be returned when the property is not found</param>
        /// <returns></returns>
        public string getString(string name, string defaultValue = "")
        {
            string result = this.get(name);
            if ((result.Length > 0) && (result[0] == '"'))
                result = result.Substring(1);
            if ((result.Length > 0) && (result[result.Length - 1] == '"'))
                result = result.Substring(0, result.Length - 1);

            result = __unescapeString(result);

            if ((result != "") && (result != "undefined"))
                return result;
            else
                return defaultValue;

        }

        /// <summary>
        /// Set or create a property as string
        /// </summary>
        /// <param name="name">The property object name </param>
        /// <param name="value">The value</param>
        public void setString(string name, string value, bool tryDefineType = false)
        {
            if (value == null)
                value = "";
            value = value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
            this.set(name, '"' + value + '"', tryDefineType ? SOType.Undefined : SOType.String);
        }

        /// <summary>
        /// Get a json property as int
        /// </summary>
        /// <param name="name">Object name of the property</param>
        /// <param name="defaultValue">Value to be returned when the property is not found</param>
        /// <returns></returns>
        public int getInt(string name, int defaultValue = 0)
        {
            string temp = getOnly(this.get(name), "0123456789-");
            if (temp != "")
                return int.Parse(temp);
            else return defaultValue;
        }

        /// <summary>
        /// Set or create a property as int
        /// </summary>
        /// <param name="name">The property object name </param>
        /// <param name="value">The value</param>
        public void setInt(string name, int value)
        {
            this.set(name, value.ToString());
        }

        /// <summary>
        /// Get a json property as Int64
        /// </summary>
        /// <param name="name">Object name of the property</param>
        /// <param name="defaultValue">Value to be returned when the property is not found</param>
        /// <returns></returns>
        public Int64 getInt64(string name, Int64 defaultValue = 0)
        {
            string temp = getOnly(this.get(name), "0123456789-");
            if (temp != "")
                return Int64.Parse(temp);
            else return defaultValue;
        }

        /// <summary>
        /// Set or create a property as int64
        /// </summary>
        /// <param name="name">The property object name </param>
        /// <param name="value">The value</param>
        public void setInt64(string name, Int64 value)
        {
            this.set(name, value.ToString());
        }

        /// <summary>
        /// Get a json property as boolean
        /// </summary>
        /// <param name="name">Object name of the property</param>
        /// <param name="defaultValue">Value to be returned when the property is not found</param>
        /// <returns></returns>
        public bool getBoolean(string name, bool defaultValue = false)
        {
            string temp = this.get(name);
            if (temp != "")
            {
                if (temp.ToLower() == "true")
                    return true;
                else
                    return false;
            }
            else return defaultValue;
        }

        /// <summary>
        /// Set or create a property as boolean
        /// </summary>
        /// <param name="name">The property object name </param>
        /// <param name="value">The value</param>
        public void setBoolean(string name, bool value)
        {
            this.set(name, value.ToString().ToLower());
        }

        /// <summary>
        /// Get a json property as DateTime
        /// </summary>
        /// <param name="name">Object name of the property</param>
        /// <param name="defaultValue">Value to be returned when the property is not found</param>
        /// <returns></returns>
        public DateTime getDateTime(string name, string format = "")
        {
            string temp = getOnly(this.get(name), "0123456789/-: TU");
            if (temp != "")
                if (format != "")
                    return DateTime.ParseExact(temp, format, System.Globalization.CultureInfo.InvariantCulture);
                else
                    return DateTime.Parse(temp);
            else
                return new DateTime(0);

        }

        /// <summary>
        /// Set or create a property as DateTime. To set a custom DateTime format, please use setString
        /// </summary>
        /// <param name="name">The property object name </param>
        /// <param name="value">The value</param>
        public void setDateTime(string name, DateTime value, string format = "", bool forceType = false)
        {
            string newV = "";
            if (format != "")
                newV = value.ToString(format);
            else
                newV = value.ToString();
            this.set(name, '"' + newV + '"', forceType ? SOType.DateTime : SOType.Undefined);
        }

        public void setDateTime_ISOFormat(string name, DateTime value, TimeSpan offset, bool forceType = false)
        {
            if (offset.Equals(TimeSpan.MinValue))
            {
                offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
            }
            string timeZone = offset.ToString();
            timeZone = timeZone.Remove(timeZone.LastIndexOf(':'));
            if (timeZone=="00:00")
                timeZone = "Z";
            else if (timeZone[0] != '-')
                timeZone = "+" + timeZone;

            this.setDateTime(name, value, "yyyy-MM-ddTHH:mm:ss" + timeZone, forceType);
        }

        public void setDateTime_ISOFormat(string name, DateTime value, bool forceType = false)
        {
            setDateTime_ISOFormat(name, value, TimeSpan.MinValue, forceType);
        }

        /// <summary>
        /// Get a json property as double. To get a custom DateTime, please use getString
        /// </summary>
        /// <param name="name">Object name of the property</param>
        /// <param name="defaultValue">Value to be returned when the property is not found</param>
        /// <returns></returns>
        public double getDouble(string name, double defaultValue = 0)
        {
            string temp = getOnly(this.get(name).Replace('.', ','), "0123456789-,");
            if (temp != null)
                return double.Parse(temp);
            else return defaultValue;
        }

        /// <summary>
        /// Set or create a property as Double
        /// </summary>
        /// <param name="name">The property object name </param>
        /// <param name="value">The value</param>
        public void setDouble(string name, double value)
        {
            if (double.IsNaN(value))
                value = 0f;
            else if (double.IsInfinity(value))
                value = 0f;
            this.set(name, value.ToString().Replace(',', '.'));
        }

        /// <summary>
        /// Return the childs count of an object (like arrays or objects)
        /// </summary>
        /// <param name="objectName">The name of the object</param>
        /// <returns></returns>
        public int getArrayLength(string objectName = "")
        {
            var finded = this.find(objectName, false, this.root);

            if (finded != null)
                return finded.__getChildsNames().Count();
            return 0;
        }

        private string getOnly(string text, string chars)
        {
            StringBuilder ret = new StringBuilder();
            foreach (var att in text)
                if (chars.Contains(att))
                    ret.Append(att); return ret.ToString();
            
        }

        public List<string> searchObjects(string searchBy, bool useContains = false)
        {
            List<string> result = new List<string>();

            var names = this.getObjectsNames("");
            names.Add("");
            string temp;
            foreach (var c in names)
            {
                if (c.Contains('.'))
                    temp = c.Substring(c.LastIndexOf('.')+1);
                else
                    temp = c;

                if ((temp.ToLower() == searchBy.ToLower()) || (useContains && temp.ToLower().Contains(searchBy.ToLower())))
                    result.Add(c);
            }

            return result;
        }

        public string searchFirst(string searchBy, bool useContains = false)
        {
            var names = searchObjects(searchBy, useContains);
            string ret = "";
            if (names.Count > 0)
                ret = names[0];
            names.Clear();
            return ret;
        }

        private string __unescapeString(string data)
        {
            //result = result.Replace("\\\\", "\\").Replace("\\\"", "\"").Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\t", "\t");
            StringBuilder nValue = new StringBuilder();
            int cont;
            for (cont = 0; cont < data.Length - 1; cont++)
            {
                if (data[cont] == '\\')
                {
                    if (data[cont + 1] == '\"')
                        nValue.Append('\"');
                    else if (data[cont + 1] == '\r')
                        nValue.Append('\r');
                    else if (data[cont + 1] == '\n')
                        nValue.Append('\n');
                    else if (data[cont + 1] == '\t')
                        nValue.Append('\t');
                    else if (data[cont + 1] == '\\')
                        nValue.Append('\\');
                    else
                        nValue.Append('?');

                    cont++;
                }
                else
                    nValue.Append(data[cont]);

                //cont++;


            }
            if (cont < data.Length)
                nValue.Append(data[cont]);

            return nValue.ToString();
        }

        public void Dispose()
        {
            this.clear();
        }

    }
}

