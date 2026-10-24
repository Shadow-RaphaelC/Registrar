using DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace Models
{
    public class Teacher : Record
    {
        // Champs persistés dans Teachers.json
        public string Code { get; set; }              // ex. "CLG-420-11412"
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;
        public string Email { get; set; }
        public string Phone { get; set; }

        // Image stockée hors-BD via le système ImageAsset (idem User)
        const string Avatars_Folder = @"/App_Assets/Teachers/";
        const string Default_Avatar = @"no_avatar.png";

        [ImageAsset(Avatars_Folder, Default_Avatar)]
        public string Avatar { get; set; } = Avatars_Folder + Default_Avatar;

        #region Navigation properties

        [JsonIgnore] public string FullName => LastName + " " + FirstName;
        [JsonIgnore] public string Caption => Code + " " + LastName + " " + FirstName;

        /// <summary>Nombre d'années (avec décimales) depuis l'embauche.</summary>
        [JsonIgnore]
        public double Seniority
        {
            get
            {
                TimeSpan span = DateTime.Now - StartDate;
                return Math.Round(span.TotalDays / 365.25, 1);
            }
        }

        [JsonIgnore]
        public List<Allocation> Allocations =>
            DB.Allocations.ToList().Where(a => a.TeacherId == Id).ToList();

        [JsonIgnore]
        public List<Allocation> NextSessionAllocations =>
            DB.Allocations.ToList().Where(a => a.TeacherId == Id && a.IsNextSession).ToList();

        /// <summary>Tous les cours assignés à ce prof, peu importe l'année.</summary>
        [JsonIgnore]
        public List<Course> Courses
        {
            get
            {
                var courses = new List<Course>();
                foreach (var allocation in Allocations.OrderBy(a => a.Course?.Code))
                {
                    if (allocation.Course != null)
                        courses.Add(allocation.Course);
                }
                return courses;
            }
        }

        /// <summary>Cours assignés pour la prochaine session.</summary>
        [JsonIgnore]
        public List<Course> NextSessionCourses
        {
            get
            {
                var courses = new List<Course>();
                foreach (var allocation in NextSessionAllocations.OrderBy(a => a.Course?.Code))
                {
                    if (allocation.Course != null)
                        courses.Add(allocation.Course);
                }
                return courses;
            }
        }

        [JsonIgnore]
        public SelectList NextSessionCoursesToSelectList =>
            SelectListUtilities<Course>.Convert(NextSessionCourses, "Caption");

        #endregion

        #region Helpers de modification

        public void DeleteAllAllocations()
        {
            foreach (Allocation allocation in Allocations)
                DB.Allocations.Delete(allocation.Id);
        }

        public void DeleteNextSessionAllocations()
        {
            foreach (Allocation allocation in NextSessionAllocations)
                DB.Allocations.Delete(allocation.Id);
        }

        /// <summary>Met à jour les allocations de la prochaine session.</summary>
        public void UpdateAllocations(List<int> selectedCoursesId)
        {
            DeleteNextSessionAllocations();
            if (selectedCoursesId != null)
            {
                foreach (int courseId in selectedCoursesId)
                {
                    DB.Allocations.Add(new Allocation { TeacherId = Id, CourseId = courseId });
                }
            }
        }

        #endregion

        #region Validation

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(FirstName)) return false;
            if (string.IsNullOrWhiteSpace(LastName)) return false;
            // Email facultatif (pas dans la JSON de seed) mais s'il est fourni il doit être valide
            if (!string.IsNullOrWhiteSpace(Email) && !IsValidEmail(Email)) return false;
            if (StartDate > DateTime.Now) return false;
            if (StartDate < new DateTime(1950, 1, 1)) return false;
            return true;
        }

        private static bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        #endregion

        #region Génération du code prof

        /// <summary>
        /// Génère un code prof unique au format CLG-420-[5 chiffres] (cf. PDF).
        /// Exemple : CLG-420-11412
        /// </summary>
        public static string GenerateUniqueCode()
        {
            var random = new Random();
            string code;
            int safety = 0;
            do
            {
                int randomPart = random.Next(0, 100000);              // 0 à 99999
                code = "CLG-420-" + randomPart.ToString("D5");        // padding à 5 chiffres
                safety++;
                if (safety > 100) throw new Exception("Impossible de générer un code prof unique.");
            }
            while (DB.Teachers.ToList().Any(t => t.Code == code));
            return code;
        }

        #endregion
    }
}
