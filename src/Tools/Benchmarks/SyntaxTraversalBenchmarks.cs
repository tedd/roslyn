// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Benchmarks;

[MemoryDiagnoser]
public class SyntaxTraversalBenchmarks
{
    private SyntaxNode _root = null!;

    [Params(16, 512)]
    public int MethodCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var source = new StringBuilder("class C {");
        for (var i = 0; i < MethodCount; i++)
        {
            source.Append("int M").Append(i).Append("(int x) => x > 0 ? F(x) : -x;");
        }

        source.Append("static int F(int x) => x + 1; }");
        _root = CSharpSyntaxTree.ParseText(SourceText.From(source.ToString())).GetRoot();

        if (!_root.DescendantNodes().SequenceEqual(EnumerateChildren(_root)) ||
            !_root.DescendantNodesAndSelf().SequenceEqual(EnumerateChildren(_root).Prepend(_root)))
        {
            throw new InvalidOperationException("Traversal differs from the child-node walk.");
        }
    }

    [Benchmark]
    public int DescendantNodes()
        => CountNodes(_root.DescendantNodes());

    [Benchmark]
    public int DescendantNodesAndSelf()
        => CountNodes(_root.DescendantNodesAndSelf());

    [Benchmark]
    public int DescendantNodesWithExplicitSpan()
        => CountNodes(_root.DescendantNodes(_root.FullSpan));

    private static int CountNodes(IEnumerable<SyntaxNode> nodes)
    {
        var count = 0;
        foreach (var node in nodes)
        {
            count++;
        }

        return count;
    }

    private static IEnumerable<SyntaxNode> EnumerateChildren(SyntaxNode node)
    {
        foreach (var child in node.ChildNodes())
        {
            yield return child;
            foreach (var descendant in EnumerateChildren(child))
            {
                yield return descendant;
            }
        }
    }
}
