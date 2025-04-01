using System.Collections.Generic;
using UnityEngine;

public static class CsvReader
{
    private const char Delimiter = ',';
    
    public static List<string[]> ReadCsvFile(TextAsset file)
    {
        //var streamReader = new StreamReader(filePath);
        string fileContent = file.text;
        string[] contentLines = fileContent.Split('\n');

        List<string[]> csvData = new List<string[]>();

        foreach (string line in contentLines)
        {
            csvData.Add(line.Split(Delimiter));
        }
        
        return csvData;
    }
    
    public static List<string[]> TextToData(string fileContent)
    {
        //var streamReader = new StreamReader(filePath);
        string[] contentLines = fileContent.Split('\n');

        List<string[]> csvData = new List<string[]>();

        foreach (string line in contentLines)
        {
            csvData.Add(line.Split(Delimiter));
        }
        
        return csvData;
    }
}