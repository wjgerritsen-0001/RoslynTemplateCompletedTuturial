using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = MakeConst.Test.CSharpCodeFixVerifier<
    MakeConst.MakeConstAnalyzer,
    MakeConst.MakeConstCodeFixProvider>;

namespace MakeConst.Test
{
    [TestClass]
    public class MakeConstUnitTest
    {
        #region testCode
        private const string LocalIntCouldBeConstant = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            {|#0:int i = 0;|}
            Console.WriteLine(i);
        }
    }
}";

        private const string LocalIntCouldBeConstantFixed = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            const int i = 0;
            Console.WriteLine(i);
        }
    }
}";
        private const string MultipleInitializers = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int i = 0, j = DateTime.Now.DayOfYear;
            Console.WriteLine(i + j);
        }
    }
}";
        private const string InitializerNotConstant = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int i = DateTime.Now.DayOfYear;
            Console.WriteLine(i);
        }
    }
}";
        private const string NoInitializer = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int i;
            i = 0;
            Console.WriteLine(i);
        }
    }
}";
        private const string AlreadyConst = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            const int i = 0;
            Console.WriteLine(i);
        }
    }
}";
        private const string VariableAssigned = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int i = 0;
            Console.WriteLine(i++);
        }
    }
}";
        private const string DeclarationIsInvalid = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            int x = {|#0:""abc""|};
        }
    }
}";
        private const string ReferenceTypeIsntString = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            object s = ""abc"";
        }
    }
}";
        private const string ConstantIsString = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            {|#0:string s = ""abc"";|}
        }
    }
}";

        private const string ConstantIsStringFixed = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            const string s = ""abc"";
        }
    }
}";
        private const string DeclarationUsesVar = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            {|#0:var item = 4;|}
        }
    }
}";

        private const string DeclarationUsesVarFixedHasType = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            const int item = 4;
        }
    }
}";
        private const string StringDeclarationUsesVar = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            {|#0:var item = ""abc"";|}
        }
    }
}";
        private const string StringDeclarationUsesVarFixedHasType = @"
using System;

namespace MakeConstTest
{
    class Program
    {
        static void Main(string[] args)
        {
            const string item = ""abc"";
        }
    }
}";
        #endregion
        
        //No diagnostics expected to show up
        [DataTestMethod]
        [DataRow(""),
         DataRow(VariableAssigned),
         DataRow(AlreadyConst),
         DataRow(NoInitializer),
         DataRow(InitializerNotConstant),
         DataRow(MultipleInitializers),
         DataRow(ReferenceTypeIsntString)]
        public async Task WhenTestCodeIsValidNoDiagnosticIsTriggeredAsync(string testCode)
        {
            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [TestMethod]
        public async Task WhenTestCodeIsInValidOnlyOtherDiagnosticIsTriggeredAsync()
        {
            DiagnosticResult expected = DiagnosticResult
                .CompilerError("CS0029")
                .WithLocation(0)
                .WithArguments("string", "int");
            await VerifyCS.VerifyAnalyzerAsync(DeclarationIsInvalid, expected);
        }

        [DataTestMethod]
        [DataRow(LocalIntCouldBeConstant, LocalIntCouldBeConstantFixed),
         DataRow(ConstantIsString, ConstantIsStringFixed),
         DataRow(DeclarationUsesVar, DeclarationUsesVarFixedHasType),
         DataRow(StringDeclarationUsesVar, StringDeclarationUsesVarFixedHasType)]
        public async Task WhenDiagnosticIsRaisedFixUpdatesCode(
            string test,
            string fixTest)
        {
            var expected = VerifyCS.Diagnostic(MakeConstAnalyzer.DiagnosticId)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessage(new LocalizableResourceString(nameof(MakeConst.Resources.AnalyzerMessageFormat), MakeConst.Resources.ResourceManager, typeof(MakeConst.Resources)).ToString())
                .WithLocation(0);

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }


    }
}
