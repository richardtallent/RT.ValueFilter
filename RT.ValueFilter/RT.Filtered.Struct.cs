using System;
using System.Collections.Generic;
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
	public struct FilteredStruct<T> where T : IEquatable<T> {

		private T pvtValue;

		public FilteredStruct(Func<T, T> validateFunction, T initialValue) {
			Filter = validateFunction;
			pvtValue = Filter(initialValue);
		}

		public FilteredStruct(Func<T, T> validateFunction) {
			Filter = validateFunction;
			pvtValue = Filter(default(T));
		}

		public T Value {
			get { return pvtValue; }
			set { this.pvtValue = Filter(value); }
		}

		public Func<T, T> Filter { get; set; }

		public override int GetHashCode() {
			return pvtValue.GetHashCode();
		}

		public override bool Equals(object obj) {
			if(obj is FilteredStruct<T>) {
				return EqualityComparer<T>.Default.Equals(this.Value, ((FilteredStruct<T>)obj).Value);
			} else if(obj is T) {
				return EqualityComparer<T>.Default.Equals(this.Value, (T)obj);
			}
			return false;
		}

		public static bool operator ==(FilteredStruct<T> left, FilteredStruct<T> right) {
			return left.Equals(right);
		}

		public static bool operator !=(FilteredStruct<T> left, FilteredStruct<T> right) {
			return !left.Equals(right);
		}

	}

}