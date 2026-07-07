namespace SimplicityTools.Tests.Shared;

/// <summary>
/// Shared battery of modern-C# snippets with the expected cyclomatic complexity of every measured
/// unit in document order. This file is linked (via <c>&lt;Compile Include&gt;</c>) into BOTH
/// SimplicityTools.Metrics.Tests and SimplicityTools.Analyzers.Tests so the two complexity
/// implementations — <c>SimplicityTools.Metrics.CyclomaticComplexityAnalyzer</c> and
/// <c>SimplicityTools.Analyzers.CyclomaticComplexityCalculator</c> — are proven to produce
/// identical numbers for identical code. If a counting rule changes, both implementations and
/// this file must change together (see "Complexity counting rules" in
/// docs/using-the-simplicity-tools.md).
/// </summary>
public static class ComplexityCountingTestCases
{
    public static IEnumerable<object[]> Cases()
    {
        // Empty method: base complexity 1.
        yield return
        [
            """
            public class C
            {
                public void M() { }
            }
            """,
            new[] { 1 }
        ];

        // Nested ifs: +1 each; else is free.
        yield return
        [
            """
            public class C
            {
                public int M(bool left, bool right)
                {
                    if (left)
                    {
                        if (right)
                        {
                            return 2;
                        }

                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            """,
            new[] { 3 }
        ];

        // && , || and ?? each +1.
        yield return
        [
            """
            public class C
            {
                public bool M(bool a, bool b, string? s) => a && b || (s ?? "x").Length > 0;
            }
            """,
            new[] { 4 }
        ];

        // ??= counts like ??.
        yield return
        [
            """
            public class C
            {
                private string? _name;

                public string M(string? value)
                {
                    _name ??= value ?? "unknown";
                    return _name;
                }
            }
            """,
            new[] { 3 }
        ];

        // Each ?. is +1 (documented, opinionated), plus the trailing ??.
        yield return
        [
            """
            public class C
            {
                public int M(string? value) => value?.Trim()?.Length ?? 0;
            }
            """,
            new[] { 4 }
        ];

        // Switch statement: pattern and constant case labels +1 each; default: is free.
        yield return
        [
            """
            public class C
            {
                public int M(object value)
                {
                    switch (value)
                    {
                        case int number when number > 0:
                            return 1;
                        case string text:
                            return text.Length;
                        case null:
                            return -1;
                        default:
                            return 0;
                    }
                }
            }
            """,
            new[] { 4 }
        ];

        // Pattern case label with an "and" combinator: label +1, combinator +1.
        yield return
        [
            """
            public class C
            {
                public int M(object value)
                {
                    switch (value)
                    {
                        case int number and > 5:
                            return number;
                        default:
                            return 0;
                    }
                }
            }
            """,
            new[] { 3 }
        ];

        // Switch expression: arms +1 each, "or" +1; the bare discard arm is free, but a discard
        // arm with a when clause still counts.
        yield return
        [
            """
            public class C
            {
                public string M(int value) => value switch
                {
                    < 0 => "negative",
                    0 or 1 => "small",
                    _ when value > 100 => "huge",
                    _ => "normal",
                };
            }
            """,
            new[] { 5 }
        ];

        // "and"/"or" pattern combinators +1 each in is-expressions too.
        yield return
        [
            """
            public class C
            {
                public bool M(int value) => value is > 0 and < 10 or 42;
            }
            """,
            new[] { 3 }
        ];

        // Local functions are separate units; their bodies do not count toward the parent.
        yield return
        [
            """
            public class C
            {
                public int M(bool flag)
                {
                    if (flag)
                    {
                        return Local(1);
                    }

                    return Local(2);

                    int Local(int value)
                    {
                        if (value > 1)
                        {
                            return 1;
                        }

                        return 0;
                    }
                }
            }
            """,
            new[] { 2, 2 }
        ];

        // Lambdas count toward the enclosing member.
        yield return
        [
            """
            public class C
            {
                public System.Func<int, int> M(bool flag)
                {
                    if (flag)
                    {
                        return x => x > 0 ? x : -x;
                    }

                    return x => x;
                }
            }
            """,
            new[] { 3 }
        ];

        // Get and set accessor bodies are separate units.
        yield return
        [
            """
            public class C
            {
                private int _value;

                public int Value
                {
                    get => _value > 0 ? _value : 0;
                    set
                    {
                        if (value >= 0)
                        {
                            _value = value;
                        }
                    }
                }
            }
            """,
            new[] { 2, 2 }
        ];

        // Constructors and expression-bodied properties are units.
        yield return
        [
            """
            public class C
            {
                private readonly int _value;

                public C(bool flag)
                {
                    _value = flag ? 1 : 0;
                }

                public int Value => _value > 0 ? _value : 0;
            }
            """,
            new[] { 2, 2 }
        ];

        // Loop statements and catch clauses +1 each.
        yield return
        [
            """
            public class C
            {
                public int M(int[] values)
                {
                    var total = 0;
                    for (var i = 0; i < values.Length; i++)
                    {
                        total += values[i];
                    }

                    foreach (var value in values)
                    {
                        total += value;
                    }

                    while (total > 100)
                    {
                        total -= 10;
                    }

                    do
                    {
                        total++;
                    }
                    while (total < 0);

                    try
                    {
                        total /= values.Length;
                    }
                    catch (System.DivideByZeroException)
                    {
                        total = 0;
                    }

                    return total;
                }
            }
            """,
            new[] { 6 }
        ];

        // Top-level statements form one method-equivalent unit.
        yield return
        [
            """
            var args = System.Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1] == "verbose")
            {
                System.Console.WriteLine("verbose");
            }

            foreach (var arg in args)
            {
                System.Console.WriteLine(arg);
            }
            """,
            new[] { 4 }
        ];

        // Top-level local functions are separate units, excluded from the top-level unit.
        yield return
        [
            """
            System.Console.WriteLine(Describe(1));
            if (System.DateTime.Now.Hour > 12)
            {
                System.Console.WriteLine(Describe(2));
            }

            string Describe(int value)
            {
                return value > 1 ? "many" : "one";
            }
            """,
            new[] { 2, 2 }
        ];
    }
}
