// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Linq;

namespace RIFF.Core
{
    public enum MonetaryAmountType
    {
        NotSet = 0,
        MonetaryAmount = 1,
        MonetaryValue = 2
    };

    public static class RFRounding
    {
        public static decimal? Round(decimal? v, int digits = 6)
        {
            return RoundPrice(v, digits);
        }

        public static decimal Round(decimal v, int digits = 6)
        {
            return RoundPrice(v, digits);
        }

        public static double? Round(double? v, int digits = 6)
        {
            if (v.HasValue)
            {
                return Math.Round(v.Value, digits, MidpointRounding.AwayFromZero);
            }
            return null;
        }

        public static decimal RoundMonetaryAmount(decimal v, MonetaryAmountType amountType = MonetaryAmountType.NotSet)
        {
            int digits = 2;
            switch (amountType)
            {
                case MonetaryAmountType.MonetaryValue:
                    digits = 4;
                    break;
            }
            return Math.Round(v, digits, MidpointRounding.AwayFromZero);
        }

        public static decimal? RoundMonetaryAmount(decimal? v, MonetaryAmountType amountType = MonetaryAmountType.NotSet)
        {
            if (v.HasValue)
            {
                return RoundMonetaryAmount(v.Value, amountType);
            }
            return null;
        }

        public static decimal RoundPercentage(decimal v, int digits = 6)
        {
            return Math.Round(v, digits + 2, MidpointRounding.AwayFromZero);
        }

        public static decimal? RoundPercentage(decimal? v, int digits = 6)
        {
            if (v.HasValue)
            {
                return RoundPercentage(v.Value, digits);
            }
            return null;
        }

        public static decimal? RoundPrice(decimal? v, int digits = 6)
        {
            if (v.HasValue)
            {
                return RoundPrice(v.Value, digits);
            }
            return null;
        }

        public static decimal RoundPrice(decimal v, int digits = 6)
        {
            return Math.Round(v, digits, MidpointRounding.AwayFromZero);
        }

        public static decimal RoundReturn(decimal amount, decimal baseAmount, int decimals = 10)
        {
            if (baseAmount != 0)
            {
                return Math.Round(amount / baseAmount, decimals + 2, MidpointRounding.AwayFromZero);
            }
            return 0;
        }

        public static decimal? RoundReturn(decimal? amount, decimal? baseAmount, int decimals = 10)
        {
            if (baseAmount.HasValue && baseAmount.Value != 0 && amount.HasValue)
            {
                return RoundReturn(amount.Value, baseAmount.Value, decimals);
            }
            return null;
        }

        /// <summary>
        /// Round all decimals to roundingDigits and adjust so that their total adds up to rounded total
        /// </summary>
        public static decimal?[] SmartRound(int digits, params decimal?[] values)
        {
            var actualTotal = RoundPrice(values.Where(v => v.HasValue).Sum(), digits);
            var roundedValues = values.Select(v => RoundPrice(v, digits)).ToArray();
            var roundedTotal = roundedValues.Where(v => v.HasValue).Sum();

            if (roundedTotal != actualTotal)
            {
                var roundedAdjustmentNecessary = actualTotal - roundedTotal;
                decimal smallestRelativeAdjustment = Decimal.MaxValue;
                int smallestRelativeIndex = -1;
                for (int i = 1; i < values.Length; ++i)
                {
                    if (values[i].HasValue && values[i].Value != 0)
                    {
                        var desiredRoundedValue = roundedValues[i] + roundedAdjustmentNecessary;
                        var relativeAdjustmentNecessary = Math.Abs((desiredRoundedValue.Value - values[i].Value) / values[i].Value);
                        if (relativeAdjustmentNecessary < smallestRelativeAdjustment)
                        {
                            smallestRelativeAdjustment = relativeAdjustmentNecessary;
                            smallestRelativeIndex = i;
                        }
                    }
                }

                // adjust [smallestRelativeIndex]
                if (smallestRelativeIndex != -1)
                {
                    roundedValues[smallestRelativeIndex] += roundedAdjustmentNecessary;

                    if (roundedValues.Sum() != actualTotal)
                    {
                        throw new RFLogicException(typeof(RFRounding), "Internal error at SmartRound");
                    }
                }
            }

            return roundedValues;
        }

        public static void SmartRound<K, V>(int digits, Dictionary<K, V> dic, Func<V, decimal?> getter, Action<Dictionary<K, V>, K, decimal?> setter)
        {
            if (dic != null)
            {
                var values = SmartRound(digits, dic.OrderBy(d => d.Key).Select(d => getter(d.Value)).ToArray());
                int i = 0;
                foreach (var key in dic.Keys.OrderBy(k => k).ToList())
                {
                    if (values[i].HasValue)
                    {
                        setter(dic, key, values[i].Value);
                    }
                    i++;
                }
            }
        }

        public static void SmartRound<T>(int digits, Dictionary<T, decimal> dic)
        {
            if (dic != null)
            {
                SmartRound(digits, dic, d => d, (d, k, v) => d[k] = v.Value);
            }
        }

        public static void SmartRound<T>(int digits, Dictionary<T, decimal?> dic)
        {
            if (dic != null)
            {
                SmartRound(digits, dic, d => d.Value, (d, k, v) => d[k] = v);
            }
        }
    }
}
