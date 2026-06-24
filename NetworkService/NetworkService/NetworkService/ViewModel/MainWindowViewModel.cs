using NetworkService.Helpers;
using NetworkService.Model;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace NetworkService.ViewModel
{
    public class MainWindowViewModel : BindableBase
    {
        public MyICommand<string> NavCommand { get; private set; }

        private BindableBase _currentViewModel;
        public BindableBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public static ObservableCollection<DerEntity> AllEntities { get; set; }
            = new ObservableCollection<DerEntity>();

        public static NetworkEntitiesViewModel EntitiesVM { get; private set; }
        public static NetworkDisplayViewModel DisplayVM { get; private set; }
        public static MeasurementGraphViewModel GraphVM { get; private set; }

        public static Action<string, string> ShowToast { get; set; }

        private Thread _listenerThread;

        public MainWindowViewModel()
        {
            EntitiesVM = new NetworkEntitiesViewModel();
            DisplayVM = new NetworkDisplayViewModel();
            GraphVM = new MeasurementGraphViewModel();

            NavCommand = new MyICommand<string>(OnNav);

            CurrentViewModel = EntitiesVM;

            StartTcpListener();
        }

        private void OnNav(string destination)
        {
            switch (destination)
            {
                case "entities":
                    CurrentViewModel = EntitiesVM;
                    break;
                case "graph":
                    CurrentViewModel = GraphVM;
                    break;
            }
        }

        private void StartTcpListener()
        {
            _listenerThread = new Thread(() =>
            {
                TcpListener server = null;
                try
                {
                    server = new TcpListener(IPAddress.Any, 25675);
                    server.Start();

                    while (true)
                    {
                        TcpClient client = server.AcceptTcpClient();
                        NetworkStream stream = client.GetStream();
                        byte[] buffer = new byte[1024];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string message = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);

                        if (message == "Need object count")
                        {
                            int count = 0;
                            Application.Current.Dispatcher.Invoke(() =>
                                count = AllEntities.Count);

                            byte[] response = System.Text.Encoding.ASCII.GetBytes(count.ToString());
                            stream.Write(response, 0, response.Length);
                        }
                        else if (message.StartsWith("Entitet_"))
                        {
                            ParseAndUpdateEntity(message);
                        }

                        stream.Close();
                        client.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("TCP Listener error: " + ex.Message);
                }
                finally
                {
                    server?.Stop();
                }
            });

            _listenerThread.IsBackground = true;
            _listenerThread.Start();
        }

        private void ParseAndUpdateEntity(string message)
        {
            try
            {
                string[] parts = message.Split(':');
                int entityIndex = int.Parse(parts[0].Replace("Entitet_", ""));
                double value = double.Parse(parts[1],
                    System.Globalization.CultureInfo.InvariantCulture);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (entityIndex < AllEntities.Count)
                    {
                        DerEntity entity = AllEntities[entityIndex];
                        entity.CurrentValue = value;

                        WriteToLog(entity, value);

                        GraphVM.OnNewMeasurement(entity, value);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Parse error: " + ex.Message);
            }
        }

        private void WriteToLog(DerEntity entity, double value)
        {
            try
            {
                string logPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Log.txt");
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | " +
                              $"ID:{entity.Id} | {entity.Name} | {value:F2} MW";
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Log error: " + ex.Message);
            }
        }
        public static void RestartMeteringSimulator()
        {
            try
            {
                foreach (var proc in Process.GetProcessesByName("MeteringSimulator"))
                    proc.Kill();

                Thread.Sleep(500);

                string simulatorPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "MeteringSimulator.exe");
                if (File.Exists(simulatorPath))
                    Process.Start(simulatorPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Simulator restart error: " + ex.Message);
            }
        }
    }
}