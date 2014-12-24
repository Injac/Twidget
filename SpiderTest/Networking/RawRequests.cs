using System;
using Microsoft.SPOT;
using System.Net;
using System.Collections;
using System.IO;

namespace SpiderTest.Networking
{
    public static class RawRequests
    {
        /// <summary>
        /// Builds the server request (like simulating a web-form)
        /// for multipart/form-data. This means you can send
        /// files, or for example form values (like text) to
        /// your API backend. This is used to upload the image and
        /// text for a tweet from the device to the service.
        /// The main version was found here:
        /// https://www.ghielectronics.com/community/forum/topic?id=1010&page=2#msg29842
        /// I just changed the "KeyValueStore" property to a Hashtable (in the signature)
        /// to make it work. Original credits go to Lance.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="fileBytes">The file bytes.</param>
        /// <param name="fileParamName">Name of the file parameter.</param>
        /// <param name="fileContentType">Type of the file content.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="formValues">The form values.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public static HttpWebRequest BuildServerRequest(string url, ref byte[] fileBytes, string fileParamName, string fileContentType, string fileName
                , Hashtable formValues, string username, string password)
        {



            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString();
            byte[] boundarybytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundary + "\r\n");

            string fileHeader = "";
            byte[] fileHeaderBytes = null;
            byte[] fileTrailer = null;

            ArrayList formItems = new ArrayList();
            int requestLen = 0;

            foreach (DictionaryEntry kv in formValues)
            {
                // string itemString = "Content-Disposition: form-data; name=\"" + kv.Key.ToString() + "\"\r\n\r\n" + kv.Value.ToString() + "\r\n";
                string itemString = "Content-Disposition: form-data; name=\"" + kv.Key.ToString() + "\"\r\n\r\n" + kv.Value.ToString();
                var itemBytes = System.Text.Encoding.UTF8.GetBytes(itemString);

                requestLen += itemBytes.Length;
                requestLen += boundarybytes.Length;

                formItems.Add(itemBytes);
            }


            if (fileBytes != null && fileBytes.Length > 0)
            {
                fileHeader = "Content-Disposition: form-data; name=\"" + fileParamName + "\"; filename=\"" + fileName + "\"\r\nContent-Type: " + fileContentType + "\r\n\r\n";
                fileHeaderBytes = System.Text.Encoding.UTF8.GetBytes(fileHeader);
                //fileTrailer = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
                fileTrailer = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");

                requestLen += boundarybytes.Length + fileHeaderBytes.Length + fileTrailer.Length + fileBytes.Length;
            }

            //var eof = System.Text.Encoding.UTF8.GetBytes();
            var eof = System.Text.Encoding.UTF8.GetBytes("\r\n");
            HttpWebRequest request = null;
            Stream requestStream = null;

            request = HttpWebRequest.Create(url) as HttpWebRequest;
            request.HttpsAuthentCerts = null;

            if (request.Headers["Content-Type"] != null)
            {
                request.Headers.Remove("Content-Type");
            }

            request.ContentType = "multipart/form-data; boundary=" + boundary;

            request.Method = "POST";
            // request.KeepAlive = true;
            request.ContentLength = requestLen;

            request.Expect = string.Empty;
            request.Timeout = 1000 * 5;
            request.ReadWriteTimeout = 1000 * 5;


            try
            {
                requestStream = request.GetRequestStream();

                foreach (var item in formItems)
                {
                    requestStream.Write(boundarybytes, 0, boundarybytes.Length);
                    var bytes = item as byte[];
                    requestStream.Write(bytes, 0, bytes.Length);
                }



                if (fileBytes != null && fileBytes.Length > 0)
                {
                    requestStream.Write(boundarybytes, 0, boundarybytes.Length);
                    requestStream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length);
                    int chunkSize = 1427; // any larger will cause an SSL request to fail

                    for (int i = 0; i < fileBytes.Length; i += chunkSize)
                    {
                        var toWrite = chunkSize;

                        if (i + 1 + chunkSize > fileBytes.Length)
                            toWrite = fileBytes.Length - i;

                        requestStream.Write(fileBytes, i, toWrite);
                    }

                    requestStream.Write(fileTrailer, 0, fileTrailer.Length);

                }



                requestStream.Close();
                requestStream.Dispose();
                requestStream = null;

            }

            catch (Exception ex)
            {
                if (request != null)
                    request.Dispose();

                request = null;

                Debug.Print(ex.ToString());
            }

            finally
            {
                if (requestStream != null)
                    requestStream.Dispose();
            }

            return request;
        }
    }
}
