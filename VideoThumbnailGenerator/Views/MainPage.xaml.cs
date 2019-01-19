using System;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace VideoThumbnailGenerator.Views
{
    public sealed partial class MainPage : Page
    {
        StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            string filename = "catvideo.mp4";
            StorageFile videofile = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(@"Assets\" + filename);
            Stream fileStream = await videofile.OpenStreamForReadAsync();
            GenerateVideoThumbnail(videofile);
        }

        private async void GenerateVideoThumbnail(StorageFile file)
        {
            MediaComposition mediaComposition = new MediaComposition();
            var videoFile = await MediaClip.CreateFromFileAsync(file);
            mediaComposition.Clips.Add(videoFile);
            TimeSpan interval = videoFile.OriginalDuration.Add(new TimeSpan(videoFile.OriginalDuration.Ticks/2));

            var thumbnail = await mediaComposition.GetThumbnailAsync(interval, 640, 360, VideoFramePrecision.NearestKeyFrame);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(thumbnail);
            var thumbnailWriter = await localFolder.CreateFileAsync("generated_thumbnail.jpg", CreationCollisionOption.ReplaceExisting).AsTask();

            System.Diagnostics.Debug.WriteLine("Thumbnail Location: " + localFolder.Path);

            using (var thumbnailStream = await thumbnailWriter.OpenAsync(FileAccessMode.ReadWrite))
            using (var dataReader = new DataReader(thumbnail.GetInputStreamAt(0)))
            {
                var output = thumbnailStream.GetOutputStreamAt(0);

                await dataReader.LoadAsync((uint)thumbnail.Size);

                while (dataReader.UnconsumedBufferLength > 0)
                {
                    uint dataToRead = dataReader.UnconsumedBufferLength > 64
                                        ? 64
                                        : dataReader.UnconsumedBufferLength;

                    IBuffer buffer = dataReader.ReadBuffer(dataToRead);

                    await output.WriteAsync(buffer);
                }

                await output.FlushAsync();
            }
        }
    }
}