using System;
using System.Text;
using FluentAssertions;
using MigoLib.FileUpload;
using MigoLib.Print;
using MigoLib.State;
using NUnit.Framework;

namespace MigoLib.Tests
{
    [TestFixture]
    public class PositionalSerializerTests
    {
        private const string StateInput = "state;0.00;0.00;23;25;0;10;1;0;0;0";

        [SetUp]
        public void Init()
        {
        }

        [Test]
        public void Should_deserialize_state_input()
        {
            var serializer = PositionalSerializer
                .CreateFor<MigoStateModel>(';')
                .FixedString("state")
                .Skip(2)
                .Field(x => x.NozzleTemp)
                .Field(x => x.BedTemp);

            var migoState = serializer.Parse(StateInput);
            
            migoState.BedTemp.Should().Be(25);
            migoState.NozzleTemp.Should().Be(23);
        }
        
        [Test]
        public void Should_deserialize_percent_input()
        {
            var serializer = new PositionalSerializer<FilePercentResult>(':')
                .FixedString("filepercent")
                .Field(x => x.Percent);

            var result = serializer.Parse("filepercent:10");

            result.Percent.Should().Be(10);
        }
        
        [Test]
        [TestCase("state", false)]
        [TestCase("Xstate", true)]
        public void Should_set_iserror_on_deserialization_accordingly(string marker, bool expected)
        {
            var serializer = PositionalSerializer
                .CreateFor<MigoStateModel>(';')
                .FixedString(marker);

            _ = serializer.Parse(StateInput);

            serializer.IsError.Should().Be(expected);
        }

        [Test]
        [TestCase("printstartsuccess;fn:3DBenchy.gcode", true)]
        [TestCase("printstartfailed;fn:3DBenchy.gcode", false)]
        public void Should_parse_two_variants_of_migo_response(string incoming, bool expected)
        {
            var serializer = new PositionalSerializer<StartPrintResult>(';')
                .Switch(
                    ("printstartsuccess", x => x.PrintStarted, true), 
                    ("printstartfailed", x => x.PrintStarted, false)
                )
                .Skip(1); // skip "fn:(.*)***.gcode";

            var result = serializer.Parse(incoming);

            result.PrintStarted.Should().Be(expected);
        }
    }
}