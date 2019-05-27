using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Parsley
{
    /// <summary>
    /// Helper feature to make rigid bodies suspended in the air.
    /// </summary>
    public class Suspender : IEnumerable<Rigidbody>
    {
        internal class Item
        {
            public Rigidbody body;
            public bool wasKinematic;

            public Item(Rigidbody body, bool wasKinematic)
            {
                this.body = body;
                this.wasKinematic = wasKinematic;
            }

            public void Suspend()
            {
                ApplySuspension();
                Debug.Log($"Suspend: {body.name}");

                var manip = body.GetComponent<ManipulationHandler>();
                if (manip)
                {
                    // Put the item back into suspended mode after manipulation
                    // ManipulationHandler will otherwise set it to dynamic in case
                    // the item was suspended after manipulation started.
                    manip.OnManipulationEnded.AddListener(OnManipulationEnded);
                }
            }

            public void Drop()
            {
                body.isKinematic = wasKinematic;
                Debug.Log($"Drop: {body.name}");

                var manip = body.GetComponent<ManipulationHandler>();
                if (manip)
                {
                    manip.OnManipulationEnded.RemoveListener(OnManipulationEnded);
                }
            }

            private void ApplySuspension()
            {
                body.isKinematic = true;
            }

            private void OnManipulationEnded(ManipulationEventData evt)
            {
                ApplySuspension();
            }
        }

        private readonly HashSet<Item> suspended = new HashSet<Item>();

        public void Suspend(Rigidbody body, bool isKinematic)
        {
            var item = new Item(body, isKinematic);
            suspended.Add(item);
            item.Suspend();
        }

        public void Drop(Rigidbody body)
        {
            foreach (var item in suspended)
            {
                if (item.body == body)
                {
                    item.Drop();
                }
            }
            suspended.RemoveWhere((item) => item.body == body);
        }

        public void DropAll()
        {
            foreach (var item in suspended)
            {
                item.Drop();
            }
            suspended.Clear();
        }

        public IEnumerator<Rigidbody> GetEnumerator()
        {
            foreach (var item in suspended)
            {
                yield return item.body;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
