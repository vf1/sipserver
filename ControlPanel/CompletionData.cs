using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace ControlPanel
{
	public class CompletionData : ICompletionData
	{
		public CompletionData(string text)
		{
			this.Text = text;
		}

		public ImageSource Image
		{
			get { return null; }
		}

		public string Text
		{
			get;
			private set;
		}

		public object Content
		{
			get { return this.Text; }
		}

		public object Description
		{
			get { return this.Text; }
		}

		public double Priority
		{
			get { return 0; }
		}

		public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			textArea.Document.Replace(completionSegment, this.Text);
		}
	}
}
