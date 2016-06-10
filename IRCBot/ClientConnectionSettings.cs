namespace IRCBot
{
    public static class ClientConnectionSettings
    {
        /*
            IRC Information
        */
        public const string IRC_SERVER = "irc.foreverchat.net";
        public const int IRC_PORT = 6667;
        //TODO: ADD IRC PASS
        public const string IRC_PASSWORD = "password";
        //TODO: ADD USER MASK
        public const string IRC_USER = "USER nick 0 * :nick";  // User information defined in RFC 2812

        /*
            Telegram Information
        */
        public const string TELEGRAM_SERVER = "https://api.telegram.org";
        //TODO: ADD TELEGRAM API TOKEN
        public const string TELEGRAM_TOKEN = "id:token";

        /*
            Telegram chat and IRC channel to connect to
        */
        //TODO: ADD TELEGRAM CLIENT ID
        public const string TELEGRAM_CLIENT_ID = "channel/client id";
        //TODO: ADD IRC CHANNEL
        public static string IRC_CHANNEL = "#channel";
        //TODO: ADD IRC NICK
        public static string IRC_NICK = "nick";

        /*
            Imgur information
        */
        //TODO: ADD IMGUR CLIENT ID
        public const string IMGUR_CLIENT_ID = "client id";
        //TODO: ADD IMGUR CLIENT SECRET
        public const string IMGUR_CLIENT_SECRET = "client secret";
        //TODO: ADD IMGUR REFRESH TOKEN
        public const string IMGUR_REFRESH_TOKEN = "refresh token";
    }
}
