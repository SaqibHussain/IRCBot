using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using IRCBot.Models;

namespace IRCBot.ThirdPartyClients
{
    public class IRCClient : IDisposable
    {
        private TcpClient _ircClient;
        private NetworkStream _ircNetworkSream;
        private StreamWriter _writer;
        private StreamReader _reader;
        private Queue<IRCResponse> _messages;
        private List<string> _channels;

        private string _server, _user, _nick, _nickPassword;
        private int _port;
        private bool _disposed, _connected, _readThreadCanRun, _isConnecting;

        #region Properties
        public bool IsConnected
        {
            get
            {
                try
                {
                    if (_ircClient != null && _ircClient.Client != null && _ircClient.Client.Connected)
                    {
                        /* 
                         * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                         * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                         * -or- true if data is available for reading; 
                         * -or- true if the connection has been closed, reset, or terminated; 
                         * otherwise, returns false
                         */

                        // Detect if client disconnected
                        if (_ircClient.Client.Poll(0, SelectMode.SelectRead))
                        {
                            byte[] buff = new byte[1];
                            if (_ircClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                            {
                                // Client disconnected
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }
        #endregion

        #region Constructors
        public IRCClient(string server, int port, string user, string nick)
        {
            Construct(server, port, user, nick, "");
        }

        public IRCClient(string server, int port, string user, string nick, string nickPassword)
        {
            Construct(server, port, user, nick, nickPassword);
        }

        private void Construct(string server, int port, string user, string nick, string nickPassword)
        {
            _server = server;
            _port = port;
            _user = user;
            _nick = nick;
            _nickPassword = nickPassword;

            _messages = new Queue<IRCResponse>();
            _channels = new List<string>();
        }
        #endregion

        #region Public
        public bool Connect(int connectTimeoutMiliSeconds)
        {
            try
            {
                _isConnecting = true;

                _ircClient = new TcpClient(_server, _port);
                _ircNetworkSream = _ircClient.GetStream();
                _writer = new StreamWriter(_ircNetworkSream);
                _reader = new StreamReader(_ircNetworkSream);

                Write("NICK " + _nick);
                Write(_user);

                _readThreadCanRun = true;
                Thread readStreamThread = new Thread(new ThreadStart(ReadStreamThread));
                readStreamThread.Start();

                // Kick off the thread above to start reading from the stream
                // Once we get a 001 message, we know we're connected 
                int miliseconds = 0;
                while (true)
                {
                    // Timedout
                    if (miliseconds > connectTimeoutMiliSeconds)
                    {
                        _isConnecting = false;
                        return false;
                    }

                    if (_connected)
                    {
                        _isConnecting = false;
                        return true;
                    }

                    // Still not connected, wait for connection or timeout
                    Thread.Sleep(100);
                    miliseconds += 100;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                return false;
            }
        }

        public void JoinChannel(string channelName)
        {
            // Write to stream to tell server we want to join the channel
            string JoinString = "JOIN " + channelName;
            Write(JoinString);

            // Add this to our channel list to keep track of which channels we're in
            _channels.Add(channelName.ToLower());
        }

        public void Write(string channelName, string message)
        {
            Write($"PRIVMSG {channelName} {message}");
        }

        public void GetNames(string channelName)
        {
            Write("names " + channelName);
        }

        public IRCResponse ReadLine()
        {
            try
            {
                if (_messages.Count > 0)
                {
                    return _messages.Dequeue();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                return null;
            }
        }
        #endregion

        #region Private
        #region Stream Read/Write
        private void ReadStreamThread()
        {
            try
            {
                // Only try to read if we haven't been disposed of
                while (_readThreadCanRun)
                {
                    if (IsConnected)
                    {
                        ReadStream();
                        Thread.Sleep(100);
                    }

                    // Assume we can run but connection lost - try to reconnect
                    else if (!_isConnecting)
                    {
                        Connect(5000);
                    }
                }
                // No longer allowed to run, exit
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void ReadStream()
        {
            try
            {
                string inputLine = _reader.ReadLine();

                if (!string.IsNullOrWhiteSpace(inputLine))
                {
                    Console.WriteLine("IRC - " + inputLine);

                    // Split the lines sent from the server by spaces to parse them
                    string[] splitInput = inputLine.Split(new Char[] { ' ' });

                    if (splitInput[0] == "PING")
                    {
                        string pongReply = splitInput[1];
                        Write("PONG " + pongReply);

                        return; //Shouldn't have to do any more work if this was a reply to a ping
                    }

                    switch (splitInput[1].ToLower())
                    {
                        // Raw input from server - 001 is first one so use as indication of successful connect
                        case "001":
                            if (!String.IsNullOrWhiteSpace(_nickPassword))
                            {
                                Write("PRIVMSG NickServ Identify " + _nickPassword);

                                // Wait for identify to go through
                                Thread.Sleep(5000);
                            }

                            _connected = true;
                            break;
                        case "353":
                            HandleNamesInput(inputLine);
                            break;
                        case "notice":
                            HandleNoticeInput(splitInput);
                            break;
                        case "336": // End of /NAMES list
                        case "join":
                            break;
                        case "privmsg":
                            HandlePrivMsgInput(splitInput);
                            break;
                        default:
                            Console.WriteLine("Unhandled input type: " + inputLine);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
            }
        }

        private void Write(string message)
        {
            try
            {
                if (IsConnected)
                {
                    Console.WriteLine(message);
                    _writer.WriteLine(message);
                    _writer.Flush();
                }
                else
                {
                    throw new Exception("Connection to IRC server has been lost.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
            }
        }
        #endregion

        #region IRC Input Handlers
        private void HandleNamesInput(string inputLine)
        {
            foreach (string channel in _channels)
            {
                string[] stringSeparators = new string[] { channel.ToLower() };
                string[] splitInput = inputLine.ToLower().Split(stringSeparators, StringSplitOptions.None);

                if (splitInput.Length == 2)
                {
                    int length = splitInput[0].Length + channel.Length + 2;
                    string names = inputLine.Substring(length);
                    string[] namesArr = names.Trim().Trim(':').Split(' ');

                    StringBuilder sb = new StringBuilder();
                    foreach (string name in namesArr)
                    {
                        sb.Append(name).Append(", ");
                    }

                    string sendString = "People currently in " + channel + " : ";
                    _messages.Enqueue(new IRCResponse(sendString + sb.ToString().Trim().TrimEnd(',')));

                    break;
                }
            }
        }

        // Queue up a new message to be read if from whitelist channel
        private void HandlePrivMsgInput(string[] splitInput)
        {
            string channelName = splitInput[2].Trim(), 
                action = "", 
                message = "", 
                username = GetUserFromHost(splitInput[0]);

            // Make sure we are supposed to be monitoring the channel the message came from
            if (!AllowedToListenToChannel(channelName)) return;

            // If we're long enough, it could be that our incoming message contains
            // an action rather than just a long message, so check for this
            if (splitInput.Length > 4)
            {
                action = Common.StripIRCColours(splitInput[3].Trim());

                if (action == ":ACTION")
                {
                    message = splitInput.GetStringFromElements(4);
                    _messages.Enqueue(new IRCResponse(channelName, username, message, IRCResponseMessageType.UserAction));
                    return;
                }
            }

            // Assume we didn't break from any of the action checks above
           // just queue up an ordinary message
            message = splitInput.GetStringFromElements(3).TrimStart(':');
            _messages.Enqueue(new IRCResponse(channelName, username, message));
        }

        private void HandleNoticeInput(string[] splitInput)
        {
           
        }
        #endregion

        #region Internal Helpers
        // Check the passed in channel name exists in the list of channels
        // we're supposed to be monitoring
        private bool AllowedToListenToChannel(string channelName) => _channels.Contains(channelName.ToLower());

        // Get the username from a host
        private string GetUserFromHost(string host) => host.Trim().Split('!')[0].TrimStart(':');
        #endregion
        #endregion

        #region Disposing
        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose of managed resources
                    _readThreadCanRun = false;

                    if (_writer != null)
                        _writer.Dispose();

                    if (_reader != null)
                        _reader.Dispose();

                    if (_ircNetworkSream != null)
                        _ircNetworkSream.Dispose();

                    if (_ircClient != null)
                        _ircClient.Close();

                    _messages = null;
                    _channels = null;
                }

                // Dispose of unmanaged resources
            }
        }

        ~IRCClient()
        {
            Dispose(false);
        }
        #endregion
    }
}
