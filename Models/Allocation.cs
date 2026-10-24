using DAL;
using Newtonsoft.Json;

namespace Models
{
    /// <summary>
    /// Assignation d'un cours à un enseignant pour une année donnée.
    /// Table de jointure entre Teacher et Course (cf. PDF page 18).
    /// </summary>
    public class Allocation : Record
    {
        public Allocation()
        {
            Year = NextSession.Year;
        }

        public int TeacherId { get; set; }
        public int CourseId { get; set; }
        public int Year { get; set; }

        // Navigation properties
        [JsonIgnore]
        public Course Course => DB.Courses.Get(CourseId);

        [JsonIgnore]
        public Teacher Teacher => DB.Teachers.Get(TeacherId);

        /// <summary>
        /// True si cette allocation correspond à la prochaine session ouverte.
        /// </summary>
        [JsonIgnore]
        public bool IsNextSession =>
            Year == NextSession.Year
            && Course != null
            && NextSession.ValidSessions.Contains(Course.Session);
    }
}
