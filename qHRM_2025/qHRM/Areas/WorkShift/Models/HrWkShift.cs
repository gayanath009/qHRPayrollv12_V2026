using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Attendance.Models;
using qHRM.Areas.General.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace qHRM.Areas.WorkShift.Models
{
    public partial class HrWkShift
    {
        public HrWkShift()
        {
            HrEmpAtndcSft = new HashSet<HrEmpAtndcSft>();
            HrEmpWkShift = new HashSet<HrEmpWkShift>();
            HrWkRosterShift = new HashSet<HrWkRosterShift>();
        }

        public int SftId { get; set; }

        [Display(Name = "Shift Code")]
        [Required(ErrorMessage = "Shift Code cannot be blank.")]
        [Remote(action: "IsAlreadyExisting", controller: "HrWkShift", ErrorMessage = "Shift Code is already existing", AdditionalFields = "ex_SftCode")]
        public string SftCode { get; set; }

        [NotMapped]
        public string ex_SftCode { get; set; }

        [Display(Name = "Shift Name")]
        [Required(ErrorMessage = "Shift Name cannot be blank.")]
        public string NameEng { get; set; }
        public string NameArb { get; set; }

        [Display(Name = "Description")]
        public string Desc { get; set; }

        [Display(Name = "Start at")]
        [Required(ErrorMessage = "Enter start time")]
        [DataType(DataType.Time)]
        public DateTime StTime { get; set; }

        [Display(Name = "End at")]
        [Required(ErrorMessage = "Enter end time")]
        [DataType(DataType.Time)]
        public DateTime EndTime { get; set; }

        [Display(Name = "Grace Period (Minutes)")]
        public int GrcPrd { get; set; }

        [Display(Name = "Max Start at")]
        [DataType(DataType.Time)]
        public DateTime? CutOffTimeStart { get; set; }

        [Display(Name = "Max End at")]
        [DataType(DataType.Time)]
        public DateTime? CutOffTimeEnd { get; set; }

        //[Display(Name = "Normal OT Minutes")]
        //public int NmlOtMnt { get; set; }

        [Display(Name = "Normal OT Include")]
        public bool NOtIncld { get; set; }

        [Display(Name = "Normal OT Minimum Minutes")]
        public int NOtMinMnt { get; set; }

        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan NOtStTime { get; set; }

        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan NOtEndTime { get; set; }

        [Display(Name = "Extended OT Include")]
        public bool EOtIncld { get; set; }
        [Display(Name = "Extended OT Minimum Minutes")]
        public int EOtMinMnt { get; set; }

        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan EOtStTime { get; set; }

        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan EOtEndTime { get; set; }

        //[Display(Name = "Maximum OT Minutes")]
        //public int MaxOtMnt { get; set; }

        [Display(Name = "Over night shift")]
        public bool OvrNghtSft { get; set; }

        [Display(Name = "Off Shift")]
        public bool OffDay { get; set; }

        [Display(Name = "Weekend Shift")]
        public bool WkEnd { get; set; }

        [NotMapped]
        public string OvrNghtSft_Txt { get { if (OvrNghtSft) return "Yes"; else return "No"; } }

        [Display(Name = "Break Time")]
        public int BrkMnt { get; set; }

        public int EntId { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }
        
        public virtual OrgEntity Ent { get; set; }
        public virtual ICollection<HrEmpAtndcSft> HrEmpAtndcSft { get; set; }
        public virtual ICollection<HrEmpWkShift> HrEmpWkShift { get; set; }
        [JsonIgnore]
        public virtual ICollection<HrWkRosterShift> HrWkRosterShift { get; set; }
    }
}
