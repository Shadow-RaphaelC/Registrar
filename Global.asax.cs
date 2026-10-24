using DAL;
using EmailHandling;
using Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Timers;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Registrar
{
    public class MvcApplication : System.Web.HttpApplication
    {
        //private static Timer appTimer;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            var culture = new CultureInfo("fr-FR");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            /*
            appTimer = new Timer();
            appTimer.Interval = 10000000; // 10 second in milliseconds
            appTimer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
            appTimer.Enabled = true; // Start the timer
            */
            // cleaning
            foreach (var login in DB.Logins.ToList().Copy())
            {
                if (login.User == null) DB.Logins.Delete(login.Id);
            }
            foreach (var uvEmail in DB.UnverifiedEmails.ToList().Copy())
            {
                if (uvEmail.User == null) DB.UnverifiedEmails.Delete(uvEmail.Id);
            }
            foreach (RenewPasswordCommand renewPC in DB.RenewPasswordCommands.ToList().Copy())
            {
                if (renewPC.User == null) DB.RenewPasswordCommands.Delete(renewPC.Id);
            }
            foreach (Models.Event @event in DB.Events.ToList().Copy())
            {
                if (@event.User == null) DB.Events.Delete(@event.Id);
            }
            foreach (var notification in DB.Notifications.ToList().Copy())
            {
                if (notification.User == null) DB.Notifications.Delete(notification.Id);
            }
            // Nettoyage des inscriptions et allocations orphelines
            // (si un cours/étudiant/prof a été supprimé sans cascade)
            foreach (var registration in DB.Registrations.ToList().Copy())
            {
                if (registration.Student == null || registration.Course == null)
                    DB.Registrations.Delete(registration.Id);
            }
            foreach (var allocation in DB.Allocations.ToList().Copy())
            {
                if (allocation.Teacher == null || allocation.Course == null)
                    DB.Allocations.Delete(allocation.Id);
            }
        }
        /*
        private static void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Your cyclic function code goes here
            // Be careful with threading, as this runs on a thread pool thread
            System.Diagnostics.Debug.WriteLine("Cyclic function ran at: " + DateTime.Now);
            
        }*/

        protected void Session_Start()
        {
            // do session intialisations

        }
        protected void Session_End()
        {
            var connectedUser = Models.User.ConnectedUser;
            if (connectedUser != null)
                connectedUser.Online = false;
        }
        protected void Application_End(object sender, EventArgs e)
        {
            /*if (appTimer != null)
            {
                appTimer.Enabled = false;
                appTimer.Dispose();
            }*/
        }
    }
}
