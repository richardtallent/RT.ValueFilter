THE STRONGLY-TYPED MYTH
=======================
While C# is *capable* of being strongly typed, we often "cheat" by using *relatively* weak types like `int` and `string` to back properties that are, logically, *not* general integer or string values.

For example, we might use `int` to store a `Person.Age` backing field, because `int` will store any human age and is a convenient type to work with. While our actual acceptable range for `Age` might be, say, 0 to 120, `int` values can be negative or can exceed 2 billion. Allowing the backing field to store *any* integer value could result in undetected bad data or malicious abuse. We could choose a surrogate that *more closely* matches the intended range of values (say, `byte`), but the match is still imperfect.

`String` is perhaps the most abused general-purpose type. Strings can have null values, empty values, lengths up to 2GB, and can contain any Unicode end point at any position in its value, but it is *exceedingly rare* that you woudl want such flexibility in a real-world property.

To alleviate this issue, we of course add some validation logic to our public getters or setters, but this presents its own issues, such as:

1. What if our code also operates on the private backing field and sets a bad value or retrieves an invalid value (such as null)?
2. How can we effectively reuse and compose business logic among fields in many classes?
3. How can we efficiently inject some of the validation logic for situations where the logic should differ depending on the situation?

There are a dozen ways to deal with these issues, `RT.ValueFilter` is my own solution. I hope it will be useful to others.

HOW DOES IT WORK?
=================
`Filtered<T>` is a generic type (both `struct` and `class` implementations are provided, in separate namespaces) that *wraps* a type (such as `int` or `string`) with your chosen validation logic ("filter"), enforcing stronger rules around the allowed values for those variables. By using these as your private backing fields, you can:
- greatly simplify your public property logic,
- reuse and compose validation rules among fields in many classes,
- provide suitable initial and default values
- enforce the same logic for private, protected, and public access to your fields.

When you create an `Filtered<T>` instance, you specify (1) the interior type used to store the value and (2) a `Func<T, T>` delegate that will perform the filtering.

A small library of filter functions is also provided. Each is implemented as an extension method for fluent chaining, which simplifies composing your filter logic from a series of simpler rules.

SHOW ME SOME CODE!
==================
Let's say my `Customer` class has a `string` property called `Name`, and I want to limit the acceptable values. I could do something like this:

	using RT.ValueFilter.Struct;

	public class Customer {

		private readonly Filtered<string> name = new Filtered<string>(value =>
			value.EmptyIfNull()
				.KeepNameCharsOnly()
				.Trim()
				.CollapseWhiteSpace()
				.TruncateIfLongerThan(255)
			);

		public string Name {
			get { return name; } // implicit conversion syntactic sugar 
			set { name.Value = value; }
		}

	}

Now, even the *private* value behind `Name` is protected from all attempts to set it to a value that is not consistent with my logical definition of a "name."

This includes *the initial value*: I didn't use the constructor that includes an initial value parameter, so the constructor passes `default(T)` through the filter I provided. The filter then coalesces the default null value to empty (via `EmptyIfNull()`), so I don't have to provide a "magic" default value.

If I find myself using the same logic for other name-like fields, I can create a static function, then use it for any number of `Filtered<string>` instances:

	using RT.ValueFilter;

	public class Customer {
	    private Filtered<string> firstName = new Filtered<string>(MyFilters.NameFilter);
	    private Filtered<string> lastName = new Filtered<string>(MyFilters.NameFilter);
	    private Filtered<string> companyName = new Filtered<string>(MyFilters.NameFilter);

		public string FirstName {
			get { return firstName; }
			set { firstName.Value = value; }
		}

		public string LastName {
			get { return lastName; }
			set { lastName.Value = value; }
		}

		public string CompanyName {
			get { return companyName; }
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

This isn't limited to core .NET types like `int` and `string`... you can use it to validate *any* value or reference type. For example, if you have an email property that you want to ensure *only* gets set to your company's email domain, you could use something like this:

	using System.Net.Mail;

    public static class MyFilters {

		public static MailAddress NullIfNotCompanyEmail(this MailAddress address) {
			if(address==null) return null;
			return String.Equals(address.Host, "acme.com", StringComparison.OrdinalIgnoreCase)
				? address : null;
		}

	}

USAGE
================
My use of this library is mostly limited to private fields, usually ones that are backing public properties. I prefer using `readonly FilteredStruct<T>` for this, as they don't have the additional (albeit tiny) overhead of an object.

I wouldn't suggest making the `Filtered<T>` types public. Using the underlying type is a better choice.

It's important to note that this doesn't replace the need for client-side validation and error messages, it just ensures that the values stored will *never* be invalid, even when normal validation is bypassed or broken for some reason.

As for defining the filters, I tend to use one-off anonymous functions. I collect the filters I might reuse in a static utility class.

WHY NOT JUST USE A CLASS WITH A VIRTUAL FILTER METHOD?
======================================================
I chose to use `Func<T, T>` for Filter instead of a virtual method for a few reasons:

  1. I wanted the `struct` and `class` implementations to be as similar as possible, and `structs` can't have virtual methods.

  2. C# only supports single inheritance, so injecting the filter in the constructor encourages making the logic more composable and reusable than would be possible by overriding a method in a single inheritance chain.

  3. Injecting the filter allows for changing the filter based on the environment or runtime logic. For example, I might want to change the filter for a Phone Number field if the user changes the Country field. If a new filter is assigned, it is applied to the existing value automatically.

NAMING CONVENTIONS
==================
These are just my initial thoughts to organize my own code, I'm open to suggestions here.

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

 -  `NoHtml<string>` -- unclear if HTML tags found would be stripped, converted, or result in an exception.
 -  `Clean<string>` -- unclear what is happening
 -  `ToMD5<string>` -- unclear if this is creating an MD5 hash of the input, validating that the input is one, or something else.
 -  `IsGuid<string>` -- unclear what would happen if it is not (return type is `string`, not `bool`).
 -  `DigitsOnly<string>` -- unclear if non-digits are replaced, removed, or throw an exception. (This is unlike `AtLeast()` above, where the mechanism can be reasonably assumed to set values below the minimum to that minimum. It's a fine hair to split, but I'm trying to balance clarity and conciseness).

Some of these counter-examples are from my own earlier code. :)

VERSION HISTORY
===============
 -  2016-01-17	1.1.0	Initial Nuget release
 -  2016-08-09	1.2.0	Upgrade to .NET Core 1.0, cleanup, add (mostly empty) test project, reorg so both class and struct are named Filtered<T>
 -  2016-11-22  1.2.2   Upgrade to .NET Core 1.1 with multi-target for 4.5.1. Remove Regex compiled option (not supported in .NET Core?)
 -  2016-11-25  1.2.3   Update README, add more filters and tests, add implicit conversion for syntactic sugar.

STATUS
======
I've been using logic like this ad-hoc in my work, but this library is an attempt to centralize both the filtering concepts and a library of useful value filters.

CONTRIBUTING
============
This library is very new. Additional unit tests and generally-useful filters welcome. For pull requests:
 - Please limit code to .NET Core 1.1 and .NET 4.5.1 compatibility.
 - Please use similar code formatting (tabs, braces, etc.).
 - If your filters make any culture/region assumptions, please document them.
 - All contributions should be made under the same license as this software (MIT).
 - I've been considering creating an immutable struct version. Opinions welcome.

Code of Conduct
---------------
 1. Be nice.
 2. Give others the benefit of the doubt.
 3. If someone tells you you're being a jerk, assume they tried really hard to follow (2).
 4. Critique code, not people.

*I don't actully anticipate this being an issue, but I've just seen many larger projects start to include these, so I wanted to see how short and sweet I could make one for my little projects that would encompass my personal philosophy.*

LICENSE (MIT "Expat")
=====================
Copyright 2015-2016 Richard S. Tallent, II

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.