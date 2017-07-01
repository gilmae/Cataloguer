using System;
using System.IO;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Newtonsoft.Json;
using System.Net;
using System.Net.Http;

namespace TrolleyCar
{
    public class Program
    {
        const string DOT_DIRECTORY = "/Users/gilmae/.trolley";

        public static void Main(string[] args)
        {
            Work();

        }

        public static void Work()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            factory.AutomaticRecoveryEnabled = true;
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "cataloguing",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                                      var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                Job job = new Job();
                job = JsonConvert.DeserializeObject<Job>(message);
                try {

                  job = Catalog(ref job);
                }
                catch (Exception ex) {
                  Console.WriteLine(ex.StackTrace);
                }
                UpdateOrchestrator(new System.Uri("http://localhost:3001/cataloguingComplete"), job);

            };
            channel.BasicConsume(queue: "cataloguing",
                                 noAck: true,
                                 consumer: consumer);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();

                }
            }
        }

        public static Job Catalog(ref Job job)
        {
            string filename = Path.GetFileName(job.Path);

            if (DecipherFileName(filename, ref job))
            {
                job.Type= "TV show";

                TweakJob(ref job);

                job.Metadata = ShowDb.GetShowDetails(job.Show, job.Season, job.Episode, job.Year);
            }
            else
            {
                job.Type="movie";
            }

            return job;
        }

        public static void TweakJob(ref Job job)
        {
            char[] charsToTrim = {' ', '-', '.', '_'};
            string showname = job.Show.Trim(charsToTrim);
            showname = showname.Replace(".", " ");
            job.Show = showname;

            if (showname.ToLower() == "the magicians us") {
                job.Show =  "The Magicians";
            }
            else if (showname.ToLower() == "doctor who 2005") {
                job.Show =  "Doctor Who";
                job.Year = "2005";
            }
            else if (showname.ToLower() == "the americans 2013") {
                job.Show =  "The Americans";
                job.Year = "2013";
            }
            else if (showname.ToLower() == "archer 2009") {
                job.Show = "Archer";
                job.Year = "2009";
            }
            else if (showname.ToLower() == "twin peaks") {
              job.Year = "2017";
            }
            else if (showname.ToLower() == "the mist") {
              job.Year = "2017";
            }
        }

        public static bool DecipherFileName(string filename, ref Job job)
        {
            var filename_deciphering_regex = new System.Text.RegularExpressions.Regex(@"([\w\d\s\.]+)[\-_\.\s]+[Ss]?(\d{1,2})[eEx](\d{2}).*\.(\w{3})");
            var result = filename_deciphering_regex.Match(filename);

            Console.WriteLine(filename);

            string showname = filename;
            string season = "";
            string episode = "";
            if (result.Groups.Count > 1) {
                showname = result.Groups[1].Value;

                if (result.Groups.Count > 2) {
                    season = result.Groups[2].Value;

                    if (result.Groups.Count > 3) {
                        episode = result.Groups[3].Value;
                    }
                }

                job.Show = showname;
                job.Season = season;
                job.Episode = episode;
                return true;
            }

            return false;
        }

        public static void UpdateOrchestrator(Uri orchestratorUrl, Job job)
        {
            using (HttpClient client = new HttpClient()) {
                var content = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json");
                var result = client.PostAsync(orchestratorUrl, content).Result;
            }
        }
    }
}
