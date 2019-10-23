using System;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
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
		public static MainWindow singleton;
		private MediaPlayer messageSoundPlayer;
		private TranslationClient translationClient;
		private string messageTemplateXAML;
		private string[] langCodes;
		private bool autoTranlate, googleTranslateInit;

		public MainWindow()
		{
			singleton = this;
			InitializeComponent();

			// load message sound
			var soundURI = new Uri("message.wav", UriKind.Relative);
			messageSoundPlayer = new MediaPlayer();
			messageSoundPlayer.MediaFailed += MessageSoundPlayer_MediaFailed;
			messageSoundPlayer.Open(soundURI);

			// clone and disable template message
			messageTemplateXAML = XamlWriter.Save(messageTemplate);
			messageTemplate.Visibility = Visibility.Collapsed;

			// load settings
			Settings.Load();
			autoTranlate = Settings.obj.autoTranslate;
			if (!string.IsNullOrEmpty(Settings.obj.langCode1) && !string.IsNullOrEmpty(Settings.obj.langCode2))
			{
				langCodes = new string[2];
				langCodes[0] = Settings.obj.langCode1;
				langCodes[1] = Settings.obj.langCode2;
			}
		}

		private void MessageSoundPlayer_MediaFailed(object sender, ExceptionEventArgs e)
		{
			MessageBox.Show(this, "Cannot load audio file: " + e.ErrorException.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

			// save settings
			Settings.obj.autoTranslate = autoTranlate;
			if (langCodes != null && langCodes.Length == 2 && !string.IsNullOrEmpty(langCodes[0]) && !string.IsNullOrEmpty(langCodes[1]))
			{
				Settings.obj.langCode1 = langCodes[0];
				Settings.obj.langCode2 = langCodes[1];
			}
			Settings.Save();

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
			var datetimeTextBlock = (TextBlock)messageItem.FindName("datetimeTextBlock");
			datetimeTextBlock.Text = DateTime.Now.ToLocalTime().ToString();

			var messageTextBlock = (TextBox)messageItem.FindName("messageTextBlock");
			messageTextBlock.Text = enterTextBox.Text;

			var translateButton = (Button)messageItem.FindName("translateButton");
			translateButton.Click += translateButton_Click;

			var closeButton = (Button)messageItem.FindName("closeButton");
			closeButton.Click += closeButton_Click;

			// finish
			messageStackPanel.Children.Add(messageItem);
			enterTextBox.Clear();
			scrollViewer.ScrollToEnd();
			if (autoTranlate) translateButton_Click(translateButton, e);
			messageSoundPlayer.Stop();
			messageSoundPlayer.Play();
		}

		private void enterTextBox_OnKeyDownHandler(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Return) return;
			if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
			{
				int caret = enterTextBox.CaretIndex;
				string text = enterTextBox.Text;
				string first = text.Substring(0, enterTextBox.CaretIndex);
				string last = text.Substring(enterTextBox.CaretIndex);
				enterTextBox.Text = first + Environment.NewLine + last;
				enterTextBox.CaretIndex = caret + 1;
				e.Handled = true;
			}
			else
			{
				postButton_Click(sender, null);
			}
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
			var grid = (Grid)((Grid)button.Parent).Parent;
			var translationSeperator = (Separator)grid.FindName("translationSeperator");
			var messageTextBlock = (TextBox)grid.FindName("messageTextBlock");
			var messageTranslatedTextBlock = (TextBox)grid.FindName("messageTranslatedTextBlock");

			// check if lang can be translated
			Detection detectedLang;
			try
			{
				detectedLang = translationClient.DetectLanguage(messageTextBlock.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Error");
				return;
			}

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

		private void closeButton_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var grid = (Grid)((Grid)button.Parent).Parent;
			var messageTemplate = (Border)grid.Parent;
			messageStackPanel.Children.Remove(messageTemplate);
		}

		private void settingsButton_Click(object sender, RoutedEventArgs e)
		{
			if (!googleTranslateInit)
			{
				MessageBox.Show(this, "Google Translate not setup!", "Alert");
				return;
			}

			settingsOverlay.Show(translationClient, langCodes, autoTranlate, SettingsDoneCallback);
		}

		private void SettingsDoneCallback(string[] langCodes, bool autoTranlate)
		{
			this.langCodes = langCodes;
			this.autoTranlate = autoTranlate;
		}
	}
}
