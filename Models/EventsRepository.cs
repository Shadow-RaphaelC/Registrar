using DAL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models
{
    public class EventsRepository : Repository<Event>
    {
        public void Add(string action, string text = "")
        {
            Event @event = new Event
            {
                UserId = User.ConnectedUser != null ? User.ConnectedUser.Id : 0,
                CreationDate = DateTime.Now,
                Action = action,
                Comment = text
            };
            Add(@event);
        }
        public bool DeleteEventsJournalDay(DateTime day)
        {
            try
            {
                BeginTransaction();
                DateTime dayAfter = day.AddDays(1);
                List<Event> events = ToList().Where(l => l.CreationDate >= day && l.CreationDate < dayAfter).ToList();
                // Notice: You can delete items of List<T> collection in a foreach loop but it will fail with items of IEnumerable<T> collection
                foreach (Event @event in events.Copy())
                {
                    Delete(@event.Id);
                }
                EndTransaction();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteEventJournalDay failed : Message - {ex.Message}");
                EndTransaction();
                return false;
            }
        }
    }
}