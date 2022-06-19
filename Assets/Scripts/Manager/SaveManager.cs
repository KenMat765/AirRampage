using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static void SaveData<T>(T obj)
    {
        string file_path = Application.persistentDataPath + "/" + obj.GetType().ToString();
        using (StreamWriter writer = new StreamWriter(file_path, false))
        {
            string obj_json = JsonUtility.ToJson(obj);
            writer.Write(obj_json);
            writer.Flush();
            writer.Close();
        }
    }

    public static T LoadData<T>(string className)
    {
        string file_path = Application.persistentDataPath + "/" + className;
        if (!File.Exists(file_path)) return default(T);

        string load_json;
        using (StreamReader reader = new StreamReader(file_path))
        {
            load_json = reader.ReadToEnd();
            reader.Close();
        }
        T load_data = JsonUtility.FromJson<T>(load_json);
        return load_data;
    }
}
