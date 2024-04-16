using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenAI.ObjectModels.ResponseModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Podkast.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PodkastPage : Page
    {
        private OpenAIService openAiService;
        private StorageFile file;
        public PodkastPage()
        {
            this.InitializeComponent();
            var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = openAiKey
            });

        }
        private async void PickAFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous returned file name, if it exists, between iterations of this scenario
            PickAFileOutputTextBlock.Text = "";

            // Create a file picker
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            // See the sample code below for how to make the window accessible from the App class.
            var window = new MainWindow();

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your file picker
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a file
            file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                PickAFileOutputTextBlock.Text = "Picked file: " + file.Name;
            }
            else
            {
                PickAFileOutputTextBlock.Text = "Operation cancelled.";
            }
        }
        private List<ChatMessage> conversationContext = new List<ChatMessage>();

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = InputTextBox.Text;
            if (!string.IsNullOrEmpty(userInput))
            {
                AddMessageToConversation($"You: {userInput}");
                InputTextBox.Text = string.Empty;

                conversationContext.Add(ChatMessage.FromUser(userInput)); // Add user input to conversation context

                var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                {
                    Messages = conversationContext, // Use conversation context
                    Model = Models.ChatGpt3_5Turbo,
                    MaxTokens = 800
                });

                if (completionResult != null && completionResult.Successful)
                {
                    AddMessageToConversation("Podkast AI: " + completionResult.Choices.First().Message.Content);
                    conversationContext.Add(completionResult.Choices.First().Message); // Add AI response to conversation context
                }
                else
                {
                    AddMessageToConversation("Podkast AI: Sorry, something bad happened: " + completionResult.Error?.Message);
                }
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset podcast title and hosts to default values
            podkastTitle.Text = "Title";
            podkastHosts.Text = "Host and Speakers";

            // Reset podcast album art to the default image
            // Ensure the image path matches your project's file structure
            podkastAlbumArt.Source = new BitmapImage(new Uri("ms-appx:///Assets/Blue-Logo.png"));

            // Reset transcript, summary, and context windows to default values
            podkastTranscript.Text = "Podcast Transcript goes here.";
            podkastSummary.Text = "Podcast Summary goes here.";
            podkastSimilar.Text = "The Genre of the podcast";

            // Clear conversation window
            ConversationList.Items.Clear();
        }
        private void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                SendButton_Click(this, new RoutedEventArgs());
            }
        }
        public class MessageItem
        {
            public string Text { get; set; }
            public SolidColorBrush Color { get; set; }
        }
        private void AddMessageToConversation(string message)
        {
            var messageItem = new MessageItem();
            messageItem.Text = message;
            messageItem.Color = message.StartsWith("You:") ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.White);
            ConversationList.Items.Add(messageItem);

            // handle scrolling
            ConversationScrollViewer.UpdateLayout();
            ConversationScrollViewer.ChangeView(null, ConversationScrollViewer.ScrollableHeight, null);

        }
    }
}
