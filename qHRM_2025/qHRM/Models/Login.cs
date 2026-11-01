using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Models
{
    public class Login
    {
        [Key]
        public int Emp_Sys_Id { get; set; }
        public int? Usr_Id { get; set; }        
        public string Usr_Name { get; set; }               
        public string Full_Name { get; set; }
        private string _photoName;
        public string Photo_Name 
        { 
            get => string.IsNullOrWhiteSpace(_photoName) ? "user.png" : _photoName; 
            set => _photoName = value; 
        }
        public int? Grp_Id { get; set; }
        public int? Ent_Id { get; set; }
        public string Dshbrd_Indx_Txt { get; set; }
        public int? Dept_Id { get; set; }

    }
}
