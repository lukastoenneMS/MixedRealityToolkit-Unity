// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class PoseRecorder : InputSystemGlobalHandlerListener, IMixedRealitySourceStateHandler, IMixedRealityHandJointHandler
    {
        public PoseConfiguration config { get; private set; }

        public bool IsTracking => sourceId > 0;

        private uint sourceId = 0;

        private readonly Dictionary<TrackedHandJoint, GameObject> jointIndicators = new Dictionary<TrackedHandJoint, GameObject>();

        private static readonly TrackedHandJoint[] jointValues = (TrackedHandJoint[])Enum.GetValues(typeof(TrackedHandJoint));

        void Awake()
        {
            foreach (TrackedHandJoint joint in jointValues)
            {
                jointIndicators = 
            }
        }

        /// <inheritdoc />
        public void OnSourceDetected(SourceStateEventData eventData)
        {
            var hand = eventData.Controller as IMixedRealityHand;
            if (hand != null)
            {
                TryStartTracking(hand);
            }
        }

        /// <inheritdoc />
        public void OnSourceLost(SourceStateEventData eventData)
        {
            if (eventData.SourceId == sourceId)
            {
                StopTracking();
            }
        }

        public void OnHandJointsUpdated(InputEventData<IDictionary<TrackedHandJoint, Pose>> eventData)
        {
            if (eventData.SourceId == sourceId)
            {
                UpdateHandPose(eventData.InputData);
            }
        }

        public new void OnDisable()
        {
            base.OnDisable();
            StopTracking();
        }

        private bool TryStartTracking(IMixedRealityHand hand)
        {
            if (sourceId == 0)
            {
                sourceId = hand.InputSource.SourceId;
                config = new PoseConfiguration();

                var handPose = PollHandPose(hand);
                UpdateHandPose(handPose);

                return true;
            }
            return false;
        }

        private void StopTracking()
        {
            sourceId = 0;
            config = null;
        }

        private static IDictionary<TrackedHandJoint, Pose> PollHandPose(IMixedRealityHand hand)
        {
            var joints = new Dictionary<TrackedHandJoint, Pose>();
            foreach (TrackedHandJoint joint in jointValues)
            {
                if (hand.TryGetJoint(joint, out Pose pose))
                {
                    joints.Add(joint, pose);
                }
            }
            return joints;
        }

        private void UpdateHandPose(IDictionary<TrackedHandJoint, Pose> joints)
        {

        }

        /// <inheritdoc />
        protected override void RegisterHandlers()
        {
            InputSystem?.RegisterHandler<IMixedRealitySourceStateHandler>(this);
            InputSystem?.RegisterHandler<IMixedRealityHandJointHandler>(this);
        }

        /// <inheritdoc />
        protected override void UnregisterHandlers()
        {
            InputSystem?.UnregisterHandler<IMixedRealitySourceStateHandler>(this);
            InputSystem?.UnregisterHandler<IMixedRealityHandJointHandler>(this);
        }
    }
}