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
using Windows.UI.Popups;
using Newtonsoft.Json;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.ApplicationModel.DataTransfer;

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
            List<Uri> allowedUris = new List<Uri>();
            allowedUris.Add(new Uri("ms-appx-web:///DWT/index.html"));
            OpenIPList = new List<string>();
            WebView1.Navigate(new Uri("ms-appx-web:///DWT/index.html"));
            WebView1.ScriptNotify += WebView1_ScriptNotify;
        }

        private async void WebView1_ScriptNotify(object sender, NotifyEventArgs e)
        {
            var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(e.Value);
            string info = response.GetValueOrDefault("info", "");
            // Respond to the script notification.
            if (info == "dynamsoft_service_not_running")
            {
                // Create the message dialog and set its content
                var messageDialog = new MessageDialog("Dynamsoft Service is not running. Please download and install it.");

                // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
                messageDialog.Commands.Add(new UICommand(
                    "Download",
                    new UICommandInvokedHandler(this.CommandInvokedHandler)));
                messageDialog.Commands.Add(new UICommand(
                    "Close",
                    new UICommandInvokedHandler(this.CommandInvokedHandler)));

                // Set the command that will be invoked by default
                messageDialog.DefaultCommandIndex = 0;

                // Set the command to be invoked when escape is pressed
                messageDialog.CancelCommandIndex = 1;

                // Show the message dialog
                await messageDialog.ShowAsync();
            }
            else if (info == "image_base64") {
                if (response.ContainsKey("data")) {
                    string base64 = response.GetValueOrDefault("data","");
                    OCRImageFromBase64(base64);
                }
                
            }
        }

        private async void CommandInvokedHandler(IUICommand command)
        {
            if (command.Label == "Download") {
                string uriToLaunch = @"https://download.dynamsoft.com/Demo/DWT/DWTResources/dist/DynamsoftServiceSetup.msi";
                var uri = new Uri(uriToLaunch);

                var success = await Windows.System.Launcher.LaunchUriAsync(uri);

                if (success)
                {
                    // URI launched
                }
                else
                {
                    // URI launch failed
                }
            }
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

        private async void OCRButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("OCR");
            await WebView1.InvokeScriptAsync("GetSelectedImageInBase64", new string[] { });
        }

        private async void OCRImageFromBase64(string base64) {
            byte[] bytes;
            bytes = Convert.FromBase64String(base64);
            IBuffer buffer = WindowsRuntimeBufferExtensions.AsBuffer(bytes, 0, bytes.Length);
            InMemoryRandomAccessStream inStream = new InMemoryRandomAccessStream();
            DataWriter datawriter = new DataWriter(inStream.GetOutputStreamAt(0));
            datawriter.WriteBuffer(buffer, 0, buffer.Length);
            await datawriter.StoreAsync();
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(inStream);
            SoftwareBitmap bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            OcrEngine ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            OcrResult ocrResult = await ocrEngine.RecognizeAsync(bitmap);
            ContentDialog contentDialog = new ContentDialog
            {
                Title = "Result:",
                Content = ocrResult.Text,
                PrimaryButtonText = "Copy to clipboard",
                CloseButtonText = "Close"
            };

            ContentDialogResult result = await contentDialog.ShowAsync();

            // Delete the file if the user clicked the primary button.
            /// Otherwise, do nothing.
            if (result == ContentDialogResult.Primary)
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetText(ocrResult.Text);
                Clipboard.SetContent(dataPackage);
            }
        }

        private void WebView1_LoadCompleted(object sender, NavigationEventArgs e)
        {

        }
    }
}
