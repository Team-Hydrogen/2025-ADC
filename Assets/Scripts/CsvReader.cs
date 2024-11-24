using System.Collections.Generic;
using System.IO;
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
        
        //while (csvRow != null)
        //{
        //    var csvRowValues = csvRow.Split(Delimiter);
        //    csvData.Add(csvRowValues);
        //    csvRow = streamReader.ReadLine();
        //}
        
        return csvData;
    }
}