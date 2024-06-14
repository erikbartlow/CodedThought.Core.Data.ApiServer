using System.Data;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using CodedThought.Core.Configuration;
using CodedThought.Core.Data.Interfaces;

namespace CodedThought.Core.Data.ApiServer
{

    public class ApiServerDatabaseObject : DatabaseObject, IDatabaseObject
    {

        public ApiServerDatabaseObject()
        {
            SupportedDatabase = DBSupported.ApiServer;

        }
        #region Override Methods

        /// <summary>
        /// Sends a ping test to the configured web service Url and returns true or false depending on if it was successfull.
        /// </summary>
        /// <returns></returns>
        public override bool TestConnection()
        {
            try
            {
                if (base.CoreConnection == null)
                {
                    throw new ApplicationException("The web service connection setting information is not set.");
                }

                if (String.IsNullOrEmpty(ConnectionSetting.SourceUrl))
                {
                    throw new ApplicationException("The web service host Url is empty.");
                }
                
                if (ApiHostUri.Port == null)
                {
                    using (TcpClient client = new(ApiHostUri.Host, 80))
                        return true;
                }
                else
                {
                    using (TcpClient client = new(ApiHostUri.Host, ApiHostUri.Port ))
                        return true;
                }
            }
            catch { return false; }
        }

        protected override IDbConnection OpenConnection() => throw new NotImplementedException();

        protected ApiToken Authenticate(string username, string password) => throw new NotImplementedException();

        #endregion Override Methods

        #region Internal Methods

        public ApiConnectionSetting ConnectionSetting => (ApiConnectionSetting) base.CoreConnection; 
        public Uri ApiHostUri
        {
            get
            {
                Uri uri = new(ConnectionSetting.SourceUrl);
                return uri;
            }
        }
        /// <summary>Encodes the basic authentication to pass in HttpClient web calls.</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private string EncodeBasicAuthCredentials(string username, string password)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes($"Basic {username} {password}");
            return System.Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>Creates an Api parameter</summary>
        /// <param name="actionName">    </param>
        /// <param name="parameterValue"></param>
        /// <returns><see cref="CodedThought.Core.Data.Parameter" /></returns>
        public override IDataParameter CreateApiParameter(string parameterName, string parameterValue)
        {
            ApiParameter param = new(DbType.String, ParameterDirection.Input, parameterName, parameterValue);
            return param;
        }


        #endregion Internal Methods

        #region Non-Implemented Interface Methods

        public override void Commit() => throw new NotImplementedException();

        public override string ConvertToChar(string columnName) => throw new NotImplementedException();

        public override IDataParameter CreateBetweenParameter(string srcTableColumnName, BetweenParameter betweenParam) => throw new NotImplementedException();

        public override IDataParameter CreateBooleanParameter(string srcTableColumnName, bool parameterValue) => throw new NotImplementedException();

        public override IDataParameter CreateCharParameter(string srcTableColumnName, string parameterValue, int size) => throw new NotImplementedException();

        public override IDataParameter CreateDateTimeParameter(string srcTableColumnName, DateTime parameterValue) => throw new NotImplementedException();

        public override IDataParameter CreateDoubleParameter(string srcTableColumnName, double parameterValue) => throw new NotImplementedException();

        public override IDataParameter CreateEmptyParameter() => new ApiParameter();

        public override IDataParameter CreateGuidParameter(string srcTableColumnName, Guid parameterValue) => throw new NotImplementedException();

        public override IDataParameter CreateInt32Parameter(string srcTableColumnName, int parameterValue) => throw new NotImplementedException();

        public override IDataParameter CreateOutputParameter(string parameterName, DbTypeSupported returnType) => throw new NotImplementedException();

        public override IDataParameter CreateParameter(object obj, TableColumn col, IDBStore store) => throw new NotImplementedException();

        public override ParameterCollection CreateParameterCollection() => throw new NotImplementedException();

        public override IDataParameter CreateReturnParameter(string parameterName, DbTypeSupported returnType) => throw new NotImplementedException();

        public override IDataParameter CreateStringParameter(string srcTableColumnName, string parameterValue) => throw new NotImplementedException();

        public override IDataParameter CreateXMLParameter(string srcTaleColumnName, string parameterValue) => throw new NotImplementedException();

        public override string GetCaseDecode(string columnName, string equalValue, string trueValue, string falseValue, string alias) => throw new NotImplementedException();

        public override string GetCurrentDateFunction() => throw new NotImplementedException();

        public override string GetDateOnlySqlSyntax(string dateColumn) => throw new NotImplementedException();

        public override string GetDatePart(string datestring, DateFormat dateFormat, DatePart datePart) => throw new NotImplementedException();

        public override string GetDateToStringForColumn(string columnName, DateFormat dateFormat) => throw new NotImplementedException();

        public override string GetDateToStringForValue(string value, DateFormat dateFormat) => throw new NotImplementedException();

        public override string GetFunctionName(FunctionName functionName) => throw new NotImplementedException();

        public override string GetIfNullFunction(string validateColumnName, string optionColumnName) => throw new NotImplementedException();

        public override string GetIfNullFunction() => throw new NotImplementedException();

        public override string GetStringFromBlob(IDataReader reader, string columnName) => throw new NotImplementedException();

        public override string GetStringToDateSqlSyntax(string dateString) => throw new NotImplementedException();

        public override string GetStringToDateSqlSyntax(DateTime dateSQL) => throw new NotImplementedException();

        public override string GetYearSQLSyntax(string dateString) => throw new NotImplementedException();

        public override string ToDate(string datestring, DateFormat dateFormat) => throw new NotImplementedException();

        public override Type ToSystemType(string dbTypeName) => throw new NotImplementedException();

        protected override IDataAdapter CreateDataAdapter(IDbCommand cmd) => throw new NotImplementedException();

        protected override byte[] GetBlobValue(IDataReader reader, string columnName) => throw new NotImplementedException();

        public override string GetTableDefinitionQuery(string tableName) => throw new NotImplementedException();

        public override string GetDefaultSessionSchemaNameQuery() => throw new NotImplementedException();
        public override void Add(string tableName, object obj, List<TableColumn> columns, IDBStore store) => throw new NotImplementedException();
        public override DbTypeSupported ToDbSupportedType(string dbTypeName) => throw new NotImplementedException();
        public override string GetViewDefinitionQuery(string viewName) => throw new NotImplementedException();
        public override string GetTableListQuery() => throw new NotImplementedException();
        public override string GetViewListQuery() => throw new NotImplementedException();
        public override List<TableColumn> GetTableDefinition(string tableName) => throw new NotImplementedException();
        public override List<TableColumn> GetViewDefinition(string viewName) => throw new NotImplementedException();
        public override List<TableSchema> GetTableDefinitions() => throw new NotImplementedException();
        public override List<ViewSchema> GetViewDefinitions() => throw new NotImplementedException();

        #endregion Non-Implemented Interface Methods
    }
}