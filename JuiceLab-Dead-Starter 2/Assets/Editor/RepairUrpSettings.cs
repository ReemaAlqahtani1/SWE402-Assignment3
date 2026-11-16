using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class RepairUrpSettings
{
    [MenuItem("Tools/JuiceLab/Repair URP Settings")]
    public static void Repair()
    {
        const string path = "Assets/UniversalRenderPipelineGlobalSettings.asset";
        var asset = AssetDatabase.LoadMainAssetAtPath(path);
        if (asset == null) throw new InvalidOperationException("URP settings asset not found.");
        var versionField = asset.GetType().GetField("k_LastVersion", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (versionField == null) throw new InvalidOperationException("Cannot determine installed URP settings version.");
        int expected = (int)versionField.GetValue(null);
        Directory.CreateDirectory("Library/JuiceLabBackups");
        File.Copy(path, "Library/JuiceLabBackups/URP-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".asset", true);
        Undo.RecordObject(asset, "Repair URP settings compatibility");
        UnityEditor.SerializationUtility.ClearAllManagedReferencesWithMissingTypes(asset);
        var serialized = new SerializedObject(asset);
        foreach (string listPath in new[] { "m_Settings.m_SettingsList.m_List", "m_Settings.m_RuntimeSettings.m_List" })
        {
            var list = serialized.FindProperty(listPath);
            if (list == null || !list.isArray) continue;
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                var item = list.GetArrayElementAtIndex(i);
                if (item.propertyType == SerializedPropertyType.ManagedReference && item.managedReferenceValue == null)
                    list.DeleteArrayElementAtIndex(i);
            }
        }
        var version = serialized.FindProperty("m_AssetVersion");
        if (version == null) throw new InvalidOperationException("URP version property not found.");
        version.intValue = expected;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log("URP settings repaired for installed version " + expected + ". Try the Web build again.", asset);
    }
}
