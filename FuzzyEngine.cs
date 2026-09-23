using System;
using System.Collections.Generic;

namespace SeasonFuzzy
{
    public class SeasonMembership
    {
        public double Winter;
        public double Spring;
        public double Summer;
        public double Autumn;
    }

    public class TimeMembership
    {
        public double Night;
        public double Dawn;
        public double Day;
        public double Dusk;
    }

    public class RainMembership
    {
        public double None;
        public double Light;
        public double Heavy;
    }

    public class FiredRule
    {
        public string Name { get; set; }
        public double Weight { get; set; }
        public string ConsequentDescription { get; set; }
    }

    public class SceneOutputs
    {
        public SeasonMembership Season;
        public TimeMembership Time;
        public RainMembership RainState;

        // Pure Sugeno defuzzified outputs
        public double SkyBrightness;
        public double CloudCover;
        public double Stars;
        public double Rain;
        public double Snow;
        public double Foliage;
        public double GroundSnow;

        // Visual helpers
        public double RainFraction;
        public double Twilight;
        public double DimAmount;

        public List<FiredRule> ActiveRules = new List<FiredRule>();
    }

    public static class FuzzyEngine
    {
        static readonly int[] DaysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        public static int GetDaysInMonth(int month) => DaysInMonth[month - 1];

        public static double Clamp01(double x) => Math.Max(0.0, Math.Min(1.0, x));

        public static double Trimf(double x, double a, double b, double c)
        {
            if (x <= a || x >= c) return 0.0;
            if (x < b) return (x - a) / (b - a);
            if (x == b) return 1.0;
            return (c - x) / (c - b);
        }

        public static double Trapmf(double x, double a, double b, double c, double d)
        {
            if (x < a || x > d) return 0.0;
            if (x >= b && x <= c) return 1.0; 
            if (x < b) return (b == a) ? 1.0 : (x - a) / (b - a);
            return (d == c) ? 1.0 : (d - x) / (d - c);
        }

        public static double CyclicTrap(double h, double a, double b, double c, double d)
        {
            return Math.Max(Trapmf(h, a, b, c, d), Math.Max(Trapmf(h + 24, a, b, c, d), Trapmf(h - 24, a, b, c, d)));
        }

        public static int DayOfYear(int month, int day)
        {
            int doy = day;
            for (int i = 0; i < month - 1; i++) doy += DaysInMonth[i];
            return doy;
        }

        public static SeasonMembership FuzzifySeason(int doy)
        {
            // Winter peaks around Dec 15 to Jan 15 (Day 350 to Day 15) and wraps cleanly across year end
            double winter;
            if (doy <= 75)
            {
                winter = Trapmf(doy, -20, 0, 15, 75);
            }
            else
            {
                winter = Trapmf(doy, 300, 345, 366, 390);
            }

            double spring = Trimf(doy, 45, 105, 165);
            double summer = Trimf(doy, 135, 196, 255);
            double autumn = Trimf(doy, 225, 288, 345);

            double sum = winter + spring + summer + autumn;
            if (sum <= 0.00001) sum = 1.0;

            return new SeasonMembership
            {
                Winter = winter / sum,
                Spring = spring / sum,
                Summer = summer / sum,
                Autumn = autumn / sum
            };
        }

        public static TimeMembership FuzzifyTime(double h)
        {
            // Cyclic trapezoids over 24-hour cycle
            double night = CyclicTrap(h, 20, 22, 28, 30); // 20:00 to 06:00
            double dawn = CyclicTrap(h, 4, 6, 6, 8);
            double day = CyclicTrap(h, 6, 8, 17, 19);
            double dusk = CyclicTrap(h, 17, 19, 19, 21);

            double sum = night + dawn + day + dusk;
            if (sum <= 0.00001) sum = 1.0;

            return new TimeMembership
            {
                Night = night / sum,
                Dawn = dawn / sum,
                Day = day / sum,
                Dusk = dusk / sum
            };
        }

        public static RainMembership FuzzifyRain(double mm)
        {
            return new RainMembership
            {
                None = Trapmf(mm, -10, 0, 5, 20),
                Light = Trimf(mm, 10, 35, 60),
                Heavy = Trapmf(mm, 45, 75, 100, 110)
            };
        }

        private class SugenoRule
        {
            public string Name;
            public double Weight;
            public double Brightness;
            public double Cloud;
            public double Stars;
            public double Rain;
            public double Snow;
            public double Foliage;
            public double GroundSnow;
            public string Consequent;
            public bool IsSkyRule;
            public bool IsPrecipRule;
            public bool IsSurfaceRule;
        }

        public static SceneOutputs Infer(int doy, double hour, double rainMm)
        {
            SeasonMembership s = FuzzifySeason(doy);
            TimeMembership t = FuzzifyTime(hour);
            RainMembership r = FuzzifyRain(rainMm);

            List<SugenoRule> rules = new List<SugenoRule>();

            void AddRule(string name, double ant, double br, double cld, double st, double rn, double sn, double fol, double gsn, string desc, bool sky, bool precip, bool surface)
            {
                if (ant > 0.0001)
                {
                    rules.Add(new SugenoRule
                    {
                        Name = name,
                        Weight = ant,
                        Brightness = br,
                        Cloud = cld,
                        Stars = st,
                        Rain = rn,
                        Snow = sn,
                        Foliage = fol,
                        GroundSnow = gsn,
                        Consequent = desc,
                        IsSkyRule = sky,
                        IsPrecipRule = precip,
                        IsSurfaceRule = surface
                    });
                }
            }

            // --- 1. Sky & Atmospheric Subsystem Rules ---
            AddRule("R1: Day & Clear", Math.Min(t.Day, r.None), 1.0, 0.05, 0.0, 0.0, 0.0, 0.0, 0.0, "Daylight Clear", true, false, false);
            AddRule("R2: Day & LightRain", Math.Min(t.Day, r.Light), 0.65, 0.60, 0.0, 0.0, 0.0, 0.0, 0.0, "Overcast Dim Day", true, false, false);
            AddRule("R3: Day & HeavyRain", Math.Min(t.Day, r.Heavy), 0.35, 0.95, 0.0, 0.0, 0.0, 0.0, 0.0, "Dark Stormy Day", true, false, false);
            AddRule("R4: Dawn/Dusk & Clear", Math.Min(Math.Max(t.Dawn, t.Dusk), r.None), 0.45, 0.20, 0.20, 0.0, 0.0, 0.0, 0.0, "Soft Twilight", true, false, false);
            AddRule("R5: Dawn/Dusk & Rain", Math.Min(Math.Max(t.Dawn, t.Dusk), Math.Max(r.Light, r.Heavy)), 0.25, 0.85, 0.0, 0.0, 0.0, 0.0, 0.0, "Dreary Twilight", true, false, false);
            AddRule("R6: Night & Clear", Math.Min(t.Night, r.None), 0.08, 0.05, 1.0, 0.0, 0.0, 0.0, 0.0, "Clear Starry Night", true, false, false);
            AddRule("R7: Night & Overcast", Math.Min(t.Night, Math.Max(r.Light, r.Heavy)), 0.02, 0.90, 0.0, 0.0, 0.0, 0.0, 0.0, "Pitch Dark Overcast", true, false, false);

            // --- 2. Precipitation Subsystem Rules ---
            AddRule("R8: Dry Condition", r.None, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, "No Precipitation", false, true, false);
            AddRule("R9: Winter Light Snow", Math.Min(s.Winter, r.Light), 0.0, 0.0, 0.0, 0.0, 0.50, 0.0, 0.0, "Winter Light Snow", false, true, false);
            AddRule("R10: Winter Blizzard", Math.Min(s.Winter, r.Heavy), 0.0, 0.0, 0.0, 0.0, 1.00, 0.0, 0.0, "Winter Blizzard", false, true, false);
            AddRule("R11: Non-Winter Light Rain", Math.Min(1.0 - s.Winter, r.Light), 0.0, 0.0, 0.0, 0.45, 0.0, 0.0, 0.0, "Gentle Rain", false, true, false);
            AddRule("R12: Non-Winter Heavy Rain", Math.Min(1.0 - s.Winter, r.Heavy), 0.0, 0.0, 0.0, 0.95, 0.0, 0.0, 0.0, "Heavy Downpour", false, true, false);

            // --- 3. Biome / Foliage / Ground Subsystem Rules ---
            AddRule("R13: Winter Ground Mantle", s.Winter, 0.0, 0.0, 0.0, 0.0, 0.0, 0.05, 0.85, "Winter Snowpack", false, false, true);
            AddRule("R14: Spring Sprouting", s.Spring, 0.0, 0.0, 0.0, 0.0, 0.0, 0.65, 0.05, "Spring Growth", false, false, true);
            AddRule("R15: Summer Peak Lush", s.Summer, 0.0, 0.0, 0.0, 0.0, 0.0, 1.00, 0.00, "Lush Summer Leaves", false, false, true);
            AddRule("R16: Autumn Deciduous", s.Autumn, 0.0, 0.0, 0.0, 0.0, 0.0, 0.75, 0.00, "Autumn Canopy", false, false, true);

            // --- Sugeno Defuzzification: Output = SUM(w_i * z_i) / SUM(w_i) ---
            double twSky = 0.0, sumBr = 0, sumCld = 0, sumSt = 0;
            double twPrecip = 0.0, sumRn = 0, sumSn = 0;
            double twSurface = 0.0, sumFol = 0, sumGsn = 0;

            var activeList = new List<FiredRule>();

            foreach (var rule in rules)
            {
                if (rule.IsSkyRule)
                {
                    twSky += rule.Weight;
                    sumBr += rule.Weight * rule.Brightness;
                    sumCld += rule.Weight * rule.Cloud;
                    sumSt += rule.Weight * rule.Stars;
                }
                if (rule.IsPrecipRule)
                {
                    twPrecip += rule.Weight;
                    sumRn += rule.Weight * rule.Rain;
                    sumSn += rule.Weight * rule.Snow;
                }
                if (rule.IsSurfaceRule)
                {
                    twSurface += rule.Weight;
                    sumFol += rule.Weight * rule.Foliage;
                    sumGsn += rule.Weight * rule.GroundSnow;
                }

                activeList.Add(new FiredRule
                {
                    Name = rule.Name,
                    Weight = rule.Weight,
                    ConsequentDescription = rule.Consequent
                });
            }

            // Avoid division by zero
            if (twSky <= 0.0001) twSky = 1.0;
            if (twPrecip <= 0.0001) twPrecip = 1.0;
            if (twSurface <= 0.0001) twSurface = 1.0;

            double defuzzBrightness = Clamp01(sumBr / twSky);
            double defuzzCloud = Clamp01(sumCld / twSky);
            double defuzzStars = Clamp01(sumSt / twSky);

            double defuzzRain = Clamp01(sumRn / twPrecip);
            double defuzzSnow = Clamp01(sumSn / twPrecip);

            double defuzzFoliage = Clamp01(sumFol / twSurface);
            double defuzzGroundSnow = Clamp01(sumGsn / twSurface);

            double twi = t.Dawn + t.Dusk;
            double dim = Clamp01(0.75 * t.Night + 0.20 * twi + 0.25 * defuzzCloud);

            return new SceneOutputs
            {
                Season = s,
                Time = t,
                RainState = r,
                SkyBrightness = defuzzBrightness,
                CloudCover = defuzzCloud,
                Stars = defuzzStars,
                Rain = defuzzRain,
                Snow = defuzzSnow,
                Foliage = defuzzFoliage,
                GroundSnow = defuzzGroundSnow,
                RainFraction = rainMm / 100.0,
                Twilight = twi,
                DimAmount = dim,
                ActiveRules = activeList
            };
        }
    }
}