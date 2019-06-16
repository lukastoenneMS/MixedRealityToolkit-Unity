// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Reflection;

namespace Microsoft.MixedReality.Toolkit.Utilities.Json
{
    /// <summary>
    /// Details of converting enums to JSON values.
    /// </summary>
    [System.Serializable]
    [AttributeUsage(AttributeTargets.All)]
    public class JSONEnumAttribute : System.Attribute
    {
        private bool useIntValue;
        public bool UseIntValue => useIntValue;

        public JSONEnumAttribute(bool useIntValue)
        {
            this.useIntValue = useIntValue;
        }
    }

    /// <summary>
    /// Attribute for JSON integer schema details.
    /// </summary>
    [System.Serializable]
    [AttributeUsage(AttributeTargets.Field)]
    public class JSONIntegerAttribute : System.Attribute
    {
        private int minimum;
        public int Minimum => minimum;

        public JSONIntegerAttribute(int minimum)
        {
            this.minimum = minimum;
        }
    }

    /// <summary>
    /// Attribute for JSON array schema details.
    /// </summary>
    [System.Serializable]
    [AttributeUsage(AttributeTargets.Field)]
    public class JSONArrayAttribute : System.Attribute
    {
        private int minItems;
        public int MinItems => minItems;

        public JSONArrayAttribute(int minItems)
        {
            this.minItems = minItems;
        }
    }
}