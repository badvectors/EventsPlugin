using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
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

        private static readonly Version _version = new Version(1, 3);
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

        public static string DatasetPath { get; set; }

        public Plugin()
        {
            if (!Profile.Name.Contains("Australia")) return;

            _eventsMenu = new CustomToolStripMenuItem(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Settings, new ToolStripMenuItem("Events"));
            _eventsMenu.Item.Click += EventsMenu_Click;
            MMI.AddCustomMenuItem(_eventsMenu);

            GetSettings();

            UpdateFiles();

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

                if (Events == null) return;

                foreach (var ev in Events)
                {
                    await GetBookings(ev);
                }
            }
            catch (Exception ex)
            {
                Errors.Add(new Exception($"Could not fetch list of events: {ex.Message}"), "Events Plugin");
            }
        }

        private static async Task GetBookings(Event ev)
        {
            var web = new HtmlWeb();

            foreach (var url in ev.Urls)
            {
                if (url.EndsWith(".json"))
                {
                    await Json(ev, url);
                    continue;
                }

                var htmlDocument = web.Load(url);

                if (htmlDocument.Text.Contains("Booking System by Dave Roverts")) Roverts(ev, htmlDocument);
            }
        }

        private static void Roverts(Event ev, HtmlAgilityPack.HtmlDocument htmlDocument)
        {
            var rows = htmlDocument.DocumentNode.SelectNodes("//table[@class='table table-hover table-responsive']//tr");

            if (rows == null) return;

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

        private static async Task Json(Event ev, string url)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(url);

                var bookings = JsonConvert.DeserializeObject<List<Booking>>(response);

                foreach (var booking in bookings)
                {
                    ev.Bookings.Add(new Booking()
                    {
                        CID = booking.CID,
                        Callsign = booking.Callsign,
                        From = booking.From,
                        To = booking.To,
                        Type = booking.Type,
                        CTOT = booking.CTOT,
                        ETA = booking.ETA
                    });
                }
            }
            catch { }
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

            if (flightDataRecord == null) return null;

            if (itemType != "LABEL_EVENT") return null;

            var booking = _selectedEvent.Bookings
                .FirstOrDefault(x => x.Callsign == flightDataRecord.Callsign);

            if (booking == null) return null;

            if (flightDataRecord.ATD != DateTime.MaxValue)
            {
                return new CustomLabelItem()
                {
                    Type = itemType,
                    ForeColourIdentity = Colours.Identities.StaticTools,
                    Text = "EV"
                };
            }

            return new CustomLabelItem()
            {
                Type = itemType,
                ForeColourIdentity = Colours.Identities.StaticTools,
                Text = booking.COTB()
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

            if (flightDataRecord == null) return null;

            var booking = _selectedEvent.Bookings
                .FirstOrDefault(x => x.Callsign == flightDataRecord.Callsign);

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
                    Text = booking.COTB(),
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

        private void GetSettings()
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);

            if (!configuration.HasFile) return;

            if (!File.Exists(configuration.FilePath)) return;

            var config = File.ReadAllText(configuration.FilePath);

            XmlDocument doc = new XmlDocument();

            doc.LoadXml(config);

            XmlElement root = doc.DocumentElement;

            var userSettings = root.SelectSingleNode("userSettings");

            var settings = userSettings.SelectSingleNode("vatsys.Properties.Settings");

            foreach (XmlNode node in settings.ChildNodes)
            {
                if (node.Attributes.GetNamedItem("name").Value == "DatasetPath")
                {
                    DatasetPath = node.InnerText;
                    break;
                }
            }
        }

        private void UpdateFiles()
        {
            var showWarning = false;

            var stripsFile = DatasetPath + "\\Strips.xml";

            var stripsSource = AssemblyDirectory + "\\Strips.xml";

            if (!File.Exists(stripsFile))
            {
                File.Copy(stripsSource, stripsFile, true);
                showWarning = true;
            }
            else
            {
                var existingFile = File.ReadAllText(stripsFile);
                
                var newFile = File.ReadAllText(stripsSource);

                if (existingFile != newFile)
                {
                    File.Copy(stripsSource, stripsFile, true);
                    showWarning = true;
                }
            }

            var labelsFile = DatasetPath + "\\Labels.xml";

            var labelsSource = AssemblyDirectory + "\\Labels.xml";

            if (!File.Exists(labelsFile))
            {
                File.Copy(labelsSource, labelsFile, true);
                showWarning = true;
            }
            else
            {
                var existingFile = File.ReadAllText(labelsFile);

                var newFile = File.ReadAllText(labelsSource);

                if (existingFile != newFile)
                {
                    File.Copy(labelsSource, labelsFile, true);
                    showWarning = true;
                }
            }

            if (showWarning)
                Errors.Add(new Exception("Updates installed. Restart vatSys for changes to take effect."), "Events Plugin");
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
