﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using MrCMS.Entities.Documents;
using MrCMS.Entities.Documents.Layout;
using MrCMS.Entities.Documents.Web;
using MrCMS.Helpers;
using MrCMS.Services;
using MrCMS.Web.Application.Pages;
using MrCMS.Website;
using NHibernate;

namespace MrCMS.Web.Areas.Admin.Controllers
{
    public class WebpageController : BaseDocumentController<Webpage>
    {
        private readonly IFormService _formService;
        private readonly ISession _session;

        public WebpageController(IDocumentService documentService, ISiteService siteService, IFormService formService, ISession session)
            : base(documentService, siteService)
        {
            _formService = formService;
            _session = session;
        }

        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);
            var id = filterContext.RouteData.Values["id"];
            int idVal;
            if (!string.IsNullOrWhiteSpace(Convert.ToString(id)) && int.TryParse(Convert.ToString(id), out idVal))
            {
                var document = _documentService.GetDocument<Webpage>(idVal);
                if (document != null && !document.IsAllowedForAdmin(MrCMSApplication.CurrentUser))
                {
                    filterContext.Result = new RedirectResult("~/admin");
                }
            }
        }

        protected override void PopulateEditDropdownLists(Webpage doc)
        {
            IEnumerable<Layout> layouts = _documentService.GetAllDocuments<Layout>().Where(x => x.Hidden == false);

            ViewData["Layout"] = layouts.BuildSelectItemList(layout => layout.Name,
                                                             layout => layout.Id.ToString(CultureInfo.InvariantCulture),
                                                             layout =>
                                                             doc != null && doc.Layout != null &&
                                                             doc.Layout.Id == layout.Id,
                                                             SelectListItemHelper.EmptyItem("Default Layout"));

            var documentTypeDefinitions = (doc.Parent as Webpage).GetValidWebpageDocumentTypes(_documentService).ToList();
            ViewData["DocumentTypes"] = documentTypeDefinitions;

            doc.AdminViewData(ViewData, _session);
        }

        public override ActionResult Show(Webpage document)
        {
            if (document == null)
                return RedirectToAction("Index");

            return View((object)document);
        }

        [HttpPost]
        public ActionResult PublishNow(Webpage webpage)
        {
            _documentService.PublishNow(webpage);

            return RedirectToAction("Edit", new { id = webpage.Id });
        }

        [HttpPost]
        public ActionResult Unpublish(Webpage webpage)
        {
            _documentService.Unpublish(webpage);

            return RedirectToAction("Edit", new { id = webpage.Id });
        }
        [HttpPost]
        public ActionResult HideWidget(int id, int widgetId, int layoutAreaId)
        {
            _documentService.HideWidget(id, widgetId);
            return RedirectToAction("Edit", new { id, layoutAreaId });
        }

        [HttpPost]
        public ActionResult ShowWidget(int id, int widgetId, int layoutAreaId)
        {
            _documentService.ShowWidget(id, widgetId);
            return RedirectToAction("Edit", new { id, layoutAreaId });
        }

        [HttpPost]
        public void SetParent(Webpage webpage, int? parentId)
        {
            _documentService.SetParent(webpage, parentId);
        }

        [HttpGet]
        public JsonResult GetForm(Webpage webpage)
        {
            return Json(_formService.GetFormStructure(webpage));
        }

        [HttpPost]
        public void SaveForm(int id, string data)
        {
            _formService.SaveFormStructure(id, data);
        }

        public ActionResult ViewChanges(int id)
        {
            var documentVersion = _documentService.GetDocumentVersion(id);
            if (documentVersion == null)
                return RedirectToAction("Index");

            return PartialView(documentVersion);
        }

        [ValidateInput(false)]
        public string GetUnformattedBodyContent(TextPage textPage)
        {
            return textPage.BodyContent;
        }

        [ValidateInput(false)]
        public string GetFormattedBodyContent(TextPage textPage)
        {
            MrCMSApplication.CurrentPage = textPage;
            var htmlHelper = MrCMSHtmlHelper.GetHtmlHelper(this);
            return htmlHelper.ParseShortcodes(textPage.BodyContent).ToHtmlString();
        }

        public PartialViewResult Postings(Webpage webpage, int page = 1, string search = null)
        {
            var data = _formService.GetFormPostings(webpage, page, search);

            return PartialView(data);
        }

        public ActionResult ViewPosting(int id)
        {
            return PartialView(_formService.GetFormPosting(id));
        }

        public ActionResult Versions(Document doc, int page = 1)
        {
            var data = doc.GetVersions(page);

            return PartialView(data);
        }
    }
}