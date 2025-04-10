using UnityEngine;

public static class CsvReader
{
    private const char Delimiter = ',';
    
    public static string[][] ReadCsvString(string fileContent)
    {
        string[] contentLines = fileContent.Split('\n');

        string[][] csvData = new string[contentLines.Length][];

        for (int index = 0; index < contentLines.Length; index++)
        {
            csvData[index] = contentLines[index].Split(Delimiter);
        }
        
        return csvData;
    }
    
    public static string[][] ReadCsvFile(TextAsset file)
    {
        return ReadCsvString(file.text);
    }
}