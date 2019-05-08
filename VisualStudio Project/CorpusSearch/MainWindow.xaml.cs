using System;
using System.Text;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;

namespace CorpusSearch
{
    public partial class MainWindow : Window
    {

        BackgroundWorker worker;
        ManualResetEvent _pauseEvent = new ManualResetEvent(true);
        string search = "";
        bool isLoaded = false;
        string[] OpenedFiles;
        Dictionary<string, string> Results = new Dictionary<string, string>();
        Stopwatch watch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();
            isLoaded = true;
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        // ========== Backgroun worker
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            watch.Start();
            if (OpenedFiles != null)
            {
                var FoundCount = 0;
                foreach (var file in OpenedFiles)
                {
                    using (StreamReader reader = new StreamReader(file))
                    {
                        var LineNo = 0;
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            LineNo++;
                            if (Regex.IsMatch(line, search))
                            {
                                Results.Add(Path.GetFileNameWithoutExtension(file) + ":" + LineNo, line);
                                FoundCount++;
                                (sender as BackgroundWorker).ReportProgress(FoundCount, Path.GetFileNameWithoutExtension(file) + ":" + LineNo);
                            }
                        }
                    }
                }
                watch.Stop();
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
                lstResults.SelectedIndex = 0;
            lblResults.Content = e.ProgressPercentage + " Results";
            lstResults.Items.Add((string)e.UserState);
        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (Results.Count == 0)
                lblResults.Content = "Not Found";
            lblResults.Content += " (in " + watch.Elapsed.TotalSeconds.ToString("0.00") + " sec)";
            watch.Reset();
            lblStatus.Background = Brushes.AliceBlue;
            btnExport.Visibility = Visibility.Visible;
        }

        // 
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (!worker.IsBusy)
            {
                if (OpenedFiles != null)
                {
                    Results.Clear();
                    btnExport.Visibility = Visibility.Collapsed;
                    lblResults.Content = "Searching...";
                    lblStatus.Background = Brushes.Orange;
                    if (lstResults.Items.Count > 0)
                        lstResults.Items.Clear();
                    search = txtSearch.Text;
                    worker.RunWorkerAsync();
                }
                else
                    MessageBox.Show("No Text Corpora file selected! Click Browse button.");
            }
            else
            {
                worker.CancelAsync();
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*";
            OFD.Multiselect = true;
            if (OFD.ShowDialog() == true)
            {
                btnExport.Visibility = Visibility.Collapsed;
                OpenedFiles = OFD.FileNames;
                lstResults.Items.Clear();
                rtbResult.Document.Blocks.Clear();
                lblResults.Content = "";
                txtFileName.Text = "";
                foreach (var file in OpenedFiles)
                    txtFileName.Text += file + "\r\n";
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            if (lstResults.Items.Count > 0)
                lstResults.Items.Clear();
            rtbResult.Document.Blocks.Clear();
            btnExport.Visibility = Visibility.Collapsed;
            lblResults.Content = "";
            if (txtFileName.Text != "")
            {
                txtFileName.Text = "";
                Array.Clear(OpenedFiles, 0, OpenedFiles.Length);
            }
        }

        private void lstResults_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            rtbResult.Document.Blocks.Clear();
            if (lstResults.Items.Count > 0)
            {
                var Text = Results[lstResults.SelectedItem.ToString()];
                var parag = new Paragraph();
                var matches = Regex.Matches(Text, txtSearch.Text);
                var index = 0;
                foreach (Match match in matches)
                {
                    parag.Inlines.Add(new Run(Text.Substring(index, match.Index - index)));
                    var r2 = new Run(match.ToString());
                    r2.Background = Brushes.Red;
                    r2.Foreground = Brushes.White;
                    parag.Inlines.Add(r2);
                    index = match.Index + match.Length;
                }
                parag.Inlines.Add(new Run(Text.Substring(index)));
                rtbResult.Document.Blocks.Add(parag);
            }
        }
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (Results.Count > 0)
            {
                var Output = new StringBuilder();
                foreach (var item in Results)
                    Output.Append(item.Value + "\n");
                SaveFileDialog SFD = new SaveFileDialog();
                SFD.Filter = "Text file (*.txt)|*.txt";
                if (SFD.ShowDialog() == true)
                    File.WriteAllText(SFD.FileName, Output.ToString());
            }
            else
                MessageBox.Show("No results for export!");
        }

        private void chkRTL_Checked(object sender, RoutedEventArgs e)
        {
            if (isLoaded)
            {
                if (chkRTL.IsChecked == false)
                {
                    txtSearch.Text = "words";
                    rtbResult.FlowDirection = FlowDirection.LeftToRight;
                }
                else
                {
                    txtSearch.Text = "وشەکان";
                    rtbResult.FlowDirection = FlowDirection.RightToLeft;
                }
            }
        }
    }
}
