using System;
using System.Linq;
using System.Threading.Tasks;
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
        }

        private void EventsWindow_Load(object sender, EventArgs e)
        {
            UpdateComboBox();
        }

        private void ComboBoxDisplay_SelectedIndexChanged(object sender, EventArgs e)
        {
            Plugin.SelectedEvent = comboBoxDisplay.Text;
        }

        private async void ButtonRefresh_Click(object sender, EventArgs e)
        {
            await Plugin.GetEvents();

            UpdateComboBox();
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