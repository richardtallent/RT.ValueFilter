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
	/// This works the same way as the class. Lower potential overhead, but since this is mutable,
	/// it's best to only use this in situations with very limited access, such as private backing
	/// fields (and you should be in the habit, even privately, of accessing their inner value 
	/// through the exposed property getter, not messing with the struct directly.)
	/// </summary>
	public struct FilteredStruct<T> {

		private T value;

		public FilteredStruct(Func<T, T> validateFunction, T initialValue = default(T)) {
			Filter = validateFunction;
			// We can't set Value yet (struct limitation), but we still need to ensure that
			// initialValue is valid, so we call Filter() here as well.
			value = Filter(initialValue);
		}

		public T Value {
			get { return value; }
			set { this.value = Filter(value); }
		}

		public Func<T, T> Filter { get; set; }

		public static implicit operator T(FilteredStruct<T> fitleredValue) {
			return fitleredValue.Value;
		}

    }

}