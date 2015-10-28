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
        private const string BedroomID = "aerwwsdfxx-aasdzzz";//find your Arduino Device ID located in bedroom
        private const string LivingroomID = "aeraexx-aasdxxx";//find your Arduino Device ID located in living room

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
                    switch (info.Id.ToString())
                    {
                        case BedroomID:
                            await ReadAsyncBedroom(ReadCancellationTokenSource.Token);
                            break;
                        case LivingroomID:
                            await ReadAsyncBedroom(ReadCancellationTokenSource.Token);
                            break;
                    }

                }

            }
            catch
            {
                //don't think need to write error log at the moment
            }
        }

        private async Task ReadAsyncBedroom(CancellationToken cancellationToken)
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
                    sendRFID(this.rfidTags, "bedroom");
                }

            }
            catch
            {
                //don't think need to write error log at the moment
            }
        }

        private async Task ReadAsyncLivingroom(CancellationToken cancellationToken)
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
                    sendRFID(this.rfidTags, "living room");
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
        private void sendRFID(string myrfid, string loc)
        {
            try
            {

                List<string> rfidList = JsonConvert.DeserializeObject<List<string>>(myrfid);
                //get all clothes records in database:
                HttpWebRequest getrequest = (HttpWebRequest)HttpWebRequest.Create("http://mythingapi.azurewebsites.net/api/MyThings");

                getrequest.Method = "GET";
                getrequest.ContentType = "application/json;charset=utf-8";

                getrequest.BeginGetResponse((x) =>
                {
                    HttpWebResponse response = (HttpWebResponse)getrequest.EndGetResponse(x);
                    Stream mystream = response.GetResponseStream();
                    StreamReader sr = new StreamReader(mystream);
                    string returnval = sr.ReadToEnd();
                    List<MyThingObj> existingItemList = JsonConvert.DeserializeObject<List<MyThingObj>>(returnval);

                    if (existingItemList.Count < rfidList.Count)
                    {
                        //find the missing rfid:
                        foreach (string rfDetected in rfidList)
                        {
                            if (existingItemList.FindAll(o=>o.rfid.Equals(rfDetected)).Count>0)
                            {
                                continue;
                            }
                            else
                            {
                                //add new rfid
                                string itembody = JsonConvert.SerializeObject(new MyThingObj()
                                {
                                    rfid = rfDetected,
                                    status = "1",
                                    location =loc
                                });
                                HttpWebRequest Addrequest = (HttpWebRequest)HttpWebRequest.Create("http://mythingapi.azurewebsites.net/api/MyThings");

                                Addrequest.Method = "POST";
                                Addrequest.ContentType = "application/json;charset=utf-8";

                                Addrequest.BeginGetRequestStream((ar) =>
                                {
                                    System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                                    byte[] bytes = encoding.GetBytes(itembody);
                                    Stream arStream = Addrequest.EndGetRequestStream(ar);
                                    arStream.Write(bytes, 0, bytes.Length);
                                    Addrequest.BeginGetResponse((arRes) =>
                                    {
                                        HttpWebResponse thingresponse = (HttpWebResponse)Addrequest.EndGetResponse(arRes);

                                    }, Addrequest);
                                }, Addrequest);
                            }
                        }
                    }
                    else if (existingItemList.Count > rfidList.Count)
                    {
                        string itembody = string.Empty;
                        foreach (MyThingObj myThing in existingItemList)
                        {
                            if (rfidList.Contains(myThing.rfid)) {
                                //new clt is added:
                                itembody = JsonConvert.SerializeObject(new MyThingObj()
                                {
                                    rfid = myThing.rfid,
                                    status = "0",
                                    location = loc
                                });

                            }
                            else {
                                itembody = JsonConvert.SerializeObject(new MyThingObj()
                                {
                                    rfid = myThing.rfid,
                                    status = "0",
                                    location = "missing"
                                });
                            }
                            HttpWebRequest clothrequest = (HttpWebRequest)HttpWebRequest.Create("http://mythingapi.azurewebsites.net/api/MyThings/" + myThing.id);

                            clothrequest.Method = "PUT";
                            clothrequest.ContentType = "application/json;charset=utf-8";

                            clothrequest.BeginGetRequestStream((a) =>
                            {

                                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();


                                byte[] bytes = encoding.GetBytes(itembody);

                                Stream requestStream = clothrequest.EndGetRequestStream(a);
                                // Send the data.
                                requestStream.Write(bytes, 0, bytes.Length);
                            }, clothrequest);

                            //after sending data, get the response
                            clothrequest.BeginGetResponse((cr) =>
                            {
                                HttpWebResponse res = (HttpWebResponse)clothrequest.EndGetResponse(cr);

                            }, null);
                        }
                    }
                    

                }, null);
            }
            catch
            {
                //have no idea what to catch, but this process cannot fail and halt the background app.
            }
        }

        private class MyThingObj
        {
            public int id { get; set; }

            public String status { get; set; }

            public String rfid { get; set; }

            public Byte[] image { get; set; }

            public String name { get; set; }

            public String type { get; set; }

            public String location { get; set; }

            public DateTime lastUpdatedAt { get; set; }

        }

    }
}
