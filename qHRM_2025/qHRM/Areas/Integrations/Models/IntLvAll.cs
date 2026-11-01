using System;
using System.ComponentModel.DataAnnotations;

namespace qHRM.Areas.Leave.Models
{
    public class IntLvAll
    {
		[Key]
		public int ID { get; set; }
		public int? IncidentNo { get; set; }
		public string QID { get; set; }
		public string  EmpName { get; set; }
		public string Email { get; set; }
		public string Department { get; set; }
		public string Duty_Handed_Over { get; set; }
		public string ContactNo_OnLeave { get; set; }
		public string LVType { get; set; }
		public string LVType_Sub { get; set; }
		[DataType(DataType.Date)]
		public DateTime? LV_Start_Date { get; set; }
		[DataType(DataType.Date)]
		public DateTime? LV_End_Date { get; set; }
		public int? LV_Days { get; set; }
		public int? LV_Year { get; set; }
		public int? LV_Month { get; set; }
		public string Status { get; set; }
		[DataType(DataType.Date)]
		public DateTime? LV_Initiated_Date { get; set; }
		public string LVType_Sub_Pay { get; set; }
		[DataType(DataType.Date)]
		public DateTime? LV_End_Date_Actual { get; set; }
	}
}
