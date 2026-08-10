using System.ComponentModel;

namespace TenOver.WinForm.Example
{
    /// <summary>
    /// A single metric tile matching the TrackMan-style readouts along the
    /// bottom of the simulator screen (e.g. "FACE ANG.", value, "deg").
    ///
    /// Title and UnitOfMeasure are regular properties — set them in code via
    /// the constructor, or in the designer's Properties panel after dropping
    /// the control onto a form. Value is not settable directly; instead,
    /// wire <see cref="OnValueUpdated"/> up as the event handler for
    /// whatever data source produces updated readings (e.g. a polling
    /// loop's shot/telemetry event), and the tile will update itself
    /// whenever that event fires.
    /// </summary>
    public partial class MetricTileControl : UserControl
    {
        private const string defaultValue = "- - -";

        private string _title = "Title";
        private string _unitOfMeasure = "Unit";

        /// <summary>The metric's display name (e.g. "Face Ang."). Shown uppercased.</summary>
        [Category("Appearance")]
        [Description("The metric's display name, e.g. \"Face Ang.\". Shown uppercased on the tile.")]
        [DefaultValue("Title")]
        public string Title
        {
            get => _title;
            set
            {
                _title = value ?? throw new ArgumentNullException(nameof(value));
                if (lblTitle != null)
                    lblTitle.Text = _title.ToUpperInvariant();
            }
        }

        /// <summary>The metric's unit of measure (e.g. "deg", "mph", "rpm").</summary>
        [Category("Appearance")]
        [Description("The metric's unit of measure, e.g. \"deg\", \"mph\", \"rpm\".")]
        [DefaultValue("Unit")]
        public string UnitOfMeasure
        {
            get => _unitOfMeasure;
            set
            {
                _unitOfMeasure = value ?? throw new ArgumentNullException(nameof(value));
                if (lblUnit != null)
                    lblUnit.Text = _unitOfMeasure;
            }
        }

        /// <summary>
        /// The current displayed value. Not settable directly, and hidden
        /// from the designer's Properties panel — update it at runtime by
        /// raising an event on your data source that this control is
        /// subscribed to via <see cref="OnValueUpdated"/>.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Value { get; private set; } = defaultValue;

        /// <summary>
        /// Sets Title and UnitOfMeasure up front. Equivalent to setting the
        /// properties individually after construction — this is just a
        /// convenience for the common case of knowing both at creation time.
        /// </summary>
        public MetricTileControl(string title, string unitOfMeasure) : this()
        {
            Title = title;
            UnitOfMeasure = unitOfMeasure;
        }

        /// <summary>
        /// Parameterless constructor required by the WinForms designer to
        /// add this control to the Toolbox and drop it onto a form's
        /// surface. Also usable directly if you'd rather set
        /// <see cref="Title"/>/<see cref="UnitOfMeasure"/> via the
        /// Properties panel or object initializer syntax instead of the
        /// other constructor.
        /// </summary>
        public MetricTileControl()
        {
            InitializeComponent();
            lblTitle.Text = _title.ToUpperInvariant();
            lblUnit.Text = _unitOfMeasure;
            lblValue.Text = defaultValue;
        }

        /// <summary>
        /// Event handler to wire up to an external data source's event
        /// (e.g. <c>client.MetricUpdated += faceAngleTile.OnValueUpdated;</c>).
        /// Updates <see cref="Value"/> and the displayed text whenever the
        /// source raises its event. Safe to call from a background thread —
        /// this hops back onto the UI thread automatically if needed.
        /// </summary>
        public void OnValueUpdated(object? sender, MetricValueEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnValueUpdated(sender, e)));
                return;
            }

            Value = e.Value;
            lblValue.Text = Value;
        }

        /// <summary>
        /// Resets the tile back to its placeholder "- - -" display, e.g.
        /// when the device disconnects or a new session starts.
        /// </summary>
        public void Reset()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(Reset));
                return;
            }

            Value = defaultValue;
            lblValue.Text = defaultValue;
        }
    }
}