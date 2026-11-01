using System;
using System.Collections.Generic;

namespace qHRM.Areas.SysAdmin.Models
{
    public partial class SysError
    {
        public int Id { get; set; }
        public string ErrCode { get; set; }
        public string ErrName { get; set; }
        public DateTime? ModDateTime { get; set; }
    }
}
