using DAL;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Models
{
    public class Course : Record
    {
        // Champs persistés dans Courses.json
        public string Code { get; set; }       // ex. "420-KB1-LG"
        public string Title { get; set; }      // ex. "Introduction à la programmation"
        public int Session { get; set; }       // 1 à 6 (cf. structure du programme)

        #region Navigation properties (calculées, pas persistées)

        /// <summary>Format d'affichage utilisé partout : "[session] Sigle Titre".</summary>
        [JsonIgnore] public string Caption => "[" + Session + "] " + Code + " " + Title;

        /// <summary>Toutes les inscriptions (toutes années confondues) à ce cours.</summary>
        [JsonIgnore]
        public List<Registration> Registrations =>
            DB.Registrations.ToList().Where(r => r.CourseId == Id).ToList();

        /// <summary>Inscriptions de la prochaine session uniquement.</summary>
        [JsonIgnore]
        public List<Registration> NextSessionRegistrations =>
            DB.Registrations.ToList().Where(r => r.CourseId == Id && r.IsNextSession).ToList();

        /// <summary>Étudiants inscrits à ce cours, peu importe l'année.</summary>
        [JsonIgnore]
        public List<Student> Students
        {
            get
            {
                var students = new List<Student>();
                foreach (var registration in Registrations.OrderBy(r => r.Student?.LastName))
                {
                    if (registration.Student != null)
                        students.Add(registration.Student);
                }
                return students;
            }
        }

        /// <summary>Étudiants inscrits à ce cours pour la prochaine session.</summary>
        [JsonIgnore]
        public List<Student> NextSessionStudents
        {
            get
            {
                var students = new List<Student>();
                foreach (var registration in NextSessionRegistrations.OrderBy(r => r.Student?.LastName))
                {
                    if (registration.Student != null)
                        students.Add(registration.Student);
                }
                return students;
            }
        }

        /// <summary>Allocations (assignations à des enseignants) toutes années.</summary>
        [JsonIgnore]
        public List<Allocation> Allocations =>
            DB.Allocations.ToList().Where(a => a.CourseId == Id).ToList();

        /// <summary>
        /// Liste utilisée par la vue de modification : pour le multi-select des
        /// étudiants déjà inscrits dans ce cours pour la prochaine session.
        /// </summary>
        [JsonIgnore]
        public SelectList NextSessionStudentsToSelectList =>
            SelectListUtilities<Student>.Convert(NextSessionStudents, "Caption");

        /// <summary>Cours dont la session figure dans NextSession.ValidSessions, format SelectList.
        /// Utilisé par la vue Étudiant-Modification (liste de droite des cours disponibles).</summary>
        public static SelectList NextSessionToSelectList
        {
            get
            {
                var validSessions = NextSession.ValidSessions;
                var courses = DB.Courses.ToList()
                    .Where(c => validSessions.Contains(c.Session))
                    .OrderBy(c => c.Code);
                return SelectListUtilities<Course>.Convert(courses, "Caption");
            }
        }

        #endregion

        #region Helpers de modification

        /// <summary>Supprime toutes les inscriptions liées à ce cours.</summary>
        public void DeleteAllRegistrations()
        {
            foreach (Registration registration in Registrations)
                DB.Registrations.Delete(registration.Id);
        }

        /// <summary>Supprime les allocations liées à ce cours.</summary>
        public void DeleteAllAllocations()
        {
            foreach (Allocation allocation in Allocations)
                DB.Allocations.Delete(allocation.Id);
        }

        /// <summary>Supprime les inscriptions de la prochaine session uniquement.</summary>
        public void DeleteNextSessionRegistrations()
        {
            foreach (Registration registration in NextSessionRegistrations)
                DB.Registrations.Delete(registration.Id);
        }

        /// <summary>
        /// Remplace l'ensemble des étudiants inscrits pour la prochaine session.
        /// Pattern fourni dans le PDF : on supprime ceux de la prochaine session
        /// puis on ajoute la nouvelle sélection.
        /// </summary>
        public void UpdateNextSessionRegistrations(List<int> selectedStudentsId)
        {
            DeleteNextSessionRegistrations();
            if (selectedStudentsId != null)
            {
                foreach (int studentId in selectedStudentsId)
                {
                    DB.Registrations.Add(new Registration { StudentId = studentId, CourseId = Id });
                }
            }
        }

        #endregion

        #region Validation

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Code)) return false;
            if (string.IsNullOrWhiteSpace(Title)) return false;
            if (Session < 1 || Session > 6) return false;
            return true;
        }

        /// <summary>Détecte les sigles dupliqués (à utiliser avant Add ou Update).</summary>
        public bool CodeConflict()
        {
            return DB.Courses.ToList().Any(c => c.Code == Code && c.Id != Id);
        }

        #endregion
    }
}
