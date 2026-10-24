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
    /// Contrôleur pour la gestion des étudiants du programme 420.
    /// Suit le patron MVC du cours (cf. ContactsController de M. Chourot).
    /// </summary>
    public class StudentsController : Controller
    {
        // ----------------------------------------------------------------------
        // Variables de session utilisées par ce contrôleur :
        //  - CurrentStudentId    : id de l'étudiant courant (Détails / Edit / Delete)
        //  - CurrentStudentName  : nom de l'étudiant courant (boîtes de confirmation)
        //  - Search              : true si la barre de recherche est ouverte
        //  - SearchString        : chaîne de recherche courante
        //  - CurrentSelectedYear : 0 = toutes les cohortes, sinon année spécifique
        //  - StudentsYearsList   : liste des cohortes existant en BD
        //
        // Session est un dictionnaire propre à chaque usager connecté.
        // Sa durée de vie est de 20 minutes par défaut (cf. Web.config).
        // ----------------------------------------------------------------------

        /// <summary>
        /// Initialise les variables de session si elles n'existent pas encore.
        /// Appelée au début de chaque action publique pour éviter les NullRef.
        /// </summary>
        private void InitSessionVariables()
        {
            if (Session["CurrentStudentId"] == null)    Session["CurrentStudentId"] = 0;
            if (Session["CurrentStudentName"] == null)  Session["CurrentStudentName"] = "";
            if (Session["Search"] == null)              Session["Search"] = false;
            if (Session["SearchString"] == null)        Session["SearchString"] = "";
            if (Session["CurrentSelectedYear"] == null) Session["CurrentSelectedYear"] = 0;
            if (Session["StudentsYearsList"] == null)   CompileStudentsYearsList();
        }

        /// <summary>
        /// Recompile la liste des années (cohortes) ayant au moins un étudiant.
        /// Sert à alimenter le menu déroulant du filtre d'année.
        /// </summary>
        private void CompileStudentsYearsList()
        {
            List<int> years = DB.Students.ToList()
                                .Select(s => s.Year)
                                .Distinct()
                                .OrderByDescending(y => y)
                                .ToList();
            Session["StudentsYearsList"] = years;
        }

        /// <summary>
        /// Réinitialise les références à l'étudiant courant.
        /// Appelée au retour à la liste pour éviter les fuites de contexte.
        /// </summary>
        private void ResetCurrentStudentInfo()
        {
            Session["CurrentStudentId"] = 0;
            Session["CurrentStudentName"] = "";
        }

        // ----------------------------------------------------------------------
        // Vue liste des étudiants
        // ----------------------------------------------------------------------

        /// <summary>
        /// Vue principale de la liste des étudiants.
        /// La liste elle-même est rendue par GetStudents (AJAX, aux 5 secondes).
        /// </summary>
        [UserAccess(Access.View)]
        public ActionResult List()
        {
            InitSessionVariables();
            ResetCurrentStudentInfo();
            return View();
        }

        /// <summary>
        /// Action AJAX qui produit la vue partielle des étudiants filtrés.
        /// Appelée par AutoRefreshedPanel toutes les 5 secondes.
        /// Retourne null si rien n'a changé : optimise la bande passante et évite
        /// de réécraser le DOM inutilement.
        /// </summary>
        [UserAccess(Access.View)]
        public ActionResult GetStudents(bool forceRefresh = false)
        {
            IEnumerable<Student> result = null;
            // forceRefresh est true quand la vue est produite pour la première fois,
            // ou suite à un changement de filtre côté client.
            // DB.Students.HasChanged est true si un étudiant a été modifié depuis le dernier appel.
            if (forceRefresh || DB.Students.HasChanged || DB.Registrations.HasChanged)
            {
                InitSessionVariables();
                bool search = (bool)Session["Search"];
                string searchString = (string)Session["SearchString"];
                int selectedYear = (int)Session["CurrentSelectedYear"];

                // Démarre avec tous les étudiants
                result = DB.Students.ToList();

                // Filtrage par chaîne de recherche (nom, prénom ou code)
                if (search && !string.IsNullOrEmpty(searchString))
                {
                    result = result.Where(s =>
                        (s.FirstName ?? "").ToLower().Contains(searchString) ||
                        (s.LastName  ?? "").ToLower().Contains(searchString) ||
                        (s.Code      ?? "").ToLower().Contains(searchString));
                }

                // Filtrage par année (0 = toutes)
                if (selectedYear != 0)
                {
                    result = result.Where(s => s.Year == selectedYear);
                }

                // Recompile la liste des années pour le menu déroulant
                CompileStudentsYearsList();

                // Tri : cohortes décroissantes, puis alphabétique par nom
                return PartialView(result.OrderByDescending(s => s.Year)
                                         .ThenBy(s => s.LastName)
                                         .ThenBy(s => s.FirstName));
            }
            return null;
        }

        /// <summary>
        /// Active/désactive la barre de recherche (icône loupe du menu).
        /// </summary>
        [UserAccess(Access.View)]
        public ActionResult ToggleSearch()
        {
            if (Session["Search"] == null) Session["Search"] = false;
            Session["Search"] = !(bool)Session["Search"];

            // En refermant on remet à zéro les filtres pour éviter
            // une recherche cachée qui masquerait des résultats.
            if (!(bool)Session["Search"])
            {
                Session["SearchString"] = "";
                Session["CurrentSelectedYear"] = 0;
            }
            return RedirectToAction("List");
        }

        /// <summary>
        /// Action AJAX : met à jour la chaîne de recherche en session.
        /// Le panneau d'auto-refresh sera ensuite rappelé côté client.
        /// </summary>
        [UserAccess(Access.View)]
        public ActionResult SetSearchString(string value)
        {
            Session["SearchString"] = (value ?? "").ToLower();
            return new EmptyResult();
        }

        /// <summary>
        /// Action AJAX : met à jour l'année sélectionnée dans le filtre.
        /// </summary>
        [UserAccess(Access.View)]
        public ActionResult SetSearchYear(int value)
        {
            Session["CurrentSelectedYear"] = value;
            return new EmptyResult();
        }

        // ----------------------------------------------------------------------
        // Vue détails d'un étudiant
        // ----------------------------------------------------------------------

        /// <summary>
        /// Affiche les détails d'un étudiant (informations + ses inscriptions).
        /// Stocke l'id dans Session pour les actions Edit/Delete subséquentes,
        /// pour qu'elles ne soient pas appelables avec un id arbitraire (sécurité).
        /// </summary>
        [UserAccess(Access.View)]
        public ActionResult Details(int id)
        {
            InitSessionVariables();
            Student student = DB.Students.Get(id);
            if (student != null)
            {
                Session["CurrentStudentId"] = id;
                Session["CurrentStudentName"] = student.FullName;
                return View(student);
            }
            return RedirectToAction("List");
        }

        /// <summary>
        /// Action AJAX : rafraîchit la liste d'inscriptions de l'étudiant
        /// courant (si un autre usager modifie ses inscriptions).
        /// </summary>
        [UserAccess(Access.View)]
        public ActionResult GetStudentRegistrations(bool forceRefresh = false)
        {
            int id = Session["CurrentStudentId"] != null ? (int)Session["CurrentStudentId"] : 0;
            if (id == 0) return null;

            if (forceRefresh || DB.Registrations.HasChanged || DB.Courses.HasChanged)
            {
                Student student = DB.Students.Get(id);
                if (student == null) return null;
                return PartialView(student);
            }
            return null;
        }

        // ----------------------------------------------------------------------
        // Vue d'ajout d'étudiant
        // ----------------------------------------------------------------------

        /// <summary>
        /// Formulaire de création d'un nouvel étudiant.
        /// Le code est pré-généré (format [Année][6 chiffres aléatoires]).
        /// </summary>
        [UserAccess(Access.Write)]
        public ActionResult Create()
        {
            ViewBag.Action = "Create";
            ViewBag.Creating = true;
            // Code généré au format [Année courante][6 chiffres], cf. PDF
            return View("Edit", new Student { Code = Student.GenerateUniqueCode() });
        }

        /// <summary>
        /// Réception du formulaire de création.
        /// L'attribut [ValidateAntiForgeryToken] valide le jeton anti-CSRF.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAccess(Access.Write)]
        public ActionResult Create(Student student)
        {
            if (!student.IsValid())
            {
                // En cas d'invalidité, on renvoie le formulaire avec le modèle
                // pour que les champs restent remplis.
                ViewBag.Action = "Create";
                ViewBag.Creating = true;
                return View("Edit", student);
            }
            // Régénère un code propre côté serveur (sécurité contre injection
            // d'un code arbitraire via le formulaire).
            student.Code = Student.GenerateUniqueCode();
            int newId = DB.Students.Add(student);
            return RedirectToAction("Details", new { id = newId });
        }

        // ----------------------------------------------------------------------
        // Vue de modification d'un étudiant
        // ----------------------------------------------------------------------

        /// <summary>
        /// Formulaire de modification. L'id provient de la session (mis par Details),
        /// pas de l'URL : empêche un usager malveillant de modifier un étudiant
        /// qu'il n'a pas consulté.
        /// </summary>
        [UserAccess(Access.Write)]
        public ActionResult Edit()
        {
            int id = Session["CurrentStudentId"] != null ? (int)Session["CurrentStudentId"] : 0;
            if (id == 0) return RedirectToAction("List");

            Student student = DB.Students.Get(id);
            if (student == null) return RedirectToAction("List");

            ViewBag.Action = "Edit";
            ViewBag.Creating = false;

            // Données pour le contrôle à double liste (cf. PDF page 11) :
            //  - liste de gauche : cours déjà sélectionnés pour la prochaine session
            //  - liste de droite : tous les cours disponibles dans la prochaine session
            ViewBag.Registrations = student.NextSessionCoursesToSelectList;
            ViewBag.Courses = Course.NextSessionToSelectList;
            return View(student);
        }

        /// <summary>
        /// Réception du formulaire de modification.
        /// Le paramètre selectedCoursesId est le nom utilisé par le helper SelectionLists.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAccess(Access.Write)]
        public ActionResult Edit(Student student, List<int> selectedCoursesId)
        {
            int id = Session["CurrentStudentId"] != null ? (int)Session["CurrentStudentId"] : 0;
            Student stored = DB.Students.Get(id);
            if (stored == null) return RedirectToAction("List");

            if (!student.IsValid())
            {
                // Si invalide, on renvoie le formulaire en gardant les données
                ViewBag.Action = "Edit";
                ViewBag.Creating = false;
                ViewBag.Registrations = stored.NextSessionCoursesToSelectList;
                ViewBag.Courses = Course.NextSessionToSelectList;
                return View(student);
            }

            // On force l'id et le code stockés en BD : l'usager ne peut PAS
            // modifier ces champs même s'il tente de les injecter via le formulaire.
            student.Id = id;
            student.Code = stored.Code;

            DB.Students.Update(student);
            // Mise à jour des inscriptions de la prochaine session
            stored.UpdateRegistrations(selectedCoursesId);
            return RedirectToAction("Details", new { id });
        }

        // ----------------------------------------------------------------------
        // Suppression d'un étudiant
        // ----------------------------------------------------------------------

        /// <summary>
        /// Supprime l'étudiant courant ET toutes ses inscriptions (cascade).
        /// </summary>
        [UserAccess(Access.Write)]
        public ActionResult Delete()
        {
            int id = Session["CurrentStudentId"] != null ? (int)Session["CurrentStudentId"] : 0;
            if (id != 0)
            {
                Student student = DB.Students.Get(id);
                if (student != null)
                {
                    // Cascade : on doit retirer toutes les inscriptions de l'étudiant
                    // pour ne pas laisser de Registrations orphelines en BD.
                    student.DeleteAllRegistrations();
                    DB.Students.Delete(id);
                }
                ResetCurrentStudentInfo();
            }
            return RedirectToAction("List");
        }

        // ----------------------------------------------------------------------
        // Validation AJAX d'unicité du courriel
        // (cf. addConflictValidation dans validation.js)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Retourne true (JSON) si le courriel est déjà utilisé par un AUTRE étudiant.
        /// Le formulaire désactive le bouton submit tant que la réponse est true.
        /// </summary>
        [UserAccess(Access.Write)]
        public JsonResult CheckEmailConflict(string Email)
        {
            int id = Session["CurrentStudentId"] != null ? (int)Session["CurrentStudentId"] : 0;
            bool conflict = DB.Students.ToList()
                              .Any(s => string.Equals(s.Email, Email, StringComparison.OrdinalIgnoreCase) && s.Id != id);
            // JsonRequestBehavior.AllowGet : obligatoire pour la vérification CORS du navigateur
            return Json(conflict, JsonRequestBehavior.AllowGet);
        }

        /// <summary>Page À propos.</summary>
        public ActionResult About()
        {
            return View();
        }
    }
}
