namespace TenOver.WinForm.Example
{
    /// <summary>
    /// Carries an updated metric value to a <see cref="MetricTileControl"/>.
    /// Value is a string so each metric (deg, mph, rpm, yds, ft, etc.) can
    /// format itself however it needs to ("--- " placeholder, "5.2", "1,234").
    /// </summary>
    public sealed class MetricValueEventArgs : EventArgs
    {
        public string Value { get; }

        public MetricValueEventArgs(string value)
        {
            Value = value;
        }
    }
}
