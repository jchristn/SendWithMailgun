using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestWrapper;

namespace SendWithMailgun
{
    /// <summary>
    /// Mailgun sender.
    /// </summary>
    public class MailgunSender
    {
        #region Public-Members

        /// <summary>
        /// Domain.
        /// </summary>
        public string Domain
        {
            get
            {
                return _Domain;
            }
        }

        /// <summary>
        /// API key.
        /// </summary>
        public string ApiKey
        {
            get
            {
                return _ApiKey;
            }
        }

        /// <summary>
        /// Base URL.  
        /// </summary>
        public string BaseUrl
        {
            get
            {
                return _BaseUrl;
            }
        }

        /// <summary>
        /// Method to invoke to send log messages.
        /// </summary>
        public Action<string> Logger { get; set; } = null;

        #endregion

        #region Private-Members

        private string _Header = "[MailgunSender] ";
        private string _Domain = null;
        private string _ApiKey = null;
        private string _BaseUrl = null;
        private SerializationHelper _Serializer = new SerializationHelper();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="domain">Domain.</param>
        /// <param name="apiKey">API key.</param>
        /// <param name="baseUrl">Base URL.</param>
        public MailgunSender(
            string domain, 
            string apiKey, 
            string baseUrl = "https://api.mailgun.net/v3/")
        {
            if (String.IsNullOrEmpty(domain)) throw new ArgumentNullException(nameof(domain));
            if (String.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (String.IsNullOrEmpty(baseUrl)) throw new ArgumentNullException(nameof(baseUrl));

            if (!baseUrl.EndsWith("/")) baseUrl = baseUrl + "/";

            _Domain = domain;
            _ApiKey = apiKey;
            _BaseUrl = baseUrl;

            MailgunInstrumentation.EnsureInitialized();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Send an email.
        /// </summary>
        /// <param name="to">To line.  Multiple addresses should be comma-separated.</param>
        /// <param name="from">From address.</param>
        /// <param name="subject">Subject.</param>
        /// <param name="body">Body.</param>
        /// <param name="isHtml">Flag indicating if the body is HTML.</param>
        /// <param name="cc">CC line.  Multiple addresses should be comma-separated.</param>
        /// <param name="bcc">BCC line.  Multiple addresses should be comma-separated.</param>
        /// <returns>ID of sent message.</returns>
        public string Send(
            string to, 
            string from, 
            string subject, 
            string body, 
            bool isHtml = false, 
            string cc = null, 
            string bcc = null)
        {
            return SendAsync(to, from, subject, body, isHtml, cc, bcc).Result;
        }

        /// <summary>
        /// Send an email asynchronously.
        /// </summary>
        /// <param name="to">To line.  Multiple addresses should be comma-separated.</param>
        /// <param name="from">From address.</param>
        /// <param name="subject">Subject.</param>
        /// <param name="body">Body.</param>
        /// <param name="isHtml">Flag indicating if the body is HTML.</param>
        /// <param name="cc">CC line.  Multiple addresses should be comma-separated.</param>
        /// <param name="bcc">BCC line.  Multiple addresses should be comma-separated.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>ID of sent message.</returns>
        public async Task<string> SendAsync(
            string to, 
            string from, 
            string subject, 
            string body, 
            bool isHtml = false, 
            string cc = null, 
            string bcc = null, 
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(to)) throw new ArgumentNullException(nameof(to));
            if (String.IsNullOrEmpty(from)) throw new ArgumentNullException(nameof(from));
            if (String.IsNullOrEmpty(body)) throw new ArgumentNullException(nameof(body));

            string url = _BaseUrl + _Domain + "/messages";

            Logger?.Invoke(_Header + "using URL " + url);

            RestResponse resp = null;

            using (MailgunOperation op = new MailgunOperation(MailgunTelemetry.OperationSend, MailgunTelemetry.SpanSend, url))
            {
                op.SetTag(MailgunTelemetry.AttributeDomain, _Domain);
                RecordSendShape(op, to, cc, bcc, body, isHtml);

                try
                {
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict.Add("domain", _Domain);
                    dict.Add("to", to);
                    dict.Add("from", from);
                    if (!String.IsNullOrEmpty(subject)) dict.Add("subject", subject);
                    if (isHtml) dict.Add("html", body);
                    else dict.Add("text", body);
                    if (!String.IsNullOrEmpty(cc)) dict.Add("cc", cc);
                    if (!String.IsNullOrEmpty(bcc)) dict.Add("bcc", bcc);

                    RestRequest req = new RestRequest(url, HttpMethod.Post);
                    req.Authorization.User = "api";
                    req.Authorization.Password = _ApiKey;

                    using (MailgunStage stage = op.BeginStage(MailgunTelemetry.StageRequest))
                    {
                        try
                        {
                            MailgunTraceContext.Inject(req.Headers);
                            resp = await req.SendAsync(dict, token).ConfigureAwait(false);
                            stage.Complete();
                        }
                        catch (Exception e)
                        {
                            stage.Fail(e);
                            throw;
                        }
                    }

                    if (resp != null)
                    {
                        op.SetStatusCode(resp.StatusCode);

                        Logger?.Invoke(_Header + "response " + resp.StatusCode + ": " + resp.ContentLength + " bytes");
                        if (resp.StatusCode == 200 && resp.ContentLength > 0)
                        {
                            string id = null;
                            string message = null;

                            using (MailgunStage stage = op.BeginStage(MailgunTelemetry.StageDeserialize))
                            {
                                try
                                {
                                    Dictionary<string, object> respDict = _Serializer.DeserializeJson<Dictionary<string, object>>(resp.DataAsString);
                                    if (respDict != null && respDict.ContainsKey("id")) id = respDict["id"]?.ToString();
                                    if (respDict != null && respDict.ContainsKey("message")) message = respDict["message"]?.ToString();
                                    stage.Complete();
                                }
                                catch (Exception e)
                                {
                                    stage.Fail(e);
                                    throw;
                                }
                            }

                            Logger?.Invoke(_Header + "id: " + id + ", message: " + message);

                            if (String.IsNullOrEmpty(id))
                            {
                                op.Complete(MailgunTelemetry.OutcomeMalformedResponse);
                            }
                            else
                            {
                                op.SetTag(MailgunTelemetry.AttributeMessageId, id);
                                op.Complete(MailgunTelemetry.OutcomeSuccess);
                            }

                            return id;
                        }
                        else
                        {
                            op.Complete(resp.StatusCode == 200 ? MailgunTelemetry.OutcomeEmptyResponse : MailgunTelemetry.OutcomeHttpError);
                            return null;
                        }
                    }
                    else
                    {
                        Logger?.Invoke(_Header + "no response received");
                        op.Complete(MailgunTelemetry.OutcomeNoResponse);
                        return null;
                    }
                }
                catch (Exception e)
                {
                    op.Fail(e, token.IsCancellationRequested);

                    e.Data.Add("Url", url);
                    e.Data.Add("To", to);
                    e.Data.Add("From", from);
                    e.Data.Add("Cc", cc);
                    e.Data.Add("Bcc", bcc);
                    e.Data.Add("Subject", subject);
                    e.Data.Add("Body", body);
                    e.Data.Add("IsHtml", isHtml);

                    if (resp != null)
                    {
                        e.Data.Add("StatusCode", resp.StatusCode);

                        if (!String.IsNullOrEmpty(resp.DataAsString))
                            e.Data.Add("Response", resp.DataAsString);
                    }

                    throw;
                }
            }
        }

        #endregion

        #region Private-Methods

        private static void RecordSendShape(MailgunOperation op, string to, string cc, string bcc, string body, bool isHtml)
        {
            try
            {
                string format = isHtml ? MailgunTelemetry.BodyFormatHtml : MailgunTelemetry.BodyFormatText;
                int recipients = MailgunInstrumentation.CountRecipients(to, cc, bcc);

                op.SetTag(MailgunTelemetry.AttributeRecipientCount, recipients);
                op.SetTag(MailgunTelemetry.AttributeBodyFormat, format);

                KeyValuePair<string, object> formatTag = new KeyValuePair<string, object>(MailgunTelemetry.AttributeBodyFormat, format);
                if (MailgunInstrumentation.SendRecipients.Enabled)
                    MailgunInstrumentation.SendRecipients.Record(recipients, formatTag);
                if (MailgunInstrumentation.SendBodySize.Enabled)
                    MailgunInstrumentation.SendBodySize.Record(Encoding.UTF8.GetByteCount(body), formatTag);
            }
            catch (Exception)
            {
            }
        }

        #endregion
    }
}
