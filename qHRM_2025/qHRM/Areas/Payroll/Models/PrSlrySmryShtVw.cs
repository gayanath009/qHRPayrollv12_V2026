namespace qHRM.Areas.Payroll.Models
{
    public class PrSlrySmryShtVw
    {
        public int Index { get; set; }
        public string RptHdr { get; set; }
        public string RptTtl { get; set; }
        public bool InHouse { get; set; }
        public decimal IntlStfSlryAmt { get; set; }
        public decimal OtscdStfSlryAmt { get; set; }
        public decimal ttlStfSlryAmt { get; set; }
        public string AmtTxt { get; set; }
    }
}
