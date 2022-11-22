
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Net.Http.Headers;
using Terminal.Gui;
using tricorder;
using tricorder.Scanner;
using tricorder.Windows;


// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

IServiceProvider serviceProvider = null;
var runId = Guid.NewGuid().ToString();

try
{
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddScoped<DbConnection>(e =>
        new SQLiteConnection($"Data Source=tricorder-{runId};Mode=Memory;Cache=Shared"));
    serviceCollection.AddTransient<MainWindow>();
    serviceCollection.AddTransient<DiskVolumeScanWindow>();
    serviceCollection.AddScoped<VolumeScanner>();
    serviceCollection.AddLogging(l => l
                        .AddFilter("Microsoft", LogLevel.Warning)
                        .AddFilter("System", LogLevel.Warning)
                        .AddFilter("tricorder", LogLevel.Debug)
                        .AddConsole());

    serviceProvider = serviceCollection.BuildServiceProvider();
}
catch (Exception e)
{
    Console.Out.WriteLine("Error occured");
    Console.Out.WriteLine(e.Message);
}
Application.Init();
Application.Top.Add(serviceProvider.GetRequiredService<MainWindow>());
Application.Run();

Application.Shutdown();

