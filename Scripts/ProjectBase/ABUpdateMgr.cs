using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ABUpdateMgr : SingletonAutoMono<ABUpdateMgr>
{
    private Dictionary<string, ABInfo> remoteABInfo = new Dictionary<string, ABInfo>();
    private Dictionary<string, ABInfo> localABInfo = new Dictionary<string, ABInfo>();
    
    // 待下载的AB包列表
    private List<string> downLoadList = new List<string>();


    public void CheckUpdate(UnityAction<bool> overCallback)
    {
        remoteABInfo.Clear();
        localABInfo.Clear();
        downLoadList.Clear();
        DownloadAbCompareFile((isOver) =>
        {
            if (isOver)
            {
                string remoteInfo = File.ReadAllText(Application.persistentDataPath + "/ABCompareInfo_TMP.txt");
                GetABCompareFileInfo(remoteInfo, remoteABInfo);
                GetLocalABCompareFileInfo((localOver) =>
                {
                    if (localOver)
                    {
                        Debug.Log("Local ABCompare File Success");
                        foreach (var abName in remoteABInfo.Keys)
                        {
                        
                            if (!localABInfo.ContainsKey(abName))
                            {
                                downLoadList.Add(abName);
                            }
                            else
                            {
                                if (localABInfo[abName].md5 != remoteABInfo[abName].md5)
                                {
                                    downLoadList.Add(abName);
                                }
                                print("Remove" + abName);
                                localABInfo.Remove(abName);
                            }
                        }
                        Debug.Log("Compare Success");
                        foreach (var abName in localABInfo.Keys)
                        {
                            if (File.Exists(Application.persistentDataPath + "/" + abName))
                            {
                                File.Delete(Application.persistentDataPath + "/" + abName);
                            }
                        }
                        DownLoadAbFile((abIsOver) =>
                        {
                            if (abIsOver)
                            {
                                File.WriteAllText(Application.persistentDataPath + "/ABCompareInfo.txt", remoteInfo);
                            }
                            overCallback?.Invoke(abIsOver);
                        });
                        Debug.Log("Success");
                    }
                    else
                    {
                        Debug.Log("Local Load Failed");
                    }
   
                });
            }
            else
            {
                overCallback?.Invoke(false);
            }
        });
    }
    
    
    
    public async void DownloadAbCompareFile(UnityAction<bool> overCallback)
    {
        string fileName = "ABCompareInfo.txt";
        bool isOver = false;
        int reDownloadNum = 5;
        string path = Application.persistentDataPath;
        while (!isOver && reDownloadNum > 0)
        {
            await Task.Run(() =>
            {
                // 从资源服务器下载资源对比文件
                isOver = DownloadFile(fileName, path + "/ABCompareInfo_TMP.txt");
            });
            reDownloadNum--;
        }
        overCallback?.Invoke(isOver);
    }

    public void GetABCompareFileInfo(string info, Dictionary<string, ABInfo> dict)
    {

        string[] strs = info.Split('|');
        string[] infos = null;
        for (int i = 0; i < strs.Length; i++)
        {
            infos = strs[i].Split(' ');
            dict.Add(infos[0], new ABInfo(infos[0], infos[1], infos[2]));
        }
    }

    public void GetLocalABCompareFileInfo(UnityAction<bool> overCallback)
    {
        if (File.Exists(Application.persistentDataPath + "/ABCompareInfo.txt"))
        {
            StartCoroutine(GetLocalABCompareFileInfo(Application.persistentDataPath + "/ABCompareInfo.txt", overCallback));
        }
        else if (File.Exists(Application.streamingAssetsPath + "/ABCompareInfo.txt"))
        {
            StartCoroutine(GetLocalABCompareFileInfo(Application.streamingAssetsPath + "/ABCompareInfo.txt", overCallback));
        }
    }

    IEnumerator GetLocalABCompareFileInfo(string filePath, UnityAction<bool> overCallback)
    {
        UnityWebRequest request = UnityWebRequest.Get(filePath);
        yield return request.SendWebRequest();
        // 获取资源对比文件字符串信息
        if (request.result == UnityWebRequest.Result.Success)
        {
            GetABCompareFileInfo(request.downloadHandler.text, localABInfo);
            overCallback?.Invoke(true);
        }
        else
        {
            overCallback?.Invoke(false);
        }
        
        
    }

    public async void DownLoadAbFile(UnityAction<bool> overCallback)
    {
        string localPath = Application.persistentDataPath + "/";
        bool isOver = false;
        List<string> tempList = new List<string>();
        int reDownloadNum = 5;  // 重新下载次数
        int downloadOverNum = 0;
        while (downLoadList.Count > 0 && reDownloadNum > 0)
        {
            for (int i = 0; i < downLoadList.Count; i++)
            {
                isOver = false;
                await Task.Run(() =>
                {
                    isOver = DownloadFile(downLoadList[i], localPath + downLoadList[i]);
                });
                if (isOver)
                {
                    Debug.Log($"Download File nums {++downloadOverNum}/{downLoadList.Count} ......");
                    tempList.Add(downLoadList[i]);
                }
            }

            for (int i = 0; i < tempList.Count; i++)
            {
                downLoadList.Remove(tempList[i]);
            }

            reDownloadNum--;
        }
    
        overCallback?.Invoke(tempList.Count != 0);
        if (!isOver)
        {
            Debug.LogError($"Network Error");
        }
    }

    public bool DownloadFile(string fileName, string localPath)
    {
        try
        {
            FtpWebRequest ftpWebRequest = WebRequest.Create(new Uri("ftp://127.0.0.1/AB/PC/" + fileName)) as FtpWebRequest;
            NetworkCredential credential = new NetworkCredential("User", "123456");
            if (ftpWebRequest != null)
            {
                ftpWebRequest.Credentials = credential;
                ftpWebRequest.Proxy = null;
                ftpWebRequest.KeepAlive = false;
                ftpWebRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                ftpWebRequest.UseBinary = true;
                FtpWebResponse download = ftpWebRequest.GetResponse() as FtpWebResponse;
                Stream downloadStream = download.GetResponseStream();
                using (FileStream file = File.Create(localPath))
                {
                    byte[] bytes = new byte[2048];
                    int contentLenght = downloadStream.Read(bytes, 0, bytes.Length);
                    while (contentLenght != 0)
                    {
                        file.Write(bytes, 0, contentLenght);
                        contentLenght = downloadStream.Read(bytes, 0, bytes.Length);
                    }
                    file.Close();
                    downloadStream.Close();
                }

                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Upload Failed : {e}");
            return false;
        }

        return false;
    }

    public class ABInfo
    {
        public string name;
        public long size;
        public string md5;


        public ABInfo(string name, string size, string md5)
        {
            this.name = name;
            this.size = long.Parse(size);
            this.md5 = md5;
        }
    }
}
