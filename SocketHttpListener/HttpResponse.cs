﻿using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using SocketHttpListener.Net;

namespace SocketHttpListener
{
    internal class HttpResponse : HttpBase
    {
        #region Private Fields

        private string _code;
        private string _reason;

        #endregion

        #region Private Constructors

        private HttpResponse(string code, string reason, Version version, NameValueCollection headers)
            : base(version, headers)
        {
            _code = code;
            _reason = reason;
        }

        #endregion

        #region Internal Constructors

        internal HttpResponse(HttpStatusCode code)
            : this(code, code.GetDescription())
        {
        }

        internal HttpResponse(HttpStatusCode code, string reason)
            : this(((int)code).ToString(), reason, HttpVersion.Version11, new NameValueCollection())
        {
            Headers["Server"] = "websocket-sharp/1.0";
        }

        #endregion

        #region Public Properties

        public CookieCollection Cookies
        {
            get
            {
                return Headers.GetCookies(true);
            }
        }

        public bool IsProxyAuthenticationRequired
        {
            get
            {
                return _code == "407";
            }
        }

        public bool IsUnauthorized
        {
            get
            {
                return _code == "401";
            }
        }

        public bool IsWebSocketResponse
        {
            get
            {
                var headers = Headers;
                return ProtocolVersion > HttpVersion.Version10 &&
                       _code == "101" &&
                       headers.Contains("Upgrade", "websocket") &&
                       headers.Contains("Connection", "Upgrade");
            }
        }

        public string Reason
        {
            get
            {
                return _reason;
            }
        }

        public string StatusCode
        {
            get
            {
                return _code;
            }
        }

        #endregion

        #region Internal Methods

        internal static HttpResponse CreateCloseResponse(HttpStatusCode code)
        {
            var res = new HttpResponse(code);
            res.Headers["Connection"] = "close";

            return res;
        }

        internal static HttpResponse CreateUnauthorizedResponse(string challenge)
        {
            var res = new HttpResponse(HttpStatusCode.Unauthorized);
            res.Headers["WWW-Authenticate"] = challenge;

            return res;
        }

        internal static HttpResponse CreateWebSocketResponse()
        {
            var res = new HttpResponse(HttpStatusCode.SwitchingProtocols);

            var headers = res.Headers;
            headers["Upgrade"] = "websocket";
            headers["Connection"] = "Upgrade";

            return res;
        }

        internal static HttpResponse Parse(string[] headerParts)
        {
            var statusLine = headerParts[0].Split(new[] { ' ' }, 3);
            if (statusLine.Length != 3)
                throw new ArgumentException("Invalid status line: " + headerParts[0]);

            var headers = new WebHeaderCollection();
            for (int i = 1; i < headerParts.Length; i++)
                headers.SetInternal(headerParts[i]);

            return new HttpResponse(
              statusLine[1], statusLine[2], new Version(statusLine[0].Substring(5)), headers);
        }

        internal static HttpResponse Read(Stream stream, int millisecondsTimeout)
        {
            return Read<HttpResponse>(stream, Parse, millisecondsTimeout);
        }

        #endregion

        #region Public Methods

        public void SetCookies(CookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0)
                return;

            var headers = Headers;
            foreach (var cookie in cookies.Sorted)
                headers.Add("Set-Cookie", cookie.ToClientString());
        }

        public override string ToString()
        {
            var output = new StringBuilder(64);
            output.AppendFormat("HTTP/{0} {1} {2}{3}", ProtocolVersion, _code, _reason, CrLf);

            var headers = Headers;
            foreach (var key in headers.AllKeys)
                output.AppendFormat("{0}: {1}{2}", key, headers[key], CrLf);

            output.Append(CrLf);

            var entity = EntityBody;
            if (entity.Length > 0)
                output.Append(entity);

            return output.ToString();
        }

        #endregion
    }
}