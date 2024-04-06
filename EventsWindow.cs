using System;
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
            comboBoxDisplay.Items.Clear();

            foreach (var ev in EventsPlugin.Events)
            {
                comboBoxDisplay.Items.Add(ev.Name);
            }
        }
    }
}