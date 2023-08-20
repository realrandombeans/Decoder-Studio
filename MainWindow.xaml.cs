using Microsoft.Win32;
using System;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StudioDecoder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isProcessing;
        private Dictionary<char, int> characterToBitMapping;
        public MainWindow()
        {
            InitializeComponent();
            logTextBox.AppendText("Welcome to Decoder Studio" +
                "" + Environment.NewLine);
            characterToBitMapping = new Dictionary<char, int>
            {
                {'@', 0},
                {'A', 1},
                {'B', 2},
                {'C', 3},
                {'D', 4},
                {'E', 5},
                {'F', 6},
                {'G', 7},
                {'H', 8},
                {'I', 9},
                {'J', 10},
                {'K', 11},
                {'L', 12},
                {'M', 13},
                {'N', 14},
                {'O', 15},
                {'P', 16},
                {'Q', 17},
                {'R', 18},
                {'S', 19},
                {'T', 20},
                {'U', 21},
                {'V', 22},
                {'W', 23},
                {'X', 24},
                {'Y', 25},
                {'Z', 26},
                {'[', 27},
                {System.IO.Path.DirectorySeparatorChar, 28},
                {']', 29},
                {'^', 30},
                {'_', 31}
            };
        }

        private void CommonCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void Button_ClickAsync(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Browse Studio C Data",
                DefaultExt = "CEC",
                Filter = "CEC Files (*.CEC)|*.CEC",
                Multiselect = false
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string filePath = openFileDialog.FileName;
                logTextBox.AppendText("Loading File - " + openFileDialog.FileName + Environment.NewLine);

                if (VideoCheckbox.IsChecked == true)
                {
                    OpenFileDialog openFileVideoDialog = new OpenFileDialog
                    {
                        Title = "Browse Videos",
                        DefaultExt = "mp4",
                        Filter = "Video Files|*.wmv;*.avi;*.mpeg;*.mp4;*.mkv;*.mov|Audio Files|*.mp3;*.wav|All Files|*.*",
                        Multiselect = false
                    };

                    bool? videoresult = openFileVideoDialog.ShowDialog();
                    if (videoresult == true)
                    {
                        logTextBox.AppendText("Loading Video - " + openFileVideoDialog.FileName + Environment.NewLine);
                        VideoPlayer.Source = new Uri(openFileVideoDialog.FileName, UriKind.Absolute);
                    }
                }
                await ProcessFileLiveAsync(filePath);
            }
        }
        private async Task ProcessFileLiveAsync(string filePath)
        {
            isProcessing = true;

            using (StreamReader reader = new StreamReader(filePath))
            {
                int lineNumber = 0;
                string line;

                while ((line = reader.ReadLine()) != null && isProcessing)
                {
                    lineNumber++;

                    // Ignore the first three characters
                    string frameData = line.Substring(3);

                    int frameCount = 0;
                    bool isNonFrameData = false;
                    string nonFrameData = "";

                    for (int i = 0; i < frameData.Length; i++)
                    {
                        if (frameData[i] == '!')
                        {
                            if (isNonFrameData)
                            {
                                ConvertNonFrameDataToBit(nonFrameData); // Convert non-frame data to bit
                                isNonFrameData = false;
                                nonFrameData = "";
                            }
                            frameCount++;
                        }
                        else
                        {
                            if (!isNonFrameData)
                            {
                                isNonFrameData = true;
                                nonFrameData = "";
                            }
                            nonFrameData += frameData[i];
                        }

                        // Delay for frame rate
                        double intervalInSeconds = 1.0 / 30.0;
                        await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds));
                    }

                    if (isNonFrameData)
                    {
                        ConvertNonFrameDataToBit(nonFrameData); // Convert non-frame data to bit
                    }
                }
            }

            isProcessing = false;
        }


        private void ConvertNonFrameDataToBit(string nonFrameData)
        {
            try
            {
                string logMessage = "";
                if (nonFrameData.Length >= 2)
                {
                    char character = nonFrameData[1];
                    if (characterToBitMapping.TryGetValue(character, out int bit))
                    {
                        if (nonFrameData[0] == '0' || nonFrameData[0] == '3')
                        {
                            logMessage = $"Bit value for '{character}' is: {bit} and has been disabled";
                            logTextBox.AppendText(logMessage + Environment.NewLine);
                        }
                        else if (nonFrameData[0] == '1' || nonFrameData[0] == '2')
                        {
                            logMessage = $"Bit value for '{character}' is: {bit} and has been enabled";
                            logTextBox.AppendText(logMessage + Environment.NewLine);
                        }
                    }
                    else
                    {
                        // Append the error message to the TextBox
                        logTextBox.AppendText($"No bit mapping found for character '{character}'." + Environment.NewLine);
                    }
                }
                if (nonFrameData.Contains("*E")) // Video Play
                {

                }
            }
            catch (Exception ex)
            {
                // Append the error message to the TextBox
                logTextBox.AppendText($"An error occurred: {ex.Message}" + Environment.NewLine);
            }
        }


        public void StopProcessing()
        {
            isProcessing = false;
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            logTextBox.AppendText("Paused Processing" + Environment.NewLine);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            logTextBox.AppendText("Unpaused Processing" + Environment.NewLine);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            StopProcessing();
            logTextBox.AppendText("Cancellled Processing" + Environment.NewLine);
        }
        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            logTextBox.AppendText("Forward 10 seconds" + Environment.NewLine);
        }
        private void About_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.Show();
        }
        private void Github_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/realrandombeans/Decoder-Studio");
        }
    }
}
