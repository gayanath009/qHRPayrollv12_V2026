using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

using qHRM.Areas.Employee.Models;
using qHRM.Areas.Entity.Data;
using qHRM.Areas.Payroll.Data;
using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static qHRM.Areas.WorkShift.Data.WorkShiftContext;

namespace qHRM.Areas.Employee.Data
{
    public class EmployeeContext
    {

        #region ==============Member Variables==============
        private readonly qHRMasterContext _hrmContext;
        #endregion 

        #region ==============Employee Configuration==============
        public HrEmpConfig GetActiveHrEmpConfig()
        {
            try
            {
                return _hrmContext.HrEmpConfig.Where(x => x.RecStat == 1).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public int SaveHrEmpConfig(HrEmpConfig hrEmpConfig, int empSysId)
        {
            try
            {
                var modifiedPropList = new List<PropView>();
                var extEmpCnfNtfLvls = new List<HrEmpCnfNtfLvl>();
                var extHrEmpConfig = _hrmContext.HrEmpConfig.Where(x => x.RecStat == 1).FirstOrDefault();

                if (extHrEmpConfig != null)
                {
                    _hrmContext.Entry(extHrEmpConfig).State = EntityState.Detached;
                    var newModelType = hrEmpConfig.GetType();
                    var extModelType = extHrEmpConfig.GetType();

                    var newModelPropList = newModelType.GetProperties();
                    var extModelPropList = extModelType.GetProperties();

                    hrEmpConfig.CrtId = extHrEmpConfig.CrtId;
                    hrEmpConfig.CrtDateTime = extHrEmpConfig.CrtDateTime;

                    extEmpCnfNtfLvls = _hrmContext.HrEmpCnfNtfLvl.Where(x => x.EmpCnfId == extHrEmpConfig.EmpCnfId && x.RecStat == 1).ToList();
                    modifiedPropList = newModelPropList.Join(extModelPropList, ex => ex.Name, nw => nw.Name, (nw, ex) => new { nw, ex })
                                            .Where(x => ((x.nw.GetValue(hrEmpConfig) != null) ? x.nw.GetValue(hrEmpConfig).ToString() : "") != ((x.ex.GetValue(extHrEmpConfig) != null) ? x.ex.GetValue(extHrEmpConfig).ToString() : ""))
                                            .Select(z => new PropView
                                            {
                                                Prop = z.nw.Name,
                                                Val = z.ex.GetValue(hrEmpConfig) != null ? z.ex.GetValue(hrEmpConfig).ToString() : ""
                                            }).ToList();
                    extHrEmpConfig.RecStat = 0;
                    _hrmContext.HrEmpConfig.Attach(extHrEmpConfig);
                    _hrmContext.Entry(extHrEmpConfig).State = EntityState.Modified;
                }
                if (modifiedPropList.Count() > 0 || extHrEmpConfig == null)
                {
                    hrEmpConfig.EmpCnfId = 0;
                    hrEmpConfig.CrtId = empSysId;
                    hrEmpConfig.CrtDateTime = DateTime.Now;
                    _hrmContext.HrEmpConfig.Add(hrEmpConfig);
                    _hrmContext.SaveChanges();
                    foreach (var extGrt in extEmpCnfNtfLvls)
                    {
                        _hrmContext.HrEmpCnfNtfLvl.Add(new HrEmpCnfNtfLvl
                        {
                            Desc = extGrt.Desc,
                            LwrBndryYrs = extGrt.LwrBndryYrs,
                            UprBndryYrs = extGrt.UprBndryYrs,
                            EftDays = extGrt.EftDays,
                            EmpCnfId = hrEmpConfig.EmpCnfId,
                            RecStat = 1,
                            CrtId = extGrt.CrtId,
                            CrtDateTime = DateTime.Now
                        });
                    }
                    _hrmContext.SaveChanges();
                    return 1;
                }
                else
                    return 0;
            }
            catch (Exception ex) { throw ex; }
        }
        #region ==============End of Service Notification==============
        public partial class HrEmpCnfNtfLvlVw
        {
            public int NtfLvlId { get; set; }
            public string Desc { get; set; }
            public int LwrBndryYrs { get; set; }
            public int UprBndryYrs { get; set; }
            public int EftDays { get; set; }
            public int EmpCnfId { get; set; }
        }

        public IEnumerable<HrEmpCnfNtfLvlVw> GetHrEmpCnfNtfLvlByEmpCnfId(int empCnfId)
        {
            try
            {
                return _hrmContext.HrEmpCnfNtfLvl
                        .Where(z => z.EmpCnfId == empCnfId && z.RecStat == 1)
                        .Select(x => new HrEmpCnfNtfLvlVw
                        {
                            NtfLvlId = x.NtfLvlId,
                            LwrBndryYrs = x.LwrBndryYrs,
                            UprBndryYrs = x.UprBndryYrs,
                            EftDays = x.EftDays,
                            EmpCnfId = x.EmpCnfId
                        });
            }
            catch (Exception ex) { throw ex; }
        }

        public int GetMaxNtfLvlByEmpCnfId(int empCnfId)
        {
            try
            {
                var hrEmpCnfNtfLvl = _hrmContext.HrEmpCnfNtfLvl.Where(z => z.EmpCnfId == empCnfId && z.RecStat == 1);
                if (hrEmpCnfNtfLvl.Count() > 0)
                    return hrEmpCnfNtfLvl.Max(x => x.UprBndryYrs);
                else
                    return 0;
            }
            catch (Exception ex) { throw ex; }
        }

        public HrEmpCnfNtfLvl GetHrEmpCnfNtfLvlById(int ntfLvlId)
        {
            try
            {
                return _hrmContext.HrEmpCnfNtfLvl.Where(x => x.NtfLvlId == ntfLvlId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        //GetHrEmpCnfNtfLvlByEmpCnfId
        #endregion ==============End of Service Notification==============
        #endregion ==============Employee Configuration==============

        #region ==============Constructors==============
        public EmployeeContext(qHRMasterContext hrmContext)
        {
            _hrmContext = hrmContext;

        }
        #endregion

        #region ==============General Info==============
        public partial class EmpSimpleView1
        {
            public int EmpSysId { get; set; }
            public string EmpDesc { get; set; }
        }

        public partial class EmpSimpleView2
        {
            public int EmpSysId { get; set; }
            public string EmpId { get; set; }
            public string PmtId { get; set; }
            public string Name { get; set; }  
        }

        public partial class HrEmpGen
        {
            public int ESysId { get; set; }
            public string FulNme { get; set; }
            public string EmpId { get; set; }
            public string DisplayText => !string.IsNullOrEmpty(EmpId) ? $"{EmpId} - {FulNme}" : FulNme;
        }

        public class EmpView2
        {
            public string EmpName { set; get; } //text
            public int EmpSysId { set; get; } //id
            public int DeptId { set; get; } //groupId
            public string Color { set; get; } //color
            public string Designation { set; get; } //designation
        }

        public partial class EmpSysIdRptMngIdVw
        {
            public int EmpSysId { get; set; }
            public int? RptMngId { get; set; }
        }

        public class EmpWkRstView
        {
            public int RecId { get; set; }
            public string EmpName { set; get; } //text
            public int EmpSysId { set; get; } //id            
            public string Color { set; get; } //color
            public string GroupId { set; get; } //designation
        }

        public partial class EmpCntSmry
        {
            public int CurTotEmpCnt { get; set; }
            public int OutSrcdEmpCnt { get; set; }
            public int InCnt { get; set; }
            public int OutCnt { get; set; }
        }

        public IEnumerable<EmpView> GetEmpView(string key, int value, int usrId)
        {
            try
            {
                string sp = "[HR_EmpGenInfo_Select2] {0},{1},{2}";
                object[] param = { key, value, usrId };
                var x = _hrmContext.EmpView.FromSqlRaw(sp, param).ToList();
                return x; //.Where(x => GetEmpSbrdnts(usrId).Contains(x.EmpSysId));
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<EmpView2> GetEmpView2s(SftSchTypes typ, int schId)
        {
            try
            {
                List<EmpView2> empView2Lst = new List<EmpView2>();
                if (typ == SftSchTypes.EMP)
                {
                    var emp = _hrmContext.HrEmpGenInfo
                                .Join(_hrmContext.HrEmpJobAssign, gi => gi.EmpSysId, jb => jb.EmpSysId, (gi, jb) => new { gi, jb })
                                .Join(_hrmContext.OrgDesignation, p => p.jb.DesigId, ds => ds.DesigId, (p, ds) => new { p, ds })
                                .Where(z => z.p.gi.EmpSysId == schId && z.p.jb.WkStat == 1)
                                .Select(x => new EmpView2
                                {
                                    EmpSysId = x.p.gi.EmpSysId,
                                    EmpName = x.p.gi.FirstName + " " + x.p.gi.LastName,
                                    DeptId = x.p.jb.DeptId,
                                    Color = "FFFFFF",
                                    Designation = x.ds.NameEng
                                }).FirstOrDefault();
                    empView2Lst.Add(emp);
                    GetChildEmpList(schId, empView2Lst);
                }
                else if (typ == SftSchTypes.DEPT)
                {
                    EntityContext entityContext = new EntityContext(_hrmContext);
                    var deptLst = entityContext.GetRecursiveDeptList(schId);

                    empView2Lst = deptLst
                                 .Join(_hrmContext.HrEmpJobAssign, dp => dp.DeptId, jb => jb.DeptId, (dp, jb) => new {dp, jb })
                                 .Join(_hrmContext.OrgDesignation, p => p.jb.DesigId, ds => ds.DesigId, (p, ds) => new { p, ds })
                                 .Join(_hrmContext.HrEmpGenInfo, q => q.p.jb.EmpSysId, emp => emp.EmpSysId, (q, emp) => new { q, emp })
                                 .Where(z => z.q.p.jb.EmpSysId == schId && z.q.p.jb.WkStat == 1)
                                 .Select(x => new EmpView2
                                 {
                                     EmpName = x.emp.FirstName + " " + x.emp.LastName,
                                     EmpSysId = x.emp.EmpSysId,
                                     DeptId = x.q.p.jb.DeptId,
                                     Color = "FFFFFF",
                                     Designation = x.q.ds.NameEng
                                 }).ToList();
                }

                return empView2Lst;
            }
            catch (Exception ex) { throw ex; }
        }

        private void GetChildEmpList(int rptMngId, List<EmpView2> empLst)
        {
            try
            {
                var tmpEmpView2 = new List<EmpView2>();
                tmpEmpView2 = _hrmContext.HrEmpJobAssign
                                .Join(_hrmContext.HrEmpGenInfo, jb => jb.EmpSysId, gi => gi.EmpSysId, (jb, gi) => new { jb, gi })
                                .Join(_hrmContext.OrgDesignation, p => p.jb.DesigId, ds => ds.DesigId, (p, ds) => new { p, ds })
                                .Where(z => z.p.jb.RptMngId == rptMngId && z.p.jb.WkStat == 1 && z.p.gi.CurrStat == 0)
                                .Select(x => new EmpView2 
                                {
                                    EmpSysId = x.p.gi.EmpSysId
                                    ,
                                    EmpName = x.p.gi.FirstName + " " + x.p.gi.LastName
                                    ,
                                    DeptId = x.p.jb.DeptId
                                    ,
                                    Color = "FFFFFF"
                                    ,
                                    Designation = x.ds.NameEng
                                }).ToList();

                foreach (var emp in tmpEmpView2)
                {
                    empLst.Add(emp);
                    GetChildEmpList(emp.EmpSysId, empLst);
                }
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<EmpView> GetEmpViewGrpByNltyRtoId(int entId, int empRtoId, int usrId)
        {
            try
            {
                string sp = "[HR_EmpGenInfo_Select2] {0},{1},{2}";
                object[] param = { "ALL", 0, usrId };
                var empLst = new List<EmpView>();
                empLst = _hrmContext.EmpView.FromSqlRaw(sp, param).ToList();

                var orgNltyEmpRatio = _hrmContext.OrgNltyEmpRatio.Where(x => x.EmpRtoId == empRtoId && x.EntId == entId).FirstOrDefault();
                if (orgNltyEmpRatio != null)
                {
                    if (orgNltyEmpRatio.NltyId != null)
                    {
                        return empLst.Where(x => x.CurrStat == 0 && x.NltyId == orgNltyEmpRatio.NltyId).ToList();
                    }
                    else 
                    {
                        return empLst.Where(e => (_hrmContext.OrgNationality.Where(z => z.NltyTypId == orgNltyEmpRatio.NltyTypId).Select(x => x.NltyId).Contains(e.NltyId)) && e.CurrStat == 0 );
                    }
                }
                else
                {
                    return new List<EmpView>();
                }
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<EmpView> GetEmpViewGrpByNltyName(int entId, string nltyName, int usrId)
        {
            try
            {
                string sp = "[HR_EmpGenInfo_Select2] {0},{1},{2}";
                object[] param = { "ALL", 0, usrId };
                var empLst = new List<EmpView>();
                empLst = _hrmContext.EmpView.FromSqlRaw(sp, param).ToList();

                var orgNationality = _hrmContext.OrgNationality.Where(x => x.NltyNameEng == nltyName).FirstOrDefault();
                if (orgNationality != null)
                {
                    return empLst.Where(x => x.CurrStat == 0 && x.NltyId == orgNationality.NltyId).ToList();                    
                }
                else
                {
                    var orgNltyType = _hrmContext.OrgNltyType.Where(x => x.NltyTypNameEng == nltyName).FirstOrDefault();

                    return empLst.Where(e => (_hrmContext.OrgNationality.Where(z => z.NltyTypId == orgNltyType.NltyTypId).Select(x => x.NltyId).Contains(e.NltyId)) && e.CurrStat == 0);
                }
            }
            catch (Exception ex) { throw ex; }
        }

        public HrEmpGenInfo GetEmpGenInfoByEmpSysId(int empSysId)
        {
            try
            {
                return _hrmContext.HrEmpGenInfo.Where(x => x.EmpSysId == empSysId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpGenInfo> GetAllEmpGenInfo()
        {
            try
            {
                return _hrmContext.HrEmpGenInfo.ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpGen> GetActvEmpNameLst(int entId)
        {
            try { 
                var x =  _hrmContext.HrEmpGenInfo
                                .Join(_hrmContext.HrEmpJobAssign, jb => jb.EmpSysId, emp => emp.EmpSysId, (emp, jb) => new { emp, jb })
                                .Join(_hrmContext.OrgDepartment, p => p.jb.DeptId, dp => dp.DeptId, (p, dp) => new { p, dp })
                                .Where(z => z.p.emp.CurrStat == 0 && z.p.jb.WkStat == 1 && z.dp.EntId == entId)
                                .Select(z => new HrEmpGen
                                {
                                    ESysId = z.p.emp.EmpSysId,
                                    FulNme = z.p.emp.FirstName + "" + (z.p.emp.MdlName == null ? "" : " " + z.p.emp.MdlName) + " " + z.p.emp.LastName,
                                    EmpId = z.p.emp.EmpId,
                                }).ToList().OrderBy(o => o.FulNme);
                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public bool GetUserShowAllEmployees(int empSysId, int? grpId = null)
        {
            try
            {
                if (IsRestrictedSelfOnlyUser(empSysId, grpId) || IsOutsourceAdmin(empSysId, grpId))
                {
                    return false;
                }

                if (grpId.HasValue && grpId.Value > 0)
                {
                    var isGlobalAll = _hrmContext.AdmUsrGrpEmpGrp.Any(x => x.GrpId == grpId.Value && x.RecStat == 1 && (x.EmpGrpId == -1 || x.EmpGrpId == -100));
                    if (isGlobalAll) return true;

                    var ug = _hrmContext.AdmUserGroup.FirstOrDefault(x => x.GrpId == grpId.Value);
                    if (ug != null && ug.ShwAlEmp && ug.GrpId != 11 && ug.GrpId != 4 && ug.GrpId != 8 && ug.GrpId != 12) // Grp 11 is BU Heads, 4/8/12 are Outsource/QCS
                        return ug.ShwAlEmp;
                }

                if (empSysId > 0)
                {
                    var usr = _hrmContext.AdmUser.FirstOrDefault(u => u.EmpSysId == empSysId && u.RecStat == 1);
                    if (usr != null && usr.GrpId > 0)
                    {
                        if (usr.GrpId == 4 || usr.GrpId == 8 || usr.GrpId == 12) return false;

                        var isGlobalAll = _hrmContext.AdmUsrGrpEmpGrp.Any(x => x.GrpId == usr.GrpId && x.RecStat == 1 && (x.EmpGrpId == -1 || x.EmpGrpId == -100));
                        if (isGlobalAll) return true;

                        var ug = _hrmContext.AdmUserGroup.FirstOrDefault(x => x.GrpId == usr.GrpId);
                        if (ug != null && ug.ShwAlEmp && ug.GrpId != 11 && ug.GrpId != 4 && ug.GrpId != 8 && ug.GrpId != 12)
                            return ug.ShwAlEmp;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsRestrictedSelfOnlyUser(int empSysId, int? grpId = null)
        {
            try
            {
                // Grp_Id = 4 (Out Source User) and Grp_Id = 12 (QCS User)
                if (grpId.HasValue && grpId.Value > 0)
                {
                    if (grpId.Value == 4 || grpId.Value == 12) return true;
                    var ug = _hrmContext.AdmUserGroup.FirstOrDefault(x => x.GrpId == grpId.Value && x.RecStat == 1);
                    if (ug != null && (ug.GrpId == 4 || ug.GrpId == 12
                        || (ug.Desc != null && (ug.Desc.Contains("Out Source User") || ug.Desc.Contains("Outsource User") || ug.Desc.Contains("QCS User")))
                        || (ug.Name != null && (ug.Name.Contains("Out Source User") || ug.Name.Contains("Outsource User") || ug.Name.Contains("QCS User")))))
                    {
                        return true;
                    }
                }

                if (empSysId > 0)
                {
                    var isDirectGrp = _hrmContext.AdmUser.Any(u => u.EmpSysId == empSysId && u.RecStat == 1 && (u.GrpId == 4 || u.GrpId == 12));
                    if (isDirectGrp) return true;

                    var isRestricted = (from usr in _hrmContext.AdmUser
                                        join ug in _hrmContext.AdmUserGroup on usr.GrpId equals ug.GrpId
                                        where usr.EmpSysId == empSysId && usr.RecStat == 1 && ug.RecStat == 1
                                           && (ug.GrpId == 4 || ug.GrpId == 12
                                               || (ug.Desc != null && (ug.Desc.Contains("Out Source User") || ug.Desc.Contains("Outsource User") || ug.Desc.Contains("QCS User")))
                                               || (ug.Name != null && (ug.Name.Contains("Out Source User") || ug.Name.Contains("Outsource User") || ug.Name.Contains("QCS User"))))
                                        select usr.UsrId).Any();
                    if (isRestricted) return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsOutsourceAdmin(int empSysId, int? grpId = null)
        {
            try
            {
                // Specifically check for Sanuja Jayanthi (EmpSysId 97)
                if (empSysId == 97) return true;

                if (grpId.HasValue && grpId.Value > 0)
                {
                    if (grpId.Value == 8) return true;
                    var ug = _hrmContext.AdmUserGroup.FirstOrDefault(x => x.GrpId == grpId.Value && x.RecStat == 1);
                    if (ug != null && (ug.GrpId == 8 
                        || (ug.Desc != null && (ug.Desc.Contains("OutSource Admin") || ug.Desc.Contains("Outsource Admin")))
                        || (ug.Name != null && (ug.Name.Contains("OutSource Admin") || ug.Name.Contains("Outsource Admin")))))
                    {
                        return true;
                    }
                }

                if (empSysId > 0)
                {
                    var isDirectGrp = _hrmContext.AdmUser.Any(u => u.EmpSysId == empSysId && u.RecStat == 1 && u.GrpId == 8);
                    if (isDirectGrp) return true;

                    var isOsAdmin = (from usr in _hrmContext.AdmUser
                                     join ug in _hrmContext.AdmUserGroup on usr.GrpId equals ug.GrpId
                                     where usr.EmpSysId == empSysId && usr.RecStat == 1 && ug.RecStat == 1
                                        && (ug.GrpId == 8 
                                            || (ug.Desc != null && (ug.Desc.Contains("OutSource Admin") || ug.Desc.Contains("Outsource Admin")))
                                            || (ug.Name != null && (ug.Name.Contains("OutSource Admin") || ug.Name.Contains("Outsource Admin"))))
                                     select usr.UsrId).Any();
                    if (isOsAdmin) return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public HashSet<int> GetOutsourceEmployeeSysIds(int? currentAdminEmpSysId = null)
        {
            try
            {
                // Find all outsource employees referring strictly to Wrktyp_Id = 2 or Grp_Id = 7 of HR_EmpGenInfo table
                var empList = _hrmContext.HrEmpGenInfo
                    .Where(emp => emp.WrktypId == 2 || emp.GrpId == 7)
                    .Select(emp => emp.EmpSysId)
                    .ToList();

                var empIds = new HashSet<int>(empList);

                // Always include Sanuja Jayanthi (EmpSysId: 97)
                empIds.Add(97);

                // Include current logged-in Outsource Admin if provided
                if (currentAdminEmpSysId.HasValue && currentAdminEmpSysId.Value > 0)
                {
                    empIds.Add(currentAdminEmpSysId.Value);
                }

                // Also include any Outsource Admin users configured in ADM_User (Grp_Id = 8)
                var osAdminEmpIds = _hrmContext.AdmUser
                    .Where(usr => usr.RecStat == 1 && usr.GrpId == 8 && usr.EmpSysId > 0)
                    .Select(usr => usr.EmpSysId)
                    .ToList();

                foreach (var adminEmpId in osAdminEmpIds)
                {
                    empIds.Add(adminEmpId);
                }

                return empIds;
            }
            catch
            {
                return new HashSet<int>();
            }
        }

        public IEnumerable<HrEmpGen> GetActvEmpNameLst(int entId, int empSysId, int? grpId = null)
        {
            try
            {
                // 1. Restricted Self-Only Users: Outsource User (Grp_Id = 4) and QCS User (Grp_Id = 12)
                if (IsRestrictedSelfOnlyUser(empSysId, grpId))
                {
                    var self = _hrmContext.HrEmpGenInfo
                        .Where(e => e.EmpSysId == empSysId)
                        .Select(e => new HrEmpGen
                        {
                            ESysId = e.EmpSysId,
                            FulNme = ((e.FirstName ?? "").Trim() + (string.IsNullOrWhiteSpace(e.MdlName) ? "" : " " + e.MdlName.Trim()) + (string.IsNullOrWhiteSpace(e.LastName) ? "" : " " + e.LastName.Trim())).Trim(),
                            EmpId = e.EmpId,
                        })
                        .ToList();
                    return self;
                }

                // 2. Outsource Admin: Sees all employees assigned as Wrktyp_Id = 2 in HR_EmpGenInfo, plus herself
                if (IsOutsourceAdmin(empSysId, grpId))
                {
                    var outsourceIds = GetOutsourceEmployeeSysIds(empSysId);
                    if (empSysId > 0) outsourceIds.Add(empSysId);

                    var osEmps = _hrmContext.HrEmpGenInfo
                        .Where(e => (e.WrktypId == 2 || outsourceIds.Contains(e.EmpSysId)) && e.CurrStat == 0)
                        .Select(e => new HrEmpGen
                        {
                            ESysId = e.EmpSysId,
                            FulNme = ((e.FirstName ?? "").Trim() + (string.IsNullOrWhiteSpace(e.MdlName) ? "" : " " + e.MdlName.Trim()) + (string.IsNullOrWhiteSpace(e.LastName) ? "" : " " + e.LastName.Trim())).Trim(),
                            EmpId = e.EmpId,
                        })
                        .OrderBy(o => o.FulNme)
                        .ToList();

                    if (!osEmps.Any(e => e.ESysId == empSysId) && empSysId > 0)
                    {
                        var selfEmp = _hrmContext.HrEmpGenInfo.FirstOrDefault(e => e.EmpSysId == empSysId);
                        if (selfEmp != null)
                        {
                            osEmps.Add(new HrEmpGen
                            {
                                ESysId = selfEmp.EmpSysId,
                                FulNme = ((selfEmp.FirstName ?? "").Trim() + (string.IsNullOrWhiteSpace(selfEmp.MdlName) ? "" : " " + selfEmp.MdlName.Trim()) + (string.IsNullOrWhiteSpace(selfEmp.LastName) ? "" : " " + selfEmp.LastName.Trim())).Trim(),
                                EmpId = selfEmp.EmpId,
                            });
                        }
                    }

                    if (!osEmps.Any())
                    {
                        osEmps = _hrmContext.HrEmpGenInfo
                            .Where(e => e.WrktypId == 2 || outsourceIds.Contains(e.EmpSysId))
                            .Select(e => new HrEmpGen
                            {
                                ESysId = e.EmpSysId,
                                FulNme = ((e.FirstName ?? "").Trim() + (string.IsNullOrWhiteSpace(e.MdlName) ? "" : " " + e.MdlName.Trim()) + (string.IsNullOrWhiteSpace(e.LastName) ? "" : " " + e.LastName.Trim())).Trim(),
                            EmpId = e.EmpId,
                            })
                            .OrderBy(o => o.FulNme)
                            .ToList();
                    }
                    return osEmps.OrderBy(o => o.FulNme).ToList();
                }

                // 3. Regular users / Admin / BU Heads
                bool showAll = GetUserShowAllEmployees(empSysId, grpId);
                var allowedIds = GetEmpSbrdnts(empSysId).ToHashSet();
                if (empSysId > 0) allowedIds.Add(empSysId);

                if (!showAll || allowedIds.Count <= 100)
                {
                    if (allowedIds.Any())
                    {
                        var emps = _hrmContext.HrEmpGenInfo
                            .Where(e => allowedIds.Contains(e.EmpSysId) && e.CurrStat == 0)
                            .Select(e => new HrEmpGen
                            {
                                ESysId = e.EmpSysId,
                                FulNme = ((e.FirstName ?? "").Trim() + (string.IsNullOrWhiteSpace(e.MdlName) ? "" : " " + e.MdlName.Trim()) + (string.IsNullOrWhiteSpace(e.LastName) ? "" : " " + e.LastName.Trim())).Trim(),
                            EmpId = e.EmpId,
                            })
                            .OrderBy(o => o.FulNme)
                            .ToList();

                        if (!emps.Any(e => e.ESysId == empSysId) && empSysId > 0)
                        {
                            var self = _hrmContext.HrEmpGenInfo.FirstOrDefault(e => e.EmpSysId == empSysId);
                            if (self != null)
                            {
                                emps.Add(new HrEmpGen
                                {
                                    ESysId = self.EmpSysId,
                                    FulNme = ((self.FirstName ?? "").Trim() + (string.IsNullOrWhiteSpace(self.MdlName) ? "" : " " + self.MdlName.Trim()) + (string.IsNullOrWhiteSpace(self.LastName) ? "" : " " + self.LastName.Trim())).Trim(),
                                    EmpId = self.EmpId,
                                });
                            }
                        }

                        if (emps.Any())
                        {
                            return emps.OrderBy(o => o.FulNme).ToList();
                        }
                    }
                }

                return GetActvEmpNameLst(entId > 0 ? entId : 1);
            }
            catch (Exception ex) { throw ex; }
        }


        public IEnumerable<HrEmpGen> GetEmps_NotCreatedAsUsers()
        {
            try
            {

                IEnumerable<HrEmpGen> objFiltered = from em in _hrmContext.HrEmpGenInfo
                                                    where !_hrmContext.AdmUser.Any(a => a.EmpSysId == em.EmpSysId)
                                                    select new HrEmpGen
                                                    {
                                                        ESysId = em.EmpSysId,
                                                        FulNme = em.FirstName + "" + (em.MdlName == null ? "" : " " + em.MdlName) + " " + em.LastName,
                                                        EmpId = em.EmpId,
                                                    };
               
                return objFiltered.ToList().OrderBy(n => n.FulNme);

            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<EmpSimpleView1> GetRepotingManagers(int empSysId)
        {
            IEnumerable<EmpSimpleView1> filteredEmps = from Emp in _hrmContext.HrEmpGenInfo
                                                      where (Emp.CurrStat == 0 || Emp.CurrStat == -1) && Emp.EmpSysId != empSysId
                                                      select new EmpSimpleView1
                                                      {
                                                          EmpSysId = Emp.EmpSysId,
                                                          EmpDesc = Emp.FirstName + "" + (Emp.MdlName == null ? "" : " " + Emp.MdlName) + " " + Emp.LastName
                                                      };
            return filteredEmps.ToList().OrderBy(n => n.EmpDesc);
        }

        public IEnumerable<EmpSimpleView1> GetNewEmpForSlryStruct(int entId)
        {
            PayrollContext payrollContext = new PayrollContext(_hrmContext);

            var lstSlryPrcHdr = payrollContext.GetLstSlryPrcHdr(entId);
            var curSlryMnt = new DateTime(lstSlryPrcHdr.EftYear, lstSlryPrcHdr.EftMonth, 01);

            if (lstSlryPrcHdr.PrcStat == 3)
            {
                curSlryMnt = curSlryMnt.AddMonths(1);
            }

            IEnumerable<EmpSimpleView1> filteredEmps = from emp in _hrmContext.HrEmpGenInfo
                                                           where (emp.CurrStat == 0 || emp.SrvEndDate >= curSlryMnt) && !_hrmContext.HrPrEmpSlryStruc.Any(x => x.EmpSysId == emp.EmpSysId)                                                           
                                                           select new EmpSimpleView1
                                                           {
                                                               EmpSysId = emp.EmpSysId,
                                                               EmpDesc = emp.FirstName + "" + (emp.MdlName == null ? "" : " " + emp.MdlName) + " " + emp.LastName
                                                               //EmpDesc =  Emp.FullName
                                                           };
            return filteredEmps.ToList().OrderBy(n=>n.EmpDesc);
        }

        public IEnumerable<EmpSimpleView1> GetEmployeeViewByEntId(int entId, DateTime curDate)
        {
            PayrollContext payrollContext = new PayrollContext(_hrmContext);

            var lstSlryPrcHdr = payrollContext.GetLstSlryPrcHdr(entId);
            var curSlryMnt = new DateTime(lstSlryPrcHdr.EftYear, lstSlryPrcHdr.EftMonth, 01);

            if (lstSlryPrcHdr.PrcStat == 3)
            {
                curSlryMnt = curSlryMnt.AddMonths(1);
            }

            return _hrmContext.HrEmpGenInfo
                                .Join(_hrmContext.HrEmpJobAssign, jb => jb.EmpSysId, emp => emp.EmpSysId, (emp, jb) => new { emp, jb })
                                .Join(_hrmContext.OrgDepartment, p => p.jb.DeptId, dp => dp.DeptId, (p, dp) => new { p, dp })
                                .Where(z => (z.p.emp.CurrStat == 0 || z.p.emp.SrvEndDate >= curSlryMnt) && z.p.jb.StDate <= curDate && (z.p.jb.EndDate == null || z.p.jb.EndDate > curDate) && z.dp.EntId == entId)
                                
                                .Select(x => new EmpSimpleView1
                                {
                                    EmpSysId = x.p.emp.EmpSysId,
                                    EmpDesc = x.p.emp.FirstName + "" + (x.p.emp.MdlName == null ? "" : " " + x.p.emp.MdlName) + " " + x.p.emp.LastName
                                }).ToList().OrderBy(n => n.EmpDesc);
        }

        public IEnumerable<EmpSimpleView1> GetOutsourceEmployeeViewByEntId(int entId, DateTime curDate)
        {
            PayrollContext payrollContext = new PayrollContext(_hrmContext);

            var lstSlryPrcHdr = payrollContext.GetLstSlryPrcHdr(entId);
            var curSlryMnt = lstSlryPrcHdr != null
                ? new DateTime(lstSlryPrcHdr.EftYear, lstSlryPrcHdr.EftMonth, 01)
                : new DateTime(curDate.Year, curDate.Month, 01);

            if (lstSlryPrcHdr != null && lstSlryPrcHdr.PrcStat == 3)
            {
                curSlryMnt = curSlryMnt.AddMonths(1);
            }

            return _hrmContext.HrEmpGenInfo
                                .Join(_hrmContext.HrEmpJobAssign, jb => jb.EmpSysId, emp => emp.EmpSysId, (emp, jb) => new { emp, jb })
                                .Join(_hrmContext.OrgDepartment, p => p.jb.DeptId, dp => dp.DeptId, (p, dp) => new { p, dp })
                                .Where(z => z.p.emp.WrktypId == 2
                                    && (z.p.emp.CurrStat == 0 || z.p.emp.SrvEndDate >= curSlryMnt)
                                    && z.p.jb.WkStat == 1
                                    && z.dp.EntId == entId)
                                .Select(x => new EmpSimpleView1
                                {
                                    EmpSysId = x.p.emp.EmpSysId,
                                    EmpDesc = x.p.emp.EmpId + " - " + x.p.emp.FirstName + (x.p.emp.MdlName == null ? "" : " " + x.p.emp.MdlName) + " " + x.p.emp.LastName
                                }).ToList().OrderBy(n => n.EmpDesc);
        }

        public List<int> GetChldEmpSysIdLst(int empSysId)
        {
            try
            {
                var chldEmpSysIdLst = new List<int>();
                GetChldEmpSysIdLst(empSysId, chldEmpSysIdLst);
                return chldEmpSysIdLst;
            }
            catch (Exception ex) { throw ex; }
        }

        private void GetChldEmpSysIdLst(int empSysId, List<int> empSysIdLst) {
            try
            {
                var x = _hrmContext.HrEmpGenInfo
                    .Join(_hrmContext.HrEmpJobAssign, emp => emp.EmpSysId, jb => jb.EmpSysId, (emp, jb) => new { emp, jb })
                    .Where(z => z.jb.RptMngId == empSysId && z.jb.WkStat == 1 && z.emp.CurrStat == 0)
                    .Select(x => x.emp.EmpSysId);
                foreach(int cEmpSysId in x)
                {
                    empSysIdLst.Add(cEmpSysId);
                    GetChldEmpSysIdLst(cEmpSysId, empSysIdLst);
                }
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<EmpWkRstView> GetEmpForWkRstr(string feild, int entId, int? empSysId)
        {
            try
            {
           
                string sp = "[HR_EmpGenInfo_Select2] {0},{1},{2}";
                object[] param = { feild, entId, empSysId };
                IEnumerable<HrEmpGenInfoView> list = new List<HrEmpGenInfoView>();
                list = _hrmContext.HrEmpGenInfoView.FromSqlRaw(sp, param).ToList();
                var wrkRstEmp = from em in list
                                select new EmpWkRstView
                                {
                                    RecId = em.Emp_Sys_Id,
                                    EmpSysId = em.Emp_Sys_Id,
                                    EmpName = em.Full_Name,
                                    Color = "540033",
                                    GroupId = "1"
                                    
                                   };
                return wrkRstEmp.OrderBy(n => n.EmpSysId);


            }
            catch (Exception ex) { throw ex; }
        }

        public EmpCntSmry GetEmpCntSmry(string schTyp, int refId, DateTime frmDt, DateTime toDt)
        {
            try
            {
                var empCntSmry = new EmpCntSmry();
                var jbs = _hrmContext.HrEmpGenInfo
                                            .Join(_hrmContext.HrEmpJobAssign, emp => emp.EmpSysId, jb => jb.EmpSysId, (emp, jb) => new { emp, jb })
                                            .Where(x => x.emp.CurrStat == 0 && x.jb.WkStat == 1//(x.jb.StDate <= DateTime.Today && (x.jb.EndDate == null || (x.jb.EndDate >= DateTime.Today))) 
                                                && (schTyp == "ALL" || (schTyp == "DPT" && x.jb.DeptId == refId)))
                                            .Select(x => x.jb);

                empCntSmry.CurTotEmpCnt = jbs.Count();
                empCntSmry.OutSrcdEmpCnt = jbs.Where(x => x.DeptId == 7).Count();

                var emps = _hrmContext.HrEmpGenInfo
                        .Join(_hrmContext.HrEmpJobAssign, emp => emp.EmpSysId, jb => jb.EmpSysId, (emp, jb) => new { emp, jb })
                        .Where(x => ((x.emp.JoinedDate >= frmDt && x.emp.JoinedDate <= toDt) || (x.emp.SrvEndDate >= frmDt && x.emp.SrvEndDate <= toDt))
                            && x.jb.WkStat == 1
                            && (schTyp == "ALL" || (schTyp == "DPT" && x.jb.DeptId == refId)))
                        .Select(z => z.emp);

                empCntSmry.InCnt = emps.Where(x => x.JoinedDate >= frmDt && x.JoinedDate <= toDt).Count();
                empCntSmry.OutCnt = emps.Where(x => x.SrvEndDate >= frmDt && x.SrvEndDate <= toDt).Count();
                return empCntSmry;
            }
            catch (Exception ex) { throw ex; }
        }

        #region Employee Sort Order
        private int GetMxSrtOrdByDeptId(int deptId)
        {
            try
            { 
                var mxSrtOrd = _hrmContext.HrEmpGenInfo
                        .Join(_hrmContext.HrEmpJobAssign, emp => emp.EmpSysId, jb => jb.EmpSysId, (emp, jb) => new { emp, jb })
                        .Where(z => z.jb.DeptId == deptId && z.emp.CurrStat == 0 && z.jb.WkStat == 1)
                        .Max(m => m.emp.SrtOrd);
                if (mxSrtOrd != null)
                    return (int)mxSrtOrd;
                else
                {
                    return _hrmContext.OrgDepartment.Where(z => z.DeptId == deptId).First().SrtOrd * 1000000;
                }
                
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<EmpSrtOrdVw> GetEmpSrtOdrVws(int entId, int empSysId)
        {
            try
            {
                return _hrmContext.HrEmpGenInfo
                        .Join(_hrmContext.HrEmpJobAssign, e => e.EmpSysId, j => j.EmpSysId, (e, j) => new { e, j })
                        .Join(_hrmContext.OrgDesignation, p => p.j.DesigId, ds => ds.DesigId, (p, ds) => new { p, ds })
                        .Join(_hrmContext.OrgDepartment, q => q.p.j.DeptId, dp => dp.DeptId, (q, dp) => new { q, dp })
                        .Where(z => z.q.p.e.CurrStat == 0 && z.q.p.j.WkStat == 1 && z.dp.EntId == entId && (empSysId == 0 || z.q.p.e.EmpSysId == empSysId)).OrderBy(o => o.dp.SrtOrd).ThenBy(t => t.q.p.e.SrtOrd)
                        .Select(x => new EmpSrtOrdVw
                        {
                            EmpSysId = x.q.p.e.EmpSysId,
                            EmpId = x.q.p.e.EmpId,
                            EmpName = x.q.p.e.FirstName + (x.q.p.e.MdlName == null ? "": " " + x.q.p.e.MdlName) + " " + x.q.p.e.LastName,
                            EmpSrtOrd = x.q.p.e.SrtOrd?? 0,
                            DeptId = x.dp.DeptId,
                            DeptSrtOrd = x.dp.SrtOrd,
                            DeptName = x.dp.NameEng,
                            DesigId = x.q.ds.DesigId,
                            DesigName = x.q.ds.NameEng
                        });
            }
            catch (Exception ex) { throw ex; }
        }

        public int UpdateEmpSrtOrd(int deptId, int srtOrd, int empSysId)
        {
            try
            {
                var empGenInfos = _hrmContext.HrEmpGenInfo
                                  .Join(_hrmContext.HrEmpJobAssign, e => e.EmpSysId, j => j.EmpSysId, (e, j) => new { e, j })
                                  .Where(z => z.e.CurrStat == 0 && z.j.WkStat == 1 && z.j.DeptId == deptId && z.e.EmpSysId != empSysId && z.e.SrtOrd >= srtOrd)
                                  .Select(x => x.e).OrderBy(o => o.SrtOrd);
                var nwSrtOrd = srtOrd + 1;
                foreach(var empGenInfo in empGenInfos)
                {
                    if (empGenInfo.SrtOrd != nwSrtOrd)
                    {
                        empGenInfo.SrtOrd = nwSrtOrd;
                        nwSrtOrd += 1;
                    }
                }
                _hrmContext.SaveChanges();
                return 1;
            }
            catch { throw; }
        }
        #endregion Employee Sort Order

        #endregion

        #region ==============Job Assignments==============
        public IEnumerable<HrEmpJobAssignView> GetJbViews(int empSysId)
        {
            return from jb in _hrmContext.HrEmpJobAssign 
                   join dp in _hrmContext.OrgDepartment 
                        on jb.DeptId equals dp.DeptId 
                   join ds in _hrmContext.OrgDesignation 
                        on jb.DesigId equals ds.DesigId
                   join em in _hrmContext.HrEmpGenInfo
                        on jb.RptMngId equals em.EmpSysId into jbAsgns
                   from jbAsgn in jbAsgns.DefaultIfEmpty()
                   where jb.EmpSysId == empSysId  
                   select new HrEmpJobAssignView
                     {
                       JbAsngId = jb.JbAsngId
                       ,
                       EmpSysId = jb.EmpSysId
                       ,
                       StDate = jb.StDate
                       ,
                       EndDate = jb.EndDate
                       ,
                       DeptId = jb.DeptId
                       ,
                       DeptName = dp.NameEng
                       ,
                       DesigId = jb.DesigId
                       ,
                       DesigName = ds.NameEng
                       ,
                       EmpTypId = jb.EmpTypId
                       ,
                       EmpTypName = (jb.WkStat == 1) ? "Permanent" : (jb.WkStat == 2) ? "Contract" : (jb.WkStat == 3) ? "Training" : (jb.WkStat == 4) ? "Temporary" : "Other"
                       ,
                       WkStat = jb.WkStat
                       ,
                       WkStatName = (jb.WkStat == 1) ? "Active " : "Closed"
                       ,
                       RptMngId = jb.RptMngId
                       ,
                       RptMngName = jbAsgn == null ? String.Empty : jbAsgn.FullName
                   };
        }

        public HrEmpJobAssign GetCurrentAssignment(int empSysId)
        {
            var assign = _hrmContext.HrEmpJobAssign.OrderByDescending(x => x.StDate).Where(x => x.EmpSysId == empSysId && x.WkStat == 1).FirstOrDefault();
            if (assign == null)
            {
                assign = _hrmContext.HrEmpJobAssign.OrderByDescending(x => x.StDate).Where(x => x.EmpSysId == empSysId).FirstOrDefault();
            }
            return assign;
        }

        public HrEmpJobAssign GetCurrentAssignment(int empSysId, int jbAsngId)
        {
            return _hrmContext.HrEmpJobAssign.OrderByDescending(x => x.StDate).Where(x => x.EmpSysId == empSysId && x.JbAsngId == jbAsngId && x.WkStat == 1).FirstOrDefault();
        }

        public int InsertNewJbAssignment(HrEmpJobAssign empJbAsgn, int userId)
        {
            try
            {
                var nwDpt = true;
                empJbAsgn.WkStat = 1;
                empJbAsgn.CrtDateTime = DateTime.Now;
                empJbAsgn.CrtId = userId;
                var extEmpJbAsgn = _hrmContext.HrEmpJobAssign.Where(x => x.WkStat == 1 && x.EmpSysId == empJbAsgn.EmpSysId).FirstOrDefault();
                if (extEmpJbAsgn != null)
                {
                    extEmpJbAsgn.EndDate = empJbAsgn.StDate;
                    extEmpJbAsgn.WkStat = 0;
                    if (extEmpJbAsgn.DeptId == empJbAsgn.DeptId)
                        nwDpt = false;
                }
                
                _hrmContext.HrEmpJobAssign.Add(empJbAsgn);
                if (nwDpt)
                {
                    var emp = _hrmContext.HrEmpGenInfo.Where(z => z.EmpSysId == empJbAsgn.EmpSysId).First();
                    emp.SrtOrd = GetMxSrtOrdByDeptId(empJbAsgn.DeptId) + 1;
                }
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }

        public int UpdateJbAssignment(HrEmpJobAssign empJbAsgn)
        {
            try
            {
                var _changes = _hrmContext.HrEmpJobAssign.Update(empJbAsgn);                
                _changes.State = EntityState.Modified;
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public int DeactivateJbAssignment(HrEmpJobAssign empJbAsgn)
        {
            try
            {
                var objEmpJobAsgn = _hrmContext.HrEmpJobAssign.Where(x => x.EmpSysId == empJbAsgn.EmpSysId).FirstOrDefault();
                objEmpJobAsgn.WkStat = 0;

                var varChanges = _hrmContext.HrEmpJobAssign.Attach(objEmpJobAsgn);
                varChanges.State = EntityState.Modified;
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception) { return 0; }
        }

        private IEnumerable<HrEmpJobAssign> GetEmpSbrdntByHrchy(int? rptMngId)
        {
            var empJbAsgns = _hrmContext.HrEmpJobAssign.Where(x => x.WkStat == 1);
            var root = empJbAsgns.FirstOrDefault(x => x.EmpSysId == rptMngId);
            if (root == null) yield break;
            var empJbAsgnStck = new Stack<HrEmpJobAssign>(new[] { root });
            while (empJbAsgnStck.Any())
            {
                HrEmpJobAssign empJbAsgn = empJbAsgnStck.Pop();
                yield return empJbAsgn;
                foreach (var emp in empJbAsgns.Where(x => x.RptMngId == empJbAsgn.EmpSysId))
                    empJbAsgnStck.Push(emp);
            }
        }

        public IEnumerable<int> GetEmpSbrdnts(int empSysId)
        {
            if (IsOutsourceAdmin(empSysId))
            {
                return GetOutsourceEmployeeSysIds(empSysId);
            }

            try
            {
                var usrId = _hrmContext.AdmUser.Where(u => u.EmpSysId == empSysId && u.RecStat == 1).Select(u => (int?)u.UsrId).FirstOrDefault();
                if (usrId.HasValue && usrId.Value > 0)
                {
                    var conn = _hrmContext.Database.GetDbConnection();
                    bool wasOpen = conn.State == System.Data.ConnectionState.Open;
                    if (!wasOpen) conn.Open();
                    try
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT TRY_CAST(EmpSysId AS INT) FROM dbo.HR_EmpSbrdnts(@usrId) WHERE EmpSysId IS NOT NULL";
                            var p = cmd.CreateParameter();
                            p.ParameterName = "@usrId";
                            p.Value = usrId.Value;
                            cmd.Parameters.Add(p);

                            var list = new List<int>();
                            using (var rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    if (!rdr.IsDBNull(0))
                                    {
                                        list.Add(rdr.GetInt32(0));
                                    }
                                }
                            }
                            if (list.Any())
                            {
                                return list.Distinct();
                            }
                        }
                    }
                    finally
                    {
                        if (!wasOpen) conn.Close();
                    }
                }
            }
            catch
            {
                // Fallback to in-memory evaluation
            }

            var usrGrp = (from usr in _hrmContext.AdmUser
                          join ug in _hrmContext.AdmUserGroup on usr.GrpId equals ug.GrpId
                          where usr.EmpSysId == empSysId
                          select ug).FirstOrDefault();

            var ugeg = usrGrp != null ? _hrmContext.AdmUsrGrpEmpGrp.Where(x => x.GrpId == usrGrp.GrpId && x.RecStat == 1).ToList() : null;
            bool isAll = ugeg != null && ugeg.Any(x => x.EmpGrpId == -1 || x.EmpGrpId == -100);

            if (isAll)
            {
                return _hrmContext.HrEmpGenInfo.Where(x => x.CurrStat == 0).Select(z => z.EmpSysId);
            }
            else if (usrGrp != null && usrGrp.EmpGrpSbs)
            {
                return from emp in _hrmContext.HrEmpGenInfo
                       join ug_eg in _hrmContext.AdmUsrGrpEmpGrp on emp.GrpId equals ug_eg.EmpGrpId
                       where emp.EmpSysId == empSysId || (ug_eg.GrpId == usrGrp.GrpId && ug_eg.RecStat == 1)
                       select emp.EmpSysId;
            }
            else
            {
                var hier = GetEmpSbrdntByHrchy(empSysId);
                return hier != null ? hier.Where(x => x.WkStat == 1).Select(z => z.EmpSysId) : new List<int> { empSysId };
            }
        }

        public IEnumerable<HrEmpGen> GetHierarchySubordinates(int rootEmpSysId, int? grpId = null)
        {
            try
            {
                if (IsOutsourceAdmin(rootEmpSysId, grpId))
                {
                    var outsourceIds = GetOutsourceEmployeeSysIds(rootEmpSysId);
                    return _hrmContext.HrEmpGenInfo
                        .Where(e => e.CurrStat == 0 && outsourceIds.Contains(e.EmpSysId))
                        .Select(e => new HrEmpGen
                        {
                            ESysId = e.EmpSysId,
                            FulNme = ((e.FirstName ?? "").Trim() + (string.IsNullOrWhiteSpace(e.MdlName) ? "" : " " + e.MdlName.Trim()) + (string.IsNullOrWhiteSpace(e.LastName) ? "" : " " + e.LastName.Trim())).Trim(),
                            EmpId = e.EmpId,
                        }).OrderBy(o => o.FulNme).ToList();
                }

                bool showAll = GetUserShowAllEmployees(rootEmpSysId, grpId);
                var activeJobAssigns = _hrmContext.HrEmpJobAssign.Where(x => x.WkStat == 1).ToList();
                var activeEmps = _hrmContext.HrEmpGenInfo.Where(x => x.CurrStat == 0).ToDictionary(x => x.EmpSysId);

                var allowedEmpIds = new HashSet<int>();

                if (showAll)
                {
                    allowedEmpIds = activeEmps.Keys.ToHashSet();
                }
                else
                {
                    if (rootEmpSysId > 0)
                    {
                        allowedEmpIds.Add(rootEmpSysId);
                        var queue = new Queue<int>();
                        queue.Enqueue(rootEmpSysId);

                        while (queue.Count > 0)
                        {
                            var currentId = queue.Dequeue();
                            var subordinates = activeJobAssigns.Where(x => x.RptMngId == currentId).Select(x => x.EmpSysId);
                            foreach (var subId in subordinates)
                            {
                                if (allowedEmpIds.Add(subId))
                                {
                                    queue.Enqueue(subId);
                                }
                            }
                        }
                    }
                }

                var result = new List<HrEmpGen>();

                // Root / Logged in employee first
                if (activeEmps.TryGetValue(rootEmpSysId, out var rootEmp))
                {
                    result.Add(new HrEmpGen
                    {
                        ESysId = rootEmp.EmpSysId,
                        FulNme = $"{rootEmp.FirstName} {(string.IsNullOrEmpty(rootEmp.MdlName) ? "" : rootEmp.MdlName + " ")}{rootEmp.LastName} (My Attendance)".Trim(),
                        EmpId = rootEmp.EmpId,
                    });
                }
                else if (rootEmpSysId > 0)
                {
                    var emp = _hrmContext.HrEmpGenInfo.FirstOrDefault(e => e.EmpSysId == rootEmpSysId);
                    if (emp != null)
                    {
                        result.Add(new HrEmpGen
                        {
                            ESysId = emp.EmpSysId,
                            FulNme = $"{emp.FirstName} {(string.IsNullOrEmpty(emp.MdlName) ? "" : emp.MdlName + " ")}{emp.LastName} (My Attendance)".Trim(),
                            EmpId = emp.EmpId,
                        });
                    }
                }

                // Subordinates ordered by name
                var subList = allowedEmpIds
                    .Where(id => id != rootEmpSysId && activeEmps.ContainsKey(id))
                    .Select(id =>
                    {
                        var e = activeEmps[id];
                        return new HrEmpGen
                        {
                            ESysId = e.EmpSysId,
                            FulNme = $"{e.FirstName} {(string.IsNullOrEmpty(e.MdlName) ? "" : e.MdlName + " ")}{e.LastName}".Trim(),
                            EmpId = e.EmpId,
                        };
                    })
                    .OrderBy(o => o.FulNme);

                result.AddRange(subList);
                return result;
            }
            catch (Exception)
            {
                return new List<HrEmpGen>();
            }
        }
        #endregion

        #region ==============Employee Grade==============
        public partial class Grd
        {
            public int GrdId { get; set; }
            public string TitleEng { get; set; }
        }

        public IEnumerable<HrEmpGrade> GetAllGradeByEntId(int entId)
        {
            try
            {
                
                return _hrmContext.HrEmpGrade.Where(x => x.RecStat == 1 && x.EntId == entId && x.GrdId != 0).ToList();
                
            }
            catch (Exception ex) { throw ex; }
        }

        public HrEmpGrade GetGradeById(int id)
        {
            try
            {
                return _hrmContext.HrEmpGrade.Where(x => x.GrdId == id).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public HrEmpGrade GetGradeByCode(string code)
        {
            try
            {
                return _hrmContext.HrEmpGrade.Where(x => x.GrdCode == code).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<Grd> GetFilteredGrades()
        {
            try
            {
                IEnumerable<Grd> objFiltered = from HrGrades in _hrmContext.HrEmpGrade
                                               where HrGrades.RecStat == 1
                                               select new Grd
                                               {
                                                   GrdId = HrGrades.GrdId,
                                                   TitleEng = HrGrades.TitleEng
                                               };
                return objFiltered.ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Employee Group==============
        public partial class Grp
        {
            public int GrpId { get; set; }
            public string TitleEng { get; set; }
        }

        public IEnumerable<HrEmpGroup> GetAllByEntId(int entId)
        {
            try
            {
                return _hrmContext.HrEmpGroup.Where(x => x.RecStat == 1 && x.EntId == entId && x.GrpId != 0).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public HrEmpGroup GetById(int grpId)
        {
            try
            {
                return _hrmContext.HrEmpGroup.Where(x => x.GrpId == grpId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public HrEmpGroup GetByCode(string code)
        {
            try
            {
                return _hrmContext.HrEmpGroup.Where(x => x.GrpCode == code).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<Grp> GetFilteredEmpGroups()
        {
            try
            {
                IEnumerable<Grp> objFiltered = from HrGroups in _hrmContext.HrEmpGroup
                                               where HrGroups.RecStat == 1
                                               select new Grp
                                               {
                                                   GrpId = HrGroups.GrpId,
                                                   TitleEng = HrGroups.TitleEng
                                               };
                return objFiltered.ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Probation Periods==============
        public IEnumerable<HrEmpProbation> GetAllProbation(int empSysId)
        {
            try
            {
                return _hrmContext.HrEmpProbation.Where(x => x.EmpSysId == empSysId).ToList();
            }
            catch { throw; }
        }

        public HrEmpProbation GetActvProbation(int empSysId)
        {
            try
            {
                return _hrmContext.HrEmpProbation.OrderByDescending(x => x.StDate).Where(x => x.EmpSysId == empSysId && x.WkStat == 1).FirstOrDefault();
            }
            catch { throw; }
        }

        public HrEmpProbation GetCurrentProbation(int empSysId)
        {
            try
            {
                return _hrmContext.HrEmpProbation.OrderByDescending(x => x.StDate).Where(x => x.EmpSysId == empSysId && x.WkStat == 1 && x.EndDate > DateTime.Today).FirstOrDefault();
            }
            catch { throw; }
        }

        public HrEmpProbation GetProbationById(int probId)
        {
            try
            {
                return _hrmContext.HrEmpProbation.Where(x => x.ProbId == probId).FirstOrDefault();
            }
            catch { throw; }
        }

        public HrEmpProbation GetExistingProbation(int empSysId, DateTime newStDate)
        {
            try
            {
                return _hrmContext.HrEmpProbation.OrderByDescending(x => x.StDate).Where(x => x.EmpSysId == empSysId && x.WkStat == 1 && x.EndDate > newStDate).FirstOrDefault();
            }
            catch { throw; }
        }

        public int InsertProbation(HrEmpProbation empProb, int usrId)
        {
            try
            {
                empProb.WkStat = 1;
                empProb.CrtDateTime = DateTime.Today;
                empProb.CrtId = usrId;
                var prob = _hrmContext.HrEmpProbation.Where(x => x.WkStat == 1).FirstOrDefault();
                if (prob != null)
                {
                    prob.EndDate = empProb.StDate;
                    prob.WkStat = 0;
                }
                _hrmContext.HrEmpProbation.Add(empProb);
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception) { return 0; }
        }
        #endregion

        #region ==============Dependant Document==============
        public partial class HrEmpDpndDocView
        {
            public int DocId { get; set; }
            public int DpndId { get; set; }
            public int DocTypId { get; set; }
            public string DocTyp { get; set; }
            public string Desc { get; set; }
        }

        public IEnumerable<HrEmpDpndDocView> GetDocsByDpndId(int dpndId)
        {
            try
            {
                return _hrmContext.HrEmpDpndDoc.Join(_hrmContext.SysSelectListElement, dd => dd.DocTypId, le => le.Indx, (dd, le) => new { dd, le })
                                                .Join(_hrmContext.SysSelectListType, p => p.le.LstTypId, lt => lt.LstTypId, (p, lt) => new { p, lt })
                                                .Where(z => z.lt.LstTypKey == "DPN_DOC_TYP" && z.p.dd.DpndId == dpndId)
                                                .Select(x => new HrEmpDpndDocView
                                                {
                                                    DocId = x.p.dd.DocId,
                                                    DpndId = x.p.dd.DpndId,
                                                    DocTypId = x.p.dd.DocTypId,
                                                    DocTyp = x.p.le.ElmntTxt,
                                                    Desc = x.p.dd.Desc
                                                });                
            }
            catch (Exception ex) { throw ex; }
        }


        public IEnumerable<HrEmpDpndDoc> GetDpnDocsByDocId(int docId)
        {
            try
            {
                return _hrmContext.HrEmpDpndDoc.Where(x => x.DocId == docId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Employee Pre-Experience==============
        public IEnumerable<HrEmpPreExp> GetEmpExpById(int expId)
        {
            try
            {
                return _hrmContext.HrEmpPreExp.Where(x => x.ExpId == expId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }


        public IEnumerable<HrEmpPreExp> GetEmpExpEmpSysId(int empSysId)
        {
            try
            {
                return _hrmContext.HrEmpPreExp.Where(x => x.EmpSysId == empSysId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }


        public IEnumerable<HrEmpPreExp> GetEmpExpByCompany(string comName)
        {
            try
            {
                return _hrmContext.HrEmpPreExp.Where(x => x.CompName.ToLower() == comName.ToLower()).ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region  ==============Educational Qualification==============
        public IEnumerable<HrEmpEduQualif> GetQualficationById(int QuaId)
        {
            try
            {
                return _hrmContext.HrEmpEduQualif.Where(x =>  x.QualifId == QuaId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }


        public IEnumerable<HrEmpEduQualif> GetQualficationEmpSysId(int empSysId)
        {
            try
            {
                return _hrmContext.HrEmpEduQualif.Where(x => x.EmpSysId == empSysId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Residance Info==============
        public IEnumerable<HrEmpResidInfo> GetResidanceByEmpSysId(int empSysId)
        {
            try
            {
                return _hrmContext.HrEmpResidInfo.Where(x => x.RecStat == 1 && x.EmpSysId == empSysId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpResidInfo> GetResidanceById(int ResidId)
        {
            try
            {
                return _hrmContext.HrEmpResidInfo.Where(x => x.RecStat == 1 && x.ResidId == ResidId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpResidInfo> GeAllResidance()
        {
            try
            {
                return _hrmContext.HrEmpResidInfo.Where(x => x.RecStat == 1).ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Employee Document==============

        public partial class HrEmpDocumentView
        {
            public int DocId { get; set; }
            public string DocName { get; set; }            
            public int DocTypId { get; set; }
            public string DocTyp { get; set; }
            public string Desc { get; set; }
        }
        public IEnumerable<HrEmpDocumentView> GetEmpDocsByEmpSysId(int empSysId)
        {
            try
            {

                return _hrmContext.HrEmpDocument.Join(_hrmContext.SysSelectListElement, dd => dd.DocTypId, le => le.Indx, (dd, le) => new { dd, le })
                                                .Join(_hrmContext.SysSelectListType, p => p.le.LstTypId, lt => lt.LstTypId, (p, lt) => new { p, lt })
                                                .Where(z => z.lt.LstTypKey == "EMP_DOC_TYP" && z.p.dd.EmpSysId == empSysId)
                                                .Select(x => new HrEmpDocumentView
                                                {
                                                    DocId = x.p.dd.DocId,
                                                    DocName = x.p.dd.DocName,
                                                    DocTypId = x.p.dd.DocTypId,
                                                    DocTyp = x.p.le.ElmntTxt,
                                                    Desc = x.p.dd.Desc
                                                });

                //return _hrmContext.HrEmpDocument.Where(x => x.EmpSysId == empSysId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }


        public IEnumerable<HrEmpDocument> GetEmpDocsByDocId(int docId)
        {
            try
            {
                return _hrmContext.HrEmpDocument.Where(x => x.DocId == docId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Bank Accounts==============
        public IEnumerable<HrEmpBankAcct> GetAccountById(int acctId)
        {
            try
            {
                return _hrmContext.HrEmpBankAcct.Where(x => x.RecStat == 1 & x.AcctId == acctId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpBankAcct> GetAccountByAcctNo(string acctNo)
        {
            try
            {
                return _hrmContext.HrEmpBankAcct.Where(x => x.RecStat == 1 & x.AcctNo == acctNo).ToList();
            }
            catch (Exception ex) { throw ex; }
        }


        public IEnumerable<HrEmpBankAcct> GetAccountsByEmpSysId(int empSysId)
        {
            try
            {
                return _hrmContext.HrEmpBankAcct.Where(x => x.RecStat == 1 && x.EmpSysId == empSysId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region  ==============Sponsor Details==============
        public IEnumerable<HrEmpSponsor> GetSponsorById(int spnsrId)
        {
            try
            {
                return _hrmContext.HrEmpSponsor.Where(x => x.RecStat == 1 & x.SpnsrId == spnsrId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }


        public IEnumerable<HrEmpSponsor> GetSponsorsByEmpSysId(int empSysId)
        {
            try
            {
                return _hrmContext.HrEmpSponsor.Where(x => x.RecStat == 1 && x.EmpSysId == empSysId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Employee Dependent==============
        public IEnumerable<HrEmpDependentView> GetDependantsByEmpSysId(string feild, int empSysId, int dpndId)
        {
            try
            {
                string sp = "[HR_EmpDependent_Select] {0},{1},{2}";
                object[] param = { feild, empSysId, dpndId };
                var list = new List<HrEmpDependentView>();
                list = _hrmContext.HrEmpDependentView.FromSqlRaw(sp, param).ToList();
                return list;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpDependent> GetDependantsByEmpSysId(int empSysId)
        {
            try
            {
                return _hrmContext.HrEmpDependent.Where(x => x.RecStat == 1 && x.EmpSysId == empSysId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpDependent> GetDependantsById(int DpndId)
        {
            try
            {
                return _hrmContext.HrEmpDependent.Where(x => x.RecStat == 1 && x.DpndId == DpndId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpDependent> GetDependants()
        {
            try
            {
                return _hrmContext.HrEmpDependent.Where(x => x.RecStat == 1).ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Employee Photo==============
        public partial class PhtMaxIndx
        {
            public int EmpSysId { get; set; }
        public int PhtId { get; set; }
        }

        public IEnumerable<HrEmpPhoto> GetPhotosByEmpSysId(int empSysId)
        {
            try
            {
                return _hrmContext.HrEmpPhoto.Where(x => x.EmpSysId == empSysId).OrderByDescending(x => x.CrtDateTime).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<HrEmpPhoto> GetPhotoByPhotoId(int empSysId, int phtId)
        {
            try
            {
                return _hrmContext.HrEmpPhoto.Where(x => x.EmpSysId == empSysId && x.PhtId == phtId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public int DeleteEmpPhoto(HrEmpPhoto hrEmpPhoto)
        {
            try
            {
                _hrmContext.HrEmpPhoto.Remove(hrEmpPhoto);
                _hrmContext.SaveChanges();
                return 1;
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion
    }
}
