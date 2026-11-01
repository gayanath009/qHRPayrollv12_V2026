using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrLvEncsh
    {
        public int LvEncshId { get; set; }
        public int EmpSysId { get; set; }
        public int PlcyId { get; set; }
        public string RefTyp { get; set; }
        public int RefId { get; set; }
        public bool IncldLstMnthSlry { get; set; }
        public bool IncldPrjAlwnc { get; set; }
        public bool IncldTktFee { get; set; }
        public string FrmDtTyp { get; set; }
        public DateTime FrmDt { get; set; }
        public DateTime ToDt { get; set; }
        public int StatIndx { get; set; }
        public string Rmks { get; set; }
        public int EftWrkDys { get; set; }
        public int NpCnt { get; set; }
        public int ErndLvCnt { get; set; }
        public decimal GrsSlry { get; set; }
        public decimal LvSlry { get; set; }
        public decimal LstMntSlry { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
