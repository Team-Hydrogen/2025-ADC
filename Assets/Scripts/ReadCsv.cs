using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ReadCsv : MonoBehaviour
{
    public static List<List<string>> ReadCsvFile(string filePath)
    {
        List<List<string>> data = new List<List<string>>();
        var streamReader = new StreamReader(filePath);
        bool endOfFile = false;
        while (!endOfFile)
        {
            var line = streamReader.ReadLine();
            if (line == null)
            {
                endOfFile = true;
                break;
            }
            
            var values = line.Split(',').ToList();
            data.Add(values);
        }
        return data;
    }
}