// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities.ShapeMatching
{
    [Serializable]
    internal class SerializablePoseConfiguration
    {
        public Vector3[] targets;
        public float[] weights;
        public string[] identifiers;
    }

    public static class PoseSerializationUtils
    {
        public static void Serialize(string filepath, PoseConfiguration config)
        {
            Serialize(filepath, config, null);
        }

        public static void Serialize(string filepath, PoseConfiguration config, string[] identifiers)
        {
            Debug.Assert(identifiers == null || identifiers.Length == config.Length);

            var serializable = new SerializablePoseConfiguration();
            serializable.targets = config.Targets;
            serializable.weights = config.Weights;
            serializable.identifiers = identifiers;

            using (StreamWriter writer = File.CreateText(filepath))
            {
                string json = JsonUtility.ToJson(serializable, true);
                writer.Write(json);
            }
        }

        public static PoseConfiguration Deserialize(string filepath)
        {
            return Deserialize(filepath, out string[] identifiers);
        }

        public static PoseConfiguration Deserialize(string filepath, out string[] identifiers)
        {
            string json = File.ReadAllText(filepath);

            var serializable = JsonUtility.FromJson<SerializablePoseConfiguration>(json);

            var result = new PoseConfiguration(serializable.targets, serializable.weights);
            identifiers = serializable.identifiers;
            return result;
        }
    }
}