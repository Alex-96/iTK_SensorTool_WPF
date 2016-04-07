using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SensorTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const int SplashScreenMinimumDelayMiliseconds = 7000;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            var splashScreen = new SplashScreen();
            splashScreen.Show();

            var splashScreenDelayTask = Task.Delay(App.SplashScreenMinimumDelayMiliseconds);

            var mainWindow = new MainWindow();

            await
                Task.WhenAll(
                    mainWindow.RunLongProcess(),
                    splashScreenDelayTask
                );

            splashScreen.Close();
            mainWindow.Show();
        }

    }
}
