﻿using System;
using Xunit;
using RT.ValueFilter;
using RT.ValueFilter.Struct;

namespace Tests {

	public class NameString : RT.ValueFilter.Class.Filtered<string> {
		public NameString() : base(
			value => value.EmptyIfNull()
			.KeepNameCharsOnly()
			.Trim()
			.CollapseWhiteSpace()
			.TruncateIfLongerThan(255)
		)
		{ }
	}

	public static class Validators {

		public static string NameValidator(this string s) =>
			s.EmptyIfNull()
			.KeepNameCharsOnly()
			.Trim()
			.CollapseWhiteSpace()
			.TruncateIfLongerThan(255);

		public static int Min(this int i, int j) => Math.Min(i, j);
		public static int Max(this int i, int j) => Math.Max(i, j);

		public static string ValidateCASRN(this string s) {
			int sum = 0;
			if (string.IsNullOrEmpty(s)) return string.Empty;
			var l = s.Length;
			for (var i = 1; i <= l; i++)
			{
				var c = s[i - 1];
				if (c == '-') continue;
				var n = c - '0';
				if (n < 0 || n > 9) return string.Empty;
				if (i == l) return n == (sum % 10) ? s : string.Empty;
				sum += n * (i + 1);
			}
			return string.Empty;
		}

	}

	public class LatLong {
		private Filtered<int> _lat = new Filtered<int>(v => v.Min(-90).Max(90));
		private Filtered<int> _long = new Filtered<int>(v => v.Min(-180).Max(180));
		public int Latitude { get => _lat; set => _lat.Value = value; }
		public int Longitude { get => _long; set => _long.Value = value; }
	}

	public class Customer {
		private Filtered<string> _firstName = new Filtered<string>(Validators.NameValidator);
		private Filtered<string> _lastName = new Filtered<string>(Validators.NameValidator);
		public string FirstName { get => _firstName; set => _firstName.Value = value; }
		public string LastName { get => _lastName; set => _lastName.Value = value; }

		private Filtered<int> _age = new Filtered<int>(value => Math.Min(Math.Max(0, value), 130));
		public int Age { get => _age; set => _age.Value = value; }
	}

	public class FilterTests {

		[Fact] public void TestRemoveNonDigits() {
			var t = "Hello world 123";
			var f = new Filtered<string>(StringFilters.RemoveNonDigits, t);
			Assert.Equal("123", f);
		}

		[Fact] public void TestTrim() {
			var o = new Customer();
			Assert.False(o.FirstName == null);
			o.FirstName = "  Richard  ";
			Assert.Equal("Richard", o.FirstName);
		}

	}

}