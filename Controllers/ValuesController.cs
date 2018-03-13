using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AdysTech.InfluxDB.Client.Net;
using System.Net;
using System.Net.NetworkInformation;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;
using System.Net.Sockets;
using System.Globalization;

namespace Server.Controllers
{ 
    [Route("api/[controller]")]
    // Controler for living room
    public class LivingRooomController : Controller
    {
        // http://localhost:90/api/livingrooom?tempFromSenzor=20 - example of get

        [HttpGet]
        public string Get(double tempFromSenzor)
        {
            var connection = InternetConnection.CheckConnection();
            SettingsFile fileSettings = new SettingsFile();

            if (connection) {
                InfluxDBCreator influxDBCreator = new InfluxDBCreator();
                ItemInSettings databaseValuesUser = influxDBCreator.ReadFromDatabase();

                // Write to JSON settings from user
                fileSettings.WriteData(databaseValuesUser.Temperature, databaseValuesUser.Light, databaseValuesUser.Blinds);

                // Write to database temp from senzor and generate values
                influxDBCreator.WriteToDatabase(tempFromSenzor);

                Console.WriteLine("-----------------------------------------------------------");

                return databaseValuesUser.Temperature;
            } else {
                ItemInSettings userValueSettingFile = fileSettings.ReadData();

                Console.WriteLine("-----------------------------------------------------------");

                return userValueSettingFile.Temperature;
                // return temp + if internet is working -> "25+0" or "25+1"
            }
        }

        public class InfluxDBCreator {

            private InfluxDBClient client;
            private List<String> databaseNames;

            public InfluxDBCreator() {
                // connection
                client = new InfluxDBClient("http://18.221.12.219:8086", "", "");
                databaseNames = client.GetInfluxDBNamesAsync().Result;
                Console.Write("Connection done \n");
                Console.Write("Database: \n");

                Console.Write(databaseNames[4] + "\n");
                Console.Write(databaseNames[5] + "\n");
            }

            public ItemInSettings ReadFromDatabase() {

                ItemInSettings userOptionsFromDatabase = new ItemInSettings();
                
                // table - UserWish 
                var queryResultSet = client.QueryMultiSeriesAsync(databaseNames[5], "select * from LivingRoom").Result;
                var numberOfRecordsResult = queryResultSet[0].Entries.Count;

                userOptionsFromDatabase.Temperature = queryResultSet.Last()?.Entries[numberOfRecordsResult-1].Temperature;
                userOptionsFromDatabase.Blinds = queryResultSet.Last()?.Entries[numberOfRecordsResult - 1].Blinds;
                userOptionsFromDatabase.Light = queryResultSet.Last()?.Entries[numberOfRecordsResult - 1].Light;

                return userOptionsFromDatabase;
            }

            public void WriteToDatabase(double tempFromSenzor) {

                // prepare writing of temperature from senzor to InfluxDB 
                var valMixed = new InfluxDatapoint<InfluxValueField>();
                valMixed.UtcTimestamp = GetTimeInternet();

                // generate value for humudity
                Random rnd = new Random();
                double humidity = tempFromSenzor - rnd.Next(1, 13);

                // what is to be writting
                valMixed.Fields.Add("Temperature", new InfluxValueField(tempFromSenzor));
                valMixed.Fields.Add("Humidity", new InfluxValueField(humidity));

                // post data
                valMixed.MeasurementName = "LivingRoom";
                valMixed.Precision = TimePrecision.Seconds;

                var sendResponse = client.PostPointAsync(databaseNames[4], valMixed).Result;


                //--------------------------------------------------------
                // KidsRoom - generate random data
                valMixed = new InfluxDatapoint<InfluxValueField>();
                valMixed.Fields.Add("Temperature", new InfluxValueField(tempFromSenzor + rnd.Next(1,4)));
                valMixed.Fields.Add("Humidity", new InfluxValueField(humidity + rnd.Next(1, 4)));
                valMixed.MeasurementName = "KidsRoom";
                valMixed.Precision = TimePrecision.Seconds;

                sendResponse = client.PostPointAsync(databaseNames[4], valMixed).Result;

                // Bedroom - generate random data
                valMixed = new InfluxDatapoint<InfluxValueField>();
                valMixed.Fields.Add("Temperature", new InfluxValueField(tempFromSenzor + rnd.Next(1, 4)));
                valMixed.Fields.Add("Humidity", new InfluxValueField(humidity + rnd.Next(1, 4)));
                valMixed.MeasurementName = "Bedroom";
                valMixed.Precision = TimePrecision.Seconds;

                sendResponse = client.PostPointAsync(databaseNames[4], valMixed).Result;

                // Kitchen - generate random data
                valMixed = new InfluxDatapoint<InfluxValueField>();
                valMixed.Fields.Add("Temperature", new InfluxValueField(tempFromSenzor + rnd.Next(1, 4)));
                valMixed.Fields.Add("Humidity", new InfluxValueField(humidity + rnd.Next(1, 4)));
                valMixed.MeasurementName = "Kitchen";
                valMixed.Precision = TimePrecision.Seconds;

                sendResponse = client.PostPointAsync(databaseNames[4], valMixed).Result;
                //--------------------------------------------------------

                // Check if the write to dabase was successful
                if (sendResponse)
                {
                    Console.Write("Send OK \n");
                }
                else
                {
                    Console.Write("Database problem \n");
                }
            }

            
            private static DateTime GetTimeInternet() {
                
                var client = new TcpClient("time.nist.gov", 13);
                DateTime localDateTime;

                using (var streamReader = new StreamReader(client.GetStream()))
                {
                    var response = streamReader.ReadToEnd();
                    var utcDateTimeString = response.Substring(7, 17);
                    localDateTime = DateTime.ParseExact(utcDateTimeString, "yy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                }

                Console.Write(localDateTime.ToString() + "\n");
                DateTime finalTime = localDateTime.AddHours(9);
                return finalTime;
            }
            
        }

        public class InternetConnection {
            public static bool CheckConnection() {
                Ping ping = new Ping();
                PingReply pingStatus =
                    ping.Send(IPAddress.Parse("208.69.34.231"), 1000);

                if (pingStatus.Status == IPStatus.Success)
                {
                    Console.Write("Internet connection istablished \n");
                    return true;
                }
                else
                {
                    Console.Write("Internet connection not istablished \n");
                    return false;
                }
            }
        }

        // TODO generovanie hodnot pre ostatne izby - volanie funkcie pri zapiovani do databazy
        public class GenerateData {

        }

        public class SettingsFile {

            private string[] drives;
            private string pathOfSettings;

            // Create file with user settings
            public SettingsFile() {
                drives = System.IO.Directory.GetLogicalDrives();
                Console.Write(drives[0] + "\n");
                Directory.CreateDirectory(drives[0] + "\\Settings");
                pathOfSettings = drives[0] + "\\Settings" + "\\Settings.json";
            }
             
            // write data from user to JSON
            public void WriteData(string temperature, string light, string blinds) {

                // create JSON object from user settings
                dynamic livingRooom = new JObject();
                livingRooom.Temperature = temperature;
                livingRooom.Light = light;
                livingRooom.Blinds = blinds;

                // Write Json object to file
                System.IO.File.WriteAllText(pathOfSettings, livingRooom.ToString());

            }
            public ItemInSettings ReadData()
            {
                ItemInSettings item;
                using (StreamReader r = new StreamReader(pathOfSettings))
                {
                    string json = r.ReadToEnd();
                    item = JsonConvert.DeserializeObject<ItemInSettings>(json);
                }

                Console.Write("Temperature from user: " +item.Temperature.ToString() + "\n");
                Console.Write("Light: " + item.Light.ToString() + "\n");
                Console.Write("Blinds:" + item.Blinds.ToString() + "\n");

                return item;
            }

        }

        public class ItemInSettings
        {
            // Settings of Living Room from user
            public string Temperature = "25";
            public string Light = "0";
            public string Blinds = "0";
        }

    }
}
