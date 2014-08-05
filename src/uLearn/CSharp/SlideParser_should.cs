using System;
using System.Linq;
using NUnit.Framework;

namespace uLearn.CSharp
{
	[TestFixture]
	public class SlideParser_should
	{
		[Test]
		public void make_markdown_from_comments()
		{
			Slide slide = GenerateSlide("SingleComment.cs");
			Assert.That(slide.Blocks.Length, Is.EqualTo(1));
			Assert.That(slide.Blocks[0].IsCode, Is.False);
			Assert.That(slide.Blocks[0].Text, Is.EqualTo("==Multiline comment\r\nShould become markdown text"));
		}

		[Test]
		public void fail_on_wrong_identation()
		{
			Assert.That(() => GenerateSlide("WrongIdentation.cs"), Throws.Exception);
		}

		[Test]
		public void ignore_wrong_identation_of_empty_lines()
		{
			var slide = GenerateSlide("WrongIdentationOfEmptyLines.cs");
			Assert.That(slide.Blocks.Length, Is.EqualTo(1));
			Assert.That(slide.Blocks[0].Text.SplitToLines().Length, Is.EqualTo(1));
		}

		[Test]
		public void ignore_comments_inside_methods()
		{
			var slide = GenerateSlide("CommentsInsideCodeBlock.cs");
			Assert.That(slide.Blocks.Length, Is.EqualTo(1));
			Assert.That(slide.Blocks[0].IsCode);
			Assert.That(slide.Blocks[0].Text, Is.StringContaining("Not a block!"));
			Assert.That(slide.Blocks[0].Text, Is.StringContaining("Not a block too"));
		}

		[Test]
		public void make_separate_blocks_from_separate_comments()
		{
			Slide slide = GenerateSlide("ManyComments.cs");
			Assert.That(slide.Blocks.Length, Is.EqualTo(3));
			Assert.That(slide.Blocks[1].Text, Is.EqualTo("2nd block"));
		}

		[Test]
		public void make_ShowOnSlide_method_as_code_block()
		{
			Slide slide = GenerateSlide("Simple.cs");
			Assert.That(slide.Blocks.Length, Is.EqualTo(1));
			Assert.That(slide.Blocks[0].IsCode);
			Assert.That(slide, Is.TypeOf<Slide>());
		}

		[Test]
		public void make_class_as_code_block()
		{
			Slide slide = GenerateSlide("NestedClass.cs");
			Assert.That(slide.Blocks.Length, Is.EqualTo(1));
			Assert.That(slide.Blocks[0].IsCode);
			Assert.That(slide.Blocks[0].Text, Is.StringContaining("class Point"));
		}

		[Test]
		public void remove_attributes_from_nested_class_members()
		{
			Slide slide = GenerateSlide("NestedClass.cs");
			Assert.That(slide.Blocks[0].Text, Is.Not.StringContaining("["));
		}

		[Test]
		public void remove_hidden_members_of_nested_class()
		{
			Slide slide = GenerateSlide("NestedClass.cs");
			Assert.That(slide.Blocks[0].Text, Is.Not.StringContaining("Hidden"));
		}

		[Test]
		public void remove_common_nesting_of_nested_class()
		{
			Slide slide = GenerateSlide("NestedClass.cs");
			Assert.That(slide.Blocks[0].Text, Is.StringStarting("public class Point"));
		}

		[Test]
		public void remove_Excluded_members_from_solution()
		{
			var slide = (ExerciseSlide) GenerateSlide("NestedClass.cs");
			var solution = slide.Solution.BuildSolution("");
			Assert.That(solution, Is.Not.StringContaining("["));
			Assert.That(solution, Is.Not.StringContaining("]"));
			Assert.That(solution, Is.Not.StringContaining("public int X, Y"));
			Assert.That(solution, Is.Not.StringContaining("public Point(int x, int y)"));
		}

		
		[Test]
		public void make_code_block_with_method_signature_if_specified()
		{
			Slide slide = GenerateSlide("Simple.cs");
			Assert.That(slide.Blocks[0].Text, Is.StringContaining("public void Method()"));
		}

		[Test]
		public void remove_common_nesting_in_method_body()
		{
			Slide slide = GenerateSlide("Simple.cs");
			Assert.That(slide.Blocks[0].Text, Is.StringStarting("Console.WriteLine(42);"));
		}

		[Test]
		public void remove_common_nesting_in_method_with_header()
		{
			Slide slide = GenerateSlide("Simple.cs");
			Assert.That(slide.Blocks[0].Text, Is.StringContaining("\npublic void Method()"));
		}

		[Test]
		public void remove_method_header_from_code_block()
		{
			Slide slide = GenerateSlide("Simple.cs");
			var blockText = slide.Blocks[0].Text;
			Assert.That(blockText, Is.StringContaining("Console.WriteLine(42);"));
			Assert.That(blockText, Is.Not.StringContaining("HiddenMethodHeader"));
		}

		[Test]
		public void remove_hidden_members_from_code_block()
		{
			Slide slide = GenerateSlide("Simple.cs");
			var blockText = slide.Blocks[0].Text;
			Assert.That(blockText, Is.Not.StringContaining("Hidden"));
		}

		[Test]
		public void not_show_members_of_hidden_class()
		{
			Slide slide = GenerateSlide("HiddenNestedClass.cs");
			var blockText = slide.Blocks[0].Text;
			Assert.That(blockText, Is.Not.StringContaining("Hidden"));
		}

		[Test]
		public void join_adjacent_code_blocks()
		{
			Slide slide = GenerateSlide("AdjacentCodeBlocks.cs");
			Assert.That(slide.Blocks[0].IsCode, Is.True);
			Assert.That(slide.Blocks[1].IsCode, Is.False);
			Assert.That(slide.Blocks[2].IsCode, Is.True);
			Assert.That(slide.Blocks.Length, Is.EqualTo(3));
		}

		[Test]
		public void preserve_blocks_order_as_in_source_file()
		{
			Slide slide = GenerateSlide("SlideWithComments.cs");
			Assert.That(slide.Blocks.Length, Is.EqualTo(3));
			Assert.That(slide.Blocks[0].Text, Is.StringStarting("Comment"));
			Assert.That(slide.Blocks[1].IsCode);
			Assert.That(slide.Blocks[2].Text, Is.StringStarting("Final"));
		}

		[Test]
		public void make_excercise_slide_from_method_with_exercise_attribute()
		{
			var slide = (ExerciseSlide) GenerateSlide("Exercise.cs");
			Assert.That(slide.Blocks.Single().Text, Is.EqualTo("Add 2 and 3 please!"));
			Assert.That(slide.ExerciseInitialCode, Is.StringStarting("public void Add_2_and_3()"));
			Assert.That(slide.ExerciseInitialCode, Is.Not.StringContaining("NotImplementedException"));
			Assert.That(slide.HintsHtml.Length, Is.EqualTo(0));
		}

		[Test]
		public void not_count_exercise_as_code_block()
		{
			var slide = GenerateSlide("Exercise.cs");
			Assert.That(slide.Blocks.Where(b => b.IsCode), Is.Empty);
		}

		[Test]
		public void extract_ExpectedOutput()
		{
			var slide = (ExerciseSlide) GenerateSlide("Exercise.cs");
			Assert.That(slide.ExpectedOutput, Is.EqualTo("5"));
		}

		[Test]
		public void uncomment_special_comments_with_starter_code()
		{
			var slide = (ExerciseSlide) GenerateSlide("ExerciseWithStarterCode.cs");
			var exerciseLines = slide.ExerciseInitialCode.SplitToLines();
			Assert.That(exerciseLines.Length, Is.EqualTo(4), slide.ExerciseInitialCode);
			Assert.That(exerciseLines[2], Is.EqualTo("	return x + y;"));
		}

		[Test]
		public void make_hints_from_hint_attributes()
		{
			var slide = (ExerciseSlide) GenerateSlide("ExerciseWithHints.cs");
			Assert.That(slide.HintsHtml, Is.EqualTo(new[] {"<p>hint1</p>\n", "<p>hint2</p>\n"}).AsCollection);
		}

		[Test]
		public void provide_solution_for_server()
		{
			var slide = (ExerciseSlide) GenerateSlide("HelloWorld.cs");
			var userSolution = "/* no solution */";
			var ans = slide.Solution.BuildSolution(userSolution).SourceCode;
			Console.WriteLine(ans);
			StringAssert.DoesNotContain("[", ans);
			StringAssert.Contains("void Main(", ans);
			StringAssert.Contains(userSolution, ans);
			StringAssert.DoesNotContain("void HelloKitty(", ans);
		}

		[Test]
		public void include_video()
		{
			var slide = GenerateSlide("Includes.cs");
			var renderedText = slide.Blocks.First().RenderedText;
			var expected =
				"<iframe class='embedded-video' width='800' height='450' src='//www.youtube.com/embed/81Ub0SMxZQo' frameborder='0' allowfullscreen></iframe>";
			Assert.That(renderedText, Contains.Substring(expected));
		}

		[Test]
		public void include_code()
		{
			var slide = GenerateSlide("Includes.cs");
			var renderedText = slide.Blocks[1].RenderedText;
			var expected = "included(_HelloWorld.cs)";
			Assert.That(renderedText, Contains.Substring(expected));
		}

		private static Slide GenerateSlide(string name)
		{
			return SlideParser.ParseSlide(@".\tests\" + name, null, includeName => "included(" + includeName + ")");
		}
	}
}