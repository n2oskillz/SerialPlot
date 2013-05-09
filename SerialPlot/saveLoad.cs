using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Windows.Forms.DataVisualization.Charting;
using Microsoft.Win32;
using System.Globalization;
using System.Diagnostics;
using System.Windows.Forms;

namespace SaveLoadNS
{
    static class saveLoad
    {
        /// <summary>
        /// Encode string to Base64
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static private string EncodeTo64(string data)
        {
            //byte[] toBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(data);
            byte[] toBytes = System.Text.UTF8Encoding.UTF8.GetBytes(data);
            return System.Convert.ToBase64String(toBytes);
        }

        /// <summary>
        /// Decode string to Base64
        /// </summary>
        /// <param name="encodedData"></param>
        /// <returns></returns>
        static private string DecodeFrom64(string data)
        {
            byte[] bytes = System.Convert.FromBase64String(data);
            //return System.Text.ASCIIEncoding.ASCII.GetString(bytes);
            return System.Text.UTF8Encoding.UTF8.GetString(bytes);
        }

        static public string saveSignal(Signal sig)
        {
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = dialog.Filter = "csv file (.csv)|*.csv";
            //place where to save
            string path = ""; //replace with save dialog

            string content;

            //title stuff
            string title = EncodeTo64(sig.title.Text);
            string titleName = EncodeTo64(sig.title.Name);

            //chartArea
            string chartAreaName = EncodeTo64(sig.chartArea.Name);
            string xTitle = EncodeTo64(sig.chartArea.AxisX.Title);
            string yTitle = EncodeTo64(sig.chartArea.AxisY.Title);

            //regExp
            string regExpParseString = EncodeTo64(sig.regExpParseString);
            string regExpMatchString = EncodeTo64(sig.regExpMatchString);

            //series
            string seriesName = EncodeTo64(sig.series.Name);

            //color
            string lineColor = EncodeTo64(sig.lineColor);

            content = titleName + "," +
                      chartAreaName + "," +
                      seriesName + "," +
                      title + "," +
                      xTitle + "," +
                      yTitle + "," +
                      regExpMatchString + "," +
                      regExpParseString + "," +
                      lineColor;

            foreach(DataPoint value in sig.series.Points)
            {
                content += "," + value.YValues[0].ToString(CultureInfo.InvariantCulture);
            }

            //save file if dialog ok
            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, content);
            }
            
            //save in this format
            //<signal title:"xxx", xTitle="", yTitle"", regExpMatch"", regExpParse"", startTime Value="yyyyMMDD">
            //HHMMSSxxxx, 12.32412, //delta from begining
            //34.123,
            //43.234,
            //56.235
            //<signal/>
            return path;
        }

        static public Signal loadSignal()
        {
            //file dialog
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = dialog.Filter = "csv file (.csv)|*.csv";

            //if file was readed
            if (dialog.ShowDialog() == true)
            {
                Signal resultSignal = new Signal();

                //read all text
                string signalString = File.ReadAllText(dialog.FileName);

                //split csv
                string[] signalArray = signalString.Split(',');

                /*content = titleName + "," +
                      chartAreaName + "," +
                      seriesName + "," +
                      title + "," +
                      xTitle + "," +
                      yTitle + "," +
                      regExpMatchString + "," +
                      regExpParseString;*/


                //title
                resultSignal.title.Text = DecodeFrom64(signalArray[3]);
                resultSignal.title.Name = DecodeFrom64(signalArray[0]);

                //chartArea
                resultSignal.chartArea.Name = DecodeFrom64(signalArray[1]);
                resultSignal.chartArea.AxisX.Title = DecodeFrom64(signalArray[4]);
                resultSignal.chartArea.AxisY.Title = DecodeFrom64(signalArray[5]);

                //regExp
                resultSignal.updateRegExpParse(DecodeFrom64(signalArray[7]));
                resultSignal.updateRegExpMatch(DecodeFrom64(signalArray[6]));

                //series
                resultSignal.series.Name = DecodeFrom64(signalArray[2]);

                //color
                resultSignal.lineColor = DecodeFrom64(signalArray[8]);

                //read values
                for (int i = 9; i < signalArray.Length; i++)
                {
                    double valueDouble = double.Parse(signalArray[i], CultureInfo.InvariantCulture);
                    resultSignal.series.Points.AddY(valueDouble);
                }

                //return signal
                return resultSignal;
            }
            else
            {
                return null;
            }
        }
    }
}
