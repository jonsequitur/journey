﻿using System;
using Microsoft.DotNet.Interactive.Journey.Tests.Utilities;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.DotNet.Interactive.Journey.Tests
{
    public class TeacherValidationTests : ProgressiveLearningTestBase
    {
        private async Task RunAllCells(FileInfo file, CompositeKernel kernel)
        {
            var notebook = await NotebookLessonParser.ReadFileAsInteractiveDocument(file, kernel);
            
            foreach (var cell in notebook.Elements.Where(e=> e.Language != "markdown"))
            {
                await kernel.SendAsync(new SubmitCode(cell.Contents, cell.Language));
            }
        }

        [Fact]
        public async Task teacher_can_use_scratchpad_to_validate_their_material()
        {
            var filename = "teacherValidation.dib";
            var file = new FileInfo(GetNotebookPath(filename));
            
            var kernel = await CreateKernel(LessonMode.TeacherMode);
            using var events = kernel.KernelEvents.ToSubscribedList();

            await RunAllCells(file, kernel);

            var displayedValueProduceds = events
                .OfType<DisplayedValueProduced>()
                .Where(e => e.Value is ChallengeEvaluation);

            displayedValueProduceds
                .Should()
                .SatisfyRespectively(
                    e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                        .Value.ContainsAll(
                            "Challenge func rule failed",
                            "Challenge func not done"),
                    e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                        .Value.ContainsAll(
                            "Challenge func rule passed",
                            "Challenge func completed"),
                    e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                        .Value.ContainsAll(
                            "Challenge math rule passed",
                            "Challenge math message"),
                    e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                        .Value.ContainsAll(
                            "Challenge math rule failed",
                            "Challenge math message"));
        }

        [Fact]
        public async Task teacher_can_use_add_rule_when_starting_a_lesson()
        {
            var kernel = await CreateKernel(LessonMode.TeacherMode);
            await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetNotebookPath("teacherValidation.dib")}");

            var result = await kernel.SubmitCodeAsync("1");

            using var events = result.KernelEvents.ToSubscribedList();

            events.Should().ContainSingle<DisplayedValueProduced>(
                e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                    .Value.ContainsAll(
                        "Challenge func rule",
                        "Challenge func rule failed"));
        }

        [Fact]
        public async Task teacher_can_use_on_code_submitted_when_starting_a_lesson()
        {
            var kernel = await CreateKernel(LessonMode.TeacherMode);
            await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetNotebookPath("teacherValidation.dib")}");

            var result = await kernel.SubmitCodeAsync("1");

            using var events = result.KernelEvents.ToSubscribedList();

            events.Should().ContainSingle<DisplayedValueProduced>(
                e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                    .Value.ContainsAll(
                        "Challenge func not done"));
        }

        [Fact]
        public async Task teacher_can_use_add_rule_when_progressing_the_student_to_different_challenge()
        {
            var kernel = await CreateKernel(LessonMode.TeacherMode);
            await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetNotebookPath("teacherValidation.dib")}");
            await kernel.SubmitCodeAsync("CalculateTriangleArea = (double x, double y) => 0.5 * x * y;");

            var result = await kernel.SubmitCodeAsync("Math.Sqrt(pi)");
            using var events = result.KernelEvents.ToSubscribedList();

            events.Should().ContainSingle<DisplayedValueProduced>(
                e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                    .Value.ContainsAll(
                        "Challenge math rule",
                        "Challenge math passed"));
        }

        [Fact]
        public async Task teacher_can_use_on_code_submitted_when_progressing_the_student_to_different_challenge()
        {
            var kernel = await CreateKernel(LessonMode.TeacherMode);
            await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetNotebookPath("teacherValidation.dib")}");
            await kernel.SubmitCodeAsync("CalculateTriangleArea = (double x, double y) => 0.5 * x * y;");

            var result = await kernel.SubmitCodeAsync("Math.Sqrt(pi)");

            using var events = result.KernelEvents.ToSubscribedList();
            events.Should().ContainSingle<DisplayedValueProduced>(
                e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                    .Value.ContainsAll(
                        "Challenge math message"));
        }
    }
}
