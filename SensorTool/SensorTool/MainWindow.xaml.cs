using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro;
using LiveCharts;
using PP_ComLib_Wrapper;
using System.Windows.Threading;
using System.Threading;

namespace SensorTool
{    
    public partial class MainWindow : MetroWindow
    {
        PP_ComLib_WrapperClass ppSensor, ppActuator;
        threadDECdata DECdata;

        int I2C_ADDR = 0x00;
        //int ChartPointer = 0;

        string lastActiveSensorPort;
        string lastActiveActuatorPort;

        public SeriesCollection ChartSeries { get; set; }
        public Func<double, string> YFormatter { get; set; }
        public Func<double, string> XFormatter { get; set; }

        bool guiRunState = false;
        Thread ioThread = null;

        struct threadParams
        {
            public string sensorPort;
            public string actuatorPort;
        }

        public struct GUI_State
        {
            public string sensorPort;
            public string actuatorPort;

            public bool threadRunning;

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                {
                    return false;
                }

                GUI_State instance = (GUI_State)obj;

                return (this.sensorPort == instance.sensorPort) &&
                       (this.actuatorPort == instance.actuatorPort) &&
                       (this.threadRunning == instance.threadRunning);
            }

            public override int GetHashCode()
            {
                return 0; //not needed, added just to remove compiler warning
            }

        }
        GUI_State guiStatePrev;

        private DispatcherTimer timer = null;
        private DispatcherTimer ChartTimer = null;
        private int TimerIndex;

        //------------------------------------------------------------------------------------------
        // Auxiliary Methods
        //------------------------------------------------------------------------------------------
        #region Aux_Methods

        void GetActivePorts(out string sensorPort, out string actuatorPort)
        {
            sensorPort = cmbSensors.SelectedText;
            actuatorPort = cmbActuators.SelectedText;
        }
        
        void LoadPorts()
        {
            string[] ports;
            string strError;
            string activeSensorPort, activeActuatorPort;

            ppSensor.GetPorts(out ports, out strError);

            //0.  Remember current active ports
            GetActivePorts(out activeSensorPort, out  activeActuatorPort)
//            activeSensorPort = cmbSensors.SelectedText;
//            activeActuatorPort = cmbActuators.SelectedText;

            //1. Load current device list in Sensor List
            cmbSensors.Items.Clear();
            cmbSensors.Items.AddRange(ports);
            if( cmbSensors.Items.Contains(lastActiveSensorPort))cmbSensors.SelectedIndex = cmbSensors.Items.IndexOf(lastActiveSensorPort);
//            for (int i = 0; i < ports.Length; i++)
//            {
//                cmbSensors.Items.Add(ports[i]);
//                //if (ports[i] == activeSensorPort) cmbSensors.Text = activeSensorPort;
//                if (ports[i] == lastActiveSensorPort) cmbSensors.Text = lastActiveSensorPort;
//            }

            //2. Load current device list in Actuator List
            cmbActuators.Items.Clear();
            cmbActuators.Items.AddRange(ports);
            if( cmbActuators.Items.Contains(lastActiveActuatorPort))cmbActuators.SelectedIndex = cmbActuators.Items.IndexOf(lastActiveActuatorPort);
//            for (int i = 0; i < ports.Length; i++)
//            {
//                cmbActuators.Items.Add(ports[i]);
//                //if (ports[i] == activeActuatorPort) cmbActuators.Text = activeActuatorPort;
//                if (ports[i] == lastActiveActuatorPort) cmbActuators.Text = lastActiveActuatorPort;
//            }
        }


        delegate void DlgOneStringParam(string port);

        void Connect(string portName)
        {
            //1. Executed only in PPCOM thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new DlgOneStringParam(Connect), new object[] { portName });
                return;
            }

            //2. Executed only in Main Thread
            LoadPorts();
        }

        void Disconnect(string portName)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new DlgOneStringParam(Disconnect), new object[] { portName });
                return;
            }

            StopIO_Process();
            LoadPorts();
        }

        bool SUCCEEDED(int hr)
        {
            return hr >= 0;
        }

        void GetGuiState(out GUI_State guiState)
        {
            guiState = new GUI_State();
            GetActivePorts(out guiState.sensorPort, out guiState.actuatorPort);
            guiState.threadRunning = (ioThread != null) && (guiRunState == true);
        }

        #endregion Aux_Methods

        //------------------------------------------------------------------------------------------
        // Thread Operations
        //------------------------------------------------------------------------------------------
        #region IO_Operations

        int SetLampState(int state, out string strError)
        {
            int hr;

            byte[] data = new byte[] { 0x04, 0x01, (byte)((state == 0x00) ? 0x00 : 0xFF), 0x01 };
            hr = ppActuator.I2C_SendData(I2C_ADDR, data, out strError);
            Thread.Sleep(100);

            return hr;
        }

        void StopIO_Process()
        {
            guiRunState = false;

            if (ioThread != null)
            {
                ioThread.Join(200); //10 sec to give user time to react on I2C Error Msg box and close it

                if (ioThread.ThreadState != System.Threading.ThreadState.Stopped)
                    ioThread.Abort();
            }
        }

        void RunIO_Process(object parameters)
        {
            //--->>> Execute Run in separate Thread! <<<--------------
            //--->>> Also add error handling, if port is busy, or bad port name, etc <<<----------                       

            int hr;
            string strError;


            //1. OpenPorts
            ppActuator.ClosePort(out strError);
            ppSensor.ClosePort(out strError);

            threadParams threadParameters = (threadParams)parameters;

            if (threadParameters.actuatorPort == threadParameters.sensorPort)
            {
                MessageBox.Show("Actuator and Sensor Ports both have same Name!", "Failed to Start!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            hr = ppSensor.OpenPort(threadParameters.sensorPort, out strError);
            if (!SUCCEEDED(hr))
            {
                MessageBox.Show(strError, "Failed to Open Port!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            hr = ppActuator.OpenPort(threadParameters.actuatorPort, out strError);
            if (!SUCCEEDED(hr))
            {
                MessageBox.Show(strError, "Failed to Open Port!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //2. SetProtocol, Speed, Power
            ppActuator.SetProtocol(enumInterfaces.I2C, out strError);
            ppActuator.I2C_SetSpeed(enumI2Cspeed.CLK_100K, out strError);
            ppActuator.SetPowerVoltage("5.0", out strError);
            ppActuator.PowerOn(out strError);

            ppSensor.SetProtocol(enumInterfaces.I2C, out strError);
            ppSensor.I2C_SetSpeed(enumI2Cspeed.CLK_100K, out strError);
            ppSensor.SetPowerVoltage("5.0", out strError);
            ppSensor.PowerOn(out strError);

            //Fade off LEDs on Sensor Port
            ppSensor.I2C_SendData(I2C_ADDR, new byte[] { 0x04, 0x01, 0x0, 0x01 }, out strError);
            Thread.Sleep(150);
            ppSensor.I2C_SendData(I2C_ADDR, new byte[] { 0x04, 0x02, 0x0, 0x01 }, out strError);
            Thread.Sleep(150);

            ppActuator.I2C_SendData(I2C_ADDR, new byte[] { 0x04, 0x01, 0x0, 0x01 }, out strError);
            Thread.Sleep(150);
            ppActuator.I2C_SendData(I2C_ADDR, new byte[] { 0x04, 0x02, 0x10, 0x01 }, out strError);
            Thread.Sleep(150);

            //3. Run working loop
            int Illumination_Reference = 0x370;
            int Illumination_Delta = 0x10;

            guiRunState = true;
            while (guiRunState)
            {
                DoEvents(); //Experimental ! ! !

                //1. Read Sensors
                byte[] sensorData;
                int illumination, temperature;

                hr = ppSensor.I2C_ReadDataFromReg(I2C_ADDR, new byte[] { 0x00 }, 4, out sensorData, out strError);
                if (!SUCCEEDED(hr))
                {
                    MessageBox.Show(strError, "I2C Comm Failed with Sensor Port!!!", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }

                temperature = (sensorData[0] << 8) + sensorData[1];
                illumination = (sensorData[2] << 8) + sensorData[3];

                DECdata.DECtemperature = Convert.ToInt32(temperature);
                DECdata.DECillumination = Convert.ToInt32(illumination);

                // Set Status Lables
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    this.statusBart_Illumination.Content = "Illumination: " + DECdata.DECillumination.ToString();
                    this.statusBart_Temperature.Content = "Temperature: " + DECdata.DECtemperature.ToString();

                    //this.Chart.Series[0].Values.Add(
                    //    new IlluminationViewModel { Illumination = (double)DECdata.DECillumination, DateTime = DateTime.Now });
                    //this.Chart.Series[1].Values.Add((double)DECdata.DECillumination);
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        this.statusBart_Illumination.Content = "Illumination: " + DECdata.DECillumination.ToString()));
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        this.statusBart_Temperature.Content = "Temperature: " + DECdata.DECtemperature.ToString()));

                    //Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    //    this.Chart.Series[0].Values.Add(
                    //        new IlluminationViewModel { Illumination = (double)DECdata.DECillumination, DateTime = DateTime.Now })));
                    //Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    //    this.Chart.Series[1].Values.Add((double)DECdata.DECtemperature)));
                }

                //ChartPointer++;
                //if (ChartPointer>=100)
                //    if (Application.Current.Dispatcher.CheckAccess())
                //    {
                //        this.Chart.Series[0].Values.RemoveAt(0);
                //        //this.Chart.Series[1].Values.RemoveAt(0);
                //    }
                //    else
                //    {
                //        //Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                //        //this.Chart.Series[0].Values.RemoveAt(0)));
                //        //Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                //        //this.Chart.Series[1].Values.RemoveAt(0)));
                //    }
                

                //if (InvokeRequired)
                //    this.Invoke(new MethodInvoker(() =>
                //    {
                //        textBox_ConsoleOutput.AppendText("\r\n>>> Illum: " + illumination.ToString("X4") + "   Temp: " + temperature.ToString("X4"));
                //    }));

                // Use Debugger.Log only for debugging
                //Debugger.Log(0, "", "\r\n>>> Illum: " + illumination.ToString("X4") + "   Temp: " + temperature.ToString("X4"));

                //2. Act upon Sensor State
                if (illumination < (Illumination_Reference - Illumination_Delta))
                    hr = SetLampState(0, out strError);
                else
                    if (illumination > (Illumination_Reference + Illumination_Delta))
                    hr = SetLampState(1, out strError);
                if (!SUCCEEDED(hr))
                {
                    MessageBox.Show(strError, "I2C Comm Failed with Sensor Port!!!", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }

                //3. Display Current Data

            }
            guiRunState = false;

            //4. Thread Complete - shutdown LEDs on Actuator/Sensor
            ppActuator.PowerOff(out strError);
            ppSensor.PowerOff(out strError);
        }

        #endregion IO_Operations

        //------------------------------------------------------------------------------------------
        // GUI Event Handlers
        //------------------------------------------------------------------------------------------
        #region GUI_Events

        private void timer_ElementControllerStart()
        {
            timer = new DispatcherTimer();  // если надо, то в скобках указываем приоритет, например DispatcherPriority.Render
            timer.Tick += new EventHandler(timer_ElementControllerTick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            timer.Start();
        }

        private void timer_ElementControllerTick(object sender, EventArgs e)
        {
            
            //1. Get Current GUI State
            GUI_State guiStateNow;
            string DefaultText = "iTKerry's Sensor Tool :: ";

            GetGuiState(out guiStateNow);

            //2. Check if it was changed
            if (guiStateNow.Equals(guiStatePrev)) return;

            //3. Apply Changes to Controls
            btnRun.IsEnabled = (guiStateNow.actuatorPort != String.Empty) &&
                             (guiStateNow.sensorPort != String.Empty) &&
                             (guiStateNow.threadRunning == false);

            btnStop.IsEnabled = guiStateNow.threadRunning == true;

            cmbActuators.IsEnabled = !guiStateNow.threadRunning;
            cmbSensors.IsEnabled = !guiStateNow.threadRunning;

            //Read data from sensor

            //4. Set status labels            
            if (guiStateNow.threadRunning == true)
            {
                statusBar_RunningStatus.Content = "Running...";
                AppStyle_Running();

                //taskbarNotify.Text = DefaultText + "Running";
                this.Title = DefaultText + "Running";
                ChartTimer.Start();
            }
            else
            {
                statusBar_RunningStatus.Content = "Stopped...";
                AppStyle_Stopped();

                statusBart_Illumination.Content = "Illumination: -";
                statusBart_Temperature.Content = "Temperature: -";

                //taskbarNotify.Text = DefaultText + "Stopped";

                this.Title = DefaultText + "Stopped";
                ChartTimer.Stop();
            }

            //5. Remember current state
            guiStatePrev = guiStateNow;

            guiStateNow.threadRunning = false;

            TimerIndex++;
        }

        private void ChartTimerOnTick(object sender, EventArgs eventArgs)
        {
            if (Chart.Series[0].Values.Count > 30) Chart.Series[0].Values.RemoveAt(0);
            Chart.Series[0].Values.Add(new IlluminationViewModel
            {
                Illumination = DECdata.DECillumination,
                DateTime = DateTime.Now
            });
            if (Chart.Series[1].Values.Count > 30) Chart.Series[1].Values.RemoveAt(0);
            Chart.Series[1].Values.Add(new TemperatureViewModel
            {
                Temperature = DECdata.DECtemperature,
                DateTime = DateTime.Now
            });
        }

        public void AppStyle_Stopped()
        {
            ThemeManager.ChangeAppStyle(this,
                                        ThemeManager.GetAccent("Orange"),
                                        ThemeManager.GetAppTheme("BaseDark"));
        }

        public void AppStyle_Running()
        {
            ThemeManager.ChangeAppStyle(this,
                                        ThemeManager.GetAccent("Green"),
                                        ThemeManager.GetAppTheme("BaseDark"));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {           
            LoadPorts();

            timer_ElementControllerStart();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopIO_Process();
        }

        private void cmbSensors_SelectedIndexChanged(object sender, EventArgs e)
        {
            lastActiveSensorPort = cmbSensors.SelectedText;
        }

        private void cmbActuators_SelectedIndexChanged(object sender, EventArgs e)
        {
            lastActiveActuatorPort = cmbActuators.SelectedText;
        }

        private void btnRun_Click_1(object sender, RoutedEventArgs e)
        {
            threadParams data = new threadParams();
            GetActivePorts(out data.sensorPort, out data.actuatorPort);

            if ((ioThread != null) && (ioThread.ThreadState == ThreadState.Running))
                return;

            ioThread = new Thread(RunIO_Process);
            ioThread.Start(data);
        }

        private void btnStop_Click_1(object sender, RoutedEventArgs e)
        {
            StopIO_Process();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsFlyout.IsOpen = true;
        }

        public MainWindow()
        {
            InitializeComponent();

            ppActuator = new PP_ComLib_WrapperClass();
            ppSensor = new PP_ComLib_WrapperClass();
            DECdata = new threadDECdata();
            ChartTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };

            ppActuator.w_ConnectToLatest();
            ppSensor.w_ConnectToLatest();

            ppActuator.OnConnect += Connect;
            ppActuator.OnDisconnect += Disconnect;
            ChartTimer.Tick += ChartTimerOnTick;

            var IlluminationConfig = new SeriesConfiguration<IlluminationViewModel>().
                Y(model => model.Illumination).
                X(model => model.DateTime.ToOADate());

            var TemperatureConfig = new SeriesConfiguration<TemperatureViewModel>().
                Y(model => model.Temperature).
                X(model => model.DateTime.ToOADate());

            //now we create our series with this configuration
            ChartSeries = new SeriesCollection(IlluminationConfig)
            {
                new LineSeries {
                    Values = new ChartValues<IlluminationViewModel>(),
                    PointRadius = 0,
                    Title = "Illumination",
                    Fill = Brushes.Transparent },
                new LineSeries {
                    Values = new ChartValues<TemperatureViewModel>(),
                    PointRadius = 0,
                    Title = "Illumination",
                    Fill = Brushes.Transparent }
            };

            XFormatter = val => DateTime.FromOADate(val).ToString("hh:mm:ss tt");
            YFormatter = val => Math.Round(val) + "";

            DataContext = this;
        }
        #endregion GUI_Events

        // Expetimental ! ! ! Read this http://stackoverflow.com/questions/4502037/where-is-the-application-doevents-in-wpf
        public static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }

        // SplashScreen
        public async Task RunLongProcess()
        {
            await Task.Delay(1000);
        }
    }

    public class IlluminationViewModel
    {
        public double Illumination { get; set; }
        public DateTime DateTime { get; set; }
    }
    public class TemperatureViewModel
    {
        public double Temperature { get; set; }
        public DateTime DateTime { get; set; }
    }
}
