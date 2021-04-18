#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class CSVDownloader : EditorWindow {

    // 設定ファイルを保存するパス
    public static string folderPath = "Assets/MasterData/";
    // 設定ファイル名
    public static string settingsFileName = "MyGDE Settings.asset";
    // ウェブ公開版
    public static string url = "https://docs.google.com/spreadsheets/d/e/{0}/pub?gid={1}&single=true&output=csv";

    [MenuItem("Tools/MyGDE/Create Settings File")]
    static void CreateSettingsFile() {

        // ScriptableObjectを探す
        var path = folderPath + settingsFileName;
        MyGDESettingsData settings = AssetDatabase.LoadAssetAtPath<MyGDESettingsData>(path);

        // ない場合
        if (settings == null) {
            // フォルダがない場合は作成
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            settings = ScriptableObject.CreateInstance<MyGDESettingsData>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.Refresh();
        }
        // ある場合
        else {
            Debug.Log("Settings file already exists.");
        }

    }

    public static async UniTask DownloadAsync(string docID, List<SheetInfo> si) {

        List<UniTask> tasks = new List<UniTask>();

        // SheetInfoの数だけURLを生成
        for (int i = 0; i < si.Count; i++) {
            string fullURL = string.Format(url, docID, si[i].sheetGID);
            // URLからCSVをダウンロード
            tasks.Add(GetCSVAsync(fullURL, si[i].sheetName));
            // Debug.Log("url : " + fullURL);
        }

        // すべての通信が終わるまで待つ
        await UniTask.WhenAll(tasks);

        Debug.Log(tasks.Count + " 件の通信が完了しました");
    }
    private static async UniTask GetCSVAsync(string url, string sheetName)
    {
        using (var request = UnityWebRequest.Get(url))
        {
            // キャッシュしない
            request.useHttpContinue = false;
            try
            {
                // CSVダウンロード
                var requestTask = await request.SendWebRequest().ToUniTask().Timeout(System.TimeSpan.FromSeconds(5f));
                switch (requestTask.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                        Debug.Log("Connection Error Occured(isHttpError)");
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.Log("Protocol Error Occured(isNetworkError)");
                        break;
                }
                if (requestTask.error != null)
                {
                    Debug.Log("通信に失敗しました:" + requestTask.error);
                }
                else
                {
                    // クラスファイル生成
                    ParseCsv(request.downloadHandler.text, sheetName);
                }
            }
            catch (TimeoutException e)
            {
                Debug.Log("タイムアウトしました:" + e.Source);
            }
        }
    }

    public static void ParseCsv(string csv, string sheetName) {

        var sheet = CSVParser.LoadFromString(csv);

        // 有効な列
        List<int> validIndex = new List<int>();
        // 名前行
        List<string> nameRow = sheet[0];
        // 型行
        List<string> typeRow = sheet[1];

        // 変数名
        List<string> varName = new List<string>();
        // 型名
        List<string> varType = new List<string>();

        // 名前行を解析
        for (int i = 0; i < nameRow.Count; i++) {
            var clm = nameRow[i];

            // ignoreを含む場合は無視する
            if (clm.Contains("ignore")) {
                // do nothing
            }
            else {
                // 変数名を保持
                varName.Add(clm);

                // 型名を保持
                if (typeRow[i].Contains("list")) {
                    // アンダーバーで区切る
                    varType.Add("List<" + typeRow[i].Split('_')[1] + ">");
                }
                else {
                    varType.Add(typeRow[i]);
                }

                // 有効な列番号として保持
                validIndex.Add(i);
            }
        }

        // Rowクラスを生成

        StringBuilder sb = new StringBuilder();
        string rowClass = "Master" + sheetName + "Row";
        string path = folderPath + rowClass + ".cs";

        sb.Append(
            @"using System.Collections.Generic;
public class $CLASS$ {
$VARIABLES$
}"
        );

        string varStr = "";
        for (int i = 0; i < varType.Count; i++) {
            varStr += "\tpublic " + varType[i] + " _" + varName[i] + ";\n";
        }

        sb.Replace("$CLASS$", rowClass);
        sb.Replace("$VARIABLES$", varStr);

        WriteFile(sb, path);

        Debug.Log("Finish generating code " + rowClass);

        // Dataクラスを生成

        sb = new StringBuilder();
        string dataClass = "Master" + sheetName + "Data";
        path = folderPath + dataClass + ".cs";

        sb.Append(
            @"using System.Collections.Generic;
public static class $DATACLASS$ {

    public static $ROWCLASS$ Get(string key) {
		if(!allData.ContainsKey(key)) return null;
		return allData[key];
	}

    public static Dictionary<string, $ROWCLASS$> allData = new Dictionary<string, $ROWCLASS$>(){
$DATA$
    };
}"
        );

        string dataStr = "";

        // データ行から
        for (int row = 2; row < sheet.Count; row++) {
            for (int i = 0; i < validIndex.Count; i++) {

                int clm = validIndex[i];

                // 最初の列（_key）
                if (i == 0) {
                    dataStr += "\t\t{\"" + sheet[row][clm] + "\", new " + rowClass + "{_key=\"" + sheet[row][clm] +
                               "\",";
                    continue;
                }

                // データ列以降
                else if (i > 1) {
                    dataStr += ",";
                }

                switch (varType[i]) {
                    case "int":
                        bool isBlank = string.IsNullOrWhiteSpace(sheet[row][clm]);
                        dataStr += "_" + varName[i] + "=" + (isBlank ? "0" : sheet[row][clm]);
                        break;
                    case "string":
                        dataStr += "_" + varName[i] + "=\"" + sheet[row][clm] + "\"";
                        break;
                    case "List<int>":
                        dataStr += "_" + varName[i] + "=new List<int>{" + sheet[row][clm] + "}";
                        break;
                    case "List<string>":
                        var splitStr = sheet[row][clm].Split(',');
                        var joinStr = "";
                        for (int ls = 0; ls < splitStr.Length; ls++) {
                            if (ls > 0) joinStr += ",";
                            joinStr += "\"" + splitStr[ls] + "\"";
                        }

                        dataStr += "_" + varName[i] + "=new List<string>{" + joinStr + "}";
                        break;
                    default:
                        Debug.Log($"型が正しくありません varType[i]:[{varType[i]}]");
                        break;
                }

            }

            dataStr += "}},\n";
        }

        sb.Replace("$DATACLASS$", dataClass);
        sb.Replace("$ROWCLASS$", rowClass);
        sb.Replace("$DATA$", dataStr);

        WriteFile(sb, path);

        Debug.Log("Finish generating code " + dataClass);
    }

    static void WriteFile(StringBuilder sb, string fileName) {
        string fullPath = string.Empty;
        var results = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName) + " t:Script");
        if (results != null && results.Length > 0) {
            string assetPath = AssetDatabase.GUIDToAssetPath(results[0]);
            fullPath = Path.Combine(Environment.CurrentDirectory, assetPath);
        }
        else
            fullPath = Path.Combine(fileName);

        File.WriteAllText(fullPath, sb.ToString());
        Debug.Log(fileName);

        // アセットを更新する
        AssetDatabase.Refresh();
    }

}

#endif