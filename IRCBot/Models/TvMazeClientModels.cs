using System;

namespace IRCBot.Models
{
    public class TvMazeSearchResult
    {
        public TvMazeSearchResult(string message)
        {
            Found = false;
            Message = message;
        }

        public TvMazeSearchResult(TvMazeShow show)
        {
            Show = show;
            Found = true;
            Message = "";
        }

        public bool Found { get; set; }

        public string Message { get; set; }
        public TvMazeShow Show { get; set; }
    }
    public class TvMazeResults
    {
        public TvMazeResult[] Results { get; set; }
    }

    public class TvMazeResult
    {
        public string score { get; set; }
        public TvMazeShow show { get; set; }
    }

    public class TvMazeShow
    {
        public string id { get; set; }
        public string url { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string language { get; set; }
        public string[] genres { get; set; }
        public string status { get; set; }
        public string runtime { get; set; }
        public string premiered { get; set; }
        public TvMazeSchedule schedule { get; set; }
        public TvMazeRating rating { get; set; }
        public string weight { get; set; }
        public TvMazeNetwork network { get; set; }
        public TvMazeWebchannel webChannel { get; set; }
        public TvMazeExternals externals { get; set; }
        public TvMazeImage image { get; set; }
        public string summary { get; set; }
        public string updated { get; set; }
        public TvMazeLinks _links { get; set; }
        // The following are not deserialised properties, we add them in
        // after making additional calls
        public TvMazeEpisode NextEpisode { get; set; }
        public TvMazeEpisode PrevEpisode { get; set; }
    }

    public class TvMazeSchedule
    {
        public string time { get; set; }
        public string[] days { get; set; }
    }

    public class TvMazeRating
    {
        public string average { get; set; }
    }

    public class TvMazeNetwork
    {
        public string id { get; set; }
        public string name { get; set; }
        public TvMazeCountry country { get; set; }
    }

    public class TvMazeCountry
    {
        public string name { get; set; }
        public string code { get; set; }
        public string timezone { get; set; }
    }

    public class TvMazeWebchannel
    {
        public string id { get; set; }
        public string name { get; set; }
        public TvMazeCountry country { get; set; }
    }

    public class TvMazeExternals
    {
        public string TvMaze { get; set; }
        public string thetvdb { get; set; }
        public string imdb { get; set; }
    }

    public class TvMazeImage
    {
        public string medium { get; set; }
        public string original { get; set; }
    }

    public class TvMazeLinks
    {
        public TvMazeSelf self { get; set; }
        public TvMazePreviousepisode previousepisode { get; set; }
        public TvMazeNextepisode nextepisode { get; set; }
    }

    public class TvMazeSelf
    {
        public string href { get; set; }
    }

    public class TvMazePreviousepisode
    {
        public string href { get; set; }
    }

    public class TvMazeNextepisode
    {
        public string href { get; set; }
    }

    public class TvMazeEpisode
    {
        public bool HasAirDate
        {
            get
            {
                return !(airdate == "(x)");
            }
        }
        public string id { get; set; }
        public string url { get; set; }
        public string name { get; set; }
        public string season { get; set; }
        public string number { get; set; }
        public string airdate { get; set; }
        public string airtime { get; set; }
        public DateTime airstamp { get; set; }
        public int runtime { get; set; }
        public TvMazeImage image { get; set; }
        public string summary { get; set; }
        public TvMazeLinks _links { get; set; }
    }
}
