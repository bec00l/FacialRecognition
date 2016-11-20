using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Plugin.Connectivity;
using Plugin.Media;
using Plugin.Media.Abstractions;


namespace FacialRecognition
{
    public partial class MainPage : ContentPage
    {
        private readonly IFaceServiceClient faceServiceClient;
        private readonly EmotionServiceClient emotionServiceClient;
        public MainPage()
        {
            InitializeComponent();
            this.faceServiceClient = new FaceServiceClient("2931cfb9ea8b471f8352363894703f23");

            this.emotionServiceClient = new EmotionServiceClient("1d28bbfcdce94db29899c8e0a97b148a");
        }


        /// <summary>
        /// The take picture button_ on clicked.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        /// <exception cref="NotImplementedException">
        /// </exception>
        private async void TakePictureButton_OnClicked(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();

            if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
            {
                await DisplayAlert("No Camera!", "Taking a photo is not supported.", "OK");
                return;
            }

            var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                SaveToAlbum = true,
                Name = "test.jpg"
            });

            SetImageSource(file);

        }

        /// <summary>
        /// The upload picture button_ on clicked picture button_ on clicked.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        /// <exception cref="NotImplementedException">
        /// </exception>
        private async void  UploadPictureButton_OnClickedPictureButton_OnClicked(object sender, EventArgs e)
        {
            if (!CrossMedia.Current.IsPickPhotoSupported)
            {
                await DisplayAlert("No upload!", "Picking a photo is not supported.", "OK");
                return;
            }

            var file = await CrossMedia.Current.PickPhotoAsync();
            SetImageSource(file);
        }

        private async void SetImageSource(MediaFile file)
        {
            if (file == null)
            {
                return;
            }

            this.Indicator1.IsVisible = true;
            this.Indicator1.IsRunning = true;

            Image1.Source = ImageSource.FromStream(() => file.GetStream());

            FaceEmotionDetection data = await DetectFaceAndEmotionAsync(file);
            this.BindingContext = data;

            this.Indicator1.IsRunning = false;
            this.Indicator1.IsVisible = false;
        }

        private async Task<FaceEmotionDetection> DetectFaceAndEmotionAsync(MediaFile input)
        {
            try
            {
                Emotion[] emotionResult = await emotionServiceClient.RecognizeAsync(input.GetStream());

                var faceEmotion = emotionResult[0]?.Scores.ToRankedList();

                var requiredFaceAttributes = new FaceAttributeType[]
                {
                    FaceAttributeType.Age,
                    FaceAttributeType.Gender,
                    FaceAttributeType.Smile,
                    FaceAttributeType.FacialHair,
                    FaceAttributeType.HeadPose,
                    FaceAttributeType.Glasses
                };

                var faces = await faceServiceClient.DetectAsync(input.GetStream(), false, false, requiredFaceAttributes);

                var faceAttributes = faces[0]?.FaceAttributes;

                FaceEmotionDetection faceEmotionDetection = new FaceEmotionDetection();
                faceEmotionDetection.Age = Math.Round(faceAttributes.Age);
                faceEmotionDetection.Emotion = faceEmotion.FirstOrDefault().Key;
                faceEmotionDetection.Glasses = faceAttributes.Glasses.ToString();
                faceEmotionDetection.Smile = faceAttributes.Smile;
                faceEmotionDetection.Gender = faceAttributes.Gender;
                faceEmotionDetection.Beard = faceAttributes.FacialHair.Beard;
                faceEmotionDetection.Moustache = faceAttributes.FacialHair.Moustache;

                return faceEmotionDetection;


            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
                return null;
            }
        }
    }
}
