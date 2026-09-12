namespace EventsPlugin
{
    partial class EventsWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelEvent = new System.Windows.Forms.Label();
            this.radioDepartures = new System.Windows.Forms.RadioButton();
            this.radioArrivals = new System.Windows.Forms.RadioButton();
            this.listViewSlots = new System.Windows.Forms.ListView();
            this.labelBookings = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // labelEvent
            //
            this.labelEvent.AutoEllipsis = true;
            this.labelEvent.Font = new System.Drawing.Font("Terminus (TTF)", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
            this.labelEvent.Location = new System.Drawing.Point(16, 12);
            this.labelEvent.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelEvent.Name = "labelEvent";
            this.labelEvent.Size = new System.Drawing.Size(255, 20);
            this.labelEvent.TabIndex = 0;
            this.labelEvent.Text = "No current event";
            //
            // radioDepartures
            //
            this.radioDepartures.AutoSize = true;
            this.radioDepartures.Checked = true;
            this.radioDepartures.Font = new System.Drawing.Font("Terminus (TTF)", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
            this.radioDepartures.Location = new System.Drawing.Point(16, 38);
            this.radioDepartures.Name = "radioDepartures";
            this.radioDepartures.Size = new System.Drawing.Size(105, 21);
            this.radioDepartures.TabIndex = 1;
            this.radioDepartures.TabStop = true;
            this.radioDepartures.Text = "Departures";
            this.radioDepartures.UseVisualStyleBackColor = true;
            this.radioDepartures.CheckedChanged += new System.EventHandler(this.Mode_CheckedChanged);
            //
            // radioArrivals
            //
            this.radioArrivals.AutoSize = true;
            this.radioArrivals.Font = new System.Drawing.Font("Terminus (TTF)", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
            this.radioArrivals.Location = new System.Drawing.Point(150, 38);
            this.radioArrivals.Name = "radioArrivals";
            this.radioArrivals.Size = new System.Drawing.Size(89, 21);
            this.radioArrivals.TabIndex = 2;
            this.radioArrivals.Text = "Arrivals";
            this.radioArrivals.UseVisualStyleBackColor = true;
            this.radioArrivals.CheckedChanged += new System.EventHandler(this.Mode_CheckedChanged);
            //
            // listViewSlots
            //
            this.listViewSlots.Font = new System.Drawing.Font("Terminus (TTF)", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.listViewSlots.FullRowSelect = true;
            this.listViewSlots.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listViewSlots.HideSelection = false;
            this.listViewSlots.Location = new System.Drawing.Point(16, 66);
            this.listViewSlots.MultiSelect = false;
            this.listViewSlots.Name = "listViewSlots";
            this.listViewSlots.Size = new System.Drawing.Size(255, 300);
            this.listViewSlots.Sorting = System.Windows.Forms.SortOrder.None;
            this.listViewSlots.TabIndex = 3;
            this.listViewSlots.UseCompatibleStateImageBehavior = false;
            this.listViewSlots.View = System.Windows.Forms.View.Details;
            //
            // labelBookings
            //
            this.labelBookings.AutoSize = true;
            this.labelBookings.Location = new System.Drawing.Point(16, 378);
            this.labelBookings.Name = "labelBookings";
            this.labelBookings.Size = new System.Drawing.Size(96, 17);
            this.labelBookings.TabIndex = 4;
            this.labelBookings.Text = "";
            //
            // EventsWindow
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(284, 406);
            this.Controls.Add(this.labelBookings);
            this.Controls.Add(this.listViewSlots);
            this.Controls.Add(this.radioArrivals);
            this.Controls.Add(this.radioDepartures);
            this.Controls.Add(this.labelEvent);
            this.ForeColor = System.Drawing.SystemColors.InfoText;
            this.HasMinimizeButton = false;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximumSize = new System.Drawing.Size(288, 434);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(288, 434);
            this.Name = "EventsWindow";
            this.Resizeable = false;
            this.Text = "Event";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.EventsWindow_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label labelEvent;
        private System.Windows.Forms.RadioButton radioDepartures;
        private System.Windows.Forms.RadioButton radioArrivals;
        private System.Windows.Forms.ListView listViewSlots;
        private System.Windows.Forms.Label labelBookings;
    }
}
