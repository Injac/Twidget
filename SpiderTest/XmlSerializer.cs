using System;
using System.Collections;
using System.Ext.Xml;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace System.Xml.Serialization
{
    public class XmlSerializer
    {
        private readonly Type typeToSerialize;

        public XmlSerializer(Type type)
        {
            this.typeToSerialize = type;
        }

        public void Serialize(Stream stream, object instance)
        {
            using (XmlWriter xmlWriter = XmlWriter.Create(stream))
            {
                xmlWriter.WriteRaw("<?xml version=\"1.0\"?>");
                xmlWriter.WriteStartElement(this.typeToSerialize.Name);

                foreach (FieldInfo fieldInfo in this.typeToSerialize.GetFields())
                {
                    Type fieldType = fieldInfo.FieldType;

                    if (fieldType.IsEnum)
                    {
                        SerializeEnum(instance, xmlWriter, fieldInfo);
                        continue;
                    }

                    if (fieldType.IsArray)
                    {
                        SerializeArray(instance, xmlWriter, fieldInfo);
                        continue;
                    }

                    if (fieldType.IsClass && fieldType.Name != "String")
                    {
                        // TODO: what about embedded classes? recursive call??
                        continue;
                    }

                    if (!fieldType.IsValueType && fieldType.Name != "String")
                    {
                        // TODO: throw or raise event for unsupported type
                        continue;
                    }

                    object fieldValue = fieldInfo.GetValue(instance);

                    if (fieldValue != null)
                    {
                        switch (fieldType.Name)
                        {
                            case "Boolean":
                                // these need to be lowercase
                                xmlWriter.WriteElementString(fieldInfo.Name, fieldValue.ToString().ToLower());
                                break;

                            case "Char":
                                xmlWriter.WriteElementString(fieldInfo.Name, Encoding.UTF8.GetBytes(fieldValue.ToString())[0].ToString());
                                break;

                            case "DateTime":
                                xmlWriter.WriteElementString(fieldInfo.Name, ((DateTime)fieldValue).ToString("s"));
                                break;

                            default:
                                // TODO: structs fall through to here
                                xmlWriter.WriteElementString(fieldInfo.Name, fieldValue.ToString());
                                break;
                        }
                    }
                }

                xmlWriter.WriteEndElement();
            }
        }

        private static void SerializeEnum(object instance, XmlWriter xmlWriter, FieldInfo fieldInfo)
        {
            // TODO: desktop .NET serializes enums with their .ToString() value ("Two") in the case below
            // NETMF does not have the ability to parse an enum and only serializes the base value (1) in the case below
        }

        private static void SerializeArray(object instance, XmlWriter xmlWriter, FieldInfo fieldInfo)
        {
            object array = fieldInfo.GetValue(instance);
            string typeName = array.GetType().GetElementType().Name;

            switch (typeName)
            {
                case "Boolean":
                    typeName = "boolean";
                    break;

                case "Byte":
                    // TODO: this is not an array but a base64 encoded string but have not figured out how to decode it
                    //xmlWriter.WriteElementString(fieldInfo.Name, Convert.ToBase64String((byte[])array));
                    return;

                case "SByte":
                    typeName = "byte";
                    break;

                case "Char":
                    typeName = "char";
                    break;

                case "DateTime":
                    typeName = "dateTime";
                    break;

                case "Double":
                    typeName = "double";
                    break;

                case "Guid":
                    typeName = "guid";
                    break;

                case "Int16":
                    typeName = "short";
                    break;

                case "UInt16":
                    typeName = "unsignedShort";
                    break;

                case "Int32":
                    typeName = "int";
                    break;

                case "UInt32":
                    typeName = "unsignedInt";
                    break;

                case "Int64":
                    typeName = "long";
                    break;

                case "UInt64":
                    typeName = "unsignedLong";
                    break;

                case "Single":
                    typeName = "float";
                    break;

                case "String":
                    typeName = "string";
                    break;
            }

            xmlWriter.WriteStartElement(fieldInfo.Name);

            foreach (var item in (IEnumerable)array)
            {
                switch (typeName)
                {
                    case "boolean":
                        // these need to be lowercase
                        xmlWriter.WriteElementString(typeName, item.ToString().ToLower());
                        break;

                    case "char":
                        xmlWriter.WriteElementString(typeName, Encoding.UTF8.GetBytes(item.ToString())[0].ToString());
                        break;

                    case "dateTime":
                        xmlWriter.WriteElementString(typeName, ((DateTime)item).ToString("s"));
                        break;

                    default:
                        xmlWriter.WriteElementString(typeName, item.ToString());
                        break;
                }
            }

            xmlWriter.WriteEndElement();
        }

        public object Deserialize(Stream stream)
        {
            object instance = this.typeToSerialize.GetConstructor(new Type[0]).Invoke(null);

            using (XmlReader xmlReader = XmlReader.Create(stream, new XmlReaderSettings()
            {
                IgnoreComments = true, IgnoreWhitespace = true
            }))
            {
                while (xmlReader.Read())
                {
                    if (xmlReader.NodeType == XmlNodeType.Element)
                    {
                        foreach (FieldInfo fieldInfo in this.typeToSerialize.GetFields())
                        {
                            if (xmlReader.Name == fieldInfo.Name)
                            {
                                Type fieldType = fieldInfo.FieldType;

                                if (fieldType.IsEnum)
                                {
                                    fieldInfo.SetValue(instance, DeserializeEnum(xmlReader));
                                    continue;
                                }

                                if (fieldType.IsArray)
                                {
                                    fieldInfo.SetValue(instance, DeserializeArray(xmlReader));
                                    continue;
                                }

                                if (fieldType.IsClass && fieldType.Name != "String")
                                {
                                    // TODO: what about embedded classes? recursive call??
                                    continue;
                                }

                                if (!fieldType.IsValueType && fieldType.Name != "String")
                                {
                                    // TODO: throw or raise event for unsupported type
                                    continue;
                                }

                                string tempValue = xmlReader.ReadElementString(fieldInfo.Name);

                                switch (fieldType.Name)
                                {
                                    case "Boolean":
                                        fieldInfo.SetValue(instance, tempValue == "true");
                                        break;

                                    case "Byte":
                                        fieldInfo.SetValue(instance, Convert.ToByte(tempValue));
                                        break;

                                    case "SByte":
                                        fieldInfo.SetValue(instance, Convert.ToSByte(tempValue));
                                        break;

                                    case "Char":
                                        fieldInfo.SetValue(instance, Convert.ToChar(Convert.ToByte(tempValue)));
                                        break;

                                    case "DateTime":
                                        fieldInfo.SetValue(instance, GetDateTimeFromString(tempValue));
                                        break;

                                    case "Double":
                                        fieldInfo.SetValue(instance, Convert.ToDouble(tempValue));
                                        break;

                                    case "Guid":
                                        fieldInfo.SetValue(instance, GetGuidFromString(tempValue));
                                        break;

                                    case "Int16":
                                        fieldInfo.SetValue(instance, Convert.ToInt16(tempValue));
                                        break;

                                    case "UInt16":
                                        fieldInfo.SetValue(instance, Convert.ToUInt16(tempValue));
                                        break;

                                    case "Int32":
                                        fieldInfo.SetValue(instance, Convert.ToInt32(tempValue));
                                        break;

                                    case "UInt32":
                                        fieldInfo.SetValue(instance, Convert.ToUInt32(tempValue));
                                        break;

                                    case "Int64":
                                        fieldInfo.SetValue(instance, Convert.ToInt64(tempValue));
                                        break;

                                    case "UInt64":
                                        fieldInfo.SetValue(instance, Convert.ToUInt64(tempValue));
                                        break;

                                    case "Single":
                                        fieldInfo.SetValue(instance, (Single)Convert.ToDouble(tempValue));
                                        break;

                                    case "String":
                                        fieldInfo.SetValue(instance, tempValue);
                                        break;

                                    default:
                                        break;
                                }

                                continue;
                            }
                        }
                    }
                }
            }

            return instance;
        }

        private static object DeserializeEnum(XmlReader xmlReader)
        {
            // TODO: desktop .NET serializes enums with their .ToString() value ("Two") in the case below
            // NETMF does not have the ability to parse an enum and only serializes the base value (1) in the case below
            return null;
        }

        private static object DeserializeArray(XmlReader xmlReader)
        {
            string startingElementName = xmlReader.Name;
            xmlReader.Read();
            string arrayType = xmlReader.Name;

            ArrayList array = new ArrayList();
            Type returnType = null;

            while (xmlReader.Name != startingElementName)
            {
                string tempValue = xmlReader.ReadElementString();

                switch (arrayType)
                {
                    case "boolean":
                        returnType = returnType ?? typeof(bool);
                        array.Add(tempValue == "true");
                        break;

                    // TODO: this is not an array but a base64 encoded string
                    //case "byte":
                    //    //returnType = returnType ?? typeof(bool);
                    //    //return Convert.FromBase64String(tempValue);
                    //    //throw new Exception("Should never reach this");
                    //    break;

                    case "byte":
                        returnType = returnType ?? typeof(sbyte);
                        array.Add(Convert.ToSByte(tempValue));
                        break;

                    case "char":
                        returnType = returnType ?? typeof(char);
                        array.Add(Convert.ToChar(Convert.ToByte(tempValue)));
                        break;

                    case "dateTime":
                        returnType = returnType ?? typeof(DateTime);
                        array.Add(GetDateTimeFromString(tempValue));
                        break;

                    case "double":
                        returnType = returnType ?? typeof(double);
                        array.Add(Convert.ToDouble(tempValue));
                        break;

                    case "guid":
                        returnType = returnType ?? typeof(Guid);
                        array.Add(GetGuidFromString(tempValue));
                        break;

                    case "short":
                        returnType = returnType ?? typeof(short);
                        array.Add(Convert.ToInt16(tempValue));
                        break;

                    case "unsignedShort":
                        returnType = returnType ?? typeof(ushort);
                        array.Add(Convert.ToUInt16(tempValue));
                        break;

                    case "int":
                        returnType = returnType ?? typeof(int);
                        array.Add(Convert.ToInt32(tempValue));
                        break;

                    case "unsignedInt":
                        returnType = returnType ?? typeof(uint);
                        array.Add(Convert.ToUInt32(tempValue));
                        break;

                    case "long":
                        returnType = returnType ?? typeof(long);
                        array.Add(Convert.ToInt64(tempValue));
                        break;

                    case "unsignedLong":
                        returnType = returnType ?? typeof(ulong);
                        array.Add(Convert.ToUInt64(tempValue));
                        break;

                    case "float":
                        returnType = returnType ?? typeof(float);
                        array.Add((Single)Convert.ToDouble(tempValue));
                        break;

                    case "string":
                        returnType = returnType ?? typeof(string);
                        array.Add(tempValue);
                        break;

                    default:
                        returnType = returnType ?? typeof(object);
                        break;
                }
            }

            return array.ToArray(returnType);
        }

        private static object GetGuidFromString(string tempValue)
        {
            byte[] guidBytes = new byte[16];
            string[] split = tempValue.Split('-');
            int location = 0;

            for (int i = 0; i < split.Length; i++)
            {
                byte[] tempArray = HexToBytes(split[i]);

                // TODO: is this needed or will it always need to be reversed
                //bool temp = Microsoft.SPOT.Hardware.SystemInfo.IsBigEndian;

                if (i < 3)
                {
                    int end = tempArray.Length - 1;

                    for (int start = 0; start < end; start++)
                    {
                        byte b = tempArray[start];
                        tempArray[start] = tempArray[end];
                        tempArray[end] = b;
                        end--;
                    }
                }

                Array.Copy(tempArray, 0, guidBytes, location, tempArray.Length);
                location += split[i].Length / 2;
            }

            return new Guid(guidBytes);
        }

        private static byte[] HexToBytes(string hexString)
        {
            // Based on http://stackoverflow.com/a/3974535
            if (hexString.Length == 0 || hexString.Length % 2 != 0)
                return new byte[0];

            byte[] buffer = new byte[hexString.Length / 2];
            char c;

            for (int bx = 0, sx = 0; bx < buffer.Length; ++bx, ++sx)
            {
                // Convert first half of byte
                c = hexString[sx];
                byte b = (byte)((c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0')) << 4);

                // Convert second half of byte
                c = hexString[++sx];
                b |= (byte)(c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0'));
                buffer[bx] = b;
            }

            return buffer;
        }

        private static DateTime GetDateTimeFromString(string tempValue)
        {
            int year;
            int month;
            int day;
            int hour;
            int minute;
            int second;
            int millisecond;

            year = int.Parse(tempValue.Substring(0, 4));
            month = int.Parse(tempValue.Substring(5, 2));
            day = int.Parse(tempValue.Substring(8, 2));
            hour = int.Parse(tempValue.Substring(11, 2));
            minute = int.Parse(tempValue.Substring(14, 2));
            second = int.Parse(tempValue.Substring(17, 2));
            // NETMF serializes out to this
            //2012-06-27T20:02:40
            // .NET does this
            //2012-06-27T20:36:57.995-07:00
            //                     ^ ^^^^^^
            //                     |     |
            //           milliseconds   timezone (-7 from GMT) (sometimes)
            //millisecond = tempValue.Length == 19 ? 0 : int.Parse(tempValue.Substring(20, 2));
            millisecond = 0;

            return new DateTime(year, month, day, hour, minute, second, millisecond);
        }
    }
}
