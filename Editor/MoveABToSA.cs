using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class MoveABToSA : EditorWindow
{
    private string sourcePath = "";
    private string outputPath = "";
    private string compareFileName = "";
    private Vector2 scrollPos;
    private Dictionary<string, bool> fileSelections = new Dictionary<string, bool>();
    private bool selectAll = false;
    private bool isProcessing = false;
    private float processTime = 0f;
    private const string SourcePathKey = "MoveABToSA_SourcePath";
    private const string OutputPathKey = "MoveABToSA_OutputPath";
    private const string CompareFileNamePathKey = "MoveABToSA_CompareFileName";
    
    [MenuItem("Tools/Move AssetBundle")]
    public static void ShowWindow()
    {
        GetWindow<MoveABToSA>("Move AssetBundle File");
    }
    
    private void OnEnable()
    {
        // 加载上一次保存的路径
        sourcePath = EditorPrefs.GetString(SourcePathKey, "");
        outputPath = EditorPrefs.GetString(OutputPathKey, "");
        compareFileName = EditorPrefs.GetString(CompareFileNamePathKey, "");
        if (!string.IsNullOrEmpty(sourcePath))
        {
            UpdateFileList(); // 加载文件列表
        }
    }

    private void UpdateFileList()
    {
        fileSelections.Clear();
        if (Directory.Exists(sourcePath))
        {
            // 获取所有文件
            string[] allFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);

            // 筛选无后缀的文件
            var noExtensionFiles = allFiles
                .Where(file => !Path.GetFileName(file).Contains("."))
                .ToArray();
            foreach (string file in noExtensionFiles)
            {
                fileSelections[file] = false;
            }
            UnityEngine.Debug.Log($"Found {noExtensionFiles.Length} number files in {sourcePath}");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Source path does not exist: {sourcePath}");
        }
    }

    private void OnDisable()
    {
        // 在窗口关闭时保存路径
        EditorPrefs.SetString(SourcePathKey, sourcePath);
        EditorPrefs.SetString(OutputPathKey, outputPath);
        EditorPrefs.SetString(CompareFileNamePathKey, compareFileName);
    }

    private void OnGUI()
    {
        GUILayout.Label("Move AssetBundle File", EditorStyles.boldLabel);
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

        EditorGUILayout.BeginHorizontal();
        compareFileName = EditorGUILayout.TextField("New AssetBundle Name", compareFileName);
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
        if (GUILayout.Button("Copy File"))
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

        string abCompareInfo = "";
        // 异步处理文件复制
        await Task.Run(() =>
        {
            foreach (string sourceFile in selectedFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(sourceFile);
                string destFile = Path.Combine(outputPath, fileName).Replace("\\", "/"); // 统一使用正斜杠
                File.Copy(sourceFile, destFile, true);
                outputFiles.Add(destFile);
                UnityEngine.Debug.Log($"Copied to: {destFile}");

                FileInfo fileInfo = new FileInfo(outputPath + "/" + fileName);
                abCompareInfo += fileInfo.Name + " " + fileInfo.Length + " " +
                                 MD5Helper.CalculateMD5(outputPath + "/" + fileName);
                abCompareInfo += "|";
            }
        });
        abCompareInfo = abCompareInfo[..^1];
        await File.WriteAllTextAsync(outputPath + "/" + compareFileName, abCompareInfo);
        
        stopwatch.Stop();
        processTime = stopwatch.ElapsedMilliseconds / 1000f;
        isProcessing = false;
        // 确保 AssetDatabase 刷新完成
        AssetDatabase.Refresh();
        await Task.Delay(100); // 短暂延迟，确保刷新完成
        Repaint();
    }

    private void UpdateAllSelections(bool value)
    {
        foreach (var key in new List<string>(fileSelections.Keys))
        {
            fileSelections[key] = value;
        }
    }
    
    
}
