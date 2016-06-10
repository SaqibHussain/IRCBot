using System;
using System.Diagnostics;
using System.Net;
using System.Collections.Generic;
using System.Threading;

using IRCBot.Models;

using Newtonsoft.Json;
using RestSharp;

namespace IRCBot.ThirdPartyClients
{
    public class TelegramClient : IDisposable
    {
        private RestClient _client;
        private string _url, _token, _clientID, _lastUpdateId = "";

        // Internal stopwatch to keep track of the last time
        // we requested anything from telegram to avoid spamming
        // them with requests
        // TODO: switch this to web stream instead???
        private Stopwatch _stopWatch;

        private Queue<TelegramReadResponse> _messages;

        private bool _readThreadCanRun, _disposed;


        #region Constructors
        public TelegramClient(string url, string token, string clientID)
        {
            _url = url;
            _clientID = clientID;
            _client = new RestClient(url);
            _token = token;
            _stopWatch = new Stopwatch();
            _stopWatch.Start();
            _messages = new Queue<TelegramReadResponse>();

            // Kick off a thread to constantly read from telegram and queue up messages
            _readThreadCanRun = true;
            Thread telegramReadThread = new Thread(new ThreadStart(ReadThreadRunner));
            telegramReadThread.Start();
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Read a message from the queue of messages from Telegram
        /// </summary>
        /// <returns></returns>
        public TelegramReadResponse Read()
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

        /// <summary>
        /// Send a new message to the Telegram chat
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendMessage(string message)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(message))
                    return;

                RestRequest request = new RestRequest("/bot" + _token + "/sendMessage", Method.POST);
                request.AddParameter("chat_id", _clientID);
                request.AddParameter("text", message);
                RestResponse response = (RestResponse)_client.Execute(request);
                //var content = response.Content; // raw content as string
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Constantly read from TG and place any incoming messages in a queue
        /// </summary>
        private void ReadThreadRunner()
        {
            try
            {
                // Check that we are not disposed of
                while (_readThreadCanRun)
                {
                    GetMessagesFromTelegram();
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Make telegram API call to get messages
        /// </summary>
        private void GetMessagesFromTelegram()
        {
            try
            {
                // Check more than 5 seconds have elapsed since we last requested anything
                TimeSpan ts = _stopWatch.Elapsed;
                if (ts.Seconds < 5)
                    return;

                RestRequest request = new RestRequest("/bot" + _token + "/getUpdates", Method.GET);

                //Add an update id if we have one to avoid getting lots of messages back again and again
                if (!String.IsNullOrEmpty(_lastUpdateId))
                    request.AddParameter("offset", (Int32.Parse(_lastUpdateId) + 1).ToString());

                // Execute the request
                RestResponse response = (RestResponse)_client.Execute(request);

                // Raw content as string
                var content = response.Content;

                if (String.IsNullOrWhiteSpace(content))
                    return;

                TelegramResponse data = JsonConvert.DeserializeObject<TelegramResponse>(content);

                // Verify something was deserialised and that the returned data is ok
                if (data == null || !data.ok)
                    return;

                foreach (TelegramResult result in data.result)
                {
                    try {
                        //Check the messages are coming from the correct chat group - We only work with one group at a time
                        if (result.message.chat.id == _clientID)
                        {
                            //Store the last update id for user on the next api call
                            _lastUpdateId = result.update_id;

                            string text = result.message.text;
                            string username = result.message.from.username;

                            TelegramPhoto[] photo = result.message.photo;

                            if (text != null)
                            {
                                _messages.Enqueue(new TelegramReadResponse(text, username));
                            }
                            else if (photo != null)
                            {
                                string filePath = GetPhotoFilePath(photo[photo.Length - 1].file_id);
                                byte[] imageBytes = GetPhoto(filePath);

                                if (imageBytes != null)
                                {
                                    _messages.Enqueue(new TelegramReadResponse(imageBytes));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.Write(ex.StackTrace);
                    }
                }

                // Restart the stopwatch to indicate a successful read from telegram
                _stopWatch.Restart();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                //return $"Error when trying to get messages from Telegram. Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Helper to get a file path from Telegram based on a file id
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        private string GetPhotoFilePath(string fileId)
        {
            try
            {
                //Make request to get file
                RestRequest photoRequest = new RestRequest("/bot" + _token + "/getFile", Method.GET);
                photoRequest.AddParameter("file_id", fileId);

                RestResponse response = (RestResponse)_client.Execute(photoRequest);
                var content = response.Content; // raw content as string

                if (String.IsNullOrEmpty(content))
                    return "";

                TelegramFileResponse photoData = JsonConvert.DeserializeObject<TelegramFileResponse>(content);

                if (!photoData.ok)
                    return "";

                return photoData.result.file_path;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                return "";
            }
        }

        /// <summary>
        ///  Helper to get a file from Telegram based on file path
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private byte[] GetPhoto(string filePath)
        {
            try {
                //string uri = "https://api.telegram.org/file";
                //uri += "/bot" + _token + "/" + filePath;
                string uri = $"{_url}/file/bot{_token}/{filePath}";

                byte[] imageBytes = null;

                using (var webClient = new WebClient())
                {
                    imageBytes = webClient.DownloadData(uri);
                }

                return imageBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                return null;
            }
        }
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
                    _messages = null;
                    _stopWatch.Stop();
                }

                // Dispose of unmanaged resources
            }
        }

        ~TelegramClient()
        {
            Dispose(false);
        }
        #endregion
    }
}
