using System;
using System.Linq;
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
    public class AdmUserGroupController : Controller
    {
        private readonly SysAdminContext _sysAdminContext;

        public AdmUserGroupController(SysAdminContext sysAdminContext)
        {
            this._sysAdminContext = sysAdminContext;
        }

        // GET: UserGroup
        public ActionResult Index()
        {
            try
            {
                //No Access Granted
                if (HttpContext.Session.GetString("SY_ADM") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

                ViewBag.DataSource = _sysAdminContext.GetAdmUserGroups("_all", 0).ToList();
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        // GET: UserGroup/Details/5
        public IActionResult Details(int id)
        {
            try
            {
                var UsrGrp = _sysAdminContext.GetAdmUserGroups("_all", 0).Where(x => x.GrpId == id).FirstOrDefault();
                return View(UsrGrp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("User Group", ex.Message);
            }

        }

        // GET: UserGroup/Create
        [HttpGet]
        public IActionResult CreateNew(AdmUserGroup usrGrp)
        {
            try
            {
                return PartialView("_UserGroup_Create", usrGrp);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        // POST: UserGroup/Create
        [HttpPost]
        public IActionResult Create(AdmUserGroup usrGrp)
        {

            try
            {
                usrGrp.RecStat = 1;
                usrGrp.CrtDateTime = DateTime.Today;
                usrGrp.DshbrdId = 1;
                var _result = _sysAdminContext.InsertAdmUserGroup(usrGrp);
                if (_result == 1)
                    return RedirectToAction("Index", "AdmUserGroup").WithSuccess("QCS", "Record Added Successfully");
                else
                    return RedirectToAction("Index", "AdmUserGroup").WithWarning("QCS", "Adding Record got failed");

            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "AdmUserGroup").WithWarning("QCS", "Adding Record got failed " + ex.Message.ToString());
            }
        }

        // GET: UserGroup/Edit/5
        public ActionResult EditGet(int id)
        {
            try
            {
                var oUsrGrp = _sysAdminContext.GetAdmUserGroups("_all", 0).Where(x => x.GrpId == id).FirstOrDefault();
                oUsrGrp.exName = oUsrGrp.Name;
                return PartialView("_UserGroup_Edit", oUsrGrp);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

        // POST: UserGroup/Edit/5
        [HttpPost]
        public IActionResult Edit(AdmUserGroup usrGrp)
        {

            try
            {
                usrGrp.RecStat = 1;
                usrGrp.CrtDateTime = DateTime.Today;
                var _result = _sysAdminContext.UpdateAdmUserGroup(usrGrp);
                if (_result == 1)
                    return RedirectToAction("Index", "AdmUserGroup").WithSuccess("QCS", "Record Updated Successfully");
                else
                    return RedirectToAction("Index", "AdmUserGroup").WithWarning("QCS", "Updating Record got failed");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "AdmUserGroup").WithWarning("QCS", "Updating Record got failed " + ex.Message.ToString());
            }

        }

        // GET: UserGroup/Delete/5
        public IActionResult DeleteGet(int id)
        {
            try
            {
                var oUsrGrp = _sysAdminContext.GetAdmUserGroups("_all", 0).Where(x => x.GrpId == id).FirstOrDefault();
                return PartialView("_UserGroup_Delete", oUsrGrp);
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index)).WithWarning("User Group", ex.Message);
            }


        }

        // POST: UserGroup/Delete/5
        [HttpPost]
        public IActionResult Delete(AdmUserGroup obj)
        {
            try
            {
                var _result = _sysAdminContext.DeleteAdmUserGroup(obj);
                if (_result == 1)
                    return RedirectToAction("Index", "AdmUserGroup").WithSuccess("QCS", "Record Updated Successfully");
                else
                    return RedirectToAction("Index", "AdmUserGroup").WithWarning("QCS", "Updating Record got failed");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "AdmUserGroup").WithWarning("QCS", "Updating Record got failed " + ex.Message.ToString());
            }
        }


        public IActionResult IsExists([Bind(Prefix = "Name")] string value, [Bind(Prefix = "exName")] string exvalue)
        {
            if (exvalue != null)
            {
                if (value != exvalue)
                {
                    var _itemfound = _sysAdminContext.GetAdmUserGroups("_all", 0).Where(x => x.Name.ToLower() == value.ToLower()).FirstOrDefault();
                    if (_itemfound != null)
                        return Json(data: false);
                    else
                        return Json(data: true);
                }
            }
            else
            {
                var _itemfound = _sysAdminContext.GetAdmUserGroups("_all", 0).Where(x => x.Name.ToLower() == value.ToLower()).FirstOrDefault();
                if (_itemfound != null)
                    return Json(data: false);
                else
                    return Json(data: true);

            }
            return Json(data: true);
        }
    }
}