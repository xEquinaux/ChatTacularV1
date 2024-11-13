using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Chat_tacular.Properties;
using Twitchbot.Core;
using Twitchbot.Util;
using TwitchLib.Client.Models;
using TwitchLib.Client;
using TwitchLib.Communication.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Client.Events;
using Emote = Twitchbot.Util.Emote;
using TwitchLib.Client.Extensions;

namespace Chat_tacular;

public partial class ImageBase : Window, IComponentConnector
{
	internal static System.Windows.Media.Brush FG_Text = System.Windows.Media.Brushes.Black;

	internal static System.Windows.Media.Brush BG_Text = System.Windows.Media.Brushes.Black;

	private List<string> verblist = new List<string>();

	internal static List<Emote> UsernameImg = new List<Emote>();

	internal static List<Emote> CustomEmote = new List<Emote>();

	internal static string ImageFile = string.Empty;

	internal static float MarginLeft = 0.2f;

	internal static IDictionary<string, string> UserData = new Dictionary<string, string>();

	internal static IDictionary<string, string> BroadcasterEmotes = new Dictionary<string, string>();

	internal static IDictionary<string, Emote> GlobalEmotes = new Dictionary<string, Emote>();

	private bool flag;

	internal bool closing;

	internal RichTextBox[] outline => new RichTextBox[6] { text_outline1, text_outline2, text_outline3, text_outline4, text_outline5, text_outline6 };

	internal Paragraph[] outlineText => new Paragraph[6] { text_chat_outline1, text_chat_outline2, text_chat_outline3, text_chat_outline4, text_chat_outline5, text_chat_outline6 };

	public class IdkConnect
    {
        public static IdkConnect Instance;
        public TwitchClient client;
	
        public IdkConnect()
        {
            Instance = this;
                                                                             
            ConnectionCredentials credentials = new ConnectionCredentials(Chat_tacular.App.username, Chat_tacular.App.auth);
	        var clientOptions = new ClientOptions
                {
                    MessagesAllowedInPeriod = 750,
                    ThrottlingPeriod = TimeSpan.FromSeconds(30)
                };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, Chat_tacular.App.channel);

            //client.OnLog += Client_OnLog;
            //client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += App.Chat.MessageHandler;
            //client.OnWhisperReceived += Client_OnWhisperReceived;
            //client.OnNewSubscriber += Client_OnNewSubscriber;
            //client.OnConnected += Client_OnConnected;

            client.Connect();
        }
	}

	public ImageBase()
	{
		InitializeComponent();
		App.Chat = this;
		new Login().ShowDialog();
		App.Settings = new Settings();
		App.Settings.Show();
		if (Login.ProgramOAuth.Length == 0)
		{
			Login.ProgramOAuth = App.ProgramOAuth;
		}
		string text = "";
		if (App.auth == null || App.channel == null || App.username == null || App.auth.Length == 0 || App.username.Length < 4 || App.channel.Length < 4)
		{
			return;
		}
		int num = 0;
		while (!closing && num++ < 3)
		{
			try
			{
				text = App.GetUserData(App.username, Login.ProgramOAuth);
			}
			catch
			{
				using StreamReader streamReader = new StreamReader(WebRequest.Create("http://localhost").GetResponse().GetResponseStream());
				IDictionary<string, string> dictionary = JsonDecode.Contents(streamReader.ReadToEnd());
				if (dictionary.ContainsKey("access_token"))
				{
					Login.ProgramOAuth = dictionary["access_token"];
					continue;
				}
				return;
			}
			break;
		}
		if (text.Length > 0 && UserData != null)
		{
			UserData = JsonDecode.Contents(text.Substring(text.IndexOf("[")));
			BroadcasterEmotes = JsonDecode.Contents(App.GetBroadcasterEmotes(UserData["id"], App.auth));
			GlobalEmotes = JsonDecode.Emotes(App.GetGlobalEmotes(UserData["id"], App.ProgramOAuth));
		}
		new IdkConnect();
		//Twitch twitch = new Twitch(App.auth, App.username, App.channel);
		//twitch.Connect();
		//new Bot(twitch).ChatHandler(App.channel);
		string path;
		if (!Directory.Exists(path = ".\\" + App.username + "\\custom\\username"))
		{
			Directory.CreateDirectory(path);
		}
		string path2;
		if (!Directory.Exists(path2 = ".\\" + App.username + "\\custom\\emotes"))
		{
			Directory.CreateDirectory(path2);
		}
		UsernameImg = ListEmotes(path);
		CustomEmote = ListEmotes(path2);
		string path3;
		if (!File.Exists(path3 = App.username + "\\bannedverbs.txt"))
		{
			using (new StreamWriter(path3))
			{
			}
		}
		using (StreamReader streamReader2 = new StreamReader(path3))
		{
			string text2 = "";
			while (!streamReader2.EndOfStream)
			{
				text2 = streamReader2.ReadLine().Replace("\r", "").Replace("\n", "")
					.Replace(" ", "");
				if (text2.Length > 0)
				{
					verblist.Add(text2);
				}
			}
		}
		base.ContentRendered += delegate
		{
			Window_SizeChanged(null, null);
		};
		bg_image.Effect = new BlurEffect
		{
			Radius = 0.0,
			RenderingBias = RenderingBias.Quality
		};
		matte_image.Effect = new BlurEffect
		{
			Radius = 4.0,
			RenderingBias = RenderingBias.Quality
		};
		bg_media.Effect = new BlurEffect
		{
			Radius = 0.0,
			RenderingBias = RenderingBias.Quality
		};
		richtextbox_bg.Effect = new BlurEffect
		{
			Radius = 3.0,
			RenderingBias = RenderingBias.Quality
		};
		bg_media.MediaEnded += Bg_media_MediaEnded;
		//Bot.MessageReceivedEvent += MessageHandler;
		base.Width = 640.0;
		base.Height = 480.0;
	}

	private void Bg_media_MediaEnded(object sender, RoutedEventArgs e)
	{
		bg_media.Stop();
		bg_media.Play();
	}

	private string MessageFromBuffer(string buffer)
	{
		string[] array = buffer.Split(';');
		if (array.Length < 14)
		{
			for (int i = 0; i < array.Length - 1; i++)
			{
				array[i] = array[i].Replace(" ", "");
			}
		}
		string text = ((array.Length != 14) ? array[15] : array[13]);
		return text.Substring(text.IndexOf(":") + 1).Substring(text.Substring(text.IndexOf(":") + 1).IndexOf(":") + 1);
	}

	private bool VerbDetector(string Message)
	{
		for (int i = 0; i < verblist.Count; i++)
		{
			if (Message.Contains(verblist[i]))
			{
				Italic italic = new Italic();
				italic = new Italic(new Run(": Message removed by moderator.\n"));
				italic.Foreground = BG_Text;
				text_chat_bg.Inlines.Add(italic);
				italic = new Italic(new Run(": Message removed by moderator.\n"));
				italic.Foreground = FG_Text;
				text_chat.Inlines.Add(italic);
				richtextbox.ScrollToEnd();
				richtextbox_bg.ScrollToEnd();
				return true;
			}
		}
		return false;
	}

	internal void MessageHandler(object sender, OnMessageReceivedArgs e)
	{
		text_chat.Dispatcher.BeginInvoke((ThreadStart)delegate
		{
			//if ((e.BadgeFlag[0] || e.BadgeFlag[1]) && e.ChatMessage.Message == "!clear")
			//{
			//	text_chat_bg.Inlines.Clear();
			//	text_chat.Inlines.Clear();
			//}
			//else
			{
				string message = e.ChatMessage.Message;
				Run run = new Run();
				bool action = message.StartsWith("\u0001ACTION");
				//if (Settings.Instance.check_badges.IsChecked.Value)
				//{
				//	AddBadge(e);
				//}
				string color = e.ChatMessage.ColorHex;
				if (e.ChatMessage.ColorHex == null)
				{
					color = "#FFFFFF";
				}
				System.Windows.Media.Brush userColor = CustomUsernameImage(e.ChatMessage.Username, color, action);
				if (!VerbDetector(message))
				{
					List<Emote> list = new List<Emote>();
					for (int i = 0; i < GlobalEmotes.Count; i++)
					{
						string text = GlobalEmotes.Keys.ToArray()[i];
						if (message.Contains(text) && text.Length > 2)
						{
							list.Add(GlobalEmotes[text]);
						}
					}
					AddEmoteToChat(message, list.Union(CustomEmote).ToList(), action, userColor);
					run = new Run("\n");
					text_chat_bg.Inlines.Add(run);
					run = new Run("\n");
					text_chat.Inlines.Add(run);
					for (int j = 0; j < outlineText.Length; j++)
					{
						run = new Run("\n");
						outlineText[j].Inlines.Add(run);
					}
					while (text_chat.Inlines.Count > 4000)
					{
						text_chat_bg.Inlines.Remove(text_chat_bg.Inlines.First());
						text_chat.Inlines.Remove(text_chat.Inlines.First());
						for (int k = 0; k < outlineText.Length; k++)
						{
							outlineText[k].Inlines.Remove(text_chat.Inlines.First());
						}
					}
					richtextbox.ScrollToEnd();
					richtextbox_bg.ScrollToEnd();
					for (int l = 0; l < outline.Length; l++)
					{
						outline[l].ScrollToEnd();
					}
				}
			}
		});
	}

	private void AddBadge(Bot.MessageReceivedArgs e)
	{
		System.Drawing.Brush[] array = new System.Drawing.Brush[7]
		{
			System.Drawing.Brushes.Red,
			System.Drawing.Brushes.Green,
			System.Drawing.Brushes.LightPink,
			System.Drawing.Brushes.Silver,
			System.Drawing.Brushes.MediumPurple,
			System.Drawing.Brushes.CornflowerBlue,
			System.Drawing.Brushes.Purple
		};
		System.Windows.Controls.Image[] array2 = new System.Windows.Controls.Image[array.Length];
		System.Windows.Controls.Image[] array3 = new System.Windows.Controls.Image[array.Length];
		System.Drawing.FontFamily fontFamily = new System.Drawing.FontFamily(richtextbox.FontFamily.Source);
		System.Drawing.FontStyle style = System.Drawing.FontStyle.Regular;
		fontFamily.GetCellAscent(style);
		_ = richtextbox.FontSize / (double)fontFamily.GetEmHeight(style);
		for (int i = 0; i < array.Length; i++)
		{
			if (!e.BadgeFlag[i])
			{
				continue;
			}
			using (Bitmap bitmap = new Bitmap(10, 10))
			{
				using Graphics graphics = Graphics.FromImage(bitmap);
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				graphics.FillRectangle(System.Drawing.Brushes.Black, new Rectangle(0, 0, 10, 10));
				graphics.FillEllipse(array[i], new Rectangle(0, 0, 9, 9));
				array2[i] = ChatImage(DrawingToImgSrc(bitmap, transparent: true), new Thickness(0.0, 0.0, 0.0, 5.0), 10, 10);
				array3[i] = ChatImage(DrawingToImgSrc(bitmap, transparent: true), new Thickness(0.0, 0.0, 0.0, 5.0), 10, 10);
			}
			text_chat_bg.Inlines.Add(array2[i]);
			text_chat.Inlines.Add(array3[i]);
		}
	}

	private ImageSource DrawingToImgSrc(Bitmap bmp, bool transparent = false)
	{
		if (transparent)
		{
			bmp.MakeTransparent(System.Drawing.Color.Black);
		}
		int num = bmp.Width * ((PixelFormats.Pbgra32.BitsPerPixel + 7) / 8);
		BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
		BitmapSource result = BitmapSource.Create(bmp.Width, bmp.Height, 96.0, 96.0, PixelFormats.Pbgra32, null, bitmapData.Scan0, num * bmp.Height, num);
		bmp.UnlockBits(bitmapData);
		return result;
	}

	private System.Windows.Media.Brush CustomUsernameImage(string userName, string userColor, bool action = false)
	{
		Run run = new Run();
		System.Windows.Media.Brush result = null;
		if (UsernameImg.Any((Emote t) => t.name == userName))
		{
			Emote emote = UsernameImg.Find((Emote t) => t.name == userName);
			text_chat_bg.Inlines.Add(ChatImage(emote.image.url_1x, new Thickness(0.0), VerticalAlignment.Center));
			text_chat.Inlines.Add(ChatImage(emote.image.url_1x, new Thickness(0.0), VerticalAlignment.Center));
		}
		else
		{
			if (userColor == string.Empty || !userColor.StartsWith("#"))
			{
				userColor = "#FFFFFF";
			}
			Bold bold = new Bold(new Run(userName));
			result = (bold.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(userColor));
			text_chat_bg.Inlines.Add(bold);
			bold = new Bold(new Run(userName));
			bold.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(userColor);
			text_chat.Inlines.Add(bold);
		}
		if (!action)
		{
			run = new Run(": ");
			run.Foreground = BG_Text;
			text_chat_bg.Inlines.Add(run);
			run = new Run(": ");
			run.Foreground = FG_Text;
			text_chat.Inlines.Add(run);
		}
		return result;
	}

	private List<Emote> ListEmotes(string path, string ext = ".png", string ext2 = ".gif")
	{
		List<Emote> list = new List<Emote>();
		List<string> list2 = Directory.EnumerateFiles(path).ToList();
		list2.RemoveAll((string t) => !t.EndsWith(ext) && !t.EndsWith(ext2));
		for (int i = 0; i < list2.Count(); i++)
		{
			list.Add(new Emote
			{
				id = i.ToString(),
				name = list2[i].Substring(path.Length + 1).Replace(".png", "").Replace(".gif", ""),
				image = new Twitchbot.Util.Image
				{
					url_1x = list2[i]
				}
			});
		}
		return list;
	}

	private void AddEmoteToChat(string Message, List<Emote> emote, bool action = false, System.Windows.Media.Brush userColor = null)
	{
		Run run = new Run();
		string empty = string.Empty;
		for (int i = 0; i < Message.Length; i++)
		{
			empty = Message.Substring(i);
			if (empty.Contains(" "))
			{
				empty = Message.Substring(i, empty.IndexOf(" "));
			}
			for (int j = 0; j < emote.Count; j++)
			{
				if (empty == emote[j].name)
				{
					text_chat_bg.Inlines.Add(ChatImage(emote[j].image.url_1x, new Thickness(0.0, 0.0, 0.0, -7.0)));
					text_chat.Inlines.Add(ChatImage(emote[j].image.url_1x, new Thickness(0.0, 0.0, 0.0, -7.0)));
					i += emote[j].name.Length;
					break;
				}
			}
			if (action)
			{
				Message = Message.Replace("ACTION", "").Replace("\u0001", "");
			}
			if (i < Message.Length)
			{
				run = new Run(Message.Substring(i, 1));
				run.Foreground = ((action && userColor != null) ? userColor : BG_Text);
				text_chat_bg.Inlines.Add(run);
				run = new Run(Message.Substring(i, 1));
				run.Foreground = System.Windows.Media.Brushes.WhiteSmoke; //((action && userColor != null) ? userColor : FG_Text);
				text_chat.Inlines.Add(run);
			}
		}
	}

	private System.Windows.Controls.Image ChatImage(string path, Thickness margin, VerticalAlignment align)
	{
		BitmapImage bitmapImage = new BitmapImage(new Uri(Path.GetFullPath(path)));
		float num = (float)text_chat.FontSize * 1.2f;
		double num2 = bitmapImage.Width / bitmapImage.Height;
		return new System.Windows.Controls.Image
		{
			Source = bitmapImage,
			Height = num,
			Width = num2 * (double)num,
			Margin = margin,
			VerticalAlignment = align
		};
	}

	private System.Windows.Controls.Image ChatImage(string uri, Thickness margin, int width = 28, int height = 28, VerticalAlignment align = VerticalAlignment.Center)
	{
		Uri uri2 = new Uri("C:\\");
		uri2 = (uri.Contains("http") ? new Uri(uri) : new Uri(Path.GetFullPath(uri)));
		BitmapImage source = new BitmapImage(uri2);
		return new System.Windows.Controls.Image
		{
			Source = source,
			Width = width,
			Height = height,
			Margin = margin,
			VerticalAlignment = align
		};
	}

	private System.Windows.Controls.Image ChatImage(ImageSource image, Thickness margin, int width = 28, int height = 28, VerticalAlignment align = VerticalAlignment.Center)
	{
		return new System.Windows.Controls.Image
		{
			Source = image,
			Width = width,
			Height = height,
			Margin = margin,
			VerticalAlignment = align
		};
	}

	internal void Window_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		float num = Math.Abs(MarginLeft - 1f) - MarginLeft;
		double num2 = base.ActualWidth - Math.Abs(base.Width - base.ActualWidth);
		double num3 = base.RenderSize.Height - Math.Abs(base.Height - base.RenderSize.Height);
		Math.Abs(richtextbox.Height - num3);
		if (num2 <= 0.0)
		{
			num2 = 100.0;
		}
		if (num3 <= 0.0)
		{
			num3 = 100.0;
		}
		bg_image.Width = num2 + 28.0;
		bg_image.Height = num3 + 28.0;
		matte_image.Width = Math.Abs(num2 * (double)num - 16.0);
		matte_image.Height = num3 + 28.0;
		if (ImageFile != string.Empty)
		{
			ImageSource imageSource = ChatPanel((int)(num2 * (double)MarginLeft - 16.0), 0f, (int)num2, (int)num3, ImageFile);
			if (imageSource != null)
			{
				matte_image.Source = imageSource;
			}
		}
		bg_media.Height = num3;
		richtextbox.Width = Math.Abs(num2 * (double)num - 16.0);
		richtextbox.Height = num3;
		richtextbox_bg.Width = richtextbox.Width;
		richtextbox_bg.Height = richtextbox.Height;
		image_backing.Width = matte_image.Width;
		image_backing.Height = matte_image.Height;
		richtextbox.Margin = new Thickness(num2 * (double)MarginLeft, 0.0, 0.0, 0.0);
		richtextbox_bg.Margin = new Thickness(num2 * (double)MarginLeft, 0.0, 0.0, 0.0);
		matte_image.Margin = new Thickness(richtextbox.Margin.Left, -16.0, 0.0, 0.0);
		image_backing.Margin = matte_image.Margin;
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		text_chat.FontFamily = new System.Windows.Media.FontFamily(Login.ChatFont);
		text_chat_bg.FontFamily = new System.Windows.Media.FontFamily(Login.ChatFont);
		bg_color.Stroke = new SolidColorBrush(Login.ChatBGColor);
		bg_color.Fill = new SolidColorBrush(Login.ChatBGColor);
		text_chat.FontSize = Math.Max(Login.ChatFontSize, 18);
		text_chat_bg.FontSize = Math.Max(Login.ChatFontSize, 18);
		if (Login.FgChatFontColor.Length > 0)
		{
			text_chat.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(Login.FgChatFontColor);
		}
		if (Login.BgChatFontColor.Length > 0)
		{
			text_chat_bg.Foreground = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(Login.BgChatFontColor);
		}
		App.Chat.Width = Login.ChatDimensions.X;
		App.Chat.Height = Login.ChatDimensions.Y;

		//	 TODO DEBUG
		Settings.Instance.list_fonts.SelectedIndex = Settings.GetItemIndex(Login.ChatFont, Settings.Instance.list_fonts.Items);
		Settings.Instance.list_fonts.UpdateLayout();
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		closing = true;
		try
		{
			if (bg_color != null)
			{
				System.Windows.Media.Color color = ((SolidColorBrush)bg_color.Stroke).Color;
				Login.ChatBGColor = System.Windows.Media.Color.FromRgb(color.R, color.G, color.B);
				Login.ChatFontSize = (int)text_chat.FontSize;
				Login.ChatDimensions = new System.Windows.Point(App.Chat.Width, App.Chat.Height);
				Chat_tacular.Properties.Settings.Default.loginChannel = App.channel;
				Chat_tacular.Properties.Settings.Default.loginName = App.username;
			}
		}
		catch
		{
		}
		finally
		{
			if (!flag && MessageBox.Show("Clear saved login and settings details? y/n", "Closing", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				Chat_tacular.Properties.Settings.Default.Reset();
				flag = true;
			}
			Chat_tacular.Properties.Settings.Default.Save();
			Application.Current?.Shutdown();
		}
	}

	private ImageSource ChatPanel(float x, float y, int w, int h, string file)
	{
		if (file == string.Empty)
		{
			return null;
		}
		Task<ImageSource> task = new Task<ImageSource>(delegate
		{
			using Bitmap bitmap = new Bitmap(file);
			int width = bitmap.Width;
			int height = bitmap.Height;
			int num = width * ((PixelFormats.Bgr24.BitsPerPixel + 7) / 8);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				_ = width / height;
				graphics.DrawImage(bitmap, new RectangleF(0f, 0f, width, height), new RectangleF(x, y, width, height), GraphicsUnit.Pixel);
			}
			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
			BitmapSource result2 = BitmapSource.Create(width, height, 96.0, 96.0, PixelFormats.Bgr24, null, bitmapData.Scan0, num * height, num);
			bitmap.UnlockBits(bitmapData);
			return result2;
		});
		if (task.IsCompleted)
		{
			ImageSource result = task.Result;
			task.Dispose();
			return result;
		}
		return null;
	}
}
