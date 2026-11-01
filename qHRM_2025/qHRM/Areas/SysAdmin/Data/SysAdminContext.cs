using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Models;

namespace qHRM.Areas.SysAdmin.Data
{
    public class SysAdminContext
    {
        #region ==============Member Variables==============
        private readonly qHRMasterContext _hrmContext;
        #endregion

        #region ==============Constructors==============
        public SysAdminContext(qHRMasterContext hrmContext)
        {
            this._hrmContext = hrmContext;
        }
        #endregion

        #region ==============AdmEntityUser==============
        public IEnumerable<AdmEntityUser> Get()        
        {
            try
            {                           
                return _hrmContext.AdmEntityUser.ToList();                
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<AdmEntityUser> Get(int usrId)
        {
            try
            {
                return _hrmContext.AdmEntityUser.Where(x => x.UsrId == usrId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public int Insert(AdmEntityUser admEntUsr)
        {
            try
            {
                _hrmContext.AdmEntityUser.Add(admEntUsr);
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }

        public int Update(AdmEntityUser admEntUsr)
        {
            try
            {                
                var varChanges = _hrmContext.AdmEntityUser.Attach(admEntUsr);
                varChanges.State = EntityState.Modified;
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }

        public int Delete(AdmEntityUser admEntUsr)
        {
            try
            {
                _hrmContext.Remove(admEntUsr);
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============AdmUser==============
        public IEnumerable<AdmUserView> GetView(string feild, int usrId, string usrName, string pwd)
        {
            try
            {
                string sp = "[ADM_User_Select] {0},{1},{2},{3}";
                object[] param = { feild, usrId, usrName, pwd };
                var list = new List<AdmUserView>();
                list = _hrmContext.AdmUserView.FromSqlRaw(sp, param).ToList();
                return list;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<AdmUserLogsView> GetUserActivityLog(DateTime vdate, int? usrId = null)
        {
            try
            {
                var query = _hrmContext.AdmUserLogs.Where(x => x.CrtDateTime.Date == vdate.Date);
                if (usrId.HasValue)
                {
                    query = query.Where(x => x.UsrId == usrId.Value);
                }

                var list = query
                       .Join(_hrmContext.AdmUser, a => a.UsrId, b => b.UsrId, (a, b) => new { a, b })
                       .Join(_hrmContext.HrEmpGenInfo, c => c.b.EmpSysId, d => d.EmpSysId, (c, d) => new { c, d })
                        .Select(x => new AdmUserLogsView
                        {
                            EmpName = x.d.FullName,
                            InTime = x.c.a.InTime.Value == null ? "" : x.c.a.InTime.Value.ToShortTimeString(),
                            OutTime = x.c.a.OutTime.Value == null ? "" : x.c.a.OutTime.Value.ToShortTimeString(),
                            LogDate = x.c.a.CrtDateTime.ToString("dd/MM/yyyy")
                        });
                return list;
            }
            catch (Exception ex) { throw ex; }
        }

        public int Insert(AdmUser admUsr)
        {
            try
            {
                _hrmContext.AdmUser.Add(admUsr);
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }

        public int Update(AdmUser admUsr)
        {
            try
            {
                var existing = _hrmContext.AdmUser.FirstOrDefault(u => u.UsrId == admUsr.UsrId);
                if (existing != null)
                {
                    existing.UsrName = admUsr.UsrName;
                    existing.GrpId = admUsr.GrpId;
                    existing.EmpSysId = admUsr.EmpSysId;
                    existing.Remarks = admUsr.Remarks;
                    existing.RecStat = admUsr.RecStat;
                    if (!string.IsNullOrEmpty(admUsr.Password))
                    {
                        existing.Password = admUsr.Password;
                    }
                    _hrmContext.SaveChanges();
                    return 1;
                }
                return 0;
            }
            catch (Exception ex) { throw ex; }
        }

        public AdmUser GetAdmUser(int usrId)
        {
            try
            {
                return _hrmContext.AdmUser.FirstOrDefault(u => u.UsrId == usrId && u.RecStat == 1);
            }
            catch (Exception ex) { throw ex; }
        }

        public AdmUser GetAdmUserByUsername(string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName)) return null;
                var trimmed = userName.Trim();
                var trimmedLower = trimmed.ToLower();
                return _hrmContext.AdmUser.FirstOrDefault(u => (u.UsrName == trimmed || u.UsrName == trimmedLower) && u.RecStat == 1);
            }
            catch (Exception ex) { throw ex; }
        }

        public bool UpdatePassword(int usrId, string encryptedPassword)
        {
            try
            {
                var existing = _hrmContext.AdmUser.FirstOrDefault(u => u.UsrId == usrId);
                if (existing != null)
                {
                    existing.Password = encryptedPassword;
                    _hrmContext.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex) { throw ex; }
        }

        public int Delete(AdmUser admUsr)
        {
            try
            {
                _hrmContext.Remove(admUsr);
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }

        public int DeleteUser(int usrId)
        {
            using var transaction = _hrmContext.Database.BeginTransaction();
            try
            {
                // 1. Remove entity user assignments
                var entUsers = _hrmContext.AdmEntityUser.Where(x => x.UsrId == usrId).ToList();
                if (entUsers.Any())
                {
                    _hrmContext.AdmEntityUser.RemoveRange(entUsers);
                }

                // 2. Remove user activity logs
                var userLogs = _hrmContext.AdmUserLogs.Where(x => x.UsrId == usrId).ToList();
                if (userLogs.Any())
                {
                    _hrmContext.AdmUserLogs.RemoveRange(userLogs);
                }

                // 3. Find and remove AdmUser
                var admUsr = _hrmContext.AdmUser.FirstOrDefault(x => x.UsrId == usrId);
                if (admUsr != null)
                {
                    _hrmContext.AdmUser.Remove(admUsr);
                }

                _hrmContext.SaveChanges();
                transaction.Commit();
                return 1;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw ex;
            }
        }

        public IEnumerable<AdmUserLogs> GetLoggedUserList()
        {
            try
            {
                var list = _hrmContext.AdmUserLogs.ToList();
                return list;
            }
            catch (Exception ex) { throw ex; }
        }

        public int LogUserLogin(AdmUserLogs admUsrLogs)
        {
            try
            {
                _hrmContext.AdmUserLogs.Add(admUsrLogs);
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============AdmUserGroup==============
        public partial class AdmUsrGrp
        {
            public int GpId { get; set; }
            public string GpName { get; set; }
        }

        public List<AdmUserGroup> GetAdmUserGroups(string feild, int grpId)
        {
            try
            {
                string sp = "[ADM_UserGroup_Select] {0},{1}";
                object[] param = { feild, grpId };
                var list = _hrmContext.AdmUserGroup.FromSqlRaw(sp, param).ToList();
                return list;
            }
            catch (Exception ex) { throw ex; }
        }

        public AdmUserGroup GetAdmUserGroupByEmpSysId(int empSysId) {
            try
            {
                var x = _hrmContext.AdmUserGroup
                        .Join(_hrmContext.AdmUser, g => g.GrpId, u => u.GrpId, (g, u) => new { g, u })
                        .Where(z => z.u.EmpSysId == empSysId)
                        .Select(x => x.g);
                return x.FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<AdmUsrGrp> GetFltrdAdmUsrGrps()
        {
            try
            {
                IEnumerable<AdmUsrGrp> objFiltered = from Context in _hrmContext.AdmUserGroup
                                                     where Context.RecStat == 1
                                                     select new AdmUsrGrp
                                                     {
                                                         GpId = Context.GrpId,
                                                         GpName = Context.Name
                                                     };
                return objFiltered;
            }
            catch (Exception ex) { throw ex; }
        }

        public int InsertAdmUserGroup(AdmUserGroup usrGrp)
        {
            try
            {
                _hrmContext.AdmUserGroup.Add(usrGrp);
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }

        public int UpdateAdmUserGroup(AdmUserGroup usrGrp)
        {
            try
            {
                var varChanges = _hrmContext.AdmUserGroup.Attach(usrGrp);
                varChanges.State = EntityState.Modified;
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }

        public int DeleteAdmUserGroup(AdmUserGroup usrGrp)
        {
            try
            {
                _hrmContext.Remove(usrGrp);
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception) { return 0; }
        }
        #endregion
    }
}
