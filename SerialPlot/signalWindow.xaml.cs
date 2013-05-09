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
using System.Reflection;

using System.ComponentModel;

namespace SaveLoadNS
{
    /// <summary>
    /// Interaction logic for signalWindow.xaml
    /// </summary>
    public partial class signalWindow : Window
    {
        /// <summary>
        /// Return value
        /// </summary>
        public Signal resultSignal = null;
        public Signal tempSignal = new Signal();

        /// <summary>
        /// List of available colors
        /// </summary>
        private List<string> colorList
        {
            get
            {
                //list to store colors
                List<string> list = new List<string>();
                
                //add all colors to list
                foreach (PropertyInfo info in typeof(Colors).GetProperties())
                    list.Add(info.Name);

                //return result
                return list;
            }
        }

        public signalWindow(bool multi)
        {
            InitializeComponent();
            if (multi)
            {
                textBoxTitle.Text = "*";
                textBoxXTitle.Text = "*";
                textBoxYTitle.Text = "*";
                textBoxRegExpParse.IsEnabled = false;
                textBoxRegExpMatch.IsEnabled = false;
            }
            else //not multi recommend black
            {
                comboBoxColor.SelectedItem = "Black";
            }
        }

        public signalWindow(Signal sig) //edit purposes
        {
            InitializeComponent();

            textBoxTitle.Text = sig.title.Text;
            textBoxXTitle.Text = sig.chartArea.AxisX.Title;
            textBoxYTitle.Text = sig.chartArea.AxisY.Title;
            textBoxRegExpMatch.Text = sig.regExpMatchString;
            textBoxRegExpParse.Text = sig.regExpParseString;
            comboBoxColor.SelectedItem = sig.lineColor;
        }

        /// <summary>
        /// Event function for OK button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            //apply values
            tempSignal.title.Text = textBoxTitle.Text;
            tempSignal.chartArea.AxisX.Title = textBoxXTitle.Text;
            tempSignal.chartArea.AxisY.Title = textBoxYTitle.Text;
            tempSignal.lineColor = comboBoxColor.SelectedItem as string;
            if (tempSignal.updateRegExpMatch(textBoxRegExpMatch.Text)) //match fail
            {
                MessageBox.Show("Regular Expression match pattern error!");
                textBoxRegExpMatch.Focus();
            }
            else if (tempSignal.updateRegExpParse(textBoxRegExpParse.Text)) //parse fail
            {
                MessageBox.Show("Regular Expression parse pattern error!");
                textBoxRegExpParse.Focus();
            }
            else //all ok
            {
                //all ok replace signal
                resultSignal = tempSignal; 

                //close window
                this.Close();
            }
        }

        /// <summary>
        /// Event function for Cancel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            //resultSignal = null;
            this.Close();
        }

        private void signalWindow_loaded(object sender, RoutedEventArgs e)
        {
            //populate comboBox
            comboBoxColor.ItemsSource = colorList;
        }

        private void keyDown(object sender, KeyEventArgs e)
        {
            //enter and ok button isnt focused
            if (e.Key == Key.Enter && buttonOK.IsFocused != true) 
            {
                //interpret as ok
                buttonOK_Click(null, null);
            }
        }
    }
}