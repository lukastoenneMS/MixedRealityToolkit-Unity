// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Physics;
using Microsoft.MixedReality.Toolkit.Utilities.Editor;
using UnityEngine;
using UnityEngine.Events;

namespace Microsoft.MixedReality.Toolkit.Utilities.Solvers
{
    /// <summary>
    /// Provides a solver that follows the TrackedObject/TargetTransform in an orbital motion.
    /// </summary>
    public class StageProject : Solver
    {
        [SerializeField]
        [Tooltip("The desired orientation of this object. Default sets the object to face the TrackedObject/TargetTransform. CameraFacing sets the object to always face the user.")]
        private SolverOrientationType orientationType = SolverOrientationType.FollowTrackedObject;

        /// <summary>
        /// The desired orientation of this object.
        /// </summary>
        /// <remarks>
        /// Default sets the object to face the TrackedObject/TargetTransform. CameraFacing sets the object to always face the user.
        /// </remarks>
        public SolverOrientationType OrientationType
        {
            get { return orientationType; }
            set { orientationType = value; }
        }

        [SerializeField]
        [Tooltip("Raycast direction in relation to the TrackedObject/TargetTransform.")]
        private Vector3 localDirection = Vector3.forward;

        /// <summary>
        /// Raycast direction in relation to the TrackedObject/TargetTransform.
        /// </summary>
        public Vector3 LocalDirection
        {
            get { return localDirection; }
            set { localDirection = value; }
        }

        [SerializeField]
        [Tooltip("Maximum distance to project the object.")]
        private float maxDistance = 2.0f;

        /// <summary>
        /// Maximum distance to project the object.
        /// </summary>
        public float MaxDistance
        {
            get { return maxDistance; }
            set { maxDistance = value; }
        }

        [SerializeField]
        [Tooltip("Layers to project on.")]
        private LayerMask[] projectionSurfaces = { UnityEngine.Physics.DefaultRaycastLayers };

        /// <summary>
        /// Layers to project on.
        /// </summary>
        public LayerMask[] ProjectionSurfaces
        {
            get { return projectionSurfaces; }
            set { projectionSurfaces = value; }
        }

        public override void SolverUpdate()
        {
            if (SolverHandler.TransformTarget)
            {
                Vector3 rayStart = SolverHandler.TransformTarget.position;
                Vector3 rayTerminus = rayStart + maxDistance * (SolverHandler.TransformTarget.rotation * localDirection);

                var rayStep = new RayStep(rayStart, rayTerminus);
                bool isHit = MixedRealityRaycaster.RaycastSimplePhysicsStep(rayStep, maxDistance, projectionSurfaces, out RaycastHit result);

                if (isHit)
                {
                    GoalPosition = result.point;
                }
                else
                {
                    GoalPosition = rayTerminus;
                }

                UpdateWorkingPositionToGoal();
                UpdateWorkingRotationToGoal();
            }
        }
    }
}