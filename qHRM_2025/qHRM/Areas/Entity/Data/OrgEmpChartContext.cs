using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Entity.Models;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Entity.Data
{
    public class OrgEmpChartContext
    {
        #region ==============Member Variables==============
        private readonly qHRMasterContext _hrmContext;
        private readonly EmployeeContext _empContext;
        private readonly SysAdminContext _sysAdminContext; 
        private List<OrgEmpCht> _fullOrgEmpChtObj;
        #endregion

        #region ==============Constructors==============
        public OrgEmpChartContext(qHRMasterContext hrmContext, EmployeeContext empContext, SysAdminContext sysAdminContext)
        {
            _hrmContext = hrmContext;
            _empContext = empContext;
            _sysAdminContext = sysAdminContext;
        }
        #endregion

        #region ==============Organization Chat Section==============

        public IEnumerable<OrgEmpCht> GetEmpObjects(int empSysId, DateTime curDate)
        {
            List<OrgEmpCht> orgEmpChtObjs = new List<OrgEmpCht>();
            try
            {
                //var cldList = _empContext.GetEmpSbrdnts(empSysId);
                var admUsrGrp = _sysAdminContext.GetAdmUserGroupByEmpSysId(empSysId);

                string sp = "[ORG_GetOrgChartEmployees] {0}";
                object[] param = { curDate };
                _fullOrgEmpChtObj = _hrmContext.OrgEmpChtObj.FromSqlRaw(sp, param).ToList();

                if (_fullOrgEmpChtObj.Count > 0)
                {
                    // Deduplicate raw stored procedure results by EmpId to ensure each employee only appears once in the chart
                    _fullOrgEmpChtObj = _fullOrgEmpChtObj
                        .Where(x => !string.IsNullOrWhiteSpace(x.EmpId))
                        .GroupBy(x => x.EmpId.Trim())
                        .Select(g => g.First())
                        .ToList();

                    //Root
                    OrgEmpCht rootOrgEmpChtObj;
                    if (empSysId == 0 || admUsrGrp.ShwAlEmp)
                        rootOrgEmpChtObj = _fullOrgEmpChtObj.Where(x => x.RptMngId == null || x.RptMngId.Trim() == "" || x.RptMngId.Trim() == "0").FirstOrDefault();                    
                    else
                        rootOrgEmpChtObj = _fullOrgEmpChtObj.Where(x => x.EmpId == empSysId.ToString()).FirstOrDefault();

                    
                    if (rootOrgEmpChtObj != null)
                    {
                        rootOrgEmpChtObj.RptMngId = "";
                        orgEmpChtObjs.Add(rootOrgEmpChtObj);
                        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rootOrgEmpChtObj.EmpId.Trim() };
                        GetChildEmpObjects(orgEmpChtObjs, rootOrgEmpChtObj.EmpId.Trim(), curDate, visited);
                    }
                }
                return orgEmpChtObjs;
            }
            catch (Exception ex) { throw ex; }
        }

        private void GetChildEmpObjects(List<OrgEmpCht> orgEmpChtObjs, string parentId, DateTime curDate, HashSet<string> visited)
        {
            try
            {
                var tmpOrgEmpChtObjs = _fullOrgEmpChtObj.Where(x => (x.RptMngId ?? "").Trim() == parentId);

                foreach (var orgEmpChtObj in tmpOrgEmpChtObjs)
                {
                    string empId = (orgEmpChtObj.EmpId ?? "").Trim();
                    if (!string.IsNullOrEmpty(empId) && visited.Add(empId))
                    {
                        orgEmpChtObjs.Add(orgEmpChtObj);
                        GetChildEmpObjects(orgEmpChtObjs, empId, curDate, visited);
                    }
                }
            }
            catch { throw; }
        }

        
        #endregion
    }
}
