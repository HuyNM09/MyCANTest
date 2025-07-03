using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

public class ExcelTestCaseLoader
{
    /// <summary>
    /// Đọc file Excel và trả về danh sách (ID, Name) test case.
    /// </summary>
    /// <param name="filePath">Đường dẫn tới file Excel</param>
    /// <returns>List các Tuple chứa ID và Name</returns>
    public static List<(string Id, string Name)> LoadTestCases(string filePath)
    {
        var result = new List<(string, string)>();

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Không tìm thấy file Excel: " + filePath);

        using (var workbook = new XLWorkbook(filePath))
        {
            var worksheet = workbook.Worksheet(1); // Trang đầu tiên
            int row = 1;

            while (true)
            {
                var idCell = worksheet.Cell(row, 1);
                var nameCell = worksheet.Cell(row, 2);

                if (idCell.IsEmpty() && nameCell.IsEmpty())
                    break;

                string id = idCell.GetString().Trim();
                string name = nameCell.GetString().Trim();

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                    result.Add((id, name));

                row++;
            }
        }

        return result;
    }
}
