// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Transactions;

namespace RIFF.Core
{
    internal class RFSQLCatalog : RFCatalog
    {
        protected static int _commandTimeout = 240;
        protected static bool _firstInit = true;
        protected static int _maxKeyLength = 2048;
        protected static int _maxResults = 100000;
        protected static bool _trackKeyHash = true;
        protected static bool _useTransactions = true;
        protected string _connectionString;

        public RFSQLCatalog(RFComponentContext context)
            : base(context)
        {
            _connectionString = context.SystemConfig.DocumentStoreConnectionString;
            if (_firstInit && _connectionString.NotBlank())
            {
                var sqlConnection = new SqlConnection(_connectionString);
                sqlConnection.Open();
                context.Log.Info(this, "Connected to SQL Server database [{0}] on server {1} (v{2}).", sqlConnection.Database, sqlConnection.DataSource, sqlConnection.ServerVersion);
                sqlConnection.Close();
                _firstInit = false;

                _useTransactions = RFSettings.GetAppSetting("UseTransactions", true);
                _trackKeyHash = RFSettings.GetAppSetting("TrackKeyHash", true);
                _maxResults = RFSettings.GetAppSetting("MaxResults", _maxResults);
                _commandTimeout = RFSettings.GetAppSetting("CommandTimeout", _commandTimeout);
                _maxKeyLength = RFSettings.GetAppSetting("MaxKeyLength", _maxKeyLength);

                context.Log.Debug(this, "UseTransactions: {0}, CommandTimeout: {1}s, KeyHash: {2}", _useTransactions, _commandTimeout, _trackKeyHash);
            }
        }

        public override Dictionary<RFGraphInstance, RFCatalogKey> GetKeyInstances(RFCatalogKey key)
        {
            var t = key.GetType();
            var keys = new Dictionary<RFGraphInstance, RFCatalogKey>();
            string keyType = t.FullName;

            Log.Debug(this, "GetKeyInstances {0}", keyType);
            try
            {
                var dataTable = new DataTable();
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string getKeysSQL = "RIFF.GetKeyInstances";
                    using (var getCommand = CreateCommand(getKeysSQL, connection))
                    {
                        var rootHash = RFStringHelpers.QuickHash(key.RootKey().ToString());

                        getCommand.CommandType = CommandType.StoredProcedure;
                        getCommand.Parameters.AddWithValue("@KeyType", keyType);
                        getCommand.Parameters.AddWithValue("@SerializedKey", RFXMLSerializer.SerializeContract(key));
                        getCommand.Parameters.AddWithValue("@RootHash", rootHash);
                        using (var reader = getCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                        {
                            dataTable.Load(reader);
                        }
                    }
                }
                if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count > 0)
                {
                    // cache deserializer if key is explicit
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        try
                        {
                            var catalogKeyID = (long)dataRow["CatalogKeyID"];
                            var retrievedKeyType = dataRow["KeyType"].ToString();
                            var serializedKey = dataRow["SerializedKey"].ToString();
                            var graphInstanceName = dataRow["GraphInstanceName"].ToString();
                            var graphInstanceDate = new RFDate((int)dataRow["GraphInstanceDate"]);
                            var deserializedKey = RFXMLSerializer.DeserializeContract(retrievedKeyType, serializedKey);

                            keys.Add(
                                new RFGraphInstance
                                {
                                    Name = graphInstanceName,
                                    ValueDate = graphInstanceDate
                                },
                                deserializedKey as RFCatalogKey);
                        }
                        catch (Exception ex)
                        {
                            Log.Exception(this, ex, "Error deserializing key {0}", dataRow["SerializedKey"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(this, "Error retrieving key instances", ex);
            }

            return keys;
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        public override RFCatalogKeyMetadata GetKeyMetadata(RFCatalogKey key)
        {
            var keyType = key.GetType().FullName;
            var keyString = key.ToString();
            var keyHash = RFStringHelpers.QuickHash(keyString);

            //Log.Debug(this, "GetKeyMetadata {0}", keyType);
            try
            {
                var dataTable = new DataTable();
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var getCommand = CreateCommand("[RIFF].[GetKeyMetadata]", connection))
                    {
                        getCommand.CommandType = CommandType.StoredProcedure;
                        getCommand.Parameters.AddWithValue("@KeyType", keyType);
                        getCommand.Parameters.AddWithValue("@SerializedKey", keyString);
                        getCommand.Parameters.AddWithValue("@KeyHash", keyHash);

                        using (var reader = getCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                        {
                            dataTable.Load(reader);
                        }
                    }
                }
                if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count > 0)
                {
                    var dataRow = dataTable.Rows[0];
                    return new RFCatalogKeyMetadata
                    {
                        ContentType = dataRow["ContentType"].ToString(),
                        KeyType = dataRow["KeyType"].ToString(),
                        Key = RFXMLSerializer.DeserializeContract(dataRow["KeyType"].ToString(), dataRow["SerializedKey"].ToString()) as RFCatalogKey,
                        KeyReference = (long)dataRow["CatalogKeyID"],
                        Metadata = RFMetadata.Deserialize(dataRow["Metadata"].ToString()),
                        UpdateTime = (DateTimeOffset)dataRow["UpdateTime"],
                        IsValid = (bool)dataRow["IsValid"],
                        DataSize = (long)dataRow["DataSize"]
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Exception(this, "Error retrieving key metadata", ex);
            }
            return null;
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        public override Dictionary<long, RFCatalogKey> GetKeysByType(Type t)
        {
            var keys = new Dictionary<long, RFCatalogKey>();
            string keyType = t.FullName;
            var retrieveAllKeys = t.Equals(typeof(RFCatalogKey));

            Log.Debug(this, "GetKeysByType {0}", keyType);
            try
            {
                var dataTable = new DataTable();
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var getKeysSQL = String.Format("SELECT [CatalogKeyID], [KeyType], CAST([SerializedKey] AS VARCHAR({0})) AS [SerializedKey] FROM [RIFF].[CatalogKey]", _maxKeyLength);
                    if (!retrieveAllKeys)
                    {
                        getKeysSQL = getKeysSQL + " WHERE [KeyType] = @KeyType";
                    }
                    using (var getCommand = CreateCommand(getKeysSQL, connection))
                    {
                        if (!retrieveAllKeys)
                        {
                            getCommand.Parameters.AddWithValue("@KeyType", keyType);
                        }
                        using (var reader = getCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                        {
                            dataTable.Load(reader);
                        }
                    }
                }

                if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count > 0)
                {
                    // cache deserializer if key is explicit
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        try
                        {
                            var catalogKeyID = (long)dataRow["CatalogKeyID"];
                            var retrievedKeyType = dataRow["KeyType"].ToString();
                            var serializedKey = dataRow["SerializedKey"].ToString();
                            var deserializedKey = RFXMLSerializer.DeserializeContract(retrievedKeyType, serializedKey);

                            keys.Add(
                                catalogKeyID,
                                deserializedKey as RFCatalogKey);
                        }
                        catch (Exception ex)
                        {
                            Log.Exception(this, ex, "Error deserializing key {0}", dataRow["SerializedKey"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(this, "Error retrieving keys by type", ex);
            }

            return keys;
        }

        public override RFCatalogEntry LoadItem(RFCatalogKey itemKey, int version = 0, bool ignoreContent = false)
        {
            RFCatalogEntry entry = null;

            if (itemKey.Plane == RFPlane.User)
            {
                //Log.Debug(this, "LoadItem {0}", catalogKey.ToString());
            }
            try
            {
                var dataRow = new Dictionary<string, object>();
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    try
                    {
                        long catalogEntryID = 0;
                        using (var command = LoadEntryCommand(itemKey, version, connection))
                        {
                            using (var reader = command.ExecuteReader(System.Data.CommandBehavior.SingleRow))
                            {
                                if (reader.HasRows)
                                {
                                    reader.Read();

                                    catalogEntryID = reader.GetInt64(7);

                                    dataRow.Add("CatalogKeyID", reader.GetInt64(0));
                                    dataRow.Add("KeyType", reader.GetString(1));
                                    dataRow.Add("SerializedKey", reader.GetString(2));
                                    dataRow.Add("Version", reader.GetInt32(3));
                                    dataRow.Add("Metadata", reader.IsDBNull(4) ? String.Empty : reader.GetString(4));
                                    dataRow.Add("IsValid", reader.GetBoolean(5));
                                    dataRow.Add("UpdateTime", reader.GetDateTimeOffset(6));
                                    dataRow.Add("CatalogEntryID", catalogEntryID);
                                    dataRow.Add("ContentType", reader.GetString(8));
                                    dataRow.Add("BinaryContent", reader.IsDBNull(9) ? null : reader.GetSqlBinary(9).Value);
                                }
                            }
                        }
                    }
                    catch (SqlException)
                    {
                        throw;
                    }
                    catch (InvalidOperationException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(this, ex, "Error loading entry {0}", itemKey);
                    }
                }
                if (dataRow.Any())
                {
                    entry = ExtractEntry(itemKey.StoreType, dataRow, ignoreContent);
                }
            }
            catch (SqlException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Exception(this, ex, "Error loading entry {0}", itemKey);
            }

            return entry;
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        public override bool SaveItem(RFCatalogEntry item, bool overwrite = false)
        {
            if (item == null)
            {
                Log.Warning(this, "SaveItem with null");
                return false;
            }

            if (item.Key.Plane == RFPlane.User)
            {
                //Log.Debug(this, "SaveItem {0}", entry.Key.ToString());
            }
            try
            {
                var serializedContent = (item is RFDocument && item.IsValid) ? SerializeContent((item as RFDocument).Content) : new byte[0];
                var compressedContent = CompressContent(serializedContent);
                var keyType = item.Key.GetType().FullName;
                var keyString = item.Key.ToString();
                var keyHash = RFStringHelpers.QuickHash(keyString);
                var keyHashResource = "KH" + keyHash;

                using (var scope = Transaction.Current == null ? new TransactionScope(_useTransactions ? TransactionScopeOption.Required : TransactionScopeOption.Suppress, new TransactionOptions
                {
                    IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                    Timeout = TimeSpan.FromSeconds(30)
                }) : null)
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        // acquire lock on the hash
                        string lockSQL = "sp_getapplock";
                        string unlockSQL = "sp_releaseapplock @Resource = '" + keyHashResource + "';";
                        using (var lockCommand = new SqlCommand(lockSQL, connection))
                        {
                            lockCommand.CommandType = CommandType.StoredProcedure;
                            lockCommand.Parameters.AddWithValue("@Resource", keyHashResource);
                            lockCommand.Parameters.AddWithValue("@LockMode", "Exclusive");
                            lockCommand.Parameters.AddWithValue("@LockOwner", "Transaction");
                            var retValue = lockCommand.Parameters.Add("@ReturnVal", SqlDbType.Int);
                            retValue.Direction = ParameterDirection.ReturnValue;
                            //RFStatic.Log.Debug(this, "sp_getapplock called...");
                            lockCommand.ExecuteNonQuery();
                            if ((int)retValue.Value < 0)
                            {
                                Log.Error(this, "sp_getapplock returned {0}", retValue.Value);
                            }
                            //RFStatic.Log.Debug(this, "sp_getapplock returned {0}", retValue.Value);
                        }

                        try
                        {
                            // find or create existing key
                            long catalogKeyID = 0;
                            var getKeySQL = String.Format("SELECT [CatalogKeyID] FROM [RIFF].[CatalogKey] WHERE [KeyHash] = @KeyHash AND [KeyType] = @KeyType AND CAST([SerializedKey] AS VARCHAR({0})) = CAST(CAST(@SerializedKey AS XML) AS VARCHAR({0}))", _maxKeyLength);
                            if (!_trackKeyHash)
                            {
                                getKeySQL = String.Format("SELECT [CatalogKeyID] FROM [RIFF].[CatalogKey] WHERE [KeyType] = @KeyType AND CAST([SerializedKey] AS VARCHAR({0})) = CAST(CAST(@SerializedKey AS XML) AS VARCHAR({0}))", _maxKeyLength);
                            }
                            using (var getCommand = CreateCommand(getKeySQL, connection))
                            {
                                getCommand.Parameters.AddWithValue("@KeyType", keyType);
                                if (_trackKeyHash)
                                {
                                    getCommand.Parameters.AddWithValue("@KeyHash", keyHash);
                                }
                                getCommand.Parameters.AddWithValue("@SerializedKey", keyString);
                                var result = getCommand.ExecuteScalar();
                                if (result != null)
                                {
                                    catalogKeyID = (long)result;
                                }
                            }

                            if (catalogKeyID == 0)
                            {
                                string createKeySQL = "INSERT INTO [RIFF].[CatalogKey] ( [KeyType], [SerializedKey], [KeyHash], [RootHash], [FriendlyString] )"
                                    + " VALUES ( @KeyType, @SerializedKey, @KeyHash, @RootHash, @FriendlyString ); SELECT SCOPE_IDENTITY()";
                                using (var createKeyCommand = CreateCommand(createKeySQL, connection))
                                {
                                    var rootHash = RFStringHelpers.QuickHash(item.Key.RootKey().ToString());

                                    createKeyCommand.Parameters.AddWithValue("@KeyType", keyType);
                                    createKeyCommand.Parameters.AddWithValue("@SerializedKey", keyString);
                                    createKeyCommand.Parameters.AddWithValue("@KeyHash", keyHash);
                                    createKeyCommand.Parameters.AddWithValue("@RootHash", rootHash);
                                    createKeyCommand.Parameters.AddWithValue("@FriendlyString", RFStringHelpers.StringToSQL(item.Key.FriendlyString(), true, 100, false));

                                    var result = createKeyCommand.ExecuteScalar();
                                    if (result != null)
                                    {
                                        catalogKeyID = (long)((decimal)result);
                                    }
                                }
                            }

                            if (catalogKeyID == 0)
                            {
                                throw new RFSystemException(this, "Unable to create new catalog key.");
                            }

                            // lookup any existing entries and calculate next version
                            int version = 1;
                            string getVersionSQL = "SELECT MAX([Version]) FROM [RIFF].[CatalogEntry] WHERE [CatalogKeyID] = @CatalogKeyID";
                            using (var getVersionCommand = CreateCommand(getVersionSQL, connection))
                            {
                                getVersionCommand.Parameters.AddWithValue("@CatalogKeyID", catalogKeyID);

                                var result = getVersionCommand.ExecuteScalar();
                                if (result != null && result != DBNull.Value)
                                {
                                    version = ((int)result) + 1;
                                }
                            }

                            // there is an existing document - compare content
                            if (version > 1)
                            {
                                long existingEntryID = 0;
                                using (var loadExistingCommand = LoadEntryCommand(item.Key, version - 1, connection))
                                {
                                    using (var reader = loadExistingCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                                    {
                                        var dataTable = new DataTable();
                                        dataTable.Load(reader);
                                        if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count == 1)
                                        {
                                            switch (item.Key.StoreType)
                                            {
                                                case RFStoreType.Document:
                                                    existingEntryID = (long)dataTable.Rows[0]["CatalogEntryID"];
                                                    try
                                                    {
                                                        var existingType = dataTable.Rows[0]["ContentType"].ToString();
                                                        var existingBinaryContent = dataTable.Rows[0]["BinaryContent"] as byte[];
                                                        var existingValid = (bool)dataTable.Rows[0]["IsValid"];

                                                        // decompress binary content to avoid
                                                        // flagging update if only compression changed
                                                        if (existingBinaryContent != null && existingBinaryContent.Length > 0 && existingValid == item.IsValid)
                                                        {
                                                            var rawExistingContent = DecompressContent(existingBinaryContent);
                                                            if (Enumerable.SequenceEqual(serializedContent ?? new byte[0], rawExistingContent ?? new byte[0]))
                                                            {
                                                                //transaction.Rollback(); -- to avoid zombiecheck errors
                                                                if (item.Key.Plane == RFPlane.User)
                                                                {
                                                                    Log.Info(this, "Not required to update {0}/{1}/{2}", item.Key.GetType().Name, item.Key.FriendlyString(), item.Key.GetInstance());
                                                                }
                                                                if (!_useTransactions) // lock will be auto released on transaction which prevents other thread coming in before transaction is completed
                                                                {
                                                                    using (var unlockCommand = new SqlCommand(unlockSQL, connection))
                                                                    {
                                                                        unlockCommand.ExecuteNonQuery();
                                                                    }
                                                                }
                                                                scope.Complete();
                                                                return false;
                                                            }
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Log.Exception(this, "Unable to compare to existing", ex);
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                }
                                if (overwrite && existingEntryID > 0)
                                {
                                    // update content rather than create new version
                                    var document = item as RFDocument;
                                    string updateDocumentSQL = "UPDATE [RIFF].[CatalogDocument] SET [BinaryContent] = @BinaryContent, [ContentType] = @ContentType where [CatalogEntryID] = @CatalogEntryID";
                                    using (var updateDocumentCommand = CreateCommand(updateDocumentSQL, connection))
                                    {
                                        updateDocumentCommand.Parameters.AddWithValue("@CatalogEntryID", existingEntryID);
                                        updateDocumentCommand.Parameters.AddWithValue("@ContentType", document.Type);
                                        if (compressedContent == null || compressedContent.Length == 0)
                                        {
                                            updateDocumentCommand.Parameters.AddWithValue("@BinaryContent", new byte[0]);
                                        }
                                        else
                                        {
                                            updateDocumentCommand.Parameters.AddWithValue("@BinaryContent", compressedContent);
                                        }

                                        var affected = updateDocumentCommand.ExecuteNonQuery();
                                        if (affected != 1)
                                        {
                                            throw new RFSystemException(this, "Unable to update document.");
                                        }
                                    }
                                    string updateEntrySQL = "UPDATE [RIFF].[CatalogEntry] SET [UpdateTime] = @UpdateTime, [Metadata] = @Metadata, [IsValid] = @IsValid where [CatalogEntryID] = @CatalogEntryID";
                                    using (var updateEntryCommand = CreateCommand(updateEntrySQL, connection))
                                    {
                                        updateEntryCommand.Parameters.AddWithValue("@CatalogEntryID", existingEntryID);
                                        updateEntryCommand.Parameters.AddWithValue("@IsValid", item.IsValid);
                                        updateEntryCommand.Parameters.AddWithValue("@Metadata", item.Metadata != null ? ((object)item.Metadata.Serialize() ?? DBNull.Value) : DBNull.Value);
                                        updateEntryCommand.Parameters.AddWithValue("@UpdateTime", item.UpdateTime.Year < 1980 ? new DateTimeOffset(DateTime.Now) : item.UpdateTime);

                                        var affected = updateEntryCommand.ExecuteNonQuery();
                                        if (affected != 1)
                                        {
                                            throw new RFSystemException(this, "Unable to update entry.");
                                        }
                                    }
                                    if (item.Key.Plane == RFPlane.User)
                                    {
                                        Log.Debug(this, "Overwritten item {0}", item.Key.ToString());
                                    }
                                    if (!_useTransactions) // lock will be auto released on transaction which prevents other thread coming in before transaction is completed
                                    {
                                        using (var unlockCommand = new SqlCommand(unlockSQL, connection))
                                        {
                                            unlockCommand.ExecuteNonQuery();
                                        }
                                    }
                                    scope?.Complete();
                                    return true;
                                }
                            }

                            // create entry
                            long catalogEntryID = 0;
                            string createEntrySQL = "INSERT INTO [RIFF].[CatalogEntry] ( [CatalogKeyID], [Version], [Metadata], [IsValid], [UpdateTime] )" +
                                " VALUES (@CatalogKeyID, @Version, @Metadata, @IsValid, @UpdateTime); SELECT SCOPE_IDENTITY()";
                            using (var createEntryCommand = CreateCommand(createEntrySQL, connection))
                            {
                                createEntryCommand.Parameters.AddWithValue("@CatalogKeyID", catalogKeyID);
                                createEntryCommand.Parameters.AddWithValue("@Metadata", item.Metadata != null ? ((object)item.Metadata.Serialize() ?? DBNull.Value) : DBNull.Value);
                                createEntryCommand.Parameters.AddWithValue("@Version", version);
                                createEntryCommand.Parameters.AddWithValue("@IsValid", item.IsValid);
                                createEntryCommand.Parameters.AddWithValue("@UpdateTime", item.UpdateTime.Year < 1980 ? new DateTimeOffset(DateTime.Now) : item.UpdateTime);

                                var result = createEntryCommand.ExecuteScalar();
                                if (result != null && result != DBNull.Value)
                                {
                                    catalogEntryID = (long)((decimal)result);
                                }
                            }

                            if (catalogEntryID == 0)
                            {
                                throw new RFSystemException(this, "Unable to create new catalog entry.");
                            }

                            // create content
                            switch (item.Key.StoreType)
                            {
                                case RFStoreType.Document:
                                    {
                                        var document = item as RFDocument;
                                        string createDocumentSQL = "INSERT INTO [RIFF].[CatalogDocument] ( [CatalogEntryID], [ContentType], [BinaryContent] ) VALUES ( @CatalogEntryID, @ContentType, @BinaryContent )";
                                        using (var createDocumentCommand = CreateCommand(createDocumentSQL, connection))
                                        {
                                            createDocumentCommand.Parameters.AddWithValue("@CatalogEntryID", catalogEntryID);
                                            createDocumentCommand.Parameters.AddWithValue("@ContentType", document.Type);
                                            if (compressedContent == null || compressedContent.Length == 0)
                                            {
                                                createDocumentCommand.Parameters.AddWithValue("@BinaryContent", new byte[0]);
                                            }
                                            else
                                            {
                                                createDocumentCommand.Parameters.AddWithValue("@BinaryContent", compressedContent);
                                            }

                                            var affected = createDocumentCommand.ExecuteNonQuery();
                                            if (affected != 1)
                                            {
                                                throw new RFSystemException(this, "Unable to create document.");
                                            }
                                        }
                                    }
                                    break;

                                default:
                                    throw new RFSystemException(this, "Unknown store type {0}", item.Key.StoreType);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Exception(this, ex, "Error saving entry (inner) {0}", item.Key);
                        }
                        if (!_useTransactions) // lock will be auto released on transaction which prevents other thread coming in before transaction is completed
                        {
                            using (var unlockCommand = new SqlCommand(unlockSQL, connection))
                            {
                                unlockCommand.ExecuteNonQuery();
                            }
                        }
                    }
                    if (scope != null)
                    {
                        scope.Complete();
                    }
                }
                if (item.Key.Plane == RFPlane.User)
                {
                    Log.Info(this, "Saved key {0}/{1}/{2}", item.Key.GetType().Name, item.Key.FriendlyString(), item.Key.GetInstance());
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(this, ex, "Error saving entry (outer) {0}", item.Key);
                return false;
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        public override List<RFCatalogKeyMetadata> SearchKeys(Type t, DateTime? startTime = null, DateTime? endTime = null, int limitResults = 0, RFDate? valueDate = null, bool latestOnly = false)
        {
            var keys = new List<RFCatalogKeyMetadata>();
            var keyType = t.FullName;
            var retrieveAllKeys = t.Equals(typeof(RFCatalogKey));
            if (limitResults == 0)
            {
                limitResults = _maxResults;
            }

            // the current query does not support snapshot-in-time, need to extend KeysLatestView to
            // take in a ValueDate param
            if (valueDate != null)
            {
                latestOnly = false;
            }

            //Log.Debug(this, "GetKeyMetadata {0}", keyType);
            try
            {
                var dataTable = new DataTable();
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    /*

                    var viewName = latestOnly ? "[KeysLatestView]" : "[KeysView]";

                    var getKeysSQL = String.Format("SELECT TOP {0} [CatalogKeyID], [KeyType], [ContentType], [Metadata], [Version], [UpdateTime], [SerializedKey], [IsValid], [DataSize], [GraphInstanceName], [GraphInstanceDate] FROM [RIFF].{1} WITH(NOLOCK)", limitResults, viewName);
                    var criteria = new List<string>();
                    if (!retrieveAllKeys)
                    {
                        criteria.Add("[KeyType] = @KeyType");
                    }
                    if (startTime.HasValue)
                    {
                        criteria.Add("[UpdateTime] >= @StartTime");
                    }
                    if (endTime.HasValue)
                    {
                        criteria.Add("[UpdateTime] <= @EndTime");
                    }
                    if (valueDate.HasValue)
                    {
                        criteria.Add("([GraphInstanceDate] IS NULL OR [GraphInstanceDate] <= @ValueDate)");
                    }
                    if (criteria.Count > 0)
                    {
                        getKeysSQL = getKeysSQL + " WHERE " + String.Join(" AND ", criteria);
                    }
                    getKeysSQL = getKeysSQL + " ORDER BY [UpdateTime] DESC";*/
                    using (var getCommand = CreateCommand("[RIFF].[SearchKeys]", connection))
                    {
                        getCommand.CommandType = CommandType.StoredProcedure;
                        getCommand.Parameters.AddWithValue("@LimitResults", limitResults);

                        if (retrieveAllKeys)
                        {
                            getCommand.Parameters.AddWithValue("@KeyType", DBNull.Value);
                        }
                        else
                        {
                            getCommand.Parameters.AddWithValue("@KeyType", keyType);
                        }

                        if (startTime.HasValue)
                        {
                            getCommand.Parameters.AddWithValue("@StartTime", startTime);
                        }

                        if (endTime.HasValue)
                        {
                            getCommand.Parameters.AddWithValue("@EndTime", startTime);
                        }

                        if (valueDate.HasValue)
                        {
                            getCommand.Parameters.AddWithValue("@GraphInstanceDate", valueDate.Value.ToYMD());
                        }

                        if (latestOnly)
                        {
                            getCommand.Parameters.AddWithValue("@LatestInstanceOnly", true);
                            getCommand.Parameters.AddWithValue("@ExcludeFiles", true);
                        }

                        using (var reader = getCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                        {
                            dataTable.Load(reader);
                        }
                    }
                }

                if (dataTable != null && dataTable.Rows != null && dataTable.Rows.Count > 0)
                {
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        try
                        {
                            keys.Add(new RFCatalogKeyMetadata
                            {
                                ContentType = dataRow["ContentType"].ToString(),
                                KeyType = dataRow["KeyType"].ToString(),
                                Key = RFXMLSerializer.DeserializeContract(dataRow["KeyType"].ToString(), dataRow["SerializedKey"].ToString()) as RFCatalogKey,
                                KeyReference = (long)dataRow["CatalogKeyID"],
                                Metadata = RFMetadata.Deserialize(dataRow["Metadata"].ToString()),
                                UpdateTime = (DateTimeOffset)dataRow["UpdateTime"],
                                IsValid = (bool)dataRow["IsValid"],
                                DataSize = (long)dataRow["DataSize"],
                                Instance = (dataRow["GraphInstanceName"] != DBNull.Value && dataRow["GraphInstanceDate"] != DBNull.Value) ? new RFGraphInstance
                                {
                                    Name = dataRow["GraphInstanceName"].ToString(),
                                    ValueDate = new RFDate((int)dataRow["GraphInstanceDate"])
                                } : null
                            });
                        }
                        catch (Exception ex)
                        {
                            Log.Exception(this, ex, "Error deserializing key metadata {0}", dataRow["SerializedKey"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Exception(this, "Error retrieving key metadata", ex);
            }

            return keys;
        }

        protected static byte[] CompressContent(byte[] serializedContent)
        {
            return RFCompressor.CompressBytes(serializedContent);
        }

        protected static byte[] DecompressContent(byte[] compressedContent)
        {
            return RFCompressor.DecompressBytes(compressedContent);
        }

        protected static object DeserializeContent(string type, byte[] data)
        {
            return RFXMLSerializer.BinaryDeserializeContract(type, RFCompressor.DecompressBytes(data));
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        protected static SqlCommand LoadEntryCommand(RFCatalogKey itemKey, int version, SqlConnection connection, SqlTransaction transaction = null)
        {
            if (itemKey.StoreType != RFStoreType.Document)
            {
                throw new Exception(String.Format("Unrecognized store type {0}", itemKey.StoreType));
            }

            SqlCommand sqlCommand = null;
            if (transaction != null)
            {
                sqlCommand = CreateCommand("[RIFF].[GetDocument]", connection, transaction);
            }
            else
            {
                sqlCommand = CreateCommand("[RIFF].[GetDocument]", connection);
            }

            var keyString = itemKey.ToString();
            sqlCommand.CommandType = CommandType.StoredProcedure;
            sqlCommand.Parameters.AddWithValue("@KeyType", itemKey.GetType().FullName);
            sqlCommand.Parameters.AddWithValue("@SerializedKey", keyString);
            sqlCommand.Parameters.AddWithValue("@KeyHash", RFStringHelpers.QuickHash(keyString));
            sqlCommand.Parameters.AddWithValue("@Version", version);
            return sqlCommand;
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        protected static SqlCommand LoadStructureCommand(string tableName, long catalogEntryID, SqlConnection connection, SqlTransaction transaction)
        {
            var commandText = String.Format("SELECT * FROM {0} WHERE [CatalogEntryID] = @CatalogEntryID", tableName);
            var sqlCommand = CreateCommand(commandText, connection, transaction);
            sqlCommand.Parameters.AddWithValue("@CatalogEntryID", catalogEntryID);
            return sqlCommand;
        }

        protected static byte[] SerializeContent(object o)
        {
            return RFXMLSerializer.BinarySerializeContract(o);
        }

        protected RFCatalogEntry ExtractDocument(Dictionary<string, object> dataRow, bool ignoreContent)
        {
            var type = dataRow["ContentType"].ToString();
            if (ignoreContent)
            {
                return new RFDocument
                {
                    Content = null,
                    Type = type
                };
            }
            else
            {
                var binaryContent = dataRow["BinaryContent"] as byte[];
                if (binaryContent != null && binaryContent.Length > 0)
                {
                    return new RFDocument
                    {
                        Content = DeserializeContent(type, binaryContent),
                        Type = type
                    };
                }
                else if (dataRow.ContainsKey("Content"))
                {
                    return new RFDocument
                    {
                        Content = RFXMLSerializer.DeserializeContract(type, dataRow["Content"].ToString()),
                        Type = type
                    };
                }
                return new RFDocument
                {
                    Content = null,
                    Type = type
                };
            }
        }

        protected RFCatalogEntry ExtractEntry(RFStoreType storeType, Dictionary<string, object> dataRow, bool ignoreContent)
        {
            RFCatalogEntry entry = null;
            switch (storeType)
            {
                case RFStoreType.Document:
                    entry = ExtractDocument(dataRow, ignoreContent);
                    break;

                default:
                    throw new Exception(String.Format("Unrecognized store type {0}", storeType));
            }

            // common fields
            var type = dataRow["KeyType"].ToString();
            entry.Key = (RFCatalogKey)RFXMLSerializer.DeserializeContract(type, dataRow["SerializedKey"].ToString());
            entry.Metadata = RFMetadata.Deserialize(dataRow["Metadata"].ToString()) ?? new RFMetadata();
            entry.UpdateTime = (DateTimeOffset)dataRow["UpdateTime"];
            entry.Version = (int)dataRow["Version"];
            entry.IsValid = (bool)dataRow["IsValid"];
            return entry;
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        private static SqlCommand CreateCommand(string sql, SqlConnection conn, SqlTransaction tran = null)
        {
            SqlCommand c = null;
            if (tran == null)
            {
                c = new SqlCommand(sql, conn);
            }
            else
            {
                c = new SqlCommand(sql, conn, tran);
            }
            c.CommandTimeout = _commandTimeout;
            return c;
        }
    }
}
