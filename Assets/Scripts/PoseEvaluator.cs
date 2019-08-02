// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class PoseEvaluator
    {
        private readonly PointSetTransformSolver solver = new PointSetTransformSolver(PointSetTransformSolver.ScaleSolverMode.Fixed);

        public PoseMatch EvaluatePose(Vector3[] input, PoseConfiguration config)
        {
            return EvaluatePose(input, config, out bool reflectionCase);
        }

        public PoseMatch EvaluatePose(Vector3[] input, PoseConfiguration config, out bool reflectionCase)
        {
            if (input.Length != config.Length)
            {
                throw new ArgumentException($"Input size {input.Length} does not match configuration size {config.Length}");
            }

            FindMinErrorTransform(input, config, out PoseMatch result, out reflectionCase);

            return result;
        }

        /// <summary>
        /// Scale weight on each point such that the maximum error for the given input is not exceeded.
        /// </summary>
        /// <returns>A new pose configuration with adjusted weights</returns>
        public PoseConfiguration GetErrorLimitedConfig(Vector3[] input, PoseConfiguration config, PoseMatch match, float maxError)
        {
            float sqrMaxError = maxError * maxError;

            float[] newWeights = new float[config.Length];
            for (int i = 0; i < config.Length; ++i)
            {
                Vector3 p = match.Offset.Multiply(config.Targets[i]);
                float sqrResidual = (p - input[i]).sqrMagnitude;
                if (sqrResidual > 0.0f && sqrResidual * config.Weights[i] > sqrMaxError)
                {
                    newWeights[i] = sqrMaxError / sqrResidual;
                }
                else
                {
                    newWeights[i] = config.Weights[i];
                }
            }

            return new PoseConfiguration(config.Targets, newWeights);
        }

        private bool FindMinErrorTransform(Vector3[] input, PoseConfiguration config, out PoseMatch match, out bool reflectionCase)
        {
            if (solver.Solve(input, config.Targets, config.Weights))
            {
                match = new PoseMatch(new Pose(solver.CentroidOffset, solver.RotationOffset), solver.ConditionNumber);
                reflectionCase = solver.ReflectionCase;
                return true;
            }

            match = null;
            reflectionCase = false;
            return false;
        }
    }
}