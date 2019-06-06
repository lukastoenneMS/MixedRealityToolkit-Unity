#define ENABLE_DEBUG_HELPERS

using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parsley
{
    [Serializable]
    public class DebugHelpers : MonoBehaviour
    {
        public GameObject PoseIndicator = null;

        private static bool isInitialized = false;
        private static DebugHelpers instance = null;
        private static DebugHelpers Instance
        {
            get
            {
                if (!isInitialized && Application.isPlaying)
                {
                    instance = GameObject.FindObjectOfType<DebugHelpers>();
                    isInitialized = true;
                }
                return instance;
            }
        }

        private readonly Dictionary<string, Tuple<GameObject, int>> debugObjects = new Dictionary<string, Tuple<GameObject, int>>();

        public static void ShowPose(string id, MixedRealityPose pose, float scale = 1.0f, float timeout = 0.0f)
        {
#if ENABLE_DEBUG_HELPERS
            if (Instance)
            {
                var ob = Instance.ShowObject(id, Instance.PoseIndicator, timeout);
                ob.transform.position = pose.Position;
                ob.transform.rotation = pose.Rotation;
                ob.transform.localScale = Vector3.one * scale;
            }
#endif
        }

        public static void ShowPose(MonoBehaviour component, string id, MixedRealityPose pose, float scale = 1.0f, float timeout = 0.0f)
        {
            ShowPose(ComponentId(component, id), pose, scale, timeout);
        }

        private static string ComponentId(MonoBehaviour component, string id)
        {
            return component.GetInstanceID().ToString() + "_" + id;
        }

        private GameObject ShowObject(string id, GameObject prefab, float timeout = -1.0f)
        {
            if (debugObjects.TryGetValue(id, out Tuple<GameObject, int> debugOb))
            {
                if (debugOb.Item2 == prefab.GetInstanceID())
                {
                    return debugOb.Item1;
                }
                else
                {
                    GameObject.Destroy(debugOb.Item1);
                    debugObjects.Remove(id);
                }
            }

            var value = new Tuple<GameObject, int>(GameObject.Instantiate(prefab, transform), prefab.GetInstanceID());
            debugObjects.Add(id, value);

            return value.Item1;
        }
    }
}
