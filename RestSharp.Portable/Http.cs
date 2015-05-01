﻿#region License
//   Copyright 2010 John Sheehan
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 
using KinveyUtils;


#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using RestSharp.Extensions;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Threading;



namespace RestSharp
{
	/// <summary>
	/// HttpWebRequest wrapper
	/// </summary>
	public partial class Http : IHttp //, IHttpFactory
    {
        //public IMessageHandlerFactory HandlerFactory = new SimpleMessageHandlerFactory<DefaultMessageHandler>();

        #region Private Members

        private IMessageHandler _handler;
        private IRequestMessage _message;
        private IHttpClient _client;
        private IHttpRequest _request;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
		public Http(IHttpRequest request, bool followRedirects)
        {
            var handler = new DefaultMessageHandler();
			handler.Instance.AllowAutoRedirect = followRedirects;
            var message = new DefaultRequestMessage();
            var client = new HttpClientWrapper(handler);
            
            Configure(request, handler, message, client);
        }

        internal Http(IHttpRequest request, IMessageHandler handler, IRequestMessage message, IHttpClient client)
        {           
            Configure(request, handler, message, client);
        }

        #endregion

        #region Public Methods

        ///<summary>
        /// Creates an IHttp
        ///</summary>
        ///<returns></returns>
        //public IHttp Create()
        //{
        //    return new Http();
        //}

        /// <summary>
        /// Execute an async GET-style request with the specified HTTP Method.  
        /// </summary>
        /// <param name="httpMethod">The HTTP method to execute.</param>
        /// <param name="token">A task cancellation token</param>
        /// <returns></returns>
        public async Task<HttpResponse> AsGetAsync(HttpMethod httpMethod, CancellationToken token)
        {
            return await MakeRequestAsync(httpMethod, token);
        }

        /// <summary>
        /// Execute an async POST-style request with the specified HTTP Method.  
        /// </summary>
        /// <param name="httpMethod">The HTTP method to execute.</param>
        /// <param name="token">A task cancellation token</param>
        /// <returns></returns>
        public async Task<HttpResponse> AsPostAsync(HttpMethod httpMethod, CancellationToken token)
        {
            return await MakeRequestAsync(httpMethod, token);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Configure the class with the necessarily default stuff
        /// </summary>
        /// <param name="request"></param>
        /// <param name="handler"></param>
        /// <param name="message"></param>
        /// <param name="client"></param>
        private void Configure(IHttpRequest request, IMessageHandler handler, IRequestMessage message, IHttpClient client)
        {
            _handler = handler;
            _message = message;
            _client = client;
            _request = request;
        }

        /// <summary>
        /// Make requests to using the specified HTTP verb
        /// </summary>
        /// <param name="method">The HTTP method used to make the request</param>
        /// <returns></returns>
        private async Task<HttpResponse> MakeRequestAsync(HttpMethod method, CancellationToken token)
        {          
            this._handler.Configure(this._request);
            this._message.Configure(method, this._request);
            this._client.Configure(this._request);

            var httpResponse = new HttpResponse();

            //token.ThrowIfCancellationRequested();

			HttpRequestMessage backup = new HttpRequestMessage (this._message.Instance.Method, this._message.Instance.RequestUri);
			backup.Headers.UserAgent.ParseAdd ("wat");



			Logger.Log ("------------------------REQUEST");
			Logger.Log(this._message.Instance.Method + " -> " + this._message.Instance.RequestUri.ToString());
			foreach(var h in this._message.Instance.Headers){
				Logger.Log(h.Key + " -> " + h.Value.FirstOrDefault().ToString());
			}
			Logger.Log ("User-Agent -> (" + this._message.Instance.Headers.UserAgent + ")" );
			Logger.Log ("Accept -> (" + this._message.Instance.Headers.Accept + ")" );
			if (this._message.Instance.Content != null){
				Logger.Log(this._message.Instance.Content.ToString());
			}

			Logger.Log ("------------------------END REQUEST");

            try
            {

				WebRequest request = WebRequest.Create (this._message.Instance.RequestUri.ToString());
				request.Method = this._message.Instance.Method.ToString();

				foreach(var h in this._message.Instance.Headers){
					request.Headers[h.Key] = h.Value.FirstOrDefault();
				}

//				request.


//				if (this._message.Instance.Content != null){
//					Logger.Log(this._message.Instance.Content.ToString());
//				}				// If required by the server, set the credentials.
				//request.Credentials = CredentialCache.DefaultCredentials;
//				request.Headers["UserAgent"]= "appname";
				using (var response = (HttpWebResponse)(await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null)))
				{
					using (var responseStream = response.GetResponseStream())
					{
						using (var sr = new StreamReader(responseStream))
						{

							string received = await sr.ReadToEndAsync();
							Logger.Log("sigh " + received);
							HttpResponseMessage resp = new HttpResponseMessage(response.StatusCode);
							resp.RequestMessage = this._message.Instance;

							resp.Content = new StringContent(received);
							await httpResponse.ConvertFromResponseMessage(resp);

						}
					}
				}



//
//				HttpClient client = new HttpClient();
//				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://msdn.microsoft.com/");
//
//				request.Headers.UserAgent.ParseAdd("New User Agent Value");
//
//				HttpResponseMessage response = await client.SendAsync(request);
//				string resultCode = response.StatusCode.ToString();
//
//

//                var responseMessage = await this._client.Instance.SendAsync(backup);
//                await httpResponse.ConvertFromResponseMessage(responseMessage);
            }
            //catch (InvalidOperationException exc)
            //{
            //    // Happens if an invalid URL is provided

            //    // NOTE: It should not really even be possible to get here
            //    // since internally we the UrlBuilder builds a proper URL

            //    httpResponse.ErrorMessage = exc.Message;
            //    httpResponse.ErrorException = exc;
            //    httpResponse.ResponseStatus = ResponseStatus.Error;
            //}
            catch (HttpRequestException exc)
            {
                // Happens if the DNS lookup fails, or request times out naturally
                // Here we try to return the inner exception which is generally more useful

                if (exc.InnerException != null)
                {
                    httpResponse.ErrorMessage = exc.InnerException.Message;
                    httpResponse.ErrorException = exc.InnerException;
                    httpResponse.ResponseStatus = ResponseStatus.Error;
                }
                else
                {
                    httpResponse.ErrorMessage = exc.Message;
                    httpResponse.ErrorException = exc;
                    httpResponse.ResponseStatus = ResponseStatus.Error;
                }
            }
            catch (TaskCanceledException exc)
            {
                // Happens if the user sets a timeout which expires OR
                // if the task is canceled.  We need to test to see which 
                // caused the exception

                if (exc.CancellationToken.IsCancellationRequested)
                {
                    httpResponse.ErrorMessage = exc.Message;
                    httpResponse.ErrorException = exc;
                    httpResponse.ResponseStatus = ResponseStatus.Cancelled;
                }
                else
                {
                    httpResponse.ErrorMessage = exc.Message;
                    httpResponse.ErrorException = exc;
                    httpResponse.ResponseStatus = ResponseStatus.TimedOut;
                }
            }

            //catch (Exception exc)
            //{
            //    // Catch all.  If this ever gets called we should really 
            //    // add a new case for it above.  In those cases maybe we really should 
            //    // just let this bubble out.
            //    httpResponse.ErrorMessage = exc.Message;
            //    httpResponse.ErrorException = exc;
            //    httpResponse.ResponseStatus = ResponseStatus.Error;
            //}

            return httpResponse;
        }

        #endregion

    }
}