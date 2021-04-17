#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[Serializable]
public class MyGDESettingsData : ScriptableObject {
    public string _documentID;
    public List<SheetInfo> _sheetInfos;
    public string _lastUpdate;
}


[System.Serializable]
public class SheetInfo {
    public string sheetName;
    public string sheetGID;
}


[UnityEditor.CustomEditor(typeof(MyGDESettingsData))]
public class MyGDESettingsDataEditor : UnityEditor.Editor {
    public override async void OnInspectorGUI() {
        base.OnInspectorGUI();
        MyGDESettingsData mod = target as MyGDESettingsData;
        if (GUILayout.Button("Import CSV")) {
            await CSVDownloader.DownloadAsync(mod._documentID, mod._sheetInfos);
            mod._lastUpdate = DateTime.Now.ToString();
        }
    }
}

#endif