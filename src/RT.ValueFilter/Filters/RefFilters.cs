﻿using System;
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
	/// Don't use this for strings, there's a more efficient version for that use case.
	/// This is good for any reference that could be accidentally set to null, and if
	/// it is, should instead be created using the parameterless constructor.
	/// 
	/// This is basically syntactic sugar.
	/// </summary>
	public static class RefFilters {

		/// <summary>
		/// If <paramref name="value"/> is null, instantiates a new object and returns it.
		/// If not, returns the value.
		/// </summary>
		/// <typeparam name="T">Must be a class, not a nullable value type.</typeparam>
		public static T NewIfNull<T>(this T value) where T : class, new() {
			return value ?? new T();
		}

		/// <summary>
		/// If <paramref name="value"/> is null, throws an exception.
		/// </summary>
		/// <typeparam name="T">Must be a class, not a nullable value type.</typeparam>
		public static T ErrorIfNull<T>(this T value) where T : class {
			if(value == null) throw new ArgumentNullException(nameof(value));
			return value;
		}

	}

}