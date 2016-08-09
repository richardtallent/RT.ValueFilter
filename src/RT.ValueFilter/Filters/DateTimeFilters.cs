using System;
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

	/// <summary>
	/// Common useful filters for DateTime values
	/// </summary>
	public static class DateTimeFilters {

		private static DateTime Year1900 = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// Sets the <paramref name="value"/> to <paramref name="minValue"/> if it is before that date.
		/// </summary>
		public static DateTime OnOrAfter(this DateTime value, DateTime minValue) {
			return (value.ToUniversalTime().CompareTo(minValue.ToUniversalTime()) < 0) ? minValue : value;
		}

		/// <summary>
		/// Sets the <paramref name="value"/> to <paramref name="maxValue"/> if it is after that date.
		/// </summary>
		public static DateTime OnOrBefore(this DateTime value, DateTime maxValue) {
			return (value.ToUniversalTime().CompareTo(maxValue.ToUniversalTime()) > 0) ? maxValue : value;
		}

		/// <summary>
		/// Useful for restricting values that will be used in Excel or for `smalldatetime` SQL types
		/// </summary>
		public static DateTime OnOrAfter1900(this DateTime value) {
			return value.ToUniversalTime().OnOrAfter(Year1900);
		}

		/// <summary>
		/// Useful for restricting search criteria and validating input
		/// </summary>
		public static DateTime BeforeFuture(this DateTime value) {
			return value.ToUniversalTime().OnOrBefore(DateTime.UtcNow);
		}

		/// <summary>
		/// Useful for restricting search criteria
		/// </summary>
		public static DateTime NotMoreThanYearsAgo(this DateTime value, int years) {
			var dMin = DateTime.UtcNow.AddYears(0 - years.AtLeastZero());
			return value.OnOrAfter(dMin);
		}

		/// <summary>
		/// Useful for restricting search criteria
		/// </summary>
		public static DateTime AtLeastYearsAgo(this DateTime value, int years) {
			var dMax = DateTime.UtcNow.AddYears(0 - years.AtLeastZero());
			return value.OnOrBefore(dMax);
		}

	}

}