using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DistractorClouds.PanelGeneration
{
	/// <summary>
	/// Source: https://github.com/tiago-peres/blog/blob/master/csvreader/CSVReader.cs
	/// </summary>
	public class CSVReader
	{
	
		static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
		static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
		static char[] TRIM_CHARS = { '\"' };

		public static List<Dictionary<string, string>> Read(string fileContent)
		{
			var list = new List<Dictionary<string, string>>();

			var lines = Regex.Split (fileContent, LINE_SPLIT_RE);

			if(lines.Length <= 1) return list;

			var header = Regex.Split(lines[0], SPLIT_RE);
			for(var i=1; i < lines.Length; i++) {

				var values = Regex.Split(lines[i], SPLIT_RE);
				if(values.Length == 0 ||values[0] == "") continue;

				var entry = new Dictionary<string, string>();
				for(var j=0; j < header.Length && j < values.Length; j++ ) {
					string value = values[j];
					value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
					
					entry[header[j]] = value;
				}
				list.Add (entry);
			}
			return list;
		}
	}
}
