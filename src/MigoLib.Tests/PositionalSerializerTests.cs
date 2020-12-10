using FluentAssertions;
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
                .CreateFor<MigoState>(';')
                .FixedString("state")
                .Skip(2)
                .Field(x => x.NozzleTemp)
                .Field(x => x.BedTemp);

            var migoState = serializer.Parse(StateInput);
            
            migoState.BedTemp.Should().Be(25);
            migoState.NozzleTemp.Should().Be(23);
        }
        
        [Test]
        [TestCase("state", false)]
        [TestCase("Xstate", true)]
        public void Should_set_iserror_on_deserialization_accordingly(string marker, bool expected)
        {
            var serializer = PositionalSerializer
                .CreateFor<MigoState>(';')
                .FixedString(marker);

            _ = serializer.Parse(StateInput);

            serializer.IsError.Should().Be(expected);
        }
    }
}