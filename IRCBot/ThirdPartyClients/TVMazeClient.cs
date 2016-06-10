using System;
using System.IO;
using System.Net;

using IRCBot.Models;

using Newtonsoft.Json;

namespace IRCBot.ThirdPartyClients
{
    public class TVMazeClient
    {

        private const string API_URL = "http://api.tvmaze.com/";
        private const string API_SHOW_SEARCH_URL = "search/shows?q=";

        public TVMazeClient()
        {

        }

        public TvMazeSearchResult Serach(string showName)
        {
            try
            {
                var result = SearchShow(showName);

                if (result == null)
                {
                    return new TvMazeSearchResult("Null object when deserialising data from TVMaze. Query url was: ");
                }

                if (result.Length < 1)
                {
                    return new TvMazeSearchResult("No results found on TVMaze.");
                }
                else
                {
                    TvMazeShow show = result[0].show;
                    AddEpisodeInfo(show);
                    return new TvMazeSearchResult(show);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return new TvMazeSearchResult(ex.Message);
            }
        }

        private TvMazeResult[] SearchShow(string showName)
        {
            try
            {
                string queryUrl = $"{API_URL}{API_SHOW_SEARCH_URL}{showName}";
                string response = GetResponseFromUrl(queryUrl);
                TvMazeResult[] result = JsonConvert.DeserializeObject<TvMazeResult[]>(response);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Adds next and previous episode information into the given show
        /// </summary>
        /// <param name="show"></param>
        private void AddEpisodeInfo(TvMazeShow show)
        {
            try
            {
                if (show?._links != null)
                {
                    if (!string.IsNullOrWhiteSpace(show._links.previousepisode?.href))
                    {
                        try
                        {
                            string prevousEpisode = GetResponseFromUrl(show?._links?.previousepisode?.href);
                            TvMazeEpisode previousEpisode = JsonConvert.DeserializeObject<TvMazeEpisode>(prevousEpisode);
                            show.PrevEpisode = previousEpisode;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(show._links.nextepisode?.href))
                    {
                        try
                        {
                            string nextEpisodeResponse = GetResponseFromUrl(show?._links?.nextepisode?.href);
                            TvMazeEpisode nextEpisode = JsonConvert.DeserializeObject<TvMazeEpisode>(nextEpisodeResponse);
                            show.NextEpisode = nextEpisode;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private string GetResponseFromUrl(string url)
        {
            try
            {
                WebRequest webRequest = WebRequest.Create(url);
                using (WebResponse webResp = webRequest.GetResponse())
                using (Stream webStream = webResp.GetResponseStream())
                using (StreamReader reader = new StreamReader(webStream))
                {
                    return reader.ReadToEnd();
                }
            }
        
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
    }
}
