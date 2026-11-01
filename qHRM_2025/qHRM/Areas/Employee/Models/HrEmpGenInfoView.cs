using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpGenInfoView 
    {
        [Key]
        public int Emp_Sys_Id { get; set; }        
        public string Emp_Id { get; set; }

        [NotMapped]
        public string ex_Emp_Id { get; set; }
        public string Title { get; set; }
        public string Full_Name { get; set; }
        public DateTime DOB { get; set; }
        public DateTime Joined_Date { get; set; }
        public string Gender { get; set; }
        public string Pmt_Typ { get; set; }
        public string Pmt_Id { get; set; }
        [NotMapped]
        public string ex_Pmt_Id { get; set; }
        public DateTime Pmt_Exp_Date { get; set; }
        public string Pst_No { get; set; }
        public DateTime Pst_Exp_Date { get; set; }
        public string Loc_Name { get; set; }
        public string Grade_Name { get; set; }
        public string Group_Name { get; set; }
        public string Lv_Plcy_Name { get; set; }

        public string Nlty_Name { get; set; }
        public int Nlty_Id { get; set; }
        public string Cntry_Name { get; set; }
        public string Pst_Cntry_Name { get; set; }
        private string _photoName;
        public string Photo_Name 
        { 
            get => string.IsNullOrWhiteSpace(_photoName) ? "user.png" : _photoName; 
            set => _photoName = value; 
        }

    }
}
