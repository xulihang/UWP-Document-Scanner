using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using System.Diagnostics;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWP_DWT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private List<string> OpenIPList { get; set; }
        public MainPage()
        {
            this.InitializeComponent();
            OpenIPList = new List<string>();
            WebView1.Navigate(new Uri("ms-appx-web:///DWT/index.html"));
        }

        private async void CameraButton_Click(object sender, RoutedEventArgs e)
        {
            // Using Windows.Media.Capture.CameraCaptureUI API to capture a photo
            CameraCaptureUI dialog = new CameraCaptureUI();
            dialog.VideoSettings.AllowTrimming = true;
            StorageFile file = await dialog.CaptureFileAsync(CameraCaptureUIMode.Photo);
            string base64 = await StorageFileToBase64(file);
            await WebView1.InvokeScriptAsync("LoadImageFromBase64", new string[] { base64 });
            Debug.WriteLine(base64);

        }

        //https://stackoverflow.com/questions/18553691/metro-getting-the-base64-string-of-a-storagefile
        private async Task<string> StorageFileToBase64(StorageFile file)
        {
            string Base64String = "";

            if (file != null)
            {
                IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
                var reader = new DataReader(fileStream.GetInputStreamAt(0));
                await reader.LoadAsync((uint)fileStream.Size);
                byte[] byteArray = new byte[fileStream.Size];
                reader.ReadBytes(byteArray);
                Base64String = Convert.ToBase64String(byteArray);
            }

            return Base64String;
        }

        private void WebView1_LoadCompleted(object sender, NavigationEventArgs e)
        {

        }

        private List<string> GetLocalIPList()
        {
            List<string> IPs = new List<string>();
            foreach (HostName localHostName in NetworkInformation.GetHostNames())
            {
                if (localHostName.IPInformation != null)
                {
                    if (localHostName.Type == HostNameType.Ipv4)
                    {
                        IPs.Add(localHostName.RawName);
                    }
                }
            }
            return IPs;
        }

        private void DetectOpenRemoteScanPorts()
        {
            foreach (string localIP in GetLocalIPList())
            {
                //ip: 192.168.8.65
                string prefix = localIP.Substring(0, localIP.LastIndexOf(".") + 1);
                for (int i = 1; i <= 255; i++)
                {
                    string IP = prefix + i;
                    AppendIPIfOpen(IP);
                }
            }
        }

        private async void AppendIPIfOpen(string ip)
        {
            HttpClient httpClient = new HttpClient();
            //how to set time out: https://stackoverflow.com/questions/19535004/windows-web-http-httpclient-timeout-option
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(200));
            Uri requestUri = new Uri("http://"+ip+":18622");
            
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            bool isOpen = false;
            try
            {
                httpResponse = await httpClient.GetAsync(requestUri).AsTask(cts.Token);
                isOpen = true;
                Debug.WriteLine("http://" + ip + ":18622 is open.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine("http://" + ip + ":18622 is not open.");
            }
            if (isOpen) {
                OpenIPList.Add(ip);
            }
        }
    }
}
