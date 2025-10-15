using System.IO;
using UnityEngine;

public class FileLogger{
    private readonly string filePath;

    public FileLogger(string fileName){
        string folderPath = Application.persistentDataPath + "/Logs";
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        filePath = Path.Combine(folderPath, fileName + ".log");
    }

    public void Log(string message){
        string logEntry = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        File.AppendAllText(filePath, logEntry + "\n");
    }
}
