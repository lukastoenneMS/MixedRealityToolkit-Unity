// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Text;
using System.Reflection;

namespace Microsoft.MixedReality.Toolkit.Utilities.Json
{
    public class JsonBuilder
    {
        public string Build(object obj)
        {
            return AppendObject(obj);
        }

        private string AppendArray(Array array, MemberInfo member)
        {
            Type type = array.GetType();
            var builder = new StringBuilder();

            int count = 0;
            builder.Append("[");
            foreach (var item in array)
            {
                string result = AppendItem(item, null);
                if (result.Length > 0)
                {
                    if (count > 0)
                    {
                        builder.Append(",");
                    }

                    builder.Append(result);

                    ++count;
                }
            }
            builder.Append("]");

            var attr = member?.GetCustomAttribute<JSONArrayAttribute>();
            if (attr != null)
            {
                if (count < attr.MinItems)
                {
                    return "";
                }
            }
            return builder.ToString();
        }

        private string AppendObject(object obj)
        {
            Type type = obj.GetType();
            var fields = type.GetFields();
            var builder = new StringBuilder();

            int count = 0;
            builder.Append("{");
            foreach (var field in fields)
            {
                if (field.IsStatic || field.IsNotSerialized || !field.FieldType.IsSerializable)
                {
                    continue;
                }

                var result = AppendItem(field.GetValue(obj), field);
                if (result.Length > 0)
                {
                    if (count > 0)
                    {
                        builder.Append(",");
                    }

                    builder.Append("\"" + SanitizeString(field.Name) + "\":" + result);

                    ++count;
                }
            }
            builder.Append("}");

            if (count == 0)
            {
                return "";
            }
            return builder.ToString();
        }

        private string AppendItem(object obj, MemberInfo member)
        {
            if (obj == null)
            {
                return "";
            }

            Type type = obj.GetType();
            if (IsString(type))
            {
                return "\"" + obj.ToString() + "\"";
            }
            else if (type.IsEnum)
            {
                var attr = member?.GetCustomAttribute<JSONEnumAttribute>();
                if (attr != null)
                {
                    if (attr.IgnoreValues != null)
                    {
                        foreach (var ignoreValue in attr.IgnoreValues)
                        {
                            if ((int)obj == (int)ignoreValue)
                            {
                                return "";
                            }
                        }
                    }
                    if (attr.UseIntValue)
                    {
                        return ((int)obj).ToString();
                    }
                }

                return "\"" + obj.ToString() + "\"";
            }
            else if (IsInteger(type))
            {
                var attr = member?.GetCustomAttribute<JSONIntegerAttribute>();
                if (attr != null)
                {
                    if ((int)obj < attr.Minimum)
                    {
                        return "";
                    }
                }

                return obj.ToString();
            }
            else if (IsFloat(type))
            {
                return obj.ToString();
            }
            else if (IsBoolean(type))
            {
                return obj.ToString().ToLower();
            }
            else if (type.IsArray)
            {
                return AppendArray(obj as Array, member);
            }
            else
            {
                return AppendObject(obj);
            }
        }

        private static string SanitizeString(string s)
        {
            return s.Replace("\"", "\\\n");
        }

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
    }
}