using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Syncfusion.EJ2.PdfViewer;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace qHRM.Areas.Payroll
{

    [Area("Payroll")]
    [Authorize]
    public class HrPrPDFViewerController : Controller
    {
        private readonly IHostingEnvironment _host;
        private IMemoryCache _cache;

        public HrPrPDFViewerController(IHostingEnvironment host,
                                       IMemoryCache memoryCache)
        {
            _host = host;            
            _cache = memoryCache;
        }


        [AcceptVerbs("Post")]
        [HttpPost]
        [Route("api/[controller]/Load")]

        public ActionResult Index([FromBody] Dictionary<string, string> jsonData)
        {
            try
            {
                PdfRenderer pdfviewer = new PdfRenderer(_cache);
                MemoryStream stream = new MemoryStream();

                object jsonResult = new object();
                string documentPath = null;
                documentPath = _host.WebRootPath + @"\attach\slrySheet\PaySheet_2020_7_20200818154425.pdf";

                if (!string.IsNullOrEmpty(documentPath))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(documentPath);
                    stream = new MemoryStream(bytes);
                }
                jsonResult = pdfviewer.Load(stream, jsonData);
                return Content(JsonConvert.SerializeObject(jsonResult));

            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }           
        }


       

        // GET: HrPrPDFViewer/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: HrPrPDFViewer/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: HrPrPDFViewer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: HrPrPDFViewer/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: HrPrPDFViewer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: HrPrPDFViewer/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: HrPrPDFViewer/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}