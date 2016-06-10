using System;
using System.Threading;

using IRCBot.Models;
using IRCBot.ThirdPartyClients;

namespace IRCBot
{
    public static class TelegramBot
    {

        private const string CTRLK = "\u0003";
        private const string CTRLB = "\u0002";

        private static ImgurClient _imgur;
        private static IRCClient _ircClient;
        private static TelegramClient _telegramClient;

        public static void Start()
        {

            try
            {
                InitialiseClients();
                StartThreads();

                while (true)
                {
                    Thread.Sleep(10000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                Console.ReadKey();
            }
            finally
            {
                // Dispose of any resources
                if (_ircClient != null)
                {
                    _ircClient.Dispose();
                    _ircClient = null;
                }

                if (_telegramClient != null)
                {
                    _telegramClient.Dispose();
                    _telegramClient = null;
                }
            }
        }

        /// <summary>
        ///  Helper to initialise all of the clients we will need
        /// </summary>
        private static void InitialiseClients()
        {
            _ircClient = new IRCClient(
                ClientConnectionSettings.IRC_SERVER, 
                ClientConnectionSettings.IRC_PORT, 
                ClientConnectionSettings.IRC_USER, 
                ClientConnectionSettings.IRC_NICK, 
                ClientConnectionSettings.IRC_PASSWORD);

            // Try and connect to the irc server with a 20 second timeout
            if (!_ircClient.Connect(20000))
            {
                throw new Exception("Failed to connect to IRC server.");
            }

            _ircClient.JoinChannel(ClientConnectionSettings.IRC_CHANNEL);

            _imgur = new ImgurClient(
                ClientConnectionSettings.IMGUR_CLIENT_ID,
                ClientConnectionSettings.IMGUR_CLIENT_SECRET, 
                ClientConnectionSettings.IMGUR_REFRESH_TOKEN);

            _telegramClient = new TelegramClient(
                ClientConnectionSettings.TELEGRAM_SERVER,
                ClientConnectionSettings.TELEGRAM_TOKEN,
                ClientConnectionSettings.TELEGRAM_CLIENT_ID);
        }

        /// <summary>
        ///  Helper to kick off all the threads to do their various tasks
        /// </summary>
        private static void StartThreads()
        {
            //Kick off thread to handle input from telegram
            Thread telegramHandlerThread = new Thread(new ThreadStart(TelegramHandlerThread));
            telegramHandlerThread.Start();

            //Kick off thread to handle input from irc
            Thread ircHandlerThread = new Thread(new ThreadStart(IrcHandlerThread));
            ircHandlerThread.Start();

            //Kick off thread to handle misc cleanup tasks
            Thread cleanupThread = new Thread(new ThreadStart(HandleCleanup));
            cleanupThread.Start();
        }

        #region IRC Handlers
        private static void IrcHandlerThread()
        {
            try
            {
                while (true)
                {
                    // Query for any messages from IRC messages queue
                    var ircResponse = _ircClient.ReadLine();

                    // Check if we got a response back
                    if (ircResponse != null)
                    {
                        // Check if the message is from the bot (i.e. a trigger response)
                        if (ircResponse.ResponseType == IRCResponseMessageType.ServiceMessage)
                        {
                            // Just send the message straight to telegram
                            _telegramClient.SendMessage(ircResponse.Message);
                        }
                        else if (ircResponse.ResponseType == IRCResponseMessageType.UserAction)
                        {
                            // Just send the message straight to telegram
                            _telegramClient.SendMessage(ircResponse.Message);
                        }
                        else if (ircResponse.ResponseType == IRCResponseMessageType.UserMessage)
                        {
                            // Parse the message
                            if (ircResponse.Message.StartsWith("!"))
                            {
                                HandleTrigger(ircResponse.Message);
                            }

                            _telegramClient.SendMessage($"{ircResponse.Username}: {ircResponse.Message}");

                        }
                    }

                    Thread.Sleep(250);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
            }
        }
        #endregion

        #region Telegram Handlers
        private static void TelegramHandlerThread()
        {
            try
            {
                while (true)
                {
                    var telegramResponse = _telegramClient.Read();

                    if (telegramResponse != null)
                    {
                        // Get username and add colours
                        string username = telegramResponse.Username;
                        string usernameWithColours = "\u0003" + "14" + username + "\u0003" + "\u0003" + "7" + "\u0002" + ":" + "\u0002" + "\u0003" + " ";

                        if (telegramResponse.IsImage)
                        {
                            string uploadUrl = _imgur.Upload(telegramResponse.ImageBytes).ImageUrl;
                            _ircClient.Write(ClientConnectionSettings.IRC_CHANNEL, usernameWithColours + uploadUrl);
                        }
                        else
                        {
                            string message = telegramResponse.Message;

                            if (message.StartsWith("/"))
                            {
                                HandleTelegramSlashTrigger(message);
                            }
                            else if (message.StartsWith("!"))
                            {
                                HandleTrigger(message);
                            }
                            else
                            {
                                _ircClient.Write(ClientConnectionSettings.IRC_CHANNEL, usernameWithColours + message);
                            }
                        }
                    }

                    Thread.Sleep(250);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
            }
        }

        private static void HandleTelegramSlashTrigger(string message)
        {
            // Send the trigger to irc without username because we may want to activate a trigger on another bot
            _ircClient.Write(ClientConnectionSettings.IRC_CHANNEL, message);

            try //In case we error trying to parse input from telegram
            {
                string[] splitString = message.Split(' ');
                switch (splitString[0])
                {
                    case "/names":
                        _ircClient.GetNames(ClientConnectionSettings.IRC_CHANNEL);
                        break;
                    case "/blockurls":
                    case "/urlblock":
                    case "/blockurlsfrom":
                        Thread blockNameThread = new Thread(() => HandleTelegramUserUrlBlockTrigger(splitString));
                        blockNameThread.Start();
                        break;
                    case "/addcensor":
                    case "/censor":
                    case "/censorword":
                        Thread censorThread = new Thread(() => HandleTelegramCensorTrigger(splitString));
                        censorThread.Start();
                        break;
                    default:
                        break;
                }
            }
            catch { }
        }

        private static void HandleTelegramUserUrlBlockTrigger(string[] splitInput)
        {
            //if (splitInput.Length < 2)
            //{
            //    SendToTelegram("You must give a username to block", false);
            //    return;
            //}

            //string username = "";
            //username = splitInput[1].Trim();

            //if (String.IsNullOrEmpty(username))
            //{
            //    SendToTelegram("You must give a username to block", false);
            //    return;
            //}

            //string s = System.IO.File.ReadAllText("BlockedUrlNames.txt");
            //s = s + "\n" + splitInput[1];
            //System.IO.File.WriteAllText("BlockedUrlNames.txt", s);
            //Common.Init();

            //SendToTelegram("All urls from " + username + " will be blocked.", false);
        }

        public static void HandleTelegramCensorTrigger(string[] splitInput)
        {
            //if (splitInput.Length < 3)
            //{
            //    SendToTelegram("You must give provide the censored word. E.g. /censor lemon l***n", false);
            //    return;
            //}

            //string word = splitInput[1].Trim();
            //string censoredWord = splitInput[2].Trim();

            //if (String.IsNullOrEmpty(word) || String.IsNullOrEmpty(censoredWord))
            //{
            //    SendToTelegram("Error with the input string. Try again.", false);
            //    return;
            //}

            //string s = System.IO.File.ReadAllText("Censor.txt");
            //s = s + "\n" + word + "," + censoredWord;
            //System.IO.File.WriteAllText("Censor.txt", s);
            //Common.Init();

            //SendToTelegram("Word has been censored", false);
        }
        #endregion

        private static void HandleTrigger(string message)
        {
            try //In case we error trying to parse input from irc
            {
                string[] splitString = message.Split(' ');
                switch (splitString[0])
                {
                    case "!ep":
                        // Because of the way eptrigger is written, we have to pass some IRC expected stuff at the start of the string for it to work properly - need to refactor
                        Thread tvThread = new Thread(() => TvSearchHandler(message.Substring(3)));
                        tvThread.Start();
                        break;
                    case "!imdb":
                        // Because of the way eptrigger is written, we have to pass some IRC expected stuff at the start of the string for it to work properly - need to refactor
                        Thread imdbThread = new Thread(() => ImdbSearchHandler(message.Substring(5)));
                        imdbThread.Start();
                        break;
                    default:
                        break;
                }
            }
            catch { }
        }

        private static void HandleCleanup()
        {
            try
            {
                //string[] uploads = File.ReadAllLines("ImgurUploads.txt");

                //foreach (string upload in uploads)
                //{
                //    string[] split = upload.Split(new string[] { "!:!" }, StringSplitOptions.RemoveEmptyEntries);

                //    if (split.Length < 2)
                //        continue;

                //    string dateTime = split[0];
                //    string imageId = split[1];

                //    DateTime parsedDateTime = DateTime.Parse(dateTime);

                //    if (parsedDateTime.AddHours(1) < DateTime.Now)
                //        continue;

                //    _imgur.Delete(imageId);
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while performing cleanup");
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
            }
        }

        private static void ImdbSearchHandler(string queryTerm)
        {

        }

        private static void TvSearchHandler(string queryTerm)
        {
            TVMazeClient tvMaze = new TVMazeClient();
            var result = tvMaze.Serach(queryTerm);
            if (result.Found)
            {
                TvMazeShow show = result.Show;
                TvMazeEpisode lastEp = show.PrevEpisode;
                TvMazeEpisode nextEp = show.NextEpisode;
                string line1 = $" 14{show.name} 15-- {Common.GenerateCommaList(show.schedule?.days)} {show.schedule?.time}  8(14{show.status}8) (14{show.network?.name}8)";
                string line2 = $"{CTRLK}14{CTRLB}Prev Episode {CTRLB}{CTRLK}15-- {lastEp?.airdate} {CTRLK}08({CTRLK}14{lastEp?.season}{CTRLK}08x{CTRLK}14{lastEp?.number}{CTRLK}08){CTRLK}14 - {lastEp?.name}";
                DateTime airing = Convert.ToDateTime(nextEp?.airstamp);
                TimeSpan span = airing.Subtract(DateTime.UtcNow);
                string line3 = $"{CTRLK}14{CTRLB}Next Episode {CTRLB}{CTRLK}15-- {nextEp?.airdate} {CTRLK}08({CTRLK}14{nextEp?.season}{CTRLK}08x{CTRLK}14{nextEp?.number}{CTRLK}08){CTRLK}14 - {nextEp?.name} - Airing in: {CTRLK}08{span.Days}{CTRLK}14 days {CTRLK}08{span.Hours}{CTRLK}14 hours {CTRLK}08{span.Minutes}{CTRLK}14 minutes {CTRLK}08{span.Seconds}{CTRLK}14 seconds";
                Write(line1); Write(line2); Write(line3);
            }
            else
            {
                Write(result.Message);
            }
        }

        private static void Write(string message)
        {
            _ircClient.Write(ClientConnectionSettings.IRC_CHANNEL, message);
            _telegramClient.SendMessage(Common.StripColours(message));
        }
    }
}
