using System;
using System.Linq;
using vatsys;

namespace EventsPlugin
{
    public partial class EventsWindow : BaseForm
    {
        public EventsWindow()
        {
            InitializeComponent();

            BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);

            buttonRefresh.BackColor = BackColor;
            buttonRefresh.ForeColor = ForeColor;
        }

        private void EventsWindow_Load(object sender, EventArgs e)
        {
            UpdateComboBox();

            DisplayBookings();
        }

        private void ComboBoxDisplay_SelectedIndexChanged(object sender, EventArgs e)
        {
            Plugin.SelectedEvent = comboBoxDisplay.Text;

            DisplayBookings();
        }

        private async void ButtonRefresh_Click(object sender, EventArgs e)
        {
            await Plugin.GetEvents();

            UpdateComboBox();

            DisplayBookings();
        }

        private void DisplayBookings()
        {
            var selectedEvent = Plugin.Events.FirstOrDefault(x => x.Name == Plugin.SelectedEvent);

            if (selectedEvent != null)
            {
                LabelBookings.Text = $"Bookings: {selectedEvent.Bookings.Count}";
            }
            else
            {
                LabelBookings.Text = "";
            }
        }

        private void UpdateComboBox()
        {
            comboBoxDisplay.Items.Clear();

            comboBoxDisplay.Items.Add(string.Empty);

            foreach (var ev in Plugin.Events)
                comboBoxDisplay.Items.Add(ev.Name);

            if (Plugin.SelectedEvent == null || !Plugin.Events.Any(x => x.Name == Plugin.SelectedEvent))
                comboBoxDisplay.SelectedIndex = 0;
            else
                comboBoxDisplay.Text = Plugin.SelectedEvent;
        }
    }
}