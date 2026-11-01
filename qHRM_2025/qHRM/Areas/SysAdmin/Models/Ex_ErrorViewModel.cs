using System;

namespace qHRM.Areas.SysAdmin.Models
{
    public class Ex_ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
