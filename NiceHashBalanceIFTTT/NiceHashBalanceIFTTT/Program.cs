using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace NiceHashBalanceIFTTT
{
    class Program
    {
        static void Main(string[] args)
        {
			if (args.Count() != 3)
			{
				Console.WriteLine("Usage: <WalletAddress> <Event> <Key>");
			}
			else
			{
				float pendingBalance = UpdateBalance(args);
				PushResultToIFTTT(args, pendingBalance);
			}

        }
        private static float UpdateBalance(string[] args)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://api.nicehash.com/api?method=stats.provider&addr=" + args[0]);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "GET";
            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    dynamic returnData = JsonConvert.DeserializeObject(result);
                    if (returnData != null)
                    {
                        return getBalanceFromResults(returnData.result.stats);
                    }
                    return 0f;
                }
            }
            catch (Exception ex)
            {
                return 0f;
            }
        }
        private static float getBalanceFromResults(dynamic stats)
        {
            float total = 0;
            foreach (dynamic stat in stats)
            {
                float balance = 0;
                if (float.TryParse(stat.balance.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out balance))
                {
                    total += balance;
                }
                else
                {

                }
            }
            return total;
        }
        private static void PushResultToIFTTT(string[] args, float balance)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(string.Format("https://maker.ifttt.com/trigger/{0}/with/key/{1}",args[1],args[2]));
            JObject payload = new JObject(
                new JProperty("value1", args[0]),
                new JProperty("value2", balance.ToString(
                    )));
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "Post";
            httpWebRequest.ContentLength = payload.ToString().Length;
            StreamWriter requestWriter = new StreamWriter(httpWebRequest.GetRequestStream(), System.Text.Encoding.ASCII);
            requestWriter.Write(payload.ToString());
            requestWriter.Close();

            try
            {
                WebResponse webResponse = httpWebRequest.GetResponse();
                Stream webStream = webResponse.GetResponseStream();
                StreamReader responseReader = new StreamReader(webStream);
                string response = responseReader.ReadToEnd();
                Console.Out.WriteLine(response);
                responseReader.Close();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("-----------------");
                Console.Out.WriteLine(e.Message);
            }
        }
    }
}
