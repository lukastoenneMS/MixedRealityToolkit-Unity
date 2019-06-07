﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Plays back input animation via the input simulation system.
    /// </summary>
    public interface IMixedRealityInputPlaybackService : IMixedRealityInputDeviceManager
    {
        /// <summary>
        /// The animation currently being played.
        /// </summary>
        InputAnimation Animation { get; set; }

        /// <summary>
        /// True if the animation is currently playing.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// The local time relative to the start of the animation.
        /// </summary>
        float LocalTime { get; set; }

        /// <summary>
        /// Start playing the animation.
        /// </summary>
        void Play();

        /// <summary>
        /// Stop playing the animation and jump to the start.
        /// </summary>
        void Stop();

        /// <summary>
        /// Pause playback and keep the current local time.
        /// </summary>
        void Pause();
    }
}
