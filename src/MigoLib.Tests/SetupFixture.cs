using MigoLib.Fake;
using NUnit.Framework;
using Microsoft.Extensions.Logging;

namespace MigoLib.Tests
{
    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            var logger = Init.LoggerFactory.CreateLogger<FakeMigo>();
            TestEnvironment.FakeMigo = new FakeMigo(TestEnvironment.Ip, TestEnvironment.Port, logger);
            TestEnvironment.FakeMigo.Start();
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            TestEnvironment.FakeMigo.Stop();
        }
    }
}