namespace IRCBot.Models
{
    #region For passing messages around in application
    public class TelegramReadResponse
    {
        public string Message { get; set; }
        public string Username { get; set; }
        public bool IsImage { get; set; }
        public byte[] ImageBytes { get; set; }

        public TelegramReadResponse(string message, string username)
        {
            Message = message;
            Username = username;
            IsImage = false;
        }

        public TelegramReadResponse(byte[] imageBytes)
        {
            IsImage = true;
            ImageBytes = imageBytes;
            Message = "";
            Username = "";
        }
    }
    #endregion

    #region for /getUpdates call - Deserialisation objects
    public class TelegramResponse
    {
        //Sample respone:
        /*
        {"ok":true,"result":[{"update_id":914544921,
        "message":{"message_id":4,"from":{"id":81211878,"first_name":"Saqib","last_name":"Hussain","username":"Dougie118"},"chat":{"id":81211878,"first_name":"Saqib","last_name":"Hussain","username":"Dougie118"},"date":1435396034,"text":"test"}},{"update_id":914544922,
        "message":{"message_id":6,"from":{"id":81211878,"first_name":"Saqib","last_name":"Hussain","username":"Dougie118"},"chat":{"id":81211878,"first_name":"Saqib","last_name":"Hussain","username":"Dougie118"},"date":1435399042,"text":"LOOOOOl"}},{"update_id":914544923,
        "message":{"message_id":7,"from":{"id":81211878,"first_name":"Saqib","last_name":"Hussain","username":"Dougie118"},"chat":{"id":81211878,"first_name":"Saqib","last_name":"Hussain","username":"Dougie118"},"date":1435399049,"text":"boo"}}]}
         * */
        public bool ok { get; set; }
        //Make this an array because we get an array of results
        public TelegramResult[] result { get; set; }
    }

    public class TelegramResult
    {
        public string update_id { get; set; }
        public TelegramMessage message { get; set; }
    }

    public class TelegramMessage
    {
        public string message_id { get; set; }
        public TelegramFrom from { get; set; }
        public TelegramChat chat { get; set; }
        public string date { get; set; }
        public string text { get; set; }
        public TelegramPhoto[] photo { get; set; }
    }

    public class TelegramFrom
    {
        public string id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string username { get; set; }
    }

    public class TelegramChat
    {
        public string id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string username { get; set; }
    }

    public class TelegramPhoto
    {
        public string file_id { get; set; }
        public string file_size { get; set; }
        public string width { get; set; }
        public string height { get; set; }
    }
    #endregion

    #region for /getFile calls
    public class TelegramFileResponse
    {
        //Sample respone:
        /*
        {  "ok":true,
            "result":{
                "file_id":"AgADBAADhKkxG-Yx1wQ3KhHen87lXWLkcjAABDsT2_koqhH_jIgBAAEC",
                "file_size":228434,
                "file_path":"photo\/file_35.jpg"}
        } */

        public bool ok { get; set; }
        //Make this an array because we get an array of results
        public TelegramFileResult result { get; set; }
    }

    public class TelegramFileResult
    {
        public TelegramFileResult()
        {

        }
        public string file_id { get; set; }
        public string file_size { get; set; }
        public string file_path { get; set; }
    }
    #endregion
}
