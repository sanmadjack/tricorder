using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace tricorder.Windows
{
    internal class MainWindow: Window
    {
        private const string DISK_VOLUME_SCAN = "Disk Volumes";
        private readonly IServiceProvider serviceProvider;

        ListView mainMenuListView;

        public MainWindow(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            Title = "Tricorder v"+ Assembly.GetExecutingAssembly().GetName().Version;

            var menuLabel = new Label()
            {
                Text = "Please select a scanning mode."
            };

            mainMenuListView = new ListView()
            {
                Y = Pos.Top(menuLabel)+1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            mainMenuListView.SetSource(new string[]
            {
                DISK_VOLUME_SCAN,
                "Exit"
            });
            mainMenuListView.KeyDown += Actions_KeyDown;


            Add(menuLabel , mainMenuListView);
        }



        private void Actions_KeyDown(KeyEventEventArgs obj)
        {
            if(obj.KeyEvent.Key==Key.Enter)
            {
                switch(mainMenuListView.SelectedItem)
                {
                    case 0:
                        var window =  serviceProvider.GetRequiredService<DiskVolumeScanWindow>();
                        Add(window);
                        window.Go();
                        break;
                    case 1:
                        Application.RequestStop();
                        break;
                }
            }
        }



    }
}
