using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpPhoto
    {
        public int EmpSysId { get; set; }
        public int PhtId { get; set; }
        public byte[] Photo { get; set; }
        public string PhotoName { get; set; }

        [NotMapped]
        [Required(ErrorMessage ="Photo is required")]
        public string ex_PhotoName { get; set; }
        public string PhotoMime { get; set; }

        [NotMapped]
        public string ex_PhotoMime { get; set; }

        [Display(Name = "Note")]        
        [StringLength(100)]
        public string Note { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
