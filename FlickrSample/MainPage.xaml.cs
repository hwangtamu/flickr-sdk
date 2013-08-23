using FlickrSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace FlickrSample
{
    public sealed partial class MainPage : Page
    {
        private const string ConsumerKey = "";
        private const string ConsumerSecret = "";
        private const string CallbackUrl = "";

        private StorageFile _storageFile;
        private FlickrApi _flickr;

        public MainPage()
        {
            this.InitializeComponent();

            _flickr = new FlickrApi(
                key: ConsumerKey,
                secret: ConsumerSecret,
                callbackUrl: CallbackUrl
            );
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (string.IsNullOrEmpty(ConsumerKey) || string.IsNullOrEmpty(ConsumerSecret) || string.IsNullOrEmpty(CallbackUrl))
            {
                ButtonConnect.IsEnabled = false;
                await new MessageDialog("You have to set your consumer key on the source code.").ShowAsync();
            }

            _storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/SamplePicture.jpg"));
            await DisplayStorageFileAsync(_storageFile);
        }

        private async void ButtonLoadImageClick(object sender, RoutedEventArgs e)
        {
            var fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fileOpenPicker.ViewMode = PickerViewMode.Thumbnail;
            fileOpenPicker.FileTypeFilter.Add(".png");
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.FileTypeFilter.Add(".bmp");

            var storageFile = await fileOpenPicker.PickSingleFileAsync();
            if (storageFile == null)
            {
                _storageFile = null;
                return;
            }

            _storageFile = storageFile;

            await DisplayStorageFileAsync(storageFile);
        }

        private async Task DisplayStorageFileAsync(StorageFile storageFile)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.SetSource(await _storageFile.OpenAsync(FileAccessMode.Read));

            ImagePhoto.Source = bitmapImage;
        }

        private async void ButtonConnectClick(object sender, RoutedEventArgs e)
        {
            ButtonUpload.IsEnabled = false;

            try
            {
                GridProgressRing.Visibility = Visibility.Visible;

                await _flickr.InitAsync();

                ButtonUpload.IsEnabled = true;
            }
            catch
            {
                ButtonUpload.IsEnabled = false;
            }

            if (!ButtonUpload.IsEnabled)
            {
                await new MessageDialog("Unable to login.").ShowAsync();
            }

            GridProgressRing.Visibility = Visibility.Collapsed;
        }

        private async void ButtonUploadClick(object sender, RoutedEventArgs e)
        {
            if (_storageFile == null)
            {
                await new MessageDialog("No file to share.").ShowAsync();
                return;
            }

            GridProgressRing.Visibility = Visibility.Visible;

            await _flickr.UploadPhotoAsync(_storageFile);

            await new MessageDialog("Photo uploaded.").ShowAsync();

            GridProgressRing.Visibility = Visibility.Collapsed;
        }
    }
}
