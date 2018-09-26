using System;
using System.IO;
using System.Windows;
using System.Xml.Serialization;
using LocalChat.XML;

namespace LocalChat
{
	namespace XML
	{
		[XmlRoot]
		public class AppSettings
		{
			[XmlElement] public bool autoTranslate;
			[XmlElement] public string langCode1, langCode2;
		}
	}

	static class Settings
	{
		public static AppSettings obj;
		private static readonly string appSettingsFilename;

		static Settings()
		{
			obj = new AppSettings();
			string dataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			appSettingsFilename = Path.Combine(dataFolder, "LocalChat", "Settings.xml");
		}

		public static void Load()
		{
			if (!File.Exists(appSettingsFilename)) return;

			try
			{
				var xml = new XmlSerializer(typeof(AppSettings));
				using (var stream = new FileStream(appSettingsFilename, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					obj = (AppSettings)xml.Deserialize(stream);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(MainWindow.singleton, "Load Settings Error: " + e.Message);
				obj = new AppSettings();
			}
		}

		public static bool Save()
		{
			string path = Path.GetDirectoryName(appSettingsFilename);
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);

			try
			{
				var xml = new XmlSerializer(typeof(AppSettings));
				using (var stream = new FileStream(appSettingsFilename, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					xml.Serialize(stream, obj);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(MainWindow.singleton, "Save Settings Error: " + e.Message);
				return false;
			}

			return true;
		}
	}
}
