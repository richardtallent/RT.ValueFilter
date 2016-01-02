using System.Text.RegularExpressions;
/*
	Copyright 2015 Richard S. Tallent, II

	Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files
	(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge,
	publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to
	do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
	MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
	LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
	CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


namespace RT.ValueFilter {

	public static class StringValidators {

		/// <summary>
		/// This is more efficient than using NewIfNull<string>, as it doesn't need to create a new string.
		/// Note that you should use this BEFORE MOST of the other validators here since they don't have
		/// their own null-checking/coalescing.
		/// </summary>
		public static string EmptyIfNull(this string value) {
			return value ?? string.Empty;
		}

		/// <summary>
		/// Primarily useful for variables going to nullable database fields that should not be sent to
		/// the database as empty strings. Note that this should come AFTER any other validators, since
		/// many of them could strip out everything in the original string and leave you with an empty
		/// string in the end (also, calling this *before* them could result in a null reference error).
		/// </summary>
		public static string NullIfEmpty(this string value) {
			return string.IsNullOrEmpty(value) ? null : value;
		}

		/// <summary>
		/// A more sophisticated version of Substring that won't blow up for null values or those that are
		/// shorter than the maximum. Note that this does NOT coalesce null values -- use EmptyIfNull 
		/// before or after this if you want that behavior.
		/// </summary>
		public static string TruncateAt(this string value, int maxLength) {
			if(string.IsNullOrEmpty(value)) return value;
			if(maxLength < 1) return string.Empty;
			if(maxLength > value.Length) return value;
			return value.Substring(0, maxLength);
		}

		// TODO: RegEx is not the fastest way to filter characters. Many of the functions below
		// should be rewritten for performance.

		/// <summary>
		/// Convert all whitespace or underscore sequences to a single space.
		/// </summary>
		public static string CollapseWhitespace(this string value) {
			return Regex.Replace(value, @"[_\s]+", " ", RegexOptions.Compiled);
		}

		/// <summary>
		/// Removes control characters -- almost always a good idea.
		/// </summary>
		public static string NoControlChars(this string value) {
			return Regex.Replace(value, @"[\u0000-\u0008\u000B\u000C\u000E-\u001F]", string.Empty, RegexOptions.Compiled);
		}

		/// <summary>
		/// Note that this is ALL digit characters, not just 0-9
		/// </summary>
		public static string DigitsOnly(this string value) {
			return Regex.Replace(value, @"\D", string.Empty, RegexOptions.Compiled);
		}

		/// <summary>
		/// Only Arabic numerals, i.e., 0-9. This is probably the droid you're looking for.
		/// </summary>
		public static string ArabicDigitsOnly(this string value) {
			return Regex.Replace(value, @"[^0-9]", string.Empty, RegexOptions.Compiled);
		}

		/// <summary>
		/// Whitelists characters used in personal names. This includes:
		///		- Various letters (not \w, which would include some undesired number characters, and joining characters like underscores)
		///		- Numbers 0-9 (for suffixes and some military prefixes)
		///		- Ordinal indicators and lowercase superscript letters (for suffixes like Richard 1ˢᵗ)
		///		- Roman numerals (for suffixes like Henry Ⅷ)
		///		- periods (for initials)
		///		- commas (for suffixes)
		///		- hyphens
		///		- spaces (even within a single name part)
		///		- apostrophes, and right single quotes (e.g., O'Malley).
		///
		/// Situations this *won't* cover:
		///		- double quotes (used in nicknames, e.g., 'Vinny "The Dog" Jones').
		///		- parenthesized previous/maiden names, often used in genealogy and obituaries
		///	
		/// For most purposes, this is a good way to prevent people from abusing name or username fields while still allowing personal
		/// names in most cultures.
		/// </summary>
		public static string NameCharsOnly(this string value) {
			return Regex.Replace(value, @"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Lm}\u00aa\u00ba\.\u1d43-\u1d63,0-9 ’'\u2160-\u216C-]", string.Empty, RegexOptions.Compiled);
		}

		public static string WordCharsOnly(this string value) {
			return Regex.Replace(value, @"\W", string.Empty, RegexOptions.Compiled);
		}

		public static string NoWhitespace(this string value) {
			return Regex.Replace(value, @"\s", string.Empty, RegexOptions.Compiled);
		}

		/// <summary>
		/// If a string is permitted to include an A tag, you should use this to at least prevent someone 
		/// from maliciously using HTML events (or the href) on the tag to run arbitrary javascript.
		/// 
		/// Note that this MUST be called RECURSIVELY to prevent someone from doing something like this:
		///		a href="javajavascript:script:Evil()"
		///	
		/// Honestly it's just *not* a good idea to allow untrusted HTML, it's far better to strip it out
		/// and use MarkDown, BBCode, etc. for user-editable rich text, or have very strict, server-side
		/// whitelist of allowed tags *and* attributes (which is outside the scope of this project but
		/// could be implemented as a Filter).
		/// </summary>
		public static string NoScripts(this string value) {
			const string pattern = @"(?:javascript:|onclick|ondblclick|onblur|onchange|oncontextmenu|onfocus|ondrag|ondrop|onmouse)";
			var re = new Regex(pattern, RegexOptions.IgnoreCase & RegexOptions.Compiled);

			while(re.IsMatch(value)) {
				value = re.Replace(value, string.Empty);
			}

			return value;
		}

		/// <summary>
		/// Using HtmlDecode is preferable, since it won't lose anything in translation. This is provided
		/// for situations where even an attempted HTML Entity reference would be disallowed.
		/// </summary>
		public static string NoHtmlReferences(this string value) {
			return Regex.Replace(value, @"&(?:#x?[0-9]+|[A-Za-z][A-Za-z0-9]+);", string.Empty, RegexOptions.Compiled);
		}

		/// <summary>
		/// This should cover a wide range of HTML tag attempts, even badly-formed ones that browsers
		/// might still accept. This cuts from any less-than until the next less-than or greater than,
		/// or until the end of the string.
		/// 
		/// If incoming data may have legitimate <> characters, use strict=false so less-than characters
		/// that could be mathematical in nature are not stripped. This isn't foolproof, but should
		/// avoid stripping out less-than symbols (and the text following) used in the usual ways.
		/// 
		/// That said, always HtmlEncode the returned string before it goes back to a browser, just in
		/// case -- some browsers can be quite promiscuous in accepting badly-formed tags. This is 
		/// doubly true if you call HtmlDecode *after* this, to avoid tags being "created" by using 
		/// &gt; / &lt; or numeric equivalents.
		/// </summary>
		public static string NoHtmlTags(this string value, bool strict) {
			var pattern = strict ?
				@"<[^<>]*(?:[<>]|$)"
				: @"<[^<> 0-9=+\(\[\{Σ±-]+(?:[<>]|$)";
			return Regex.Replace(value, pattern, string.Empty, RegexOptions.Compiled);
		}

		public static string NoHtmlTags(this string value) {
			return value.NoHtmlTags(strict: true);
		}

	}

}