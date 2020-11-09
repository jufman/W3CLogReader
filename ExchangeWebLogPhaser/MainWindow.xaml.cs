using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace W3CLogReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadAsyncButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".log"
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;
                ErrorsTreeView.Items.Clear();
                OutputListView.Items.Clear();

                try
                {
                    LoadLogsAsync(filename);
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.ToString());
                }
            }
        }


        public async void LoadLogsAsync( string path)
        {
            ProgressProgressBar.IsIndeterminate = true;
            ProgressProgressBar.Visibility = Visibility.Visible;
            ErrorsTreeView.IsEnabled = false;

            var TotalTime = Stopwatch.StartNew();

            OutputListView.Items.Add("Starting Count");

            var time = Stopwatch.StartNew();

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                List<int> Parts = new List<int>();

                Parts = await TotalLines(path);

                OutputListView.Items.Add("Finished Count");
                OutputListView.Items.Add($"Count took: { time.Elapsed.ToString() }");

                time.Restart();
                OutputListView.Items.Add("Starting Procress");

                List<Task<List<Core.Objects.Log>>> worker = new List<Task<List<Core.Objects.Log>>>();

                int offset = 0;
                int start = 0;

                OutputListView.Items.Add($"Using { Parts.Count } Workers");

                foreach (int pos in Parts)
                {
                    int size = (pos) - offset;

                    worker.Add(
                        Task.Run(() =>
                        {
                            byte[] data = new byte[size];
                            fs.Read(data, start, size);

                            List<Core.Objects.Log> logs = ProcressBytes(data);

                            return logs;
                        }));

                    offset += size;
                }

                var res = await Task.WhenAll(worker);

                List<Core.Objects.GroupedLogs> GroupedLogs = new List<Core.Objects.GroupedLogs>();

                int count = 0;

                foreach (var item in res)
                {
                    count += item.Count;

                    item.ForEach(log =>
                    {
                        Core.Objects.GroupedLogs groupedLogs = GroupedLogs.Find(A => A.Username == log.Username);

                        if (groupedLogs != null)
                        {
                            groupedLogs.Logs.Add(log);
                        }
                        else
                        {
                            GroupedLogs.Add(new Core.Objects.GroupedLogs()
                            {
                                Username = log.Username,
                                Logs = { log }
                            });
                        }
                    });
                }

                GroupedLogs.ForEach(item =>
                {
                    TreeViewItem viewItem = new TreeViewItem();
                    viewItem.Header = item.Username;
                    viewItem.IsExpanded = true;

                    item.Logs.OrderByDescending(a => a.DateTime).ToList().ForEach(Lockout =>
                    {
                        viewItem.Items.Add(Lockout.GetDetails());
                    });

                    ErrorsTreeView.Items.Add(viewItem);
                });

                OutputListView.Items.Add("Completed Procress");
                time.Stop();
                OutputListView.Items.Add($"Count took: { time.Elapsed.ToString() }");

                OutputListView.Items.Add($"Total Number Of Items: { count }");
            }

            OutputListView.Items.Add($"Total Time: { TotalTime.Elapsed.ToString() }");

            ProgressProgressBar.Visibility = Visibility.Hidden;
            ErrorsTreeView.IsEnabled = true;
        }

        private void ProcressLogs()
        {

        }

        public List<Core.Objects.Log> ProcressBytes(byte[] data)
        {
            List<Core.Objects.Log> Logs = new List<Core.Objects.Log>();

            using (StreamReader streamReader = new StreamReader(new MemoryStream(data)))
            {
                string line;

                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] lineParts = line.Split(' ');

                    if (lineParts.Length != 15)
                    {
                        continue;
                    }

                    /*
                    if (lineParts[11] != "401" || lineParts[7] == "-")
                    {
                        continue;
                    }
                    */

                    if (lineParts[11] == "200" || lineParts[7] == "-" || lineParts[11] == "500" || lineParts[11] == "302" || lineParts[8].StartsWith("10."))
                    {
                        continue;
                    }

                    string username = lineParts[7].ToLower();

                    Logs.Add(new Core.Objects.Log()
                    {
                        DateTime = DateTime.Parse($"{ lineParts[0] } { lineParts[1] }Z"),
                        Method = lineParts[3],
                        Uri = lineParts[4],
                        UriQuery = lineParts[5],
                        Port = lineParts[6],
                        Username = username,
                        ClientIp = lineParts[8],
                        UserAgent = lineParts[9],
                        Status = lineParts[11],
                    });
                }
            }

            return Logs;
        }

        private async Task<List<int>> TotalLines(string Path)
        {
            List<int> Points = new List<int>();

            List<Task> worker = new List<Task>();

            int bufferSize = 100000000;
            int LineSplit = 50000;

            long LineSplitCount = 0;

            int totalSize = 0;

            int CurrentByte = 0;

            int offset = 0;

            int totalLines = 0;

            using (FileStream fs = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize))
            {
                long bytesToRead = fs.Length;
                totalSize = Convert.ToInt32(fs.Length);

                List<int> BytePoints = GetPoints(fs.Length, bufferSize);

                OutputListView.Items.Add($"Using { BytePoints.Count } Workers");

                foreach (int point in BytePoints)
                {
                    try
                    {
                        int size = (point) - offset;
                        int bytesRead = 0;

                        worker.Add(Task.Run(() =>
                        {
                            byte[] buffer = new byte[size];

                            bytesRead = fs.Read(buffer, 0, size);
                            for (int i = 0; bytesRead > i; i++)
                            {
                                if (buffer[i] == '\n')
                                {
                                    totalLines++;
                                    LineSplitCount++;
                                    if (LineSplitCount == LineSplit)
                                    {
                                        Points.Add(CurrentByte);
                                        LineSplitCount = 0;
                                    }
                                }
                                i++;
                                CurrentByte++;
                            }

                            bytesToRead -= bytesRead;
                        }));

                        offset += size;
                    }
                    catch (Exception e)
                    {
                        e.ToString();
                    }
                }
                await Task.WhenAll(worker);
            }

            Points.Add(totalSize);

            OutputListView.Items.Add($"Total Lines Read: { String.Format("{0:n0}", totalLines) }");

            return Points;
        }

        private List<int> GetPoints(long total, int splitSize)
        {
            List<int> Points = new List<int>();

            int totalSize = Convert.ToInt32(total);
            int CurrentSize = 0;

            while (totalSize > splitSize)
            {
                CurrentSize += splitSize;
                Points.Add(CurrentSize);

                totalSize -= splitSize;
            }

            Points.Add(Convert.ToInt32(total));

            return Points;
        }
        
    }
}
