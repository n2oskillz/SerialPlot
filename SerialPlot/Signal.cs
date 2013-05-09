using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Media;

namespace SaveLoadNS
{
    public class Signal //luokan oltava public jos sitä käytetään public-luokkana muualla
    {
        #region regExp objects
        /// <summary>
        /// This is the regular expression object that does the matching
        /// </summary>
        private Regex regExpMatch { get; set; }

        /// <summary>
        /// Represents pattern for regular expression match
        /// </summary>
        public string regExpMatchString { get; set; }

        /// <summary>
        /// This is the regular expression object that does the parching
        /// </summary>
        private Regex regExpParse { get; set; }

        /// <summary>
        /// Represents pattern for regular expression parse
        /// </summary>
        public string regExpParseString { get; set; }
        #endregion

        #region Plot variables (plot data and settings)
        /// <summary>
        /// string representation of the color
        /// </summary>
        private string _lineColor;

        /// <summary>
        /// The visible property for color
        /// </summary>
        public string lineColor
        {
            get
            {
                return _lineColor; //return the string representation of the color
            }
            set
            {
                //Convert string to color and update the plot color
                series.Color = colorFromString(value);

                //save string representation
                _lineColor = value;
            }
        }

        /// <summary>
        /// Save plot axis data here
        /// Area name
        /// Titles for X and Y-axis
        /// </summary>
        public ChartArea chartArea { get; set; }


        /// <summary>
        /// Save plot data here
        /// </summary>
        public Series series { get; set; }

        /// <summary>
        /// Save title for plot here
        /// </summary>
        public Title title { get; set; }
        #endregion


        /// <summary>
        /// Initiate variables
        /// </summary>
        private void initVar()
        {
            string timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            chartArea = new ChartArea("chartArea:" + timeStamp);
            series = new Series("series:" + timeStamp);
            series.ChartType = SeriesChartType.Line;
            lineColor = "Black"; //default color
            title = new Title();
            title.Name = "title:" + timeStamp; //set name to 
            regExpMatchString = "";
            regExpParseString = "";
        }


        /// <summary>
        /// Constructor without any parameters
        /// </summary>
        public Signal()
        {
            initVar();
        }


        /// <summary>
        /// Constructor with predefined names
        /// </summary>
        /// <param name="titleIn"></param>
        /// <param name="xTitle"></param>
        /// <param name="yTitle"></param>
        /// <param name="areaName"></param>
        public Signal(string titleIn, string xTitle, string yTitle, string areaName)
        {
            initVar();

            title.Text = titleIn;
            chartArea.AxisX.Title = xTitle;
            chartArea.AxisY.Title = yTitle;
            chartArea.Name = areaName;
            title.DockedToChartArea = chartArea.Name;
        }


        /// <summary>
        /// Convert string representation of color to System.Drawing.Color
        /// </summary>
        /// <param name="colorName"></param>
        /// <returns></returns>
        private System.Drawing.Color colorFromString(string colorName)
        {
            System.Drawing.Color resultColor;

            //if not null => convert. If null => keep old
            if (colorName != null)
            {
                BrushConverter conv = new BrushConverter();
                SolidColorBrush brs = conv.ConvertFromString(colorName) as SolidColorBrush;
                System.Windows.Media.Color color = brs.Color;
                resultColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
            }
            else
            {
                resultColor = series.Color; //old color
            }

            return resultColor;
        }


        /// <summary>
        /// This function sets new pattern for match function. Returns true if pattern failure
        /// </summary>
        /// <param name="pattern">
        /// string repsentation of the new regular expression pattern. Hint: use @"" -formatting.
        /// </param>
        /// <returns></returns>
        public bool updateRegExpMatch(string pattern)
        {
            bool result = false;
            //create new regexp object
            try
            {
                regExpMatch = new Regex(pattern);
                regExpMatchString = pattern;
            }
            catch (ArgumentException) //nothing to do with it just dismiss
            {
                result = true;
                Debug.WriteLine("updateRegExpMatch Regex pattern generation failed:" + pattern, "ERROR");
            }
            return result;
        }

        /// <summary>
        /// This function sets new pattern for parse function. Returns true if pattern failure
        /// </summary>
        /// <param name="pattern">
        /// string repsentation of the new regular expression pattern. Hint: use @"" -formatting.
        /// </param>
        /// <returns></returns>
        public bool updateRegExpParse(string pattern)
        {
            bool result = false;
            //create new regexp object
            try
            {
                regExpParse = new Regex(pattern);
                regExpParseString = pattern;
            }
            catch (ArgumentException)
            {
                result = true;
                Debug.WriteLine("updateRegExpParse Regex pattern generation failed:" + pattern, "ERROR");
            }
            return result;
        }


        /// <summary>
        /// Check for input string if its a match
        /// </summary>
        /// <param name="textToCheck">String to look match from</param>
        /// <returns></returns>
        public bool checkMatch(string textToCheck)
        {
            return regExpMatch.IsMatch(textToCheck);
        }


        /// <summary>
        /// Parse value from string. Returns empty if something went wrong.
        /// </summary>
        /// <param name="data">String of input data</param>
        /// <returns>When used right returns nice clean value.</returns>
        public string parseData(string data)
        {
            string result = "";

            //try and return "" if nothing found
            try
            {
                Match match = regExpParse.Match(data);
                result = match.Value;
            }
            catch (ArgumentNullException)
            {
                Debug.WriteLine("parseData received empty data", "ERROR"); //send debug message
            }
            return result;
        }
    }
}
