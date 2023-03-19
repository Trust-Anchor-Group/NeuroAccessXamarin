using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Waher.Content.Markdown;
using Waher.Events;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Extensions
{
	/// <summary>
	/// An extensions class for the <see cref="string"/> class.
	/// </summary>
	public static class StringExtensions
	{
		/// <summary>
		/// Returns the number of Unicode symbols, which may be represented by one or two chars, in a string.
		/// </summary>
		public static int GetUnicodeLength(this string Str)
		{
			if (Str is null)
				throw new ArgumentNullException(nameof(Str));

			Str = Str.Normalize();

			int UnicodeCount = 0;
			for (int i = 0; i < Str.Length; i++)
			{
				UnicodeCount++;

				// Jump over the second surrogate char.
				if (char.IsSurrogate(Str, i))
				{
					i++;
				}
			}

			return UnicodeCount;
		}
	}
}
