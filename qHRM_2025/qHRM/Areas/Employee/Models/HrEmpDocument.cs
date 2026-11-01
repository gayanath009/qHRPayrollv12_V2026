using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpDocument
    {
        public int DocId { get; set; }
        public int EmpSysId { get; set; }

        [Display(Name = "Document Type")]
        [Required(ErrorMessage = "Please specify Document Type")]
        public int DocTypId { get; set; }

        [Display(Name = "Desciption")]
        [Required(ErrorMessage = "Please specify Description")]
        [StringLength(150)]
        public string Desc { get; set; }
        public byte[] ScnCopy { get; set; }

        [Required(ErrorMessage = "Please attach Document")]
        public string DocName { get; set; }
        
        [NotMapped]
        public string ex_DocName { get; set; }
        public string DocMime { get; set; }
        
        [NotMapped]
        public string ex_DocMime { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
