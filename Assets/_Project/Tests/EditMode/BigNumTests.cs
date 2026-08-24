using GrowNa.Core;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class BigNumTests
    {
        [TestCase(0.0, "0")]
        [TestCase(7.9, "7")]
        [TestCase(999.0, "999")]
        [TestCase(1000.0, "1.00K")]
        [TestCase(12345.0, "12.3K")]
        [TestCase(999999.0, "1000K")]
        [TestCase(1234567.0, "1.23M")]
        [TestCase(1.0e12, "1.00T")]
        public void Format_matches_expected_shape(double value, string expected)
            => Assert.AreEqual(expected, BigNum.Format(value));

        [Test]
        public void Format_uses_two_letter_suffix_past_trillion()
            => Assert.AreEqual("1.00aa", BigNum.Format(1.0e15));

        [Test]
        public void Format_survives_double_max()
            => Assert.IsFalse(BigNum.Format(double.MaxValue).Contains("∞"));

        [Test]
        public void Format_keeps_sign()
            => Assert.AreEqual("-1.00K", BigNum.Format(-1000.0));

        [Test]
        public void Format_handles_nan_and_infinity()
        {
            Assert.AreEqual("0", BigNum.Format(double.NaN));
            Assert.AreEqual("∞", BigNum.Format(double.PositiveInfinity));
        }

        [Test]
        public void Percent_renders_two_decimals()
            => Assert.AreEqual("1.80%", BigNum.Percent(0.018));

        [TestCase(30.0, "30초")]
        [TestCase(90.0, "1분")]
        [TestCase(3600.0, "1시간")]
        [TestCase(3725.0, "1시간 2분")]
        public void Duration_renders_korean_units(double seconds, string expected)
            => Assert.AreEqual(expected, BigNum.Duration(seconds));

        [Test]
        public void Wallet_format_delegates_to_bignum()
            => Assert.AreEqual(BigNum.Format(1234567.0), Wallet.Format(1234567.0));
    }
}
