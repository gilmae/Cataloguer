﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        static IDictionary<string, Job> _tweaks { get; set; }
        static IDictionary<string, string> _config;

        public static void Main(string[] args)
        {
            _tweaks = JsonConvert.DeserializeObject<IDictionary<string, Job>>(File.ReadAllText(@"tweaks.json"));

            _config = JsonConvert.DeserializeObject<IDictionary<string, string>>(File.ReadAllText(@".config"));

            switch (args[0])
            {
                case "tweak":
                    Console.WriteLine("tweaking");
                    Tweak(args);
                    break;
                case "work":
                    Console.WriteLine("Working");
                    Work();
                    break;
            }
        }

        public static void Tweak(string[] args)
        {
            string key = args[1].ToLower();
            if (!_tweaks.ContainsKey(key))
            {
                _tweaks[key] = new Job();
            }

            Job job = _tweaks[key];

            string newValue = args[3];

            switch (args[2])
            {
                case "year":
                case "y":
                    job.Year = newValue;
                    break;
                case "show":
                case "s":
                    job.Show = newValue;
                    break;
                case "episode":
                case "ep":
                case "e":
                    job.Episode = newValue;
                    break;
                case "season":
                case "se":
                    job.Season = newValue;
                    break;
            }

            _tweaks[key] = job;

            File.WriteAllText(@"tweaks.json", JsonConvert.SerializeObject(_tweaks));
        }

        public static void Work()
        {
            var factory = new ConnectionFactory() { HostName = _config["queue_host"] };
            factory.AutomaticRecoveryEnabled = true;
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: _config["queue_name"],
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
                        try
                        {
                            Catalog(ref job);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }

                    };
                    channel.BasicConsume(queue: _config["queue_name"],
                                         noAck: true,
                                         consumer: consumer);

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();

                }
            }
        }

        public static void Catalog(ref Job job)
        {
            string filename = Path.GetFileName(job.Path);

            if (DecipherFileName(filename, ref job))
            {
                job.Type = "TV show";

                char[] charsToTrim = { ' ', '-', '.', '_' };
                string showname = job.Show.Trim(charsToTrim);
                showname = showname.Replace(".", " ");
                job.Show = showname;

                if (_tweaks.ContainsKey(showname.ToLower()))
                {
                    job = UpdateJob(job, _tweaks[showname.ToLower()]);
                }

                job.Metadata = ShowDb.GetShowDetails(job.Show, job.Season, job.Episode, job.Year);
            }
            else
            {
                job.Type = "movie";
            }

            UpdateOrchestrator(new System.Uri(_config["orchestrator"]), job);

        }

        public static bool DecipherFileName(string filename, ref Job job)
        {
            var filename_deciphering_regex = new System.Text.RegularExpressions.Regex(@"([\w\d\s\.]+)[\-_\.\s]+[Ss]?(\d{1,2})[eEx](\d{2}).*\.(\w{3})");
            var result = filename_deciphering_regex.Match(filename);

            Console.WriteLine(filename);

            string showname = filename;
            string season = "";
            string episode = "";
            if (result.Groups.Count > 1)
            {
                showname = result.Groups[1].Value;

                if (result.Groups.Count > 2)
                {
                    season = result.Groups[2].Value;

                    if (result.Groups.Count > 3)
                    {
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
            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json");
                var result = client.PostAsync(orchestratorUrl, content).Result;
            }
        }

        public static Job UpdateJob(Job existing, Job newData)
        {
            if (!string.IsNullOrEmpty(newData.Show))
            {
                existing.Show = newData.Show;
            }
            if (!string.IsNullOrEmpty(newData.Season))
            {
                try
                {
                    existing.Season = (int.Parse(existing.Season) + int.Parse(newData.Season)).ToString();
                }
                catch 
                {
                    
                }
            }
            if (!string.IsNullOrEmpty(newData.Episode))
            {
                try 
                { 
                    existing.Episode = (int.Parse(existing.Episode) + int.Parse(newData.Episode)).ToString(); 
                }
                catch 
                {
                    
                }
            }
            if (!string.IsNullOrEmpty(newData.Year))
            {
                existing.Year = newData.Year;
            }

            return existing;
        }
    }
}
