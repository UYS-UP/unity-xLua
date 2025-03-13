using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class AssetBundleTool : EditorWindow
{
    private string[] platforms = { "PC", "IOS", "Android" };
    private int plantformsNowIndex = 0;
    private string serverIP = "ftp://127.0.0.1/";
    
    [MenuItem("Tools/AssetBundle Tool")]
    private static void OpenWindow()
    {
        AssetBundleTool windown = EditorWindow.GetWindowWithRect<AssetBundleTool>(new Rect(0, 0, 300, 180));
        windown.Show();
    }

    private void OnGUI()
    {

        GUI.Label(new Rect(10, 10, 150, 15), "Platform");
        plantformsNowIndex = GUI.Toolbar(new Rect(10, 30, 250, 20), plantformsNowIndex, platforms);
        GUI.Label(new Rect(10, 60, 150, 15), "Source Server IP");
        serverIP = GUI.TextField(new Rect(10, 80, 250, 20), serverIP);
        if (GUI.Button(new Rect(10, 120, 150, 30), "Upload"))
        {
            UploadAllAbFile();
        }
    }
    
    
    private void UploadAllAbFile()
    {
        DirectoryInfo directoryInfo = Directory.CreateDirectory(Application.dataPath + "/ABRes/AB/" + platforms[plantformsNowIndex]);
        FileInfo[] fileInfos = directoryInfo.GetFiles();
        foreach (var info in fileInfos)
        {
            if (info.Extension == "" ||
                info.Extension == ".txt")
            {
                FtpUploadFile(info.FullName, info.Name);
            }
        }
    }


    private async void FtpUploadFile(string filePath, string fileName)
    {
        await Task.Run(() =>
        {
            try
            {
                FtpWebRequest ftpWebRequest = WebRequest.Create(new Uri(serverIP + fileName)) as FtpWebRequest;
                NetworkCredential credential = new NetworkCredential("User", "123456");
                if (ftpWebRequest != null)
                {
                    ftpWebRequest.Credentials = credential;
                    ftpWebRequest.Proxy = null;
                    ftpWebRequest.KeepAlive = false;
                    ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile;
                    ftpWebRequest.UseBinary = true;
                    Stream upLoadStream = ftpWebRequest.GetRequestStream();

                    using (FileStream file = File.OpenRead(filePath))
                    {
                        byte[] bytes = new byte[2048];
                        int contentLenght = file.Read(bytes, 0, bytes.Length);
                        while (contentLenght != 0)
                        {
                            upLoadStream.Write(bytes, 0, contentLenght);
                            contentLenght = file.Read(bytes, 0, bytes.Length);
                        }
                        file.Close();
                        upLoadStream.Close();
                    }
                    Debug.Log("Upload Success ! ! !");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Upload Failed ! ! !");
            }
        });
        
    }
}
