﻿using Azure;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection.PortableExecutable;
using CodedThought.Core.Data.Interfaces;
using System.Text.Json.Serialization;
using CodedThought.Core.Data.ApiServer;

namespace CodedThought.Core.Data
{

    public class GenericApiDataStore : GenericDataStore
    {

        #region Declarations

        /// <summary>Gets the Api response message when using the CodedThought.Core.Data.Api data provider.</summary>
        public string? ApiResponseMessage { get; private set; }
        private const string ORM_API_KEY_EXT = ".api";
        #endregion Declarations

        #region Constructors

        /// <summary>Initializes a new instance of the <see cref="GenericDataStore" /> class.</summary>
        /// <param name="databaseSchemaName">Name of the database schema.</param>
        public GenericApiDataStore(IServiceProvider serviceProvider, IMemoryCache cache, ConnectionSetting connectionSetting)
            : base(serviceProvider, cache, connectionSetting)
        {
            UseBasicAuth = false;
            PopulateCurrentConnection(connectionSetting);
            CommandTimeout = connectionSetting.Timeout;
        }

        #endregion Constructors

        #region Properties

        public new DatabaseConnection? CurrentDatabaseConnection { get; set; }

        public ApiDataControllerAttribute ApiDataControllerAttribute { get; set; }
        /// <summary>Gets or sets the database object instance.</summary>
        /// <value>The database object instance.</value>
        private new IDatabaseObject? DatabaseObjectInstance { get; set; }

        public new IDatabaseObject? DeriveDatabaseObject => DatabaseObjectInstance;

        /// <summary>Gets or sets the database connection to use.</summary>
        /// <value>The database to use.</value>
        public new DatabaseConnection DatabaseToUse
        {
            get
            {
                return _specifiedDatabaseConnection;
            }
            set
            {
                _specifiedDatabaseConnection = value;
                DefaultSchemaName = value.SchemaName;
                CommandTimeout = value.CommandTimeout;
            }
        }

        protected static string? SourceUrl { get; set; }
        protected static string? Controller { get; set; }
        protected static string? Username { get; set; }
        protected static string? Password { get; set; }
        protected static Boolean UseBasicAuth { get; set; }
        public static string? RawResponse { get; private set; } = string.Empty;
        public int HttpTimeout { get { return CommandTimeout; } set { CommandTimeout = value; } }
        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Parses an assembly for ORM mappings.
        /// </summary>
        /// <param name="assembly">The assembly to parse.</param>
        public override void LoadAssemblyAndORM(Assembly callingAssembly)
        {
            if (UseHttpCache)
            {

                listLoadedAssemblies = _cache.GetFromHttpCache<List<string>>(ORM_ASSEMBLIES_KEY);
                if (listLoadedAssemblies == null)
                {
                    // Load all data aware assemblies.
                    listLoadedAssemblies = [];
                    List<Assembly> dataAwareAssemblies = GetDataAwareAssemblies();
                    dataAwareAssemblies.ForEach(a =>
                    {
                        listLoadedAssemblies.Add(a.GetName().Name);
                        GenerateMapCollection(ORM, a);
                        // Cache the objects for next time.
                        _cache.AddToHttpCache(ORM_KEY, ORM);
                        _cache.AddToHttpCache(ORM_ASSEMBLIES_KEY, listLoadedAssemblies);
                    });
                }
                listLoadedAssemblies ??= [];
                if (!listLoadedAssemblies.Contains(callingAssembly.GetName().Name))
                {
                    listLoadedAssemblies.Add(callingAssembly.GetName().Name);
                    GenerateMapCollection(ORM, callingAssembly);

                    //re-cache the objects after updates
                    _cache.AddToHttpCache(ORM_KEY, ORM);
                    _cache.AddToHttpCache(ORM_ASSEMBLIES_KEY, listLoadedAssemblies);
                }
            }
            else
            {
                listLoadedAssemblies = _runtimeCache.GetFromLocalCache<List<string>>(ORM_ASSEMBLIES_KEY);
                if (listLoadedAssemblies == null)
                {
                    // Load all data aware assemblies.
                    listLoadedAssemblies = [];
                    List<Assembly> dataAwareAssemblies = GetDataAwareAssemblies();
                    dataAwareAssemblies.ForEach(a =>
                    {
                        listLoadedAssemblies.Add(a.GetName().Name);
                        GenerateMapCollection(ORM, a);
                        // Cache the objects for next time.
                        _runtimeCache.AddToLocalCache(ORM_KEY, ORM);
                        _runtimeCache.AddToLocalCache(ORM_ASSEMBLIES_KEY, listLoadedAssemblies);
                    });
                }
                if (!listLoadedAssemblies.Contains(callingAssembly.GetName().Name))
                {
                    listLoadedAssemblies.Add(callingAssembly.GetName().Name);
                    GenerateMapCollection(ORM, callingAssembly);

                    //re-cache the objects after updates
                    _runtimeCache.AddToLocalCache(ORM_KEY, ORM);
                    _runtimeCache.AddToLocalCache(ORM_ASSEMBLIES_KEY, listLoadedAssemblies);
                }
            }
        }


        /// <summary>Gets the name of the key property.</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public new string? GetKeyPropertyName<T>()
        {
            foreach (ApiDataParameterAttribute attr in ((ApiDataControllerAttribute) ORM[$"{typeof(T).FullName}{ORM_API_KEY_EXT}"][typeof(T)]).Properties)
            {
                if (attr.Options.HasFlag(ApiDataParameterOptions.DefaultParameter))
                {
                    return attr.PropertyName;
                }
            }
            return null;
        }

        /// <summary>Gets any <see cref="ApiDataParameterAttribute" /> from the passed property if one is found.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public ApiDataParameterAttribute GetApiDataParameterFromProperty<T>(string propertyName)
        {
            PropertyInfo property = null;
            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                if (prop.Name == propertyName)
                {
                    property = prop;
                }
            }
            if (property != null)
            {
                ApiDataParameterAttribute apiParameter = (ApiDataParameterAttribute) property.GetCustomAttribute(typeof(ApiDataParameterAttribute), true);
                return apiParameter != null ? apiParameter : null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>Gets the parameter name to be used with the Api.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public String GetApiParameterNameFromProperty<T>(string propertyName)
        {
            ApiDataParameterAttribute attrib = GetApiDataParameterFromProperty<T>(propertyName);
            return attrib != null ? attrib.ParameterName : null;
        }

        /// <summary>Encodes the basic authentication to pass in HttpClient web calls.</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        internal string EncodeBasicAuthCredentials(string username, string password) => Convert.ToBase64String(Encoding.UTF8.GetBytes($"Basic {username}:{password}"));
        /// <summary>
        /// Retrieves an object from the Api.
        /// </summary>
        /// <typeparam name="dynamic"></typeparam>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<TResponse> GetValue<TResponse>(string controller, string action, ParameterCollection parameters) where TResponse : notnull, new()
        {
            try
            {
                ApiDataReader? reader;
                reader = await base.DatabaseObjectInstance.Get(controller, action, parameters);
                ApiResponseMessage = RawResponse = reader.Raw;
                if (reader.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    dynamic responseObj = DeserializeApiResponse<TResponse>(reader);
                    return responseObj;
                }
                else
                {
                    throw new CodedThoughtApplicationException($"There was an error calling the web service.  The reported error was {(int) reader.StatusCode} {reader.ErrorMessage}.");
                }
            }
            catch
            {

                throw;
            }
        }
        /// <summary>Retrieves an object from the database of type T for the given primary key.</summary>
        /// <typeparam name="T">The type of object to retrieve.</typeparam>
        /// <param name="objectID">The primary key of the object.</param>
        /// <returns>Returns an object of the type T.</returns>
        public async Task<T> Get<T, B>(string action, int objectID) where T : class, new() where B : class, new()
        {
            ParameterCollection parameters = new();
            SetParameterCollectionDbObject(parameters);
            // Find the primary key column of the object as an ApiDataParameterAttribute.
            ApiDataParameterAttribute columnAttribute = GetApiDataParameterFromProperty<B>(propertyName: GetKeyPropertyName<T>());
            parameters.AddApiParameter(columnAttribute.ParameterName, objectID.ToString());
            return await Get<T, B>(action, parameters);
        }

        /// <summary>Retrieves an object based on the supplied Key-Value pair collection.</summary>
        /// <typeparam name="T">The type of object to retrieve.</typeparam>
        /// <param name="parameters">A collection of Key-Value pairs.</param>
        /// <returns>Returns an object of the type T.</returns>
        public async Task<T> Get<T, B>(string action, ParameterCollection parameters) where T : class, new() where B : class, new()
        {
            ApiDataReader? reader;
            try
            {
                ApiDataControllerAttribute attr = GetApiDataControllerAttribute<B>();
                SetParameterCollectionDbObject(parameters);
                base.DatabaseObjectInstance.CommandTimeout = this.HttpTimeout;
                reader = await base.DatabaseObjectInstance.Get(attr.Options.HasFlag(ApiDataControllerOptions.NoController) ? string.Empty : attr.ControllerName, action, parameters);
                ApiResponseMessage = RawResponse = reader.Raw;
                if (reader.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    dynamic responseObj = DeserializeApiResponse<T>(reader);
                    return responseObj;
                }
                else
                {
                    throw new CodedThoughtApplicationException($"There was an error calling the web service.  The reported error was {(int) reader.StatusCode} {reader.ErrorMessage}.");
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>Retrieves a list of objects matching the supplied Kay-Value par criteria.</summary>
        /// <typeparam name="T">The type of objects to retrieve.</typeparam>
        /// <typeparam name="B">The type of Data Aware entity to retrieve.</typeparam>
        /// <param name="list">      A reference to the list in which the items are to be returned.</param>
        /// <param name="parameters">A collection of Key-Value pairs.</param>
        public async Task<T> GetMultiple<T, B>(string action, ParameterCollection parameters) where T : class, new() where B : class, new()
        {
            ApiDataReader reader = null;
            try
            {
                ApiDataControllerAttribute attr = GetApiDataControllerAttribute<B>();
                SetParameterCollectionDbObject(parameters);
                base.DatabaseObjectInstance.CommandTimeout = this.HttpTimeout;
                reader = await base.DatabaseObjectInstance.Get(attr.Options.HasFlag(ApiDataControllerOptions.NoController) ? string.Empty : attr.ControllerName, action, parameters);
                ApiResponseMessage = RawResponse = reader.Raw;
                if (reader.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    dynamic responseObj = DeserializeApiResponse<T>(reader);
                    return responseObj;
                }
                else
                {
                    throw new CodedThoughtApplicationException($"There was an error calling the web service.  The reported error was {(int) reader.StatusCode} {reader.ErrorMessage}.");
                }

            }
            catch
            {
                throw;
            }
        }

        /// <summary>Saves the passed object to the Api based on the current controller and configured action name.</summary>
        /// <typeparam name="T">The type of objects to retrieve.</typeparam>
        /// <typeparam name="B">The type of Data Aware entity to retrieve.</typeparam>
        /// <param name="obj">The instance of the object</param>
        /// <returns><see cref="HttpResponseMessage" /></returns>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [Obsolete("The Save<T,B> method is deprecated in favor of the more standard naming of Post<T, B>.")]
        public async Task<T> Save<T, B>(B obj)
        {
            ApiDataControllerAttribute attr = GetApiDataControllerAttribute<B>();
            return attr.Action != null
                ? await Post<T, B>(attr.Action, obj)
                : throw new CodedThoughtApplicationException($"The object type, {typeof(B).Name}, does not contain an action.  Please update its ApiDataControllerAttribute or use the other Save method to explicitly pass the action.");
        }
        /// <summary>Saves the passed object to the Api based on the current controller and passed action name.</summary>
        /// <typeparam name="T">The type of object to posted.</typeparam>
        /// <typeparam name="B">The type of Data Aware entity to store.</typeparam>
        /// <param name="obj">The instance of the object</param>
        /// <returns><see cref="HttpResponseMessage" /></returns>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        [Obsolete("The Save<T,B> method is deprecated in favor of the more standard naming of Post<T, B>.")]
        public async Task<T> Save<T, B>(string action, B obj) => await Post<T, B>(action, obj);
        /// <summary>Posts the passed object to the Api based on the current controller and passed action name.</summary>
        /// <typeparam name="T">The type of object to posted.</typeparam>
        /// <typeparam name="B">The type of Data Aware entity to store.</typeparam>
        /// <param name="obj">The instance of the object</param>
        /// <returns><see cref="HttpResponseMessage" /></returns>
        public async Task<T> Post<T, B>(string action, B obj)
        {
            try
            {
                ApiDataControllerAttribute attr = GetApiDataControllerAttribute<B>();
                return await Post<T, B>(attr.ControllerName, action, obj);
            }
            catch
            {
                throw;
            }
        }
        /// <summary>Posts the passed object to the Api based on the passed controller and action name.</summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <typeparam name="BObject"></typeparam>
        /// <param name="controller"></param>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<TResponse> Post<TResponse, BObject>(string controller, string action, BObject obj)
        {
            try
            {
                string serializedObj = JsonSerializer.Serialize(obj);
                using (HttpClient httpClient = new())
                {
                    Uri baseUri = new(SourceUrl);
                    httpClient.Timeout = new TimeSpan(0, 0, this.HttpTimeout);
                    httpClient.BaseAddress = baseUri;
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders
                        .Accept
                        .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    if (UseBasicAuth)
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {EncodeBasicAuthCredentials(Username, Password)}");

                    HttpRequestMessage requestMessage;
                    if (string.IsNullOrEmpty(controller))
                    {
                        requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{SourceUrl}/{action}")
                        {
                            // CONTENT-TYPE Header
                            Content = new StringContent(serializedObj, Encoding.UTF8, "application/json")
                        };
                    }
                    else
                    {
                        requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{SourceUrl}/{controller}/{action}")
                        {
                            // CONTENT-TYPE Header
                            Content = new StringContent(serializedObj, Encoding.UTF8, "application/json")
                        };
                    }
                    using (HttpResponseMessage apiCall = await httpClient.SendAsync(requestMessage))
                    {
                        if (apiCall.IsSuccessStatusCode)
                        {
                            string apiResponse = await apiCall.Content.ReadAsStringAsync();
                            ApiResponseMessage = RawResponse = apiResponse;
                            return DeserializeApiResponse<TResponse>(apiResponse);
                        }
                        else
                        {
                            throw new CodedThoughtApplicationException($"There was an error calling the web service.  The reported error was {apiCall.StatusCode}.");
                        }
                    }
                }
            }
            catch
            {

                throw;
            }
        }

        /// <summary>Deletes the passed object using the Api based on the current controller and configured action.</summary>
        /// <typeparam name="T">The type of objects to post.</typeparam>
        /// <typeparam name="B">The type of Data Aware entity to delete.</typeparam>
        /// <param name="obj">The instance of the object</param>
        /// <returns><see cref="HttpResponseMessage" /></returns>
        public async Task<T> Remove<T, B>(T obj)
        {
            ApiDataControllerAttribute attr = GetApiDataControllerAttribute<B>();
            List<PropertyInfo> properties = typeof(T).GetProperties().ToList();
            object? keyValue = null;
            foreach (PropertyInfo prop in properties)
            {
                if (prop.GetCustomAttribute<ApiDataParameterAttribute>(true).PropertyName == GetKeyPropertyName<T>())
                {
                    keyValue = prop.GetValue(obj);
                }
            }
            return keyValue == null
                ? throw new CodedThoughtApplicationException($"The object type, {typeof(T).Name}, does not contain a default parameter.  Please add the DefaultParameter flag to an Api attribute or use the other Remove method to explicity pass the key value.")
                : attr.Action != null && keyValue != null
                ? await Remove<T, B>(attr.Action, keyValue.ToString())
                : throw new CodedThoughtApplicationException($"The object type, {typeof(T).Name}, does not contain an action.  Please update its ApiDataControllerAttribute or use the other Remove method to explicitly pass the action.");
        }

        /// <summary>Deletes the passed object using the Api based on the current controller and configured action.</summary>
        /// <typeparam name="T">The type of objects to post.</typeparam>
        /// <typeparam name="B">The type of Data Aware entity to delete.</typeparam>
        /// <param name="obj">The instance of the object</param>
        /// <returns><see cref="HttpResponseMessage" /></returns>
        public async Task<T> Remove<T, B>(string action, string id)
        {
            try
            {
                ApiDataControllerAttribute attr = GetApiDataControllerAttribute<B>();
                using (HttpClient httpClient = new HttpClient())
                {
                    Uri baseUri = new(SourceUrl);
                    httpClient.Timeout = new TimeSpan(0, 0, this.HttpTimeout);
                    httpClient.BaseAddress = baseUri;
                    httpClient.DefaultRequestHeaders.Clear();
                    // ACCEPT Header
                    httpClient.DefaultRequestHeaders
                        .Accept
                        .Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    if (UseBasicAuth)
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {EncodeBasicAuthCredentials(Username, Password)}");

                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"{SourceUrl}/{attr.ControllerName}/{action}");
                    using (HttpResponseMessage apiCall = await httpClient.SendAsync(requestMessage))
                    {
                        if (apiCall.IsSuccessStatusCode)
                        {
                            string apiResponse = await apiCall.Content.ReadAsStringAsync();
                            ApiResponseMessage = RawResponse = apiResponse;
                            return DeserializeApiResponse<T>(apiResponse);
                        }
                        else
                        {
                            throw new CodedThoughtApplicationException($"There was an error calling the web service.  The reported error was {apiCall.StatusCode}.");
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>Sorts the specified list.</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">         The list.</param>
        /// <param name="sortBy">       The property to sort by.</param>
        /// <param name="sortDirection">The direction to sort the list.</param>
        public new void Sort<T>(ref List<T> list, PropertyInfo sortBy, ListSortDirection sortDirection) where T : class, new()
        {
            try
            {
                string propertyName = sortBy.Name;

                list.Sort(delegate (T obj1, T obj2)
                {
                    return obj1.GetType().GetProperty(propertyName).MemberType.CompareTo(obj2.GetType().GetProperty(propertyName).MemberType);
                });
            }
            catch
            {
                throw;
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>Parses the passed REST Api connection string.</summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private void PopulateCurrentConnection(ConnectionSetting? connectionSetting)
        {
            ArgumentNullException.ThrowIfNull(connectionSetting);
            string[] urlParts = connectionSetting.ConnectionString.Split(";".ToCharArray());
            for (int i = 0; i <= urlParts.Length - 1; i++)
            {
                string[] connectionParameter = urlParts[i].Split("=".ToCharArray());
                switch (connectionParameter[0])
                {
                    case "Api Url":
                    case "Data Source":
                        SourceUrl = connectionParameter[1];
                        break;
                    case "User Id":
                        Username = connectionParameter[1];
                        break;
                    case "Password":
                        Password = connectionParameter[1];
                        break;
                }
            }
            if (Username != null && Password != null)
                UseBasicAuth = true;
        }
        private ApiDataControllerAttribute GetApiDataControllerAttribute<T>() => (ApiDataControllerAttribute) ORM[$"{typeof(T).FullName}{ORM_API_KEY_EXT}"][typeof(T)];
        protected T DeserializeApiResponse<T>(ApiDataReader reader)
        {
            dynamic objectJson = Helpers.Serialize<T>(reader.Data);
            return Helpers.Deserialize<T>(objectJson);
        }

        protected T DeserializeApiResponse<T>(string response) => Helpers.Deserialize<T>(response);

        /// <summary>Gets the parameters names from the passed data object.</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public new List<String> GetColumnNames<T>()
        {
            List<string> listParameters = new();
            foreach (ApiDataParameterAttribute attrColumn in ((ApiDataControllerAttribute) ORM[$"{typeof(T).FullName}{ORM_API_KEY_EXT}"][typeof(T)]).Properties)
            {
                listParameters.Add(attrColumn.ParameterName);
            }
            return listParameters;
        }

        /// <summary>Gets the source name from object.</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public new string GetSourceNameFromObject<T>()
        {
            ApiDataControllerAttribute source = (ApiDataControllerAttribute) ORM[$"{typeof(T).FullName}{ORM_API_KEY_EXT}"][typeof(T)];
            return source.ControllerName;
        }

        #endregion Private Methods
    }
}