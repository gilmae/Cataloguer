using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace TrolleyCar {
    public class ShowDb {
        public static string GetShowDetails(string showname, string season, string episode, string year) {
            Uri uri = new Uri(string.Format("http://www.omdbapi.com/?t={0}&apikey=b8188029&y={3}&Season={1}&Episode={2}",
                showname.Replace(" ", "%20"),
                season, episode, year));

            Console.WriteLine(uri);
            using (HttpClient httpClient = new HttpClient()) {
                var response = httpClient.GetAsync(uri).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }
    }
}
