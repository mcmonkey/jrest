using System;
using System.Text;

namespace JRest.Util
{
	public class Base64StringEncoding
	{

		public static string decode ( string value, int index, int count, Encoding encoding )
		{
			char[] value_chars = value.ToCharArray(index, count);
			byte[] bytes = Convert.FromBase64CharArray( value_chars, 0, value_chars.Length);
			return new String ( Encoding.UTF8.GetChars ( bytes ) );
		}

		public static string decode ( string value, Encoding encoding )
		{
			return decode(value, 0, value.Length, encoding);
		}
	}
}
