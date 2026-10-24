using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models
{
    public class LoginsRepository : Repository<Login>
    {
        public int Add(int userId)
        {
            try
            {
                Login login = new Login();
                login.LoginDate = login.LogoutDate = DateTime.Now;
                login.UserId = userId;
                login.IpAddress = HttpContext.Current.Request.UserHostAddress;
                //login.IpAddress = "144.172.187.215";
                if (login.IpAddress != "::1")
                {
                    WebServices.GeoLocation gl = WebServices.GeoLocation.Call(login.IpAddress);
                    login.City = gl.city;
                    login.RegionName = gl.regionName;
                    login.CountryCode = gl.countryCode;
                }
                login.Id = Add(login);
                return login.Id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddLogin failed : Message - {ex.Message}");
                return 0;
            }
        }
        public bool UpdateLogout(int loginId)
        {
            try
            {
                Login login = Get(loginId);
                if (login != null)
                {
                    login.LogoutDate = DateTime.Now;
                    return Update(login);
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateLogout failed : Message - {ex.Message}");
                return false;
            }
        }
        public bool UpdateLogoutByUserId(int userId)
        {
            try
            {
                Login login = ToList().Where(l => l.UserId == userId).OrderByDescending(l => l.LoginDate).FirstOrDefault();
                if (login != null)
                {
                    login.LogoutDate = DateTime.Now;
                    return Update(login);
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateLogoutByUserId failed : Message - {ex.Message}");
                return false;
            }
        }
        public bool DeleteLoginsJournalDay(DateTime day)
        {
            try
            {
                BeginTransaction();
                DateTime dayAfter = day.AddDays(1);
                List<Login> logins = ToList().Where(l => l.LoginDate >= day && l.LoginDate < dayAfter).ToList();
                // Notice: You can delete items of List<T> collection in a foreach loop but it will fail with items of IEnumerable<T> collection
                foreach (Login login in logins.Copy())
                {
                    Delete(login.Id);
                }
                EndTransaction();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteLoginsJournalDay failed : Message - {ex.Message}");
                EndTransaction();
                return false;
            }
        }
    }

}