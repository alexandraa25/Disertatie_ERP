using ClosedXML.Excel;

namespace ERPSystem.Shared.BusinessLogic;

public class ExcelExportService
{
    public void FormatSheet(IXLWorksheet sheet)
    {
        var usedRange = sheet.RangeUsed();

        if (usedRange == null)
            return;

        usedRange.SetAutoFilter();

        var header = sheet.Row(1);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        sheet.Columns().AdjustToContents();

        foreach (var column in sheet.Columns())
        {
            if (column.Width > 40)
                column.Width = 40;
        }

        sheet.SheetView.FreezeRows(1);

        foreach (var cell in usedRange.CellsUsed())
        {
            if (cell.Value.IsDateTime)
                cell.Style.DateFormat.Format = "dd.MM.yyyy";

            if (cell.Value.IsNumber)
                cell.Style.NumberFormat.Format = "#,##0.00";
        }
    }
}