using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Tweetinvi;
using Tweetinvi.Core.Interfaces;
using TwidgetApi.CustomFilters;
using TwidgetApi.Model;

namespace TwidgetApi.Controllers
{
    //[RequireHttps]
    public class TwidgetController : ApiController
    {


        public TwidgetController()
        {
            ///TODO:INSERT YOUR DATA FROM TWITTER HERE
            TwitterCredentials.SetCredentials(
                "USERACCESS_TOKEN",
                "USERACCESS_SECRET",
                "CONSUMER_KEY",
                "CONSUMER_SECRET");
        }

        [HttpPost]
        [ActionName("api/Twidget/TextOnly")]
        public async Task<string> UpdateStatus(HttpRequestMessage request)
        {

            var status = await request.Content.ReadAsStringAsync();

            var tweet = Tweet.PublishTweet(status);

            if (tweet != null)
            {
                return tweet.IdStr;
            }

            return default(string);
        }

        [HttpPost]
        [Route("api/Twidget/WithImage")]
        public async Task<object> UpdateStatusWithMedia()
        {

            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }



            var provider = new MultipartMemoryStreamProvider();

            string tweetText = default(string);

            byte[] imageData = null;



            try
            {
                await Request.Content.ReadAsMultipartAsync<MultipartMemoryStreamProvider>(new MultipartMemoryStreamProvider()).ContinueWith(async (tsk) =>
                {
                    MultipartMemoryStreamProvider prvdr = tsk.Result;




                    foreach (HttpContent ctnt in prvdr.Contents)
                    {
                        var header = ctnt.Headers.FirstOrDefault(h=>h.Key.Equals("Content-Disposition"));



                        if (header.Key != null && header.Value != null)
                        {

                            var enumerator = header.Value.GetEnumerator();
                            enumerator.MoveNext();
                            var value = enumerator.Current;

                            if (value.Contains("TweetImage"))
                            {
                                // You would get hold of the inner memory stream here
                                System.IO.Stream stream = await ctnt.ReadAsStreamAsync();
                                imageData = ReadFully(stream);

                                continue;


                            }

                            if (value.Contains("TweetText"))
                            {
                                tweetText = await ctnt.ReadAsStringAsync();
                                continue;
                            }
                        }


                    }
                });


                if (tweetText != null && imageData != null)
                {
                    if (imageData.Length > 10 && tweetText.Length > 10)
                    {
                        var tweet = Tweet.PublishTweetWithMedia(tweetText, imageData);

                        if (tweet != null)
                        {
                            return tweet.IdStr;
                        }
                    }
                }

                return default(string);
            }

            catch (System.Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }


        }


        public static byte[] ReadFully(System.IO.Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

    }
}
