﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.MixedReality.Toolkit.Input
{ 
    public interface IInputSimulationService : IMixedRealityInputDeviceManager
    {
        /// <summary>
        /// Typed representation of the ConfigurationProfile property.
        /// </summary>
        MixedRealityInputSimulationProfile InputSimulationProfile { get; }

        SimulatedHandData HandDataLeft { get; }
        SimulatedHandData HandDataRight { get; }

        bool IsSimulatingHandLeft { get; }
        bool IsSimulatingHandRight { get; }

        bool IsAlwaysVisibleHandLeft { get; }
        bool IsAlwaysVisibleHandRight { get; }
    }
}