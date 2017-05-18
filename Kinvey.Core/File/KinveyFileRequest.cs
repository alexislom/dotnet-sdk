// Copyright (c) 2015, Kinvey, Inc. All rights reserved.
//
// This software is licensed to you under the Kinvey terms of service located at
// http://www.kinvey.com/terms-of-use. By downloading, accessing and/or using this
// software, you hereby accept such terms of service  (and any agreement referenced
// therein) and agree that you have read, understand and agree to be bound by such
// terms of service and are of legal age to agree to such terms with Kinvey.
//
// This software contains valuable confidential and proprietary information of
// KINVEY, INC and is subject to applicable licensing agreements.
// Unauthorized reproduction, transmission or distribution of this file and its
// contents is a violation of applicable laws.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using RestSharp;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using ModernHttpClient;

namespace Kinvey
{
	internal class KinveyFileRequest : AbstractKinveyClientRequest<FileMetaData>
	{
		internal KinveyFileRequest (AbstractClient client, string requestMethod, string uriTemplate, FileMetaData httpContent, Dictionary<string, string> uriParameters)
			: base(client, requestMethod, uriTemplate, httpContent, uriParameters)
		{
		}

		#region KinveyFileRequest upload methods

		internal async Task uploadFileAsync(FileMetaData metadata, byte[] input)
		{
			// First, send the empty request to determine the state of the resumable upload

			// 1. To request the upload status, create an empty PUT request to the resumable session URI.
			FileMetaData resumeStatusMetaData = new FileMetaData();
			resumeStatusMetaData.uploadUrl = metadata.uploadUrl;
			resumeStatusMetaData.mimetype = metadata.mimetype;

			// 2. Add a Content-Range header indicating that the current position in the file is unknown.
			// Format: */<total_size_of_file>
			//resumeStatusMetaData.headers = new Dictionary<string, string>();
			//resumeStatusMetaData.headers.Add("Content-Range", $"bytes */{metadata.size}");
			resumeStatusMetaData.size = metadata.size;

			// 3. Send the request.
			HttpResponseMessage resumeStatusResponse = await UploadFileAsync(resumeStatusMetaData, new ByteArrayContent(new byte[0]));
			//var resumeStatusResponse = await UploadInitResumable(resumeStatusMetaData, null);



			// Second, check the response to determine if the file has already been uploaded.
			switch ((int)resumeStatusResponse.StatusCode)
			{
				case 200: // OK
				case 201: // Created
					// File upload was completed, nothing to process
					break;

				case 308: // Resume Incomplete

					// Get range header, and resume upload after the specified byte
					foreach (var header in resumeStatusResponse.Headers)
					{
						//if (header.Key.Equals("Range"))
						//{
						//	// Get last known byte uploaded
						//	break;
						//}
					}
					break;

				case 500: // Internal Server Error
				case 502: // Bad Gateway
				case 503: // Service Unavailable
				case 504: // Gateway Timeout
					break;

				default:
					break;
			}

			HttpResponseMessage response = await UploadFileAsync(metadata, new ByteArrayContent(input));
		}

		internal async Task uploadFileAsync(FileMetaData metadata, Stream input)
		{
			if (input.CanSeek)
			{
				input.Position = 0;
			}

			HttpResponseMessage response = await UploadFileAsync(metadata, new StreamContent(input));
		}

		private async Task<HttpResponseMessage> UploadFileAsync(FileMetaData metadata, HttpContent input)
		{
			string uploadURL = metadata.uploadUrl;

			MediaTypeHeaderValue mt = new MediaTypeHeaderValue(metadata.mimetype);
			input.Headers.ContentType = mt;
			input.Headers.ContentRange = ContentRangeHeaderValue.Parse($"bytes */{metadata.size}");

			var httpClient = new HttpClient(new NativeMessageHandler());
			Uri requestURI = new Uri(uploadURL);

			if (metadata.headers != null)
			{
				foreach (var header in metadata.headers)
				{
					httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
					//KinveyUtils.Logger.Log("HEADER ${header.Key} -> ${header.Value}");
				}
			}

			var response = await httpClient.PutAsync(requestURI, input);
			//response.EnsureSuccessStatusCode();
			return response;
		}

		private async Task<IRestResponse> UploadInitResumable(FileMetaData metadata, HttpContent input)
		{
			RestClient restClient = new RestClient(metadata.uploadUrl);
			RestRequest restRequest = new RestRequest();
			System.Net.Http.HttpContent content = new ByteArrayContent(new byte[0]);
			content.Headers.ContentType = new MediaTypeHeaderValue(metadata.mimetype);
			content.Headers.ContentRange = ContentRangeHeaderValue.Parse($"bytes */{metadata.size}");
			//content.Headers.Add("Content-Range", $"bytes */{metadata.size}");
			//content.Headers.Add("Content-Type", metadata.mimetype);

			restRequest.AddBody(content);
			//restRequest.AddParameter("Content-Range", $"bytes */{metadata.size}", ParameterType.RequestBody);
			//restRequest.AddHeader("Content-Range", $"bytes */{metadata.size}");
			restRequest.Method = Method.PUT;
			RestRequest request = BuildRestRequest();

			restClient.Authenticator = RequestAuth;

			var req = await restClient.ExecuteAsync(restRequest);
			//var response = req.Result;
			//return response;
			return req;
		}

		#endregion

		#region KinveyFileRequest download methods

		internal async Task downloadFileAsync(FileMetaData metadata, Stream stream)
		{
			IRestResponse response = await downloadFileAsync(metadata);
			MemoryStream ms = new MemoryStream(response.RawBytes);
			ms.CopyTo(stream);
		}

		internal async Task downloadFileAsync(FileMetaData metadata, byte[] output)
		{
			IRestResponse response = await downloadFileAsync(metadata);
			output = response.RawBytes;
		}

		private async Task<IRestResponse> downloadFileAsync(FileMetaData metadata)
		{
			string downloadURL = metadata.downloadURL;
			RestClient client = new RestClient(downloadURL);

			RestRequest request = new RestRequest();
			request.Method = Method.GET;

			IRestResponse response = await client.ExecuteAsync(request);
			return response;
		}

		#endregion
	}
}

