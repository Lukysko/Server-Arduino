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
        // http://localhost:90/api/livingrooom?tempFromSenzor=20 - example of Get

        [HttpGet]
        public string Get(double tempFromSenzor)
        {
            var connection = InternetConnection.CheckConnection();
            JsonFile fileSettings = new JsonFile();
            if (connection) {
                InfluxDBCreator influxDBCreator = new InfluxDBCreator();

                // prerobit na vracanie pola s hodnotami
                string tempFromUSer = influxDBCreator.ReadFromDatabase();

                // Write to JSON settings from user
                fileSettings.WriteData(tempFromUSer);

                // Write to database temp from senzor and generate values
                // Generate data - kazda izba vlastna funkcia
                influxDBCreator.WriteToDatabase(tempFromSenzor);
                fileSettings.ReadData();
                return tempFromUSer;

            } else {
                fileSettings.ReadData();
                return "5";
            }
        }

        public class InfluxDBCreator {
            // variable
            private InfluxDBClient client;
            private List<String> databaseNames;

            public InfluxDBCreator() {
                // connection
                client = new InfluxDBClient("http://52.26.99.159:8086", "", "");
                databaseNames = client.GetInfluxDBNamesAsync().Result;
                Console.Write("Connection done \n");
                Console.Write("Database: \n");


                // testovaci vypis
                Console.Write(databaseNames[0] + "\n");
                Console.Write(databaseNames[1] + "\n");
                Console.Write(databaseNames[3] + "\n");
            }

            public string ReadFromDatabase() {
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

                int Out = 25;
                return Out.ToString();
            }

            public void WriteToDatabase(double tempFromSenzor) {

                // prepare writing of temperature from senzor to InfluxDB
                var valMixed = new InfluxDatapoint<InfluxValueField>();
                valMixed.UtcTimestamp = DateTime.UtcNow;
                valMixed.Fields.Add("actual", new InfluxValueField(tempFromSenzor));
                // posta data
                valMixed.MeasurementName = "temperature";
                valMixed.Precision = TimePrecision.Seconds;

                var sendResponse = client.PostPointAsync(databaseNames[1], valMixed).Result;
                if (sendResponse)
                {
                    Console.Write("Send OK");
                }
                else
                {
                    Console.Write("Database problem");
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
                    Console.Write("Internet connection istablished");
                    return true;
                }
                else
                {
                    Console.Write("Internet connection not istablished");
                    return false;
                }
            }
        }

        public class GenerateData {

        }

        public class JsonFile {

            private string[] drives;
            private string pathOfSettings;

            public JsonFile() {
                drives = System.IO.Directory.GetLogicalDrives();
                Console.Write(drives[0] + "\n");
                Directory.CreateDirectory(drives[0] + "\\Settings");
                pathOfSettings = drives[0] + "\\Settings" + "\\Settings.json";
            }
             
            // write data from user to JSON
            public void WriteData(string temperature) {

                // create JSON object
                dynamic livingRooom = new JObject();
                livingRooom.Temperature = temperature;

                // Write Json object to file
                System.IO.File.WriteAllText(pathOfSettings, livingRooom.ToString());

            }
            public void ReadData()
            {
                ItemInSettings item;
                using (StreamReader r = new StreamReader(pathOfSettings))
                {
                    string json = r.ReadToEnd();
                    item = Newtonsoft.Json.JsonConvert.DeserializeObject<ItemInSettings>(json);
                }
                Console.Write(item.Temperature.ToString());
            }

        }

        public class ItemInSettings
        {
            public int Temperature;
            // TO-DO ostatne parametere - huminidity ..
        }

    }
}
