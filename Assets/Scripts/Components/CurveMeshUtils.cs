// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public static class CurveMeshUtils
    {
        private static Vector3 xAxis = new Vector3(1, 0, 0);
        private static Vector3 yAxis = new Vector3(0, 1, 0);
        private static Vector3 dirAxis = new Vector3(0, 0, 1);

        public static void GenerateCurveMesh(Mesh mesh, SplineCurve curve, int resolution, float thickness)
        {
            Vector3[] circle = GetCircle(resolution);
            int count = curve.Count;
            var CPs = curve.ControlPoints;

            if (count == 0)
            {
                mesh.Clear();
                return;
            }

            Vector3[] vertices = new Vector3[count * resolution];
            Vector3[] normals = new Vector3[count * resolution];
            int[] triangles = new int[(count - 1) * resolution * 6];

            int v = 0;
            Vector3 dir = dirAxis;
            Vector3 halfDir = dirAxis;
            Quaternion rotation = Quaternion.identity;
            for (int i = 0; i < count; ++i)
            {
                Vector3 pos = CPs[i].position;

                Quaternion deltaRot;
                if (i < count - 1)
                {
                    Vector3 nextPos = CPs[i+1].position;
                    Vector3 nextDir = nextPos - pos;
                    if (i == 0)
                    {
                        deltaRot = Quaternion.FromToRotation(dirAxis, nextDir);
                        halfDir = nextDir;
                    }
                    else
                    {
                        Vector3 nextHalfDir = dir + nextDir;
                        deltaRot = Quaternion.FromToRotation(halfDir, nextHalfDir);
                        halfDir = nextHalfDir;
                    }

                    dir = nextDir;
                }
                else
                {
                    deltaRot = Quaternion.FromToRotation(halfDir, dir);
                }
                rotation = deltaRot * rotation;

                for (int j = 0; j < resolution; ++j)
                {
                    vertices[v] = (rotation * circle[j]) * thickness + pos;
                    normals[v] = rotation * circle[j];

                    ++v;
                }
            }

            int t = 0;
            for (int i = 0; i < count - 1; ++i)
            {
                int di0 = i * resolution;
                int di1 = (i + 1) * resolution;
                for (int j = 0; j < resolution; ++j)
                {
                    int dj0 = j;
                    int dj1 = j < resolution - 1 ? j + 1 : 0;
                    triangles[t + 0] = di0 + dj0;
                    triangles[t + 1] = di1 + dj0;
                    triangles[t + 2] = di1 + dj1;
                    triangles[t + 3] = di1 + dj1;
                    triangles[t + 4] = di0 + dj1;
                    triangles[t + 5] = di0 + dj0;

                    t += 6;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();
        }

        public static void GenerateLineShapeMesh(Mesh mesh, LineShape shape, int resolution, float thickness)
        {
            Vector3[] circle = GetCircle(resolution);
            var lines = shape.Lines;
            int count = lines.Count;

            if (count == 0)
            {
                mesh.Clear();
                return;
            }

            Vector3[] vertices = new Vector3[2 * count * resolution];
            Vector3[] normals = new Vector3[2 * count * resolution];
            int[] triangles = new int[count * resolution * 6];

            int v = 0;
            for (int i = 0; i < count; ++i)
            {
                Vector3 start = lines[i].start;
                Vector3 end = lines[i].end;
                Quaternion rotation = Quaternion.FromToRotation(dirAxis, end - start);

                for (int j = 0; j < resolution; ++j)
                {
                    vertices[v] = (rotation * circle[j]) * thickness + start;
                    normals[v] = rotation * circle[j];
                    ++v;
                }
                for (int j = 0; j < resolution; ++j)
                {
                    vertices[v] = (rotation * circle[j]) * thickness + end;
                    normals[v] = rotation * circle[j];
                    ++v;
                }
            }

            int t = 0;
            for (int i = 0; i < count; ++i)
            {
                int di0 = (2 * i) * resolution;
                int di1 = (2 * i + 1) * resolution;
                for (int j = 0; j < resolution; ++j)
                {
                    int dj0 = j;
                    int dj1 = j < resolution - 1 ? j + 1 : 0;
                    triangles[t + 0] = di0 + dj0;
                    triangles[t + 1] = di1 + dj0;
                    triangles[t + 2] = di1 + dj1;
                    triangles[t + 3] = di1 + dj1;
                    triangles[t + 4] = di0 + dj1;
                    triangles[t + 5] = di0 + dj0;

                    t += 6;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();
        }

        private static Vector3[] GetCircle(int resolution)
        {
            Vector3[] circle = new Vector3[resolution];
            for (int i = 0; i < resolution; ++i)
            {
                float a = 2.0f * Mathf.PI * (float)i / (float)(resolution - 1);
                circle[i] = xAxis * Mathf.Cos(a) + yAxis * Mathf.Sin(a);
            }
            return circle;
        }
    }
}
