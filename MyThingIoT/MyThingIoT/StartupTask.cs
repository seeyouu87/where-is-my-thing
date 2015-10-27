using System;
using Windows.ApplicationModel.Background;
using System.Threading.Tasks;
using System.Threading;
using Windows.Storage.Streams;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using System.Collections.ObjectModel;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace MyThingIoT
{
    public sealed class StartupTask : IBackgroundTask
    {
        private SerialDevice serialPort = null;
        DataReader dataReaderObject = null;

        private CancellationTokenSource ReadCancellationTokenSource;
        private string rfidTags
        {
            get; set;
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            //run core function to read and send tags over to Web API
            while (true)
            {//may consider using thread timer
                read_RFID();
            }
        }

        private async void read_RFID()
        {
            try
            {
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);
                foreach (DeviceInformation info in dis)
                {
                    serialPort = await SerialDevice.FromIdAsync(info.Id);
                    // Configure serial settings
                    serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                    serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                    serialPort.BaudRate = 9600;
                    serialPort.Parity = SerialParity.None;
                    serialPort.StopBits = SerialStopBitCount.One;
                    serialPort.DataBits = 8;

                    // Create cancellation token object to close I/O operations when closing the device
                    ReadCancellationTokenSource = new CancellationTokenSource();
                    await ReadAsync(ReadCancellationTokenSource.Token);

                }

            }
            catch
            {
                //don't think need to write error log at the moment
            }
        }

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            try
            {
                Task<UInt32> loadAsyncTask;

                uint ReadBufferLength = 1024;

                // If task cancellation was requested, comply
                cancellationToken.ThrowIfCancellationRequested();

                // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
                dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

                // Create a task object to wait for data on the serialPort.InputStream
                loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

                // Launch the task and wait
                UInt32 bytesRead = await loadAsyncTask;

                if (bytesRead > 0)
                {
                    //get the RFID tags returned by Arduino + RFID module
                    this.rfidTags = dataReaderObject.ReadString(bytesRead);
                    //run the send RFID function:
                    sendRFID(this.rfidTags);
                }

            }
            catch
            {
                //don't think need to write error log at the moment
            }
        }

        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }

        /// <summary>
        /// Process RFID data and send over to web api
        /// </summary>
        private void sendRFID(string myrfid)
        {
            try
            {

                List<string> rfidList = JsonConvert.DeserializeObject<List<string>>(myrfid);
                //get all clothes records in database:
                HttpWebRequest getrequest = (HttpWebRequest)HttpWebRequest.Create("http://smartwardrobe.azurewebsites.net/api/clothes");

                getrequest.Method = "GET";
                getrequest.ContentType = "application/json;charset=utf-8";

                getrequest.BeginGetResponse((x) =>
                {
                    HttpWebResponse response = (HttpWebResponse)getrequest.EndGetResponse(x);
                    Stream mystream = response.GetResponseStream();
                    StreamReader sr = new StreamReader(mystream);
                    string returnval = sr.ReadToEnd();
                    List<string> existingRFIDList = JsonConvert.DeserializeObject<List<string>>(returnval);

                    if (existingRFIDList.Count > rfidList.Count)
                    {
                        //cloth has been taken out
                        //find the missing rfid:
                        foreach (string existRFID in existingRFIDList)
                        {
                            if (rfidList.Contains(existRFID))
                            {
                                continue;
                            }
                            else
                            {
                                //add action
                                string actionbody = JsonConvert.SerializeObject(new ActionObj()
                                {
                                    rfid = existRFID,
                                    status = "1"
                                });
                                HttpWebRequest actionrequest = (HttpWebRequest)HttpWebRequest.Create("http://smartwardrobe.azurewebsites.net/api/actions");

                                actionrequest.Method = "POST";
                                actionrequest.ContentType = "application/json;charset=utf-8";

                                actionrequest.BeginGetRequestStream((ar) =>
                                {
                                    System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                                    byte[] bytes = encoding.GetBytes(actionbody);
                                    Stream arStream = actionrequest.EndGetRequestStream(ar);
                                    arStream.Write(bytes, 0, bytes.Length);
                                    actionrequest.BeginGetResponse((arRes) =>
                                    {
                                        HttpWebResponse actionresponse = (HttpWebResponse)actionrequest.EndGetResponse(arRes);

                                    }, actionrequest);
                                }, actionrequest);
                            }
                        }
                    }
                    else if (existingRFIDList.Count < rfidList.Count)
                    {
                        //new clt is added:
                        string clothbody = JsonConvert.SerializeObject(new ClothObj()
                        {
                            rfid = myrfid,
                            status = "0",
                            img = new byte[] { },
                            name = "test",
                            type = "dress",
                            color = "yellow",
                            createdAt = DateTime.Now,
                            lastUpdatedAt = DateTime.Now,
                            frequency = 0
                        });

                        HttpWebRequest clothrequest = (HttpWebRequest)HttpWebRequest.Create("http://smartwardrobe.azurewebsites.net/api/clothes/" + myrfid);

                        clothrequest.Method = "POST";
                        clothrequest.ContentType = "application/json;charset=utf-8";

                        clothrequest.BeginGetRequestStream((a) =>
                        {

                            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();


                            byte[] bytes = encoding.GetBytes(clothbody);

                            Stream requestStream = clothrequest.EndGetRequestStream(a);
                            // Send the data.
                            requestStream.Write(bytes, 0, bytes.Length);
                        }, clothrequest);

                        //after sending data, get the response
                        clothrequest.BeginGetResponse((cr) =>
                        {
                            HttpWebResponse clothresponse = (HttpWebResponse)clothrequest.EndGetResponse(cr);

                        }, null);
                    }
                    else
                    {
                        //all clothes are back inside wardrobe, update all status:
                        //add action
                        string actionbody = JsonConvert.SerializeObject(new ActionObj()
                        {
                            rfid = "",
                            status = "1"
                        });
                        HttpWebRequest actionrequest = (HttpWebRequest)HttpWebRequest.Create("http://smartwardrobe.azurewebsites.net/api/actions");

                        actionrequest.Method = "POST";
                        actionrequest.ContentType = "application/json;charset=utf-8";

                        actionrequest.BeginGetRequestStream((ar) =>
                        {
                            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                            byte[] bytes = encoding.GetBytes(actionbody);
                            Stream arStream = actionrequest.EndGetRequestStream(ar);
                            arStream.Write(bytes, 0, bytes.Length);
                            actionrequest.BeginGetResponse((arRes) =>
                            {
                                HttpWebResponse actionresponse = (HttpWebResponse)actionrequest.EndGetResponse(arRes);

                            }, actionrequest);
                        }, actionrequest);
                    }

                }, null);
            }
            catch
            {
                //have no idea what to catch, but this process cannot fail and halt the background app.
            }
        }

        private class ActionObj
        {
            public int id;
            public string status = "0";
            public string rfid;
            public DateTime createdAt = DateTime.Now;
        }


        //Generic object for Clothes
        private class ClothObj
        {
            public int id;
            public string status = "0";
            public string rfid;
            public Byte[] img;
            public string name;
            public string type;
            public string color;
            public DateTime createdAt = DateTime.Now;

            public DateTime lastUpdatedAt = DateTime.Now;

            public int frequency = 0;
        }

    }
}
