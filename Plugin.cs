﻿using HtmlAgilityPack;
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

        public static List<Event> Events { get; set; } = new List<Event>();

        private static readonly Version _version = new Version(1, 1);
        private static readonly string _versionUrl = "https://raw.githubusercontent.com/badvectors/EventsPlugin/master/Version.json";
        public static HttpClient _httpClient = new HttpClient();

        private static readonly string _dataUrl = "https://data.vatsim.net/v3/vatsim-data.json";
        private Timer _dataTimer { get; set; } = new Timer();

        private static CustomToolStripMenuItem _eventsMenu;
        private static EventsWindow _eventsWindow;

        private static readonly string _eventsUrl = "https://raw.githubusercontent.com/badvectors/EventsPlugin/master/Events.json";
        public static string SelectedEvent
        {
            get { return _selectedEvent?.Name; }
            set
            {
                var ev = Events.FirstOrDefault(x => x.Name == value);
                if (ev == null)
                {
                    _selectedEvent = null;
                    return;
                }
                else
                {
                    _selectedEvent = ev;
                }
                foreach (var fdr in FDP2.GetFDRs)
                    fdr.LocalOpData = fdr.LocalOpData;
            }
        }
        private static Event _selectedEvent { get; set; }

        public Plugin()
        {
            _eventsMenu = new CustomToolStripMenuItem(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Settings, new ToolStripMenuItem("Events"));
            _eventsMenu.Item.Click += EventsMenu_Click;
            MMI.AddCustomMenuItem(_eventsMenu);

            _ = CheckVersion();

            _ = GetEvents();

            _dataTimer.Elapsed += new ElapsedEventHandler(PositionTimer_Elapsed);
            _dataTimer.Interval = 15000.0;
            _dataTimer.AutoReset = false;
            _dataTimer.Start();
        }

        private async void PositionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await ProcessVatsimData();

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

        private static async Task CheckVersion()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(_versionUrl);

                var version = JsonConvert.DeserializeObject<Version>(response);

                if (version.Major == _version.Major && version.Minor == _version.Minor) return;

                Errors.Add(new Exception("A new version of the plugin is available."), "Events Plugin");
            }
            catch { }
        }

        public static async Task GetEvents()
        {
            try
            {
                SelectedEvent = null;

                Events.Clear();

                var response = await _httpClient.GetStringAsync(_eventsUrl);

                Events = JsonConvert.DeserializeObject<List<Event>>(response);

                foreach (var ev in Events)
                {
                    GetBookings(ev);
                }
            }
            catch 
            {
                Errors.Add(new Exception("Could not fetch list of events."), "Events Plugin");
            }
        }

        private static void GetBookings(Event ev)
        {
            var web = new HtmlWeb();

            foreach (var url in ev.Urls)
            {
                var htmlDocument = web.Load(url);

                if (htmlDocument.Text.Contains("Booking System by Dave Roverts")) Roverts(ev, htmlDocument);
            }
        }

        private static void Roverts(Event ev, HtmlAgilityPack.HtmlDocument htmlDocument)
        {
            var rows = htmlDocument.DocumentNode.SelectNodes("//table[@class='table table-hover table-responsive']//tr");

            foreach (HtmlNode row in rows.Skip(1))
            {
                var cols = row.SelectNodes(".//td");

                if (cols[6].InnerText.Contains("Click here")) continue;

                var from = cols[0].InnerText.Trim();
                var to = cols[1].InnerText.Trim();
                var ctot = cols[2].InnerText.Trim().Replace("z", "");
                var eta = cols[3].InnerText.Trim().Replace("z", "");
                var callsign = cols[4].InnerText.Trim();
                var type = cols[5].InnerText.Trim();
                var cid = cols[6].InnerText.Trim().Replace("Booked [", "").Replace("]", "");

                ev.Bookings.Add(new Booking()
                {
                    CID = cid,
                    Callsign = callsign,
                    From = from,
                    To = to,
                    Type = type,
                    CTOT = ctot,
                    ETA = eta
                });
            }
        }

        private async Task<VatsimData> GetVatsimData()
        {
            try
            {
                var response = await _httpClient.GetAsync(_dataUrl);

                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<VatsimData>(jsonResponse);
            }
            catch { return null; }
        }

        private async Task ProcessVatsimData()
        {
            if (_selectedEvent == null) return;

            var vatsimData = await GetVatsimData();

            if (vatsimData == null) return;

            foreach (var pilot in vatsimData.pilots)
            {
                var bookingByCID = _selectedEvent.Bookings
                    .FirstOrDefault(x => x.CID == pilot.cid.ToString());

                var bookingByCallsign = _selectedEvent.Bookings
                    .FirstOrDefault(x => x.Callsign == pilot.callsign);

                if (bookingByCID != null && bookingByCID.Callsign != pilot.callsign)
                {
                    bookingByCID.Callsign = pilot.callsign;
                }

                if (bookingByCallsign != null && bookingByCallsign.CID != pilot.cid.ToString())
                {
                    bookingByCallsign.Callsign = null;
                }
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
            if (_selectedEvent == null) return null;

            if (itemType != "LABEL_EVENT") return null;

            if (flightDataRecord == null) return null;

            var booking = _selectedEvent.Bookings
                .FirstOrDefault(x => x.Callsign == flightDataRecord.Callsign);

            if (booking == null) return null;

            return new CustomLabelItem()
            {
                Type = itemType,
                ForeColourIdentity = Colours.Identities.StaticTools,
                Text = booking.CTOT
            };
        }

        public CustomColour SelectASDTrackColour(Track track)
        {
            return null;
        }

        public CustomColour SelectGroundTrackColour(Track track)
        {
            return null;
        }

        public CustomStripItem GetCustomStripItem(string itemType, Track track, FDP2.FDR flightDataRecord, RDP.RadarTrack radarTrack)
        {
            if (_selectedEvent == null) return null;

            if (itemType != "STRIP_ATD") return null;

            if (flightDataRecord == null) return null;

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

            var booking = _selectedEvent.Bookings
                .FirstOrDefault(x => x.Callsign == flightDataRecord.Callsign);

            if (booking == null) return null;

            return new CustomStripItem()
            {
                Text = booking.CTOT,
                Border = BorderFlags.None,
                ForeColourIdentity = Colours.Identities.StaticTools,
                BorderColourIdentity = Colours.Identities.State,
            };
        }
    }
}