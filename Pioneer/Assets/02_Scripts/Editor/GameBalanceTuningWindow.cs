using UnityEditor;
using UnityEngine;

public class GameBalanceTuningWindow : EditorWindow
{
    private const string AssetPath = "Assets/Resources/GameBalanceSettings.asset";

    private GameBalanceSettings settings;
    private SerializedObject serializedSettings;
    private bool autoApplyInPlayMode = true;

    [MenuItem("Tools/Game Balance Tuning")]
    private static void Open()
    {
        GetWindow<GameBalanceTuningWindow>("Game Balance");
    }

    private void OnEnable()
    {
        LoadOrCreateSettings();
    }

    private void OnGUI()
    {
        LoadOrCreateSettings();

        if (settings == null)
        {
            EditorGUILayout.HelpBox("GameBalanceSettings asset could not be loaded.", MessageType.Error);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Default Balance Settings", EditorStyles.boldLabel);
        EditorGUILayout.ObjectField(settings, typeof(GameBalanceSettings), false);
        autoApplyInPlayMode = EditorGUILayout.Toggle("Auto Apply In Play Mode", autoApplyInPlayMode);

        serializedSettings.Update();
        EditorGUILayout.PropertyField(serializedSettings.FindProperty("dayDuration"));
        EditorGUILayout.PropertyField(serializedSettings.FindProperty("nightDuration"));
        EditorGUILayout.PropertyField(serializedSettings.FindProperty("enemySpawnTable"), true);
        EditorGUILayout.PropertyField(serializedSettings.FindProperty("enemyScaleTable"), true);

        if (serializedSettings.ApplyModifiedProperties())
        {
            settings.NormalizeTotals();
            EditorUtility.SetDirty(settings);

            if (autoApplyInPlayMode)
                ApplyToRunningGameManager();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Normalize Enemy Totals"))
        {
            settings.NormalizeTotals();
            EditorUtility.SetDirty(settings);

            if (autoApplyInPlayMode)
                ApplyToRunningGameManager();
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying || GameManager.Instance == null))
        {
            if (GUILayout.Button("Apply To Running GameManager"))
            {
                ApplyToRunningGameManager();
            }
        }

        using (new EditorGUI.DisabledScope(GameManager.Instance == null))
        {
            if (GUILayout.Button("Copy Current GameManager Values To Asset"))
            {
                GameManager.Instance.CopyCurrentBalanceTo(settings);
                EditorUtility.SetDirty(settings);
                serializedSettings.Update();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("GameManager loads Assets/Resources/GameBalanceSettings.asset automatically when the scene starts. During Play Mode, edited values are applied immediately while Auto Apply is enabled.", MessageType.Info);
    }

    private void LoadOrCreateSettings()
    {
        if (settings != null && serializedSettings != null)
            return;

        settings = AssetDatabase.LoadAssetAtPath<GameBalanceSettings>(AssetPath);

        if (settings == null)
        {
            settings = CreateInstance<GameBalanceSettings>();
            settings.NormalizeTotals();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
        }

        serializedSettings = new SerializedObject(settings);
    }

    private void ApplyToRunningGameManager()
    {
        if (!Application.isPlaying || GameManager.Instance == null)
            return;

        GameManager.Instance.SetBalanceSettings(settings);
        EditorUtility.SetDirty(GameManager.Instance);
    }
}
