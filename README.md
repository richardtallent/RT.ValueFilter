WHAT IS RT.VALUEFILTER?
=======================

This class library is made up of two pieces:

 1. A selection of useful static functions to "filter" values, exposed as extension methods to allow them to be called fluently, mixing and matching them to narrow the allowed values for some variable; and

 2. A mechanism to ensure that a given variable can *never* hold an invalid value.

**NOTE: This code is, to say the least, very new. I've done similar work before, but this is basically a rewrite to clean it up and get it into one cohesive library, and I'm just starting the process. I posted to Github early in the process just to get the ball rolling and so I can access this repository easily from every computer.**

WHY?
====

I grew tired of writing the same validation logic over and over, and decided to centralize key reusable logic in a single place. I also wanted to use a fluent (chained method) approach, which maximizes readability. For example, to validate a user's Full Name field, I might call:

	name = value.EmptyIfNull()
		.NameCharsOnly()
		.Trim()
		.CollapseWhitespace()
		.MaxLength(255);

Each of these calls returns a modified version of the incoming value when it detects a value that is invalid (or, for some filters, could throw an exception instead).

By placing these calls in my property setters, I can ensure that changes to the properties always result in an acceptable value.

But this lead to two more issues:

  1. How do we ensure that the *initial* value of the private backing field of a property is valid? After all, the very values we may be trying to avoid (null, 0, etc.) may be the default value for those types. This would require purposefully setting valid literal values to the backing fields (an opportunity for whole classes of human error), or putting the filter calls in the *getter* so bad defaults are changed before accessed (a potential performance issue, not to mention we should catch bad values when they are set, not retrieved).

  2. Similarly, how to we ensure that any changes made *directly* to the private backing field are also valid? Some programmers always treat their backing properties as hands-off, as if they weren't accessible to anything but the exposed property getter/setter (a good practice IMHO), but it's easy to make the mistake of doing so. Since we can't directly subclass the types we're most likely to need to filter (`string`, `int`, etc.), we have to encapsulate them to manage how they are set no matter which code sets them.

My solution for this is a generic wrapper `struct` / `class` (I've implemented it both ways, use whichever you prefer) that manages access to the underlying value using a specified Filter delegate (which happens, not coincidentally, to share the same interface as my library of filters). By defining the filter at the backing field, *all* values set to the field (even the default value if an initial value is not specified).

USAGE
=====

In most situations, you'll want to simply use the generic class or struct and specify the filter in the constructor. E.g.:

    private Filtered<string> name = new Filtered<string>(MyFilters.NameFilter);

    public string Name {
		get { return name; }			// Gets the current value (w/implicit conversion sugar)
		set { name.Value = value; }		// Calls the filter while setting the value
    }

You can also use anonymous functions, perfect for chaining a few general filters or implementing some custom logic that doesn't need to be reused elsewhere:

    private Filtered<string> name = new Filtered<string> { Filter = value =>
		value.EmptyIfNull()
			.NameCharsOnly()
			.Trim()
			.CollapseWhitespace()
			.MaxLength(255)
		};

If you have a set of filters that you are using over and over, you can create inherit from the generic class and call the base constructor to set up your filter chain:

    public class FilteredNameString : Filtered<string> {
		public FilteredNameString() : base(
			value => value.EmptyIfNull()
			.NameCharsOnly()
			.Trim()
			.CollapseWhitespace()
			.MaxLength(255)
		) {
			// Other constructor code here
		}
    }

Of course, you could also just define another static `Func<T, T>` that implements this chain an then use the normal mechanism -- it's up to you.

**Struct or Class?**

Use whichever you prefer. My own preference is use the `struct` for private backing fields of class properties, and then making a habit of never accessing the private member directly. For Local variables, I'm more inclined to use the class. Here are the situations where I might want to go further and create a subclass rather than passing the Filter in the constructor:

 -  Complex, reusable but uncommon logic (avoids polluting the extension method list)

 -  Situations where I might want to access part of the value (e.g., Uri, which is a string but has individual members for accessing the host, protocol, etc.)

 -  Situations where the logic needs to be broken up into multiple functions due to complexity. For example, if I wanted to convert MarkDown to HTML.

 -  Places where I might want to have the type system tell me at compile-time if I try to do something stupid, like assigning a Name variable to a Phone value.

WHY NOT JUST USE A CLASS WITH A VIRTUAL FILTER METHOD?
======================================================

I chose to use a `Func<T, T>` property for Filter instead of a virtual method for a few reasons:

  1. I wanted the `struct` and `class` implementations to be as similar as possible, and the former, being sealed, can't have virtual methods.

  2. C# only supports single inheritance, so passing in the filter encourages making the logic more composable and reusable than would be possible by overriding a method in a single inheritance chain.

  3. I might want to swap out the validation function for a type at runtime. This could be done for testing, logging, or business logic that uses variations of validation logic based on other criteria (for example, dynamically swapping the validation for a phone number based on a separately-chosen country field).

SHOULD MY PUBLIC PROPERTIES USE THE FILTERED<> TYPES?
=====================================================

You could expose your properties as their Filtered<T> type, but I wouldn't suggest it. Exposing a mutable `struct` is asking for trouble, as is exposing an object that someone could set to null or swap out your logic from outside the object.

Instead, I recommend making your properties use the underlying type (string, int, etc.). Your getter code will be the same either way (due to implicit conversion from Filtered<T> to T, and the setter code is also trivial. Exposing the native types is also friendlier to calling or reflecting code, especially OR/M or JSON libraries, and allows you to change the filter implementations out without changing your public interface.

CONTRIBUTING
============

This is a very new library. It could use:
 - A unit test project
 - More *generally-useful* filters
 - Bug fixes

Guidelines for pull requests:
 - Please minimize additional assembly references.
 - Please limit code to the .NET Core so it can be as useful to as many people as possible.
 - Please use similar code formatting (same-line "{"), tabs-not-spaces, etc.).
 - If your validation code makes any culture/region assumptions, please document them.
 - All contributions should be made under the same license as this software (MIT).

Code of Conduct
 1. Don't be a jerk.
 2. Assume others aren't trying to be a jerk.
 3. If someone tells you you're being a jerk, assume they tried really hard to follow (2).
 4. Critique code, not people.

LICENSE
=======

Copyright 2015 Richard S. Tallent, II

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
