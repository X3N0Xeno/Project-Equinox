using System;
using System.Collections.Generic;
using System.Drawing;

namespace SeasonFuzzy
{
    public enum SettlementPriority
    {
        ShelterAndFire,
        FoodAndForaging,
        WorkAndFuel,
        CourtshipAndMating,
        ColonizationAndExpansion,
        RitualAndFaith,
        WarAndInvasion
    }

    public enum LifeStage
    {
        Baby,
        Adult,
        Elder
    }

    public enum SocietalCreed
    {
        Savages,
        IdolWorshippers,
        StrugglingTribe,
        GodFearing
    }

    public enum HistoricalEra
    {
        StoneAge,
        MedievalEra,
        IndustrialEra,
        ModernEra
    }

    public class WorldEvent
    {
        public double Year;
        public string Text;
        public Color TextColor;
    }

    public class SettlementNode
    {
        public int Id;
        public string Name;
        public float X;
        public float FlameIntensity = 0.6f;
        public double LocalFuel = 60.0;
        public bool IsEstablished = false;
        public float EstablishmentProgress = 0f;

        public double LocalMorality = 0.65;
        public SocietalCreed Creed = SocietalCreed.GodFearing;
        public int WarTargetId = -1;
        public bool IsAtWar => WarTargetId != -1;

        public int LeaderEntityId = -1;
        public string LeaderTitle = "Chieftain";
        public string LeaderName = "None";
    }

    public class HolySmiteEffect
    {
        public float X;
        public float Y;
        public float Timer = 1.0f;
    }

    public class CitizenGenome
    {
        public double Health;
        public double Intelligence;
        public double Strength;
        public double Agility;
        public double Potency;

        public static CitizenGenome RandomGenome(Random rng)
        {
            return new CitizenGenome
            {
                Health = rng.Next(40, 65),
                Intelligence = rng.Next(35, 60),
                Strength = rng.Next(40, 65),
                Agility = rng.Next(35, 60),
                Potency = rng.Next(40, 65)
            };
        }

        public static CitizenGenome Cross(CitizenGenome dad, CitizenGenome mom, Random rng)
        {
            double avgPotency = (dad.Potency + mom.Potency) / 2.0;
            double mutationBias = (avgPotency - 50.0) / 12.0;

            Func<double, double, double> blend = (v1, v2) =>
            {
                double baseVal = (v1 + v2) / 2.0;
                double mutation = (rng.NextDouble() * 14.0 - 7.0) + mutationBias;
                return Math.Max(10.0, Math.Min(100.0, baseVal + mutation));
            };

            return new CitizenGenome
            {
                Health = blend(dad.Health, mom.Health),
                Intelligence = blend(dad.Intelligence, mom.Intelligence),
                Strength = blend(dad.Strength, mom.Strength),
                Agility = blend(dad.Agility, mom.Agility),
                Potency = blend(dad.Potency, mom.Potency)
            };
        }
    }

    public class HumanEntity
    {
        public int Id;
        public string FirstName;
        public string LastName;
        public string Gender;
        public string FatherName = "Unknown";
        public string MotherName = "Unknown";

        public float BaseX;
        public float TargetX;
        public float CurrentX;
        public float WalkCycle;
        public double AgeYears;
        public double MaxLifespanYears;
        public double CurrentHealth = 100.0;
        public bool IsAlive = true;
        public bool HasTorch = false;
        public bool IsArmed = false;
        public bool IsLeader = false;
        public string CauseOfDeath = "";

        public int AssignedSettlementId = 1;
        public bool IsPioneer = false;

        public float MatingCooldown = 0f;
        public CitizenGenome Genome;

        public string SpeechText = "";
        public float SpeechTimer = 0f;
        public float NextSpeechCooldown = 2f;

        public LifeStage Stage
        {
            get
            {
                if (AgeYears < 4.0) return LifeStage.Baby;
                if (AgeYears < 55.0) return LifeStage.Adult;
                return LifeStage.Elder;
            }
        }

        public string FullName => $"{FirstName} {LastName}";
    }

    public class GraveMarker
    {
        public float X;
        public float Y;
        public string Name;
        public string DeathQuote = "Resting in Peace";
        public float FadeTimer = 4.0f;
        public bool IsDesecrated = false;
    }

    public class SocietyOutputs
    {
        public double MoodPercent;
        public string MoodDescription;
        public SettlementPriority Priority;
        public HistoricalEra Era;
        public double PerceivedEraProgress;
        public double MoralityPercent;
        public double CampfireFlame;
        public double PerceivedTemperature;
        public double ClimateShock;
        public int Population;
        public int BabyCount;
        public int FoodReserves;
        public int TotalDeaths;
        public double CurrentYear;
        public double AverageHealth;
        public double AverageIntelligence;
        public double AverageStrength;
        public int ActiveColoniesCount;
        public double MapControlPercent;
        public List<WorldEvent> LiveFeed = new List<WorldEvent>();
        public int SelectedSettlementId = -1;
    }
}