using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using qHRM.Models;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Data;

namespace qHRM.Areas.SysAdmin.Data
{
    public class UserPrivilegeContext

    {
        private readonly qHRMasterContext _hrmContext;
        private SqlConnection _con;        

        public UserPrivilegeContext(qHRMasterContext hrmContext)
        {
            this._hrmContext = hrmContext;
            //this._con = con;
        }

        private void sqlClientConn()
        {
            _con = new SqlConnection(SQLContext.conString());
        }

 
        public List<SysSystemPrivilege> Get(string feild, int usrId)
        {
            try
            {
                var list = new List<SysSystemPrivilege>();
                var _feild = new SqlParameter("@Field", feild);
                var _grpId = new SqlParameter("@Grp_Id", usrId);
                object[] param = { feild, usrId };
                list = _hrmContext.SysSystemPrivilege.FromSqlRaw("ADM_Privilege_Select {0},{1}", param).ToList();
                return list;
            }
            catch (Exception ex) { throw ex; }
        }
    


    }
}
