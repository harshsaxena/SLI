using App2.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Lumia.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;
using Lumia.InteropServices.WindowsRuntime;
using Windows.Storage.Search;
using System.Threading.Tasks;

using Windows.Media.SpeechSynthesis;









// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace App2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Interpret : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private static readonly IEnumerable<string> SupportedImageFileTypes = new List<string> { ".jpeg", ".jpg", ".png" };

        private WriteableBitmap originalBitmap;
        private WriteableBitmap editedBitmap;
        private WriteableBitmap listBitmap;//image obtained from list 
        public Interpret()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            var app = Application.Current as App;
            if (app != null)
            {
                app.FilesOpenPicked += OnFilesOpenPicked;
                app.FileSavePicked += OnFileSavePicked;
            }
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }
        // This is how the FileOpenPicker can be used to select images
        //   from the camera roll (or the user can choose to take a new picture)

        private void AppBarBtnInfo_Click(object sender, RoutedEventArgs e)
        {
            desc.Text = "Choose a Pic to Interpret";
            if (desc.Visibility == Visibility.Visible)
                desc.Visibility = Visibility.Collapsed;
            else
                desc.Visibility = Visibility.Visible;
        }


        private async void AppBarBtnChoose_Click(object sender, RoutedEventArgs e)
        {
            desc.Visibility = Visibility.Collapsed;
            AppBarBtnSpeech.Visibility = Visibility.Collapsed;
            tb.Visibility = Visibility.Collapsed;
            //// Shortpath to get first file from Camera Roll. Useful for development.
            //var files = await KnownFolders.CameraRoll.GetFilesAsync();
            //OnFilesOpenPicked(new ReadOnlyCollection<StorageFile>(new List<StorageFile> { files.FirstOrDefault() }));
            //return;

            // Pick photo or take new one
            var fop = new FileOpenPicker();
            foreach (var fileType in SupportedImageFileTypes)
            {
                fop.FileTypeFilter.Add(fileType);
            }
            fop.PickSingleFileAndContinue();
        }

        ///////////////////////////////////////////////////////////////////////////
        // Use the Nokia Imaging SDK to apply a filter to a selected image

        private async void AppBarBtnEdit_Click(object sender, RoutedEventArgs e)
        {
            progressRing.IsEnabled = true;
            progressRing.IsActive = true;
            progressRing.Visibility = Visibility.Visible;

            // Create NOK Imaging SDK effects pipeline and run it
            var imageStream = new BitmapImageSource(originalBitmap.AsBitmap());
            using (var effect = new FilterEffect(imageStream))
            {
                var filter = new Lumia.Imaging.Adjustments.GrayscaleFilter();
                effect.Filters = new[] { filter };
                
                // Render the image to a WriteableBitmap.
                var renderer = new WriteableBitmapRenderer(effect, originalBitmap);
                editedBitmap = await renderer.RenderAsync();
                editedBitmap.Invalidate();
            }

            Image.Source = originalBitmap;

            Image.Visibility = Visibility.Collapsed;

           

            //Resizing the editedBitmap to 128x128
            var resized1 = editedBitmap.Resize(128, 128, Windows.UI.Xaml.Media.Imaging.WriteableBitmapExtensions.Interpolation.Bilinear);

            //converting the editedBitmap to byte array
            byte[] edit_arr = resized1.ToByteArray();

            
            
            
            //obtaining the images folder
            StorageFolder folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFolder subfolder = await folder.GetFolderAsync("Images");
         
            
            //create list of all the files in the images folder
            var pictures = await subfolder.GetFilesAsync();
            
            

            double ldiff = 50;//least percentage difference for an image to be a match
            string dispText = "Try again";//default message to be displayed

            byte threshold = 124;
            
            //process through all images 
            foreach (var file in pictures)
            {
                
                if (file != null)
                {
                    // Use WriteableBitmapEx to easily load image from a stream
                    using (var stream = await file.OpenReadAsync())
                    {
                        listBitmap = await new WriteableBitmap(1, 1).FromStream(stream);
                        stream.Dispose();
                    }

                    //convert obtained image to byte array
                    byte[] list_arr = listBitmap.ToByteArray();


                    byte[] difference = new byte[edit_arr.Length];

                    //compare byte array of both the images
                    for (int i=0;i<list_arr.Length;i++)
                    {
                        difference[i] = (byte)Math.Abs(edit_arr[i]-list_arr[i]);
                    }


                    //calculate percentage difference
                    int differentPixels = 0;

                    foreach(byte b in difference )
                    {
                        if (b > threshold)
                            differentPixels++;
                    }

                    double percentage = (double)differentPixels / (double)list_arr.Length;
                    percentage = percentage * 100;
                    

                    if (percentage <= ldiff)
                    {
                        ldiff = percentage;
                        dispText =file.DisplayName;
                    }
                }
            }

            tb.Text = dispText;

            progressRing.IsEnabled = false;
            progressRing.IsActive = false;
            progressRing.Visibility = Visibility.Collapsed;


            tb.Visibility = Visibility.Visible;
            Image.Visibility = Visibility.Visible;
            

            var tmp = new RenderTargetBitmap();
            await tmp.RenderAsync(source);

            var buffer = await tmp.GetPixelsAsync();
            var width = tmp.PixelWidth;
            var height = tmp.PixelHeight;
            editedBitmap = await new WriteableBitmap(1, 1).FromPixelBuffer(buffer, width, height);

            AppBarBtnSpeech.IsEnabled = true;
            AppBarBtnSpeech.Visibility = Visibility.Visible;

            AppBarBtnSave.IsEnabled = true;


        }

       
        ///////////////////////////////////////////////////////////////////////////
        // Use the FileSavePicker to save the photo with the selected file name

        private void AppBarBtnSave_Click(object sender, RoutedEventArgs e)
        {
            var fsp = new FileSavePicker
            {
                DefaultFileExtension = ".jpg",
                SuggestedFileName = "editedPhoto.jpg",
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            fsp.FileTypeChoices.Add("JPEG", new List<string> { ".jpg" });
            fsp.PickSaveFileAndContinue();
        }

        private async void OnFilesOpenPicked(IReadOnlyList<StorageFile> files)
        {
            // Load picked file
            if (files.Count > 0)
            {
                // Check if image and pick first file to show
                var imageFile = files.FirstOrDefault(f => SupportedImageFileTypes.Contains(f.FileType.ToLower()));
                if (imageFile != null)
                {
                    // Use WriteableBitmapEx to easily load from a stream
                    using (var stream = await imageFile.OpenReadAsync())
                    {
                        originalBitmap = await new WriteableBitmap(1, 1).FromStream(stream);
                    }
                    Image.Source = originalBitmap;
                    AppBarBtnEdit.IsEnabled = true;
                    AppBarBtnSave.IsEnabled = false;
                }
            }
        }





        private async void OnFileSavePicked(StorageFile storageFile)
        {
            using (var stream = await storageFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                await editedBitmap.ToStreamAsJpeg(stream);
            }
        }
        #endregion

        private void AppBarBtnSpeech_Click(object sender, RoutedEventArgs e)
        {
            SpeakText(audioPlayer, tb.Text);
        }

        private async void SpeakText(MediaElement audioPlayer, string TTS)
        {
            SpeechSynthesizer ttssynthesizer = new SpeechSynthesizer();

            //Set the Voice/Speaker
            using (var Speaker = new SpeechSynthesizer())
            {
                Speaker.Voice = (SpeechSynthesizer.AllVoices.First(x => x.Gender == VoiceGender.Female));
                ttssynthesizer.Voice = Speaker.Voice;
            }

            SpeechSynthesisStream ttsStream = await ttssynthesizer.SynthesizeTextToStreamAsync(TTS);

            audioPlayer.SetSource(ttsStream, "");
        }
    }
}
