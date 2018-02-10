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
                // Generate data - kazda izba vlastna funkcia
                influxDBCreator.WriteToDatabase(tempFromSenzor);

                return databaseValuesUser.Temperature;
                // return temp + if internet is working -> "25+0" or "25+1"

            } else {
                ItemInSettings userValueSettingFile = fileSettings.ReadData();
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


                // testovaci vypis
                Console.Write(databaseNames[0] + "\n");
                Console.Write(databaseNames[1] + "\n");
                Console.Write(databaseNames[2] + "\n");
            }

            public ItemInSettings ReadFromDatabase() {

                ItemInSettings userOptionsFromDatabase = new ItemInSettings();
                userOptionsFromDatabase.Blinds = "1";
                userOptionsFromDatabase.Light = "1";
                userOptionsFromDatabase.Temperature = "25";

                // prerobit cez nove tablku
                /*
                var queryResultSet = client.QueryMultiSeriesAsync(databaseNames[3], "select * from temperature").Result;
                var numberOfRecordsResult = queryResultSet[0].Entries.Count;
                var setValueFromUserResult = queryResultSet.Last()?.Entries[numberOfRecordsResult-1].Setpoint;
                Console.Write(setValueFromUserResult);
                valMixed.Fields.Add("setpoint", new InfluxValueField(Convert.ToDouble(setValueFromUserResult)));
                var queryResult = client.QueryMultiSeriesAsync(databaseNames[1], "select * from temperature").Result;
                Console.Write(queryResult);
                var numberOfRecords = queryResult[0].Entries.Count;
                Console.Write(numberOfRecords);
                var setValueFromUser = queryResult.Last()?.Entries[numberOfRecords-1].Setpoint;
                Console.Write("teeeeeeeeeeeeest");
                Console.Write(setValueFromUser.ToString());
                */

                return userOptionsFromDatabase;
            }

            public void WriteToDatabase(double tempFromSenzor) {

                // prepare writing of temperature from senzor to InfluxDB
                var valMixed = new InfluxDatapoint<InfluxValueField>();
                valMixed.UtcTimestamp = DateTime.UtcNow;
                valMixed.Fields.Add("actual", new InfluxValueField(tempFromSenzor));

                // post data
                valMixed.MeasurementName = "temperature";
                valMixed.Precision = TimePrecision.Seconds;

                var sendResponse = client.PostPointAsync(databaseNames[1], valMixed).Result;

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
            // Settings of Living Room
            public string Temperature = "25";
            public string Light = "0";
            public string Blinds = "0";
        }

    }
}
