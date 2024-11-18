using System.Collections.Generic;
using System.IO;

public static class CsvReader
{
    private const char Delimiter = ',';
    
    public static List<string[]> ReadCsvFile(string filePath)
    {
        var streamReader = new StreamReader(filePath);
        var csvData = new List<string[]>();
        var csvRow = streamReader.ReadLine();
        
        while (csvRow != null)
        {
            var csvRowValues = csvRow.Split(Delimiter);
            csvData.Add(csvRowValues);
            csvRow = streamReader.ReadLine();
        }
        
        return csvData;
    }
}