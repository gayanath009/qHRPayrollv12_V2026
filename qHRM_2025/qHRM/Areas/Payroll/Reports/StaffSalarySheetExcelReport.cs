using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using qHRM.Areas.Payroll.Models;
using qHRM.SharedSysFuncs;
using Syncfusion.Drawing;
using Syncfusion.XlsIO;

namespace qHRM.Areas.Payroll.Reports
{
    public static class StaffSalarySheetExcelReport
    {
        public static byte[] GenerateReport(
            string companyName,
            int year,
            int month,
            IEnumerable<PrSlryShtVw> displayEmployees,
            decimal internalStaffNet,
            decimal outsourcedStaffNet,
            int sctn)
        {
            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Excel2016;

                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet sheet = workbook.Worksheets[0];
                sheet.Name = "Salary Sheet";
                sheet.IsGridLinesVisible = true;

                string compTitle = !string.IsNullOrWhiteSpace(companyName) ? companyName : "Qatar Computer Services WLL";
                string monthName = (month >= 1 && month <= 12)
                    ? new DateTime(year > 0 ? year : 2026, month, 1).ToString("MMMM")
                    : "September";

                string categoryPrefix;
                if (sctn == 0)
                    categoryPrefix = "In-House Staff ";
                else if (sctn == 1)
                    categoryPrefix = "Outsourced Staff ";
                else
                    categoryPrefix = "Staff ";

                string reportSubtitle = $"{categoryPrefix}Salary Sheet for the Month of {monthName} - {year}";

                // Row 1: Company Title
                sheet.Range["A1:X1"].Merge();
                sheet.Range["A1"].Text = compTitle;
                sheet.Range["A1"].CellStyle.Font.Bold = true;
                sheet.Range["A1"].CellStyle.Font.Size = 15;
                sheet.Range["A1"].CellStyle.Font.FontName = "Calibri";
                sheet.Range["A1"].HorizontalAlignment = ExcelHAlign.HAlignCenter;
                sheet.Range["A1"].VerticalAlignment = ExcelVAlign.VAlignCenter;
                sheet.SetRowHeight(1, 26);

                // Row 2: Subtitle
                sheet.Range["A2:X2"].Merge();
                sheet.Range["A2"].Text = reportSubtitle;
                sheet.Range["A2"].CellStyle.Font.Bold = true;
                sheet.Range["A2"].CellStyle.Font.Size = 12;
                sheet.Range["A2"].CellStyle.Font.FontName = "Calibri";
                sheet.Range["A2"].HorizontalAlignment = ExcelHAlign.HAlignCenter;
                sheet.Range["A2"].VerticalAlignment = ExcelVAlign.VAlignCenter;
                sheet.SetRowHeight(2, 22);

                // Row 4 & 5: Header Setup
                sheet.SetRowHeight(4, 18);
                sheet.SetRowHeight(5, 18);

                // Merge vertical headers (Row 4 & 5)
                void SetHeaderCell(string range, string text, ExcelHAlign align = ExcelHAlign.HAlignCenter)
                {
                    IRange r = sheet.Range[range];
                    r.Merge();
                    r.Text = text;
                    r.CellStyle.Font.Bold = true;
                    r.CellStyle.Font.Size = 9.5;
                    r.CellStyle.Font.FontName = "Calibri";
                    r.CellStyle.Color = Color.FromArgb(242, 244, 247);
                    r.HorizontalAlignment = align;
                    r.VerticalAlignment = ExcelVAlign.VAlignCenter;
                    r.BorderAround(ExcelLineStyle.Thin, Color.Black);
                    r.WrapText = true;
                }

                void SetSubHeaderCell(string cell, string text, ExcelHAlign align = ExcelHAlign.HAlignCenter)
                {
                    IRange r = sheet.Range[cell];
                    r.Text = text;
                    r.CellStyle.Font.Bold = true;
                    r.CellStyle.Font.Size = 9;
                    r.CellStyle.Font.FontName = "Calibri";
                    r.CellStyle.Color = Color.FromArgb(242, 244, 247);
                    r.HorizontalAlignment = align;
                    r.VerticalAlignment = ExcelVAlign.VAlignCenter;
                    r.BorderAround(ExcelLineStyle.Thin, Color.Black);
                    r.WrapText = true;
                }

                SetHeaderCell("A4:A5", "S. No");
                SetHeaderCell("B4:B5", "Code");
                SetHeaderCell("C4:C5", "Name");
                SetHeaderCell("D4:D5", "Designation");
                SetHeaderCell("E4:E5", "Absent");
                SetHeaderCell("F4:F5", "Basic Salary");
                SetHeaderCell("G4:G5", "Housing");
                SetHeaderCell("H4:H5", "Trans.");

                // Allowance Group
                SetHeaderCell("I4:L4", "Allowance");
                SetSubHeaderCell("I5", "Posit.");
                SetSubHeaderCell("J5", "Mobile");
                SetSubHeaderCell("K5", "Project");
                SetSubHeaderCell("L5", "Other");

                SetHeaderCell("M4:M5", "Total Fixed Salary");
                SetHeaderCell("N4:N5", "Incent");
                SetHeaderCell("O4:O5", "OT");
                SetHeaderCell("P4:P5", "Other Payment");
                SetHeaderCell("Q4:Q5", "Gross Salary");

                // Deductions Group
                SetHeaderCell("R4:U4", "Deductions");
                SetSubHeaderCell("R5", "Sl Adv / Loan");
                SetSubHeaderCell("S5", "No Pay");
                SetSubHeaderCell("T5", "Other Deduct");
                SetSubHeaderCell("U5", "Total");

                // Net Salary Group
                SetHeaderCell("V4:W4", "Net Salary");
                SetSubHeaderCell("V5", "Bank Transfer");
                SetSubHeaderCell("W5", "Cash Payment");

                SetHeaderCell("X4:X5", "Remarks");

                // Data Rows
                int curRow = 6;
                int sNo = 1;
                var list = displayEmployees?.ToList() ?? new List<PrSlryShtVw>();

                decimal totBasic = 0, totHousing = 0, totTrans = 0, totPosit = 0, totMobile = 0, totProj = 0, totOtherAlwnc = 0;
                decimal totFixed = 0, totIncent = 0, totOt = 0, totOtherPmt = 0, totGross = 0;
                decimal totSlAdvLoan = 0, totNoPay = 0, totOtherDeduct = 0, totDeductions = 0;
                decimal totBank = 0, totCash = 0;

                string numberFormat = "#,##0.00";

                foreach (var emp in list)
                {
                    decimal basic = emp.Basic;
                    decimal housing = emp.HrAlwnc;
                    decimal trans = emp.TrnsAlwnc;
                    decimal posit = emp.PstnAlwnc;
                    decimal mobile = emp.MobAlwnc;
                    decimal proj = emp.ProjAlwnc;
                    decimal otherAlwnc = emp.OthrAlwnc + emp.MedAlwnc + emp.SpcAlwnc;
                    decimal totalFixed = basic + housing + trans + posit + mobile + proj + otherAlwnc;
                    decimal incent = emp.IncntvAmt;
                    decimal ot = emp.OtAmt;
                    decimal otherPmt = emp.OthrPmt + emp.BonusAmt;
                    decimal gross = emp.GrsSlry > 0 ? emp.GrsSlry : (totalFixed + incent + ot + otherPmt);
                    decimal slAdvLoan = emp.SlryAdv + emp.StfLnPmt;
                    decimal noPay = emp.NoPayAmt;
                    decimal otherDeduct = emp.OthrRcvryAmt;
                    decimal totalDeduct = emp.TotDctAmt > 0 ? emp.TotDctAmt : (slAdvLoan + noPay + otherDeduct);
                    decimal bank = emp.PmtMod == 1 ? emp.NtSlry : (emp.BnkAmt > 0 ? emp.BnkAmt : 0);
                    decimal cash = emp.PmtMod == 0 ? emp.NtSlry : (emp.CshAmt > 0 ? emp.CshAmt : 0);

                    totBasic += basic;
                    totHousing += housing;
                    totTrans += trans;
                    totPosit += posit;
                    totMobile += mobile;
                    totProj += proj;
                    totOtherAlwnc += otherAlwnc;
                    totFixed += totalFixed;
                    totIncent += incent;
                    totOt += ot;
                    totOtherPmt += otherPmt;
                    totGross += gross;
                    totSlAdvLoan += slAdvLoan;
                    totNoPay += noPay;
                    totOtherDeduct += otherDeduct;
                    totDeductions += totalDeduct;
                    totBank += bank;
                    totCash += cash;

                    sheet.Range[curRow, 1].Number = sNo;
                    sheet.Range[curRow, 1].HorizontalAlignment = ExcelHAlign.HAlignCenter;

                    sheet.Range[curRow, 2].Text = emp.EmpId ?? "";
                    sheet.Range[curRow, 2].HorizontalAlignment = ExcelHAlign.HAlignCenter;

                    sheet.Range[curRow, 3].Text = emp.FullName ?? "";
                    sheet.Range[curRow, 3].CellStyle.Font.Bold = true; // Bold employee name
                    sheet.Range[curRow, 3].HorizontalAlignment = ExcelHAlign.HAlignLeft;

                    sheet.Range[curRow, 4].Text = emp.DesigName ?? "";
                    sheet.Range[curRow, 4].HorizontalAlignment = ExcelHAlign.HAlignLeft;

                    sheet.Range[curRow, 5].Number = (double)emp.AbFct;
                    sheet.Range[curRow, 5].HorizontalAlignment = ExcelHAlign.HAlignCenter;

                    void SetNum(int col, decimal val)
                    {
                        IRange c = sheet.Range[curRow, col];
                        c.Number = (double)val;
                        c.NumberFormat = numberFormat;
                        c.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    }

                    SetNum(6, basic);
                    SetNum(7, housing);
                    SetNum(8, trans);
                    SetNum(9, posit);
                    SetNum(10, mobile);
                    SetNum(11, proj);
                    SetNum(12, otherAlwnc);
                    SetNum(13, totalFixed);
                    SetNum(14, incent);
                    SetNum(15, ot);
                    SetNum(16, otherPmt);
                    SetNum(17, gross);
                    SetNum(18, slAdvLoan);
                    SetNum(19, noPay);
                    SetNum(20, otherDeduct);
                    SetNum(21, totalDeduct);
                    SetNum(22, bank);
                    SetNum(23, cash);

                    sheet.Range[curRow, 24].Text = emp.Rmks ?? "";
                    sheet.Range[curRow, 24].HorizontalAlignment = ExcelHAlign.HAlignLeft;

                    // Row style: 8.5pt Calibri, thin borders
                    IRange rowRange = sheet.Range[curRow, 1, curRow, 24];
                    rowRange.CellStyle.Font.Size = 9;
                    rowRange.CellStyle.Font.FontName = "Calibri";
                    rowRange.VerticalAlignment = ExcelVAlign.VAlignCenter;
                    rowRange.BorderInside(ExcelLineStyle.Thin, Color.FromArgb(200, 200, 200));
                    rowRange.BorderAround(ExcelLineStyle.Thin, Color.Black);
                    sheet.SetRowHeight(curRow, 19);

                    curRow++;
                    sNo++;
                }

                // Totals Row
                int totRowIdx = curRow;
                sheet.Range[totRowIdx, 1, totRowIdx, 5].Merge();
                sheet.Range[totRowIdx, 1].Text = "Total";
                sheet.Range[totRowIdx, 1].CellStyle.Font.Bold = true;
                sheet.Range[totRowIdx, 1].HorizontalAlignment = ExcelHAlign.HAlignCenter;

                void SetTotNum(int col, decimal val)
                {
                    IRange c = sheet.Range[totRowIdx, col];
                    c.Number = (double)val;
                    c.NumberFormat = numberFormat;
                    c.HorizontalAlignment = ExcelHAlign.HAlignRight;
                }

                SetTotNum(6, totBasic);
                SetTotNum(7, totHousing);
                SetTotNum(8, totTrans);
                SetTotNum(9, totPosit);
                SetTotNum(10, totMobile);
                SetTotNum(11, totProj);
                SetTotNum(12, totOtherAlwnc);
                SetTotNum(13, totFixed);
                SetTotNum(14, totIncent);
                SetTotNum(15, totOt);
                SetTotNum(16, totOtherPmt);
                SetTotNum(17, totGross);
                SetTotNum(18, totSlAdvLoan);
                SetTotNum(19, totNoPay);
                SetTotNum(20, totOtherDeduct);
                SetTotNum(21, totDeductions);
                SetTotNum(22, totBank);
                SetTotNum(23, totCash);

                IRange totRange = sheet.Range[totRowIdx, 1, totRowIdx, 24];
                totRange.CellStyle.Font.Bold = true;
                totRange.CellStyle.Font.Size = 9.5;
                totRange.CellStyle.Font.FontName = "Calibri";
                totRange.CellStyle.Color = Color.FromArgb(248, 249, 250);
                totRange.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                totRange.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Double;
                totRange.BorderInside(ExcelLineStyle.Thin, Color.FromArgb(200, 200, 200));
                sheet.SetRowHeight(totRowIdx, 22);

                curRow += 2; // Blank row separation

                // Summaries Table (aligned with columns T:W, matching Image 3)
                int smryRow = curRow;
                decimal grandTotalNet = internalStaffNet + outsourcedStaffNet;

                void SetSmryRow(int rIdx, string title, decimal amount, bool isBold = false)
                {
                    sheet.Range[rIdx, 20, rIdx, 22].Merge();
                    IRange tRange = sheet.Range[rIdx, 20];
                    tRange.Text = title;
                    tRange.CellStyle.Font.Bold = isBold;
                    tRange.CellStyle.Font.Size = 9.5;
                    tRange.CellStyle.Font.FontName = "Calibri";
                    tRange.HorizontalAlignment = ExcelHAlign.HAlignLeft;
                    tRange.VerticalAlignment = ExcelVAlign.VAlignCenter;

                    IRange vRange = sheet.Range[rIdx, 23];
                    vRange.Number = (double)amount;
                    vRange.NumberFormat = numberFormat;
                    vRange.CellStyle.Font.Bold = isBold;
                    vRange.CellStyle.Font.Size = 9.5;
                    vRange.CellStyle.Font.FontName = "Calibri";
                    vRange.HorizontalAlignment = ExcelHAlign.HAlignRight;
                    vRange.VerticalAlignment = ExcelVAlign.VAlignCenter;

                    IRange fullR = sheet.Range[rIdx, 20, rIdx, 23];
                    fullR.BorderAround(ExcelLineStyle.Thin, Color.Black);
                    fullR.CellStyle.Color = isBold ? Color.FromArgb(242, 244, 247) : Color.White;
                    sheet.SetRowHeight(rIdx, 19);
                }

                SetSmryRow(smryRow, "Internal Staff Total Net Salary", internalStaffNet, false);
                SetSmryRow(smryRow + 1, "Outsourced Staff Total Net Salary", outsourcedStaffNet, false);
                SetSmryRow(smryRow + 2, "Total Net Salary (QR)", grandTotalNet, true);

                // Amount in Words
                int wordsRow = smryRow + 4;
                sheet.Range[wordsRow, 1, wordsRow, 24].Merge();
                string amountInWords;
                try
                {
                    amountInWords = "Qatari Riyal " + (grandTotalNet > 0 ? NumberToWords.convert(Convert.ToInt32(grandTotalNet)) : "Zero");
                }
                catch
                {
                    amountInWords = $"Qatari Riyal {grandTotalNet:N2}";
                }
                IRange wordsCell = sheet.Range[wordsRow, 1];
                wordsCell.Text = amountInWords;
                wordsCell.CellStyle.Font.Bold = true;
                wordsCell.CellStyle.Font.Size = 10;
                wordsCell.CellStyle.Font.FontName = "Calibri";
                wordsCell.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                wordsCell.VerticalAlignment = ExcelVAlign.VAlignCenter;
                sheet.SetRowHeight(wordsRow, 22);

                // Column Widths
                sheet.SetColumnWidth(1, 6);   // S. No
                sheet.SetColumnWidth(2, 11);  // Code
                sheet.SetColumnWidth(3, 26);  // Name
                sheet.SetColumnWidth(4, 26);  // Designation
                sheet.SetColumnWidth(5, 8);   // Absent
                sheet.SetColumnWidth(6, 13);  // Basic Salary
                sheet.SetColumnWidth(7, 11);  // Housing
                sheet.SetColumnWidth(8, 10);  // Trans.
                sheet.SetColumnWidth(9, 10);  // Posit.
                sheet.SetColumnWidth(10, 10); // Mobile
                sheet.SetColumnWidth(11, 11); // Project
                sheet.SetColumnWidth(12, 10); // Other
                sheet.SetColumnWidth(13, 15); // Total Fixed
                sheet.SetColumnWidth(14, 11); // Incent
                sheet.SetColumnWidth(15, 8);  // OT
                sheet.SetColumnWidth(16, 13); // Other Pmt
                sheet.SetColumnWidth(17, 14); // Gross
                sheet.SetColumnWidth(18, 13); // Sl Adv / Loan
                sheet.SetColumnWidth(19, 10); // No Pay
                sheet.SetColumnWidth(20, 12); // Other Deduct
                sheet.SetColumnWidth(21, 12); // Total Deduct
                sheet.SetColumnWidth(22, 14); // Bank Transfer
                sheet.SetColumnWidth(23, 13); // Cash Payment
                sheet.SetColumnWidth(24, 30); // Remarks

                byte[] bytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    bytes = ms.ToArray();
                }

                workbook.Close();
                return bytes;
            }
        }
    }
}
