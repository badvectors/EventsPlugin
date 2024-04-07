using System.Collections.Generic;

namespace EventsPlugin
{
    public class Event
    {
        public string Name { get; set; }
        public string[] Urls { get; set; }
        public List<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
