using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Rainmeter;

namespace CityLatLongPlugin
{
    internal class Measure
    {
        private string ResultSave;
        private string OnCompleteAction;
        private API api;
        private Dictionary<string, string> cityData;
        private string LastResult; // To store the result of the last execution
        private string Longitude;
        private string Latitude;
        private string Country;

        internal Measure()
        {
            cityData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Hardcoded data with quotes
            string csvData = @"
""city"",""city_ascii"",""lat"",""lng"",""country"",""iso2"",""iso3"",""admin_name"",""capital"",""population"",""id""
""Tokyo"",""Tokyo"",""35.6897"",""139.6922"",""Japan"",""JP"",""JPN"",""Tōkyō"",""primary"",""37732000"",""1392685764""
""Jakarta"",""Jakarta"",""-6.1750"",""106.8275"",""Indonesia"",""ID"",""IDN"",""Jakarta"",""primary"",""33756000"",""1360771077""
""Delhi"",""Delhi"",""28.6100"",""77.2300"",""India"",""IN"",""IND"",""Delhi"",""admin"",""32226000"",""1356872604""
""Guangzhou"",""Guangzhou"",""23.1300"",""113.2600"",""China"",""CN"",""CHN"",""Guangdong"",""admin"",""26940000"",""1156237133""";

            // Load the CSV data into a dictionary
            LoadCsvData(csvData);

            LastResult = "Click to fetch city info.";
        }

        private void LoadCsvData(string csvData)
        {
            var lines = csvData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var values = line.Split(',');
                if (values.Length > 0 && values[0] != "\"city\"") // Skip header line
                {
                    string city = values[0].Trim('"');
                    string lat = values[2].Trim('"');
                    string lng = values[3].Trim('"');
                    string country = values[4].Trim('"');
                    cityData[city] = $"{lat},{lng},{country}";
                }
            }
        }

        internal void Reload(API api, ref double maxValue)
        {
            this.api = api;
            ResultSave = api.ReadPath("ResultSave", "").Trim();
            OnCompleteAction = api.ReadString("OnCompleteAction", "").Trim();

            if (string.IsNullOrEmpty(ResultSave))
            {
                Log(API.LogType.Error, "CityLatLongPlugin.dll: 'ResultSave' must be provided.");
            }
        }

        internal string GetLastResult()
        {
            return LastResult;
        }

        internal void Execute(string cityName)
        {
            if (cityData.ContainsKey(cityName))
            {
                var data = cityData[cityName].Split(',');
                Latitude = data[0];
                Longitude = data[1];
                Country = data[2];

                LastResult = $"City: {cityName}, Country: {Country}, Lat: {Latitude}, Lng: {Longitude}";
                SaveResult(cityName);
            }
            else
            {
                LastResult = $"No Result Found for city '{cityName}'.";
                SaveNoResult(cityName); // Call a new method to save the "No Result Found" entry.
            }

            // Log result for debugging
            Log(API.LogType.Debug, $"CityLatLongPlugin.dll: {LastResult}");

            // Execute the OnCompleteAction if specified
            if (!string.IsNullOrEmpty(OnCompleteAction))
            {
                api.Execute(OnCompleteAction);
            }
        }


        private void SaveResult(string cityName)
        {
            try
            {
                if (string.IsNullOrEmpty(ResultSave))
                {
                    Log(API.LogType.Error, "CityLatLongPlugin.dll: 'ResultSave' is not specified.");
                    return;
                }

                using (StreamWriter writer = new StreamWriter(ResultSave, false)) // Overwrite the file
                {
                    writer.WriteLine("[Result_1]");
                    writer.WriteLine("Meter=String");
                    writer.WriteLine($"Text={cityName},{Country}");
                    writer.WriteLine("MeterStyle=Result_String");
                    writer.WriteLine($"LeftMouseUpAction=[!WriteKeyValue Variables Longitude \"{Longitude}\"  \"#@#GlobalVar.nek\"][!WriteKeyValue Variables Latitude \"{Latitude}\" \"#@#GlobalVar.nek\"][!WriteKeyValue Variables City \"{cityName}\" \"#@#GlobalVar.nek\"][!UpdateMeasure mToggle]");
                }

                Log(API.LogType.Debug, $"CityLatLongPlugin.dll: Results saved to {ResultSave}");
            }
            catch (Exception ex)
            {
                Log(API.LogType.Error, $"CityLatLongPlugin.dll: Error saving results - {ex.Message}");
            }
        }

        private void SaveNoResult(string cityName)
        {
            try
            {
                if (string.IsNullOrEmpty(ResultSave))
                {
                    Log(API.LogType.Error, "CityLatLongPlugin.dll: 'ResultSave' is not specified.");
                    return;
                }

                using (StreamWriter writer = new StreamWriter(ResultSave, false)) // Overwrite the file
                {
                    writer.WriteLine("[Result_1]");
                    writer.WriteLine("Meter=String");
                    writer.WriteLine($"Text=No Result Found \"{cityName}\"");
                    writer.WriteLine("MeterStyle=Result_String");
                    writer.WriteLine("MouseOverAction=[]");
                    writer.WriteLine("MouseLeaveAction=[]");
                }

                Log(API.LogType.Debug, $"CityLatLongPlugin.dll: No Result Found saved to {ResultSave}");
            }
            catch (Exception ex)
            {
                Log(API.LogType.Error, $"CityLatLongPlugin.dll: Error saving no result - {ex.Message}");
            }
        }


        internal void Log(API.LogType type, string message)
        {
            api?.Log(type, message);
        }
    }

    public static class Plugin
    {
        static IntPtr StringBuffer = IntPtr.Zero;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            GCHandle.FromIntPtr(data).Free();

            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new API(rm), ref maxValue);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            return 0.0; // Nothing to update automatically
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;

            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }

            string stringValue = measure.GetLastResult();
            if (stringValue != null)
            {
                StringBuffer = Marshal.StringToHGlobalUni(stringValue);
            }

            return StringBuffer;
        }

        [DllExport]
        public static void ExecuteBang(IntPtr data, [MarshalAs(UnmanagedType.LPWStr)] string args)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;

            // Extract city name by removing the "Execute " prefix if it exists
            if (!string.IsNullOrEmpty(args))
            {
                string command = args.Trim();
                if (command.StartsWith("Execute ", StringComparison.OrdinalIgnoreCase))
                {
                    string cityName = command.Substring(8).Trim(); // Extract city name after "Execute "
                    measure.Execute(cityName);
                }
                else
                {
                    measure.Log(API.LogType.Error, "CityLatLongPlugin.dll: Invalid command format. Use 'Execute <CityName>'.");
                }
            }
            else
            {
                measure.Log(API.LogType.Error, "CityLatLongPlugin.dll: No arguments provided in ExecuteBang.");
            }
        }

    }
}
