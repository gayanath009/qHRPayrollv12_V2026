using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using qHRM.Areas.Payroll.Models;
using Syncfusion.Drawing;
using Syncfusion.XlsIO;

namespace qHRM.Areas.Payroll.Reports
{
    public static class StaffLeaveTicketEncashExcelReport
    {
        public static byte[] GenerateReport(
            int year,
            int month,
            IEnumerable<HrPrStaffLvTktEncashVw> records)
        {
            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Excel2016;

                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet sheet = workbook.Worksheets[0];
                sheet.Name = "Leave & Ticket Encashment";
                sheet.IsGridLinesVisible = true;

                int prevMonth = month == 1 ? 12 : month - 1;
                int prevYear = month == 1 ? year - 1 : year;
                string monthName = new DateTime(year, month, 1).ToString("MMMM");
                string prevYY = (prevYear % 100).ToString("D2");
                string curYY = (year % 100).ToString("D2");
                string prevMM = prevMonth.ToString("D2");
                string curMM = month.ToString("D2");

                string title = $"Internal/Outsource Staff Leave Salary & Air Ticket Encashment- {monthName} {year}";
                string cutoff = $"(Cut off period 21/{prevMM}/{prevYY} to 20/{curMM}/{curYY})";

                // Title rows
                sheet.Range["A2:Q2"].Merge();
                sheet.Range["A2"].Text = title;
                sheet.Range["A2"].CellStyle.Font.Bold = true;
                sheet.Range["A2"].CellStyle.Font.Size = 13;
                sheet.Range["A2"].CellStyle.Font.FontName = "Calibri";
                sheet.Range["A2"].HorizontalAlignment = ExcelHAlign.HAlignCenter;
                sheet.Range["A2"].VerticalAlignment = ExcelVAlign.VAlignCenter;
                sheet.SetRowHeight(2, 22);

                sheet.Range["A3:Q3"].Merge();
                sheet.Range["A3"].Text = cutoff;
                sheet.Range["A3"].CellStyle.Font.Bold = true;
                sheet.Range["A3"].CellStyle.Font.Size = 10;
                sheet.Range["A3"].CellStyle.Font.FontName = "Calibri";
                sheet.Range["A3"].HorizontalAlignment = ExcelHAlign.HAlignCenter;
                sheet.Range["A3"].VerticalAlignment = ExcelVAlign.VAlignCenter;
                sheet.SetRowHeight(3, 18);

                // Table Header Row 5
                int headerRow = 5;
                sheet.SetRowHeight(headerRow, 38);
                string[] headers = new string[]
                {
                    "Employee ID",
                    "Courtesy Title",
                    "Full Name",
                    "Department",
                    "Designation",
                    "Joined Date",
                    "Join Date after Annual Vacation",
                    "Nationality",
                    "Next Leave Reset Date",
                    "Applicable Days",
                    "Eligible Leave Days",
                    "Eligible Leave Days Rounded\n(-) Paid Leave",
                    "Monthly Salary",
                    "Total Leave Salary",
                    "Air Ticket Value",
                    "Total Settlement",
                    "Remarks"
                };

                for (int c = 0; c < headers.Length; c++)
                {
                    var cell = sheet.Range[headerRow, c + 1];
                    cell.Text = headers[c];
                    cell.CellStyle.Font.Bold = true;
                    cell.CellStyle.Font.Size = 9;
                    cell.CellStyle.Font.FontName = "Calibri";
                    cell.CellStyle.WrapText = true;
                    cell.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                    cell.VerticalAlignment = ExcelVAlign.VAlignCenter;
                    cell.BorderAround(ExcelLineStyle.Thin, Color.Black);
                }

                int currentRow = 6;
                var internalStaff = records.Where(x => x.StaffCategory == "INTERNAL").ToList();
                var outsourceStaff = records.Where(x => x.StaffCategory == "OUTSOURCE").ToList();

                // 1. Internal Staff rows
                decimal internalLeaveSum = 0;
                decimal internalTicketSum = 0;
                decimal internalSettlementSum = 0;

                foreach (var emp in internalStaff)
                {
                    sheet.SetRowHeight(currentRow, 22);
                    FillEmployeeRow(sheet, currentRow, emp);
                    internalLeaveSum += emp.TotalLeaveSalary;
                    internalTicketSum += emp.AirTicketValue;
                    internalSettlementSum += emp.TotalSettlement;
                    currentRow++;
                }

                // Internal Subtotal Row
                if (internalStaff.Any())
                {
                    sheet.SetRowHeight(currentRow, 24);
                    sheet.Range[currentRow, 1, currentRow, 13].Merge();
                    sheet.Range[currentRow, 1].Text = "Total Leave Salary & Air Ticket Encashment for Internal Staff:";
                    sheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
                    sheet.Range[currentRow, 1].CellStyle.Font.Size = 10;
                    sheet.Range[currentRow, 1].HorizontalAlignment = ExcelHAlign.HAlignRight;
                    sheet.Range[currentRow, 1].VerticalAlignment = ExcelVAlign.VAlignCenter;

                    sheet.Range[currentRow, 14].Number = (double)internalLeaveSum;
                    sheet.Range[currentRow, 14].NumberFormat = "#,##0";
                    sheet.Range[currentRow, 14].CellStyle.Font.Bold = true;

                    sheet.Range[currentRow, 15].Number = (double)internalTicketSum;
                    sheet.Range[currentRow, 15].NumberFormat = "#,##0";
                    sheet.Range[currentRow, 15].CellStyle.Font.Bold = true;

                    sheet.Range[currentRow, 16].Number = (double)internalSettlementSum;
                    sheet.Range[currentRow, 16].NumberFormat = "#,##0";
                    sheet.Range[currentRow, 16].CellStyle.Font.Bold = true;

                    for (int col = 1; col <= 17; col++)
                    {
                        sheet.Range[currentRow, col].BorderAround(ExcelLineStyle.Thin, Color.Black);
                    }
                    currentRow++;
                }

                // Spacer Row
                currentRow++;

                // 2. Outsource Staff rows
                decimal outsourceLeaveSum = 0;
                decimal outsourceTicketSum = 0;
                decimal outsourceSettlementSum = 0;

                foreach (var emp in outsourceStaff)
                {
                    sheet.SetRowHeight(currentRow, 22);
                    FillEmployeeRow(sheet, currentRow, emp);
                    outsourceLeaveSum += emp.TotalLeaveSalary;
                    outsourceTicketSum += emp.AirTicketValue;
                    outsourceSettlementSum += emp.TotalSettlement;
                    currentRow++;
                }

                // Outsource Subtotal Row
                if (outsourceStaff.Any())
                {
                    sheet.SetRowHeight(currentRow, 24);
                    sheet.Range[currentRow, 1, currentRow, 13].Merge();
                    sheet.Range[currentRow, 1].Text = "Total Leave Salary & Air Ticket Encashment for Outsource Staff:";
                    sheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
                    sheet.Range[currentRow, 1].CellStyle.Font.Size = 10;
                    sheet.Range[currentRow, 1].HorizontalAlignment = ExcelHAlign.HAlignRight;
                    sheet.Range[currentRow, 1].VerticalAlignment = ExcelVAlign.VAlignCenter;

                    sheet.Range[currentRow, 14].Number = (double)outsourceLeaveSum;
                    sheet.Range[currentRow, 14].NumberFormat = "#,##0";
                    sheet.Range[currentRow, 14].CellStyle.Font.Bold = true;

                    sheet.Range[currentRow, 15].Number = (double)outsourceTicketSum;
                    sheet.Range[currentRow, 15].NumberFormat = "#,##0";
                    sheet.Range[currentRow, 15].CellStyle.Font.Bold = true;

                    sheet.Range[currentRow, 16].Number = (double)outsourceSettlementSum;
                    sheet.Range[currentRow, 16].NumberFormat = "#,##0";
                    sheet.Range[currentRow, 16].CellStyle.Font.Bold = true;

                    for (int col = 1; col <= 17; col++)
                    {
                        sheet.Range[currentRow, col].BorderAround(ExcelLineStyle.Thin, Color.Black);
                    }
                    currentRow++;
                }

                // Grand Total Row
                sheet.SetRowHeight(currentRow, 26);
                sheet.Range[currentRow, 1, currentRow, 15].Merge();
                sheet.Range[currentRow, 1].Text = "Total:";
                sheet.Range[currentRow, 1].CellStyle.Font.Bold = true;
                sheet.Range[currentRow, 1].CellStyle.Font.Size = 11;
                sheet.Range[currentRow, 1].HorizontalAlignment = ExcelHAlign.HAlignRight;
                sheet.Range[currentRow, 1].VerticalAlignment = ExcelVAlign.VAlignCenter;

                decimal grandTotal = internalSettlementSum + outsourceSettlementSum;
                sheet.Range[currentRow, 16].Number = (double)grandTotal;
                sheet.Range[currentRow, 16].NumberFormat = "#,##0";
                sheet.Range[currentRow, 16].CellStyle.Font.Bold = true;
                sheet.Range[currentRow, 16].CellStyle.Font.Size = 11;
                sheet.Range[currentRow, 16].BorderAround(ExcelLineStyle.Double, Color.Black);

                for (int col = 1; col <= 17; col++)
                {
                    sheet.Range[currentRow, col].BorderAround(ExcelLineStyle.Thin, Color.Black);
                }

                // Auto-fit column widths with minimums
                sheet.Columns[0].ColumnWidth = 12; // Emp ID
                sheet.Columns[1].ColumnWidth = 10; // Title
                sheet.Columns[2].ColumnWidth = 28; // Full Name
                sheet.Columns[3].ColumnWidth = 24; // Dept
                sheet.Columns[4].ColumnWidth = 26; // Desg
                sheet.Columns[5].ColumnWidth = 13; // Joined Date
                sheet.Columns[6].ColumnWidth = 15; // Vacation return
                sheet.Columns[7].ColumnWidth = 14; // Nationality
                sheet.Columns[8].ColumnWidth = 14; // Reset date
                sheet.Columns[9].ColumnWidth = 12; // Applicable days
                sheet.Columns[10].ColumnWidth = 14; // Eligible leave days
                sheet.Columns[11].ColumnWidth = 14; // Net leave days
                sheet.Columns[12].ColumnWidth = 14; // Monthly salary
                sheet.Columns[13].ColumnWidth = 15; // Total leave salary
                sheet.Columns[14].ColumnWidth = 14; // Air ticket value
                sheet.Columns[15].ColumnWidth = 15; // Total settlement
                sheet.Columns[16].ColumnWidth = 38; // Remarks

                using (MemoryStream ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }

        private static void FillEmployeeRow(IWorksheet sheet, int row, HrPrStaffLvTktEncashVw emp)
        {
            sheet.Range[row, 1].Text = emp.EmpId;
            sheet.Range[row, 2].Text = emp.CourtesyTitle ?? "Mr.";
            sheet.Range[row, 3].Text = emp.FullName;
            sheet.Range[row, 4].Text = emp.Department;
            sheet.Range[row, 5].Text = emp.Designation;
            sheet.Range[row, 6].Text = emp.JoinedDateStr;
            sheet.Range[row, 7].Text = emp.VacationReturnDateStr;
            sheet.Range[row, 8].Text = emp.Nationality;
            sheet.Range[row, 9].Text = emp.NextResetDateStr;

            sheet.Range[row, 10].Number = emp.ApplicableDays;
            sheet.Range[row, 10].NumberFormat = "#,##0";
            sheet.Range[row, 10].HorizontalAlignment = ExcelHAlign.HAlignCenter;

            sheet.Range[row, 11].Number = (double)emp.EligibleLeaveDays;
            sheet.Range[row, 11].NumberFormat = "0.#####";
            sheet.Range[row, 11].HorizontalAlignment = ExcelHAlign.HAlignRight;

            sheet.Range[row, 12].Number = emp.NetLeaveDays;
            sheet.Range[row, 12].NumberFormat = "#,##0";
            sheet.Range[row, 12].HorizontalAlignment = ExcelHAlign.HAlignCenter;

            sheet.Range[row, 13].Number = (double)emp.MonthlySalary;
            sheet.Range[row, 13].NumberFormat = "#,##0.00";
            sheet.Range[row, 13].HorizontalAlignment = ExcelHAlign.HAlignRight;

            if (emp.TotalLeaveSalary > 0)
            {
                sheet.Range[row, 14].Number = (double)emp.TotalLeaveSalary;
                sheet.Range[row, 14].NumberFormat = "#,##0.00";
            }
            else
            {
                sheet.Range[row, 14].Text = "-";
                sheet.Range[row, 14].HorizontalAlignment = ExcelHAlign.HAlignCenter;
            }

            sheet.Range[row, 15].Number = (double)emp.AirTicketValue;
            sheet.Range[row, 15].NumberFormat = "#,##0.00";
            sheet.Range[row, 15].HorizontalAlignment = ExcelHAlign.HAlignRight;

            sheet.Range[row, 16].Number = (double)emp.TotalSettlement;
            sheet.Range[row, 16].NumberFormat = "#,##0.00";
            sheet.Range[row, 16].HorizontalAlignment = ExcelHAlign.HAlignRight;

            sheet.Range[row, 17].Text = emp.Remarks;

            for (int c = 1; c <= 17; c++)
            {
                sheet.Range[row, c].BorderAround(ExcelLineStyle.Thin, Color.LightGray);
                sheet.Range[row, c].CellStyle.Font.Size = 9;
                sheet.Range[row, c].CellStyle.Font.FontName = "Calibri";
                sheet.Range[row, c].VerticalAlignment = ExcelVAlign.VAlignCenter;
            }
        }
    }
}

