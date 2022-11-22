using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Security.AccessControl;

namespace tricorder.Scanner
{
    internal class VolumeScanner
    {
        private const string FILE_INSERT_COMMAND = "INSERT INTO files (name, length, volume_id, directory_id) VALUES (:name, :length, :volume_id, :directory_id)";
        private const string DIRECTORY_INSERT_COMMAND = "INSERT INTO directories (name, volume_id, directory_id) VALUES (:name, :volume_id, :directory_id)";

        private readonly DbConnection dbConnection;
        private readonly ILogger<VolumeScanner> logger;

        public VolumeScanner(DbConnection dbConnection, ILogger<VolumeScanner> logger)
        {
            this.dbConnection = dbConnection;
            this.logger = logger;
        }

        public async Task Scan()
        {
            try
            {
                var startTime = new DateTimeOffset();

                await dbConnection.OpenAsync();

                if (!(Tools.checkForTable(dbConnection, "files")))
                {
                    await SetupDatabase(dbConnection);
                }


                using var volumeInsertCommamd = dbConnection.CreateCommand();
                volumeInsertCommamd.CommandText = "INSERT INTO volumes (name) VALUES (:name)";
                foreach (var drive in DriveInfo.GetDrives())
                {
                    //Console.Out.WriteLine($"Scanning drive {drive.Name}");
                    volumeInsertCommamd.Parameters.Clear();
                    volumeInsertCommamd.Parameters.Add(new SQLiteParameter(parameterName: "name", drive.Name));
                    await volumeInsertCommamd.ExecuteNonQueryAsync();

                    var volumeId = await Tools.getLastId(dbConnection);

                    await LoadDirectory(dbConnection, drive.RootDirectory, volumeId: volumeId);
                }
            } catch(Exception e)
            {
                logger.LogError(e, "Error while scanning volums");
            }

        }
        private static async Task SetupDatabase(DbConnection dbConnection)
        {
            using (var cmd = dbConnection.CreateCommand())
            {
                Console.Out.WriteLine("Creating tables");
                cmd.CommandText = @"CREATE TABLE volumes (
	            id long PRIMARY KEY,
   	            name string NOT NULL
            );";
                await cmd.ExecuteNonQueryAsync();
                cmd.CommandText = @"CREATE TABLE directories (
	            id long PRIMARY KEY,
   	            name string NOT NULL,
   	            volume_id long NOT NULL,
                directory_id long NULL,
FOREIGN KEY (volume_id) REFERENCES volumes (id),
FOREIGN KEY (directory_id) REFERENCES directories (id)
            );";
                await cmd.ExecuteNonQueryAsync();
                cmd.CommandText = @"CREATE TABLE files (
	            id long PRIMARY KEY,
   	            name string NOT NULL,
	            length long NOT NULL,
   	            volume_id long NOT NULL,
	            directory_id long NULL,
FOREIGN KEY (volume_id) REFERENCES volumes (id),
FOREIGN KEY (directory_id) REFERENCES directories (id)
            );";
                await cmd.ExecuteNonQueryAsync();
                Console.Out.WriteLine("Tables created");
            }

        }

        private async Task LoadDirectory(DbConnection database, DirectoryInfo directory,
                                        long volumeId, long? directoryId = null)
        {
            using (var cmd = database.CreateCommand())
            {
                cmd.CommandText = FILE_INSERT_COMMAND;
                try
                {
                    foreach (var child in directory.GetFiles())
                    {
                        try
                        {
                            //Console.WriteLine(child.FullName);
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add(new SQLiteParameter(parameterName: "name", child.Name));
                            cmd.Parameters.Add(new SQLiteParameter(parameterName: "length", child.Length));
                            cmd.Parameters.Add(new SQLiteParameter(parameterName: "volume_id", volumeId));
                            cmd.Parameters.Add(new SQLiteParameter(parameterName: "directory_id", (object)directoryId ?? DBNull.Value));
                            await cmd.ExecuteNonQueryAsync();
                        }
                        catch (Exception e)
                        {
                            logger.LogWarning(e, $"Error while scanning file {child.Name}");
                        }
                    }
                }
                catch (Exception) { }


                try
                {
                    cmd.CommandText = DIRECTORY_INSERT_COMMAND;
                    foreach (var child in directory.GetDirectories())
                    {
                        try
                        {
                            //Console.WriteLine(child.FullName);
                            cmd.Parameters.Clear();
                            cmd.Parameters.Add(new SQLiteParameter(parameterName: "name", child.Name));
                            cmd.Parameters.Add(new SQLiteParameter(parameterName: "volume_id", volumeId));
                            cmd.Parameters.Add(new SQLiteParameter(parameterName: "directory_id", (object)directoryId ?? DBNull.Value));
                            await cmd.ExecuteNonQueryAsync();

                            var childDirectoryId = await Tools.getLastId(database);

                            await LoadDirectory(database, directory: child, volumeId: volumeId, directoryId: childDirectoryId);

                        }
                        catch (Exception e)
                        {
                            logger.LogWarning(e, $"Error while scanning directory {child.Name}");
                        }
                    }
                }
                catch (Exception) { }


            }


        }

    }
}
