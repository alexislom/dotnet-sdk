﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Globalization;
using System.Diagnostics;

namespace Kinvey.Tests
{
    public abstract class BaseTestClass
    {
        public static Client.Builder ClientBuilder
        {
            get
            {
                return new Client.Builder(AppKey, AppSecret);
            }
        }

        public static Client.Builder ClientBuilderFake
        {
            get
            {
                return new Client.Builder("_fake_", "fake");
            }
        }

        public static string AppKey
        {
            get
            {
                return EnvironmentVariable.AppKey ?? "_kid_";
            }
        }

        public static string AppSecret
        {
            get
            {
                return EnvironmentVariable.AppSecret ?? "appSecret";
            }
        }


        public static bool MockData
        {
            get
            {
                return string.IsNullOrEmpty(EnvironmentVariable.AppKey) && string.IsNullOrEmpty(EnvironmentVariable.AppSecret);
            }
        }

        protected Client BuildClient(string apiVersion = null)
        {
            Client.Builder builder = ClientBuilder.SetFilePath(TestSetup.db_dir);

            if (MockData)
            {
                builder.setBaseURL("http://localhost:8080");
            }

            if (!string.IsNullOrEmpty(apiVersion))
            {
                builder.SetApiVersion(apiVersion);
            }

            return builder.Build();
        }

        public static class EnvironmentVariable
        {
            public static string AppKey => Environment.GetEnvironmentVariable("KINVEY_APP_KEY");

            public static string AppSecret => Environment.GetEnvironmentVariable("KINVEY_APP_SECRET");
        }

        private static readonly string REQUEST_START_HEADER = "X-Kinvey-Request-Start";
        private static readonly string DATE_FORMAT = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffK";

        private static readonly int MaxApiVersion = 5;

        protected static HttpListener httpListener;
        protected const string badRequestErrorEntityCollection = "BadRequestErrorEntity";
        protected const string internalServerErrorEntityCollection = "InternalServerErrorEntity";
        protected const string forbiddenErrorEntityCollection = "ForbiddenErrorEntity";
        protected const string notFoundErrorEntityCollection = "NotFoundErrorEntity";
        protected const string conflictErrorEntityCollection = "ConflictErrorEntity";
        protected const string toDosCollection = "ToDos";
        protected const string personCollection = "person";
        protected const string flashCardCollection = "FlashCard";

        public void Delete(string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            while (fileInfo.Exists)
            {
                try
                {
                    fileInfo.Delete();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    fileInfo.Refresh();
                }
            }
        }

        private void Logout()
        {
            try
            {
                if (Client._sharedClient != null)
                {
                    using (var client = Client._sharedClient)
                    {
                        var user = client.ActiveUser;
                        if (user != null)
                        {
                            user.Logout();
                        }
                    }
                }
            }
            finally
            {
                Client.SharedClient = null;
            }
        }

        [TestInitialize]
        public virtual void Setup()
        {
            Logout();

            Delete(TestSetup.SQLiteOfflineStoreFilePath);
            Delete(TestSetup.SQLiteCredentialStoreFilePath);

            if (httpListener != null && httpListener.IsListening)
            {
                httpListener.Close();
            }
        }

        [TestCleanup]
        public virtual void Tear()
        {
            Logout();

            Delete(TestSetup.SQLiteOfflineStoreFilePath);
            Delete(TestSetup.SQLiteCredentialStoreFilePath);

            if (httpListener != null && httpListener.IsListening)
            {
                httpListener.Close();
            }
        }

        protected static void MockUserLogin(HttpListenerContext context, IEnumerable<JObject> users, IDictionary<string, JObject> signedUsers)
        {
            Assert.AreEqual(context.Request.HttpMethod, "POST");
            var json = Read<JObject>(context);
            if (json.TryGetValue("_socialIdentity", out JToken socialIdentityJson))
            {
                Assert.AreEqual(socialIdentityJson.Children().Count(), 1);
                var socialIdentityJsonChild = socialIdentityJson.First();
                Assert.AreEqual(socialIdentityJsonChild.Children().Count(), 1);
                var accessToken = default(string);

                switch (socialIdentityJsonChild.Path)
                {
                    case "_socialIdentity.kinveyAuth":
                        accessToken = socialIdentityJsonChild.First().Value<string>("access_token");
                        Assert.IsNotNull(accessToken);
                        break;
                    case "_socialIdentity.facebook":
                        accessToken = socialIdentityJsonChild.First().Value<string>("access_token");
                        Assert.IsNotNull(accessToken);
                        break;
                    case "_socialIdentity.google":
                        accessToken = socialIdentityJsonChild.First().Value<string>("access_token");
                        Assert.IsNotNull(accessToken);
                        break;
                    case "_socialIdentity.twitter":
                        accessToken = socialIdentityJsonChild.First().Value<string>("access_token");
                        Assert.IsNotNull(accessToken);
                        Assert.IsNotNull(socialIdentityJsonChild.First().Value<string>("access_token_secret"));
                        Assert.IsNotNull(socialIdentityJsonChild.First().Value<string>("consumer_key"));
                        Assert.IsNotNull(socialIdentityJsonChild.First().Value<string>("consumer_secret"));
                        break;
                    case "_socialIdentity.linkedin":
                        accessToken = socialIdentityJsonChild.First().Value<string>("access_token");
                        Assert.IsNotNull(accessToken);
                        Assert.IsNotNull(socialIdentityJsonChild.First().Value<string>("access_token_secret"));
                        Assert.IsNotNull(socialIdentityJsonChild.First().Value<string>("consumer_key"));
                        Assert.IsNotNull(socialIdentityJsonChild.First().Value<string>("consumer_secret"));
                        break;
                    case "_socialIdentity.salesforce":
                        accessToken = socialIdentityJsonChild.First().Value<string>("access_token");
                        Assert.IsNotNull(accessToken);
                        Assert.IsNotNull(socialIdentityJsonChild.First().Value<string>("refresh_token"));
                        Assert.IsNotNull(socialIdentityJsonChild.First().Value<string>("id"));
                        Assert.IsNotNull(socialIdentityJsonChild.First().Value<string>("client_id"));
                        break;
                    default:
                        Assert.Fail("Incorrect json data.");
                        break;
                }

                if (signedUsers.ContainsKey(accessToken))
                {
                    var responseJson = signedUsers[accessToken];

                    var authtoken = accessToken.Equals(TestSetup.access_token_for_401_response_fake) ? TestSetup.auth_token_for_401_response_fake : Guid.NewGuid().ToString();

                    responseJson["_kmd"] = new JObject
                    {
                        ["authtoken"] = authtoken
                    };

                    Write(context, responseJson);
                }              
                else
                {
                    Assert.Fail("Incorrect access token.");
                }
            }
            else
            {
                var username = json["username"];
                var password = json["password"];
                var user = users.SingleOrDefault(x =>
                {
                    return username.Equals(x["username"]) && password.Equals(x["password"]);
                });

                if (user != null)
                {
                    if (user["username"].ToString().Equals(TestSetup.user_without_permissions) && user["password"].ToString().Equals(TestSetup.pass_for_user_without_permissions))
                    {
                        user["_kmd"] = new JObject
                        {
                            ["authtoken"] = TestSetup.auth_token_insufficient_credentials_for_401_response_fake
                        };
                    }
                    else if (user["username"].ToString().Equals(TestSetup.user_with_corrupted_auth_token) && user["password"].ToString().Equals(TestSetup.pass_for_user_with_corrupted_auth_token))
                    {
                        user["_kmd"] = new JObject
                        {
                            ["authtoken"] = TestSetup.auth_token_corrupted_for_401_response_fake
                        };
                    }
                    else
                    {
                        user["_kmd"] = new JObject
                        {
                            ["authtoken"] = Guid.NewGuid().ToString()
                        };
                    }

                    var clone = new JObject(user);
                    clone.Remove("password");

                    Write(context, clone);
                }
                else if (username.ToString() == TestSetup.test_json_user && password.ToString() == TestSetup.test_json_pass)
                {
                    Write(context, new[]{1, 2, 3, 4, 5});
                }
                else
                {
                    context.Response.StatusCode = 401;
                    Write(context, new
                    {
                        error = "InvalidCredentials",
                        description = "Invalid credentials. Please retry your request with correct credentials.",
                        debug = "",
                    });
                }
            }
        }

        protected static void Write(HttpListenerContext context, byte[] data)
        {
            using (var outputStream = context.Response.OutputStream)
            {
                outputStream.Write(data, 0, data.Length);
            }
        }

        protected static void Write(HttpListenerContext context, string str)
        {
            using (var outputWriter = new StreamWriter(context.Response.OutputStream))
            {
                outputWriter.Write(str);
            }
        }

        protected static void Write(HttpListenerContext context, object jsonObject)
        {
            context.Response.Headers["Content-Type"] = "application/json; charset=utf-8";
            Write(context, JsonConvert.SerializeObject(jsonObject));
        }

        protected static T Read<T>(HttpListenerContext context)
        {
            using (var streamReader = new StreamReader(context.Request.InputStream))
            {
                var content = streamReader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(content);
            }
        }

        protected static byte[] Read(HttpListenerContext context)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var inputStream = context.Request.InputStream)
                {
                    inputStream.CopyTo(memoryStream);
                }
                return memoryStream.ToArray();
            }
        }

        protected static void MockBadRequest(HttpListenerContext context, string message = "Bad request")
        {
            var response = context.Response;
            response.StatusCode = 400;
            Write(context, message);
        }

        protected static void MockForbiddenRequest(HttpListenerContext context, string message = "Forbidden request")
        {
            var response = context.Response;
            response.StatusCode = 403;
            Write(context, message);
        }
        
        protected static void MockNotFoundRequest(HttpListenerContext context, string message = "Not Found")
        {
            var response = context.Response;
            response.StatusCode = 404;
            Write(context, message);
        }

        protected static void MockConflictRequest(HttpListenerContext context, string message = "Conflict")
        {
            var response = context.Response;
            response.StatusCode = 409;
            Write(context, message);
        }

        protected static void MockInternal(HttpListenerContext context, string message = "The Kinvey server encountered an unexpected error. Please retry your request.")
        {
            var response = context.Response;
            response.StatusCode = 500;
            Write(context, message);
        }

        private static bool Filter(JObject item, string key, JToken jToken)
        {
            switch (jToken.Type)
            {
                case JTokenType.Array:
                    {
                        var results = new List<bool>();
                        foreach (var queryItem in jToken)
                        {
                            var result = Filter(item, null, queryItem);
                            results.Add(result);
                        }
                        if ("$or".Equals(key))
                        {
                            return results.Aggregate((b1, b2) => b1 || b2);
                        }
                        else
                        {
                            return results.Aggregate((b1, b2) => b1 && b2);
                        }
                    }
                case JTokenType.Object:
                    {
                        var results = new List<bool>();
                        foreach (var property in jToken)
                        {
                            var result = Filter(item, key, property);
                            results.Add(result);
                        }
                        return results.Aggregate((b1, b2) => b1 && b2);
                    }
                case JTokenType.Property:
                    {
                        var property = jToken.Value<JProperty>();
                        switch (property.Name)
                        {
                            case "$regex":
                                var regex = new Regex(property.Value.Value<string>());
                                return regex.IsMatch(item[key].Value<string>());
                            case "$gt":
                                return property.Value.Value<IComparable>().CompareTo(item[key].Value<IComparable>()) < 0;
                            case "$gte":
                                return property.Value.Value<IComparable>().CompareTo(item[key].Value<IComparable>()) <= 0;
                            case "$lt":
                                return property.Value.Value<IComparable>().CompareTo(item[key].Value<IComparable>()) > 0;
                            case "$lte":
                                return property.Value.Value<IComparable>().CompareTo(item[key].Value<IComparable>()) >= 0;
                            default:
                                return Filter(item, property.Name, property.Value);
                        }
                    }
                default:
                    return item[key].Value<object>().Equals(jToken);
            }
        }

        private static bool Filter(JObject item, KeyValuePair<string, JToken> queryItem)
        {
            return Filter(item, queryItem.Key, queryItem.Value);
        }

        private static IEnumerable<JObject> Filter(IEnumerable<JObject> objects, KeyValuePair<string, JToken> queryItem)
        {
            return objects.Where((item) => Filter(item, queryItem));
        }

        protected static void MockAppDataPost(HttpListenerContext context, List<JObject> items, Client client)
        {
            JObject jObj = null;
            var binaryData = Read(context);
            var stringJson = Encoding.Default.GetString(binaryData);

            try
            {                
                jObj = JsonConvert.DeserializeObject<JObject>(stringJson);
            }
            catch (Exception)
            {

            }

            if(jObj != null)
            {
                MockAppDataPost(context, jObj, items, client);
            }
            else
            {
                var jArray = JsonConvert.DeserializeObject<JArray>(stringJson);
                MockAppDataPost(context, jArray, items, client);
            }
            
        }

        protected static void MockAppDataPost(HttpListenerContext context, JObject obj, List<JObject> items, Client client)
        {
            if (obj["_geoloc"] != null && !IsValidGeolocation(obj["_geoloc"].ToString()))
            {
                MockBadRequest(context, "Geolocation points must be in the form [longitude, latitude] with long between -180 and 180, lat between -90 and 90");
                return;
            }

            if (obj["name"] != null && obj["name"].ToString().Equals(TestSetup.entity_name_for_400_response_error))
            {
                MockBadRequest(context);
                return;
            }

            if (obj["name"] != null && obj["name"].ToString().Equals(TestSetup.entity_name_for_403_response_error))
            {
                MockForbiddenRequest(context);
                return;
            }

            if (obj["name"] != null && obj["name"].ToString().Equals(TestSetup.entity_name_for_404_response_error))
            {
                MockNotFoundRequest(context);
                return;
            }

            if (obj["name"] != null && obj["name"].ToString().Equals(TestSetup.entity_name_for_409_response_error))
            {
                MockConflictRequest(context);
                return;
            }

            if (obj["name"] != null && obj["name"].ToString().Equals(TestSetup.entity_name_for_500_response_error))
            {
                MockInternal(context);
                return;
            }

            if (obj["_id"] != null && obj["_id"].ToString().Equals(TestSetup.id_for_400_error_response_fake))
            {
                MockBadRequest(context);
                return;
            }

            if (obj["_id"] != null && obj["_id"].ToString().Equals(TestSetup.id_for_403_error_response_fake))
            {
                MockForbiddenRequest(context);
                return;
            }

            if (obj["_id"] != null && obj["_id"].ToString().Equals(TestSetup.id_for_404_error_response_fake))
            {
                MockNotFoundRequest(context);
                return;
            }

            if (obj["_id"] != null && obj["_id"].ToString().Equals(TestSetup.id_for_409_error_response_fake))
            {
                MockConflictRequest(context);
                return;
            }

            if (obj["_id"] != null && obj["_id"].ToString().Equals(TestSetup.id_for_500_error_response_fake))
            {
                MockInternal(context);
                return;
            }

            PopulateEntity(obj, client);
            items.Add(obj);
            Write(context, obj);
        }

        protected static void MockAppDataPost(HttpListenerContext context, JArray jArray, List<JObject> items, Client client)
        {
            var jObjects = jArray.ToObject<List<JObject>>();
            var jObjectsToSave = new List<JObject>();
            var jObjectErrors = new List<JObject>();

            if (jObjects.Count == 0)
            {
                MockBadRequest(context, "Request body cannot be an empty array");
                return;
            }

            if (jObjects.Count > Constants.NUMBER_LIMIT_OF_ENTITIES)
            {
                var response = context.Response;
                response.StatusCode = 413;
                Write(context, "Payload too large");
                return;
            }

            for(var index = 0; index < jObjects.Count; index ++)
            {
                if (jObjects[index]["name"] != null && jObjects[index]["name"].ToString().Equals(TestSetup.entity_name_for_400_response_error))
                {
                    jObjectsToSave.Add(null);

                    var jObjectError = new JObject
                    {
                        ["index"] = index,
                        ["code"] = 1,
                        ["errmsg"] = "Error"
                    };

                    jObjectErrors.Add(jObjectError);

                    continue;

                }
                else if (jObjects[index]["name"] != null && jObjects[index]["name"].ToString().Equals(TestSetup.entity_name_for_403_response_error))
                {
                    jObjectsToSave.Add(null);

                    var jObjectError = new JObject
                    {
                        ["index"] = index,
                        ["code"] = 1,
                        ["errmsg"] = "Error"
                    };

                    jObjectErrors.Add(jObjectError);

                    continue;
                }
                else if (jObjects[index]["name"] != null && jObjects[index]["name"].ToString().Equals(TestSetup.entity_name_for_404_response_error))
                {
                    jObjectsToSave.Add(null);

                    var jObjectError = new JObject
                    {
                        ["index"] = index,
                        ["code"] = 1,
                        ["errmsg"] = "Error"
                    };

                    jObjectErrors.Add(jObjectError);

                    continue;
                }
                else if (jObjects[index]["name"] != null && jObjects[index]["name"].ToString().Equals(TestSetup.entity_name_for_409_response_error))
                {
                    jObjectsToSave.Add(null);

                    var jObjectError = new JObject
                    {
                        ["index"] = index,
                        ["code"] = 1,
                        ["errmsg"] = "Error"
                    };

                    jObjectErrors.Add(jObjectError);

                    continue;
                }
                else if (jObjects[index]["name"] != null && jObjects[index]["name"].ToString().Equals(TestSetup.entity_name_for_500_response_error))
                {
                    jObjectsToSave.Add(null);

                    var jObjectError = new JObject
                    {
                        ["index"] = index,
                        ["code"] = 1,
                        ["errmsg"] = "Error"
                    };

                    jObjectErrors.Add(jObjectError);

                    continue;
                }
                else if (jObjects[index]["_geoloc"] != null && !IsValidGeolocation(jObjects[index]["_geoloc"].ToString()))
                {
                    jObjectsToSave.Add(null);

                    var jObjectError = new JObject
                    {
                        ["index"] = index,
                        ["code"] = 1,
                        ["errmsg"] = "Geolocation points must be in the form [longitude, latitude] with long between -180 and 180, lat between -90 and 90"
                    };

                    jObjectErrors.Add(jObjectError);

                    continue;
                }
                else
                {                    
                    PopulateEntity(jObjects[index], client);
                    items.Add(jObjects[index]);
                    jObjectsToSave.Add(jObjects[index]);
                }
            }

            if(!jObjectsToSave.Any(j=> j != null) && jObjectErrors.Count > 0)
            {
                MockInternal(context);
                return;
            }

            var multiInsertResultJsonObject = new JObject
            {
                ["Entities"] = new JArray(jObjectsToSave),
                ["Errors"] = new JArray(jObjectErrors)
            };

            Write(context, multiInsertResultJsonObject);
        }

        private static bool IsValidGeolocation(string geolocation)
        {
            if (!string.IsNullOrEmpty(geolocation))
            {
                var splitStrings = geolocation.Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                if (splitStrings.Count() > 0)
                {
                    splitStrings = splitStrings[0].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    if (!double.TryParse(splitStrings[0], out double longitude) || !(longitude >= -180 && longitude <= 180) || !double.TryParse(splitStrings[1], out double latutude) || !(latutude >= -90 && latutude <= 90))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void PopulateEntity(JObject obj, Client client)
        {
            if (obj["_id"] == null || obj["_id"].Type == JTokenType.Null)
            {
                obj["_id"] = Guid.NewGuid().ToString();
            }

            if (!(obj["_acl"] is JObject acl))
            {
                acl = new JObject();
                obj["_acl"] = acl;
            }
            acl["creator"] = client.ActiveUser.Id;

            var kmd = new JObject();
            var date = DateTime.UtcNow.ToString(DATE_FORMAT);
            kmd["ect"] = date;
            kmd["lmt"] = date;
            obj["_kmd"] = kmd;
        }

        protected static void MockAppDataGet(HttpListenerContext context, List<JObject> items, Client client)
        {
            var results = FilterByQuery(context, items, client);
            Write(context, results);
        }

        protected static void MockAppDataDelete(HttpListenerContext context, List<JObject> items, Client client)
        {
            var results = FilterByQuery(context, items, client);
            var resultsIds = results.Select(item => item["_id"].Value<string>());
            var count = items.RemoveAll(obj=> resultsIds.Contains(obj["_id"].Value<string>()));

            var jsonObject = new JObject
            {
                ["count"] = count
            };

            Write(context, jsonObject);
        }

        protected static IEnumerable<JObject> FilterByQuery(HttpListenerContext context, List<JObject> items, Client client)
        {
            var queryItems = HttpUtility.ParseQueryString(context.Request.Url.Query);
            var results = items as IEnumerable<JObject>;
            var query = queryItems["query"];
            if (query != null)
            {
                var queryObj = JsonConvert.DeserializeObject<JObject>(query);
                foreach (var queryItem in queryObj)
                {
                    results = Filter(results, queryItem);
                }
            }
            var sort = queryItems["sort"];
            if (sort != null)
            {
                var sortObject = JsonConvert.DeserializeObject<Dictionary<string, int>>(sort);
                foreach (var sortKeyValue in sortObject)
                {
                    if (sortKeyValue.Value > 0)
                    {
                        results = results.OrderBy((x) => x[sortKeyValue.Key]);
                    }
                    else
                    {
                        results = results.OrderByDescending((x) => x[sortKeyValue.Key]);
                    }
                }
            }
            var skip = queryItems["skip"];
            if (skip != null)
            {
                var skipValue = int.Parse(skip);
                results = results.Skip(skipValue);
            }
            var limit = queryItems["limit"];
            if (limit != null)
            {
                var limitValue = int.Parse(limit);
                results = results.Take(limitValue);
            }
            var fields = queryItems["fields"];
            if (fields != null)
            {
                var fieldsArray = fields.Split(",");
                results = results.Select((item) =>
                {
                    var _obj = new JObject();
                    foreach (var field in fieldsArray)
                    {
                        _obj[field] = item[field];
                    }
                    return _obj;
                });
            }
            AddRequestStartHeader(context);

            return results;
        }

            protected static void MockAppDataPut(HttpListenerContext context, List<JObject> items, string id, Client client)
        {
            var obj = Read<JObject>(context);
            var index = items.FindIndex((x) => id.Equals(x["_id"].Value<string>()));

            if(index == -1)
            {
                MockAppDataPost(context, obj, items, client);
                return;
            }

            if (obj["name"] != null && obj["name"].ToString().Equals(TestSetup.entity_name_for_400_response_error))
            {
                MockInternal(context);
                return;
            }

            if (obj["_geoloc"] != null && !IsValidGeolocation(obj["_geoloc"].ToString()))
            {                   
                MockBadRequest(context, "Geolocation points must be in the form [longitude, latitude] with long between -180 and 180, lat between -90 and 90");
                return;                
            }

            var item = items[index];
            obj["_id"] = id;
            var acl = obj["_acl"];
            if (acl == null || acl.Type == JTokenType.Null)
            {
                obj["_acl"] = acl;
            }

            obj["_kmd"] = item["_kmd"];
            var kmd = obj["_kmd"];
            kmd["lmt"] = DateTime.UtcNow.ToString(DATE_FORMAT);

            items[index] = obj;
            Write(context, obj);
        }

        protected static void MockAppData(HttpListenerContext context, List<JObject> items, Client client)
        {
            switch (context.Request.HttpMethod)
            {
                case "GET":
                    MockAppDataGet(context, items, client);
                    break;
                case "POST":
                    MockAppDataPost(context, items, client);
                    break;
                case "DELETE":
                    MockAppDataDelete(context, items, client);
                    break;
                default:
                    Assert.Fail(context.Request.RawUrl);
                    MockNotFoundRequest(context);
                    break;
            }
        }

        protected static void AddRequestStartHeader(HttpListenerContext context)
        {
            context.Response.Headers[REQUEST_START_HEADER] = DateTime.UtcNow.ToString(DATE_FORMAT);
        }

        protected static void MockAppDataGroup(HttpListenerContext context, IEnumerable<JObject> items)
        {
            Assert.AreEqual("POST", context.Request.HttpMethod);
            var json = Read<JObject>(context);
            var key = json["key"].Value<JObject>();
            var initial = json["initial"].Value<JObject>();
            var reduce = json["reduce"].Value<string>();
            var condition = json["condition"]?.Value<JObject>();

            if (condition != null)
            {
                foreach (var filter in condition)
                {
                    items = items.Where((x) => {
                        return x[filter.Key].Equals(filter.Value);
                    });
                }
            }

            var avgRegex = new Regex(@"function\(doc,out\){ out.result = \(\(\(out.result \* out.count\) \+ doc\.([^\s()-]*)\) / \(out.count \+= 1\)\); }");
            var avgMatch = avgRegex.Match(reduce);
            if (avgMatch != null && avgMatch.Groups.Count == 2)
            {
                var field = avgMatch.Groups[1].Value;
                var results = items.Select((x) => x[field].Value<int>());
                var obj = new
                {
                    result = results.Average(),
                    count = results.Count(),
                };
                Write(context, new object[] { obj });
                return;
            }

            var maxRegex = new Regex(@"function\(doc,out\){ out.result = Math\.max\(out.result, doc\.([^\s()-]*)\); }");
            var maxMatch = maxRegex.Match(reduce);
            if (maxMatch != null && maxMatch.Groups.Count == 2)
            {
                var field = maxMatch.Groups[1].Value;
                var results = items.Select((x) => x[field].Value<int>());
                var obj = new
                {
                    result = results.Max(),
                };
                Write(context, new object[] { obj });
                return;
            }

            var minRegex = new Regex(@"function\(doc,out\){ out.result = Math\.min\(out.result, doc\.([^\s()-]*)\); }");
            var minMatch = minRegex.Match(reduce);
            if (minMatch != null && minMatch.Groups.Count == 2)
            {
                var field = minMatch.Groups[1].Value;
                var results = items.Select((x) => x[field].Value<int>());
                var obj = new
                {
                    result = results.Min(),
                };
                Write(context, new object[] { obj });
                return;
            }

            var sumRegex = new Regex(@"function\(doc,out\){ out.result \+= doc\.([^\s()-]*); }");
            var sumMatch = sumRegex.Match(reduce);
            if (sumMatch != null && sumMatch.Groups.Count == 2)
            {
                var field = sumMatch.Groups[1].Value;

                items = items.Select((x) => {
                    var obj = new JObject();
                    foreach (var _key in key)
                    {
                        if (_key.Value.Value<bool>())
                        {
                            obj[_key.Key] = x[_key.Key];
                        }
                    }
                    obj[field] = x[field];
                    return obj;
                });
                var groupBy = items.GroupBy(x => {
                    var obj = new JObject();
                    foreach (var _key in key)
                    {
                        if (_key.Value.Value<bool>())
                        {
                            obj[_key.Key] = x[_key.Key];
                        }
                    }
                    return JsonConvert.SerializeObject(obj);
                });
                items = groupBy.Select(x => {
                    var obj = JsonConvert.DeserializeObject<JObject>(x.Key);
                    obj["result"] = x.Sum(a => a[field].Value<int>());
                    return obj;
                });
                Write(context, items);
                return;
            }

            MockNotFoundRequest(context);
        }

        protected static void MockAppDataDelete(HttpListenerContext context, List<JObject> items, List<JObject> deletedItems, string id)
        {
            if (id.Equals(TestSetup.id_for_400_error_response_fake))
            {
                MockBadRequest(context);
                return;
            }

            if (id.Equals(TestSetup.id_for_403_error_response_fake))
            {
                MockForbiddenRequest(context);
                return;
            }

            if (id.Equals(TestSetup.id_for_404_error_response_fake))
            {
                MockNotFoundRequest(context);
                return;
            }

            if (id.Equals(TestSetup.id_for_409_error_response_fake))
            {
                MockConflictRequest(context);
                return;
            }

            if (id.Equals(TestSetup.id_for_500_error_response_fake))
            {
                MockInternal(context);
                return;
            }

            var todoIndex = items.FindIndex((obj) => id.Equals(obj["_id"].Value<string>()));
            var jsonObject = new JObject();
            if (todoIndex != -1)
            {
                jsonObject["count"] = 1;
                var deletedItem = items[todoIndex];
                items.RemoveAt(todoIndex);
                deletedItem["_kmd"]["lmt"] = DateTime.UtcNow.ToString(DATE_FORMAT);
                deletedItems.Add(deletedItem);
            }
            else
            {
                MockNotFoundRequest(context);
                return;
            }
            Write(context, jsonObject);
        }

        protected static void MockAppDataDeltaSet(HttpListenerContext context, IEnumerable<JObject> items, IEnumerable<JObject> deletedItems)
        {
            var since = context.Request.QueryString["since"];
            var changed = items.Where(item => {
                var lmt = item["_kmd"]["lmt"].Value<string>();
                return string.Compare(lmt, since, StringComparison.InvariantCulture) > 0;
            });
            var deleted = deletedItems.Where(item => {
                var lmt = item["_kmd"]["lmt"].Value<string>();
                return string.Compare(lmt, since, StringComparison.InvariantCulture) > 0;
            });
            AddRequestStartHeader(context);
            Write(context, new { changed, deleted });
        }

        protected static void MockResponses(uint? expectedRequests = null, Client client = null)
        {
            if (httpListener != null && httpListener.IsListening)
            {
                httpListener.Close();
            }
            httpListener = new HttpListener();
            if (client == null)
            {
                client = Client.SharedClient;
            }
            var appKey = (client.RequestInitializer as KinveyClientRequestInitializer).AppKey;
            var appSecret = (client.RequestInitializer as KinveyClientRequestInitializer).AppSecret;
            var appKeySecretAuthorization = $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{appKey}:{appSecret}"))}";
            httpListener.Prefixes.Add(client.BaseUrl);
            if (new Uri(client.MICHostName).Scheme.ToLower().Equals("http"))
            {
                httpListener.Prefixes.Add(client.MICHostName);
            }
            httpListener.Start();
            var thread = new Thread(new ThreadStart(async () =>
            {
                try
                {
                    #region Existing users

                    var users = new Dictionary<string, JObject>();

                    var testUserId = Guid.NewGuid().ToString();
                    users[testUserId] = new JObject
                    {
                        ["_id"] = testUserId,
                        ["username"] = TestSetup.user,
                        ["password"] = TestSetup.pass,
                        ["email"] = $"{Guid.NewGuid().ToString()}@kinvey.com",
                        ["_acl"] = new JObject()
                        {
                            ["creator"] = testUserId,
                        },
                    };

                    var userId1 = Guid.NewGuid().ToString();
                    users[userId1] = new JObject
                    {
                        ["_id"] = userId1,
                        ["username"] = Guid.NewGuid().ToString(),
                        ["password"] = Guid.NewGuid().ToString(),
                        ["email"] = $"{Guid.NewGuid().ToString()}@kinvey.com",
                        ["first_name"] = "George",
                    };

                    var userId2 = Guid.NewGuid().ToString();
                    users[userId2] = new JObject
                    {
                        ["_id"] = userId2,
                        ["username"] = Guid.NewGuid().ToString(),
                        ["password"] = Guid.NewGuid().ToString(),
                        ["email"] = $"{Guid.NewGuid().ToString()}@kinvey.com",
                        ["first_name"] = "George",
                    };

                    var userId3 = Guid.NewGuid().ToString();
                    users[userId3] = new JObject
                    {
                        ["_id"] = userId3,
                        ["username"] = Guid.NewGuid().ToString(),
                        ["password"] = Guid.NewGuid().ToString(),
                        ["email"] = $"{Guid.NewGuid().ToString()}@kinvey.com",
                        ["first_name"] = "George",
                    };

                    var deletedSoftUserId = "5808de04e87d27107142f686";
                    users[deletedSoftUserId] = new JObject
                    {
                        ["_id"] = deletedSoftUserId,
                        ["username"] = Guid.NewGuid().ToString(),
                        ["password"] = Guid.NewGuid().ToString(),
                        ["email"] = $"{Guid.NewGuid().ToString()}@kinvey.com",
                        ["_kmd"] = new JObject()
                        {
                            ["status"] = new JObject()
                            {
                                ["val"] = "disabled",
                                ["lastChange"] = DateTime.UtcNow.ToString(DATE_FORMAT),
                            }
                        }
                    };

                    var userWithoutPermissionsId = Guid.NewGuid().ToString();
                    users[userWithoutPermissionsId] = new JObject
                    {
                        ["_id"] = userWithoutPermissionsId,
                        ["username"] = TestSetup.user_without_permissions,
                        ["password"] = TestSetup.pass_for_user_without_permissions,
                        ["email"] = $"{Guid.NewGuid().ToString()}@kinvey.com",
                        ["_acl"] = new JObject()
                        {
                            ["creator"] = userWithoutPermissionsId,
                        },
                    };

                    var userWithCorruptedAuthTokenId = Guid.NewGuid().ToString();
                    users[userWithCorruptedAuthTokenId] = new JObject
                    {
                        ["_id"] = userWithCorruptedAuthTokenId,
                        ["username"] = TestSetup.user_with_corrupted_auth_token,
                        ["password"] = TestSetup.pass_for_user_with_corrupted_auth_token,
                        ["email"] = $"{Guid.NewGuid().ToString()}@kinvey.com",
                        ["_acl"] = new JObject()
                        {
                            ["creator"] = userWithCorruptedAuthTokenId,
                        },
                    };

                    #endregion Existing users

                    #region Social networks users

                    var signedUsers = new Dictionary<string, JObject>();

                    signedUsers[TestSetup.facebook_access_token_fake] = new JObject
                    {
                        ["_id"] = Guid.NewGuid().ToString(),
                        ["username"] = "facebook_user",
                        ["_socialIdentity"] = new JObject()
                        {
                            ["facebook"] = new JObject()
                            {
                                ["data"] = Guid.NewGuid().ToString(),
                            },
                        },
                    };

                    signedUsers[TestSetup.google_access_token_fake] = new JObject
                    {
                        ["_id"] = Guid.NewGuid().ToString(),
                        ["username"] = "google_user",
                        ["_socialIdentity"] = new JObject()
                        {
                            ["google"] = new JObject()
                            {
                                ["data"] = Guid.NewGuid().ToString(),
                            },
                        },
                    };

                    signedUsers[TestSetup.twitter_access_token_fake] = new JObject
                    {
                        ["_id"] = Guid.NewGuid().ToString(),
                        ["username"] = "twitter_user",
                        ["_socialIdentity"] = new JObject()
                        {
                            ["twitter"] = new JObject()
                            {
                                ["data"] = Guid.NewGuid().ToString(),
                            },
                        },
                    };

                    signedUsers[TestSetup.linkedin_access_token_fake] = new JObject
                    {
                        ["_id"] = Guid.NewGuid().ToString(),
                        ["username"] = "linkedin_user",
                        ["_socialIdentity"] = new JObject()
                        {
                            ["linkedin"] = new JObject()
                            {
                                ["data"] = Guid.NewGuid().ToString(),
                            },
                        },
                    };

                    signedUsers[TestSetup.salesforce_access_token_fake] = new JObject
                    {
                        ["_id"] = Guid.NewGuid().ToString(),
                        ["username"] = "salesforce_user",
                        ["_socialIdentity"] = new JObject()
                        {
                            ["salesforce"] = new JObject()
                            {
                                ["data"] = Guid.NewGuid().ToString(),
                            },
                        },
                    };
                    #endregion Social networks users

                    #region MIC services

                    var micServices = new Dictionary<string, IEnumerable<JObject>>();
                    var micId1 = "_kid_" + Constants.MIC_ID_SEPARATOR + TestSetup.mic_id_fake;                                       
                    var micUser1 = new JObject
                    {
                        ["_id"] = Guid.NewGuid().ToString(),
                        ["username"] = "test",
                        ["password"] = "test"
                    };

                    var micUser2 =  new JObject
                    {
                        ["_id"] = Guid.NewGuid().ToString(),
                        ["username"] = Guid.NewGuid().ToString(),
                        ["password"] = Guid.NewGuid().ToString()
                    };

                    var micUser3 = new JObject
                    {
                        ["_id"] = Guid.NewGuid().ToString(),
                        ["username"] = "test3",
                        ["password"] = "test3"
                    };

                    micServices[micId1] = new List<JObject> { micUser1, micUser2, micUser3 };

                    var micId2 = "_kid_" + Constants.MIC_ID_SEPARATOR + "46a68b99c6284c32b4ab7d54328a06ab";
                    var micUser4 = new JObject
                    {
                        ["_id"] = Guid.NewGuid().ToString(),
                        ["username"] = Guid.NewGuid().ToString(),
                        ["password"] = Guid.NewGuid().ToString()
                    };

                    var micUser5 = new JObject
                    {
                        ["_id"] = Guid.NewGuid().ToString(),
                        ["username"] = Guid.NewGuid().ToString(),
                        ["password"] = Guid.NewGuid().ToString()
                    };

                    micServices[micId2] = new List<JObject> { micUser4, micUser5 };

                    #endregion MIC services

                    var blobs = new Dictionary<string, JObject>();
                    var files = new Dictionary<string, byte[]>();
                    var memory = new
                    {
                        Todo = new List<JObject>(),
                        Person = new List<JObject>(),
                        FlashCard = new List<JObject>(),
                    };
                    var deleted = new
                    {
                        Todo = new List<JObject>(),
                        Person = new List<JObject>(),
                        FlashCard = new List<JObject>(),
                    };
                    var count = 0u;
                    while (
                        (expectedRequests == null && httpListener.IsListening) ||
                        (expectedRequests != null && count < expectedRequests)
                    )
                    {
                        HttpListenerContext context;
                        try
                        {
                            context = await httpListener.GetContextAsync();
                        }
                        catch (HttpListenerException)
                        {
                            continue;
                        }

                        count++;
                        Console.WriteLine($"{count}");

                        const string uploadUrlSection = "/_uploadURL/";

                        var apiVersion = context.Request.Headers[Constants.STR_REQUEST_HEADER_API_VERSION];
                        var version = 0;
                        var isParsed = int.TryParse(apiVersion, out version);

                        if (!context.Request.Url.LocalPath.StartsWith(uploadUrlSection, StringComparison.Ordinal) && (!isParsed || (isParsed && version > MaxApiVersion)))
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                            Write(context, "Not implemented");
                            continue;
                        }

                        var authorization = context.Request.Headers["Authorization"];
                        if (!context.Request.Url.LocalPath.StartsWith(uploadUrlSection, StringComparison.Ordinal) && !context.Request.Url.LocalPath.StartsWith("/_downloadURL/", StringComparison.Ordinal))
                        {
                            Assert.IsNotNull(authorization);
                            Assert.IsFalse(string.IsNullOrEmpty(authorization));
                           
                            if (authorization.Contains(TestSetup.auth_token_insufficient_credentials_for_401_response_fake, StringComparison.Ordinal))
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                var error = new JObject
                                {
                                    ["error"] = "InsufficientCredentials",
                                    ["description"] = "The credentials used to authenticate this request are not authorized to run this   operation. Please retry your request with appropriate credentials.",
                                    ["debug"] = "You do not have access to create entities in this collection",
                                };
                                Write(context, error);
                                continue;
                            }

                            if (authorization.Contains(TestSetup.auth_token_corrupted_for_401_response_fake, StringComparison.Ordinal))
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                var error = new JObject
                                {
                                    ["error"] = "InvalidCredentials",
                                    ["description"] = "Invalid credentials. Please retry your request with correct credentials.",
                                    ["debug"] = "Unable to validate the authorization token included in the request",
                                };
                                Write(context, error);
                                continue;
                            }
                            
                            if (authorization.Contains(TestSetup.auth_token_for_401_response_fake, StringComparison.Ordinal))
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                var error = new JObject
                                {
                                    ["error"] = "InvalidCredentials",
                                    ["description"] = "Invalid credentials. Please retry your request with correct credentials.",
                                    ["debug"] = "Error encountered authenticating against kinveyAuth: {\"error\":\"server_error\",\"error_description\":\"An unknown server error occurred.\",\"debug\":\"Access Token not found\"}",
                                };
                                Write(context, error);
                                continue;
                            }
                        }
                        switch (context.Request.Url.LocalPath)
                        {
                            case "/rpc/_kid_/custom/test_bad":
                                Assert.AreEqual("POST", context.Request.HttpMethod);
                                MockNotFoundRequest(context);
                                break;
                            case "/rpc/_kid_/check-username-exists":
                                MockCheckUsernameExists(context, users.Values);
                                break;
                            case "/rpc/_kid_/user-forgot-username":
                                context.Response.StatusCode = 204;
                                Write(context, "");
                                break;
                            case "/rpc/_kid_/custom/test":
                                {
                                    var reader = new StreamReader(context.Request.InputStream);
                                    var json = JsonConvert.DeserializeObject<Dictionary<string, int>>(reader.ReadToEnd());
                                    var input = json["input"];
                                    Assert.AreEqual(input, 1);
                                    var response = context.Response;
                                    var jsonObject = new List<Dictionary<string, int>>()
                                        {
                                            new Dictionary<string, int>() { { "due_date", 2 } },
                                            new Dictionary<string, int>() { { "due_date", 3 } },
                                        };
                                    var jsonString = JsonConvert.SerializeObject(jsonObject);
                                    var writer = new StreamWriter(response.OutputStream);
                                    writer.Write(jsonString);
                                    writer.Close();
                                    break;
                                }
                            case "/appdata/_kid_":
                                Assert.AreEqual("GET", context.Request.HttpMethod);
                                Assert.AreEqual(appKeySecretAuthorization, authorization);
                                Write(context, new
                                {
                                    kinvey = "hello Kinvey",
                                    version = "1.0.0",
                                });
                                break;
                            case "/appdata/_fake_":
                            case "/user/_fake_":
                            case "/user/_fake_/":
                            case "/user/_fake_/login":
                                context.Response.StatusCode = 404;
                                Write(context, new
                                {
                                    error = "This app backend not found.",
                                });
                                break;
                            case "/user/_kid_/_lookup":
                                MockUserLookup(context, users.Values);
                                break;
                            case "/user/_kid_/login":
                                MockUserLogin(context, users.Values, signedUsers);
                                break;
                            case "/user/_kid_":
                            case "/user/_kid_/":
                                MockUserSignUp(context, users);
                                break;
                            case "/oauth/token":
                                MockOauthToken(context, micServices, signedUsers);
                                break;                               
                            case "/appdata/_kid_/person":
                                MockAppData(context, memory.Person, client);
                                break;
                            case "/appdata/_kid_/person/_group":
                                MockAppDataGroup(context, memory.Person);
                                break;
                            case "/appdata/_kid_/ToDos":
                                MockAppData(context, memory.Todo, client);
                                break;
                            case "/appdata/_kid_/FlashCard":
                                MockAppData(context, memory.FlashCard, client);
                                break;
                            case "/appdata/_kid_/FlashCard/_deltaset":
                                MockAppDataDeltaSet(context, memory.FlashCard, deleted.FlashCard);
                                break;
                            case "/appdata/_kid_/ToDos/_count":
                                {
                                    Assert.AreEqual("GET", context.Request.HttpMethod);
                                    var results = FilterByQuery(context, memory.Todo, client);
                                    var jsonObject = new JObject();
                                    jsonObject["count"] = results.Count();
                                    Write(context, jsonObject);
                                    break;
                                }
                            case "/appdata/_kid_/ToDos/_subscribe":
                                {
                                    Assert.AreEqual("POST", context.Request.HttpMethod);
                                    var jsonObject = new JObject();
                                    Write(context, jsonObject);
                                    break;
                                }
                            case "/appdata/_kid_/BadRequestErrorEntity":
                            case "/appdata/_kid_/BadRequestErrorEntity/_count":
                            case "/appdata/_kid_/BadRequestErrorEntity/_group":
                                MockBadRequest(context);
                                break;
                            case "/appdata/_kid_/ForbiddenErrorEntity":
                            case "/appdata/_kid_/ForbiddenErrorEntity/_count":
                            case "/appdata/_kid_/ForbiddenErrorEntity/_group":
                                MockForbiddenRequest(context);
                                break;
                            case "/appdata/_kid_/NotFoundErrorEntity":
                            case "/appdata/_kid_/NotFoundErrorEntity/_count":
                            case "/appdata/_kid_/NotFoundErrorEntity/_group":
                                MockNotFoundRequest(context);
                                break;
                            case "/appdata/_kid_/ConflictErrorEntity":
                            case "/appdata/_kid_/ConflictErrorEntity/_count":
                            case "/appdata/_kid_/ConflictErrorEntity/_group":
                                MockConflictRequest(context);
                                break;
                            case "/appdata/_kid_/InternalServerErrorEntity":
                            case "/appdata/_kid_/InternalServerErrorEntity/_count":
                            case "/appdata/_kid_/InternalServerErrorEntity/_group":
                                MockInternal(context);
                                break;
                            case "/blob/_kid_/":
                                MockBlob(context, blobs);
                                break;
                            case "/push/_kid_/register-device":
                            case "/push/_kid_/unregister-device":
                                Assert.AreEqual("POST", context.Request.HttpMethod);

                                var pushObj = Read<JObject>(context);
                                var platform = pushObj["platform"].ToString();
                                var deviceId = pushObj["deviceId"].ToString();
                                var service = pushObj["service"].ToString();                               

                                if (string.IsNullOrEmpty(platform) || string.IsNullOrEmpty(deviceId) || (platform.Equals("android") && !service.Equals("firebase")) || (platform.Equals("ios") && !string.IsNullOrEmpty(service)))
                                {
                                    string content = null;
                                    Write(context, content);                                    
                                }
                                else
                                {
                                    Write(context, pushObj);
                                }

                                break;
                            default:
                                {
                                    var regex = new Regex(@"/([^/]*)/([^/]*)/([^/]*)/?([^/]*)");
                                    var match = regex.Match(context.Request.RawUrl);
                                    if (match != null && match.Groups.Count == 5 && match.Groups[1].Value.Equals("appdata") && match.Groups[2].Value.Equals("_kid_"))
                                    {
                                        var collectionName = match.Groups[3].Value;
                                        var id = match.Groups[4].Value;
                                        Assert.IsFalse(id.StartsWith("_"));
                                        List<JObject> items;
                                        List<JObject> deletedItems;
                                        switch (collectionName)
                                        {
                                            case "ToDos":
                                                items = memory.Todo;
                                                deletedItems = deleted.Todo;
                                                break;
                                            case "person":
                                                items = memory.Person;
                                                deletedItems = deleted.Person;
                                                break;
                                            case "FlashCard":
                                                items = memory.FlashCard;
                                                deletedItems = deleted.FlashCard;
                                                break;
                                            default:
                                                items = null;
                                                deletedItems = null;
                                                break;
                                        }
                                        Assert.IsNotNull(items);
                                        Assert.IsNotNull(deletedItems);
                                        switch (context.Request.HttpMethod)
                                        {
                                            case "DELETE":
                                                MockAppDataDelete(context, items, deletedItems, id);
                                                break;
                                            case "GET":
                                                {
                                                    if (id.Equals(TestSetup.id_for_400_error_response_fake))
                                                    {
                                                        MockBadRequest(context);
                                                        break;
                                                    }

                                                    if (id.Equals(TestSetup.id_for_403_error_response_fake))
                                                    {
                                                        MockForbiddenRequest(context);
                                                        break;
                                                    }

                                                    if (id.Equals(TestSetup.id_for_404_error_response_fake))
                                                    {
                                                        MockNotFoundRequest(context);
                                                        break;
                                                    }

                                                    if (id.Equals(TestSetup.id_for_409_error_response_fake))
                                                    {
                                                        MockConflictRequest(context);
                                                        break;
                                                    }

                                                    if (id.Equals(TestSetup.id_for_500_error_response_fake))
                                                    {
                                                        MockInternal(context);
                                                        break;
                                                    }

                                                    var item = items.Find((obj) => id.Equals(obj["_id"].Value<string>()));

                                                    if(item == null)
                                                    {
                                                        MockNotFoundRequest(context);
                                                        break;
                                                    }

                                                    Write(context, item);
                                                }
                                                break;
                                            case "PUT":
                                                MockAppDataPut(context, items, id, client);
                                                break;
                                            default:
                                                Assert.Fail(context.Request.RawUrl);
                                                MockNotFoundRequest(context);
                                                break;
                                        }
                                    }
                                    else if (match != null && match.Groups.Count == 5 && match.Groups[1].Value.Equals("_uploadURL") && match.Groups[2].Value.Equals("_kid_"))
                                    {
                                        var id = match.Groups[3].Value;
                                        switch (context.Request.HttpMethod)
                                        {
                                            case "PUT":
                                                files[id] = Read(context);
                                                blobs[id]["_downloadURL"] = $"http://localhost:8080/_downloadURL/_kid_/{id}";
                                                Write(context, "");
                                                break;
                                            default:
                                                Assert.Fail(context.Request.RawUrl);
                                                MockNotFoundRequest(context);
                                                break;
                                        }
                                    }
                                    else if (match != null && match.Groups.Count == 5 && match.Groups[1].Value.Equals("_downloadURL") && match.Groups[2].Value.Equals("_kid_"))
                                    {
                                        var id = match.Groups[3].Value;
                                        switch (context.Request.HttpMethod)
                                        {
                                            case "GET":
                                                Write(context, files[id]);
                                                break;
                                            default:
                                                Assert.Fail(context.Request.RawUrl);
                                                MockNotFoundRequest(context);
                                                break;
                                        }
                                    }
                                    else if (match != null && match.Groups.Count == 5 && match.Groups[1].Value.Equals("blob") && match.Groups[2].Value.Equals("_kid_"))
                                    {
                                        var id = match.Groups[3].Value;
                                        switch (context.Request.HttpMethod)
                                        {
                                            case "GET":
                                                MockBlobGet(context, blobs, id);
                                                break;
                                            case "PUT":
                                                MockBlobPut(context, blobs, id);
                                                break;
                                            case "DELETE":
                                                MockBlobDelete(context, blobs, id);
                                                break;
                                            default:
                                                Assert.Fail(context.Request.RawUrl);
                                                MockNotFoundRequest(context);
                                                break;
                                        }
                                    }
                                    else if (match != null && match.Groups.Count == 5 && match.Groups[1].Value.Equals("user") && match.Groups[2].Value.Equals("_kid_") && match.Groups[4].Value.Equals("register-realtime"))
                                    {
                                        var id = match.Groups[3].Value;
                                        switch (context.Request.HttpMethod)
                                        {
                                            case "POST":
                                                Write(context, new JObject()
                                                {
                                                    ["subscribeKey"] = Guid.NewGuid().ToString(),
                                                    ["publishKey"] = Guid.NewGuid().ToString(),
                                                    ["userChannelGroup"] = Guid.NewGuid().ToString(),
                                                });
                                                break;
                                            default:
                                                Assert.Fail(context.Request.RawUrl);
                                                MockNotFoundRequest(context);
                                                break;
                                        }
                                    }
                                    else if (match != null && match.Groups.Count == 5 && match.Groups[1].Value.Equals("user") && match.Groups[2].Value.Equals("_kid_") && match.Groups[4].Value.Equals("unregister-realtime"))
                                    {
                                        var id = match.Groups[3].Value;
                                        switch (context.Request.HttpMethod)
                                        {
                                            case "POST":
                                                Write(context, "");
                                                break;
                                            default:
                                                Assert.Fail(context.Request.RawUrl);
                                                MockNotFoundRequest(context);
                                                break;
                                        }
                                    }
                                    else if (match != null && match.Groups.Count == 5 && match.Groups[1].Value.Equals("user") && match.Groups[2].Value.Equals("_kid_"))
                                    {
                                        var id = match.Groups[3].Value;
                                        switch (context.Request.HttpMethod)
                                        {
                                            case "GET":
                                                if (users.ContainsKey(id))
                                                {
                                                    Write(context, users[id]);
                                                }
                                                else
                                                {
                                                    MockNotFoundRequest(context);
                                                }
                                                
                                                break;
                                            case "PUT":
                                                MockUserUpdate(context, users, id);
                                                break;
                                            case "DELETE":
                                                MockUserDelete(context, users, id);
                                                break;                                                
                                            default:
                                                Assert.Fail(context.Request.RawUrl);
                                                MockNotFoundRequest(context);
                                                break;
                                        }
                                    }
                                    else if (match != null && match.Groups.Count == 5 && match.Groups[1].Value.Equals("rpc") && match.Groups[2].Value.Equals("_kid_") && match.Groups[4].Value.Equals("user-password-reset-initiate"))
                                    {
                                        var email = Uri.UnescapeDataString(match.Groups[3].Value);
                                        switch (context.Request.HttpMethod)
                                        {
                                            case "POST":
                                                context.Response.StatusCode = 204;
                                                Write(context, "");
                                                break;
                                            default:
                                                Assert.Fail(context.Request.RawUrl);
                                                MockNotFoundRequest(context);
                                                break;
                                        }
                                    }
                                    else if (match != null && match.Groups.Count == 5 && match.Groups[1].Value.Equals("rpc") && match.Groups[2].Value.Equals("_kid_") && match.Groups[4].Value.Equals("user-email-verification-initiate"))
                                    {                                       
                                        switch (context.Request.HttpMethod)
                                        {
                                            case "POST":
                                                var user = users[match.Groups[3].ToString()];
                                                Write(context, user);
                                                break;
                                            default:
                                                Assert.Fail(context.Request.RawUrl);
                                                MockNotFoundRequest(context);
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        Assert.Fail(context.Request.RawUrl);
                                        MockNotFoundRequest(context);
                                    }
                                    break;
                                }
                        }
                    }
                }
                finally
                {
                    if (expectedRequests == null && httpListener != null && httpListener.IsListening)
                    {
                        httpListener.Stop();
                    }
                }
            }))
            {
                Name = "HttpListener"
            };
            thread.Start();
        }

        private static void MockUserUpdate(HttpListenerContext context, Dictionary<string, JObject> users, string id)
        {
            var user = Read<JObject>(context);
            users[id] = user;
            Write(context, user);
        }

        private static void MockUserDelete(HttpListenerContext context, Dictionary<string, JObject> users, string query)
        {
            var id = query.Split('?')[0];
            var isRemoved = users.Remove(id);
            var kinveyDeleteResponse = new KinveyDeleteResponse
            {
                count = isRemoved ? 1 : 0
            };
            Write(context, kinveyDeleteResponse);
        }

        private static void MockUserLookup(HttpListenerContext context, IEnumerable<JObject> users)
        {
            Assert.AreEqual("POST", context.Request.HttpMethod);
            var lookup = Read<JObject>(context);
            users = users.Where(x =>
            {
                return lookup["first_name"].Equals(x["first_name"]);
            }).Select(x =>
            {
                return new JObject()
                {
                    ["_id"] = x["_id"],
                    ["username"] = x["username"],
                    ["_acl"] = x["_acl"],
                    ["email"] = x["email"],
                };
            });
            Write(context, users);
        }

        private static void MockUserSignUp(HttpListenerContext context, Dictionary<string, JObject> users)
        {
            Assert.AreEqual("POST", context.Request.HttpMethod);
            var user = Read<JObject>(context) ?? new JObject();

            var userId = Guid.NewGuid().ToString();
            user["_id"] = userId;
            user["_kmd"] = new JObject
            {
                ["authtoken"] = Guid.NewGuid().ToString()
            };

            users[userId] = user;

            Write(context, user);
        }

        private static void MockOauthToken(HttpListenerContext context, IDictionary<string, IEnumerable<JObject>> micServices, IDictionary<string, JObject> signedUsers)
        {
            Assert.AreEqual("POST", context.Request.HttpMethod);
            var data = Read(context);
            var micData = Encoding.Default.GetString(data);
            var micUserData = micData.Split('&');

            var splitedMicData = new Dictionary<string, string>();

            foreach (var micUserItem in micUserData)
            {
                var item = micUserItem.Split('=');
                splitedMicData.Add(item[0], item[1]);
            }
                      
            var clientId = splitedMicData["client_id"];
            var noMicServiceIdProvided = clientId == "_kid_";

            if (micServices.ContainsKey(clientId) || noMicServiceIdProvided)
            {
                string accessToken;
                var existingUsers = noMicServiceIdProvided ? micServices[micServices.Keys.FirstOrDefault()] : micServices[clientId];

                if (splitedMicData["grant_type"] == "refresh_token")
                {
                    if (splitedMicData["refresh_token"].Equals(TestSetup.refresh_token_for_401_response_fake))
                    {
                        accessToken = TestSetup.access_token_for_401_response_fake;
                    }
                    else
                    {
                        signedUsers.Remove(TestSetup.access_token_for_401_response_fake);

                        var existingUser = existingUsers.FirstOrDefault(x => x["username"].ToString().Equals("test3") && x["password"].ToString().Equals("test3"));

                        Assert.IsNotNull(existingUser);

                        accessToken = Guid.NewGuid().ToString();
                        signedUsers.Add(accessToken, existingUser);
                    }                   
                }
                else
                {
                    var micUserName = splitedMicData["username"];
                    var micPassword = splitedMicData["password"];

                    var existingUser = existingUsers.FirstOrDefault(x => micUserName.Equals(x["username"].ToString()) && micPassword.Equals(x["password"].ToString()));

                    Assert.IsNotNull(existingUser);

                    accessToken = micUserName.Equals("test3") && micPassword.Equals("test3") ? TestSetup.access_token_for_401_response_fake : Guid.NewGuid().ToString();
                    signedUsers.Add(accessToken, existingUser);
                }

                var responseJson = new JObject
                {
                    ["access_token"] = accessToken,
                    ["refresh_token"] = Guid.NewGuid().ToString()
                };

                Write(context, responseJson);
            }
            else
            {
                Assert.Fail("A user not found.");
            }
            
        }

        private static void MockCheckUsernameExists(HttpListenerContext context, IEnumerable<JObject> users)
        {
            var json = Read<JObject>(context);
            var username = json["username"];

            if (username.ToString() == TestSetup.test_json_user)
            {
                Write(context, new[] { 1, 2, 3, 4, 5 });
            }

            var user = users.SingleOrDefault(x => username.Equals(x["username"]));
            Write(context, new
            {
                usernameExists = user != null && user.Type != JTokenType.Null
            });
        }

        private static void MockBlob(HttpListenerContext context, Dictionary<string, JObject> blobs)
        {
            switch (context.Request.HttpMethod)
            {
                case "POST":
                    MockBlobPost(context, blobs);
                    break;
                default:
                    Assert.Fail(context.Request.HttpMethod);
                    MockNotFoundRequest(context);
                    break;
            }
        }

        private static void MockBlobPost(HttpListenerContext context, Dictionary<string, JObject> blobs)
        {
            MockBlobPut(context, blobs, Guid.NewGuid().ToString());
        }

        private static void MockBlobPut(HttpListenerContext context, Dictionary<string, JObject> blobs, string id)
        {
            var blob = Read<JObject>(context);
            blob["_id"] = id;
            if (blob["_filename"] == null || blob["_filename"].Type == JTokenType.Null)
            {
                blob["_filename"] = Guid.NewGuid().ToString();
            }
            blob["_uploadURL"] = $"http://localhost:8080/_uploadURL/_kid_/{id}";
            blob["_requiredHeaders"] = new JObject();
            blobs[id] = blob;
            Write(context, blob);
        }

        private static void MockBlobDelete(HttpListenerContext context, Dictionary<string, JObject> blobs, string id)
        {
            var removedCount = 0;

            if (blobs.ContainsKey(id))
            {
                if (blobs.Remove(id))
                {
                    removedCount++;
                }
            }

            var deleteResponse = new JObject()
            {
                ["count"] = removedCount,
                ["IDs"] = null
            };

            Write(context, deleteResponse);
        }

        private static void MockBlobGet(HttpListenerContext context, Dictionary<string, JObject> blobs, string id)
        {
            if (blobs.ContainsKey(id))
            {
                Write(context, blobs[id]);
            }
            else
            {
                MockNotFoundRequest(context);
            }                    
        }
    }
}
