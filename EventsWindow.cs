using System;
using System.Linq;
using System.Windows.Forms;
using vatsys;

namespace EventsPlugin
{
    public partial class EventsWindow : BaseForm
    {
        private const int TimeColumnWidth = 70;

        public EventsWindow()
        {
            InitializeComponent();

            BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);

            listViewSlots.BackColor = BackColor;
            listViewSlots.ForeColor = ForeColor;
        }

        private void EventsWindow_Load(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void Mode_CheckedChanged(object sender, EventArgs e)
        {
            // Fires for both the radio being unchecked and the one being checked; act once.
            if (sender is RadioButton radio && radio.Checked) UpdateDisplay();
        }

        // Redraws the event name and slot list from the plugin's current event. Safe to call from any thread.
        public void UpdateDisplay()
        {
            if (IsDisposed || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)UpdateDisplay);
                return;
            }

            var ev = Plugin.CurrentEvent;

            labelEvent.Text = ev == null
                ? "No current event"
                : string.IsNullOrWhiteSpace(ev.Description) ? ev.Name : $"{ev.Name} - {ev.Description}";

            var departures = radioDepartures.Checked;

            listViewSlots.BeginUpdate();
            listViewSlots.Items.Clear();
            listViewSlots.Columns.Clear();

            listViewSlots.Columns.Add("Callsign", 95);
            listViewSlots.Columns.Add(departures ? "COBT" : "UTC", TimeColumnWidth);

            var count = 0;

            if (ev != null)
            {
                var type = departures ? VatpacBookingSlotType.Departure : VatpacBookingSlotType.Arrival;

                // Drop slots once they are more than 30 minutes past COBT (departures) or the
                // arrival time. This only affects the list; the strips keep flagging them as EV.
                var cutoff = DateTime.UtcNow.AddMinutes(-30);

                // Order by the time actually displayed: COBT for departures, slot UTC for arrivals.
                var rows = ev.Slots
                    .Where(x => x.Type == type)
                    .Select(x => new { x.Callsign, Time = departures ? Plugin.COBT_DateTime(x.Utc) : x.Utc })
                    .Where(x => x.Time >= cutoff)
                    .OrderBy(x => x.Time)
                    .ThenBy(x => x.Callsign);

                foreach (var row in rows)
                {
                    listViewSlots.Items.Add(new ListViewItem(new[] { row.Callsign, row.Time.ToString("HHmm") }));
                    count++;
                }
            }

            listViewSlots.EndUpdate();

            // Let the callsign column absorb the spare width so the time column sits at the right edge.
            // ClientSize already excludes any vertical scrollbar, so the total never overflows.
            listViewSlots.Columns[0].Width = Math.Max(95, listViewSlots.ClientSize.Width - TimeColumnWidth);

            labelBookings.Text = ev == null ? string.Empty : $"{(departures ? "Departures" : "Arrivals")}: {count}";
        }
    }
}
