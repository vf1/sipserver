using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace ControlPanel
{
	/// <summary>
	/// Interaction logic for EditXmlConfig.xaml
	/// </summary>
	public partial class EditXmlConfig : Window
	{
		private Programme programme;
		private CompletionWindow completionWindow;
		private Dictionary<string, CompletionData[]> completions;

		public EditXmlConfig()
		{
			InitializeComponent();
			InitializeCompletionList();

			Loaded += EditXmlConfig_Loaded;
		}

		private void EditXmlConfig_Loaded(object sender, RoutedEventArgs e)
		{
			programme = DataContext as Programme;

			textEditor.ShowLineNumbers = true;
			textEditor.TextChanged += textEditor_TextChanged;
			textEditor.TextArea.TextEntered += new TextCompositionEventHandler(textEditor_TextEntered);

			programme.PropertyChanged += Programme_PropertyChanged;
		}

		private void textEditor_TextEntered(object sender, TextCompositionEventArgs e)
		{
			if (e.Text == "<" || e.Text == " ")
			{
				CompletionData[] completions1;
				if (completions.TryGetValue("!" + e.Text + "!" + GetActiveElement(1), out completions1))
				{
					completionWindow = new CompletionWindow(textEditor.TextArea);
					var completions2 = completionWindow.CompletionList.CompletionData;

					foreach (var completion in completions1)
						completions2.Add(completion);

					completionWindow.Show();
					completionWindow.Closed += delegate
					{
						completionWindow = null;
					};
				}
			}
			if (e.Text == ">")
			{
				var tag = GetOpenTag(1);
				if (tag != string.Empty)
					textEditor.Document.Insert(textEditor.CaretOffset, tag.Insert(1, "/") + ">");
			}
		}

		private void Programme_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == @"XmlConfig")
				textEditor.Text = (sender as Programme).XmlConfig;
		}

		private void textEditor_TextChanged(object sender, EventArgs e)
		{
			programme.XmlConfig = textEditor.Text;
		}

		private void MenuItem_WordWrap(object sender, RoutedEventArgs e)
		{
			textEditor.WordWrap = !textEditor.WordWrap;
		}

		private void MenuItem_ShowLineNumbers(object sender, RoutedEventArgs e)
		{
			textEditor.ShowLineNumbers = !textEditor.ShowLineNumbers;
		}

		private void InitializeCompletionList()
		{
			completions = new Dictionary<string, CompletionData[]>();

			completions.Add("!<!root", new CompletionData[] { new CompletionData(@"sipServer"), });
			completions.Add("!<!<sipServer>", new CompletionData[] 
			{ 
				new CompletionData(@"portForwardings"), 
				new CompletionData(@"turnServers"), 
				new CompletionData(@"voipProviders"), 
			});
			completions.Add("!<!<sipServer><portForwardings>", new CompletionData[] { new CompletionData(@"add"), new CompletionData(@"clear"), new CompletionData(@"remove"), });
			completions.Add("!<!<sipServer><turnServers>", new CompletionData[] { new CompletionData(@"add"), new CompletionData(@"clear"), new CompletionData(@"remove"), });
			completions.Add("!<!<sipServer><voipProviders>", new CompletionData[] { new CompletionData(@"add"), new CompletionData(@"clear"), new CompletionData(@"remove"), });

			completions.Add("! !<sipServer", new CompletionData[] 
			{ 
				new CompletionData(@"udpPort"),
				new CompletionData(@"tcpPort"),
				new CompletionData(@"domainName"),
				new CompletionData(@"isAuthorizationEnabled"),
				new CompletionData(@"isAuthIntEnabled"),
				new CompletionData(@"isTracingEnabled"),
				new CompletionData(@"wcfServiceAddress"),
				new CompletionData(@"administratorPassword"),
				new CompletionData(@"activeDirectoryGroup"),
				new CompletionData(@"isActiveDirectoryEnabled"),
				new CompletionData(@"addToWindowsFirewall"),
				new CompletionData(@"wwwPath"),
			});

			completions.Add("! !<sipServer><portForwardings><add", new CompletionData[] 
			{ 
				new CompletionData(@"protocol"),
				new CompletionData(@"localEndpoint"),
				new CompletionData(@"externalEndpoint"),
			});

			completions.Add("! !<sipServer><turnServers><add", new CompletionData[] 
			{ 
				new CompletionData(@"fqdn"),
				new CompletionData(@"udpPort"),
				new CompletionData(@"tcpPort"),
				new CompletionData(@"location"),
			});

			completions.Add("! !<sipServer><voipProviders><add", new CompletionData[] 
			{ 
				new CompletionData(@"serverHostname"),
				new CompletionData(@"outboundProxyHostname"),
				new CompletionData(@"protocol"),
				new CompletionData(@"localEndpoint"),
				new CompletionData(@"username"),
				new CompletionData(@"displayName"),
				new CompletionData(@"authenticationId"),
				new CompletionData(@"password"),
				new CompletionData(@"forwardIncomingCallTo"),
				new CompletionData(@"restoreAfterErrorTimeout"),
			});


			completions.Add("! !<configuration><sipServer", completions["! !<sipServer"]);

			var completions2 = new Dictionary<string, CompletionData[]>(completions, completions.Comparer);
			foreach (var completion in completions2)
			{
				if (completion.Key.Contains(@"<sipServer>"))
					completions.Add(completion.Key.Replace(@"<sipServer>", @"<configuration><sipServer>"), completion.Value);
			}
		}

		private string GetActiveElement(int backOffset)
		{
			string element = string.Empty;

			if (textEditor.CaretOffset > backOffset)
			{
				var text = textEditor.Document.Text.Remove(textEditor.CaretOffset - backOffset);

				var regex = new Regex(@"<(?<tag>[^!?][^\s/>]*).*?>"); // @"<(?<tag>[^!?][^\s/>]*).*?[^/]?>"
				var matches = regex.Matches(text);

				for (int i = matches.Count - 1, skip = 0; i >= 0; i--)
				{
					var tag = matches[i].Groups[@"tag"].Value;
					var selfclose = matches[i].Value.EndsWith("/>");
					if (tag[0] == '/')
					{
						skip++;
					}
					else if (selfclose == false)
					{
						if (skip > 0)
						{
							skip--;
						}
						else
						{
							element = @"<" + tag + @">" + element;
						}
					}
				}

				element += GetOpenTag(text);

				if (element == string.Empty)
					element = "root";
			}

			return element;
		}

		private string GetOpenTag(int backOffset)
		{
			string result = string.Empty;

			if (textEditor.CaretOffset > backOffset)
				result = GetOpenTag(textEditor.Document.Text.Remove(textEditor.CaretOffset - backOffset));

			return result;
		}

		private static string GetOpenTag(string text)
		{
			int open = text.LastIndexOf('<');
			int close = text.LastIndexOf('>');

			if (close < open)
			{
				int space = text.IndexOfAny(new char[] { ' ', '\t', '\r', '\n' }, open);
				if (space < 0)
					space = text.Length;

				return text.Substring(open, space - open);
			}

			return string.Empty;
		}
	}
}
