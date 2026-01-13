using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine.Rendering;

public class InitDarkUI : MonoBehaviour
{
    [InitializeOnLoad]
	public class InitOnLoad
	{
		static InitOnLoad()
		{
			if (!EditorPrefs.HasKey("DarkUI.HasCustomEditorData"))
			{
				EditorPrefs.SetInt("DarkUI.HasCustomEditorData", 1);

				string mainPath = AssetDatabase.GetAssetPath(Resources.Load("Dark UI Manager"));
				mainPath = mainPath.Replace("Resources/Dark UI Manager.asset", "").Trim();
				string darkPath = mainPath + "Editor/DUI Skin Dark.guiskin";
				string lightPath = mainPath + "Editor/DUI Skin Light.guiskin";

				EditorPrefs.SetString("DarkUI.CustomEditorDark", darkPath);
				EditorPrefs.SetString("DarkUI.CustomEditorLight", lightPath);
			}

			if (!EditorPrefs.HasKey("DarkUI.PipelineUpgrader") && GraphicsSettings.renderPipelineAsset != null)
			{
				EditorPrefs.SetInt("DarkUI.PipelineUpgrader", 1);
				
				if (EditorUtility.DisplayDialog("Dark UI SRP Upgrader", "It looks like your project is using URP/HDRP rendering pipeline, " +
					"would you like to upgrade Dark UI Manager for your project?" +
					"\n\nNote that the blur shader currently isn't compatible with URP/HDRP.", "Yes", "No"))
                {
					try
					{
						Preset defaultPreset = Resources.Load<Preset>("DUIM Presets/SRP Default");
						defaultPreset.ApplyTo(Resources.Load("Dark UI Manager"));
					}

					catch { }
				}
			}
		}
	}
}