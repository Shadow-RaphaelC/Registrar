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
    /// Contrôleur pour la gestion des cours du programme 420.
    /// Suit le même patron que StudentsController.
    /// </summary>
    public class CoursesController : Controller
    {
        // Variables de session utilisées :
        //  - CurrentCourseId    : id du cours courant
        //  - CurrentCourseTitle : titre du cours courant
        //  - CoursesSearch      : true si recherche active
        //  - CoursesSearchString : chaîne de recherche
        private void InitSessionVariables()
        {
            if (Session["CurrentCourseId"] == null)      Session["CurrentCourseId"] = 0;
            if (Session["CurrentCourseTitle"] == null)   Session["CurrentCourseTitle"] = "";
            if (Session["CoursesSearch"] == null)        Session["CoursesSearch"] = false;
            if (Session["CoursesSearchString"] == null)  Session["CoursesSearchString"] = "";
        }

        private void ResetCurrentCourseInfo()
        {
            Session["CurrentCourseId"] = 0;
            Session["CurrentCourseTitle"] = "";
        }

        // ----------------------------------------------------------------------
        // Vue liste des cours
        // ----------------------------------------------------------------------

        /// <summary>Vue principale - liste des cours regroupés par session.</summary>
        [UserAccess(Access.View)]
        public ActionResult List()
        {
            InitSessionVariables();
            ResetCurrentCourseInfo();
            return View();
        }

        /// <summary>
        /// Action AJAX qui produit la vue partielle des cours filtrés.
        /// Retourne null si rien n'a changé.
        /// </summary>
        [UserAccess(Access.View)]
        public ActionResult GetCourses(bool forceRefresh = false)
        {
            IEnumerable<Course> result = null;
            if (forceRefresh || DB.Courses.HasChanged)
            {
                InitSessionVariables();
                bool search = (bool)Session["CoursesSearch"];
                string searchString = (string)Session["CoursesSearchString"];

                result = DB.Courses.ToList();

                if (search && !string.IsNullOrEmpty(searchString))
                {
                    result = result.Where(c =>
                        (c.Code  ?? "").ToLower().Contains(searchString) ||
                        (c.Title ?? "").ToLower().Contains(searchString));
                }
                // Tri par session ascendante puis par sigle (cf. PDF page 6)
                return PartialView(result.OrderBy(c => c.Session).ThenBy(c => c.Code));
            }
            return null;
        }

        /// <summary>Active/désactive la barre de recherche.</summary>
        [UserAccess(Access.View)]
        public ActionResult ToggleSearch()
        {
            if (Session["CoursesSearch"] == null) Session["CoursesSearch"] = false;
            Session["CoursesSearch"] = !(bool)Session["CoursesSearch"];
            if (!(bool)Session["CoursesSearch"])
                Session["CoursesSearchString"] = "";
            return RedirectToAction("List");
        }

        /// <summary>Action AJAX : met à jour la chaîne de recherche en session.</summary>
        [UserAccess(Access.View)]
        public ActionResult SetSearchString(string value)
        {
            Session["CoursesSearchString"] = (value ?? "").ToLower();
            return new EmptyResult();
        }

        // ----------------------------------------------------------------------
        // Vue détails d'un cours
        // ----------------------------------------------------------------------

        /// <summary>Affiche les détails du cours + ses inscriptions par cohorte.</summary>
        [UserAccess(Access.View)]
        public ActionResult Details(int id)
        {
            InitSessionVariables();
            Course course = DB.Courses.Get(id);
            if (course != null)
            {
                Session["CurrentCourseId"] = id;
                Session["CurrentCourseTitle"] = course.Title;
                return View(course);
            }
            return RedirectToAction("List");
        }

        /// <summary>Action AJAX : rafraîchit la liste des étudiants inscrits + profs assignés.</summary>
        [UserAccess(Access.View)]
        public ActionResult GetCourseRegistrations(bool forceRefresh = false)
        {
            int id = Session["CurrentCourseId"] != null ? (int)Session["CurrentCourseId"] : 0;
            if (id == 0) return null;

            if (forceRefresh || DB.Registrations.HasChanged
                            || DB.Allocations.HasChanged
                            || DB.Students.HasChanged
                            || DB.Teachers.HasChanged)
            {
                Course course = DB.Courses.Get(id);
                if (course == null) return null;
                return PartialView(course);
            }
            return null;
        }

        // ----------------------------------------------------------------------
        // Vue d'ajout de cours
        // ----------------------------------------------------------------------

        /// <summary>Formulaire de création d'un nouveau cours.</summary>
        [UserAccess(Access.Write)]
        public ActionResult Create()
        {
            ViewBag.Action = "Create";
            ViewBag.Creating = true;
            // Pré-rempli avec une session valide pour la session courante.
            return View("Edit", new Course { Session = NextSession.ValidSessions.FirstOrDefault() });
        }

        /// <summary>Réception du formulaire de création.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAccess(Access.Write)]
        public ActionResult Create(Course course)
        {
            // IsValid vérifie : sigle non vide, titre non vide, session entre 1 et 6
            // CodeConflict vérifie : aucun autre cours ne porte ce sigle
            if (!course.IsValid() || course.CodeConflict())
            {
                ViewBag.Action = "Create";
                ViewBag.Creating = true;
                return View("Edit", course);
            }
            int newId = DB.Courses.Add(course);
            return RedirectToAction("Details", new { id = newId });
        }

        // ----------------------------------------------------------------------
        // Vue de modification d'un cours
        // ----------------------------------------------------------------------

        /// <summary>Formulaire de modification du cours courant.</summary>
        [UserAccess(Access.Write)]
        public ActionResult Edit()
        {
            int id = Session["CurrentCourseId"] != null ? (int)Session["CurrentCourseId"] : 0;
            if (id == 0) return RedirectToAction("List");
            Course course = DB.Courses.Get(id);
            if (course == null) return RedirectToAction("List");

            ViewBag.Action = "Edit";
            ViewBag.Creating = false;
            // Double liste pour les inscriptions de la prochaine session :
            //  - Gauche : étudiants déjà inscrits
            //  - Droite : tous les étudiants disponibles
            ViewBag.Registrations = course.NextSessionStudentsToSelectList;
            ViewBag.Students = Student.AllStudentsToSelectList();
            return View(course);
        }

        /// <summary>Réception du formulaire de modification.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAccess(Access.Write)]
        public ActionResult Edit(Course course, List<int> selectedCoursesId)
        {
            int id = Session["CurrentCourseId"] != null ? (int)Session["CurrentCourseId"] : 0;
            Course stored = DB.Courses.Get(id);
            if (stored == null) return RedirectToAction("List");

            if (!course.IsValid())
            {
                ViewBag.Action = "Edit";
                ViewBag.Creating = false;
                ViewBag.Registrations = stored.NextSessionStudentsToSelectList;
                ViewBag.Students = Student.AllStudentsToSelectList();
                return View(course);
            }

            // On corrige l'id puis on vérifie l'unicité du sigle (sauf le cours en cours)
            course.Id = id;
            if (course.CodeConflict())
            {
                ModelState.AddModelError("Code", "Ce sigle est déjà utilisé.");
                ViewBag.Action = "Edit";
                ViewBag.Creating = false;
                ViewBag.Registrations = stored.NextSessionStudentsToSelectList;
                ViewBag.Students = Student.AllStudentsToSelectList();
                return View(course);
            }

            DB.Courses.Update(course);
            // Le helper SelectionLists envoie les ids sous le nom "selectedCoursesId"
            // (même nom partout - c'est le contrôle qui est générique).
            stored.UpdateNextSessionRegistrations(selectedCoursesId);
            return RedirectToAction("Details", new { id });
        }

        // ----------------------------------------------------------------------
        // Suppression d'un cours
        // ----------------------------------------------------------------------

        /// <summary>Supprime le cours courant + toutes ses inscriptions et allocations.</summary>
        [UserAccess(Access.Write)]
        public ActionResult Delete()
        {
            int id = Session["CurrentCourseId"] != null ? (int)Session["CurrentCourseId"] : 0;
            if (id != 0)
            {
                Course course = DB.Courses.Get(id);
                if (course != null)
                {
                    // Cascade : on retire toutes les inscriptions et allocations
                    // pour ne pas laisser de données orphelines.
                    course.DeleteAllRegistrations();
                    course.DeleteAllAllocations();
                    DB.Courses.Delete(id);
                }
                ResetCurrentCourseInfo();
            }
            return RedirectToAction("List");
        }

        // ----------------------------------------------------------------------
        // Validation AJAX d'unicité du sigle (CheckCodeConflict)
        // ----------------------------------------------------------------------

        /// <summary>Retourne true (JSON) si le sigle est déjà utilisé par un autre cours.</summary>
        [UserAccess(Access.Write)]
        public JsonResult CheckCodeConflict(string Code)
        {
            int id = Session["CurrentCourseId"] != null ? (int)Session["CurrentCourseId"] : 0;
            bool conflict = DB.Courses.ToList()
                              .Any(c => string.Equals(c.Code, Code, StringComparison.OrdinalIgnoreCase) && c.Id != id);
            return Json(conflict, JsonRequestBehavior.AllowGet);
        }

        public ActionResult About()
        {
            return View();
        }
    }
}
