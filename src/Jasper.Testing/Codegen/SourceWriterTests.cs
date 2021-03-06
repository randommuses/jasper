﻿using System.Linq;
using Baseline;
using Jasper.Codegen.Compilation;
using Shouldly;
using Xunit;

namespace Jasper.Testing.Codegen
{
    public class SourceWriterTests
    {
        [Fact]
        public void end_block()
        {
            var writer = new SourceWriter();
            writer.Write("BLOCK:public void Go()");
            writer.Write("var x = 0;");
            writer.Write("END");

            var lines = writer.Code().ReadLines().ToArray();

            lines[3].ShouldBe("}");
        }

        [Fact]
        public void indention_within_a_block()
        {
            var writer = new SourceWriter();
            writer.Write("BLOCK:public void Go()");
            writer.Write("var x = 0;");

            var lines = writer.Code().ReadLines().ToArray();

            lines[2].ShouldBe("    var x = 0;");
        }

        [Fact]
        public void multi_end_blocks()
        {
            var writer = new SourceWriter();
            writer.Write("BLOCK:public void Go()");
            writer.Write("BLOCK:try");
            writer.Write("var x = 0;");
            writer.Write("END");
            writer.Write("END");

            var lines = writer.Code().ReadLines().ToArray();

            lines[5].ShouldBe("    }");

            // There's a line break between the blocks
            lines[7].ShouldBe("}");
        }

        [Fact]
        public void multi_level_indention()
        {
            var writer = new SourceWriter();
            writer.Write("BLOCK:public void Go()");
            writer.Write("BLOCK:try");
            writer.Write("var x = 0;");

            var lines = writer.Code().ReadLines().ToArray();

            lines[4].ShouldBe("        var x = 0;");
        }

        [Fact]
        public void write_block()
        {
            var writer = new SourceWriter();
            writer.Write("BLOCK:public void Go()");

            var lines = writer.Code().ReadLines().ToArray();

            lines[0].ShouldBe("public void Go()");
            lines[1].ShouldBe("{");
        }

        [Fact]
        public void write_several_lines()
        {
            var writer = new SourceWriter();
            writer.Write(@"
BLOCK:public void Go()
var x = 0;
END
");

            var lines = writer.Code().Trim().ReadLines().ToArray();
            lines[0].ShouldBe("public void Go()");
            lines[1].ShouldBe("{");
            lines[2].ShouldBe("    var x = 0;");
            lines[3].ShouldBe("}");
        }
    }
}