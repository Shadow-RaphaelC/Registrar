using EmailHandling;
using Models;

namespace DAL
{
    public sealed class DB
    {
        #region singleton setup
        private static readonly DB instance = new DB();
        public static DB Instance { get { return instance; } }
        #endregion

        // ---------- Account-related repositories (kept from base project) ----------

        static public UsersRepository Users { get; set; }
            = new UsersRepository();

        static public NotificationsRepository Notifications { get; set; }
            = new NotificationsRepository();

        static public LoginsRepository Logins { get; set; }
            = new LoginsRepository();

        static public EventsRepository Events { get; set; }
            = new EventsRepository();

        static public Repository<UnverifiedEmail> UnverifiedEmails { get; set; }
            = new Repository<UnverifiedEmail>();

        static public Repository<RenewPasswordCommand> RenewPasswordCommands { get; set; }
            = new Repository<RenewPasswordCommand>();

        // ---------- Registrar-domain repositories ----------

        static public Repository<Course> Courses { get; set; }
            = new Repository<Course>();

        static public Repository<Student> Students { get; set; }
            = new Repository<Student>();

        static public Repository<Teacher> Teachers { get; set; }
            = new Repository<Teacher>();

        static public Repository<Registration> Registrations { get; set; }
            = new Repository<Registration>();

        static public Repository<Allocation> Allocations { get; set; }
            = new Repository<Allocation>();
    }
}
