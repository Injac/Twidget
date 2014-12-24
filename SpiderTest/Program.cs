using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Net;
using System.Text;
using Microsoft.SPOT.Time;
using System.Net.Sockets;
using Microsoft.SPOT.Hardware;
using System.Xml.Serialization;
using System.IO;
using SpiderTest.uPLibrary.Utilities;
using SpiderTest.Networking;


namespace SpiderTest
{
    public partial class Program
    {
        //Used to check network avialability
        static bool networkAvailable;

        // This method is run when the mainboard is powered up or reset.
        void ProgramStarted()
        {

            Debug.Print("Program Started");

            InitEthernet();

            while (!networkAvailable)
            {
                Thread.Sleep(250);
            }

            //Set the time on the device. Important for all kinds
            //of requests, epecially for SSH requests. Dond't
            //Forget to update your SSL Seed.
            Utility.SetLocalTime(Time.GetNetworkTime());




            //SendTweetWithTextOnly( "Last test tweet before writing the blog-post. #devlife #Gadgeteer");

            var imageData = Resources.GetBytes(Resources.BinaryResources.iron);

            SendTweetWithImage("Ok. A last one with image. #Gadgeteer", imageData);

        }

        #region Tweet API Client Side Methods
        /// <summary>
        /// Sends the tweet with text only.
        /// </summary>
        /// <param name="tweetText">The tweet text.</param>
        private static void SendTweetWithTextOnly(string tweetText)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://[YOURHOST]/api/Twidget/TextOnly");
                byte[] data = Encoding.UTF8.GetBytes(tweetText);

                request.Method = "POST";
                request.ContentLength = data.Length;

                using (var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(data, 0, data.Length);
                }

                var response = request.GetResponse().GetResponseStream();



                if (response.CanRead && response.Length > 1)
                {
                    byte[] buffer = null;
                    buffer = new byte[response.Length];

                    if (response.CanSeek)
                    {
                        response.Position = 0;
                    }

                    response.Read(buffer, 0, (int)response.Length);

                    var responseText = new String(Encoding.UTF8.GetChars(buffer));

                    Debug.Print("Server Response: " + responseText);

                }

                response.Close();
            }

            catch (Exception ex)
            {

                Debug.Print(ex.ToString());
            }

        }

        /// <summary>
        /// Sends the tweet with image.
        /// </summary>
        /// <param name="tweetText">The tweet text.</param>
        /// <param name="imageData">The image data.</param>
        private static void SendTweetWithImage(string tweetText, byte[] imageData)
        {

            Hashtable valuesStore = new Hashtable();
            valuesStore.Add("TweetText", tweetText);

            try
            {
                var request = RawRequests.BuildServerRequest("https://[YOURHOST]/api/Twidget/WithImage",
                              ref imageData,
                              "TweetImage",
                              "image/jpg",
                              "TweetImage.jpg",
                              valuesStore,
                              "test", "test"); //user auth not used in this case

                var response = request.GetResponse().GetResponseStream();




                if (response.CanRead && response.Length > 1)
                {
                    byte[] buffer = null;
                    buffer = new byte[response.Length];

                    if (response.CanSeek)
                    {
                        response.Position = 0;
                    }

                    response.Read(buffer, 0, (int)response.Length);

                    var responseText = new String(Encoding.UTF8.GetChars(buffer));

                    Debug.Print("Server Response: " + responseText);

                }

                response.Close();

            }

            catch (System.Net.WebException ex)
            {

                Debug.Print(ex.ToString());
            }



        }
        #endregion



        #region Network Initialization

        /// <summary>
        /// Initializes the ethernet.
        /// </summary>
        private void InitEthernet()
        {
            //Works best for me
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            ethernetJ11D.NetworkUp += ethernetJ11D_NetworkUp;
            ethernetJ11D.UseThisNetworkInterface();
            ethernetJ11D.NetworkInterface.EnableStaticIP("192.168.178.32", "255.255.255.0", "192.168.178.1");
            ethernetJ11D.NetworkSettings.EnableStaticDns(new string[] { "192.168.178.1" });
            //ethernetJ11D.UseDHCP();
            //ethernetJ11D.NetworkInterface.EnableDynamicDns();

        }

        /// <summary>
        /// Ethernets the J11 d_ network up.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="state">The state.</param>
        void ethernetJ11D_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network up!");
        }

        /// <summary>
        /// Handles the NetworkAddressChanged event of the NetworkChange control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            Debug.Print("Network Address changed!");
            Debug.Print("Current Network address:");
            Debug.Print(ethernetJ11D.NetworkInterface.IPAddress);
        }

        /// <summary>
        /// Handles the NetworkAvailabilityChanged event of the NetworkChange control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="NetworkAvailabilityEventArgs"/> instance containing the event data.</param>
        void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            networkAvailable = true;
        }
        #endregion


    }




}
