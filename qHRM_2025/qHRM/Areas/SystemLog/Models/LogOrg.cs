using qHRM.Areas.Employee.Models;
using System;

namespace qHRM.Areas.SystemLog.Models
{
    public partial class LogOrg
    {
        public Guid Id { get; set; }
        public int TblId { get; set; }
        public int RecId { get; set; }
        public long Ssn { get; set; }
        public string Property { get; set; }
        public short TrType { get; set; }
        public string OldVal { get; set; }
        public string NewVal { get; set; }
        public int CrtBy { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGenInfo CrtByNavigation { get; set; }
        public virtual SysTableInfo Tbl { get; set; }
    }
}
