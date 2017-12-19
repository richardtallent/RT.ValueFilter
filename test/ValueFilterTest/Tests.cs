using System;
using Xunit;
using RT.ValueFilter;
using RT.ValueFilter.Struct;

namespace Tests
{

	public class Example
	{
		private int _age;
		public int Age
		{
			get => _age;
			set => _age = Math.Min(Math.Max(0, value), 130);
		}
	}

	public static class Validators
	{
		public static string NameValidator(this string s) =>
			s.EmptyIfNull()
			.KeepNameCharsOnly()
			.Trim()
			.CollapseWhiteSpace()
			.TruncateIfLongerThan(255);
	}

	public class TestClass
	{
		public string NotNullString { get; set; } = new Filtered<string>(value => value.EmptyIfNull());

		public int IntZeroTo100 { get; set; } = new Filtered<int>(value => value.NotMoreThan(100).AtLeastZero());


		public string Name { get; set; } = new Filtered<string>(value => value.NameValidator());

		public string Name2 { get; set; } = new FilteredNameString();

	}

	public class FilteredNameString : RT.ValueFilter.Class.Filtered<string>
	{
		public FilteredNameString() : base(
			value => value.EmptyIfNull()
			.KeepNameCharsOnly()
			.Trim()
			.CollapseWhiteSpace()
			.TruncateIfLongerThan(255)
		)
		{
			// Other constructor code here
		}
	}

	public class Tests
	{
		[Fact]
		public void Test1()
		{
			Assert.True(true);
		}

		[Fact]
		public void TestRemoveNonDigits()
		{
			var t = "Hello world 123";
			var f = new Filtered<string>(StringFilters.RemoveNonDigits, t);
			Assert.True(f.Value == "123");
		}

		public void TestImplicitConvertBackingField()
		{
			var o = new TestClass();
			o.NotNullString = null;
			Assert.False(o.NotNullString == null);
			o.IntZeroTo100 = 200;
			Assert.True(o.IntZeroTo100 == 100);
		}

	}

}