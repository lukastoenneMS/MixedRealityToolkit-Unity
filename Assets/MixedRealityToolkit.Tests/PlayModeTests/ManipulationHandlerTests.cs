﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if !WINDOWS_UWP
// When the .NET scripting backend is enabled and C# projects are built
// The assembly that this file is part of is still built for the player,
// even though the assembly itself is marked as a test assembly (this is not
// expected because test assemblies should not be included in player builds).
// Because the .NET backend is deprecated in 2018 and removed in 2019 and this
// issue will likely persist for 2018, this issue is worked around by wrapping all
// play mode tests in this check.

using Microsoft.MixedReality.Toolkit.UI;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;

namespace Microsoft.MixedReality.Toolkit.Tests
{
    public class ManipulationHandlerTests
    {
        [SetUp]
        public void Setup()
        {
            PlayModeTestUtilities.Setup();
        }

        [TearDown]
        public void TearDown()
        {
            PlayModeTestUtilities.TearDown();
        }

        /// <summary>
        /// Test creating adding a ManipulationHandler to GameObject programmatically.
        /// Should be able to run scene without getting any exceptions.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator Test01_ManipulationHandlerInstantiate()
        {
            var testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testObject.transform.localScale = Vector3.one * 0.2f;

            var manipHandler = testObject.AddComponent<ManipulationHandler>();
            // Wait for two frames to make sure we don't get null pointer exception.
            yield return null;
            yield return null;

            GameObject.Destroy(testObject);
            // Wait for a frame to give Unity a change to actually destroy the object
            yield return null;
        }

        /// <summary>
        /// Test creating ManipulationHandler and receiving hover enter/exit events
        /// from gaze provider.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator Test02_ManipulationHandlerGazeHover()
        {

            var testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testObject.transform.localScale = Vector3.one * 0.2f;

            var manipHandler = testObject.AddComponent<ManipulationHandler>();
            int hoverEnterCount = 0;
            int hoverExitCount = 0;

            manipHandler.OnHoverEntered.AddListener((eventData) => hoverEnterCount++);
            manipHandler.OnHoverExited.AddListener((eventData) => hoverExitCount++);

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(1, hoverEnterCount, $"ManipulationHandler did not receive hover enter event, count is {hoverEnterCount}");

            testObject.transform.Translate(Vector3.up);

            // First yield for physics. Second for normal frame step.
            // Without first one, second might happen before translation is applied.
            // Without second one services will not be stepped.
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.AreEqual(1, hoverExitCount, "ManipulationHandler did not receive hover exit event");

            testObject.transform.Translate(5 * Vector3.up);

            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.IsTrue(hoverExitCount == 1, "ManipulationHandler received the second hover event");

            GameObject.Destroy(testObject);

            // Wait for a frame to give Unity a chance to actually destroy the object
            yield return null;
        }

        /// <summary>
        /// Test creates an object with manipulationhandler verifies translation with articulated hand
        /// as well as forcefully ending of the manipulation
        /// from gaze provider.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator ManipulationHandlerForceRelease()
        {
            // set up cube with manipulation handler
            var testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testObject.transform.localScale = Vector3.one * 0.2f;
            Vector3 initialObjectPosition = new Vector3(0f, 0f, 1f);
            testObject.transform.position = initialObjectPosition;
            var manipHandler = testObject.AddComponent<ManipulationHandler>();
            manipHandler.HostTransform = testObject.transform;
            manipHandler.SmoothingActive = false;

            // add near interaction grabbable to be able to grab the cube with the simulated articulated hand
            testObject.AddComponent<NearInteractionGrabbable>();

            yield return new WaitForFixedUpdate();
            yield return null;

            // grab the cube - move it to the right 
            var inputSimulationService = PlayModeTestUtilities.GetInputSimulationService();
            int numSteps = 30;
            
            Vector3 handOffset = new Vector3(0, 0, 0.1f);
            Vector3 initialHandPosition = new Vector3(0, 0, 0.5f);
            Vector3 rightPosition = new Vector3(1f, 0f, 1f);

            yield return PlayModeTestUtilities.ShowHand(Handedness.Right, inputSimulationService);
            yield return PlayModeTestUtilities.MoveHandFromTo(initialHandPosition, initialObjectPosition, numSteps, ArticulatedHandPose.GestureId.Open, Handedness.Right, inputSimulationService);
            yield return PlayModeTestUtilities.MoveHandFromTo(initialObjectPosition, rightPosition, numSteps, ArticulatedHandPose.GestureId.Pinch, Handedness.Right, inputSimulationService);

            yield return null;

            // test if the object was properly translated
            float maxError = 0.05f;
            Vector3 posDiff = testObject.transform.position - rightPosition;
            Assert.IsTrue(posDiff.magnitude <= maxError, "ManipulationHandler translate failed");

            // forcefully end manipulation and drag with hand back to original position - object shouldn't move with hand
            manipHandler.ForceEndManipulation();
            yield return PlayModeTestUtilities.MoveHandFromTo(rightPosition, initialObjectPosition, numSteps, ArticulatedHandPose.GestureId.Pinch, Handedness.Right, inputSimulationService);

            posDiff = testObject.transform.position - initialObjectPosition;
            Assert.IsTrue(posDiff.magnitude > maxError, "Manipulationhandler modified objects even though manipulation was forcefully ended.");
            posDiff = testObject.transform.position - rightPosition;
            Assert.IsTrue(posDiff.magnitude <= maxError, "Manipulated object didn't remain in place after forcefully ending manipulation");

            // move hand back to object
            yield return PlayModeTestUtilities.MoveHandFromTo(initialObjectPosition, rightPosition, numSteps, ArticulatedHandPose.GestureId.Open, Handedness.Right, inputSimulationService);

            // grab object again and move to original position
            yield return PlayModeTestUtilities.MoveHandFromTo(rightPosition, initialObjectPosition, numSteps, ArticulatedHandPose.GestureId.Pinch, Handedness.Right, inputSimulationService);

            // test if object was moved by manipulationhandler
            posDiff = testObject.transform.position - initialObjectPosition;
            Assert.IsTrue(posDiff.magnitude <= maxError, "ManipulationHandler translate failed on valid manipulation after ForceEndManipulation");

            yield return PlayModeTestUtilities.HideHand(Handedness.Right, inputSimulationService);

            Object.Destroy(testObject);
        }


        /// <summary>
        /// This tests the one hand movement while camera (character) is moving around.
        /// The test will check the offset between object pivot and grab point and make sure we're not drifting
        /// out of the object on pointer rotation - this test should be the same in all rotation setups
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator ManipulationHandlerOneHandMove()
        {
            // set up cube with manipulation handler
            var testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testObject.transform.localScale = Vector3.one * 0.2f;
            Vector3 initialObjectPosition = new Vector3(0f, 0f, 1f);
            testObject.transform.position = initialObjectPosition;
            var manipHandler = testObject.AddComponent<ManipulationHandler>();
            manipHandler.HostTransform = testObject.transform;
            manipHandler.SmoothingActive = false;
            manipHandler.ManipulationType = ManipulationHandler.HandMovementType.OneHandedOnly;

            // add near interaction grabbable to be able to grab the cube with the simulated articulated hand
            testObject.AddComponent<NearInteractionGrabbable>();

            yield return new WaitForFixedUpdate();
            yield return null;

            const int numCircleSteps = 10;
            const int numHandSteps = 10;

            Vector3 handOffset = new Vector3(0, 0, 0.1f);
            Vector3 initialHandPosition = new Vector3(0, 0, 0.5f);
            Vector3 initialGrabPosition = new Vector3(-0.1f, -0.1f, 1f); // grab the left bottom corner of the cube 
            TestHand hand = new TestHand(Handedness.Right);

            // do this test for every one hand rotation mode
            for (ManipulationHandler.RotateInOneHandType type = ManipulationHandler.RotateInOneHandType.MaintainRotationToUser; type <= ManipulationHandler.RotateInOneHandType.RotateAboutGrabPoint; ++type)
            {
                manipHandler.OneHandRotationModeNear = type;

                TestUtilities.PlayspaceToOriginLookingForward();

                yield return hand.Show(initialHandPosition);
                yield return hand.MoveTo(initialGrabPosition, numHandSteps);
                yield return hand.SetGesture(ArticulatedHandPose.GestureId.Pinch);

                // save relative pos grab point to object
                Vector3 initialOffsetGrabToObjPivot = MixedRealityPlayspace.InverseTransformPoint(initialGrabPosition) - MixedRealityPlayspace.InverseTransformPoint(testObject.transform.position);

                // full circle
                const int degreeStep = 360 / numCircleSteps;

                // rotating the pointer in a circle around "the user" 
                for (int i = 1; i <= numCircleSteps; ++i)
                {

                    // rotate main camera (user)
                    MixedRealityPlayspace.PerformTransformation(
                    p =>
                    {
                        p.position = MixedRealityPlayspace.Position;
                        Vector3 rotatedFwd = Quaternion.AngleAxis(degreeStep * i, Vector3.up) * Vector3.forward;
                        p.LookAt(rotatedFwd);
                    });

                    yield return null;

                    // move hand with the camera
                    Vector3 newHandPosition = Quaternion.AngleAxis(degreeStep * i, Vector3.up) * initialGrabPosition;
                    yield return hand.MoveTo(newHandPosition, 1);

                    // make sure that the offset between grab point and object pivot hasn't changed while rotating
                    Vector3 offsetRotated = MixedRealityPlayspace.InverseTransformPoint(newHandPosition) - MixedRealityPlayspace.InverseTransformPoint(testObject.transform.position);
                    TestUtilities.AssertAboutEqual(offsetRotated, initialOffsetGrabToObjPivot, "Grab point on object changed during rotation");
                }

                yield return hand.SetGesture(ArticulatedHandPose.GestureId.Open);
                yield return hand.Hide();

            }

        }

    }
}
#endif