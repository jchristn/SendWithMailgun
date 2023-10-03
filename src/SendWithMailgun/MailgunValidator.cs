using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RestWrapper;

namespace SendWithMailgun
{
    /// <summary>
    /// Mailgun validator.
    /// </summary>
    public class MailgunValidator
    {
        #region Public-Members

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

        private string _Header = "[MailgunValidator] ";
        private string _ApiKey = null;
        private string _BaseUrl = null;
        private SerializationHelper _Serializer = new SerializationHelper();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="apiKey">API key.</param>
        /// <param name="baseUrl">Base URL.</param>
        public MailgunValidator(
            string apiKey, 
            string baseUrl = "https://api.mailgun.net/v4/")
        {
            if (String.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (String.IsNullOrEmpty(baseUrl)) throw new ArgumentNullException(nameof(baseUrl));

            if (!baseUrl.EndsWith("/")) baseUrl = baseUrl + "/";

            _ApiKey = apiKey;
            _BaseUrl = baseUrl;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Validate an email address.
        /// </summary>
        /// <param name="address">Address to validate.</param>
        /// <returns>Mailgun validation result.</returns>
        public MailgunValidationResult Validate(string address)
        {
            return ValidateAsync(address).Result;
        }

        /// <summary>
        /// Validate an email address.
        /// </summary>
        /// <param name="address">Address to validate.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Mailgun validation result.</returns>
        public async Task<MailgunValidationResult> ValidateAsync(string address, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(address)) throw new ArgumentNullException(nameof(address));

            string url = _BaseUrl + "address/validate";

            Logger?.Invoke(_Header + "using URL " + url);

            RestResponse resp = null;

            try
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                dict.Add("address", address);
                RestRequest req = new RestRequest(url, HttpMethod.Post);
                req.Authorization.User = "api";
                req.Authorization.Password = _ApiKey;

                resp = await req.SendAsync(dict, token).ConfigureAwait(false);
                if (resp != null)
                {
                    Logger?.Invoke(_Header + "response " + resp.StatusCode + ": " + resp.ContentLength + " bytes");
                    if (resp.StatusCode == 200 && resp.ContentLength > 0)
                    {
                        MailgunValidationResult result = _Serializer.DeserializeJson<MailgunValidationResult>(resp.DataAsString);
                        Logger?.Invoke(_Header + "success response received for " + address + ": " + result.Result.ToString() + " risk " + result.Risk.ToString());
                        return result;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    Logger?.Invoke(_Header + "no response received");
                    return null;
                }
            }
            catch (Exception e)
            {
                e.Data.Add("Url", url);
                e.Data.Add("Address", address);

                if (resp != null)
                {
                    e.Data.Add("StatusCode", resp.StatusCode);

                    if (!String.IsNullOrEmpty(resp.DataAsString))
                        e.Data.Add("Response", resp.DataAsString);
                }

                throw;
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
