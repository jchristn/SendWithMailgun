namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// In-process HTTP server bound to 127.0.0.1 that emulates the Mailgun API surface used by SendWithMailgun.
    /// Requests are recorded and answered with a configurable status code and body.
    /// </summary>
    public sealed class MockMailgunServer : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Root URL of the server, e.g. http://127.0.0.1:12345/ (always ends with a slash).
        /// </summary>
        public string RootUrl
        {
            get
            {
                return "http://127.0.0.1:" + _Port + "/";
            }
        }

        /// <summary>
        /// HTTP status code returned to callers.  Default is 200.
        /// </summary>
        public int StatusCode { get; set; } = 200;

        /// <summary>
        /// Response body returned to callers.  Default is an empty string (no content).
        /// </summary>
        public string ResponseBody { get; set; } = "";

        /// <summary>
        /// Response content type.  Default is application/json.
        /// </summary>
        public string ResponseContentType { get; set; } = "application/json";

        /// <summary>
        /// Delay applied before responding, in milliseconds.  Default is 0.
        /// </summary>
        public int ResponseDelayMs { get; set; } = 0;

        /// <summary>
        /// Requests received, in order.  Returns a copy.
        /// </summary>
        public List<RecordedRequest> Requests
        {
            get
            {
                lock (_RequestsLock)
                {
                    return new List<RecordedRequest>(_Requests);
                }
            }
        }

        #endregion

        #region Private-Members

        private readonly int _Port;
        private readonly HttpListener _Listener;
        private readonly CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private readonly object _RequestsLock = new object();
        private readonly List<RecordedRequest> _Requests = new List<RecordedRequest>();
        private readonly Task _AcceptTask;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Start a server on a free loopback port.
        /// </summary>
        public MockMailgunServer()
        {
            _Port = GetFreePort();
            _Listener = new HttpListener();
            _Listener.Prefixes.Add(RootUrl);
            _Listener.Start();
            _AcceptTask = Task.Run(() => AcceptLoop(_TokenSource.Token));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Return the only recorded request.
        /// </summary>
        /// <returns>Recorded request.</returns>
        /// <exception cref="TestAssertionException">Zero or more than one request was received.</exception>
        public RecordedRequest SingleRequest()
        {
            List<RecordedRequest> requests = Requests;
            if (requests.Count != 1)
                throw new TestAssertionException("Expected exactly one request to the mock server but received " + requests.Count);
            return requests[0];
        }

        /// <summary>
        /// Return a loopback port that currently has no listener.
        /// </summary>
        /// <returns>Port number.</returns>
        public static int GetFreePort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            _TokenSource.Cancel();

            try
            {
                _Listener.Stop();
                _Listener.Close();
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                _AcceptTask.Wait(1000);
            }
            catch (AggregateException)
            {
            }

            _TokenSource.Dispose();
        }

        #endregion

        #region Private-Methods

        private async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext ctx;

                try
                {
                    ctx = await _Listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception) when (token.IsCancellationRequested || !_Listener.IsListening)
                {
                    return;
                }

                _ = Task.Run(() => Handle(ctx, token));
            }
        }

        private async Task Handle(HttpListenerContext ctx, CancellationToken token)
        {
            try
            {
                RecordedRequest recorded = new RecordedRequest();
                recorded.Method = ctx.Request.HttpMethod;
                recorded.Path = ctx.Request.Url != null ? ctx.Request.Url.AbsolutePath : "";
                recorded.ContentType = ctx.Request.ContentType;
                recorded.Authorization = ctx.Request.Headers["Authorization"];

                using (StreamReader reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                {
                    recorded.Body = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                if (!String.IsNullOrEmpty(recorded.ContentType)
                    && recorded.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                {
                    recorded.Form = ParseForm(recorded.Body);
                }

                lock (_RequestsLock)
                {
                    _Requests.Add(recorded);
                }

                if (ResponseDelayMs > 0) await Task.Delay(ResponseDelayMs, token).ConfigureAwait(false);

                byte[] data = Encoding.UTF8.GetBytes(ResponseBody ?? "");
                ctx.Response.StatusCode = StatusCode;
                ctx.Response.ContentType = ResponseContentType;
                ctx.Response.ContentLength64 = data.Length;
                if (data.Length > 0) await ctx.Response.OutputStream.WriteAsync(data, 0, data.Length, token).ConfigureAwait(false);
                ctx.Response.Close();
            }
            catch (Exception)
            {
                try
                {
                    ctx.Response.Abort();
                }
                catch (Exception)
                {
                }
            }
        }

        private static Dictionary<string, string> ParseForm(string body)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>(StringComparer.Ordinal);
            if (String.IsNullOrEmpty(body)) return ret;

            foreach (string pair in body.Split('&'))
            {
                if (String.IsNullOrEmpty(pair)) continue;
                int idx = pair.IndexOf('=');
                string key = idx >= 0 ? pair.Substring(0, idx) : pair;
                string val = idx >= 0 ? pair.Substring(idx + 1) : "";
                ret[WebUtility.UrlDecode(key)] = WebUtility.UrlDecode(val);
            }

            return ret;
        }

        #endregion
    }
}
