using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using vatsys;
using vatsys.Plugin;
using Timer = System.Timers.Timer;

namespace EventsPlugin
{
    [Export(typeof(IPlugin))]
    public class Plugin : IStripPlugin, ILabelPlugin
    {
        public string Name => "Events";

        private static readonly HttpClient _httpClient = CreateHttpClient();

        private static readonly string _dataUrl = "https://data.vatsim.net/v3/vatsim-data.json";
        private Timer _dataTimer { get; set; } = new Timer();

        private static CustomToolStripMenuItem _eventsMenu;
        private static EventsWindow _eventsWindow;

#if DEBUG
        // Local copy of the VATPAC site for testing.
        private static readonly string _bookingsUrl = "https://localhost:5254/api/bookings";
#else
        private static readonly string _bookingsUrl = "https://new.vatpac.org/api/bookings";
#endif

        private static Event _event { get; set; }

        // Pilots from the last successful VATSIM data feed fetch, or null if none yet.
        private static Pilot[] _pilots { get; set; }

        // Callsign -> slot for aircraft that should be flagged as event traffic.
        // Rebuilt from the event slots and the VATSIM data feed; replaced atomically.
        private static Dictionary<string, VatpacBookingSlot> _bookings = NewBookings();

        // The current event as last fetched from the API, or null when there is none.
        public static Event CurrentEvent => _event;

        public static string DatasetPath { get; set; }

        public Plugin()
        {
            _eventsMenu = new CustomToolStripMenuItem(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Settings, new ToolStripMenuItem("Event"));
            _eventsMenu.Item.Click += EventsMenu_Click;
            MMI.AddCustomMenuItem(_eventsMenu);

            // Poll straight away on a confirmed ATC login, and clear on disconnect, rather than
            // waiting for the next timer tick.
            Network.ValidATCChanged += (s, e) => _ = Refresh();
            Network.Disconnected += (s, e) => _ = Refresh();

            _ = Refresh();

            _dataTimer.Elapsed += new ElapsedEventHandler(DataTimer_Elapsed);
            _dataTimer.Interval = 60000;
            _dataTimer.AutoReset = false;
            _dataTimer.Start();
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler();
#if DEBUG
            // The local dev server uses a self-signed certificate.
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                errors == System.Net.Security.SslPolicyErrors.None || message.RequestUri.IsLoopback;
#endif
            return new HttpClient(handler);
        }

        private async void DataTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await Refresh();

            _dataTimer.Start();
        }

        private void EventsMenu_Click(object sender, EventArgs e)
        {
            ShowEventsWindow();
        }

        private static void ShowEventsWindow()
        {
            MMI.InvokeOnGUI((MethodInvoker)delegate ()
            {
                if (_eventsWindow == null || _eventsWindow.IsDisposed)
                {
                    _eventsWindow = new EventsWindow();
                }
                else if (_eventsWindow.Visible) return;

                _eventsWindow.Show();
            });
        }

        // Only poll while logged on to an official VATSIM server as a real (non-observer, validated) controller.
        private static bool ShouldPoll =>
            Network.IsConnected && Network.IsOfficialServer && (Network.Me?.IsRealATC ?? false);

        // Fetches the current event and the VATSIM data feed, then rebuilds the bookings map.
        // When not eligible to poll (sweatbox, observer, disconnected) all state is cleared instead.
        public static async Task Refresh()
        {
            if (ShouldPoll)
            {
                await GetEvent();

                await GetVatsimData();
            }
            else
            {
                _event = null;
                _pilots = null;
            }

            UpdateBookings();

            RefreshStrips();

            _eventsWindow?.UpdateDisplay();
        }

        // Fetches the current event, with its bookings, from the VATPAC bookings API.
        // The API returns a single event, or 404 when there is no current event.
        // Whatever it returns becomes the selected event; 404 deselects.
        private static async Task GetEvent()
        {
            try
            {
                using (var response = await _httpClient.GetAsync(_bookingsUrl))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _event = null;
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();

                        var json = await response.Content.ReadAsStringAsync();

                        _event = ParseEvent(json);
                    }
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception($"Could not fetch list of events: {ex.Message}"), "Events Plugin");
            }
        }

        // Accepts either the current API shape (a single event object) or the older shape
        // (an array of events, of which the first is taken). Empty array or JSON null means no event.
        private static Event ParseEvent(string json)
        {
            var token = Newtonsoft.Json.Linq.JToken.Parse(json);

            if (token is Newtonsoft.Json.Linq.JArray array)
            {
                token = array.FirstOrDefault();
            }

            if (token == null || token.Type != Newtonsoft.Json.Linq.JTokenType.Object) return null;

            return token.ToObject<Event>();
        }

        // Fetches the VATSIM data feed. On failure the previous pilot list is kept.
        private static async Task GetVatsimData()
        {
            if (_event == null) return;

            try
            {
                var response = await _httpClient.GetAsync(_dataUrl);

                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<VatsimData>(jsonResponse);

                if (data?.pilots != null) _pilots = data.pilots;
            }
            catch { }
        }

        private static Dictionary<string, VatpacBookingSlot> NewBookings()
        {
            return new Dictionary<string, VatpacBookingSlot>(StringComparer.OrdinalIgnoreCase);
        }

        // Builds the callsign -> slot map:
        //  1. Every booked slot is keyed by its slot callsign.
        //  2. A pilot online with a slot callsign but a CID other than the booking's is not event traffic,
        //     so that callsign is removed.
        //  3. A pilot online whose CID holds a booking, but under a different callsign, is event traffic
        //     under the callsign they are actually using.
        // Pilots not present in the data feed (e.g. sweatbox) keep the plain callsign match.
        private static void UpdateBookings()
        {
            var bookings = NewBookings();

            var slots = _event?.Slots?.Where(x => x.CID.HasValue && !string.IsNullOrWhiteSpace(x.Callsign)).ToList();

            if (slots == null || slots.Count == 0)
            {
                _bookings = bookings;
                return;
            }

            foreach (var slot in slots)
            {
                if (!bookings.ContainsKey(slot.Callsign)) bookings[slot.Callsign] = slot;
            }

            if (_pilots != null)
            {
                foreach (var pilot in _pilots)
                {
                    if (string.IsNullOrWhiteSpace(pilot.callsign)) continue;

                    if (bookings.TryGetValue(pilot.callsign, out var byCallsign) && byCallsign.CID != pilot.cid)
                    {
                        bookings.Remove(pilot.callsign);
                    }
                }

                foreach (var pilot in _pilots)
                {
                    if (string.IsNullOrWhiteSpace(pilot.callsign)) continue;

                    var byCid = slots.FirstOrDefault(x => x.CID == pilot.cid
                        && string.Equals(x.Callsign, pilot.callsign, StringComparison.OrdinalIgnoreCase))
                        ?? slots.FirstOrDefault(x => x.CID == pilot.cid);

                    if (byCid == null) continue;

                    bookings[pilot.callsign] = byCid;
                }
            }

            _bookings = bookings;
        }

        private static VatpacBookingSlot GetBooking(string callsign)
        {
            if (string.IsNullOrWhiteSpace(callsign)) return null;

            return _bookings.TryGetValue(callsign, out var slot) ? slot : null;
        }

        // Nudge every FDR so vatSys re-queries the custom strip and label items.
        private static void RefreshStrips()
        {
            foreach (var fdr in FDP2.GetFDRs)
            {
                fdr.LocalOpData = fdr.LocalOpData;
            }
        }

        public void OnFDRUpdate(FDP2.FDR updated)
        {
            return;
        }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        {
            return;
        }

        public CustomLabelItem GetCustomLabelItem(string itemType, Track track, FDP2.FDR flightDataRecord, RDP.RadarTrack radarTrack)
        {
            if (_event == null) return null;

            if (flightDataRecord == null) return null;

            if (itemType != "LABEL_EVENT") return null;

            var booking = GetBooking(flightDataRecord.Callsign);

            if (booking == null) return null;

            return new CustomLabelItem()
            {
                Type = itemType,
                ForeColourIdentity = Colours.Identities.StaticTools,
                Text = "EV"
            };
        }

        public CustomStripItem GetCustomStripItem(string itemType, Track track, FDP2.FDR flightDataRecord, RDP.RadarTrack radarTrack)
        {
            if (_event == null) return null;

            if (flightDataRecord == null) return null;

            var booking = GetBooking(flightDataRecord.Callsign);

            if (itemType == "STRIP_ATD")
            {
                if (flightDataRecord.ATD != DateTime.MaxValue)
                {
                    return new CustomStripItem()
                    {
                        Text = flightDataRecord.ATD.ToString("HHmm"),
                        Border = BorderFlags.None,
                        ForeColourIdentity = Colours.Identities.Default,
                        BorderColourIdentity = Colours.Identities.State,
                    };
                }

                if (booking == null) return null;

                return new CustomStripItem()
                {
                    Text = COBT(booking.Utc),
                    Border = BorderFlags.None,
                    ForeColourIdentity = Colours.Identities.StaticTools,
                    BorderColourIdentity = Colours.Identities.State,
                };
            }

            if (itemType == "STRIP_EVENT")
            {
                if (booking == null) return null;

                return new CustomStripItem()
                {
                    Text = "EV",
                    Border = BorderFlags.None,
                    ForeColourIdentity = Colours.Identities.StaticTools,
                    BorderColourIdentity = Colours.Identities.State,
                };
            }

            return null;
        }

        public static DateTime COBT_DateTime(DateTime ctot)
        {
            return ctot.AddMinutes(-10);
        }

        public static string COBT(DateTime ctot)
        {
            return COBT_DateTime(ctot).ToString("HHmm");
        }

        public CustomColour SelectASDTrackColour(Track track)
        {
            return null;
        }

        public CustomColour SelectGroundTrackColour(Track track)
        {
            return null;
        }
    }
}
