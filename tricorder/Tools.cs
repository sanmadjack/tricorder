using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace tricorder
{
    internal class Tools
    {
        public static async Task<long> getLastId(DbConnection database)
        {
            using var cmd = database.CreateCommand();
            cmd.CommandText = "select last_insert_rowid()";
            return (long)await cmd.ExecuteScalarAsync();
        }
        public static bool checkForTable(DbConnection database, string table)
        {
            using var cmd = database.CreateCommand();
            cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name=:table;";
            cmd.Parameters.Add(new SQLiteParameter(parameterName: "table", table));
            var count = (long)cmd.ExecuteScalar();
            return count!=0;
        }

        public static string formatBytes(long byteCount)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };            
            int order = 0;
            while (byteCount >= 1024 && order < sizes.Length - 1)
            {
                order++;
                byteCount = byteCount / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            string result = String.Format("{0:0.##} {1}", byteCount, sizes[order]);
            return result;
        }
    }
}
