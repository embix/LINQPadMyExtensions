/** LINQPad FileHeader - you may want to just copy the extensions you need, no override your extensions file whith this one
<Query Kind="Program">
  <Namespace>System.IO.Compression</Namespace>
</Query>
*/

#if !LINQPAD
using System.IO.Compression; // in case you use this code out of LINQPad

public static class LinqPadMockExt
{
    public static T Dump<T>(this T item, params object[] objects)
    {
        return item;
    }
}
#endif

void Main()// used for testing extensions and showing some use cases
{
@"Vulnerabilities
make-ca-1.4
CrackLib-2.9.7
cryptsetup-2.0.6".AsMarkdownList().Dump();

	var Expected = "123456";
	var actuals = new[] { "124356", "12", "A", "aasdfasfdadfs", "456", "123456", "1233456", "123456789" };
	actuals.Select(a => new
	{
		Expected = Expected,
		Actual = a,
		Distance = StringExtensions.EditDistance(Expected, a)
	}).OrderBy(d => d.Distance).Dump("StringExtensions.EditDistance a.k.a Levenshtein distance");

	// Write code to test your extensions here. Press F5 to compile and run.
	//@"C:\temp\all.csv".ReadAsFile().Dump();

	var linqpad4extensionsfilepath = @"C:\Users\you\Documents\LINQPad Plugins\Framework 4.6\MyExtensions.FW46.linq";
	Interaction.OpenExplorerWithFileSelected(linqpad4extensionsfilepath);

	new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 13, 14 }
	.Yield()
	.Select(a => new
	{
		In = a,
		Generic = a.Convolute((b, c) => c - b),
	})
	//.Single()
	.Dump();

	new[] { DateTime.Now, DateTime.Now.AddMonths(1) }
	.Convolute((a, b) => b - a).Dump();
	new[] { 1, 5, 6, 8, 10, 20, 33 }
	.Select(i => new { Time = i, Value = i % 7 })
	.OrderBy(i => i.Time)
	.Convolute((a, b) => new { IntervalStart = a.Time, IntervalEnd = b.Time, TimeDiff = a.Time - b.Time, ValueDiff = (double)a.Value - b.Value })
	.Select(i => new { Rise = i.ValueDiff / i.TimeDiff })
	.Dump();

	Enumerable.Range(2, 30).Select(i => new { Value = i, IsPrime = new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 }.Contains(i) }).Select(i => new { i.Value, LastPrime = i.Value, i.IsPrime })
	.SubstitueMarkedFromLatestUnmarkedSampleImpl(i => !i.IsPrime, (lastPrime, currentValue) => new { currentValue.Value, lastPrime.LastPrime, currentValue.IsPrime })
	.Select(i => new { i.Value, i.LastPrime })
	.Dump();

	new { A = "A" }.Map(a => new { A = a.A, B = a.A + "B" }).Dump();

	"Gü€{µÖß".Dump(".net").ToBase64().Dump("as utf8 in base64").FromBase64().Dump("deserialized");
}

public static class IEnumerableExt
{
	/// <summary>
	/// Wraps this object instance into an IEnumerable&lt;T&gt;
	/// consisting of a single item.
	/// </summary>
	/// <typeparam name="T"> Type of the wrapped object.</typeparam>
	/// <param name="item"> The object to wrap.</param>
	/// <returns>
	/// An IEnumerable&lt;T&gt; consisting of a single item.
	/// </returns>
	public static IEnumerable<T> Yield<T>(this T item)
	{
		yield return item;
	}

	/// <summary>
	/// map one type to another
	/// </summary>
	/// <typeparam name="TIn">Type of the source object</typeparam>
	/// <param name="item">The source object to be mapped</param>
	/// <typeparam name="TIn">Type of result object</typeparam>
	/// <param name="item">mapping function (a.k.a. projection)</param>
	/// <returns>
	/// instance of the mapped object
	/// </returns>
	/// <remarks>
	/// this is almost literally a Yield().Select(...).Single() projections/mappings...
	/// which I am tired of writing explicitly
	/// </remarks>
	public static TOut Map<TIn, TOut>(this TIn item, /*Expression<*/Func<TIn, TOut>/*>*/ mapping)
	{
		return
			item
				.Yield()
				.Select(mapping)
				.Single();
	}

	// Zur Umgehung der 2100 Parametergrenze (linq/sql) Listen in Chunks teilen
	public static List<List<T>> SplitIntoChunksOfSize<T>(this IEnumerable<T> container, Int32 sizeOfChunks)
	{
		return container
			.Select((x, i) => new { Index = i, Value = x })
			.ToList()
			.GroupBy(x => x.Index / sizeOfChunks)
			.Select(x => x.Select(v => v.Value).ToList())
			.ToList();
	}

	/// <summary>
	/// null tolerant access to a Collection
	/// 
	/// usage:
	/// foreach (int i in returnArray.AsNotNull())
	/// {
	///     // do some more stuff
	/// }
	/// </summary>
	/// <typeparam name="T">Type of collection</typeparam>
	/// <param name="original"></param>
	/// <returns></returns>
	public static IEnumerable<T> AsNotNull<T>(this IEnumerable<T> original)
	{
		return original ?? new T[0];
	}

	public static IEnumerable<TOut> Convolute<TIn, TOut>(this IEnumerable<TIn> sequence, Func<TIn, TIn, TOut> function)
	{
		if (sequence == null) throw new ArgumentNullException("sequence");
		if (function == null) throw new ArgumentNullException("function");
		if (sequence.Take(2).Count() < 2) throw new ArgumentOutOfRangeException("sequence", "Must have at least 2 value to apply diff convolution.");

		// convolute in separate method due to the argument-check/yield issue (see: where did i find that skeet article?!)
		return ConvoluteImpl(sequence, function);
	}

	private static IEnumerable<TOut> ConvoluteImpl<TIn, TOut>(IEnumerable<TIn> sequence, Func<TIn, TIn, TOut> function)
	{
		TIn lastValue = default(TIn);// value not used but required for compilation
		Boolean isInitialized = false;

		foreach (var element in sequence)
		{
			if (isInitialized)
			{
				var convoluted = function(lastValue, element);
				lastValue = element;
				yield return convoluted;
			}
			else
			{
				lastValue = element;
				isInitialized = true;
			}
		}
	}

	public static IEnumerable<T> SafeSubstitueMarkedFromLatestUnmarkedSampleImpl<T>(this IEnumerable<T> sequence, Func<T, Boolean> isMarkedSample, Func<T, T, T> substituionUnmarkedMarkedOut)
	{
		var skippedFirstUnmarked = sequence.SkipWhile(isMarkedSample);
		return skippedFirstUnmarked.SubstitueMarkedFromLatestUnmarkedSampleImpl(isMarkedSample, substituionUnmarkedMarkedOut);
	}

	// kind of predicative mapping
	// or "unzip" by predicate and "zip" with applying a "map" function at once
	// todo: give enough examples from simple to complex
	// todo: find better name, todo generate pre lazy check method and make this method private to avoid unchecked access
	public static IEnumerable<T> SubstitueMarkedFromLatestUnmarkedSampleImpl<T>(this IEnumerable<T> sequence, Func<T, Boolean> isMarkedSample, Func<T, T, T> substituionUnmarkedMarkedOut)
	{
		T lastUnmarked = default(T);
		Boolean isInitialized = false;

		foreach (var element in sequence)
		{
			if (!isMarkedSample(element))
			{
				lastUnmarked = element;
				isInitialized = true;
				yield return element;
			}
			else
			{
				if (!isInitialized) throw new ArgumentException("Cannot apply substitution if there is no unmarked sample yet");

				var substitute = substituionUnmarkedMarkedOut(lastUnmarked, element);
				yield return substitute;
			}
		}
	}
}

public static class IQueryableExt
{
	//summarize im Sinne der Zusammenfassung
	public static IQueryable<TResult> SummarizeSelect<TSource, TResult>(
		this IQueryable<TSource> source,
		Expression<Func<IGrouping<Boolean, TSource>, TResult>> selector
	)
	{
		return source.GroupBy(s => true).Select(selector);
	}

	public static TResult SummarizeOrDefault<TSource, TResult>(
		this IQueryable<TSource> source,
		Expression<Func<IGrouping<Boolean, TSource>, TResult>> selector
	)
	{
		return SummarizeSelect(source, selector).SingleOrDefault();
	}

	public static TResult SummarizeOrThrow<TSource, TResult>(
		this IQueryable<TSource> source,
		Expression<Func<IGrouping<Boolean, TSource>, TResult>> selector
	)
	{
		return SummarizeSelect(source, selector).Single();
	}
}

public static class StringExtensions
{
	public static String TrimTo(this String baseString, Int32 length)
	{
		if (baseString == null || baseString.Length < length) return baseString;

		return baseString.Substring(0, length);
	}

	public static string Right(this string sValue, int iMaxLength)
	{
		//Check if the value is valid
		if (string.IsNullOrEmpty(sValue))
		{
			//Set valid empty string as string could be null
			sValue = string.Empty;
		}
		else if (sValue.Length > iMaxLength)
		{
			//Make the string no longer than the max length
			sValue = sValue.Substring(sValue.Length - iMaxLength, iMaxLength);
		}

		//Return the string
		return sValue;
	}

	public static string Left(this string value, int maxLength)
	{
		if (string.IsNullOrEmpty(value)) return value;
		maxLength = Math.Abs(maxLength);

		return (value.Length <= maxLength
			   ? value
			   : value.Substring(0, maxLength)
			   );
	}

	// Write custom extension methods here. They will be available to all queries.
	public static IEnumerable<String> ReadAsFile(this String fullFileName)
	{
		String line;
		using (var reader = File.OpenText(fullFileName))
		{
			while ((line = reader.ReadLine()) != null)
			{
				yield return line;
			}
		}
	}

	public static String[] ReadAsFile(this String fullFileName, Encoding encoding)
	{
		var file = new FileInfo(fullFileName);
		return File.ReadAllLines(fullFileName, encoding);
	}

	public static void WriteToFile(this String content, String fullFileName)
	{
		File.WriteAllText(fullFileName, content);
	}

	// Define other methods and classes here
	public static String ToIdentifierPascalCase(this String name)
	{
		// guard
		return name
		.ToCharArray()
		.Select(c => c.ToString())
		.Select(c =>
		{
			if (Regex.IsMatch(c, @"^[A-Za-z0-9]$")) return c; return "_";
		})
		.Aggregate("_", // StartValue of _ to have the first letter capitalized as well
			(a, b) =>
			{
				if (a.EndsWith("_"))
				{
					return a.Substring(0, a.Length - 1) + b.ToUpper();
				}
				return a + b;
			}
		);
	}

	// edit distance between two strings, aka levenstein distance
	public static Int32 EditDistance(this String s, String t)
	{
		int n = s.Length;
		int m = t.Length;
		int[,] d = new int[n + 1, m + 1];

		// Step 1
		if (n == 0)
		{
			return m;
		}

		if (m == 0)
		{
			return n;
		}

		// Step 2
		for (int i = 0; i <= n; d[i, 0] = i++)
		{
		}

		for (int j = 0; j <= m; d[0, j] = j++)
		{
		}

		// Step 3
		for (int i = 1; i <= n; i++)
		{
			//Step 4
			for (int j = 1; j <= m; j++)
			{
				// Step 5
				int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

				// Step 6
				d[i, j] = Math.Min(
					Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
					d[i - 1, j - 1] + cost);
			}
		}
		// Step 7
		return d[n, m];
	}

	public static String ToBase64(this String content)
	{
		var utf8Bytes = Encoding.UTF8.GetBytes(content);
		return Convert.ToBase64String(utf8Bytes);
	}

	public static String FromBase64(this String content)
	{
		var utf8Bytes = Convert.FromBase64String(content);
		return Encoding.UTF8.GetString(utf8Bytes);
	}

	public static byte[] Compress(this string text)
	{
		byte[] buffer = Encoding.UTF8.GetBytes(text);
		var memoryStream = new MemoryStream();
		using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
		{
			gZipStream.Write(buffer, 0, buffer.Length);
		}

		memoryStream.Position = 0;

		var compressedData = new byte[memoryStream.Length];
		memoryStream.Read(compressedData, 0, compressedData.Length);

		var gZipBuffer = new byte[compressedData.Length + 4];
		Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
		Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
		return gZipBuffer;
	}

	public static string DecompressString(this byte[] gZipBuffer)
	{
		using (var memoryStream = new MemoryStream())
		{
			int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
			memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

			var buffer = new byte[dataLength];

			memoryStream.Position = 0;
			using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
			{
				gZipStream.Read(buffer, 0, buffer.Length);
			}

			return Encoding.UTF8.GetString(buffer);
		}
	}
	
	public static string AsMarkdownList(this string lines, string[] separators = null)
	{
		var separator = separators ?? new []{"\n"};
		var lineArray = lines.Split(separator, StringSplitOptions.RemoveEmptyEntries);
		return AsMarkdownList(lineArray);
	}
	
	public static string AsMarkdownList(this IEnumerable<string> lines, Int32? startNumerate = 1)
	{
		var listLines = startNumerate.HasValue
			? lines.Select((s, i) => $" - [ ] {i+startNumerate.Value}. {s.Trim()}")
			: lines.Select(s => $"- [ ] {s.Trim()}");
		return String.Join("\n", listLines);
	}
	
}

public static class MyExtensions
{
	// 1-Based
	public static string ToStandardExcelColumnName(this int columnNumberOneBased)
	{
		if (columnNumberOneBased < 1) throw new ArgumentOutOfRangeException("columnNumberOneBased", "one base index means there has to be no index lesser than 1");
		int baseValue = Convert.ToInt32('A');
		int columnNumberZeroBased = columnNumberOneBased - 1;

		string ret = "";

		if (columnNumberOneBased > 26)
		{
			ret = ToStandardExcelColumnName(columnNumberZeroBased / 26);
		}

		return ret + Convert.ToChar(baseValue + (columnNumberZeroBased % 26));
	}
}

public class Interaction
{
	public static void LateBoundMessageBoxShow(String message, String title)
	{
		int num = 1572864;
		Type type1 = Type.GetType("System.Windows.Forms.MessageBox, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
		Type type2 = Type.GetType("System.Windows.Forms.MessageBoxButtons, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
		Type type3 = Type.GetType("System.Windows.Forms.MessageBoxIcon, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
		Type type4 = Type.GetType("System.Windows.Forms.MessageBoxDefaultButton, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
		Type type5 = Type.GetType("System.Windows.Forms.MessageBoxOptions, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
		type1.InvokeMember("Show", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, (Binder)null, (object)null, new object[6]
		{
			(object) message,
			(object) title,
			Enum.ToObject(type2, 0),
			Enum.ToObject(type3, 0),
			Enum.ToObject(type4, 0),
			Enum.ToObject(type5, num)
		  }, System.Globalization.CultureInfo.InvariantCulture);
	}

	//Microsoft.VisualBasic.Interaction.InputBox("Question?","Title","Default Text")
	public static String LateBoundInputBoxShow(String question, String title, String defaultText)
	{
		Type type1 = Type.GetType("Microsoft.VisualBasic.Interaction, Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
		if (type1 == null) throw new Exception("NOOO!");
		return (String)type1.InvokeMember("InputBox", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, (Binder)null, (object)null, new object[5]{
			question, title, defaultText,
			-1,-1
		});
	}

	public static void OpenExplorerWithFileSelected(String fullName)
	{
		var argument = @"/select, " + fullName;
		// Environment.SystemDirectory doesn't fit for windows 7, 8, 10 as explorer.exe now is in C:\Windows instead of C:\Windows\System32 as it was in Windows XP



		var explorerDirectory = IsWinVistaOrHigher() ? Environment.GetFolderPath(Environment.SpecialFolder.Windows) : Environment.SystemDirectory;
		var explorerPath = Path.Combine(explorerDirectory, "explorer.exe");
		Process.Start(explorerPath, argument);
	}

	static bool IsWinXPOrHigher()
	{
		OperatingSystem OS = Environment.OSVersion;
		return (OS.Platform == PlatformID.Win32NT) && ((OS.Version.Major > 5) || ((OS.Version.Major == 5) && (OS.Version.Minor >= 1)));
	}

	static bool IsWinVistaOrHigher()
	{
		OperatingSystem OS = Environment.OSVersion;
		return (OS.Platform == PlatformID.Win32NT) && (OS.Version.Major >= 6);
	}
}

public static class NotNullExtension
{
	public static TResult IfNotNull<TSource, TResult>(this TSource source, Func<TSource, TResult> accessor, TResult @default = default(TResult))
		where TSource : class
	{
		return source != null
			? accessor(source)
			: @default;
	}
}
