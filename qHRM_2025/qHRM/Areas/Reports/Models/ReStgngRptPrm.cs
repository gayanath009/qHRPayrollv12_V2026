namespace qHRM.Areas.Reports.Models
{
    public partial class ReStgngRptPrm
    {
        public long SsnRptId { get; set; }
        public int PrmId { get; set; }
        public string PrmVlv { get; set; }

        public virtual ReCnfgRptPrm Prm { get; set; }
        public virtual ReStgngRpt SsnRpt { get; set; }
    }
}
