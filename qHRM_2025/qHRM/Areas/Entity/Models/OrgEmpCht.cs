using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Entity.Models
{
    public class OrgEmpCht
    {
        [Key]
        public string EmpId { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public string RptMngId { get; set; }
        public string Image { get; set; }
    }
}
