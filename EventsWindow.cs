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
    }
}