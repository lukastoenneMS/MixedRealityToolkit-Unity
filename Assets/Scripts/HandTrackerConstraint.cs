// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class HandTrackerConstraint : HandTracker
    {
        public UnityEvent OnHandTrackStarted;
        public UnityEvent OnHandTrackStopped;

        private bool wasTracking = false;

        protected override void UpdateHandMatch(Handedness handedness, IDictionary<TrackedHandJoint, Pose> joints)
        {
            if (joints.TryGetValue(TrackedHandJoint.Palm, out Pose pose))
            {
                transform.position = pose.Position;
                transform.rotation = pose.Rotation;
            }

            if (!wasTracking)
            {
                wasTracking = true;
                OnHandTrackStarted.Invoke();
            }
        }

        protected override void ClearHandMatch()
        {
            if (wasTracking)
            {
                wasTracking = false;
                OnHandTrackStopped.Invoke();
            }
        }
    }
}