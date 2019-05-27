// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities.Editor;
using UnityEngine;
using UnityEngine.Events;

namespace Microsoft.MixedReality.Toolkit.Utilities.Solvers
{
    /// <summary>
    /// Provides a solver that follows the TrackedObject/TargetTransform in an orbital motion.
    /// </summary>
    public class AirTapHandler : InputSystemGlobalListener, IMixedRealityInputHandler
    {
        [InspectorField(Type = InspectorField.FieldTypes.Event, Label = "On Ait Tap", Tooltip = "The air has been tapped")]
        public UnityEvent OnAirTap = new UnityEvent();

        [SerializeField]
        private MixedRealityInputAction action = MixedRealityInputAction.None;

        /// <inheritdoc />
        public void OnInputUp(InputEventData eventData)
        {
        }

        /// <inheritdoc />
        public void OnInputDown(InputEventData eventData)
        {
            if (eventData.MixedRealityInputAction == action)
            {
                OnAirTap.Invoke();
            }
        }
    }
}