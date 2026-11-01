using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Models;

namespace qHRM.Areas.SysAdmin.Data
{
    public class AdmUserGrpPrvContext
    {
        private readonly qHRMasterContext _hrmContext;

        public AdmUserGrpPrvContext(qHRMasterContext hrmContext)
        {
            this._hrmContext = hrmContext;
        }

        public IEnumerable<mvGroupPrivilege> Get(string feild, int grpId)
        {
            try
            {
                var list = new List<mvGroupPrivilege>();
                object[] param = { feild, grpId };
                list = _hrmContext.mvGroupPrivilege.FromSqlRaw("ADM_UserGroupPrivilege_Select {0},{1}", param).ToList();
                return list;
            }
            catch (Exception ex) { throw ex; }
        }

        public async Task<List<mvGroupPrivilege>> GetAsync(string feild, int grpId)
        {
            try
            {
                object[] param = { feild, grpId };
                return await _hrmContext.mvGroupPrivilege.FromSqlRaw("ADM_UserGroupPrivilege_Select {0},{1}", param).ToListAsync();
            }
            catch (Exception ex) { throw ex; }
        }

        public int Create(AdmUserGroupPrivilege usrGrpPrv)
        {
            try
            {
                object[] param = { usrGrpPrv.GrpId, usrGrpPrv.PrvId, usrGrpPrv.RecView, usrGrpPrv.RecAdd, usrGrpPrv.RecEdit, usrGrpPrv.RecDelete, usrGrpPrv.CrtId };

                try
                {
                    var xx = _hrmContext.mvGroupPrivilege.FromSqlRaw("ADM_UserGroupPrivilege_Insert {0},{1},{2},{3},{4},{5},{6}", param).ToList();

                }
                catch { }
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }

        public int Remove(AdmUserGroupPrivilege usrGrpPrv)
        {
            try
            {
                object[] param = { usrGrpPrv.GrpId };
                try
                {
                    var xx = _hrmContext.mvGroupPrivilege.FromSqlRaw("ADM_UserGroupPrivilege_Delete {0}", param).ToList();
                }
                catch { }
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }
    }
}