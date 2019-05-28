// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    [MixedRealityDataProvider(
        typeof(IMixedRealityInputSystem),
        SupportedPlatforms.WindowsEditor,
        "Input Simulation Service",
        "Profiles/DefaultMixedRealityInputSimulationProfile.asset",
        "MixedRealityToolkit.SDK")]
    [DocLink("https://microsoft.github.io/MixedRealityToolkit-Unity/Documentation/InputSimulation/InputSimulationService.html")]
    public class InputSimulationService : BaseInputDeviceManager, IInputSimulationService
    {
        private ManualCameraControl cameraControl = null;
        private SimulatedHandDataProvider handDataProvider = null;

        private readonly SimulatedHandData handDataLeft = new SimulatedHandData();
        private readonly SimulatedHandData handDataRight = new SimulatedHandData();
        public SimulatedHandData HandDataLeft => handDataLeft;
        public SimulatedHandData HandDataRight => handDataRight;

        public bool IsSimulatingHandLeft => (handDataProvider != null ? handDataProvider.IsSimulatingLeft : false);
        public bool IsSimulatingHandRight => (handDataProvider != null ? handDataProvider.IsSimulatingRight : false);

        public bool IsAlwaysVisibleHandLeft => (handDataProvider != null ? handDataProvider.IsAlwaysVisibleLeft : false);
        public bool IsAlwaysVisibleHandRight => (handDataProvider != null ? handDataProvider.IsAlwaysVisibleRight : false);

        /// <summary>
        /// If true then keyboard and mouse input are used to simulate hands.
        /// </summary>
        public bool UserInputEnabled = true;

        /// <summary>
        /// Dictionary to capture all active hands detected
        /// </summary>
        private readonly Dictionary<Handedness, SimulatedHand> trackedHands = new Dictionary<Handedness, SimulatedHand>();

        /// <summary>
        /// Active controllers
        /// </summary>
        private IMixedRealityController[] activeControllers = new IMixedRealityController[0];

        /// <summary>
        /// Timestamp of the last hand device update
        /// </summary>
        private long lastHandUpdateTimestamp = 0;

        /// <summary>
        /// Indicators to show input simulation state in the viewport.
        /// </summary>
        private GameObject indicators = null;

        #region BaseInputDeviceManager Implementation

        public InputSimulationService(
            IMixedRealityServiceRegistrar registrar,
            IMixedRealityInputSystem inputSystem,
            string name, 
            uint priority, 
            BaseMixedRealityProfile profile) : base(registrar, inputSystem, name, priority, profile) { }

        /// <inheritdoc />
        public override IMixedRealityController[] GetActiveControllers()
        {
            return activeControllers;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            ArticulatedHandPose.LoadGesturePoses();
        }

        /// <inheritdoc />
        public override void Enable()
        {
            var profile = InputSimulationProfile;

            if (indicators == null && profile.IndicatorsPrefab)
            {
                indicators = GameObject.Instantiate(profile.IndicatorsPrefab);
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            GameObject.Destroy(indicators);

            DisableCameraControl();
            DisableHandSimulation();
        }

        /// <inheritdoc />
        public override void Update()
        {
            var profile = InputSimulationProfile;

            if (profile.IsCameraControlEnabled)
            {
                EnableCameraControl();
                if (CameraCache.Main)
                {
                    cameraControl.UpdateTransform(CameraCache.Main.transform);
                }
            }
            else
            {
                DisableCameraControl();
            }

            if (profile.SimulateEyePosition)
            {
                InputSystem?.EyeGazeProvider?.UpdateEyeGaze(null, new Ray(CameraCache.Main.transform.position, CameraCache.Main.transform.forward), System.DateTime.UtcNow);
            }

            switch (profile.HandSimulationMode)
            {
                case HandSimulationMode.Disabled:
                    DisableHandSimulation();
                    break;

                case HandSimulationMode.Articulated:
                case HandSimulationMode.Gestures:
                    EnableHandSimulation();

                    if (UserInputEnabled)
                    {
                        handDataProvider.UpdateHandData(handDataLeft, handDataRight);
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public override void LateUpdate()
        {
            var profile = InputSimulationProfile;

            // Apply hand data in LateUpdate to ensure external changes are applied.
            // HandDataLeft/Right can be modified after the services Update() call.
            if (profile.HandSimulationMode != HandSimulationMode.Disabled)
            {
                DateTime currentTime = DateTime.UtcNow;
                double msSinceLastHandUpdate = currentTime.Subtract(new DateTime(lastHandUpdateTimestamp)).TotalMilliseconds;
                // TODO implement custom hand device update frequency here, use 1000/fps instead of 0
                if (msSinceLastHandUpdate > 0)
                {
                    if (handDataLeft.Timestamp > lastHandUpdateTimestamp)
                    {
                        UpdateHandInputSource(Handedness.Left, handDataLeft);
                    }
                    if (handDataRight.Timestamp > lastHandUpdateTimestamp)
                    {
                        UpdateHandInputSource(Handedness.Right, handDataRight);
                    }

                    lastHandUpdateTimestamp = currentTime.Ticks;
                }
            }
        }

        #endregion BaseInputDeviceManager Implementation

        private MixedRealityInputSimulationProfile inputSimulationProfile = null;

        /// <inheritdoc/>
        public MixedRealityInputSimulationProfile InputSimulationProfile
        {
            get
            {
                if (inputSimulationProfile == null)
                {
                    inputSimulationProfile = ConfigurationProfile as MixedRealityInputSimulationProfile;
                }
                return inputSimulationProfile;
            }
        }

        private void EnableCameraControl()
        {
            if (cameraControl == null)
            {
                cameraControl = new ManualCameraControl(InputSimulationProfile);
            }
        }

        private void DisableCameraControl()
        {
            if (cameraControl != null)
            {
                cameraControl = null;
            }
        }

        private void EnableHandSimulation()
        {
            if (handDataProvider == null)
            {
                handDataProvider = new SimulatedHandDataProvider(InputSimulationProfile);
            }
        }

        private void DisableHandSimulation()
        {
            RemoveAllHandDevices();

            if (handDataProvider != null)
            {
                handDataProvider = null;
            }
        }

        // Register input sources for hands based on changes of the data provider
        private void UpdateHandInputSource(Handedness handedness, SimulatedHandData handData)
        {
            var profile = InputSimulationProfile;

            if (profile.HandSimulationMode == HandSimulationMode.Disabled)
            {
                RemoveAllHandDevices();
            }
            else
            {
                if (handData != null && handData.IsTracked)
                {
                    SimulatedHand controller = GetOrAddHandDevice(handedness, profile.HandSimulationMode);
                    controller.UpdateState(handData);
                }
                else
                {
                    RemoveHandDevice(handedness);
                }
            }
        }

        private SimulatedHand GetHandDevice(Handedness handedness)
        {
            if (trackedHands.TryGetValue(handedness, out SimulatedHand controller))
            {
                return controller;
            }
            return null;
        }

        private SimulatedHand GetOrAddHandDevice(Handedness handedness, HandSimulationMode simulationMode)
        {
            var controller = GetHandDevice(handedness);
            if (controller != null)
            {
                if (controller.SimulationMode == simulationMode)
                {
                    return controller;
                }
                else
                {
                    // Remove and recreate hand device if simulation mode doesn't match
                    RemoveHandDevice(handedness);
                }
            }

            SupportedControllerType st = simulationMode == HandSimulationMode.Gestures ? SupportedControllerType.GGVHand : SupportedControllerType.ArticulatedHand;
            IMixedRealityPointer[] pointers = RequestPointers(st, handedness);

            var inputSource = InputSystem?.RequestNewGenericInputSource($"{handedness} Hand", pointers, InputSourceType.Hand);
            switch (simulationMode)
            {
                case HandSimulationMode.Articulated:
                    controller = new SimulatedArticulatedHand(TrackingState.Tracked, handedness, inputSource);
                    break;
                case HandSimulationMode.Gestures:
                    controller = new SimulatedGestureHand(TrackingState.Tracked, handedness, inputSource);
                    break;
                default:
                    controller = null;
                    break;
            }

            System.Type controllerType = simulationMode == HandSimulationMode.Gestures ? typeof(SimulatedGestureHand) : typeof(SimulatedArticulatedHand);
            if (controller == null)
            {
                Debug.LogError($"Failed to create {controllerType} controller");
                return null;
            }

            if (!controller.SetupConfiguration(controllerType, InputSourceType.Hand))
            {
                // Controller failed to be setup correctly.
                Debug.LogError($"Failed to Setup {controllerType} controller");
                // Return null so we don't raise the source detected.
                return null;
            }

            for (int i = 0; i < controller.InputSource?.Pointers?.Length; i++)
            {
                controller.InputSource.Pointers[i].Controller = controller;
            }

            InputSystem?.RaiseSourceDetected(controller.InputSource, controller);

            trackedHands.Add(handedness, controller);
            UpdateActiveControllers();

            return controller;
        }

        private void RemoveHandDevice(Handedness handedness)
        {
            var controller = GetHandDevice(handedness);
            if (controller != null)
            {
                InputSystem?.RaiseSourceLost(controller.InputSource, controller);

                trackedHands.Remove(handedness);
                UpdateActiveControllers();
            }
        }

        private void RemoveAllHandDevices()
        {
            foreach (var controller in trackedHands.Values)
            {
                InputSystem?.RaiseSourceLost(controller.InputSource, controller);
            }
            trackedHands.Clear();
            UpdateActiveControllers();
        }

        private void UpdateActiveControllers()
        {
            activeControllers = trackedHands.Values.ToArray<IMixedRealityController>();
        }
    }
}
