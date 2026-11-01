using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using qHRM.Extensions.Alerts;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Areas.SysAdmin.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace qHRM.Areas.SysAdmin.Controllers
{
    [Area("SysAdmin")]
    [Authorize]
    public class AdmUserGroupPrivilegeController : Controller
    {
        private readonly SysAdminContext _sysAdminContext;
        private readonly AdmUserGrpPrvContext _usrGrpPrvContext; 
        private static IEnumerable<int> _listPrivil;

        public AdmUserGroupPrivilegeController(SysAdminContext sysAdminContext,
                                            AdmUserGrpPrvContext usrGrpPrvContext)
        {
            this._sysAdminContext = sysAdminContext;
            this._usrGrpPrvContext = usrGrpPrvContext;
        }

        // GET: UserGroupPrivilege
        public ActionResult Index()
        {
            //No Access Granted
            if (HttpContext.Session.GetString("SY_PRV") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var _listUG = _sysAdminContext.GetAdmUserGroups("_assign_groups",0);
            ViewBag.DataSource = _listUG;
            return View();
        }

        // GET: UserGroupPrivilege/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: UserGroupPrivilege/Create
        [HttpGet]
        public ActionResult CreateGet()
                {
            try
            {
                mvUserGroupAssign _obj = new mvUserGroupAssign();
                //-- Calling the Un-assigned User Groups --// 
                _obj.lstUsrGroup = _sysAdminContext.GetAdmUserGroups("_not_assign_groups", 0);

                //-- Calling the Un-assigned Privileges --// 
                _obj.lstParent_Priv = _usrGrpPrvContext.Get("_not_assigned_privileges_parent", 0);
                _obj.lstChild_Priv_1 = _usrGrpPrvContext.Get("_not_assigned_privileges_child_lvl_1", 0);
                _obj.lstChild_Priv_2 = _usrGrpPrvContext.Get("_not_assigned_privileges_child_lvl_2", 0);

                _listPrivil = _obj.lstChild_Priv_1.Select(l => l.PrvId).ToList();
                return PartialView("_UserGroupAssign_Create", _obj);
            }
            catch (Exception)
            {
                throw;
            }          
            
        }

        
        [HttpPost]        
        public IActionResult Create(int drpUserGroup,
                                   IEnumerable<int> listView,
                                   IEnumerable<int> listAdd,
                                   IEnumerable<int> listEdit,
                                   IEnumerable<int> listDelete)
        {
            try
            {
                
                AdmUserGroupPrivilege _obj = new AdmUserGroupPrivilege();

                foreach (int valuePrivil in _listPrivil)
                {
                    _obj.GrpId = drpUserGroup;
                    _obj.PrvId = valuePrivil;
                    _obj.RecView = listView.Contains(valuePrivil);
                    _obj.RecAdd = listAdd.Contains(valuePrivil);
                    _obj.RecEdit = listEdit.Contains(valuePrivil);
                    _obj.RecDelete = listDelete.Contains(valuePrivil);
                    _obj.RecStat = 1;
                    _obj.CrtId = 1;// Convert.ToInt32(HttpContext.Session.GetInt32("SessionUserID"));
                    _obj.CrtDateTime = DateTime.Now;
                    if (_obj.RecView == true || _obj.RecAdd == true || _obj.RecEdit == true || _obj.RecDelete == true)
                    {
                        var _msg = _usrGrpPrvContext.Create(_obj);
                    }
                }
               
               return RedirectToAction(nameof(Index)).WithSuccess("User Group Privilege", "Record Added Successfully");
               
            }
            catch
            {
                return RedirectToAction(nameof(Index)).WithSuccess("User Group Privilege", "Adding Record got failed");
            }
        }

        // GET: UserGroupPrivilege/Edit/5
        [HttpGet]
        public ActionResult EditGet(int id)
        {
            try
            {
                mvUserGroupAssign _obj = new mvUserGroupAssign();
                //-- Calling the Assigned User Groups --// 
                _obj.lstUsrGroup = _sysAdminContext.GetAdmUserGroups("_assign_groups_byID", id);

                //-- Calling the Un-assigned Privileges --// 
                _obj.lstParent_Priv = _usrGrpPrvContext.Get("_assigned_privileges_parent", id);
                _obj.lstChild_Priv_1 = _usrGrpPrvContext.Get("_assigned_privileges_child_lvl_1", id);
                _obj.lstChild_Priv_2 = _usrGrpPrvContext.Get("_assigned_privileges_child_lvl_2", id);

                _listPrivil = _obj.lstChild_Priv_1.Select(l => l.PrvId).ToList();
                return PartialView("_UserGroupAssign_Edit", _obj);            
            }                
            catch
            {
                throw;
            }
        }

        // POST: UserGroupPrivilege/Edit/5
        [HttpPost]        
        public IActionResult Edit(int drpUserGroup,
                                   IEnumerable<int> listView,
                                   IEnumerable<int> listAdd,
                                   IEnumerable<int> listEdit,
                                   IEnumerable<int> listDelete)
        {
            try
            {
                try
                {
                    //Removing the privileges 
                    AdmUserGroupPrivilege _obj = new AdmUserGroupPrivilege();
                    _obj.GrpId = drpUserGroup;
                    _usrGrpPrvContext.Remove(_obj);

                    //Removin the privileges 
                    foreach (int valuePrivil in _listPrivil)
                    {
                        _obj.GrpId = drpUserGroup;
                        _obj.PrvId = valuePrivil;
                        _obj.RecView = listView.Contains(valuePrivil);
                        _obj.RecAdd = listAdd.Contains(valuePrivil);
                        _obj.RecEdit = listEdit.Contains(valuePrivil);
                        _obj.RecDelete = listDelete.Contains(valuePrivil);
                        _obj.RecStat = 1;
                        _obj.CrtId = 1;// Convert.ToInt32(HttpContext.Session.GetInt32("SessionUserID"));
                        _obj.CrtDateTime = DateTime.Now;
                        if (_obj.RecView ==true || _obj.RecAdd == true || _obj.RecEdit == true || _obj.RecDelete == true)
                        {
                            var _msg = _usrGrpPrvContext.Create(_obj);
                        }
                    }
                    return RedirectToAction(nameof(Index)).WithSuccess("User Group Privilege", "Record Updated Successfully");
                }
                catch
                {
                    return RedirectToAction(nameof(Index)).WithSuccess("User Group Privilege", "Updating Record got failed");
                }
            }
            catch
            {
                return View();
            }
        }

        // GET: UserGroupPrivilege/Delete/5
        public ActionResult DeleteGet(int id)
        {
            var obj = _sysAdminContext.GetAdmUserGroups("_assign_groups", 0).Where(x => x.GrpId == id).FirstOrDefault();
            mvGroupPrivilege objPrv = new mvGroupPrivilege();
            objPrv.GrpId = obj.GrpId;
            objPrv.NameEng = obj.Name;
            return PartialView("_UserGroupAssign_Delete", objPrv);
        }

        // POST: UserGroupPrivilege/Delete/5
        [HttpPost]        
        public IActionResult Delete(AdmUserGroupPrivilege obj)
        {
            //int msg = 0;
            try
            {
                AdmUserGroupPrivilege _obj = new AdmUserGroupPrivilege();
                _obj.GrpId = obj.GrpId;                       
                _usrGrpPrvContext.Remove(_obj);
                return RedirectToAction(nameof(Index)).WithSuccess("User Group Privilege", "Record Deleted Successfully");
                
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("User Group Privilege", ex.Message);
            }
        }
    }
}