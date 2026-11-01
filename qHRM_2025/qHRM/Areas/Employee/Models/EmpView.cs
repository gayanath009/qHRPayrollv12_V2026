using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Employee.Models
{

    public partial class SbrdntEmpSysId
    {
        [Key]
        public int EmpSysId { get; set; }
    }

    public class EmpView
    {
        [Key]
        public int EmpSysId { get; set; }
        public string EmpId { get; set; }

        [DataType(DataType.Date)]     
        public DateTime JoinedDate { get; set; }

        [NotMapped]
        public string Ex_JindDtTxt { get { return JoinedDate.ToString("yyyy-MM-dd"); } }
        public int SrvcInYrs { get; set; }

        public DateTime Dob { get; set; }

        [NotMapped]
        public string Ex_DobTxt { get { return Dob.ToString("yyyy-MM-dd"); } }

        public string Title { get; set; }
        public string FullName { get; set; }
        public int DeptId { get; set; }
        public string DeptName { get; set; }
        public string Grade { get; set; }
        public int DesigId { get; set; }
        public string DesigName { get; set; }
        public string MblPhone { get; set; }
        public string HmPhone { get; set; }
        public int LocId { get; set; }
        public string LocName { get; set; }
        public string Gender { get; set; }
        public int NltyId { get; set; }
        public string NltyName { get; set; }
        public string EmpTyp { get; set; }
        public string PmtTyp { get; set; }
        public string PmtId { get; set; }
        public DateTime PmtExpDate { get; set; }
        public short CurrStat { get; set; }
        public string CurStatTxt { get; set; }

        [NotMapped]
        public string Ex_PmtExpDateTxt { get { return PmtExpDate.ToString("yyyy-MM-dd"); } }
        
        [NotMapped]
        public double Ex_Age { get { return CalculateAge(Dob); } }

        public string PstNo { get; set; }
        public DateTime PstExpDate { get; set; }
        public int WrktypId { get; set; }
        public string WrkTypName { get; set; }
        [NotMapped]
        public string Ex_PstExpDateTxt { get { return PstExpDate.ToString("yyyy-MM-dd"); } }
        private string _photoName;
        public string PhotoName 
        { 
            get => string.IsNullOrWhiteSpace(_photoName) ? "user.png" : _photoName; 
            set => _photoName = value; 
        }

        [NotMapped]
        public string Ex_JoinedDate { get { return JoinedDate.ToString("yyyy-MM-dd"); } }


        public static double CalculateAge(DateTime DOB)
        {   
            var ts = DateTime.Now - DOB;
            return  Math.Round(ts.Days / 365.0);             
        }

    }
}
