using qHRM.Areas.Employee.Models;
using qHRM.Areas.SysAdmin.Models;
using System;
using System.Collections.Generic;

namespace qHRM.Areas.SystemLog.Models
{
    public partial class LogHremp
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
