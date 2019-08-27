namespace OCR_Webservice_Model
{
	public class HDFile
	{
		public HDFile(string name, string text, string tag = null)
		{
			Name = name;
			Text = text;
			Tag = tag;
		}

		public string Name { get; set; }

		public string Tag { get; set; }

		public string Text { get; set; }
	}
}