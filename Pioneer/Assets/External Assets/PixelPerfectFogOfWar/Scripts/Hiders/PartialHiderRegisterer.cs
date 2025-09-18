using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FOW
{
    public class PartialHiderRegisterer : MonoBehaviour
    {
        public Material[] MaterialsToInitialize;

        private Dictionary<Material, PartialHider> InitializedMaterials;

        private void OnEnable()
        {
            RegisterMaterials();
        }

        private void OnDisable()
        {
            DeRegisterMaterials();
        }

        public void RegisterMaterials()
        {
            if (InitializedMaterials == null)
                InitializedMaterials = new Dictionary<Material, PartialHider>();
            foreach (Material mat in MaterialsToInitialize)
            {
                if (!InitializedMaterials.ContainsKey(mat))
                    InitializedMaterials.Add(mat, new PartialHider(mat));

                InitializedMaterials[mat].Register();
            }
        }

        public void DeRegisterMaterials()
        {
            if (InitializedMaterials == null)
                InitializedMaterials = new Dictionary<Material, PartialHider>();
            foreach (Material mat in MaterialsToInitialize)
            {
                if (!InitializedMaterials.ContainsKey(mat))
                    InitializedMaterials.Add(mat, new PartialHider(mat));

                InitializedMaterials[mat].Deregister();
            }
        }
    }
}