using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Areas.Employee.Models;
using qHRM.Models;

namespace qHRM.Data
{
    public class LoginContext
    {
        private readonly qHRMasterContext _hrmContext;

        public LoginContext(qHRMasterContext hrmContext)
        {
            this._hrmContext = hrmContext;
        }

        public IEnumerable<Login> GetLoginSystem(string feild, int usrId, string usrNme, string usrPwd)
        {
            try
            {
                string sp = "[ADM_User_Select] {0},{1},{2},{3}";
                object[] param = { feild, usrId, usrNme, usrPwd };
                var list = new List<Login>();
                list = _hrmContext.Login.FromSqlRaw(sp, param).ToList();
                return list;
            }
            catch (Exception ex) { throw ex; }
        }

        /// <summary>
        /// Asynchronous query to fetch Login data via stored procedure
        /// </summary>
        public async Task<List<Login>> GetLoginSystemAsync(string feild, int usrId, string usrNme, string usrPwd)
        {
            try
            {
                string sp = "[ADM_User_Select] {0},{1},{2},{3}";
                object[] param = { feild, usrId, usrNme, usrPwd };
                return await _hrmContext.Login.FromSqlRaw(sp, param).ToListAsync();
            }
            catch (Exception ex) { throw ex; }
        }

        /// <summary>
        /// Retrieves the active user record directly from ADM_User by username (case-insensitive)
        /// </summary>
        public AdmUser GetAdmUserByUsername(string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName)) return null;
                var trimmed = userName.Trim();
                var trimmedLower = trimmed.ToLower();
                return _hrmContext.AdmUser.AsNoTracking().FirstOrDefault(u => (u.UsrName == trimmed || u.UsrName == trimmedLower) && u.RecStat == 1);
            }
            catch (Exception ex) { throw ex; }
        }

        /// <summary>
        /// Asynchronous retrieval of active user record from ADM_User by username
        /// </summary>
        public async Task<AdmUser> GetAdmUserByUsernameAsync(string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName)) return null;
                var trimmed = userName.Trim();
                var trimmedLower = trimmed.ToLower();
                return await _hrmContext.AdmUser.AsNoTracking().FirstOrDefaultAsync(u => (u.UsrName == trimmed || u.UsrName == trimmedLower) && u.RecStat == 1);
            }
            catch (Exception ex) { throw ex; }
        }

        /// <summary>
        /// Updates the encrypted/hashed password for an ADM_User
        /// </summary>
        public bool UpdateAdmUserPassword(int usrId, string hashedPassword)
        {
            try
            {
                var user = _hrmContext.AdmUser.FirstOrDefault(u => u.UsrId == usrId);
                if (user != null)
                {
                    user.Password = hashedPassword;
                    _hrmContext.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex) { throw ex; }
        }

        /// <summary>
        /// Asynchronous password hash update for an ADM_User
        /// </summary>
        public async Task<bool> UpdateAdmUserPasswordAsync(int usrId, string hashedPassword)
        {
            try
            {
                var user = await _hrmContext.AdmUser.FirstOrDefaultAsync(u => u.UsrId == usrId);
                if (user != null)
                {
                    user.Password = hashedPassword;
                    await _hrmContext.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex) { throw ex; }
        }

        /// <summary>
        /// Retrieves the employee record directly from HR_EmpGenInfo by EmpSysId
        /// </summary>
        public async Task<HrEmpGenInfo> GetEmpBySysIdAsync(int empSysId)
        {
            try
            {
                return await _hrmContext.HrEmpGenInfo.AsNoTracking().FirstOrDefaultAsync(e => e.EmpSysId == empSysId);
            }
            catch
            {
                return null;
            }
        }
    }
}
