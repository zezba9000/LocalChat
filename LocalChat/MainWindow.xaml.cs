using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Translation.V2;

namespace LocalChat
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private TranslationClient translationClient;
		private string messageTemplateXAML;
		private string[] langCodes;
		private bool autoTranlate, googleTranslateInit;

		public MainWindow()
		{
			InitializeComponent();

			// clone and disable template message
			messageTemplateXAML = XamlWriter.Save(messageTemplate);
			messageTemplate.Visibility = Visibility.Collapsed;
		}

		protected override void OnContentRendered(EventArgs e)
		{
			base.OnContentRendered(e);
			
			// init google translate
			try
			{
				if (File.Exists("Local Chat.json"))
				{
					var creds = GoogleCredential.FromFile("Local Chat.json");
					translationClient = TranslationClient.Create(creds);
					googleTranslateInit = true;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, "Google Translate failed: " + ex.Message, "Warning!");
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (translationClient != null)
			{
				translationClient.Dispose();
				translationClient = null;
			}

			base.OnClosing(e);
		}

		private void postButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(enterTextBox.Text)) return;

			// get xaml objects
			var stringReader = new StringReader(messageTemplateXAML);
			var xmlReader = XmlReader.Create(stringReader);
			var messageItem = (Border)XamlReader.Load(xmlReader);

			// setup message
			var messageTextBlock = (TextBlock)messageItem.FindName("messageTextBlock");
			messageTextBlock.Text = enterTextBox.Text;

			var translateButton = (Button)messageItem.FindName("translateButton");
			translateButton.Click += translateButton_Click;

			// finish
			messageStackPanel.Children.Add(messageItem);
			enterTextBox.Clear();
			scrollViewer.ScrollToEnd();
			if (autoTranlate) translateButton_Click(translateButton, e);
		}

		private void OnKeyDownHandler(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Return) return;
			postButton_Click(sender, null);
		}

		private void translateButton_Click(object sender, RoutedEventArgs e)
		{
			// validate lang codes setup
			if (!googleTranslateInit)
			{
				MessageBox.Show(this, "Google Translate not setup!", "Alert");
				return;
			}

			if (langCodes == null || langCodes.Length != 2)
			{
				MessageBox.Show(this, "No Translation Languages setup.\nGo to 'Settings'", "Alert");
				return;
			}

			if (langCodes[0] == langCodes[1])
			{
				MessageBox.Show(this, "Translation Languages are the same.\nGo to 'Settings'", "Alert");
				return;
			}

			// get xaml objects
			var button = (Button)sender;
			var grid = (Grid)button.Parent;
			var translationSeperator = (Separator)grid.FindName("translationSeperator");
			var messageTextBlock = (TextBlock)grid.FindName("messageTextBlock");
			var messageTranslatedTextBlock = (TextBlock)grid.FindName("messageTranslatedTextBlock");

			// check if lang can be translated
			var detectedLang = translationClient.DetectLanguage(messageTextBlock.Text);
			if (detectedLang.IsReliable)
			{
				MessageBox.Show(this, "Translation unreliable!", "Alert");
				return;
			}

			string targetLang;
			if (detectedLang.Language != langCodes[0]) targetLang = langCodes[0];
			else if (detectedLang.Language != langCodes[1]) targetLang = langCodes[1];
			else
			{
				MessageBox.Show(this, "Detected language invalid: " + detectedLang.Language, "Alert");
				return;
			}

			var response = translationClient.TranslateText(messageTextBlock.Text, targetLang);
			if (string.IsNullOrEmpty(response.TranslatedText))
			{
				MessageBox.Show(this, "Failed to translate!", "Alert");
				return;
			}
			
			// finish
			messageTranslatedTextBlock.Text = response.TranslatedText;
			messageTranslatedTextBlock.Visibility = Visibility.Visible;
			translationSeperator.Visibility = Visibility.Visible;
		}

		private void settingsButton_Click(object sender, RoutedEventArgs e)
		{
			if (!googleTranslateInit)
			{
				MessageBox.Show(this, "Google Translate not setup!", "Alert");
				return;
			}

			settingsOverlay.Show(translationClient, langCodes, SettingsDoneCallback);
		}

		private void SettingsDoneCallback(string[] langCodes, bool autoTranlate)
		{
			this.langCodes = langCodes;
			this.autoTranlate = autoTranlate;
		}
	}
}
