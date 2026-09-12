using System;
using System.Collections.Generic;

namespace EventsPlugin
{
    // Shape of the VATPAC bookings API response (https://new.vatpac.org/api/bookings).
    public class Event
    {
        public Guid PilotBookingId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public List<VatpacBookingSlot> Slots { get; set; } = new List<VatpacBookingSlot>();
    }

    public class VatpacBookingSlot
    {
        public Guid PilotBookingSlotId { get; set; }
        public VatpacBookingSlotType Type { get; set; }
        public DateTime Utc { get; set; }
        public string Callsign { get; set; }
        public string Departure { get; set; }
        public string Arrival { get; set; }
        public string AircraftType { get; set; }
        public int? CID { get; set; }
    }

    public enum VatpacBookingSlotType
    {
        Departure,
        Arrival,
        Error
    }
}
