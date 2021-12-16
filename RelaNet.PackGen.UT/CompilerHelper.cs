using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace RelaNet.PackGen.UT
{
    public static class CompilerHelper
    {
        public static Assembly Compile(string code, string dll)
        {
            string fileName = dll + ".dll";
            string assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            List<MetadataReference> refs = new List<MetadataReference>();
            refs.Add(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location));
            refs.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "netstandard.dll")));
            refs.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")));
            refs.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.CoreLib.dll")));
            refs.Add(MetadataReference.CreateFromFile(".\\RelaNet.PackGen.UT.dll"));
            refs.Add(MetadataReference.CreateFromFile(".\\RelaNet.dll"));
            refs.Add(MetadataReference.CreateFromFile(".\\RelaNet.Utilities.dll"));
            refs.Add(MetadataReference.CreateFromFile(".\\RelaStructures.dll"));

            SyntaxTree parsed = CSharpSyntaxTree.ParseText(code);

            CSharpCompilation compilation = CSharpCompilation.Create(dll)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(refs)
                .AddSyntaxTrees(parsed);

            EmitResult compilationResult = compilation.Emit(fileName);
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(fileName));
        }
    }
}
