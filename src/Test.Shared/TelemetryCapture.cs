namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using SendWithMailgun;

    /// <summary>
    /// In-memory listener for the SendWithMailgun meter and activity source.
    /// On construction it starts a root test span; <see cref="Spans"/> returns only library spans in that trace,
    /// so concurrent tests do not interfere.  Use <see cref="ForPort"/> to isolate metrics by mock server port.
    /// Thread-safe.
    /// </summary>
    public sealed class TelemetryCapture : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Name of the activity source used for the root test span.
        /// </summary>
        public const string TestSourceName = "Test.Shared.Telemetry";

        /// <summary>
        /// Root test span.  Library spans started while it is current are its descendants.
        /// </summary>
        public Activity? Root
        {
            get
            {
                return _Root;
            }
        }

        /// <summary>
        /// Completed SendWithMailgun spans in the root span's trace.
        /// </summary>
        public List<Activity> Spans
        {
            get
            {
                lock (_Lock)
                {
                    if (_Root == null) return new List<Activity>();
                    return _Activities
                        .Where(a => a.Source.Name == MailgunTelemetry.ActivitySourceName && a.TraceId == _Root.TraceId)
                        .ToList();
                }
            }
        }

        /// <summary>
        /// Every SendWithMailgun measurement captured, from any caller.
        /// </summary>
        public List<RecordedMeasurement> Measurements
        {
            get
            {
                lock (_Lock)
                {
                    return new List<RecordedMeasurement>(_Measurements);
                }
            }
        }

        /// <summary>
        /// Names of SendWithMailgun instruments published to the listener.
        /// </summary>
        public List<string> PublishedInstruments
        {
            get
            {
                lock (_Lock)
                {
                    return new List<string>(_Published);
                }
            }
        }

        #endregion

        #region Private-Members

        private static readonly ActivitySource _TestSource = new ActivitySource(TestSourceName);

        private readonly object _Lock = new object();
        private readonly List<Activity> _Activities = new List<Activity>();
        private readonly List<RecordedMeasurement> _Measurements = new List<RecordedMeasurement>();
        private readonly List<string> _Published = new List<string>();
        private readonly ActivityListener _ActivityListener;
        private readonly MeterListener _MeterListener;
        private readonly Activity? _Root;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Start listening and open a root test span.
        /// </summary>
        public TelemetryCapture()
        {
            _ActivityListener = new ActivityListener();
            _ActivityListener.ShouldListenTo = source => source.Name == MailgunTelemetry.ActivitySourceName || source.Name == TestSourceName;
            _ActivityListener.Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded;
            _ActivityListener.ActivityStopped = activity =>
            {
                lock (_Lock)
                {
                    _Activities.Add(activity);
                }
            };
            ActivitySource.AddActivityListener(_ActivityListener);

            _MeterListener = new MeterListener();
            _MeterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name != MailgunTelemetry.MeterName) return;
                lock (_Lock)
                {
                    _Published.Add(instrument.Name);
                }
                listener.EnableMeasurementEvents(instrument);
            };
            _MeterListener.SetMeasurementEventCallback<long>((instrument, value, tags, state) => Add(instrument, value, tags));
            _MeterListener.SetMeasurementEventCallback<double>((instrument, value, tags, state) => Add(instrument, value, tags));
            _MeterListener.Start();

            _Root = _TestSource.StartActivity("test-root", ActivityKind.Internal);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Measurements carrying the given server.port tag.
        /// </summary>
        /// <param name="port">Port.</param>
        /// <returns>Measurements.</returns>
        public List<RecordedMeasurement> ForPort(int port)
        {
            string p = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Measurements.Where(m => m.Tag(MailgunTelemetry.AttributeServerPort) == p).ToList();
        }

        /// <summary>
        /// Return the single span with the given name.
        /// </summary>
        /// <param name="name">Span name.</param>
        /// <returns>Span.</returns>
        /// <exception cref="TestAssertionException">Zero or more than one span matched.</exception>
        public Activity SingleSpan(string name)
        {
            List<Activity> spans = Spans.Where(a => a.DisplayName == name).ToList();
            if (spans.Count != 1)
                throw new TestAssertionException("Expected exactly one '" + name + "' span but found " + spans.Count
                    + " (all: " + String.Join(", ", Spans.Select(a => a.DisplayName)) + ")");
            return spans[0];
        }

        /// <summary>
        /// Poll observable instruments (for example the build-info gauge).
        /// </summary>
        public void RecordObservables()
        {
            _MeterListener.RecordObservableInstruments();
        }

        /// <summary>
        /// Stop the root span and the listeners.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            _Root?.Dispose();
            _MeterListener.Dispose();
            _ActivityListener.Dispose();
        }

        #endregion

        #region Private-Methods

        private void Add(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            RecordedMeasurement m = new RecordedMeasurement();
            m.Instrument = instrument.Name;
            m.Unit = instrument.Unit;
            m.Value = value;
            foreach (KeyValuePair<string, object?> kvp in tags) m.Tags[kvp.Key] = kvp.Value;

            lock (_Lock)
            {
                _Measurements.Add(m);
            }
        }

        #endregion
    }
}
