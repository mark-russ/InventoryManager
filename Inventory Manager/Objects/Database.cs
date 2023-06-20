using System;
using Adericium;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Data;

namespace Inventory_Manager.Objects
{
    public delegate void SearchEventHandler(Object sender, SearchEventArgs e);
    public class SearchEventArgs
    {
        public List<InventoryItem> Results { get; set; }
        public String SearchTerms { get; set; }

        public SearchEventArgs(List<InventoryItem> result, String terms)
        {
            Results = result;
            SearchTerms = terms;
        }
    }

    public delegate void DatabaseQueryErrorEventHandler(Object sender, DatabaseQueryErrorEventArgs e);
    public class DatabaseQueryErrorEventArgs
    {
        public Exception Exception;

        public DatabaseQueryErrorEventArgs(Exception ex)
        {
            Exception = ex;
        }
    }

    public static class Database
    {
        public static uint SupportedRevision = 2;

        public static event DatabaseQueryErrorEventHandler DatabaseQueryError;
        public static event SearchEventHandler SearchResultsReceived;
        private static String LastSearch;
        private static String PendingSearch;
        private static Object myLock = new Object();
        private static Task SearchThread;

        public static Boolean IsSearching {
            get
            {
                return SearchThread == null ? false : SearchThread.Status == TaskStatus.Running;
            }
        }
        
        public static Exception Initialize(Profile profile)
        {
            try
            {
                using (MySqlConnection connection = GetConnection(profile))
                {
                    connection.Open();

                    ExecuteNonQuery(@"
                            CREATE TABLE IF NOT EXISTS `settings` (
                                `name` VARCHAR(40) COLLATE 'utf8mb4_unicode_ci',
                                `value` VARCHAR(40) COLLATE 'utf8mb4_unicode_ci',
                                PRIMARY KEY (`name`)
                            )
                            COLLATE = 'utf8mb4_unicode_ci'
                            ENGINE = InnoDB", connection);


                    uint dbRevision = 0;
                    if (uint.TryParse(ExecuteScalar("SELECT value FROM settings WHERE name='revision'", connection)?.ToString(), out dbRevision) == false) {
                        ExecuteNonQuery($"INSERT INTO settings VALUES ('revision', '{dbRevision}')", connection);
                    }

                    if (dbRevision == 0)
                    {
                        dbRevision++;

                        using (MySqlTransaction transaction = connection.BeginTransaction()) {
                            ExecuteNonQuery($@"
                            CREATE TABLE `inventory` (
	                            `id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
	                            `upc` VARCHAR(14) NULL DEFAULT NULL COLLATE 'utf8mb4_unicode_ci',
	                            `name` VARCHAR(80) NULL DEFAULT NULL COLLATE 'utf8mb4_unicode_ci',
	                            `price` DECIMAL(10, 2) NULL DEFAULT NULL,
	                            `weight` DECIMAL(10, 1) NOT NULL,
	                            `stock` INT(11) NOT NULL,
	                            `box` VARCHAR(10) NULL DEFAULT NULL COLLATE 'utf8mb4_unicode_ci',
	                            `extra` VARCHAR(255) NULL DEFAULT NULL COLLATE 'utf8mb4_unicode_ci',
	                            `note` TEXT NULL COLLATE 'utf8mb4_unicode_ci',
	                            `state` INT(11) NOT NULL,
	                            `listing_id` VARCHAR(20) NULL DEFAULT NULL COLLATE 'utf8mb4_unicode_ci',
                                PRIMARY KEY(`id`)
                            )
                            COLLATE = 'utf8mb4_unicode_ci'
                            ENGINE = InnoDB;

                 		    #########################################################
                            DROP PROCEDURE IF EXISTS DeleteIfVariant;
                            CREATE PROCEDURE `DeleteIfVariant`(
	                            IN `pUPC` VARCHAR(14),
	                            IN `pName` VARCHAR(80),
	                            IN `pItemId` INT
                            )

                            LANGUAGE SQL
                            NOT DETERMINISTIC
                            NO SQL
                            SQL SECURITY DEFINER

                            BEGIN
                                # If the current item is sold and there are other variations then remove this item from the DB.
                                IF ((SELECT state FROM inventory WHERE id=pItemId) = 2 AND EXISTS(SELECT 1 FROM inventory WHERE upc=pUPC AND name=pName AND id<>pItemId)) THEN
                                    DELETE FROM inventory WHERE id=pItemId;
                                END IF;
                            END;

                 		    #########################################################
                            DROP FUNCTION IF EXISTS IsNowDeletedVariant;
                            CREATE FUNCTION `IsNowDeletedVariant`(
	                            `pItemId` INT
                            ) RETURNS BOOL

                            LANGUAGE SQL
                            NOT DETERMINISTIC
                            NO SQL
                            SQL SECURITY DEFINER

                            BEGIN
                            	DECLARE itemUPC VARCHAR(14);
                            	DECLARE itemName VARCHAR(80);
	                            SELECT name, upc INTO itemName, itemUPC FROM inventory WHERE id=pItemId;

                                IF (EXISTS(SELECT 1 FROM inventory WHERE upc=itemUPC AND name=itemName AND id<>pItemId)) THEN
                                    DELETE FROM inventory WHERE id=pItemId;
                                    RETURN TRUE;
                                END IF;

                                RETURN FALSE;
                            END;

                            ######################################################
                            DROP FUNCTION IF EXISTS UpdateStock;
                            CREATE FUNCTION `UpdateStock` (
                                `pItemId` INT UNSIGNED, 
                                `pPurchasedStock` INT,
                                `pDeleteIfVariant` BOOL
                            ) RETURNS VARCHAR(16) 

                            LANGUAGE SQL
                            NOT DETERMINISTIC
                            NO SQL
                            SQL SECURITY DEFINER
                            
                            BEGIN
                            	DECLARE itemStock INT;
                            	 
	                            SET itemStock := (SELECT stock FROM inventory WHERE id=pItemId);
	
	                            IF (itemStock = NULL) THEN
		                            RETURN 'NOT FOUND';
	                            ELSEIF (itemStock < pPurchasedStock) THEN
		                            RETURN 'SHORTAGE';
	                            ELSEIF (itemStock = pPurchasedStock) THEN 
	                            	 IF (pDeleteIfVariant = TRUE) THEN
                                         IF (SELECT IsNowDeletedVariant(pItemId) = TRUE) THEN
		                                    RETURN 'DELETED';
                                         ELSE
		                            	    UPDATE inventory SET stock=0, state=2, price=NULL, box=NULL, extra=NULL, listing_id=NULL WHERE id=pItemId;
                                         END IF;
	                            	 ELSE
		                            	 UPDATE inventory SET stock=0, state=2, price=NULL, box=NULL, extra=NULL, listing_id=NULL WHERE id=pItemId;
	                            	 END IF;
	                            ELSE
		                            UPDATE inventory SET stock=stock-pPurchasedStock WHERE id=pItemId;
	                            END IF;
	
	                            IF (ROW_COUNT() = 1) THEN
		                            RETURN 'UPDATED';
	                            END IF;
		
	                            RETURN 'UPDATE FAILURE';
                            END;

                            UPDATE settings SET value='{dbRevision}' WHERE name='revision'", connection, transaction);

                            transaction.Commit();
                        }
                    }
                    
                    if (dbRevision == 1) {
                        dbRevision++;

                        using (MySqlTransaction transaction = connection.BeginTransaction()) {
                            ExecuteNonQuery($@"ALTER TABLE `inventory` CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci; UPDATE settings SET value='{dbRevision}' WHERE name='revision'", connection, transaction);
                            transaction.Commit();
                        }
                    }
                }
            }
            catch (Exception ex) {
                return ex;
            }
            
            return null;
        }
        
        // App.Settings.Profile.CurrentItemCategory.DeleteVariations
        public static MySqlConnection GetConnection() {
            return GetConnection(App.Settings.Profile);
        }

        public static MySqlConnection GetConnection(Profile profile) {
            return GetConnection(profile.DatabaseHostname, profile.DatabasePort, profile.DatabaseUsername, profile.DatabasePassword, profile.DatabaseName, profile.DatabaseSSLMode, profile.DatabaseTimeout);
        }

        public static MySqlConnection GetConnection(String hostname, Int32 port, String username, String password, String databaseName, String sslMode, Int32 timeout) {
            return new MySqlConnection($"SERVER={hostname};PORT={port};UID={username};PWD={Adericium.Utility.DPAPI_Unprotect(password, null, System.Security.Cryptography.DataProtectionScope.CurrentUser)};DATABASE={databaseName};SSLMODE={sslMode};CONNECTIONRESET=TRUE;CONNECTIONTIMEOUT={timeout};ALLOWUSERVARIABLES=TRUE");
        }

        public static String GetConnectionString(String hostname, Int32 port, String username, String password, String databaseName, String sslMode, Int32 timeout) {
            return $"SERVER={hostname};PORT={port};UID={username};PWD={Adericium.Utility.DPAPI_Unprotect(password, null, System.Security.Cryptography.DataProtectionScope.CurrentUser)};DATABASE={databaseName};SSLMODE={sslMode};CONNECTIONRESET=TRUE;CONNECTIONTIMEOUT={timeout};ALLOWUSERVARIABLES=TRUE";
        }
        private static MySqlCommand Query_UpdateStockFromOrders;
        public static String UpdateStockFromOrders(IList<InventoryOrder> orders)
        {
            if (Query_UpdateStockFromOrders == null) {
                Query_UpdateStockFromOrders = new MySqlCommand("UpdateStock") {
                    CommandType = CommandType.StoredProcedure
                };
                
                Query_UpdateStockFromOrders.Parameters.Add(new MySqlParameter("pItemId", MySqlDbType.UInt32) {
                    Direction = ParameterDirection.Input
                });

                Query_UpdateStockFromOrders.Parameters.Add(new MySqlParameter("pPurchasedStock", MySqlDbType.Int32) {
                    Direction = ParameterDirection.Input
                });

                Query_UpdateStockFromOrders.Parameters.Add(new MySqlParameter("pDeleteIfVariant", MySqlDbType.Bool) {
                    Direction = ParameterDirection.Input
                });

                Query_UpdateStockFromOrders.Parameters.Add(new MySqlParameter("result", MySqlDbType.VarChar) {
                    Direction = ParameterDirection.ReturnValue
                });
            }

            using (Query_UpdateStockFromOrders.Connection = GetConnection())
            {
                Query_UpdateStockFromOrders.Connection.Open();
                Query_UpdateStockFromOrders.Transaction = Query_UpdateStockFromOrders.Connection.BeginTransaction();

                foreach (var order in orders) {
                    Query_UpdateStockFromOrders.Parameters["pItemId"].Value = order.Id;
                    Query_UpdateStockFromOrders.Parameters["pPurchasedStock"].Value = order.QuantityOrdered;
                    Query_UpdateStockFromOrders.Parameters["pDeleteIfVariant"].Value = App.Settings.Profile.CurrentItemCategory.DeleteVariations;
                    Query_UpdateStockFromOrders.ExecuteNonQuery();
                    var result = Query_UpdateStockFromOrders.Parameters["result"].Value;

                    switch (result?.ToString())
                    {
                        case "UPDATED":
                        case "DELETED":
                            continue;
                        case "NOT FOUND":
                            return $"The item \"{order.Label}\" no longer exists in the inventory. The inventory update was rolled back.";
                        case "SHORTAGE":
                            return $"There is not enough \"{order.Label}\" in stock to fill this order! The inventory update was rolled back.";
                        case "UPDATE FAILURE":
                            return $"The update failed. The inventory update was rolled back.";
                        default:
                            return result?.ToString();
                    }
                    
                }

                Query_UpdateStockFromOrders.Transaction.Commit();
                return "SUCCESS";
            }
        }
        
        private static MySqlCommand Query_AddInventory;
        public static void AddInventory(InventoryItem item)
        {
            if (Query_AddInventory == null)
            {
                Query_AddInventory = new MySqlCommand("INSERT INTO inventory (upc, name, price, weight, stock, box, extra, note, state, listing_id) VALUES (@upc, @name, @price, @weight, @stock, @box, @extra, @note, @state, @listing_id)");
                Query_AddInventory.Parameters.AddWithValue("@upc", String.IsNullOrWhiteSpace(item.UPC) ? "N/A" : item.UPC);
                Query_AddInventory.Parameters.AddWithValue("@name", item.Name);
                Query_AddInventory.Parameters.AddWithValue("@price", item.Price);
                Query_AddInventory.Parameters.AddWithValue("@weight", item.Weight);
                Query_AddInventory.Parameters.AddWithValue("@stock", item.Stock);
                Query_AddInventory.Parameters.AddWithValue("@box", item.Box);
                Query_AddInventory.Parameters.AddWithValue("@extra", item.Extra);
                Query_AddInventory.Parameters.AddWithValue("@note", item.Note);
                Query_AddInventory.Parameters.AddWithValue("@state", item.State);
                Query_AddInventory.Parameters.AddWithValue("@listing_id", item.ListingId);
            }
            else
            {
                Query_AddInventory.Parameters["@upc"].Value = String.IsNullOrWhiteSpace(item.UPC) ? "N/A" : item.UPC;
                Query_AddInventory.Parameters["@name"].Value = item.Name;
                Query_AddInventory.Parameters["@price"].Value = item.Price;
                Query_AddInventory.Parameters["@weight"].Value = item.Weight;
                Query_AddInventory.Parameters["@stock"].Value = item.Stock;
                Query_AddInventory.Parameters["@box"].Value = item.Box;
                Query_AddInventory.Parameters["@extra"].Value = item.Extra;
                Query_AddInventory.Parameters["@note"].Value = item.Note;
                Query_AddInventory.Parameters["@state"].Value = item.State;
                Query_AddInventory.Parameters["@listing_id"].Value = item.ListingId;
            }

            try
            {
                using (Query_AddInventory.Connection = GetConnection())
                {
                    Query_AddInventory.Connection.Open();
                    Query_AddInventory.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                DatabaseQueryError?.Invoke(null, new DatabaseQueryErrorEventArgs(ex));
            }
        }
        
        private static MySqlCommand Query_RemoveInventory;
        public static void RemoveInventory(UInt32 id)
        {
            if (Query_RemoveInventory == null)
            {
                Query_RemoveInventory = new MySqlCommand("DELETE FROM inventory WHERE id=@id");
                Query_RemoveInventory.Parameters.AddWithValue("@id", id);
            }
            else
            {
                Query_RemoveInventory.Parameters["@id"].Value = id;
            }

            try {
                using (Query_RemoveInventory.Connection = GetConnection())
                {
                    Query_RemoveInventory.Connection.Open();
                    Query_RemoveInventory.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                DatabaseQueryError?.Invoke(null, new DatabaseQueryErrorEventArgs(ex));
            }
        }

        private static MySqlCommand Query_RemoveIfVariant;
        public static Boolean? RemoveIfVariant(InventoryItem item)
        {
            try
            {
                using (MySqlConnection connection = GetConnection()) {
                    return RemoveIfVariant(item, connection, null);
                }
            }
            catch (Exception ex) {
                DatabaseQueryError?.Invoke(null, new DatabaseQueryErrorEventArgs(ex));
            }

            return null;
        }
        private static Boolean RemoveIfVariant(InventoryItem item, MySqlConnection connection, MySqlTransaction transaction)
        {
            if (Query_RemoveIfVariant == null) {
                Query_RemoveIfVariant = new MySqlCommand("DeleteIfVariant", connection, transaction) {
                    CommandType = CommandType.StoredProcedure
                };

                Query_RemoveIfVariant.Parameters.AddWithValue("@pUPC", item.UPC);
                Query_RemoveIfVariant.Parameters.AddWithValue("@pName", item.Name);
                Query_RemoveIfVariant.Parameters.AddWithValue("@pItemId", item.Id);
            }
            else {
                Query_RemoveIfVariant.Connection = connection;
                Query_RemoveIfVariant.Transaction = transaction;
                Query_RemoveIfVariant.Parameters["@pUPC"].Value = item.UPC;
                Query_RemoveIfVariant.Parameters["@pName"].Value = item.Name;
                Query_RemoveIfVariant.Parameters["@pItemId"].Value = item.Id;
            }
            
            return Query_RemoveIfVariant.ExecuteNonQuery() == 1;
        }

        private static MySqlCommand Query_UpdateInventory;

        public enum UpdateActions {
            None,
            Updated,
            Deleted,
            Failed
        }
        public static UpdateActions UpdateInventory(InventoryItem item)
        {
            var actionTaken = UpdateActions.None;

            try
            {
                using (MySqlConnection connection = GetConnection())
                {
                    connection.Open();
                    MySqlTransaction transaction = connection.BeginTransaction();
                    actionTaken = UpdateInventory(item, connection, transaction) ? UpdateActions.Updated : UpdateActions.None;
                    
                    if (App.Settings.Profile.CurrentItemCategory.DeleteVariations) {
                        if (RemoveIfVariant(item, connection, transaction)) {
                            actionTaken = UpdateActions.Deleted;
                        }
                    }

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                DatabaseQueryError?.Invoke(null, new DatabaseQueryErrorEventArgs(ex));
                return UpdateActions.Failed;
            }

            return actionTaken;
        }
        
        private static Boolean UpdateInventory(InventoryItem item, MySqlConnection connection, MySqlTransaction transaction)
        {
            if (Query_UpdateInventory == null)
            {
                Query_UpdateInventory = new MySqlCommand("UPDATE inventory SET upc=@upc, name=@name, price=@price, weight=@weight, stock=@stock, box=@box, extra=@extra, note=@note, state=@state, listing_id=@listing_id WHERE id=@id", connection, transaction);
                Query_UpdateInventory.Parameters.AddWithValue("@id", item.Id);
                Query_UpdateInventory.Parameters.AddWithValue("@upc", item.UPC);
                Query_UpdateInventory.Parameters.AddWithValue("@name", item.Name);
                Query_UpdateInventory.Parameters.AddWithValue("@price", item.Price);
                Query_UpdateInventory.Parameters.AddWithValue("@weight", item.Weight);
                Query_UpdateInventory.Parameters.AddWithValue("@stock", item.Stock);
                Query_UpdateInventory.Parameters.AddWithValue("@box", item.Box);
                Query_UpdateInventory.Parameters.AddWithValue("@extra", item.Extra);
                Query_UpdateInventory.Parameters.AddWithValue("@note", item.Note);
                Query_UpdateInventory.Parameters.AddWithValue("@state", item.State);
                Query_UpdateInventory.Parameters.AddWithValue("@listing_id", item.ListingId);
            }
            else
            {
                Query_UpdateInventory.Connection = connection;
                Query_UpdateInventory.Transaction = transaction;
                Query_UpdateInventory.Parameters["@id"].Value = item.Id;
                Query_UpdateInventory.Parameters["@upc"].Value = item.UPC;
                Query_UpdateInventory.Parameters["@name"].Value = item.Name;
                Query_UpdateInventory.Parameters["@price"].Value = item.Price;
                Query_UpdateInventory.Parameters["@weight"].Value = item.Weight;
                Query_UpdateInventory.Parameters["@stock"].Value = item.Stock;
                Query_UpdateInventory.Parameters["@extra"].Value = item.Extra;
                Query_UpdateInventory.Parameters["@box"].Value = item.Box;
                Query_UpdateInventory.Parameters["@note"].Value = item.Note;
                Query_UpdateInventory.Parameters["@state"].Value = item.State;
                Query_UpdateInventory.Parameters["@listing_id"].Value = item.ListingId;
            }

            return Query_UpdateInventory.ExecuteNonQuery() == 1;
        }
        
        public static void StartSearching(String pending, Int32 maxSearchResults)
        {
            PendingSearch = pending;

            if (SearchThread?.IsCompleted == false) {
                return;
            }

            //SearchThread = Task.Factory.StartNew(() => StartSearchThreaded());
            SearchThread = Task.Factory.StartNew(() => {
                try
                {
                    do
                    {
                        LastSearch = PendingSearch;
                        var results = SearchInventory(PendingSearch, App.Settings.Profile.MaxSearchResults);

                        // Only submit the most up-to-date keywords as the user types.
                        if (LastSearch == PendingSearch) {
                            SearchResultsReceived?.Invoke(null, new SearchEventArgs(results, LastSearch));
                        }
                        
                    } while (PendingSearch != LastSearch);
                }
                catch (Exception ex)
                {
                    DatabaseQueryError?.Invoke(null, new DatabaseQueryErrorEventArgs(ex));
                }
            });
        }
        
        private static List<InventoryItem> SearchInventory(String terms, Int32 limit = 0)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                // Cancel search if the terms changed.
                if (terms != PendingSearch) return null;
                

                MySqlCommand query;

                if (terms.IsNumeric())
                {
                    query = new MySqlCommand("SELECT * FROM inventory WHERE upc LIKE @search OR listing_id LIKE @search ORDER BY upc LIMIT @limit", connection);
                    query.Parameters.AddWithValue("@search", terms + '%');
                    query.Parameters.AddWithValue("@limit", limit);
                }
                else
                {
                    query = new MySqlCommand("SELECT id, upc, name, price, weight, stock, box, extra, note, state, listing_id FROM inventory WHERE name LIKE @search ORDER BY upc LIMIT @limit", connection);
                    query.Parameters.AddWithValue("@search", '%' + terms + '%');
                    query.Parameters.AddWithValue("@limit", limit);
                }
                
                using (var reader = query.ExecuteReader())
                {
                    // Cancel search if the terms changed.
                    if (!reader.HasRows) return null;
                    

                    List<InventoryItem> list = new List<InventoryItem>();

                    while (reader.Read())
                    {
                        if (terms != PendingSearch) {
                            return null;
                        }
                        
                        InventoryItem item = new InventoryItem
                        {
                            Id = UInt32.Parse(reader.GetInt32(0).ToString()),
                            UPC = reader.GetString(1),
                            Name = reader.GetString(2),
                            Price = reader.IsDBNull(3) ? null : (Decimal?)reader.GetDecimal(3),
                            Weight = reader.GetDecimal(4),
                            Stock = reader.GetInt32(5),
                            Box = reader.IsDBNull(6) ? null : reader.GetString(6),
                            Extra = reader.IsDBNull(7) ? null : reader.GetString(7),
                            Note = reader.IsDBNull(8) ? null : reader.GetString(8),
                            State = (ItemStates)reader.GetInt32(9),
                            ListingId = reader.IsDBNull(10) ? null : reader.GetString(10)
                        };
                        list.Add(item);
                    }

                    return list;
                }
            }
        }
        
        private static async void StartSearchThreaded()
        {
            try
            {
                do
                {
                    LastSearch = PendingSearch;
                    SearchResultsReceived?.Invoke(null, new SearchEventArgs(await SearchInventoryThreaded(PendingSearch, App.Settings.Profile.MaxSearchResults), LastSearch));
                } while (PendingSearch != LastSearch);
            }
            catch (Exception ex)
            {
                DatabaseQueryError?.Invoke(null, new DatabaseQueryErrorEventArgs(ex));
            }
        }
        private static async Task<List<InventoryItem>> SearchInventoryThreaded(String terms, Int32 limit = 0)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                if (terms != PendingSearch)  {
                    return null;
                }

                MySqlCommand query;

                if (terms.IsNumeric()) {
                    query = new MySqlCommand("SELECT * FROM inventory WHERE upc LIKE @search OR listing_id LIKE @search ORDER BY upc LIMIT @limit", connection);
                    query.Parameters.AddWithValue("@search", terms+'%');
                    query.Parameters.AddWithValue("@limit", limit);
                }
                else {
                    query = new MySqlCommand("SELECT id, upc, name, price, weight, stock, box, extra, note, state, listing_id FROM inventory WHERE name LIKE @search ORDER BY upc LIMIT @limit", connection);
                    query.Parameters.AddWithValue("@search", '%'+terms+'%');
                    query.Parameters.AddWithValue("@limit", limit);
                }
                
                using (var reader = await query.ExecuteReaderAsync())
                {
                    if (!reader.HasRows) {
                        return null;
                    }

                    List<InventoryItem> list = new List<InventoryItem>();
                    
                    while (reader.Read())
                    {
                        if (terms != PendingSearch) {
                            return null;
                        }
                        
                        InventoryItem item = new InventoryItem
                        {
                            Id = UInt32.Parse(reader.GetInt32(0).ToString()),
                            UPC = reader.GetString(1),
                            Name = reader.GetString(2),
                            Price = reader.IsDBNull(3) ? null : (Decimal?)reader.GetDecimal(3),
                            Weight = reader.GetDecimal(4),
                            Stock = reader.GetInt32(5),
                            Box = reader.IsDBNull(6) ? null : reader.GetString(6),
                            Extra = reader.IsDBNull(7) ? null : reader.GetString(7),
                            Note = reader.IsDBNull(8) ? null : reader.GetString(8),
                            State = (ItemStates)reader.GetInt32(9),
                            ListingId = reader.IsDBNull(10) ? null : reader.GetString(10)
                        };
                        list.Add(item);
                    }

                    return list;
                }
            }
        }
       
        public static MySqlCommand Query_SearchForVariants;
        public static List<InventoryItem> SearchForVariants(InventoryItem searchItem, Boolean excludeSearchItem)
        {
            if (Query_SearchForVariants == null) {
                Query_SearchForVariants = new MySqlCommand("SELECT id, upc, name, price, weight, stock, box, extra, note, state, listing_id FROM inventory WHERE upc = @upc AND name = @name AND id<>@id ORDER BY upc");
                Query_SearchForVariants.Parameters.AddWithValue("@upc", searchItem.UPC);
                Query_SearchForVariants.Parameters.AddWithValue("@name", searchItem.Name);
                Query_SearchForVariants.Parameters.AddWithValue("@id", excludeSearchItem ? searchItem.Id : null);
            }
            else {
                Query_SearchForVariants.Parameters["@upc"].Value = searchItem.UPC;
                Query_SearchForVariants.Parameters["@name"].Value = searchItem.Name;
                Query_SearchForVariants.Parameters["@id"].Value = excludeSearchItem ? searchItem.Id : null;
            }

            using (Query_SearchForVariants.Connection = GetConnection())
            {
                Query_SearchForVariants.Connection.Open();

                using (var reader = Query_SearchForVariants.ExecuteReader())
                {
                    if (!reader.HasRows) {
                        return null;
                    }

                    List<InventoryItem> list = new List<InventoryItem>();

                    while (reader.Read())
                    {
                        InventoryItem item = new InventoryItem
                        {
                            Id = UInt32.Parse(reader.GetInt32(0).ToString()),
                            UPC = reader.GetString(1),
                            Name = reader.GetString(2),
                            Price = reader.IsDBNull(3) ? null : (Decimal?)reader.GetDecimal(3),
                            Weight = reader.GetDecimal(4),
                            Stock = reader.GetInt32(5),
                            Box = reader.IsDBNull(6) ? null : reader.GetString(6),
                            Extra = reader.IsDBNull(7) ? null : reader.GetString(7),
                            Note = reader.IsDBNull(8) ? null : reader.GetString(8),
                            State = (ItemStates)reader.GetInt32(9),
                            ListingId = reader.IsDBNull(10) ? null : reader.GetString(10)
                        };
                        list.Add(item);
                    }

                    return list;
                }
            }
        }

        public static Object ExecuteQuery(String query, MySqlConnection conn, MySqlTransaction trans)
        {
            MySqlCommand command = new MySqlCommand(query, conn, trans);
            DataTable data = new DataTable();

            if (query.StartsWith("SELECT"))
            {
                MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                adapter.Fill(data);
            }
            else
            {
                return "Affected rows: " + command.ExecuteNonQuery();
            }

            return data;
        }

        private static object ExecuteScalar(String command, MySqlConnection connection, MySqlTransaction transaction = null)
        {
            using (MySqlCommand cmd = new MySqlCommand(command, connection, transaction)) {
                return cmd.ExecuteScalar();
            }
        }

        private static int ExecuteNonQuery(String command, MySqlConnection connection, MySqlTransaction transaction = null)
        {
            using (MySqlCommand cmd = new MySqlCommand(command, connection, transaction)) {
                return cmd.ExecuteNonQuery();
            }
        }
        
    }
}
