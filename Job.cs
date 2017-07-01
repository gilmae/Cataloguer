
using Newtonsoft.Json;
using System;

namespace TrolleyCar {
    public struct Job {
        [JsonProperty("path")]
        public string Path {get;set;}
        [JsonProperty("show")]
        public string Show  {get;set;}
        [JsonProperty("season")]
        public string Season  {get;set;}
        [JsonProperty("episode")]
        public string Episode  {get;set;}
        [JsonProperty("metadata")]
        public string Metadata  {get;set;}
        [JsonProperty("job_id")]
        public string Job_id  {get;set;}
        [JsonProperty("id")]
        public int Id  {get;set;}
        [JsonProperty("created_at")]
        public DateTime Created_at  {get;set;}
        [JsonProperty("updated_at")]
        public DateTime Updated_at {get;set;}
        [JsonProperty("status")]
        public string Status  {get;set;}
        [JsonProperty("ttle")]
        public string Title  {get;set;}
        [JsonProperty("type")]
        public string Type  {get;set;}

        public string Year {get;set;}
    }
}
