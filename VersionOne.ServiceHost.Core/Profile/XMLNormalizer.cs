/*(c) Copyright 2010, VersionOne, Inc. All rights reserved. (c)*/
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace VersionOne.Profile
{
	public static class XmlNormalizer
	{
		private static Regex _regexencode = new Regex("[^A-Z_a-z0-9]", RegexOptions.Compiled);
		private static Regex _regexdecode = new Regex("_x([0-9|A-F|a-f]{2})", RegexOptions.Compiled);

		public static string TagEncode(string xmlTag)
		{
			return _regexencode.Replace(xmlTag, EncoderMatch);
		}

		public static string TagDecode(string xmlTag)
		{
			return _regexdecode.Replace(xmlTag, DecoderMatch);
		}

		private static string EncoderMatch(Match m)
		{
			return "_x" + string.Format("{0:X2}", Encoding.ASCII.GetBytes(m.Value)[0]);
		}

		private static string DecoderMatch(Match m)
		{
			return Convert.ToChar(Int32.Parse(m.Groups[1].Value, NumberStyles.HexNumber)).ToString();
		}
	}
}