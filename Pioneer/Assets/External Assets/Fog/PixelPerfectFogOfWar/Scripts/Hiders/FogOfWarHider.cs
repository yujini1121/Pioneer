using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FOW
{
    public class FogOfWarHider : MonoBehaviour
    {
        [Tooltip("Leaving this empty will make the hider use its own transform as a sample point.")]
        [FormerlySerializedAs("samplePoints")]
        public Transform[] SamplePoints;
        [Tooltip("If Enabled, the hider will never be hidden again after being revealed once.")]
        public bool PermanentlyReveal = false;
        [HideInInspector] public float MaxDistBetweenSamplePoints;

        [HideInInspector] public int NumObservers;
        [HideInInspector] public List<FogOfWarRevealer> Observers = new List<FogOfWarRevealer>();
        [HideInInspector] public Transform CachedTransform;

        private void OnEnable()
        {
            CalculateSamplePointData();
            RegisterHider();
        }

        private void OnDisable()
        {
            SetActive(true);
            DeregisterHider();
        }

        void CalculateSamplePointData()
        {
            if (SamplePoints.Length == 0)
            {
                SamplePoints = new Transform[1];
                SamplePoints[0] = transform;
            }
            MaxDistBetweenSamplePoints = 0;
            for (int i = 0; i < SamplePoints.Length; i++)
            {
                for (int j = i; j < SamplePoints.Length; j++)
                {
                    MaxDistBetweenSamplePoints = Mathf.Max(MaxDistBetweenSamplePoints, Vector3.Distance(SamplePoints[i].position, SamplePoints[j].position));
                }
            }
        }

        public void RegisterHider()
        {
            CachedTransform = transform;
            if (!FogOfWarWorld.HidersList.Contains(this))
            {
                FogOfWarWorld.HidersList.Add(this);
                FogOfWarWorld.NumHiders++;
                SetActive(false);
            }
        }

        public void DeregisterHider()
        {
            if (FogOfWarWorld.HidersList.Contains(this))
            {
                FogOfWarWorld.HidersList.Remove(this);
                FogOfWarWorld.NumHiders--;
                foreach (FogOfWarRevealer revealer in Observers)
                {
                    revealer.HidersSeen.Remove(this);
                }
                NumObservers = 0;
                Observers.Clear();
            }
        }

        public void AddObserver(FogOfWarRevealer Observer)
        {
            if (PermanentlyReveal)
            {
                enabled = false;
                return;
            }
            Observers.Add(Observer);
            if (NumObservers == 0)
            {
                SetActive(true);
            }
            NumObservers++;
        }

        public void RemoveObserver(FogOfWarRevealer Observer)
        {
            Observers.Remove(Observer);
            NumObservers--;
            if (NumObservers == 0)
            {
                SetActive(false);
            }
        }

        public delegate void OnChangeActive(bool isActive);
        public event OnChangeActive OnActiveChanged;
        void SetActive(bool isActive)
        {
            OnActiveChanged?.Invoke(isActive);
        }
    }
}