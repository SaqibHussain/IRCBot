namespace IRCBot.Models
{
    public class ImgurUploadResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ImageUrl { get; set; }
        public string ImageId { get; set; }

        public ImgurUploadResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public ImgurUploadResponse(string imageUrl, string imageId)
        {
            Success = true;
            ImageUrl = imageUrl;
            ImageId = imageId;
            Message = "";
        }
    }
    public class ImgurToken
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    public class ImgurResponse
    {
        public ImgurData data { get; set; }
        public bool success { get; set; }
        public int status { get; set; }
    }

    public class ImgurData
    {
        public string id { get; set; }
        public object title { get; set; }
        public object description { get; set; }
        public int datetime { get; set; }
        public string type { get; set; }
        public bool animated { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int size { get; set; }
        public int views { get; set; }
        public int bandwidth { get; set; }
        public object vote { get; set; }
        public bool favorite { get; set; }
        public object nsfw { get; set; }
        public object section { get; set; }
        public object account_url { get; set; }
        public int account_id { get; set; }
        public object comment_preview { get; set; }
        public string deletehash { get; set; }
        public string name { get; set; }
        public string link { get; set; }
    }
}
