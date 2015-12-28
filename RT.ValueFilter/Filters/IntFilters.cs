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

	public static class IntFilters {

		public static int AtLeast(this int value, int minValue) {
			return (value.CompareTo(minValue) < 0) ? minValue : value;
		}

		public static int NotMoreThan(this int value, int maxValue) {
			return (value.CompareTo(maxValue) > 0) ? maxValue : value;
		}

		/// <summary>
		/// Within the Gregorian calendar range for a Month value
		/// </summary>
		public static int InMonthRange(this int value) {
			return value.AtLeast(1).NotMoreThan(12);
		}

		/// <summary>
		/// Whole numbers only. Common for ages, counts, etc.
		/// </summary>
		public static int AtLeastZero(this int value) {
			return value.AtLeast(0);
		}

		/// <summary>
		/// Positive numbers only, common for database IDs, etc.
		/// </summary>
		public static int AtLeastOne(this int value) {
			return value.AtLeast(1);
		}

	}

}