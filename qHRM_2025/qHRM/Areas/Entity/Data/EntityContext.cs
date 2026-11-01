using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using qHRM.Areas.Entity.Models;
using qHRM.Areas.Leave.Models;
using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace qHRM.Areas.Entity.Data
{
    public class EntityContext
    {
        #region ==============Member Variables==============
        private readonly qHRMasterContext _hrmContext;
        #endregion

        #region ==============Constructors==============
        public EntityContext(qHRMasterContext hrmContext)
        {
            _hrmContext = hrmContext;
        }
        #endregion

        #region ==============OrgDepartment Section==============
        public partial class DepatmentView
        {
            public int DeptId { get; set; }
            public string DeptName { get; set; }
        }

        public partial class DepatmentFullView
        {
            public int DeptId { get; set; }
            public string DeptCode { get; set; }
            public string ShtName { get; set; }
            public string NameEng { get; set; }
            public string NameArb { get; set; }
            public string DescEng { get; set; }
            public string DescArb { get; set; }
            public int? ParentId { get; set; }
            public string ParentName { get; set; }
            public int EntId { get; set; }
            public short RecStat { get; set; }

        }

        public IEnumerable<DepatmentFullView> GetDepartmentEntFullViews(int entId)
        {
            try
            {
                

                var x = _hrmContext.OrgDepartment.Join(_hrmContext.OrgDepartment, dpt => dpt.ParentId, pdpt => pdpt.DeptId, (dpt, pdpt) => new { dpt, pdpt })
                                                .Where(z => z.dpt.RecStat == 1 && z.dpt.DeptId != 0 && z.dpt.EntId == entId)
                                                .Select( x=> new DepatmentFullView
                                                {
                                                    DeptId  = x.dpt.DeptId,
                                                    DeptCode = x.dpt.DeptCode,
                                                    ShtName = x.dpt.ShtName,
                                                    NameEng = x.dpt.NameEng,
                                                    NameArb  = x.dpt.NameArb,
                                                    DescEng = x.dpt.DescEng,
                                                    DescArb = x.dpt.DescArb,
                                                    ParentId = x.dpt.ParentId,
                                                    ParentName = x.pdpt.NameEng,
                                                    EntId = x.dpt.EntId,
                                                    RecStat = x.dpt.RecStat,
                                                });
                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<DepatmentView> GetDepatmentEntViews(int entId)
        {
            return from Dept in _hrmContext.OrgDepartment
                                            where (Dept.RecStat == 1 && Dept.EntId == entId && Dept.DeptId != 0) 
                                            select new DepatmentView
                                            {
                                                DeptId = Dept.DeptId,
                                                DeptName = String.Concat(Dept.DeptCode, " - ", Dept.NameEng)
                                            };
        }

        public OrgDepartment GetDepartmentById(int deptId)
        {
            try
            {
                return _hrmContext.OrgDepartment.Where(x => x.DeptId == deptId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public OrgDepartment GetDepartmentByCode(string deptCode)
        {
            try
            {
                return _hrmContext.OrgDepartment.Where(x => x.DeptCode.ToLower() == deptCode.ToLower()).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public int GetDepartmentMaxSrtOrd()
        {
            try
            {
                return _hrmContext.OrgDepartment.Max(m => m.SrtOrd);
            }
            catch (Exception ex) { throw ex; }
        }

        public partial class HrEmpView
        {
            public int ESysId { get; set; }
            public string FulNme { get; set; }
        }

        public IEnumerable<HrEmpView> GetEmployeesByDptId(int? deptId)
        {
            try
            {

                var x = _hrmContext.HrEmpGenInfo
                        .Join(_hrmContext.HrEmpJobAssign, p => p.EmpSysId, jb => jb.EmpSysId, (p, jb) => new { p, jb })
                        .Join(_hrmContext.OrgDepartment, q => q.jb.DeptId, dp => dp.DeptId, (q, dp) => new { q, dp })                       
                       .Where(z => z.dp.RecStat == 1 && z.q.p.CurrStat == 0 && z.dp.DeptId == deptId)
                       .Select(x => new HrEmpView
                       {
                           ESysId = x.q.p.EmpSysId,
                           FulNme = x.q.p.FullName
                       });

                return x;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<DepatmentView> GetRecursiveDeptList(int deptId)
        {
            try
            {
                List<DepatmentView> deptLst = new List<DepatmentView>();
                var dept = _hrmContext.OrgDepartment.Where(z => z.DeptId == deptId)
                             .Select(x => new DepatmentView { DeptId = x.DeptId, DeptName = x.NameEng }).FirstOrDefault();
                if (dept != null)
                {
                    deptLst.Add(dept);
                    GetChildDeptList(deptId, deptLst);
                }
                return deptLst;
            }
            catch (Exception ex) { throw ex; }
        }

        private void GetChildDeptList(int parentId, List<DepatmentView> deptLst)
        {
            try
            {
                var childList = _hrmContext.OrgDepartment
                                .Where(z => z.ParentId == parentId)
                                .Select(x => new DepatmentView { DeptId = x.DeptId, DeptName = x.NameEng })
                                .OrderBy(O => O.DeptId);

                foreach (var dept in childList)
                {
                    deptLst.Add(dept);
                    GetChildDeptList(dept.DeptId, deptLst);
                }
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============OrgDesignation Section==============
        public partial class DesignationView
        {
            public int DesigId { get; set; }
            public string DesigName { get; set; }
        }

        public IEnumerable<OrgDesignation> GetDesignations(string feild, int usrId)
        {
            try
            {
                string sp = "[ORG_Designation_Select] {0},{1}";
                object[] param = { feild, usrId };
                var list = new List<OrgDesignation>();
                list = _hrmContext.OrgDesignation.FromSqlRaw(sp, param).ToList();
                return list;
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<DesignationView> GetDesignationViews()
        {
            IEnumerable<DesignationView> objFiltered = from Desig in _hrmContext.OrgDesignation
                                                       where Desig.RecStat == 1
                                                       select new DesignationView
                                                       {
                                                           DesigId = Desig.DesigId,
                                                           DesigName = String.Concat(Desig.DesigCode, " - ", Desig.NameEng)
                                                       };
            return objFiltered;
        }
        #endregion

        #region ==============OrgLocation Section==============
        public partial class Loc
        {
            public int LocId { get; set; }
            public string NameEng { get; set; }
        }
        public partial class OrgLocationFullView
        {
            public int LocId { get; set; }
            public string LocCode { get; set; }
            public string NameEng { get; set; }
            public string DescEng { get; set; }
            public int? ParentId { get; set; }
            public string ParentName { get; set; }
        }

        public IEnumerable<OrgLocationFullView> GetLocFullViews(int entId)
        {
            try
            {
                return from loc in _hrmContext.OrgLocation
                       join prnt in _hrmContext.OrgLocation on loc.ParentId equals prnt.LocId into pLocs
                       from pLoc in pLocs.DefaultIfEmpty()
                       where loc.EntId == entId && loc.LocId != 0
                       select new OrgLocationFullView
                       {
                           LocId = loc.LocId,
                           LocCode = loc.LocCode,
                           NameEng = loc.NameEng,
                           DescEng = loc.DescEng,
                           ParentId = loc.ParentId,
                           ParentName = (pLoc == null)? "None" : pLoc.NameEng
                         };
            }
            catch { throw; }
        }

        public IEnumerable<OrgLocation> GetAllLocations()
        {
            try
            {
                return _hrmContext.OrgLocation.Where(x => x.RecStat == 1 && x.LocId != 0).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<OrgLocation> GetAllLocsToAsgn()
        {
            try
            {
                return _hrmContext.OrgLocation.Where(x => x.RecStat == 1 && x.LocId != 0 && x.LocId != 3).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<OrgLocation> GetParentLocation(int entId, int locId)
        {
            try
            {
                return _hrmContext.OrgLocation.Where(x => x.EntId == entId && x.RecStat == 1 && x.LocId != locId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<Loc> GetFilteredLocations()
        {
            try
            {

                IEnumerable<Loc> objFiltered = from OrgLoc in _hrmContext.OrgLocation
                                               where OrgLoc.RecStat == 1
                                               select new Loc
                                               {
                                                   LocId = OrgLoc.LocId,
                                                   NameEng = OrgLoc.NameEng
                                               };
                return objFiltered.ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============OrgHoliday Section==============
        public IEnumerable<OrgHoliday> GetAllOrgHolidaysByYear()
        {
            try
            {
                return _hrmContext.OrgHoliday.Where(x => x.RecStat == 1).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public OrgHoliday GetOrgHolidayById(int hdId) {
            try
            {
                var x = _hrmContext.OrgHoliday.Where(x => x.HdId == hdId).FirstOrDefault();
                _hrmContext.Entry(x).State = EntityState.Detached;
                return x;
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion ==============OrgHoliday Section==============
    }
}
