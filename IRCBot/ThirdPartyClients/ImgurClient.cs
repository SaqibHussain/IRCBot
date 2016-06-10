using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

using IRCBot.Models;

using RestSharp;
using Newtonsoft.Json;

namespace IRCBot.ThirdPartyClients
{
    class ImgurClient
    {
        // access_token: is your secret key used to access the user's data. It can be thought of the user's password and username combined into one, and is used to access the user's account. It expires after 1 month.
        private string _accessToken;

        // refresh_token: is used to request new access_tokens.Since access_tokens expire after 1 month, we need a way to request new ones without going through the entire authorization step again. It does not expire.
        private string _refreshToken = null;

        // Client Id and secret are needed to request a new access_token from imgur
        private string _clientId, _clientSecret;

        #region Constructors
        /// <summary>
        /// Constructor with an already known access token
        /// </summary>
        /// <param name="clientId">Your Application ID</param>
        /// <param name="clientSecret">The client_secret for the application</param>
        /// <param name="refreshToken">The refresh token returned from the authorization code exchange</param>
        /// <param name="accessToken">Access token to user data</param>
        public ImgurClient(string clientId, string clientSecret, string refreshToken, string accessToken)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _refreshToken = refreshToken;
            _accessToken = accessToken;
        }

        /// <summary>
        /// Constrcutor for an unknown access token. Will automatically get a new access token upon instantiating.
        /// </summary>
        /// <param name="clientId">Your Application ID</param>
        /// <param name="clientSecret">The client_secret for the application</param>
        /// <param name="refreshToken">The refresh token returned from the authorization code exchange</param>
        public ImgurClient(string clientId, string clientSecret, string refreshToken)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _refreshToken = refreshToken;
            GetNewAccessToken();
        }
        #endregion

        /// <summary>
        /// Uploads a new image
        /// </summary>
        /// <param name="imageBytes">Image to be uploaded in byte array</param>
        /// <returns></returns>
        public ImgurUploadResponse Upload(byte[] imageBytes)
        {
            try
            {
                // Try to upload the image three times
                int triesRemaining = 3;
                while (triesRemaining > 0)
                {
                    using (var webClient = new WebClient())
                    {
                        // Throw our access token into the header
                        webClient.Headers.Add("Authorization", $"Bearer {_accessToken}");

                        // The image to be uploaded needs be passed in the URL - put it in a NVC to make this easier
                        var values = new NameValueCollection();
                        // Imgur needs images to be base64 data
                        values.Add("image", Convert.ToBase64String(imageBytes));

                        try
                        {
                            // Upload the image and await a response
                            byte[] response = webClient.UploadValues("https://api.imgur.com/3/image", values);

                            // Deserialise response to get url and id
                            string str = Encoding.UTF8.GetString(response);
                            ImgurResponse data = JsonConvert.DeserializeObject<ImgurResponse>(str);
                            string imageUrl = data.data.link;
                            string imageId = data.data.id;

                            // If we have managed to upload a new image to imgur then record the upload time and url so that we can delete it after some time
                            //TODO: Move this out of this class
                            //File.AppendAllText("ImgurUploads.txt", DateTime.Now + "!:!" + imageId);

                            return new ImgurUploadResponse(imageUrl, imageId);
                        }
                        catch
                        {
                            triesRemaining--;

                            // The reason for failing at this point could be that the access token has expired.
                            GetNewAccessToken();
                        }
                    }
                }
                return new ImgurUploadResponse(false, "Retry limit uploading image to Imgur exceeded.");
            }
            catch (Exception ex)
            {
                return new ImgurUploadResponse(false, $"Error while uploading image to Imgur. Error was: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes image with the given image id
        /// </summary>
        /// <param name="imageId">Id of the image to be deleted</param>
        /// <returns></returns>
        public bool Delete(string imageId)
        {
            try
            {
                // Try to delete three times
                int triesRemaining = 3;
                while (triesRemaining > 0)
                {
                    RestClient client = new RestClient("https://api.imgur.com/3/image");
                    RestRequest request = new RestRequest("/" + imageId, Method.DELETE);
                    request.AddHeader("Authorization", "Bearer " + _accessToken);

                    try
                    {
                        RestResponse response = (RestResponse)client.Execute(request);
                        //var content = response.Content; // raw content as string
                        return true;
                    }
                    catch
                    {
                        triesRemaining--;
                        GetNewAccessToken();
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Requests a new access token from Imgur
        /// </summary>
        private void GetNewAccessToken()
        {
            string Url = "https://api.imgur.com/oauth2/token?";
            string DataTemplate = "client_id={0}&client_secret={1}&grant_type=refresh_token&refresh_token={2}";
            string Data = String.Format(DataTemplate, _clientId, _clientSecret, _refreshToken);

            using (WebClient Client = new WebClient())
            {
                try
                {
                    Client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                    string ApiResponse = Client.UploadString(Url, Data);

                    // Use some random JSON Parser, you´ll get access_token and refresh_token
                    var Deserializer = new JavaScriptSerializer();
                    var Response = Deserializer.DeserializeObject(ApiResponse) as Dictionary<string, object>;

                    //_refreshToken = Convert.ToString(Response["refresh_token"]);
                    _accessToken = Convert.ToString(Response["access_token"]);

                    //TODO: Find a better way to log this
                    //File.WriteAllText("ImgurRefreshToken.txt", _refreshToken);
                    //File.WriteAllText("AccessToken.txt", _accessToken);
                }
                catch
                {
                    throw;
                }
            }
        }
    }
}
