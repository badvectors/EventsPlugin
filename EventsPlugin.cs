﻿using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using vatsys;
using vatsys.Plugin;

namespace EventsPlugin
{
    [Export(typeof(IPlugin))]
    public class EventsPlugin : IStripPlugin, ILabelPlugin
    {
        public string Name => "Events";

        private static readonly Version _version = new Version(1, 0);
        private static readonly string _versionUrl = "https://raw.githubusercontent.com/badvectors/EventsPlugin/master/Version.json";
        public static HttpClient _httpClient = new HttpClient();

        private static CustomToolStripMenuItem _eventsMenu;
        private static EventsWindow _eventsWindow;

        private static readonly string _eventsUrl = "https://raw.githubusercontent.com/badvectors/EventsPlugin/master/Events.json";
        private static List<Event> _events = new List<Event>();
        private static List<Booking> _bookings = new List<Booking>();

        public EventsPlugin()
        {
            _eventsMenu = new CustomToolStripMenuItem(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Settings, new ToolStripMenuItem("Events"));
            _eventsMenu.Item.Click += EventsMenu_Click;
            MMI.AddCustomMenuItem(_eventsMenu);

            _ = CheckVersion();

            GetBookings("https://ctl.vatsim.me/cross-the-land-asia-pacific-melbourne-slots/bookings");
            GetBookings("https://ctl.vatsim.me/cross-the-land-asia-pacific-sydney-slots/bookings");
            GetBookings("https://ctl.vatsim.me/cross-the-land-asia-pacific-darwin-slots/bookings");

            _bookings.Add(new Booking()
            {
                Callsign = "JST430",
                CTOT = "0815"
            });
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

        private static void GetBookings(string url)
        {
            var web = new HtmlWeb();

            var htmlDocument = web.Load(url);

            if (htmlDocument.Text.Contains("Booking System by Dave Roverts")) Roverts(htmlDocument);
        }

        private static void Roverts(HtmlAgilityPack.HtmlDocument htmlDocument)
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

                _bookings.Add(new Booking()
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
            if (itemType != "LABEL_EVENT") return null;

            if (flightDataRecord == null) return null;

            var booking = _bookings.FirstOrDefault(x => x.Callsign == flightDataRecord.Callsign);

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
            if (itemType != "STRIP_ATD") return null;

            if (flightDataRecord == null) return null;

            if (flightDataRecord.ATD != DateTime.MaxValue)
            {
                return new CustomStripItem()
                {
                    Text = flightDataRecord.ATD.ToString("HHmm"),
                    Border = BorderFlags.None,
                    ForeColourIdentity = Colours.Identities.State,
                    BorderColourIdentity = Colours.Identities.State,
                };
            }

            var booking = _bookings.FirstOrDefault(x => x.Callsign == flightDataRecord.Callsign);

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
