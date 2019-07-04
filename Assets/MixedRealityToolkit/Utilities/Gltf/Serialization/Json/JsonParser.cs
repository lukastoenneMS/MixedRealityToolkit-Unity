// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities.Json
{
    /// <summary>
    /// JSON utility class for structured deserialization.
    /// </summary>
    /// <remarks>
    /// This class takes optional [JSON attributes](xref:Microsoft.MixedReality.Toolkit.Utilities.Json.JsonAttribute)
    /// into account to produce valid JSON according to some schema.
    /// The [Unity JsonUtility class](https://docs.unity3d.com/Manual/JSONSerialization.html) is not flexible enough to
    /// follow all aspects of a schema, e.g. minimum values or how to serialize an enum.
    /// </remarks>
    public class JsonParser
    {
        /// <summary>
        /// Deserialize the given object from a JSON string.
        /// </summary>
        public bool Parse(string json, Type type, out object obj)
        {
            return ParseObject(ref json, type, out obj);
        }

        const int maxLogLength = 120;

        private void LogError(string error)
        {
            Debug.LogError("Invalid JSON string:" + error);
        }

        private void LogMissingObject(string json, Type expected)
        {
            LogError($"Expected object of type {expected.Name} at {json.Substring(0, maxLogLength)}");
        }

        private void LogMissingLiteral(string json, string expected)
        {
            LogError($"Expected \"{expected}\" at {json.Substring(0, maxLogLength)}");
        }

        /// Append contents of the object to the JSON string.
        private bool ParseObject(ref string json, Type type, out object obj)
        {
            obj = Activator.CreateInstance(type);

            bool isDictionary = IsDictionary(type);
            IDictionary dict = null;
            Type[] argTypes = null;
            if (isDictionary)
            {
                dict = obj as IDictionary;
                argTypes = type.GetGenericArguments();
            }

            if (!ParseLiteral(ref json, "{"))
            {
                LogMissingLiteral(json, "{");
                return false;
            }
            if (ParseLiteral(ref json, "}"))
            {
                // Empty list
                return true;
            }

            while (true)
            {
                if (isDictionary)
                {
                    if (!ParseItem(ref json, argTypes[0], null, out object key))
                    {
                        LogMissingObject(json, argTypes[0]);
                        return false;
                    }
                    if (!ParseLiteral(ref json, ":"))
                    {
                        LogMissingLiteral(json, ":");
                        return false;
                    }
                    if (!ParseItem(ref json, argTypes[1], null, out object value))
                    {
                        LogMissingObject(json, argTypes[1]);
                        return false;
                    }

                    dict.Add(key, value);
                }
                else
                {
                    if (!ParseString(ref json, out string fieldName))
                    {
                        LogMissingObject(json, typeof(string));
                        return false;
                    }
                    FieldInfo fieldInfo = type.GetField(fieldName);
                    if (fieldInfo == null)
                    {
                        LogError($"Could not find field \"{fieldName}\" in {type.Name}");
                        return false;
                    }

                    if (!ParseLiteral(ref json, ":"))
                    {
                        LogMissingLiteral(json, ":");
                        return false;
                    }

                    if (!ParseItem(ref json, fieldInfo.FieldType, fieldInfo, out object value))
                    {
                        LogMissingObject(json, fieldInfo.FieldType);
                        return false;
                    }

                    fieldInfo.SetValue(obj, value);
                }

                if (!ParseLiteral(ref json, ","))
                {
                    break;
                }
            }

            if (!ParseLiteral(ref json, "}"))
            {
                LogMissingLiteral(json, "}");
                return false;
            }

            return true;
        }


        private bool ParseArray(ref string json, Type elementType, MemberInfo member, out object result)
        {
            bool ok = ParseArray(ref json, elementType, member, out Array array);
            result = array;
            return ok;
        }

        /// Append the contents of an array to the JSON string.
        private bool ParseArray(ref string json, Type elementType, MemberInfo member, out Array array)
        {
            array = Array.CreateInstance(elementType, 0);

            if (!ParseLiteral(ref json, "["))
            {
                LogMissingLiteral(json, "[");
                return false;
            }
            if (ParseLiteral(ref json, "]"))
            {
                // Empty list
                return true;
            }

            List<object> tmp = new List<object>();
            while (true)
            {
                if (!ParseItem(ref json, elementType, null, out object element))
                {
                    LogMissingObject(json, elementType);
                    return false;
                }

                tmp.Add(element);

                if (!ParseLiteral(ref json, ","))
                {
                    break;
                }
            }

            if (!ParseLiteral(ref json, "]"))
            {
                LogMissingLiteral(json, "]");
                return false;
            }

            array = Array.CreateInstance(elementType, tmp.Count);
            for (int i = 0; i < tmp.Count; ++i)
            {
                array.SetValue(tmp[i], i);
            }

            return true;
        }

        /// Parse the value of a field or array item.
        private bool ParseItem(ref string json, Type type, MemberInfo member, out object item)
        {
            item = null;
            if (IsString(type))
            {
                return ParseString(ref json, out item);
            }
            else if (type.IsEnum)
            {
                var attr = member?.GetCustomAttribute<JSONEnumAttribute>();
                if (attr != null)
                {
                    if (attr.UseIntValue)
                    {
                        return ParseEnumByValue(ref json, type, out item);
                    }
                }
                return ParseEnumByName(ref json, type, out item);
            }
            else if (IsInteger(type))
            {
                return ParseInt(ref json, out item);
            }
            else if (IsFloat(type))
            {
                return ParseFloat(ref json, out item);
            }
            else if (IsBoolean(type))
            {
                return ParseBool(ref json, out item);
            }
            else if (type.IsArray)
            {
                return ParseArray(ref json, type.GetElementType(), member, out item);
            }
            else
            {
                return ParseObject(ref json, type, out item);
            }
        }

        private static readonly char[] SpecialChars = new char[] { '{', '}', '[', ']', ',', ':' };

        private static bool ParseLiteral(ref string json, string literal)
        {
            string t = json.TrimStart();
            if (t.StartsWith(literal))
            {
                json = t.Substring(literal.Length);
                return true;
            }
            return false;
        }

        private static bool ParseString(ref string json, out object result)
        {
            bool ok = ParseString(ref json, out string s);
            result = s;
            return ok;
        }

        private static bool ParseString(ref string json, out string result)
        {
            int end = json.IndexOfAny(SpecialChars);

            var match = Regex.Match(json.Substring(0, end), @"\s*""(([^""\\]|\\.)*)""\s*");
            if (match.Success)
            {
                result = match.Groups[1].Value;
                json = json.Substring(end);
                return true;
            }

            // string t = json;
            // int quot1 = t.IndexOf('"');
            // if (quot1 >= 0)
            // {
            //     t = t.Substring(quot1 + 1);
            //     int quot2 = t.IndexOf('"');
            //     if (quot2 >= 0)
            //     {
            //         result = t.Substring(0, quot2);
            //         return true;
            //     }
            // }

            result = "";
            return false;
        }

        private static bool ParseInt(ref string json, out object result)
        {
            bool ok = ParseInt(ref json, out int i);
            result = i;
            return ok;
        }

        private static bool ParseInt(ref string json, out int result)
        {
            int end = json.IndexOfAny(SpecialChars);
            if (int.TryParse(json.Substring(0, end), out result))
            {
                json = json.Substring(end);
                return true;
            }
            return false;
        }

        private static bool ParseFloat(ref string json, out object result)
        {
            bool ok = ParseFloat(ref json, out float f);
            result = f;
            return ok;
        }

        private static bool ParseFloat(ref string json, out float result)
        {
            int end = json.IndexOfAny(SpecialChars);
            if (float.TryParse(json.Substring(0, end), out result))
            {
                json = json.Substring(end);
                return true;
            }
            return false;
        }

        private static bool ParseBool(ref string json, out object result)
        {
            bool ok = ParseBool(ref json, out bool b);
            result = b;
            return ok;
        }

        private static bool ParseBool(ref string json, out bool result)
        {
            int end = json.IndexOfAny(SpecialChars);
            if (bool.TryParse(json.Substring(0, end), out result))
            {
                json = json.Substring(end);
                return true;
            }
            return false;
        }

        private static bool ParseEnumByValue(ref string json, Type type, out object result)
        {
            int end = json.IndexOfAny(SpecialChars);
            result = Enum.Parse(type, json.Substring(0, end));
            if (result != null)
            {
                json = json.Substring(end);
                return true;
            }
            return false;
        }

        private static bool ParseEnumByName(ref string json, Type type, out object result)
        {
            if (ParseString(ref json, out string name))
            {
                result = Enum.Parse(type, name);
                if (result != null)
                {
                    return true;
                }
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Returns true if a type is an integer type.
        /// </summary>
        public static bool IsInteger(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns true if a type is an floating point type.
        /// </summary>
        public static bool IsFloat(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns true if a type is an string type.
        /// </summary>
        public static bool IsString(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns true if a type is an boolean type.
        /// </summary>
        public static bool IsBoolean(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsDictionary(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }
    }
}