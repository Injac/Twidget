using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwidgetApi.Model
{
    /// <summary>
    /// Contains the image data
    /// and the tweet.
    /// </summary>
    public class MediaTweet
    {
        public string Status
        {
            get;
            set;
        }

        public byte[] ImageData
        {
            get;
            set;
        }
    }
}
