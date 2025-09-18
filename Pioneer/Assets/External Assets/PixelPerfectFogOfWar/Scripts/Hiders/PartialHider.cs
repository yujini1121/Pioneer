using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FOW
{
    public class PartialHider
    {
        public Material HiderMaterial;
        private bool _initialized = false;

        public PartialHider(Material mat)
        {
            HiderMaterial = mat;
        }

        public void Register()
        {
            if (!FogOfWarWorld.PartialHiders.Contains(this))
                FogOfWarWorld.PartialHiders.Add(this);

            if (!_initialized)
                InitializeMaterial();
        }

        public void Deregister()
        {
            if (FogOfWarWorld.PartialHiders.Contains(this))
                FogOfWarWorld.PartialHiders.Remove(this);
        }

        void InitializeMaterial()
        {
            _initialized = true;
            FogOfWarWorld.instance.InitializeFogProperties(HiderMaterial);
            FogOfWarWorld.instance.UpdateMaterialProperties(HiderMaterial);
            FogOfWarWorld.instance.SetNumRevealers(HiderMaterial);
        }
    }
}