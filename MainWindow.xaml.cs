using Microsoft.Win32;
using System;
using System.Windows;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            logTextBox.AppendText("Welcome to StudioDecoder" + Environment.NewLine);
            characterToBitMapping = new Dictionary<char, int>
            {
                {'@', 1},
                {'A', 2},
                {'B', 3},
                {'C', 4},
                {'D', 5},
                {'E', 6},
                {'F', 7},
                {'G', 8},
                {'H', 9},
                {'I', 10},
                {'J', 11},
                {'K', 12},
                {'L', 13},
                {'M', 14},
                {'N', 15},
                {'O', 16},
                {'P', 17},
                {'Q', 18},
                {'R', 19},
                {'S', 20},
                {'T', 21},
                {'U', 22},
                {'V', 23},
                {'W', 24},
                {'X', 25},
                {'Y', 26},
                {'Z', 27},
                {'[', 28},
                {System.IO.Path.DirectorySeparatorChar, 29},
                {']', 30},
                {'^', 31},
                {'_', 32}
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

                if (videoBool.IsChecked == true)
                {
                    OpenFileDialog openFileVideoDialog = new OpenFileDialog
                    {
                        Title = "Browse Studio C Data",
                        DefaultExt = "mp4",
                        Filter = "Video Files|*.wmv;*.avi;*.mpeg;*.mp4;*.mkv;*.mov|Audio Files|*.mp3;*.wav|All Files|*.*",
                        Multiselect = false
                    };

                    bool? videoresult = openFileVideoDialog.ShowDialog();
                    if (videoresult == true)
                    {
                        logTextBox.AppendText("Loading Video - " + openFileVideoDialog.FileName + Environment.NewLine);

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
                                // Convert non-frame data to bit for all bits collected
                                ConvertNonFrameDataToBit(nonFrameData);
                                isNonFrameData = false;
                                nonFrameData = "";
                            }
                            frameCount++;
                        }
                        else if (frameData[i] == '*')
                        {
                            // Extract command between '*' and '\'
                            string command = "";
                            i++; // Move past the '*'
                            while (i < frameData.Length && frameData[i] != '\\')
                            {
                                command += frameData[i];
                                i++;
                            }

                            // Determine and log command type
                            LogCommandType(command);
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

                        // Check for additional bits within the same frame
                        if (isNonFrameData && nonFrameData.Length >= 2 && i + 1 < frameData.Length && frameData[i + 1] != '!')
                        {
                            ConvertNonFrameDataToBit(nonFrameData); // Convert non-frame data to bit
                            isNonFrameData = false;
                            nonFrameData = "";
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

        private void LogCommandType(string command)
        {
            if (command.StartsWith("A") && command.Contains("V"))
            {
                // Analog Command
                string[] parts = command.Split('V');
                if (int.TryParse(parts[0].Substring(1), out int channel) && int.TryParse(parts[1], out int value))
                {
                    string logMessage = $"Analog Command. Channel {channel}, Value {value}.";
                    logTextBox.AppendText(logMessage + Environment.NewLine);
                }
                else
                {
                    logTextBox.AppendText($"Unknown command: {command}" + Environment.NewLine);
                }
            }
            else if (command == "E")
            {
                // Showtape Start Command
                string logMessage = "Showtape Start";
                logTextBox.AppendText(logMessage + Environment.NewLine);
            }
            else if (command.StartsWith("M201") && command.Contains("V"))
            {
                // Disk Command
                string[] parts = command.Split('V');
                if (int.TryParse(parts[1], out int disk) && int.TryParse(parts[2], out int timestamp1) && int.TryParse(parts[3], out int timestamp2))
                {
                    string logMessage = $"Disk Command. Disk {disk}, Time {timestamp1}{timestamp2}.";
                    logTextBox.AppendText(logMessage + Environment.NewLine);
                }
                else
                {
                    logTextBox.AppendText($"Unknown command: {command}" + Environment.NewLine);
                }
            }
            else if (command.StartsWith("M211") && command.Contains("V"))
            {
                // Video Command
                string[] parts = command.Split('V');
                if (int.TryParse(parts[1], out int disk) && int.TryParse(parts[2], out int output))
                {
                    if (disk > 3)
                    {
                        string outputName = GetVideoOutputName(output);
                        string logMessage = $"Video Command Unknown displaying to {outputName}.";
                        logTextBox.AppendText(logMessage + Environment.NewLine);
                    }
                    else
                    {
                        string outputName = GetVideoOutputName(output);
                        string logMessage = $"Video Command. Disk {disk}, Output {outputName}.";
                        logTextBox.AppendText(logMessage + Environment.NewLine);
                    }
                }
                else
                {
                    logTextBox.AppendText($"Unknown command: {command}" + Environment.NewLine);
                }
            }
            else if (command.StartsWith("M212") && command.Contains("V"))
            {
                // Audio Command
                string[] parts = command.Split('V');
                if (int.TryParse(parts[1], out int output) && int.TryParse(parts[2], out int info))
                {
                    string logMessage = GetAudioCommandMessage(output, info);
                    logTextBox.AppendText(logMessage + Environment.NewLine);
                }
                else
                {
                    logTextBox.AppendText($"Unknown command: {command}" + Environment.NewLine);
                }
            }
            else if (command.StartsWith("M206") && command.Contains("V"))
            {
                // Fade Command
                string[] parts = command.Split('V');
                if (int.TryParse(parts[1], out int disk) && int.TryParse(parts[2], out int fadeToValue) && int.TryParse(parts[3], out int rate))
                {
                    string logMessage = $"Fade Disk {disk} to {fadeToValue} at Rate: {rate}.";
                    logTextBox.AppendText(logMessage + Environment.NewLine);
                }
                else
                {
                    logTextBox.AppendText($"Unknown command: {command}" + Environment.NewLine);
                }
            }
            else
            {
                logTextBox.AppendText($"Unknown command: {command}" + Environment.NewLine);
            }
        }


        private string GetAudioCommandMessage(int output, int info)
        {
            if (output >= 1 && output <= 7)
            {
                string outputName = GetAudioOutputName(output);
                if (info == 1)
                {
                    return $"Disk Audio to {outputName}.";
                }
                else if (info == 2)
                {
                    return $"Disk Audio to {outputName}.";
                }
                else if (info == 3)
                {
                    return $"Disk Audio to {outputName}.";
                }
                else
                {
                    return "Unknown command";
                }
            }
            else if (output == 16)
            {
                string infoText = GetAudioInfoText(info);
                return $"Disk 1 Audio to {infoText}.";
            }
            else
            {
                return "Unknown command";
            }
        }

        private string GetAudioOutputName(int output)
        {
            switch (output)
            {
                case 1: return "Center monitor Left Channel DVD Player 1";
                case 2: return "Center monitor Left Channel DVD Player 2";
                case 3: return "Center monitor Left Channel DVD Player 3";
                case 5: return "Center monitor Right Channel DVD Player 1";
                case 6: return "Center monitor Right Channel DVD Player 2";
                case 7: return "Center monitor Right Channel DVD Player 3";
                case 16: return "Disk 1";
                default: return "Unknown Output";
            }
        }

        private string GetAudioInfoText(int info)
        {
            switch (info)
            {
                case 1: return "Gameroom";
                case 2: return "ON HOLD";
                case 3: return "Karaoke";
                default: return "Unknown Info";
            }
        }

        private string GetVideoOutputName(int output)
        {
            switch (output)
            {
                case 1: return "Center Monitor";
                case 2: return "Left Monitor";
                case 3: return "Right Monitor";
                case 4: return "Apple Monitor";
                case 5: return "Ceiling Monitors";
                case 6: return "Games Monitors";
                case 7: return "Kiddie Monitor";
                case 16: return "Bluescreen";
                default: return "Unknown Output";
            }
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
                        if (nonFrameData[0] == '0')
                        {
                            logMessage = $"Bit value for '{character}' is: {bit} on DTU 1 and has been disabled";
                            logTextBox.AppendText(logMessage + Environment.NewLine);
                        }
                        else if (nonFrameData[0] == '1')
                        {
                            logMessage = $"Bit value for '{character}' is: {bit} on DTU 1 and has been enabled";
                            logTextBox.AppendText(logMessage + Environment.NewLine);
                        }
                        if (nonFrameData[0] == '2')
                        {
                            logMessage = $"Bit value for '{character}' is: {bit + 32} on DTU 1 and has been disabled";
                            logTextBox.AppendText(logMessage + Environment.NewLine);
                        }
                        else if (nonFrameData[0] == '3')
                        {
                            logMessage = $"Bit value for '{character}' is: {bit + 32} on DTU 1 and has been enabled";
                            logTextBox.AppendText(logMessage + Environment.NewLine);
                        }
                        if (nonFrameData[0] == '4')
                        {
                            logMessage = $"Bit value for '{character}' is: {bit} on DTU 2 and has been disabled";
                            logTextBox.AppendText(logMessage + Environment.NewLine);
                        }
                        else if (nonFrameData[0] == '5')
                        {
                            logMessage = $"Bit value for '{character}' is: {bit} on DTU 2 and has been enabled";
                            logTextBox.AppendText(logMessage + Environment.NewLine);
                        }
                        if (nonFrameData[0] == '6')
                        {
                            logMessage = $"Bit value for '{character}' is: {bit + 32} on DTU 2 and has been disabled";
                            logTextBox.AppendText(logMessage + Environment.NewLine);
                        }
                        else if (nonFrameData[0] == '7')
                        {
                            logMessage = $"Bit value for '{character}' is: {bit + 32} on DTU 2 and has been enabled";
                            logTextBox.AppendText(logMessage + Environment.NewLine);
                        }
                    }
                    else
                    {
                        // Append the error message to the TextBox
                        logTextBox.AppendText($"No bit mapping found for character '{character}'." + Environment.NewLine);
                    }
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
    }
}
