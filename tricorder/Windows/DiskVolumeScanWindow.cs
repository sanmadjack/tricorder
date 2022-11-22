using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using tricorder.Scanner;

namespace tricorder.Windows
{
    internal class DiskVolumeScanWindow: Window
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ProgressBar progressBar;
        private readonly Label filesScannedLabel;
        public DiskVolumeScanWindow(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            Title = "Disk Volume Scan";
            //Width = Dim.Percent(0.8f);
            //Height= Dim.Percent(0.5f);

            //filesScannedLabel = new Label()
            //{
            //    Text = "Files scanned: 0"
            //};

            progressBar = new ProgressBar()
            {
                //Y = Pos.Top(filesScannedLabel) + 1,
               Width =Dim.Fill()
            };


            Add(progressBar);
        }

        public void Go()
        {

            try
            {
                var scannerTask = Task.Run(async () =>
                {
                    using var scannerScope = serviceProvider.CreateScope();
                    var scanner = scannerScope.ServiceProvider.GetRequiredService<VolumeScanner>();
                    await scanner.Scan();
                });


                using var uiScope = serviceProvider.CreateScope();
                using var database = uiScope.ServiceProvider.GetRequiredService<DbConnection>();
                database.Open();
                using var fileCountCommand = database.CreateCommand();
                fileCountCommand.CommandText = "SELECT COUNT(*) FROM files";
                using var totalSizeCommand = database.CreateCommand();
                totalSizeCommand.CommandText = "SELECT SUM(length) FROM files";

                var tablesCreated = false;
                while (!scannerTask.IsCompleted)
                {
                    progressBar.Pulse();
                    if (!tablesCreated)
                    {
                        if (Tools.checkForTable(database, "files"))
                        {
                            tablesCreated = true;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                            continue;
                        }
                    }


                    var count = (long)fileCountCommand.ExecuteScalar();
                    var size = (long)totalSizeCommand.ExecuteScalar();

                    filesScannedLabel.Text = $"Files scanned: {count}";
                    //Console.WriteLine($"Scanned {count} files, total size {Tools.formatBytes(size)}");

                    Thread.Sleep(100);
                }
                Console.WriteLine("end of line");
                Console.In.ReadLine();

            }
            finally
            {

            }
        }

    }
}
