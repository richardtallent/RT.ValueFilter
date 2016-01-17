WHAT IS RT.VALUEFILTER?
=======================
`RT.ValueFilter` is a library that allows you to quickly and easily constrain the value of a variable. It's a prophylactic of sorts, a "firewall" against invalid data.

One of the limitations of using the native .NET value and reference types (`int`, `string`, etc.) is that they are, *in a sense*, loosely-typed. By this, I mean that the *logial type* of our variables rarely would allow every value these base types are capable of storing, but we use them as *surrogates* for the "real" type we are describing.

For example, we might use an `int` instance for a variable called Age, but logically, this variable should only store positive values, and there may very well be a lower and upper logical limit. Allowing this variable to store any valid `int` value promiscuously can result in undetected bad data or malicious abuse. Sure, `byte` or `uint` might be *closer* to our intent, but they would still be an imperfect match.

Likewise, a `string` can be `null`, can be any length up to around 2GB (or larger in some cases), and can store any Unicode endpoint. By contrast, many of variables we assign to type `string` should be significantly more limited in their length or allowed characters, or they should never be null.

Often, these limits we wish to place on a value don't really rise to the level of raising an exception when there is an issue, they can simply be modified to the correct value.

On the user-facing side, we can and should provide visual field validation so users can't shoot themselves (or us) in the foot. The same goes for APIs that a user (or person with malicious intent) could access.

However, we cannot rely on the UI or API layer to protect ourselves from invalid values. Even trusting our public property setters is problematic, as our own code can still access the private backing fields, and could set a bad value based on data coming in from a database, or another API, or an algorithm that has an edge case we didn't anticipate. (This answers the question that inevitably comes up of "isn't this why we have property setters in the first place?".)

Unfortunately, many of the types we might want to restrict or filter values for cannot be inherited, so we can't *directly* override them to only accept an appropriate subset of values.

This library is intended to be a reliable, low-overhead, consistent mechanism that allows you to wrap any variable with a set of filters that will always be applied to the value. You can create filters to respond to invalid incoming values by replacing or transforming them into valid values, throwing an exception, logging them, or performing any other desired action.

The end result is that you have a strong*er* type that more closely resembles the logical meaning of your variable, and you can build a library of filters that you can use across your project.

STATUS
======
This library is an experiment for a new personal project. I've been doing something like this ad-hoc here and there in my work, but this library is an attempt to clean up, centralize, and standardize how I do this. I'm just starting this process, but having the code up on GitHub and NuGet is convenient for me, so if someone else benefits from this library and/or wants to contribute, awesome!

IMPLEMENTATION
==============
The mechanism for this is simple: an instance of a generic type (implemented both as a `class` and a `struct`, you choose which you prefer) *wraps* your actual variable. When creating the instance, you specify the interior type and a `Func<T, T>` delegate that will perform the filtering.

A small library of delegate filter functions is also provided. Each is implemented as an extension method, which allows them to be chained fluently. This simplifies creating anonymous `Func<T, T>` functions, and also encourages composing complex filters from simpler ones.

SHOW ME SOME CODE!
==================
Let's say my `Customer` class has a `string` property called `Name`, and I want to limit the acceptable values. I could do something like this:

	using RT.ValueFilter;

	public class Customer {

		private readonly Filtered<string> name = new Filtered<string>(value =>
			value.EmptyIfNull()
				.KeepNameCharsOnly()
				.Trim()
				.CollapseWhiteSpace()
				.TruncateIfLongerThan(255)
			);

		public string Name {
			get { return name.Value; }
			set { name.Value = value; }
		}

	}

Now, the *private* value of `name` is fully protected from both internal and external attempts to set it to a value that is not consistent with my logical definition of a "name."

This includes *even the initial value*: since I didn't use the constructor that includes an initial value parameter, the constructor will use `default(T)`, but will pass it through the filter I provided in the first constructor argument. Because of this, I don't need "magic" initial values that pass muster, I can allow the filter to set the initial value to something befitting the logic. (In this example, `EmptyIfNull()` coalesces null values to empty strings, so that would end up being my initial value.)

It's important to note that this doesn't replace the need for client-side validation and error messages, it just ensures that the values stored will *never* be invalid, even when client-side validation is bypassed or broken for some reason, or when values are modified by something other than the UI.

If I find myself using the same logic for other name-like fields, I can create a static function, then use it for any number of `Filtered<string>` instances:

	using RT.ValueFilter;

	public class Customer {
	    private Filtered<string> firstName = new Filtered<string>(MyFilters.NameFilter);
	    private Filtered<string> lastName = new Filtered<string>(MyFilters.NameFilter);
	    private Filtered<string> companyName = new Filtered<string>(MyFilters.NameFilter);

		public string FirstName {
			get { return firstName.Value; }
			set { firstName.Value = value; }
		}

		public string LastName {
			get { return lastName.Value; }
			set { lastName.Value = value; }
		}

		public string CompanyName {
			get { return companyName.Value; }
			set { companyName.Value = value; }
		}

	}

	// ...
	public static class MyFilters {

		public static Filtered<string> NameFilter(string value) => value.EmptyIfNull()
			.KeepNameCharsOnly()
			.Trim()
			.CollapseWhitespace()
			.TruncateIfLongerThan(255);

	}

You could also inherit from `Filtered<T>`, creating your own derived type with the appropriate filters already in place:

    public class FilteredNameString : Filtered<string> {

		public FilteredNameString() : base(
			value => value.EmptyIfNull()
			.KeepNameCharsOnly()
			.Trim()
			.CollapseWhitespace()
			.TruncateIfLongerThan(255)
		) {
			// Other constructor code here
		}

    }

(The above, of course, would not work if you choose the `struct` version.)

This isn't limited to core .NET types like `int` and `string`... you can use it to validate *any* object, whether the type is part of the BCL, a third-party library, or your own classes. For example, if you have an email property that you want to ensure *only* gets set to your company's email domain, you could use something like this:

	using System.Net.Mail;

    public class MyFilters {

		public static MailAddress NullIfNotCompanyEmail(this MailAddress address) {
			if(address==null) return null;
			return String.Equals(address.Host, "acme.com", StringComparison.OrdinalIgnoreCase)
				? address : null;
		}

	}

USAGE
================
My use of this library is mostly limited to private fields, usually ones that are backing public properties. I prefer using `readonly FilteredStruct<T>` for this, as they don't have the additional (albeit tiny) overhead of an object.

I wouldn't suggest making the `Filtered<T>` types public. Using the underlying type is a better choice, as it will be more compatible with OR/M or serialization libraries, and will allow you to change the filter implementation without changing your interface.

As for defining the filters, I tend to use one-off anonymous functions. For filters (or parts of them) I might reuse, I create static methods in a static utility class.

WHY NOT JUST USE A CLASS WITH A VIRTUAL FILTER METHOD?
======================================================
I chose to use `Func<T, T>` for Filter instead of a virtual method for a few reasons:

  1. I wanted the `struct` and `class` implementations to be as similar as possible, and the former, being sealed, can't have virtual methods.

  2. C# only supports single inheritance, so passing in the filter encourages making the logic more composable and reusable than would be possible by overriding a method in a single inheritance chain.

  3. Injecting the filter allows for changing the filter based on the environment or runtime logic. For example, I might want to change the filter for a Phone Number field if the user changes the Country field. (If a new filter is assigned, it is applied to the existing value automatically.)

NAMING CONVENTIONS
==================
(These are just my initial thoughts to organize my own code, I'm very much open to suggestions here.)

Filter functions should be named in a way that primarily describes *how they react* to the incoming value. They should usually start with a predicate, then some phrase describing the condition they are looking for.

Examples:

 -  `ErrorIfNull<T> where T : class` -- throws an exception if the input is a null reference
 -  `TruncateIfLongerThan<string, int>` -- truncate the string to the specified length if it is longer.
 -  `RemoveNonDigits<string>` -- remove all non-digit characters.
 -  `CollapseWhiteSpace<string value>` -- replace all sequences of whitespace characters with a single space.
 -  `ConstrainToLatitude<int>` -- set a minimum of -90 and a maximum of +90.

If a predicate is not provided, the assumption should be that the value is changed to be valid (rather than some other side-effect, such as throwing an exception). Example:

 -  `EmptyIfNull<string>` -- coalesce null values to `string.Empty`.
 -  `NewIfNull<T> where T : class, new()` -- coalesce null values to `new(T)`.
 -  `AtLeast<int value, int minValue>` -- set the value to minValue if it is below.

Counter-examples:

 -  `NoHtml<string>` -- unclear if HTML tags found would be stripped, converted, or throw an exception.
 -  `Clean<string>` -- unclear what is happening
 -  `ToMD5<string>` -- unclear if this is creating an MD5 hash of the input, validating that the input is one, or something else.
 -  `IsGuid<string>` -- unclear what would happen if it is not (return type is `string`, not `bool`).
 -  `DigitsOnly<string>` -- unclear if non-digits are replaced, removed, or throw an exception. (This is unlike `AtLeast()` above, where the mechanism can be reasonably assumed to set values below the minimum to that minimum. It's a fine hair to split, but I'm trying to balance clarity and conciseness).

Some of these counter-examples are from my own earlier code. :)

CONTRIBUTING
============
This is a very new library. It could use:
 - More unit tests
 - More *generally-useful* filters
 - Bug fixes

Guidelines for pull requests:
 - Please limit code to the .NET Core.
 - Please use similar code formatting (same-line "{"), tabs, etc.).
 - If your filters make any culture/region assumptions, please document them.
 - All contributions should be made under the same license as this software (MIT).

Code of Conduct
 1. Be nice.
 2. Give others the benefit of the doubt.
 3. If someone tells you you're being a jerk, assume they tried really hard to follow (2).
 4. Critique code, not people.

*I don't actully anticipate this being an issue, but I've just seen many larger projects start to include these, so I wanted to see how short and sweet I could make one for my little projects that would encompass my personal philosophy.*

LICENSE (MIT "Expat")
=====================
Copyright 2015 Richard S. Tallent, II

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
