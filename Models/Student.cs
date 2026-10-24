using DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace Models
{
    public class Student : Record
    {
        // Champs persistés dans Students.json
        public string Code { get; set; }            // ex. "202533571" -> [Année][6 chiffres aléatoires]
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime BirthDate { get; set; } = new DateTime(2000, 1, 1);
        public string Email { get; set; }
        public string Phone { get; set; }

        #region Navigation properties (cf. PDF page 13)

        [JsonIgnore] public string FullName => LastName + " " + FirstName;
        [JsonIgnore] public string Caption => Code + " " + LastName + " " + FirstName;

        /// <summary>
        /// Année d'inscription extraite du Code (4 premiers chiffres).
        /// Sert au regroupement « Cohorte 2025 / 2026 » dans la vue liste.
        /// </summary>
        [JsonIgnore]
        public int Year
        {
            get
            {
                if (string.IsNullOrEmpty(Code) || Code.Length < 4) return 0;
                return int.TryParse(Code.Substring(0, 4), out int y) ? y : 0;
            }
        }

        [JsonIgnore]
        public List<Registration> Registrations =>
            DB.Registrations.ToList().Where(r => r.StudentId == Id).ToList();

        [JsonIgnore]
        public List<Registration> NextSessionRegistrations =>
            DB.Registrations.ToList().Where(r => r.StudentId == Id && r.IsNextSession).ToList();

        /// <summary>Tous les cours auxquels l'étudiant est inscrit (toutes années).</summary>
        [JsonIgnore]
        public List<Course> Courses
        {
            get
            {
                var courses = new List<Course>();
                foreach (var registration in Registrations.OrderBy(r => r.Course?.Code))
                {
                    if (registration.Course != null)
                        courses.Add(registration.Course);
                }
                return courses;
            }
        }

        /// <summary>Cours de la prochaine session uniquement.</summary>
        [JsonIgnore]
        public List<Course> NextSessionCourses
        {
            get
            {
                var courses = new List<Course>();
                foreach (var registration in NextSessionRegistrations.OrderBy(r => r.Course?.Code))
                {
                    if (registration.Course != null)
                        courses.Add(registration.Course);
                }
                return courses;
            }
        }

        [JsonIgnore]
        public SelectList CoursesSelectList =>
            SelectListUtilities<Course>.Convert(Courses, "Caption");

        /// <summary>Liste pour le multi-select de gauche (cours déjà sélectionnés).</summary>
        [JsonIgnore]
        public SelectList NextSessionCoursesToSelectList =>
            SelectListUtilities<Course>.Convert(NextSessionCourses, "Caption");

        /// <summary>Tous les étudiants triés par nom, format SelectList.
        /// Utilisé par la vue Cours-Modification pour la liste de droite.</summary>
        public static SelectList AllStudentsToSelectList()
        {
            var allStudents = DB.Students.ToList().OrderBy(s => s.LastName).ThenBy(s => s.FirstName);
            return SelectListUtilities<Student>.Convert(allStudents, "Caption");
        }

        #endregion

        #region Helpers de modification

        public void DeleteAllRegistrations()
        {
            foreach (Registration registration in Registrations)
                DB.Registrations.Delete(registration.Id);
        }

        public void DeleteNextSessionRegistrations()
        {
            foreach (Registration registration in NextSessionRegistrations)
                DB.Registrations.Delete(registration.Id);
        }

        /// <summary>Met à jour les inscriptions de la prochaine session (cf. PDF page 13).</summary>
        public void UpdateRegistrations(List<int> selectedCoursesId)
        {
            DeleteNextSessionRegistrations();
            if (selectedCoursesId != null)
            {
                foreach (int courseId in selectedCoursesId)
                {
                    DB.Registrations.Add(new Registration { StudentId = Id, CourseId = courseId });
                }
            }
        }

        #endregion

        #region Validation

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(FirstName)) return false;
            if (string.IsNullOrWhiteSpace(LastName)) return false;
            if (string.IsNullOrWhiteSpace(Email) || !IsValidEmail(Email)) return false;
            if (BirthDate > DateTime.Now) return false;                    // pas né dans le futur
            if (BirthDate < new DateTime(1900, 1, 1)) return false;        // pas centenaire non plus
            return true;
        }

        private static bool IsValidEmail(string email)
        {
            // Validation syntaxique simple - exigée par le PDF (« validation syntaxique »).
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        #endregion

        #region Génération du code étudiant

        /// <summary>
        /// Génère un code unique au format [Année][6 chiffres] (cf. PDF).
        /// Exemple : 2026123456
        /// </summary>
        public static string GenerateUniqueCode()
        {
            var random = new Random();
            string code;
            int safety = 0;
            do
            {
                int randomPart = random.Next(0, 1000000);            // 0 à 999999
                code = NextSession.Year.ToString() + randomPart.ToString("D6"); // padding à 6 chiffres
                safety++;
                if (safety > 100) throw new Exception("Impossible de générer un code étudiant unique.");
            }
            while (DB.Students.ToList().Any(s => s.Code == code));
            return code;
        }

        #endregion
    }
}
