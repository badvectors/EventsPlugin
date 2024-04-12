using System;

namespace EventsPlugin
{
    public class Booking
    {
        public string CID { get; set; }
        public string Callsign { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Type { get; set; }
        public string CTOT { get; set; }
        public string ETA { get; set; }

        public string CTOB()
        {
            if (CTOT == null || CTOT.Length != 4) return string.Empty;

            var hourString = CTOT.Substring(0, 2);

            var minuteString = CTOT.Substring(2, 2);

            var hourOK = int.TryParse(hourString, out var hour);

            var minuteOK = int.TryParse(minuteString, out var minute);

            if (!hourOK || !minuteOK) return string.Empty;

            var ctot = new DateTime(DateTime.UtcNow.Year, 
                DateTime.UtcNow.Month, 
                DateTime.UtcNow.Day, hour, minute, 0);

            var ctob = ctot.AddMinutes(-10);

            return ctob.ToString("HHmm");
        }
    }
}
