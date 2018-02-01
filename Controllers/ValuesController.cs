using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AdysTech.InfluxDB.Client.Net;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // http://192.168.1.6:90/api/values?tempFromSenzor=20 - example
        [HttpGet]

        //public async Task<string> Get(double tempFromSenzor)
        public string Get(double tempFromSenzor)
        {
            /* 
            var TemperatureController = new Temperature();
            
            TemperatureController.CreateConnection().Wait();
            Console.Write("Connection done \n");
            
            TemperatureController.SendTempToDatabase(tempFromSenzor).Wait();
            Console.Write("Temp send \n");
            
            var tempFromUser = TemperatureController.GetTempFromDatabase().Result;
            Console.Write("Temp read" + tempFromUser + " \n");
            
            return tempFromUser.ToString();
            */

            InfluxDBClient client;
            List<String> dbNames;
            client = new InfluxDBClient("http://52.26.99.159:8086", "", "");
            dbNames = client.GetInfluxDBNamesAsync().Result;
            Console.Write("Connection done");
            Console.Write(dbNames[0]);
            Console.Write(dbNames[1]);
            Console.Write(dbNames[3]);

             var valMixed = new InfluxDatapoint<InfluxValueField>();
             valMixed.UtcTimestamp = DateTime.UtcNow;
             valMixed.Fields.Add("actual", new InfluxValueField(tempFromSenzor));

             var queryResultSet = client.QueryMultiSeriesAsync(dbNames[3], "select * from temperature").Result;
             var numberOfRecordsResult = queryResultSet[0].Entries.Count;
             var setValueFromUserResult = queryResultSet.Last()?.Entries[numberOfRecordsResult-1].Setpoint;
             Console.Write(setValueFromUserResult);
             valMixed.Fields.Add("setpoint", new InfluxValueField(Convert.ToDouble(setValueFromUserResult)));

             valMixed.MeasurementName = "temperature";
             valMixed.Precision = TimePrecision.Seconds;
                
             var r =  client.PostPointAsync(dbNames[1], valMixed).Result;
             Console.Write("Send OK");

            var queryResult = client.QueryMultiSeriesAsync(dbNames[1], "select * from temperature").Result;
            Console.Write(queryResult);
            var numberOfRecords = queryResult[0].Entries.Count;
            Console.Write(numberOfRecords);
            var setValueFromUser = queryResult.Last()?.Entries[numberOfRecords-1].Setpoint;
            Console.Write("teeeeeeeeeeeeest");
            Console.Write(setValueFromUser.ToString());

            return setValueFromUser;
        }
        
    }

    
    public class Temperature
    {
        InfluxDBClient client;
        List<String> dbNames;

        public async Task CreateConnection(){
            client = new InfluxDBClient("http://52.26.99.159:8086", "", "");
            dbNames = await client.GetInfluxDBNamesAsync();
            Console.Write("Connection done");
            Console.Write(dbNames[0]);
            Console.Write(dbNames[1]);
        }

        public async Task SendTempToDatabase(double temp)
        {
             var valMixed = new InfluxDatapoint<InfluxValueField>();
             valMixed.UtcTimestamp = DateTime.UtcNow;
             valMixed.Fields.Add("actual", new InfluxValueField(temp));
             valMixed.Fields.Add("setpoint", new InfluxValueField(10.0));

             valMixed.MeasurementName = "temperature";
             valMixed.Precision = TimePrecision.Seconds;
                
             var r = await client.PostPointAsync(dbNames[1], valMixed);
             Console.Write("Send OK");
        }

        public async Task<string> GetTempFromDatabase()
        {
            var queryResult = await client.QueryMultiSeriesAsync(dbNames[1], "select * from temperature limit 10");
            var numberOfRecords = queryResult[0].Entries.Count;
            var setValueFromUser = queryResult.Last()?.Entries[numberOfRecords-1].Setpoint;
            Console.Write(setValueFromUser);

            return setValueFromUser;
        }
    }
}
