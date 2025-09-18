using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

namespace FOW
{
    class UnityBugWorkaround : AssetPostprocessor
    {
        //BASICALLY, every time an asset is updated in the project folder, materials are losing the compute buffer data. 
        //So, im hooking onto asset post processing, and re-initializing the material with the necessary data
        //(see the Workaround section in FogOfWarWorld)
        public delegate void AssetRefresh();
        public static event AssetRefresh OnAssetPostProcess;
#if UNITY_2021_2_OR_NEWER
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
#else
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
#endif
        {
            OnAssetPostProcess?.Invoke();
        }
    }
}
#endif
