using System.Reflection;

namespace ScreenMonitor.Tests;

public static class TestRunner
{
    public static int Run()
    {
        var total = 0;
        var passed = 0;
        var failed = 0;

        var testTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.Name.EndsWith("Tests") && !t.IsAbstract);

        foreach (var type in testTypes)
        {
            Console.WriteLine("\n=== " + type.Name + " ===");
            var instance = Activator.CreateInstance(type);

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                .OrderBy(m => m.Name);

            foreach (var method in methods)
            {
                total++;
                try
                {
                    var task = method.Invoke(instance, null);
                    if (task is Task t) t.GetAwaiter().GetResult();
                    Console.WriteLine("  [PASS] " + method.Name);
                    passed++;
                }
                catch (Exception ex)
                {
                    var realEx = ex is TargetInvocationException tie ? tie.InnerException ?? ex : ex;
                    Console.WriteLine("  [FAIL] " + method.Name);
                    Console.WriteLine("         " + realEx.Message);
                    failed++;
                }
            }
        }

        Console.WriteLine("\n======");
        Console.WriteLine(total + " total, " + passed + " passed, " + failed + " failed");
        return failed > 0 ? 1 : 0;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class TestAttribute : Attribute { }

public static class Assert
{
    public static void True(bool c, string m = null) { if (!c) throw new Exception(m ?? "Expected True"); }
    public static void False(bool c, string m = null) { if (c) throw new Exception(m ?? "Expected False"); }
    public static void Equal<T>(T e, T a, string m = null) { if (!EqualityComparer<T>.Default.Equals(e, a)) throw new Exception(m ?? string.Format("Expected: {0}, Actual: {1}", e, a)); }
    public static void NotNull(object o, string m = null) { if (o == null) throw new Exception(m ?? "Expected non-null"); }
    public static void Null(object o, string m = null) { if (o != null) throw new Exception(m ?? "Expected null"); }
    public static void Single<T>(IEnumerable<T> c, string m = null) { var cnt = c.Count(); if (cnt != 1) throw new Exception(m ?? string.Format("Expected 1, got {0}", cnt)); }
    public static void Empty<T>(IEnumerable<T> c, string m = null) { if (c.Any()) throw new Exception(m ?? "Expected empty"); }
    public static void NotEmpty<T>(IEnumerable<T> c, string m = null) { if (c == null || !c.Any()) throw new Exception(m ?? "Expected non-empty"); }
    public static void Contains<T>(IEnumerable<T> c, Func<T, bool> p, string m = null) { if (!c.Any(p)) throw new Exception(m ?? "No match"); }
    public static void Contains(string haystack, string needle, string m = null) { if (haystack == null || !haystack.Contains(needle)) throw new Exception(m ?? string.Format("Does not contain: {0}", needle)); }
    public static void StartsWith(string haystack, string needle, string m = null) { if (haystack == null || !haystack.StartsWith(needle)) throw new Exception(m ?? string.Format("Does not start with: {0}", needle)); }
}
