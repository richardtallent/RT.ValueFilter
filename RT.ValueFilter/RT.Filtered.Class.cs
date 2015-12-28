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
	/// If you have a trivial pre-built filter or you want to determine the filter at runtime, use an
	/// *initializer* when creating instances of this class.
	/// 
	/// If you want to subclass this so the filter is already assigned, you *must* have your subclass
	/// constructor(s) call the base *parameterized* constructor with your filter (and, optionally,
	/// an initial value). Otherwise the base *parameterless* constructor will execute before your own
	/// constructor, and with Filter not set yet, will result in a null reference error. (You could
	/// also always instantiate your subclass with an initializer to set the Filter, but that would
	/// make the subclass itself useless.)
	/// 
	/// Note that if your chosen Filter would throw an exception for default(T), you MUST set a valid
	/// initial value (either using the parameterized constructor, or an initializer).
	/// </summary>
	public class Filtered<T> {

		private T value;

		/// <summary>
		/// There is no parameterless constructor because we MUST validate the initial value (even if
		/// it is default), we need the Filter set to do so. I prefer initializers to parameterized
		/// constructors, but without this as the only constructor, we can't guarantee that all new()
		/// calls will have an initializer to set the Filter, or that all subclass constructors will
		/// call this base constructor to do so.
		/// </summary>
		public Filtered(Func<T, T> filter, T initialValue = default(T)) {
			Filter = filter;
			Value = initialValue;
		}

		/// <summary>
		/// Never let a bad value be set. Usually cheaper to do this in the setter anyway.
		/// </summary>
		public T Value {
			get { return value; }
			set { this.value = Filter(value); }
		}

		/// <summary>
		/// Here is where your magic is injected.
		/// </summary>
		public Func<T, T> Filter { get; set; }

		/// <summary>
		/// Syntactic sugar for getting the underlying value.
		/// </summary>
		public static implicit operator T(Filtered<T> strongValue) {
			return strongValue.Value;
		}

    }

}