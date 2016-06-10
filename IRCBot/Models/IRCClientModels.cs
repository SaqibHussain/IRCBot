namespace IRCBot.Models
{
    public enum IRCResponseMessageType
    {
        UserMessage,
        ServiceMessage,
        UserAction
    }

    public class IRCResponse
    {
        public string Channel { get; set; }
        public string Username { get; set; }
        public string Message { get; set; }
        public IRCResponseMessageType ResponseType { get; set; }

        public IRCResponse(string channel, string username, string message, IRCResponseMessageType messageType)
        {
            ResponseType = messageType;
            Message = message;
            Channel = channel;
            Username = username;
        }

        public IRCResponse(string channel, string username, string message)
            : this(channel, username, message, IRCResponseMessageType.UserMessage)
        { }

        public IRCResponse(string message)
            : this("", "", message, IRCResponseMessageType.ServiceMessage)
        { }

    }
}
