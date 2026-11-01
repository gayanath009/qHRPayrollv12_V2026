using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Integrations.Data;
using qHRM.Extensions.Alerts;
using System;
using System.Collections.Generic;
using System.Linq;
using static qHRM.Areas.Integrations.Data.LvIntgrtnContext;

namespace qHRM.Areas.Leave.Controllers
{
    [Area("Leave")]
    [Authorize]
    public class LvLdMpngController : Controller
    {
        private readonly LvIntgrtnContext _lvIntgrtnContext;

        private static List<LvMpng> _lvMpngs;

        public LvLdMpngController(LvIntgrtnContext lvIntgrtnContext)
        {
            _lvIntgrtnContext = lvIntgrtnContext;
        }

        public IActionResult Index()
        {
            //No Access Granted
            if (HttpContext.Session.GetString("LV_LD_UTL") == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            _lvMpngs = _lvIntgrtnContext.GetPndngLvs();
            ViewBag.LvMpngs = _lvMpngs;

            return View();
        }

        // GET: LvLdMpng/LoadLeaves
        [HttpPost]
        public IActionResult UploadLeaves()
        {
            try
            {
                if (ModelState.IsValid && _lvMpngs.Count > 0)
                {
                    try
                    {
                        var isValid = _lvMpngs.Count(x => x.MpdSts != "Mapped") > 0 ? false : true;
                        if (isValid)
                        {
                            _lvIntgrtnContext.SaveMpdLvs(_lvMpngs, (int)HttpContext.Session.GetInt32("userEmpSysId"));
                            _lvMpngs = _lvIntgrtnContext.GetPndngLvs();
                            ViewBag.LvMpngs = _lvMpngs;
                            return RedirectToAction(nameof(Index)).WithSuccess("Pending Leave Load", "Leave Records Loaded Successfully");
                        }
                        else
                        {
                            return RedirectToAction(nameof(Index)).WithWarning("Pending Leave Load Failed", "There are unmapped records.");
                        }

                    }
                    catch (Exception ex)
                    {
                        return Redirect(nameof(Index)).WithWarning("Pending Leave Load Failed", ex.Message.ToString());
                    }
                }
                else
                {
                    return Redirect(nameof(Index)).WithWarning("Pending Leave Load", "No records found");
                }
            }
            catch
            {
                return View();
            }
        }
    }
}
