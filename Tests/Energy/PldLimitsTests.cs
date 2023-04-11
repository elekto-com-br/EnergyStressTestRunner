using System;
using NUnit.Framework;

namespace VoltElekto.Energy
{
    [TestFixture]
    public class PldLimitsTests
    {
        [Test]
        public void BasicTest()
        {
            var limits = new StaticPldLimits();

            // Dentro 
            Assert.AreEqual(50, limits.RestrictToLimits(new DateTime(2016,1,1), 50), 1e-10);
            Assert.AreEqual(50, limits.RestrictToLimits(new DateTime(2017,1,1), 50), 1e-10);
            Assert.AreEqual(50, limits.RestrictToLimits(new DateTime(2018,1,1), 50), 1e-10);
            Assert.AreEqual(50, limits.RestrictToLimits(new DateTime(2019,1,1), 50), 1e-10);
            Assert.AreEqual(50, limits.RestrictToLimits(new DateTime(2020,1,1), 50), 1e-10);
            Assert.AreEqual(50, limits.RestrictToLimits(new DateTime(2021,1,1), 50), 1e-10);

            // Abaixo
            Assert.AreEqual(30.25, limits.RestrictToLimits(new DateTime(2015,12,31), 10), 1e-10);
            Assert.AreEqual(30.25, limits.RestrictToLimits(new DateTime(2016,12,31), 10), 1e-10);
            Assert.AreEqual(33.68, limits.RestrictToLimits(new DateTime(2017,12,31), 10), 1e-10);
            Assert.AreEqual(40.16, limits.RestrictToLimits(new DateTime(2018,12,31), 10), 1e-10);
            Assert.AreEqual(42.35, limits.RestrictToLimits(new DateTime(2019,12,31), 10), 1e-10);
            Assert.AreEqual(39.68, limits.RestrictToLimits(new DateTime(2020,12,31), 10), 1e-10);
            Assert.AreEqual(49.77, limits.RestrictToLimits(new DateTime(2021,12,31), 10), 1e-10);
            Assert.AreEqual(49.77, limits.RestrictToLimits(new DateTime(2022,12,31), 10), 1e-10);

            // Acima
            Assert.AreEqual(422.56, limits.RestrictToLimits(new DateTime(2015,12,31), 1000), 1e-10);
            Assert.AreEqual(422.56, limits.RestrictToLimits(new DateTime(2016,12,31), 1000), 1e-10);
            Assert.AreEqual(533.82, limits.RestrictToLimits(new DateTime(2017,12,31), 1000), 1e-10);
            Assert.AreEqual(505.18, limits.RestrictToLimits(new DateTime(2018,12,31), 1000), 1e-10);
            Assert.AreEqual(513.89, limits.RestrictToLimits(new DateTime(2019,12,31), 1000), 1e-10);
            Assert.AreEqual(559.75, limits.RestrictToLimits(new DateTime(2020,12,31), 1000), 1e-10);
            Assert.AreEqual(583.88, limits.RestrictToLimits(new DateTime(2021,12,31), 1000), 1e-10);
            Assert.AreEqual(583.88, limits.RestrictToLimits(new DateTime(2022,12,31), 1000), 1e-10);

        }

    }
}
