﻿using System;
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

namespace RT.ValueFilter.Struct {

	/// <summary>
	/// This works the same way as the class. Lower potential overhead, but since this is mutable,
	/// it's best to only use this in situations with very limited access, such as private backing
	/// fields (and you should be in the habit, even privately, of accessing their inner value 
	/// through the exposed property getter, not messing with the struct directly.)
	/// </summary>
	public struct Filtered<T> : IEquatable<Filtered<T>> {

		private T pvtValue;
		private Func<T, T> pvtFilter;

		/// <summary>
		/// There is no parameterless constructor because we MUST validate the initial value (even if
		/// it is default), we need the Filter set to do so. I prefer initializers to parameterized
		/// constructors, but without this as the only constructor, we can't guarantee that all new()
		/// calls will have an initializer to set the Filter, or that all subclass constructors will
		/// call this base constructor to do so.
		/// </summary>
		public Filtered(Func<T, T> validateFunction, T initialValue = default(T)) {
			// Struct won't allow calling the setter before the backing field is set, so
			// the implementation is different than the class version.
			if(validateFunction == null) throw new ArgumentNullException(nameof(validateFunction));
			pvtFilter = validateFunction;
			pvtValue = default(T);
			Value = initialValue;
		}

		/// <summary>
		/// Never let a bad value be set. Usually cheaper to do this in the setter anyway.
		/// </summary>
		public T Value {
			get { return pvtValue; }
			set { this.pvtValue = pvtFilter(value); }
		}

		/// <summary>
		/// Here is where your magic is injected.
		/// </summary>
		public Func<T, T> Filter {
			get {
				return pvtFilter;
			}
			set {
				if(value == null) throw new ArgumentNullException(nameof(Filter));
				// New Filter should be applied to the existing Value
				Value = pvtValue;
			}
		}

		/// <summary>
		/// Recommended by IEquatable. Assumes only the value matters, not the filter.
		/// </summary>
		public override int GetHashCode() => pvtValue.GetHashCode();

		/// <summary>
		/// Assumes only the value matters, not the filter.
		/// </summary>
		public bool Equals(Filtered<T> other) => EqualityComparer<T>.Default.Equals(this.Value, ((Filtered<T>)other).Value);

		/// <summary>
		/// Assumes only the value matters, not the filter.
		/// </summary>
		public override bool Equals(object obj) {
			if(obj is Filtered<T>) {
				return EqualityComparer<T>.Default.Equals(this.Value, ((Filtered<T>)obj).Value);
			} else if(obj is T) {
				return EqualityComparer<T>.Default.Equals(this.Value, (T)obj);
			}
			return false;
		}

		/// <summary>
		/// Assumes only the value matters, not the filter.
		/// </summary>
		public static bool operator ==(Filtered<T> left, Filtered<T> right) => left.Equals(right);

		/// <summary>
		/// Assumes only the value matters, not the filter.
		/// </summary>
		public static bool operator !=(Filtered<T> left, Filtered<T> right) => !left.Equals(right);

	}

}