using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.General.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpDependentView
    {
        [Key]
        public int Dpnd_Id { get; set; }
        public string Rltn_Typ { get; set; }
        public string First_Name { get; set; }        
        public string Mdl_Name { get; set; }
        public string Last_Name { get; set; }        
        public string Full_Name { get; set; }        
        public string Pst_Name { get; set; }        
        public string Name_Arb { get; set; }        
        public string Title { get; set; }        
        public DateTime DOB { get; set; }        
        public string Gender { get; set; }        
        public int Nlty_Id { get; set; }
        public string Nlty_Name { get; set; }
        public byte[] Photo { get; set; }
        public string Photo_Name { get; set; }
        public string  Photo_Mime { get; set; }                
        public int Cntry_Id { get; set; }
        public string Cntry_Name { get; set; }        
        public string Mbl_Phone { get; set; }        
        public string Psn_Email { get; set; }        
        public string Bld_Grp { get; set; }        
        public string Pmt_Typ { get; set; }
        public string Pmt_Id { get; set; }

        [NotMapped]
        public string exPmtId { get; set; }
        
        public DateTime Pmt_Exp_Date { get; set; }        
        public string Pst_No { get; set; }
        
        [NotMapped]
        public string exPstNo { get; set; }
        
        public DateTime? Pst_Exp_Date { get; set; }
        
        public int Pst_Cntry_Id { get; set; }        
        public string Pst_Cntry_Name { get; set; }        
        public string Occupation { get; set; }        
        public string Institute { get; set; }
        public string Note { get; set; }
       
    }
}
