using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        print(Application.persistentDataPath);
       ABUpdateMgr.GetInstance().CheckUpdate((isOver) =>
       {
           if (isOver)
           {
               Debug.Log("Update Success");
           }
           else
           {
               Debug.LogError("Update Failed");
           }
       });

        // LuaMgr.GetInstance().Init();
        // LuaMgr.GetInstance().DoLuaFile("Main");
        
    }
}
