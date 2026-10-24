using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using static Controllers.AccessControl;

namespace Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion des enseignants du département.
    /// Inclut la gestion de l'avatar (cf. ImageAsset sur le modèle Teacher).
    /// </summary>
    public class TeachersController : Controller
    {
        // Variables de session :
        //  - CurrentTeacherId    : id du prof courant
        //  - CurrentTeacherName  : nom du prof courant
        //  - TeachersSearch       : true si recherche active
        //  - TeachersSearchString : chaîne de recherche
        private void InitSessionVariables()
        {
            if (Session["CurrentTeacherId"] == null)     Session["CurrentTeacherId"] = 0;
            if (Session["CurrentTeacherName"] == null)   Session["CurrentTeacherName"] = "";
            if (Session["TeachersSearch"] == null)       Session["TeachersSearch"] = false;
            if (Session["TeachersSearchString"] == null) Session["TeachersSearchString"] = "";
        }

        private void ResetCurrentTeacherInfo()
        {
            Session["CurrentTeacherId"] = 0;
            Session["CurrentTeacherName"] = "";
        }

        // ----------------------------------------------------------------------
        // Vue liste des enseignants (ordre alphabétique de nom, cf. PDF page 8)
        // ----------------------------------------------------------------------

        [UserAccess(Access.View)]
        public ActionResult List()
        {
            InitSessionVariables();
            ResetCurrentTeacherInfo();
            return View();
        }

        /// <summary>Action AJAX qui produit la vue partielle des enseignants.</summary>
        [UserAccess(Access.View)]
        public ActionResult GetTeachers(bool forceRefresh = false)
        {
            IEnumerable<Teacher> result = null;
            if (forceRefresh || DB.Teachers.HasChanged)
            {
                InitSessionVariables();
                bool search = (bool)Session["TeachersSearch"];
                string searchString = (string)Session["TeachersSearchString"];

                result = DB.Teachers.ToList();

                if (search && !string.IsNullOrEmpty(searchString))
                {
                    result = result.Where(t =>
                        (t.FirstName ?? "").ToLower().Contains(searchString) ||
                        (t.LastName  ?? "").ToLower().Contains(searchString) ||
                        (t.Code      ?? "").ToLower().Contains(searchString));
                }
                // Tri alphabétique par nom puis prénom
                return PartialView(result.OrderBy(t => t.LastName).ThenBy(t => t.FirstName));
            }
            return null;
        }

        [UserAccess(Access.View)]
        public ActionResult ToggleSearch()
        {
            if (Session["TeachersSearch"] == null) Session["TeachersSearch"] = false;
            Session["TeachersSearch"] = !(bool)Session["TeachersSearch"];
            if (!(bool)Session["TeachersSearch"])
                Session["TeachersSearchString"] = "";
            return RedirectToAction("List");
        }

        [UserAccess(Access.View)]
        public ActionResult SetSearchString(string value)
        {
            Session["TeachersSearchString"] = (value ?? "").ToLower();
            return new EmptyResult();
        }

        // ----------------------------------------------------------------------
        // Vue détails d'un enseignant
        // ----------------------------------------------------------------------

        [UserAccess(Access.View)]
        public ActionResult Details(int id)
        {
            InitSessionVariables();
            Teacher teacher = DB.Teachers.Get(id);
            if (teacher != null)
            {
                Session["CurrentTeacherId"] = id;
                Session["CurrentTeacherName"] = teacher.FullName;
                return View(teacher);
            }
            return RedirectToAction("List");
        }

        /// <summary>Action AJAX : rafraîchit les allocations du prof courant.</summary>
        [UserAccess(Access.View)]
        public ActionResult GetTeacherAllocations(bool forceRefresh = false)
        {
            int id = Session["CurrentTeacherId"] != null ? (int)Session["CurrentTeacherId"] : 0;
            if (id == 0) return null;

            if (forceRefresh || DB.Allocations.HasChanged || DB.Courses.HasChanged)
            {
                Teacher teacher = DB.Teachers.Get(id);
                if (teacher == null) return null;
                return PartialView(teacher);
            }
            return null;
        }

        // ----------------------------------------------------------------------
        // Vue d'ajout d'enseignant
        // ----------------------------------------------------------------------

        /// <summary>Formulaire de création d'un nouvel enseignant.</summary>
        [UserAccess(Access.Write)]
        public ActionResult Create()
        {
            ViewBag.Action = "Create";
            ViewBag.Creating = true;
            // Code pré-généré au format CLG-420-XXXXX (5 chiffres aléatoires)
            return View("Edit", new Teacher { Code = Teacher.GenerateUniqueCode() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAccess(Access.Write)]
        public ActionResult Create(Teacher teacher)
        {
            if (!teacher.IsValid())
            {
                ViewBag.Action = "Create";
                ViewBag.Creating = true;
                return View("Edit", teacher);
            }
            // Sécurité : on régénère le code côté serveur, même si le formulaire en a soumis un.
            teacher.Code = Teacher.GenerateUniqueCode();
            int newId = DB.Teachers.Add(teacher);
            return RedirectToAction("Details", new { id = newId });
        }

        // ----------------------------------------------------------------------
        // Vue de modification d'un enseignant
        // ----------------------------------------------------------------------

        [UserAccess(Access.Write)]
        public ActionResult Edit()
        {
            int id = Session["CurrentTeacherId"] != null ? (int)Session["CurrentTeacherId"] : 0;
            if (id == 0) return RedirectToAction("List");
            Teacher teacher = DB.Teachers.Get(id);
            if (teacher == null) return RedirectToAction("List");

            ViewBag.Action = "Edit";
            ViewBag.Creating = false;
            // Double liste pour les allocations de la prochaine session :
            //  - Gauche : cours déjà alloués
            //  - Droite : tous les cours de la prochaine session
            ViewBag.Allocations = teacher.NextSessionCoursesToSelectList;
            ViewBag.Courses = Course.NextSessionToSelectList;
            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAccess(Access.Write)]
        public ActionResult Edit(Teacher teacher, List<int> selectedCoursesId)
        {
            int id = Session["CurrentTeacherId"] != null ? (int)Session["CurrentTeacherId"] : 0;
            Teacher stored = DB.Teachers.Get(id);
            if (stored == null) return RedirectToAction("List");

            if (!teacher.IsValid())
            {
                ViewBag.Action = "Edit";
                ViewBag.Creating = false;
                ViewBag.Allocations = stored.NextSessionCoursesToSelectList;
                ViewBag.Courses = Course.NextSessionToSelectList;
                return View(teacher);
            }

            // Champs immuables : id et code
            teacher.Id = id;
            teacher.Code = stored.Code;
            // Si le formulaire renvoie un Avatar vide (l'usager n'a pas touché à l'image),
            // on garde l'URL existante. Sinon (base64), ImageAsset s'occupe de la conversion
            // automatiquement dans DB.Teachers.Update.
            if (string.IsNullOrEmpty(teacher.Avatar))
                teacher.Avatar = stored.Avatar;

            DB.Teachers.Update(teacher);
            stored.UpdateAllocations(selectedCoursesId);
            return RedirectToAction("Details", new { id });
        }

        // ----------------------------------------------------------------------
        // Suppression d'un enseignant
        // ----------------------------------------------------------------------

        /// <summary>Supprime le prof + ses allocations.</summary>
        [UserAccess(Access.Write)]
        public ActionResult Delete()
        {
            int id = Session["CurrentTeacherId"] != null ? (int)Session["CurrentTeacherId"] : 0;
            if (id != 0)
            {
                Teacher teacher = DB.Teachers.Get(id);
                if (teacher != null)
                {
                    teacher.DeleteAllAllocations();
                    DB.Teachers.Delete(id);
                }
                ResetCurrentTeacherInfo();
            }
            return RedirectToAction("List");
        }

        // ----------------------------------------------------------------------
        // Validation AJAX d'unicité du courriel
        // ----------------------------------------------------------------------

        [UserAccess(Access.Write)]
        public JsonResult CheckEmailConflict(string Email)
        {
            // Courriel optionnel chez les enseignants - pas de conflit si vide
            if (string.IsNullOrWhiteSpace(Email))
                return Json(false, JsonRequestBehavior.AllowGet);
            int id = Session["CurrentTeacherId"] != null ? (int)Session["CurrentTeacherId"] : 0;
            bool conflict = DB.Teachers.ToList()
                              .Any(t => string.Equals(t.Email, Email, StringComparison.OrdinalIgnoreCase) && t.Id != id);
            return Json(conflict, JsonRequestBehavior.AllowGet);
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
