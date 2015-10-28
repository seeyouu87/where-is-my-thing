using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.Text;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MyThingApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private List<MyThingObj> existItemList;
        private StorageFile file;
        public MainPage()
        {
            this.InitializeComponent();
            DataContext = new MyThingObj();
            LoadData();
            
            PickAFileButton.Click += new RoutedEventHandler(PickAFileButton_Click);
            button.Click += UpdateButton_Click;
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            MyThingObj data = new MyThingObj();
            
            byte[] fileBytes = null;
            if (file != null)
            {
                using (IRandomAccessStreamWithContentType stream = await file.OpenReadAsync())
                {

                    fileBytes = new byte[stream.Size];

                    using (DataReader reader = new DataReader(stream))
                    {
                        await reader.LoadAsync((uint)stream.Size);

                        reader.ReadBytes(fileBytes);

                    }

                }
            }
            else
            {
                data.image = existItemList[0].image;
            }
            data.rfid = rfid.Text;
            data.name = itemName.Text;
            data.image = fileBytes;
            data.lastUpdatedAt = DateTime.Now;
            data.createdAt = existItemList[0].createdAt;
            data.type = type.Text;
            data.status = "1";
            data.location = existItemList[0].location;
            data.id = int.Parse(id.Text);

            HttpWebRequest Addrequest = (HttpWebRequest)WebRequest.Create("http://mythingapi.azurewebsites.net/api/MyThings/"+id.Text);

            Addrequest.Method = "PUT";
            Addrequest.ContentType = "application/json;charset=utf-8";
            Addrequest.Accept = "application/json;charset=utf-8";

            Addrequest.BeginGetRequestStream((ar) =>
            {
                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                byte[] bytes = encoding.GetBytes(JsonConvert.SerializeObject(data));
                Stream arStream = Addrequest.EndGetRequestStream(ar);
                arStream.Write(bytes, 0, bytes.Length);
                Addrequest.BeginGetResponse((arRes) =>
                {
                    HttpWebResponse thingresponse = (HttpWebResponse)Addrequest.EndGetResponse(arRes);
                }, Addrequest);
            }, Addrequest);
        }

        private async void LoadData() {
            //load existing data
            
            HttpWebRequest getrequest = (HttpWebRequest)HttpWebRequest.Create("http://mythingapi.azurewebsites.net/api/MyThings");

            getrequest.Method = "GET";
            getrequest.ContentType = "application/json;charset=utf-8";
            var data = await getrequest.GetResponseAsync();
            Stream mystream = data.GetResponseStream();
            StreamReader sr = new StreamReader(mystream);
            string returnval = sr.ReadToEnd();
            existItemList = JsonConvert.DeserializeObject<List<MyThingObj>>(returnval);
            
            if (existItemList.Count > 0)
            {
                this.Frame.DataContext = existItemList[0];
                id.Text = existItemList[0].id.ToString();
                itemName.Text = existItemList[0].name != null ? existItemList[0].name : string.Empty;
                rfid.Text = existItemList[0].rfid!=null? existItemList[0].rfid:string.Empty;
                type.Text = existItemList[0].type != null ? existItemList[0].type:string.Empty;
                Location.Text = "Location: " + existItemList[0].location;
                if (existItemList[0].image != null)
                { 
                    BitmapImage img = new BitmapImage();
                    using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                    {
                        await stream.WriteAsync(existItemList[0].image.AsBuffer());
                        stream.Seek(0);
                        await img.SetSourceAsync(stream);
                    }
                    image.Source = img as ImageSource;
                }
            }
        }

        private async void PickAFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous returned file name, if it exists, between iterations of this scenario
            OutputTextBlock.Text = "";

            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                OutputTextBlock.Text = "Picked photo: " + file.Name;
            }
            else
            {
                OutputTextBlock.Text = "Operation cancelled.";
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

            public DateTime createdAt { get; set; }

            public DateTime lastUpdatedAt { get; set; }

        }

        private void searchText_KeyUp(object sender, KeyRoutedEventArgs e)
        {

        }
    }
}
