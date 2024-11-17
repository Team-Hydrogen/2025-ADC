using System.Collections.Generic;
using System.IO;

public static class CsvReader
{
    public static List<string[]> ReadCsvFile(string filePath)
    {
        var data = new List<string[]>();
        var streamReader = new StreamReader(filePath);
        var isEndOfFile = false;
        
        while (!isEndOfFile)
        {
            var line = streamReader.ReadLine();
            if (line == null)
            {
                isEndOfFile = true;
                break;
            }
            var values = line.Split(",");
            data.Add(values);
        }
        
        return data;
    }
}