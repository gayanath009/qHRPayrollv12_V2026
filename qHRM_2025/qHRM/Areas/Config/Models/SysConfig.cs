using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Email.Models
{
    public class SysConfig
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Email Server")]        
        [Required(ErrorMessage = "Please specify Email Server")]
        [StringLength(50)]
        public string Server { get; set; }

        [Display(Name = "Port")]
        [Required(ErrorMessage = "Please specify Port")]        
        public int Port { get; set; }

        [Display(Name = "From Email")]
        [Required(ErrorMessage = "Please specify From Email")]
        public string FromEmail { get; set; }

        [Display(Name = "Test Email")]
        [Required(ErrorMessage = "Please specify Test Email")]
        [DataType(DataType.EmailAddress)]
        [StringLength(50)]
        [EmailAddress]
        public string TestEmail { get; set; }

        [Display(Name = "Service Account ID")]
        [Required(ErrorMessage = "Please specify Service Account ID")]        
        [StringLength(50)]        
        public string ServiceAcct { get; set; }

        [Display(Name = "Service Account Password")]
        [Required(ErrorMessage = "Please specify Service Account Password")]
        [DataType(DataType.Password)]
        public string ServiceAcctPwd { get; set; }
    }
}
