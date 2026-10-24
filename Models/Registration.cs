using DAL;
using Newtonsoft.Json;

namespace Models
{
    /// <summary>
    /// Inscription d'un étudiant à un cours pour une année donnée.
    /// Table de jointure entre Student et Course (cf. PDF page 18).
    /// </summary>
    public class Registration : Record
    {
        public Registration()
        {
            // Par défaut, une nouvelle inscription cible la prochaine session
            Year = NextSession.Year;
        }

        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int Year { get; set; }

        // Navigation properties
        [JsonIgnore]
        public Course Course => DB.Courses.Get(CourseId);

        [JsonIgnore]
        public Student Student => DB.Students.Get(StudentId);

        /// <summary>
        /// True si cette inscription correspond à la prochaine session ouverte
        /// (même année que NextSession.Year ET session du cours dans ValidSessions).
        /// </summary>
        [JsonIgnore]
        public bool IsNextSession =>
            Year == NextSession.Year
            && Course != null
            && NextSession.ValidSessions.Contains(Course.Session);
    }
}
