using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Attendance.Models
{
    [Table("HR_AtndcRegConfig")]
    public partial class HrAtndcRegConfig
    {
        [Key]
        [Column("Config_Id")]
        public int ConfigId { get; set; }

        [Display(Name = "Maximum Allowed Window (Days)")]
        [Required]
        [Column("Max_Days_Allowed")]
        public int MaxDaysAllowed { get; set; } = 14;

        [Display(Name = "Allow Future Dates")]
        [Column("Allow_Future_Dates")]
        public bool AllowFutureDates { get; set; } = false;

        [Display(Name = "Working Days")]
        [Column("Working_Days")]
        public string WorkingDays { get; set; } = "Sunday,Monday,Tuesday,Wednesday,Thursday";

        [Column("Is_Active")]
        public bool IsActive { get; set; } = true;

        [Column("Crt_DateTime")]
        public DateTime CrtDateTime { get; set; } = DateTime.Now;

        [Column("Mdf_Id")]
        public int? MdfId { get; set; }

        [Column("Mdf_DateTime")]
        public DateTime? MdfDateTime { get; set; }
    }
}

