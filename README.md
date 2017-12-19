Most C# Intrinsic Types are Weak
================================
While we think of languages like C# as "strongly typed," we fail to recognize that for common types like `int` and `string`, the range of allowed values is far greater than the allowed range of the actual property. We use these intrinsic types because they are convenient, but often, this leaves our properties unproteted from a wide range of illegal values, which could result in undetected errors or malicious abuse. For example:

```C#
public class Customer {
	public int Age { get; set; }
}
```

Using `int` for such a property is common, but it's a *terrible* type for storing human age in years--it allows negative values, and the upper bound of 2,147,483,647 is many orders of magnitude larger than a reasonable maximum of, say, 130. We could choose a surrogate that *more closely* matches the intended range of values (say, `byte`), but the match is still imperfect. So we refactor, creating a backing field to store the age and a setter to restrict incoming values:

```C#
public class Customer {
	private int _age = -1;
	public int Age {
		get => _age;
		set => _age = Math.Min(Math.Max(0, value), 130);
	}
}
```

However, this is not a robust solution. A few caveats:
1. Any code within `Customer` can freely access and mutate `_age` without the setter.
2. Until the setter is called, `_age` has an illegal value. (This is contrived, but there are many properties whose `default<T>` would be a bad value.)
3. If we have a number of classes that also have a human age field, we don't have a slick way of reusing that validation logic. Again, maybe a bit contrived for age, but not so unusual for fields like email addresses, postal codes, and names, which have complex validation logic that should be consistent across an application.
4. What happens if we want to use dependency injection to allow validation to be situational? For example, choosing a validator for a phone number field based on the country selected.
5. Explicitly-defined backing fields are cumbersome. That's why auto-implemented properties were invented.

`String` is perhaps the most abused general-purpose type. Strings can have null values, empty values, lengths up to 2GB, and can contain any Unicode end point at any position, but it is *exceedingly rare* that you would want any string property to allow such flexibility.

There are many ways to work around these issues. This library, `RT.ValueFilter`, is my own solution for .NET Standard 2.0. I hope it will be useful to others.

How Does it Work?
=================
`Filtered<T>` is a generic type (both `struct` and `class` implementations are provided, in separate namespaces) that *wraps* a type (such as `int` or `string`) with your chosen validation logic ("filter"), enforcing stronger rules around the allowed values for those variables. By using these as your private backing fields, you can:
- greatly simplify your public property logic,
- reuse and compose validation rules among fields in many classes,
- provide suitable initial and default values,
- enforce the same logic for private, protected, and public access to your fields.

When you create an `Filtered<T>` instance, you specify (1) the interior type used to store the value and (2) a `Func<T, T>` delegate that will perform the filtering.

A small library of filter functions is also provided. Each is implemented as an extension method for fluent chaining, which simplifies composing your filter logic from a series of simpler rules.

Show me some code!
==================
Here's the example above, implemented using RT.ValueFilter:

```C#
public class Customer {
	public int Age { get; set; } = new Filtered<int>(value => Math.Min(Math.Max(0, value), 130));
}
```

Here's what we end up with:
- A validated property is declared cleanly, with one line of code.
- No explicit backing field.
- Implicit conversion allows the public interface to be `int` while ensuring the field itself never stores an out-of-range value.
- If using the `struct` version of `Filtered<int>`, the memory overhead is tiny over using a bare `int`.
- The logic is only called during `Filtered<int>`'s setter and during construction, so `get` calls require no validation.
- The filter can be extracted to a static class and reused across many properties in many classes.
- Since `Func<T, T>` is fluid, filters are easy to compose, especially using extension methods.

Let's look at a more complex example, using an extension method to extract and reuse some common string validation logic for names:
```C#
public static class MyValidators {
	public static string NameValidator(this string s) => 
		s.EmptyIfNull()
		.KeepNameCharsOnly()
		.Trim()
		.CollapseWhiteSpace()
		.TruncateIfLongerThan(255);
}

public class Customer {
	public string FirstName { get; set; } = new Filtered<string>(value => value.NameValidator());
	public string LastName { get; set; } = new Filtered<string>(value => value.NameValidator());
}

public class Company {
	public string Name { get; set; } = new Filtered<string>(value => value.NameValidator());
}
```

With very little effort, I can create a little library of useful validation functions and reuse them across an entire application, or even across many applications. DRY indeed!

Even the default value is protected--properties using the `NameValidator` filer will always have a `string.Empty` value to start with, not `null`. This is because the `Filtered<T>` constructor sets the initial value by running `default<T>` through the provided filter. You can override the default value by providing a second argument to the constructor, and the initial value you provide will also go through the filter.

So, ensuring that nullable types (like `string`) are *never null* is incredibly easy, allowing you to avoid null-checking of those properties everywhere they are used.

This isn't limited to core .NET types like `int` and `string`... you can use it to validate *any* value or reference type. For example, if you have an email property that you want to ensure *only* allows email addresses within your own company, you could create a reusable filter like this:

```C#
using System.Net.Mail;
public static class MyFilters {
	public static MailAddress NullIfNotCompanyEmail(this MailAddress address) {
		if(address==null) return null;
		return String.Equals(address.Host, "acme.com", StringComparison.OrdinalIgnoreCase) ? address : null;
	}
}
```

You may be wondering why I included the class version. It's because with a class, you can easily create derived types of `Filtered<T>` with your logic already baked in. Here's the example from above for customer name, using a derived type:

```C#
public class NameString : Filtered<string> {
	public NameString() : base(
		value => value.EmptyIfNull()
		.KeepNameCharsOnly()
		.Trim()
		.CollapseWhitespace()
		.TruncateIfLongerThan(255)
	) {}
}

public class Customer {
	public string FirstName { get; set; } = new NameString();
}
```
You do incur extra memory overhead for wrapping the string object in another object, but the semantics are great!

Usage
================
Personally, I prefer using the `struct` implementation, using the base type for the public interface (as shown in the examples above).

This isn't a replacement for client-side validation and error messages. It's simply a way to ensure that values stored will *never* be invalid, even if normal validation is bypassed or broken for some reason.

As for defining the filters, I tend to use one-off anonymous functions. I collect the filters I might reuse in a static utility class.

Why Not Just Use a Class with a Virtual Filter Method?
======================================================
I chose to use `Func<T, T>` for Filter instead of a virtual method for a few reasons:

  1. I wanted the `struct` and `class` implementations to be as similar as possible, and `structs` can't have virtual methods.

  2. C# only supports single inheritance, so injecting the filter in the constructor encourages making the logic more composable and reusable than would be possible by overriding a method in a single inheritance chain.

  3. Injecting the filter allows for changing the filter based on the environment or runtime logic. If a new filter is assigned, it is applied to the existing value automatically.

Naming Conventions
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
 -  2017-04-04  1.2.4   Downgraded to csproj, targeting .NET Standard 1.1.
 -  2017-12-18  2.0.0   Upgraded target to .NET Standard 2.0, updated README
 
Status
======
I've been using logic like this ad-hoc in my work, but this library is an attempt to centralize both the filtering concepts and a library of useful value filters.

Contributing
============
This library is very new. Additional unit tests and generally-useful filters welcome. For pull requests:
 - Please limit to .NET Standard 2.0 compatibility.
 - Please use similar code formatting (tabs, braces, etc.).
 - If your filters make any culture/region assumptions, please document them.
 - All contributions should be made under the same license as this software (MIT).

Code of Conduct
---------------
 1. Be nice.
 2. Give others the benefit of the doubt.
 3. If someone tells you you're being a jerk, assume they tried really hard to follow (2).
 4. Critique code, not people.

*I don't actully anticipate this being an issue, but I've just seen many larger projects start to include these, so I wanted to see how short and sweet I could make one for my little projects that would encompass my personal philosophy.*

License (MIT "Expat")
=====================
Copyright 2015-2017 Richard S. Tallent, II

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.