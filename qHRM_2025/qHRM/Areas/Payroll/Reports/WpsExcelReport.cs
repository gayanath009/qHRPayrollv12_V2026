using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Syncfusion.XlsIO;

namespace qHRM.Areas.Payroll.Reports
{
    public class WpsEmployeeRecordDto
    {
        public int RecordSequence { get; set; }
        public string EmployeeQid { get; set; }
        public string EmployeeVisaId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeBankShortName { get; set; }
        public string EmployeeAccount { get; set; }
        public string SalaryFrequency { get; set; } = "M";
        public int NumberOfWorkingDays { get; set; }
        public decimal NetSalary { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal ExtraHours { get; set; }
        public decimal ExtraIncome { get; set; }
        public decimal Deductions { get; set; }
        public string PaymentType { get; set; }
        public string NotesComments { get; set; }
    }

    public static class WpsExcelReport
    {
        public static byte[] GenerateWpsExcel(
            string templatePath,
            int year,
            int month,
            IReadOnlyList<WpsEmployeeRecordDto> records)
        {
            if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
            {
                throw new FileNotFoundException($"WPS sample template file not found at: {templatePath}");
            }

            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Excel2016;

                using (FileStream fs = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    IWorkbook workbook = application.Workbooks.Open(fs);
                    IWorksheet sheet = workbook.Worksheets[0];

                    // a. Obtain Payer EID, Payer QID, Payer Bank Short Name, Payer IBAN from the sample file as it is
                    string employerId = sheet.Range[2, 1].DisplayText?.Trim();
                    if (string.IsNullOrWhiteSpace(employerId)) employerId = "10169600";

                    string payerEid = sheet.Range[2, 4].DisplayText?.Trim();
                    if (string.IsNullOrWhiteSpace(payerEid)) payerEid = "10169600";

                    string payerQid = sheet.Range[2, 5].DisplayText?.Trim() ?? "";

                    string payerBankShortName = sheet.Range[2, 6].DisplayText?.Trim();
                    if (string.IsNullOrWhiteSpace(payerBankShortName)) payerBankShortName = "CBQ";

                    string payerIban = sheet.Range[2, 7].DisplayText?.Trim();
                    if (string.IsNullOrWhiteSpace(payerIban)) payerIban = "QA87CBQA000000006010162429001";

                    // Update Row 2 (Batch header values)
                    sheet.Range[2, 1].Text = employerId;
                    sheet.Range[2, 2].Text = DateTime.Today.ToString("yyyyMMdd");
                    sheet.Range[2, 3].Text = DateTime.Now.ToString("HHmm");
                    sheet.Range[2, 4].Text = payerEid;
                    sheet.Range[2, 5].Text = payerQid;
                    sheet.Range[2, 6].Text = payerBankShortName;
                    sheet.Range[2, 7].Text = payerIban;
                    sheet.Range[2, 8].Text = $"{year}{month:D2}";

                    decimal totalSalaries = records != null && records.Count > 0 ? records.Sum(r => r.NetSalary) : 0m;
                    int totalRecords = records != null ? records.Count : 0;

                    sheet.Range[2, 9].Number = (double)totalSalaries;
                    sheet.Range[2, 9].NumberFormat = "0.00;[Red]0.00";

                    sheet.Range[2, 10].Number = totalRecords;
                    sheet.Range[2, 10].NumberFormat = "0;[Red]0";

                    // Remove existing sample employee rows (rows 4 onwards)
                    int existingRows = sheet.Rows.Length;
                    if (existingRows >= 4)
                    {
                        sheet.DeleteRow(4, existingRows - 3);
                    }

                    // Fill employee rows starting at row 4
                    if (records != null && records.Count > 0)
                    {
                        int currentRow = 4;
                        int seq = 1;
                        foreach (var rec in records)
                        {
                            // Col 1: Record Sequence
                            sheet.Range[currentRow, 1].Number = rec.RecordSequence > 0 ? rec.RecordSequence : seq;
                            sheet.Range[currentRow, 1].NumberFormat = "0;[Red]0";

                            // Col 2: Employee QID
                            sheet.Range[currentRow, 2].NumberFormat = "@";
                            sheet.Range[currentRow, 2].Text = rec.EmployeeQid ?? "";

                            // Col 3: Employee Visa ID
                            sheet.Range[currentRow, 3].NumberFormat = "@";
                            sheet.Range[currentRow, 3].Text = rec.EmployeeVisaId ?? "";

                            // Col 4: Employee Name
                            sheet.Range[currentRow, 4].NumberFormat = "@";
                            sheet.Range[currentRow, 4].Text = rec.EmployeeName ?? "";

                            // Col 5: Employee Bank Short Name
                            sheet.Range[currentRow, 5].NumberFormat = "@";
                            sheet.Range[currentRow, 5].Text = rec.EmployeeBankShortName ?? "";

                            // Col 6: Employee Account
                            sheet.Range[currentRow, 6].NumberFormat = "@";
                            sheet.Range[currentRow, 6].Text = rec.EmployeeAccount ?? "";

                            // Col 7: Salary Frequency
                            sheet.Range[currentRow, 7].NumberFormat = "@";
                            sheet.Range[currentRow, 7].Text = string.IsNullOrWhiteSpace(rec.SalaryFrequency) ? "M" : rec.SalaryFrequency;

                            // Col 8: Number Of Working Days
                            sheet.Range[currentRow, 8].Number = rec.NumberOfWorkingDays;
                            sheet.Range[currentRow, 8].NumberFormat = "0;[Red]0";

                            // Col 9: Net Salary
                            sheet.Range[currentRow, 9].Number = (double)rec.NetSalary;
                            sheet.Range[currentRow, 9].NumberFormat = "0.00;[Red]0.00";

                            // Col 10: Basic Salary
                            sheet.Range[currentRow, 10].Number = (double)rec.BasicSalary;
                            sheet.Range[currentRow, 10].NumberFormat = "0.00;[Red]0.00";

                            // Col 11: Extra hours
                            sheet.Range[currentRow, 11].Number = (double)rec.ExtraHours;
                            sheet.Range[currentRow, 11].NumberFormat = "0.00;[Red]0.00";

                            // Col 12: Extra Income
                            sheet.Range[currentRow, 12].Number = (double)rec.ExtraIncome;
                            sheet.Range[currentRow, 12].NumberFormat = "0.00;[Red]0.00";

                            // Col 13: Deductions
                            sheet.Range[currentRow, 13].Number = (double)rec.Deductions;
                            sheet.Range[currentRow, 13].NumberFormat = "0.00;[Red]0.00";

                            // Col 14: Payment Type
                            sheet.Range[currentRow, 14].NumberFormat = "@";
                            sheet.Range[currentRow, 14].Text = rec.PaymentType ?? "";

                            // Col 15: Notes/Comments
                            sheet.Range[currentRow, 15].Text = rec.NotesComments ?? "";

                            currentRow++;
                            seq++;
                        }
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        workbook.SaveAs(ms);
                        return ms.ToArray();
                    }
                }
            }
        }
    }
}

