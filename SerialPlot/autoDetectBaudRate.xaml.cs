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
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;

namespace SaveLoadNS
{
    /// <summary>
    /// Interaction logic for autoDetectBaudRate.xaml
    /// </summary>
    public partial class autoDetectBaudRate : Window
    {
        public int result = -1;
        private int bytesToEvaluate;
        SerialPort _serialPort = new SerialPort();
        public delegate void testBaudDelegate();
        private delegate void NoArgDelegate();
        string portName;

        public static void Refresh(DependencyObject obj)
        {
            obj.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,(NoArgDelegate)delegate{});
        }

        public autoDetectBaudRate(string _portName, Parity _parity = Parity.None, int _dataBits = 8, StopBits _stopBits = StopBits.None, Handshake _handshake = Handshake.None, int _bytesToEvaluate = 10, int _timeout = 2000)
        {
            InitializeComponent();

            portName = _portName;

            //update counter for first round
            bytesToEvaluate = _bytesToEvaluate;
            textBlock_sumASCII.Text = "0/" + bytesToEvaluate.ToString();
            

            //set serial port settings
            _serialPort.PortName = _portName;
            _serialPort.Parity = _parity;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.ReadTimeout = _timeout;
            _serialPort.DtrEnable = true;

            Dispatcher.BeginInvoke(new testBaudDelegate(testBaud));
        }

        public void testBaud()
        {
            //go trough these common baudrates
            //int[] baudRates = new int[] {300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 56000, 57600, 115200};
            int[] baudRates = new int[] {1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 56000, 57600, 115200 };
            int[] sumASCII = new int[baudRates.Length]; //save amount of ascii chars here

            //test baudrates
            for (int i = 0; i < baudRates.Length; i++)//foreach (int baud in baudRates)
            {
                //store bytesToEvaluate bytes here
                byte[] inBuff = new byte[bytesToEvaluate];

                //try open the port
                try
                {
                    _serialPort.BaudRate = baudRates[i]; //set baudrate
                    _serialPort.Open();
                }
                catch
                {
                    MessageBox.Show("Cannot open serial port:" + portName);
                    this.Close();
                    return;
                }

                int counter = 0;
                while (_serialPort.BytesToRead < bytesToEvaluate)
                {
                    Thread.Sleep(10);
                    counter++;
                    if (counter > ((2 * 1000) / 10))
                    {
                        MessageBox.Show("Timeout occured while reading bytes. Is the device sending packets?");
                        _serialPort.Close();
                        this.Close();
                        return;
                    }
                }

                //try read 10 bytes
                try
                {
                    _serialPort.Read(inBuff, 0, bytesToEvaluate);//read 10 bytes
                }
                catch (TimeoutException)
                {
                    MessageBox.Show("Timeout occured while reading bytes. Is the device sending packets?");
                    _serialPort.Close();
                    this.Close();
                    return;
                }

                //should go without errors?
                _serialPort.Close();//closes serial port and clears recieve and transmit buffers

                //count reasonable ASCII chars
                foreach (byte _byte in inBuff)
                {
                    uint value = _byte;
                    if ((value >= 32 && value <= 126) || value == 10 || value == 13) //33-126 + LF + CR => reasonable ascii char
                        sumASCII[i]++; //ascii found => increment
                }

                //update late so info can be shown little time
                textBlock_baudRate.Text = baudRates[i].ToString();
                textBlock_sumASCII.Text = sumASCII[i] + "/" + bytesToEvaluate;
                
                //force refresh by some weird way
                progressBar.Value = (int)(((i + 1) / (double)bytesToEvaluate) * 100);
                Refresh(this);
            }

            //find baudrate with most ascii characters
            int maxID = 0;
            int maxValue = 0;
            for (int i = 0; i < sumASCII.Length; i++)
            {
                if (sumASCII[i] > maxValue)
                {
                    maxID = i;
                    maxValue = sumASCII[i];
                }
            }

            //return the baudrate with most ASCII characters
            result = baudRates[maxID];
            this.Close();
            return;
        }
    }
}
