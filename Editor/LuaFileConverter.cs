using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

public class LuaFileConverter : EditorWindow
{
    private string sourcePath = "";
    private string outputPath = "";
    private string assetBundleName = "";
    private Vector2 scrollPos;
    private Dictionary<string, bool> fileSelections = new Dictionary<string, bool>();
    private bool selectAll = false;
    private bool isProcessing = false;
    private float processTime = 0f;
    private List<string> assetBundleNames = new List<string>();
    private string newAssetBundleName = ""; // 用于输入新 AssetBundle 名称
    private const string SourcePathKey = "LuaFileConverter_SourcePath";
    private const string OutputPathKey = "LuaFileConverter_OutputPath";

    [MenuItem("Tools/Convert Lua Files")]
    public static void ShowWindow()
    {
        GetWindow<LuaFileConverter>("Lua File Converter");
    }

    private void OnEnable()
    {
        // 加载上一次保存的路径
        sourcePath = EditorPrefs.GetString(SourcePathKey, "");
        outputPath = EditorPrefs.GetString(OutputPathKey, "");
        UpdateAssetBundleNames();
        if (!string.IsNullOrEmpty(sourcePath))
        {
            UpdateFileList(); // 加载文件列表
        }
    }

    private void OnDisable()
    {
        // 在窗口关闭时保存路径
        EditorPrefs.SetString(SourcePathKey, sourcePath);
        EditorPrefs.SetString(OutputPathKey, outputPath);
    }



    private void OnGUI()
    {
        GUILayout.Label("Lua File Converter", EditorStyles.boldLabel);

        // Source Path
        EditorGUILayout.BeginHorizontal();
        sourcePath = EditorGUILayout.TextField("Source Path", sourcePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Source Folder", "", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                sourcePath = selectedPath;
                UpdateFileList();
                Repaint();
            }
        }
        EditorGUILayout.EndHorizontal();

        // Output Path
        EditorGUILayout.BeginHorizontal();
        outputPath = EditorGUILayout.TextField("Output Path (Assets/...) ", outputPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Output Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    outputPath = selectedPath;
                    UnityEngine.Debug.Log($"Set outputPath to: {outputPath}");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Output path must be within the Assets folder!", "OK");
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("AssetBundle Name");
        int selectedIndex = assetBundleNames.IndexOf(assetBundleName);
        if (selectedIndex < 0 && !string.IsNullOrEmpty(assetBundleName) && !assetBundleNames.Contains(assetBundleName))
        {
            assetBundleNames.Add(assetBundleName);
            selectedIndex = assetBundleNames.Count - 1;
        }
        selectedIndex = EditorGUILayout.Popup(selectedIndex, assetBundleNames.ToArray());
        if (selectedIndex >= 0 && selectedIndex < assetBundleNames.Count)
        {
            assetBundleName = assetBundleNames[selectedIndex];
        }
        UnityEngine.Debug.Log($"Current assetBundleName: {assetBundleName}");
        
        EditorGUILayout.BeginHorizontal();
        newAssetBundleName = EditorGUILayout.TextField("New AssetBundle Name", newAssetBundleName);
        if (GUILayout.Button("Add", GUILayout.Width(60)))
        {
            if (!string.IsNullOrEmpty(newAssetBundleName) && !assetBundleNames.Contains(newAssetBundleName))
            {
                // 创建临时文件以记录 AssetBundle 名称
                string tempAssetPath = "Assets/" + newAssetBundleName + ".asset";
                AssetDatabase.CreateAsset(new UnityEngine.TextAsset(""), tempAssetPath);
                AssetImporter.GetAtPath(tempAssetPath).assetBundleName = newAssetBundleName;
                AssetDatabase.SaveAssets();

                // 刷新 AssetBundle 名称列表
                UpdateAssetBundleNames();

                // 删除临时文件
                AssetDatabase.DeleteAsset(tempAssetPath);
                UnityEngine.Debug.Log($"Temporary asset {tempAssetPath} deleted.");

                // 确保新名称在列表中
                if (!assetBundleNames.Contains(newAssetBundleName))
                {
                    assetBundleNames.Add(newAssetBundleName);
                }
                assetBundleName = newAssetBundleName;
                newAssetBundleName = "";
                Repaint();
            }
            else if (assetBundleNames.Contains(newAssetBundleName))
            {
                EditorUtility.DisplayDialog("Error", "AssetBundle name already exists!", "OK");
                newAssetBundleName = "";
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please enter a valid AssetBundle name!", "OK");
                newAssetBundleName = ""; 
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // File List
        EditorGUILayout.Space();
        if (string.IsNullOrEmpty(sourcePath))
        {
            EditorGUILayout.LabelField("Please select a source path.");
        }
        else if (fileSelections.Count == 0)
        {
            EditorGUILayout.LabelField("No .lua files found in the selected path.");
        }
        else
        {
            bool newSelectAll = EditorGUILayout.Toggle("Select All", selectAll);
            if (newSelectAll != selectAll)
            {
                selectAll = newSelectAll;
                UpdateAllSelections(selectAll);
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            Dictionary<string, bool> tempSelections = new Dictionary<string, bool>(fileSelections);
            foreach (var file in tempSelections)
            {
                bool currentValue = file.Value;
                bool newValue = EditorGUILayout.Toggle(Path.GetFileName(file.Key), currentValue);
                if (newValue != currentValue)
                {
                    fileSelections[file.Key] = newValue;
                    if (!newValue && selectAll)
                    {
                        selectAll = false;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        // Process Button
        EditorGUI.BeginDisabledGroup(isProcessing || string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(outputPath));
        if (GUILayout.Button("Convert Files"))
        {
            ProcessFilesAsync();
        }
        EditorGUI.EndDisabledGroup();

        if (isProcessing)
        {
            EditorGUILayout.LabelField("Processing...");
        }

        if (processTime > 0)
        {
            EditorGUILayout.LabelField($"Last Process Time: {processTime:F2} seconds");
        }
    }

    private void UpdateFileList()
    {
        fileSelections.Clear();
        if (Directory.Exists(sourcePath))
        {
            string[] files = Directory.GetFiles(sourcePath, "*.lua", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                fileSelections[file] = false;
            }
            UnityEngine.Debug.Log($"Found {files.Length} .lua files in {sourcePath}");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Source path does not exist: {sourcePath}");
        }
    }

    private void UpdateAllSelections(bool value)
    {
        foreach (var key in new List<string>(fileSelections.Keys))
        {
            fileSelections[key] = value;
        }
    }

    private void UpdateAssetBundleNames()
    {
        assetBundleNames.Clear();
        string[] names = AssetDatabase.GetAllAssetBundleNames();
        assetBundleNames.AddRange(names);
        if (!assetBundleNames.Contains(assetBundleName) && !string.IsNullOrEmpty(assetBundleName))
        {
            assetBundleNames.Add(assetBundleName);
        }
    }

    private async void ProcessFilesAsync()
    {
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        isProcessing = true;
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<string> selectedFiles = new List<string>();
        List<string> outputFiles = new List<string>();

        foreach (var file in fileSelections)
        {
            if (file.Value)
            {
                selectedFiles.Add(file.Key);
            }
        }

        // 异步处理文件复制
        await Task.Run(() =>
        {
            foreach (string sourceFile in selectedFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(sourceFile) + ".lua.txt";
                string destFile = Path.Combine(outputPath, fileName).Replace("\\", "/"); // 统一使用正斜杠
                File.Copy(sourceFile, destFile, true);
                outputFiles.Add(destFile);
                UnityEngine.Debug.Log($"Copied to: {destFile}");
            }
        });

        // 确保 AssetDatabase 刷新完成
        AssetDatabase.Refresh();
        await Task.Delay(100); // 短暂延迟，确保刷新完成

        // 在主线程中分配 AssetBundle
        if (!string.IsNullOrEmpty(assetBundleName))
        {
            foreach (string destFile in outputFiles)
            {
                AssignToAssetBundle(destFile);
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("assetBundleName is empty, skipping AssetBundle assignment.");
        }

        stopwatch.Stop();
        processTime = stopwatch.ElapsedMilliseconds / 1000f;
        isProcessing = false;
        AssetDatabase.Refresh();
        UpdateAssetBundleNames();
        Repaint();
    }

    private void AssignToAssetBundle(string filePath)
    {
        UnityEngine.Debug.Log(Application.dataPath);
        if (filePath.StartsWith(Application.dataPath))
        {
            string relativePath = "Assets" + filePath.Substring(Application.dataPath.Length).Replace("\\", "/"); // 统一使用正斜杠
            AssetImporter importer = AssetImporter.GetAtPath(relativePath);
            if (importer != null)
            {
                importer.assetBundleName = assetBundleName;
                importer.SaveAndReimport();
                UnityEngine.Debug.Log($"Assigned {relativePath} to AssetBundle: {assetBundleName}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Failed to get AssetImporter for: {relativePath}. Ensure the file exists and AssetDatabase is refreshed.");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"File {filePath} is not in Assets folder, cannot assign to AssetBundle.");
        }
    }
}