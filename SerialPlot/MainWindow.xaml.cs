using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

//omat
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Controls.Primitives; //Selector.SelectedItem
using System.Data;
using System.Windows.Forms.DataVisualization.Charting;
using Microsoft.Win32;
using SaveLoadNS;
using System.IO.Ports;
using System.Reflection; //PropertyInfo
using System.Threading;

namespace SaveLoadNS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EventWaitHandle COMReadLine;
        private Chart mainChart;
        private SerialPort serialPort;
        private delegate void newComDataEventHandler(string text);
        private DispatcherTimer timer;

        /// <summary>
        /// List of available handshakes
        /// </summary>
        private List<string> Handshakes
        {
            get
            {
                //list to store colors
                List<string> list = new List<string>();

                //add all colors to list
                foreach (string enumerated in typeof(Handshake).GetEnumNames())
                    list.Add(enumerated);

                //return result
                return list;
            }
        }

        /// <summary>
        /// List of available Paritys
        /// </summary>
        private List<string> Paritys
        {
            get
            {
                //list to store colors
                List<string> list = new List<string>();

                //add all colors to list
                foreach (string enumerated in typeof(Parity).GetEnumNames())
                    list.Add(enumerated);

                //return result
                return list;
            }
        }

        /// <summary>
        /// List of available Stopbits
        /// </summary>
        private List<string> Stopbits
        {
            get
            {
                //list to store colors
                List<string> list = new List<string>();

                //add all colors to list
                foreach (string enumerated in typeof(StopBits).GetEnumNames())
                    list.Add(enumerated);

                //return result
                return list;
            }
        }




        public MainWindow()
        {
            InitializeComponent(); 
        }

        /// <summary>
        /// This function is triggered right after when main window is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainLoaded(object sender, RoutedEventArgs e)
        {
            initDefaults();
            initUpdateTimer();
            COMReadLine = new EventWaitHandle(false, EventResetMode.ManualReset);
            //simulate COM with dispatcher timer event
            //initSimulatedCOM();
            
        }

        /// <summary>
        /// Default settings to gui objects example COM list, parity list, stopbits list, etc...
        /// </summary>
        private void initDefaults()
        {
            //create Chart object
            mainChart = new Chart();

            //put the chart inside the forms host
            windowsFormsHost1.Child = mainChart;

            //setup COM ports
            comboBoxCOM.Items.Clear();
            comboBoxCOM.ItemsSource = SerialPort.GetPortNames();
            comboBoxCOM.SelectedIndex = 0;

            //setup dropdowns
            comboBoxParity.Items.Clear();
            comboBoxParity.ItemsSource = Paritys;
            comboBoxParity.SelectedIndex = 0;
            
            comboBoxStopBits.Items.Clear();
            comboBoxStopBits.ItemsSource = Stopbits;
            comboBoxStopBits.SelectedIndex = 1;

            comboBoxHandshake.Items.Clear();
            comboBoxHandshake.ItemsSource = Handshakes;
            comboBoxHandshake.SelectedIndex = 0;
        }

        #region Signals to plot
        /// <summary>
        /// Use this to delete signal from plot
        /// </summary>
        /// <param name="signal"></param>
        private void deleteSignalFromPlot(Signal signal)
        {
            //remove title
            if (mainChart.Titles.FindByName(signal.title.Name) != null)
                mainChart.Titles.Remove(signal.title);
            else
                Debug.WriteLine("deletedSignalFromPlot: tried to remove non existent item" + signal.title.Name, "ERROR");

            //remove series
            if (mainChart.Series.FindByName(signal.series.Name) != null)
                mainChart.Series.Remove(signal.series);
            else
                Debug.WriteLine("deletedSignalFromPlot: tried to remove non existent item" + signal.series.Name, "ERROR");

            //remove chart area
            if (mainChart.ChartAreas.FindByName(signal.chartArea.Name) != null)
                mainChart.ChartAreas.Remove(signal.chartArea);
            else
                Debug.WriteLine("deletedSignalFromPlot: tried to remove non existent item" + signal.chartArea.Name, "ERROR");
        }


        /// <summary>
        /// Use this function to add signal to plot
        /// </summary>
        /// <param name="signal"></param>
        private void setSignalToPlot(Signal signal)
        {
            //find if there is signal named that
            if (mainChart.ChartAreas.FindByName(signal.chartArea.Name) == null) //true = not found
            {
                mainChart.ChartAreas.Add(signal.chartArea);
                mainChart.Series.Add(signal.series);
                mainChart.Series[mainChart.Series.Count - 1].ChartArea = signal.chartArea.Name;
                mainChart.Titles.Add(signal.title);
                mainChart.Titles[mainChart.Titles.Count - 1].DockedToChartArea = signal.chartArea.Name;
            }
        }
        

        /// <summary>
        /// Run this function when selection changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBoxSignal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //go trough all signals and set selected signals to plot
            foreach (Signal sig in listBoxSignals.Items)
            {
                if (listBoxSignals.SelectedItems.Contains(sig))
                    setSignalToPlot(sig); //item is on list => add to plot
                else
                    deleteSignalFromPlot(sig); //item isnt on the list => remove from plot
            }

            //disable save if nothing is selected
            if (listBoxSignals.SelectedItems.Count == 0)
            {
                menuButtonExport.IsEnabled = false;
                menuButtonSave.IsEnabled = false;
            }
            else
            {
                menuButtonExport.IsEnabled = true;
                menuButtonSave.IsEnabled = true;
            }
        }
        #endregion

        #region Adding and editing signals
        /// <summary>
        /// This function opens specify signal window so user can add signal
        /// </summary>
        /// <returns></returns>
        private Signal querySignal()
        {
            signalWindow signalDialog = new signalWindow(false);
            signalDialog.ShowDialog();
            return signalDialog.resultSignal;
        }
       

        /// <summary>
        /// This function is called when signal is to be edited
        /// </summary>
        /// <param name="sig"></param>
        /// <returns></returns>
        private Signal editSignal(Signal sig)
        {
            //start dialog
            signalWindow signalDialog = new signalWindow(sig);
            signalDialog.ShowDialog();

            if (signalDialog.resultSignal != null) //something was changed
            {
                //copy titles from "created" signal
                sig.title.Text = signalDialog.resultSignal.title.Text;
                sig.chartArea.AxisX.Title = signalDialog.resultSignal.chartArea.AxisX.Title;
                sig.chartArea.AxisY.Title = signalDialog.resultSignal.chartArea.AxisY.Title;

                //series
                sig.lineColor = signalDialog.resultSignal.lineColor;

                //update regexps should be ok because they are checked while creating new signal
                sig.updateRegExpMatch(signalDialog.resultSignal.regExpMatchString);
                sig.updateRegExpParse(signalDialog.resultSignal.regExpParseString);
            }
            return sig;
        }


        /// <summary>
        /// Go trough all selected signals and call edit function on them
        /// </summary>
        private void editSignals()
        {
            //start dialog
            signalWindow signalDialog = new signalWindow(true);
            signalDialog.ShowDialog();

            if (signalDialog.resultSignal != null) //something was changed
            {
                foreach (Signal sig in listBoxSignals.SelectedItems)
                {
                    //update only if not *
                    if (signalDialog.resultSignal.title.Text != "*")
                        sig.title.Text = signalDialog.resultSignal.title.Text;
                    if (signalDialog.resultSignal.chartArea.AxisX.Title != "*")
                        sig.chartArea.AxisX.Title = signalDialog.resultSignal.chartArea.AxisX.Title;
                    if (signalDialog.resultSignal.chartArea.AxisY.Title != "*")
                        sig.chartArea.AxisY.Title = signalDialog.resultSignal.chartArea.AxisY.Title;
                    if (signalDialog.resultSignal.lineColor != "")
                        sig.lineColor = signalDialog.resultSignal.lineColor;
                }
                //refresh items
                listBoxSignals.Items.Refresh();
            }
        }
        #endregion

        #region incoming data manipulation
        /// <summary>
        /// This function is called for every new line from COM or simulated COM
        /// </summary>
        /// <param name="data"></param>
        private void newData(string data)
        {
            //glue new data to old data at raw data textbox
            int textBoxRawBufferSize = 1000;

            textBoxRaw.AppendText("\n" + data); //append data

            if (textBoxRaw.Text.Length > textBoxRawBufferSize) //remove overleft
            {
                textBoxRaw.Clear();
                //Debug.WriteLine("cut! " + textBoxRaw.Text.Length);
                //textBoxRaw.Text = textBoxRaw.Text.Substring(textBoxRaw.Text.Length - 1 - textBoxRawBufferSize, textBoxRawBufferSize);//textBoxRaw.Text.Substring(textBoxRaw.Text.Length - textBoxRawBufferSize, textBoxRaw.Text.Length - 1);
                
            }
            
            textBoxRaw.ScrollToEnd(); //scroll to end

            //loop trough all signals
            foreach (Signal sig in listBoxSignals.Items)
            {
                //check if signal matches
                if (sig.checkMatch(data))
                {
                    //parse the value
                    try
                    {
                        double value = Double.Parse(sig.parseData(data));
                        if (data.Length > 0)
                        {
                            //increase element index
                            sig.seriesTemp.index++;

                            //if element index > size increase size
                            if (sig.seriesTemp.index == sig.seriesTemp.buffer.Length)
                            {
                                Array.Resize<double>(ref sig.seriesTemp.buffer, sig.seriesTemp.buffer.Length + 100); //increase by 100
                                Debug.WriteLine("Buffer extended for " + sig.title + "new buffer size = " + sig.seriesTemp.buffer.Length,"WARNING");
                            }

                            //store value
                            sig.seriesTemp.buffer[sig.seriesTemp.index] = value;
                        }
                    }
                    catch
                    {
                        Debug.WriteLine("newData: error with parsing", "ERROR");
                    }
                }
            }
        }





        private void newComData(object sender, EventArgs e)
        {
            string receivedText;

            try
            {
                //read new data
                if (serialPort.IsOpen)
                {
                    COMReadLine.Reset(); //disable access to serialPort
                    receivedText = serialPort.ReadLine();
                    COMReadLine.Set(); //allow access to serialPort i.e. serialPort.Close();
                    Thread.Sleep(0);//release execution for waiting close

                    //invoke newData asynchronously from mainthread to add the data
                    Dispatcher.BeginInvoke(DispatcherPriority.Send, new newComDataEventHandler(newData), receivedText);
                }
                
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Timeout error occured on ReadLine() function. Is newline string correct?");
                statusBarItem1.Content = "Not connected";
                serialPort.Close(); //prevent serial port from receiving new error
            }
        }

        /// <summary> Update Timer
        /// starts timer to update plot
        /// </summary>
        private void initUpdateTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(20);
            timer.Tick += new EventHandler(timerTick);
            timer.Start();
        }

        /// <summary> timerTick
        /// Create something to send to the newData() function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerTick(object sender, EventArgs e)
        {
            //loop trough all signals
            foreach (Signal sig in listBoxSignals.Items)
            {
                //check if signal matches
                if (sig.seriesTemp.index != -1)
                {
                    //temp array to hold all meaningful data
                    double[] tempData = new double[sig.seriesTemp.index+1];

                    //copy data to temp
                    Array.Copy(sig.seriesTemp.buffer, tempData, sig.seriesTemp.index+1);
                    sig.seriesTemp.index = -1; //empty

                    //add points to the plot
                    sig.series.Points.Add(tempData);
                }
            }
        }
        #endregion

        #region Buttons
        /// <summary>
        /// Event for add button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAddSignal_Click(object sender, RoutedEventArgs e)
        {
            //query new signal
            Signal tempSig = querySignal();

            //if everything went by plan add it
            if (tempSig != null) //not cancelled => add
                listBoxSignals.Items.Add(tempSig);
        }

        /// <summary>
        /// Event for edit button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonEditSignal_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxSignals.SelectedItems.Count == 1) //only one selected => edit one
            {
                int selectedIndex = listBoxSignals.SelectedIndex;
                Signal editedSignal = editSignal((Signal)listBoxSignals.Items[selectedIndex]);

                //dont edit if canceled
                if (editedSignal != null)
                {
                    //edit item
                    listBoxSignals.Items[selectedIndex] = editedSignal;

                    //update listbox view
                    listBoxSignals.Items.Refresh();

                    //update plot view
                    listBoxSignals.SelectedIndex = selectedIndex; //reselect old item
                    listBoxSignal_SelectionChanged(null, null);
                }
            }
            else if (listBoxSignals.SelectedItems.Count > 1)
            {
                editSignals();
            }
            else
            {
                //0 selected => nop
            }
        }

        /// <summary>
        /// Event for delete button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonDeleteSignal_Click(object sender, RoutedEventArgs e)
        {
            List<Signal> toBeDeleted = new List<Signal>();

            //has to be done because foreach doesnt work if its modified while running
            foreach (Signal sig in listBoxSignals.SelectedItems) //if nothing selected => runs 0 times
            {
                toBeDeleted.Add(sig);
            }


            foreach (Signal sig in toBeDeleted) //delets 0 if nothing was selected
            {
                deleteSignalFromPlot(sig); //remove from plot
                listBoxSignals.Items.Remove(sig); //remove from listbox
            }

            toBeDeleted.Clear(); //delete all. If nothing was selected the list should be empty anyways
        }
        #endregion

        #region Menu items
        /// <summary>
        /// Save current plot view to png file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_export(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Image file (.png)|*.png";
            
            if (dialog.ShowDialog() == true)
            {
                mainChart.SaveImage(dialog.FileName, ChartImageFormat.Png);
            }
        }

        /// <summary>
        /// Save signal
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_save(object sender, RoutedEventArgs e)
        {
            foreach (Signal sig in listBoxSignals.SelectedItems)
            {
                saveLoad.saveSignal(sig);
            }
        }

        /// <summary>
        /// Load signal
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_load(object sender, RoutedEventArgs e)
        {
            Signal signal = saveLoad.loadSignal();
            if (signal != null)
                listBoxSignals.Items.Add(signal);
        }
        #endregion

        private void buttonCaptureSignal_Click(object sender, RoutedEventArgs e)
        {
            //if open do nothing
            if (serialPort != null)    //not null
                if (serialPort.IsOpen) //is open
                    return;            //do nothing

            //open selected serial port
            serialPort = new SerialPort();
            
            //TODO add error handling
            serialPort.PortName = comboBoxCOM.SelectedValue as string;
            serialPort.BaudRate = int.Parse(textBoxBaudRate.Text);

            //Parity
            switch (comboBoxParity.SelectedValue.ToString())
            {
                case "Even":
                    serialPort.Parity = Parity.Even;
                    break;
                case "Mark":
                    serialPort.Parity = Parity.Mark;
                    break;
                case "Odd":
                    serialPort.Parity = Parity.Odd;
                    break;
                case "Space":
                    serialPort.Parity = Parity.Space;
                    break;
                default: //nothing matched select default
                    serialPort.Parity = Parity.None;
                    break;
            }

            //DataBits
            serialPort.DataBits = int.Parse(textBoxDataBits.Text); //this should have been already parsed => ok

            //StopBits
            switch (comboBoxStopBits.SelectedValue.ToString())
            {
                case "None":
                    serialPort.StopBits = StopBits.None;
                    break;
                case "OnePointFive":
                    serialPort.StopBits = StopBits.OnePointFive;
                    break;
                case "Two":
                    serialPort.StopBits = StopBits.Two;
                    break;
                default:
                    serialPort.StopBits = StopBits.One;
                    break;
            }

            //Handshake
            switch (comboBoxHandshake.SelectedValue.ToString())
            {
                case "RequestToSend":
                    serialPort.Handshake = Handshake.RequestToSend;
                    break;
                case "RequestToSendXOnXOff":
                    serialPort.Handshake = Handshake.RequestToSendXOnXOff;
                    break;
                case "XOnXOff":
                    serialPort.Handshake = Handshake.XOnXOff;
                    break;
                default:
                    serialPort.Handshake = Handshake.None;
                    break;
            }
            
            //Timeout no user value accepted here? 
            serialPort.ReadTimeout = 1000;

            //set new line character
            string newLineText = textBoxNewLineString.Text;
            //\n and \r are special symbols so replace them with special symbols
            newLineText = newLineText.Replace(@"\n", "\n");
            newLineText = newLineText.Replace(@"\r", "\r");
            serialPort.NewLine = newLineText;

            //register function for data received event
            serialPort.DataReceived += newComData;
            //serialPort.ope

            //open port
            serialPort.DtrEnable = true;
            serialPort.Open();
            statusBarItem1.Content = "Connected";
        }

        private void buttonStopCapture_Click(object sender, RoutedEventArgs e)
        {
            //if open then close
            if (serialPort != null)
            {
                if (serialPort.IsOpen)
                {
                    COMReadLine.WaitOne(); //wait serialPort.readLine() to complete

                    serialPort.Close();
                    statusBarItem1.Content = "Disconnected";
                    serialPort.Dispose();
                    serialPort = null;
                }
            }
        }

        private void debugMenuItem_Click(object sender, RoutedEventArgs e)
        {
            autoDetectBaudRate det = new autoDetectBaudRate("COM1");
            det.Owner = this;
            det.ShowDialog();

            if (det.result != -1)
                textBoxBaudRate.Text = det.result.ToString();
        }


        /// <summary>
        /// Check baudRate changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void baudRate_changed(object sender, RoutedEventArgs e)
        {
            try
            {
                int baudRate = int.Parse(textBoxBaudRate.Text);
                if (baudRate > 921600 || baudRate < 110)
                {
                    MessageBox.Show("Please enter something between 110 and 921600");
                    textBoxBaudRate.Text = "2400";
                }
            }
            catch
            {
                MessageBox.Show("Cannot parse: " + textBoxBaudRate.Text);
                textBoxBaudRate.Text = "2400";
            }
        }

        /// <summary>
        /// Check dataBit changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataBits_changed(object sender, RoutedEventArgs e)
        {
            try
            {
                int dataBits = int.Parse(textBoxDataBits.Text);
                if (dataBits > 100 || dataBits < 1)
                {
                    MessageBox.Show("Please enter something between 1 and 100");
                    textBoxDataBits.Text = "8";
                }
            }
            catch
            {
                MessageBox.Show("Cannot parse: " + textBoxDataBits.Text);
                textBoxDataBits.Text = "8";
            }
        }
    }
}