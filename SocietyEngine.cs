using System;
using System.Collections.Generic;
using System.Drawing;

namespace SeasonFuzzy
{
    public static class SocietyEngine
    {
        static Random rng = new Random(2026);
        static int nextEntityId = 1;

        static readonly string[] MaleNames = { "Adam", "Noah", "Seth", "Abel", "Enoch", "Caleb", "Joshua", "Marcus", "Luther", "Arthur", "Leo", "Victor", "Silas", "Gideon", "David" };
        static readonly string[] FemaleNames = { "Eve", "Sarah", "Leah", "Hannah", "Ruth", "Esther", "Mary", "Lydia", "Grace", "Faith", "Clara", "Elena", "Naomi", "Deborah" };
        static readonly string[] ClanNames = { "Stone", "Ash", "Frost", "Iron", "Oak", "River", "Dawn", "Barrow", "Wolf", "Vance", "Wright" };

        public static double CurrentYear = 1.0;
        public static double PerceivedEraProgress = 0.0;
        public static double TechKnowledge = 0.0;
        public static double PerceivedTemp = 15.0;
        public static double FoodReserves = 85.0;
        public static double CampfireFuel = 75.0;
        public static double GlobalMorality = 0.65;
        public static int TotalDeaths = 0;
        public static int SelectedSettlementId = -1;
        public static bool IsFeedMinimized = false;

        // Maximum archived dead records allowed in memory at any time
        public const int MaxDeadArchives = 50;

        public static List<SettlementNode> Settlements = new List<SettlementNode>();
        public static List<HumanEntity> AllCitizens = new List<HumanEntity>();
        public static List<GraveMarker> Graves = new List<GraveMarker>();
        public static List<HolySmiteEffect> Smites = new List<HolySmiteEffect>();
        public static List<WorldEvent> WorldFeed = new List<WorldEvent>();

        static SocietyEngine() => ResetSettlement();

        public static void LogWorldEvent(string text, Color col)
        {
            WorldFeed.Insert(0, new WorldEvent { Year = CurrentYear, Text = text, TextColor = col });
            if (WorldFeed.Count > 12) WorldFeed.RemoveAt(WorldFeed.Count - 1);
        }

        // Per-entry buffer pruning: keeps the registry capped at MaxDeadArchives
        public static void PrunePerEntry()
        {
            int deadCount = 0;
            for (int i = 0; i < AllCitizens.Count; i++)
            {
                if (!AllCitizens[i].IsAlive) deadCount++;
            }

            int toRemove = deadCount - MaxDeadArchives;
            for (int i = 0; i < AllCitizens.Count && toRemove > 0; i++)
            {
                if (!AllCitizens[i].IsAlive)
                {
                    AllCitizens.RemoveAt(i--);
                    toRemove--;
                }
            }
        }

        public static void ResetSettlement()
        {
            AllCitizens.Clear();
            Graves.Clear();
            Settlements.Clear();
            Smites.Clear();
            WorldFeed.Clear();

            nextEntityId = 1;
            TotalDeaths = 0;
            CurrentYear = 1.0;
            PerceivedEraProgress = 0.0;
            TechKnowledge = 0.0;
            PerceivedTemp = 15.0;
            FoodReserves = 85.0;
            CampfireFuel = 75.0;
            GlobalMorality = 0.65;
            SelectedSettlementId = 1;
            IsFeedMinimized = false;

            Settlements.Add(new SettlementNode { Id = 0, Name = "West Outpost", X = 135f, IsEstablished = false, LocalMorality = 0.40, Creed = SocietalCreed.StrugglingTribe });
            Settlements.Add(new SettlementNode { Id = 1, Name = "Capital Hearth", X = 320f, IsEstablished = true, LocalMorality = 0.75, Creed = SocietalCreed.GodFearing });
            Settlements.Add(new SettlementNode { Id = 2, Name = "East Colony", X = 505f, IsEstablished = false, LocalMorality = 0.20, Creed = SocietalCreed.Savages });

            LogWorldEvent("Genesis: Civilization founded at Capital Hearth", Color.FromArgb(235, 195, 75));

            for (int i = 0; i < 6; i++)
            {
                string gender = (i % 2 == 0) ? "M" : "F";
                SpawnPerson(320 + (i - 2.5f) * 16f, rng.Next(16, 26), gender, "Unknown", "Unknown", ClanNames[rng.Next(ClanNames.Length)], 1);
            }

            ElectLeaders(AllCitizens.FindAll(c => c.IsAlive));
        }

        public static HumanEntity SpawnPerson(float xPos, double ageYears, string gender, string dad, string mom, string clan, int settlementId = 1, CitizenGenome genome = null)
        {
            var g = genome ?? CitizenGenome.RandomGenome(rng);
            string fName = (gender == "M") ? MaleNames[rng.Next(MaleNames.Length)] : FemaleNames[rng.Next(FemaleNames.Length)];
            double baseLifespan = 60.0 + (g.Health * 0.45) + rng.Next(-5, 15);

            var person = new HumanEntity
            {
                Id = nextEntityId++,
                FirstName = fName,
                LastName = clan,
                Gender = gender,
                FatherName = dad,
                MotherName = mom,
                BaseX = xPos,
                CurrentX = xPos,
                TargetX = xPos,
                WalkCycle = (float)(rng.NextDouble() * Math.PI * 2),
                AgeYears = ageYears,
                MaxLifespanYears = baseLifespan,
                CurrentHealth = 100.0,
                Genome = g,
                IsAlive = true,
                AssignedSettlementId = settlementId,
                HasTorch = ageYears >= 4.0 && rng.NextDouble() > 0.5
            };

            AllCitizens.Add(person);
            return person;
        }

        public static void FastForwardYears(double yearsDelta, SceneOutputs env)
        {
            if (yearsDelta <= 0) return;
            int wholeYears = (int)Math.Floor(yearsDelta);
            double remainder = yearsDelta - wholeYears;

            for (int y = 0; y < wholeYears; y++) StepGenerationalCycle(1.0, env);
            if (remainder > 0.0001) StepGenerationalCycle(remainder, env);

            PrunePerEntry();

            foreach (var c in AllCitizens)
            {
                if (!c.IsAlive) continue;
                SettlementNode home = Settlements.Find(s => s.Id == c.AssignedSettlementId) ?? Settlements[1];
                c.BaseX = home.X;
                c.TargetX = home.X + (float)(rng.NextDouble() * 32.0 - 16.0);
                c.CurrentX = c.TargetX;
            }
        }

        private static void StepGenerationalCycle(double dtYears, SceneOutputs env)
        {
            CurrentYear += dtYears;
            double targetEraNum = (CurrentYear >= 1400) ? 3.0 : (CurrentYear >= 700) ? 2.0 : (CurrentYear >= 250) ? 1.0 : 0.0;
            PerceivedEraProgress += 0.15 * (targetEraNum - PerceivedEraProgress);

            List<HumanEntity> alive = AllCitizens.FindAll(c => c.IsAlive);
            if (alive.Count == 0)
            {
                SpawnPerson(315, 18.0, "M", "Adam", "Eve", "Genesis", 1);
                SpawnPerson(325, 18.0, "F", "Adam", "Eve", "Genesis", 1);
                FoodReserves = 70.0;
                alive = AllCitizens.FindAll(c => c.IsAlive);
            }

            double techSecurity = 1.0 + (PerceivedEraProgress * 1.5);
            FoodReserves = Math.Max(25.0, Math.Min(100.0, FoodReserves - (alive.Count * 1.1 * dtYears) + (alive.Count * 2.2 * techSecurity * dtYears)));

            for (int i = alive.Count - 1; i >= 0; i--)
            {
                var c = alive[i];
                c.AgeYears += dtYears;
                if (c.AgeYears >= c.MaxLifespanYears)
                {
                    c.IsAlive = false;
                    c.CauseOfDeath = "Old Age";
                    TotalDeaths++;
                    if (c.IsLeader) LogWorldEvent($"Ruler {c.FullName} died of old age", Color.FromArgb(200, 150, 140));
                    if (Graves.Count < 28)
                        Graves.Add(new GraveMarker { X = c.CurrentX, Y = 256 + (float)(rng.NextDouble() * 8), Name = c.FullName, DeathQuote = $"RIP {c.FirstName}" });
                    alive.RemoveAt(i);
                    PrunePerEntry();
                }
            }

            CheckAndReclaimSettlements(alive);
            SimulateInterSettlementPolitics(alive, dtYears);
            ElectLeaders(alive);

            int maxPopCapacity = 25 * Math.Max(1, Settlements.FindAll(s => s.IsEstablished).Count);
            var fertileMales = alive.FindAll(m => m.Stage == LifeStage.Adult && m.Gender == "M");
            var fertileFemales = alive.FindAll(f => f.Stage == LifeStage.Adult && f.Gender == "F");

            if (fertileMales.Count > 0 && fertileFemales.Count > 0 && alive.Count < maxPopCapacity)
            {
                int births = Math.Max(1, (int)Math.Round(alive.Count * 0.40 * dtYears));
                for (int b = 0; b < births; b++)
                {
                    var dad = fertileMales[rng.Next(fertileMales.Count)];
                    var mom = fertileFemales.Find(f => f.AssignedSettlementId == dad.AssignedSettlementId) ?? fertileFemales[rng.Next(fertileFemales.Count)];
                    CitizenGenome childGenome = CitizenGenome.Cross(dad.Genome, mom.Genome, rng);
                    SettlementNode home = Settlements.Find(s => s.Id == mom.AssignedSettlementId) ?? Settlements[1];
                    SpawnPerson(home.X + rng.Next(-10, 10), 0.0, rng.NextDouble() > 0.5 ? "M" : "F", dad.FullName, mom.FullName, dad.LastName, mom.AssignedSettlementId, childGenome);
                }
            }
        }

        public static SocietyOutputs Step(SceneOutputs env, double hour, double deltaSimYears)
        {
            CurrentYear += deltaSimYears;
            double targetEraNum = (CurrentYear >= 1400) ? 3.0 : (CurrentYear >= 700) ? 2.0 : (CurrentYear >= 250) ? 1.0 : 0.0;
            PerceivedEraProgress += 0.05 * (targetEraNum - PerceivedEraProgress);
            HistoricalEra currentEra = (HistoricalEra)(int)Math.Round(PerceivedEraProgress);

            double seasonTemp = (env.Season.Summer * 33.0) + (env.Season.Spring * 18.0) + (env.Season.Autumn * 12.0) + (env.Season.Winter * -15.0);
            double targetTemp = seasonTemp + ((env.Time.Day * 5.0) - (env.Time.Night * 8.0));
            PerceivedTemp += 0.08 * (targetTemp - PerceivedTemp);
            double shock = Math.Min(1.0, Math.Abs(targetTemp - PerceivedTemp) / 22.0);

            List<HumanEntity> alive = AllCitizens.FindAll(c => c.IsAlive);
            int adultWorkers = 0, babyCount = 0;
            double sumH = 0, sumI = 0, sumS = 0, sumP = 0;

            foreach (var c in alive)
            {
                if (c.Stage == LifeStage.Baby) babyCount++; else adultWorkers++;
                sumH += c.Genome.Health; sumI += c.Genome.Intelligence; sumS += c.Genome.Strength; sumP += c.Genome.Potency;
            }

            int count = Math.Max(1, alive.Count);
            double avgH = sumH / count, avgI = sumI / count, avgS = sumS / count, avgP = sumP / count;
            TechKnowledge += avgI * 0.0005 * deltaSimYears;

            double techMod = 1.0 + (PerceivedEraProgress * 1.25);
            int activeColonies = Settlements.FindAll(s => s.IsEstablished).Count;
            double mapControl = (activeColonies / (double)Settlements.Count) * 100.0;

            if (deltaSimYears > 0.000001)
            {
                double fuelBurn = ((0.04 + 0.14 * Math.Max(0, (6.0 - PerceivedTemp) / 20.0) + 0.06 * env.Time.Night) / (0.7 + avgI * 0.01)) * deltaSimYears * 100.0;
                double fuelYield = (adultWorkers * (avgS / 50.0)) * 0.045 * techMod * (1.0 - env.Rain * 0.4) * deltaSimYears * 100.0;
                CampfireFuel = Math.Max(10.0, Math.Min(100.0, CampfireFuel - fuelBurn + fuelYield));

                double foodBurn = (adultWorkers * 0.024 + babyCount * 0.010) * deltaSimYears * 100.0;
                double foodYield = (adultWorkers * (avgI / 50.0)) * (0.35 * techMod * (activeColonies * 0.6) + (env.Foliage * 0.6 + (1.0 - env.GroundSnow * 0.6) * 0.4) * techMod) * 0.05 * deltaSimYears * 100.0;
                FoodReserves = Math.Max(8.0, Math.Min(100.0, FoodReserves - foodBurn + foodYield));

                CheckAndReclaimSettlements(alive);
                SimulateInterSettlementPolitics(alive, deltaSimYears);
                ElectLeaders(alive);
                ProcessWarClashes(alive, deltaSimYears);
                ProcessLivingCycle(alive, deltaSimYears, currentEra, env.GroundSnow);
            }

            double sumMor = 0;
            foreach (var s in Settlements) sumMor += s.LocalMorality;
            GlobalMorality = sumMor / Settlements.Count;

            double flameInt = FuzzyEngine.Clamp01((CampfireFuel / 100.0) * (0.3 + 0.7 * FuzzyEngine.Trapmf(PerceivedTemp, -30, -30, -5, 5) + 0.4 * env.Time.Night));
            SettlementPriority priority = Settlements.Exists(s => s.IsAtWar) ? SettlementPriority.WarAndInvasion :
                                         (FoodReserves < 22) ? SettlementPriority.FoodAndForaging : SettlementPriority.WorkAndFuel;

            double moodScore = FuzzyEngine.Clamp01(FuzzyEngine.Trapmf(FoodReserves, 20, 50, 100, 100) * 0.5 + (GlobalMorality - 0.5) * 0.2 - shock * 0.3);

            for (int s = Smites.Count - 1; s >= 0; s--)
                if ((Smites[s].Timer -= 0.05f) <= 0) Smites.RemoveAt(s);

            UpdateEntityPositions(alive, priority, (float)env.Time.Night);

            return new SocietyOutputs
            {
                MoodPercent = moodScore,
                MoodDescription = "World Cycle",
                Priority = priority,
                Era = currentEra,
                PerceivedEraProgress = PerceivedEraProgress,
                MoralityPercent = GlobalMorality,
                CampfireFlame = flameInt,
                PerceivedTemperature = PerceivedTemp,
                ClimateShock = shock,
                Population = alive.Count,
                BabyCount = babyCount,
                FoodReserves = (int)FoodReserves,
                TotalDeaths = TotalDeaths,
                CurrentYear = CurrentYear,
                AverageHealth = avgH,
                AverageIntelligence = avgI,
                AverageStrength = avgS,
                ActiveColoniesCount = activeColonies,
                MapControlPercent = mapControl,
                LiveFeed = WorldFeed,
                SelectedSettlementId = SelectedSettlementId
            };
        }

        private static void ProcessLivingCycle(List<HumanEntity> alive, double dtYears, HistoricalEra era, double groundSnow)
        {
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                var c = alive[i];
                c.AgeYears += dtYears;
                c.MatingCooldown = Math.Max(0f, c.MatingCooldown - (float)(dtYears * 365.0));

                double vuln = (1.0 / Math.Max(0.2, (c.Genome.Health * 0.6 + c.Genome.Strength * 0.4) / 50.0)) * (era == HistoricalEra.ModernEra ? 0.35 : 0.8);
                if (c.Stage == LifeStage.Baby) vuln *= 1.4;

                bool diesCombat = c.CurrentHealth <= 0;
                bool diesAge = c.AgeYears >= c.MaxLifespanYears;
                bool diesHunger = FoodReserves <= 0.01 && rng.NextDouble() < (0.20 * vuln * dtYears * 365.0);
                bool diesCold = PerceivedTemp < 0 && CampfireFuel < 15 && rng.NextDouble() < (0.16 * vuln * dtYears * 365.0);

                if (diesCombat || diesAge || diesHunger || diesCold)
                {
                    c.IsAlive = false;
                    c.CauseOfDeath = diesCombat ? "Slain in War" : diesHunger ? "Starvation" : diesCold ? "Hypothermia" : "Old Age";
                    TotalDeaths++;
                    if (c.IsLeader) LogWorldEvent($"FALLEN: {c.FullName} ({c.CauseOfDeath})", Color.FromArgb(230, 80, 70));
                    if (Graves.Count < 28) Graves.Add(new GraveMarker { X = c.CurrentX, Y = 256 + (float)(rng.NextDouble() * 8), Name = c.FullName, DeathQuote = $"RIP {c.FirstName}" });
                    alive.RemoveAt(i);
                    PrunePerEntry();
                }
            }

            int cap = 25 * Math.Max(1, Settlements.FindAll(s => s.IsEstablished).Count);
            if (alive.Count >= 2 && FoodReserves > 22.0 && alive.Count < cap)
            {
                var males = alive.FindAll(m => m.IsAlive && m.Stage == LifeStage.Adult && m.Gender == "M" && m.MatingCooldown <= 0.1f);
                var females = alive.FindAll(f => f.IsAlive && f.Stage == LifeStage.Adult && f.Gender == "F" && f.MatingCooldown <= 0.1f);
                if (males.Count > 0 && females.Count > 0)
                {
                    var dad = males[rng.Next(males.Count)];
                    var localFemales = females.FindAll(f => f.AssignedSettlementId == dad.AssignedSettlementId);
                    var mom = (localFemales.Count > 0) ? localFemales[rng.Next(localFemales.Count)] : females[rng.Next(females.Count)];

                    dad.AssignedSettlementId = mom.AssignedSettlementId;
                    dad.TargetX = (dad.CurrentX + mom.CurrentX) / 2f - 4f;
                    mom.TargetX = dad.TargetX + 8f;

                    if (rng.NextDouble() < 0.15 * ((dad.Genome.Potency + mom.Genome.Potency) / 100.0) * dtYears * 365.0)
                    {
                        dad.MatingCooldown = 12f; mom.MatingCooldown = 18f;
                        dad.SpeechText = $"♥ {mom.FirstName}"; mom.SpeechText = $"♥ {dad.FirstName}";
                        dad.SpeechTimer = mom.SpeechTimer = 2.5f;

                        SettlementNode home = Settlements.Find(s => s.Id == mom.AssignedSettlementId) ?? Settlements[1];
                        var child = SpawnPerson(home.X + rng.Next(-6, 6), 0.0, rng.NextDouble() > 0.5 ? "M" : "F", dad.FullName, mom.FullName, dad.LastName, mom.AssignedSettlementId, CitizenGenome.Cross(dad.Genome, mom.Genome, rng));
                        FoodReserves = Math.Max(5.0, FoodReserves - 4.0);
                        if (rng.NextDouble() < 0.35) LogWorldEvent($"Birth: {child.FullName} born in {home.Name}", Color.FromArgb(140, 215, 120));
                    }
                }
            }
        }

        public static void ElectLeaders(List<HumanEntity> alive)
        {
            foreach (var node in Settlements)
            {
                if (!node.IsEstablished) { node.LeaderEntityId = -1; node.LeaderName = "None"; continue; }
                if (!alive.Exists(c => c.Id == node.LeaderEntityId && c.AssignedSettlementId == node.Id))
                {
                    var candidates = alive.FindAll(c => c.AssignedSettlementId == node.Id && c.Stage != LifeStage.Baby);
                    if (candidates.Count > 0)
                    {
                        candidates.Sort((a, b) => (b.Genome.Intelligence + b.Genome.Strength).CompareTo(a.Genome.Intelligence + a.Genome.Strength));
                        var l = candidates[0];
                        foreach (var c in candidates) c.IsLeader = false;
                        l.IsLeader = true;
                        node.LeaderEntityId = l.Id;
                        node.LeaderTitle = (node.Creed == SocietalCreed.GodFearing) ? "High Priest" : (node.Creed == SocietalCreed.Savages) ? "Warlord" : "Chieftain";
                        node.LeaderName = $"{node.LeaderTitle} {l.FullName}";
                        LogWorldEvent($"Coronation: {node.LeaderName} crowned ruler of {node.Name}", Color.FromArgb(255, 215, 80));
                    }
                    else
                    {
                        node.LeaderEntityId = -1;
                        node.LeaderName = "Unoccupied";
                    }
                }
            }
        }

        static void SimulateInterSettlementPolitics(List<HumanEntity> alive, double dtYears)
        {
            List<SettlementNode> active = Settlements.FindAll(s => s.IsEstablished && alive.Exists(c => c.AssignedSettlementId == s.Id));
            if (active.Count < 2) return;

            foreach (var node in active)
            {
                if (alive.FindAll(c => c.AssignedSettlementId == node.Id).Count > 7)
                    node.LocalMorality = Math.Max(0.05, Math.Min(0.95, node.LocalMorality + (rng.NextDouble() * 0.1 - 0.05) * dtYears));

                node.Creed = node.LocalMorality < 0.25 ? SocietalCreed.Savages : node.LocalMorality < 0.45 ? SocietalCreed.IdolWorshippers : node.LocalMorality < 0.65 ? SocietalCreed.StrugglingTribe : SocietalCreed.GodFearing;

                if (!node.IsAtWar && alive.FindAll(c => c.AssignedSettlementId == node.Id).Count >= 4)
                {
                    foreach (var other in active)
                    {
                        if (other.Id == node.Id) continue;
                        if (((node.Creed == SocietalCreed.GodFearing && other.Creed == SocietalCreed.Savages) || (node.Creed == SocietalCreed.Savages && other.Creed == SocietalCreed.GodFearing)) && rng.NextDouble() < 0.20 * dtYears)
                        {
                            node.WarTargetId = other.Id; other.WarTargetId = node.Id;
                            LogWorldEvent($"⚔ WAR: {node.Name} [{node.Creed}] declared crusade against {other.Name}!", Color.FromArgb(240, 70, 60));
                            break;
                        }
                    }
                }
            }
        }

        static void ProcessWarClashes(List<HumanEntity> alive, double dtYears)
        {
            foreach (var node in Settlements)
            {
                if (!node.IsEstablished || !node.IsAtWar) continue;
                SettlementNode target = Settlements.Find(s => s.Id == node.WarTargetId);
                if (target == null || !target.IsEstablished) { node.WarTargetId = -1; continue; }

                var attackers = alive.FindAll(c => c.AssignedSettlementId == node.Id && c.Stage == LifeStage.Adult);
                var defenders = alive.FindAll(c => c.AssignedSettlementId == target.Id && c.Stage == LifeStage.Adult);

                if (defenders.Count <= 1 || attackers.Count <= 1)
                {
                    SettlementNode win = (defenders.Count <= 1) ? node : target, lose = (defenders.Count <= 1) ? target : node;
                    lose.Creed = win.Creed; lose.LocalMorality = win.LocalMorality; lose.WarTargetId = win.WarTargetId = -1;
                    foreach (var c in alive.FindAll(x => x.AssignedSettlementId == lose.Id)) { c.SpeechText = $"We submit to {win.Creed}!"; c.SpeechTimer = 4f; }
                    LogWorldEvent($"🏆 CONQUEST: {win.Name} annexed {lose.Name}!", Color.FromArgb(255, 215, 60));
                    continue;
                }

                foreach (var att in attackers)
                {
                    att.IsArmed = true;
                    HumanEntity def = null; float minD = 999f;
                    foreach (var d in defenders) { float dist = Math.Abs(att.CurrentX - d.CurrentX); if (dist < minD) { minD = dist; def = d; } }
                    if (def != null && minD < 18f)
                    {
                        double attD = (att.Genome.Strength * 0.12 + att.Genome.Agility * 0.08) * dtYears * 100.0;
                        double defD = (def.Genome.Strength * 0.12 + def.Genome.Agility * 0.08) * dtYears * 100.0;

                        if (target.Creed == SocietalCreed.GodFearing)
                        {
                            attD *= 0.65;
                            if (rng.NextDouble() < 0.25 * dtYears * 100.0)
                            {
                                att.CurrentHealth -= 35.0; Smites.Add(new HolySmiteEffect { X = att.CurrentX, Y = 246f });
                                att.SpeechText = "Smitten by God!"; att.SpeechTimer = 2.0f;
                                LogWorldEvent($"⚡ Divine Smite struck {att.FullName}!", Color.FromArgb(255, 240, 100));
                            }
                        }
                        def.CurrentHealth -= attD; att.CurrentHealth -= defD;
                    }
                }
            }
        }

        // Detects abandoned settlements and dispatches settlers from overcrowded ones
        static void CheckAndReclaimSettlements(List<HumanEntity> alive)
        {
            // Find any settlement that is either not established OR established but has 0 living citizens
            SettlementNode emptyNode = Settlements.Find(s => !s.IsEstablished || alive.FindAll(c => c.AssignedSettlementId == s.Id).Count == 0);

            if (emptyNode != null)
            {
                // Find a populated settlement with at least 5 adults to spare
                SettlementNode sourceNode = Settlements.Find(s => s.Id != emptyNode.Id && alive.FindAll(c => c.AssignedSettlementId == s.Id && c.Stage == LifeStage.Adult).Count >= 5);

                if (sourceNode != null)
                {
                    var male = alive.Find(c => c.AssignedSettlementId == sourceNode.Id && c.Stage == LifeStage.Adult && c.Gender == "M" && !c.IsPioneer);
                    var female = alive.Find(c => c.AssignedSettlementId == sourceNode.Id && c.Stage == LifeStage.Adult && c.Gender == "F" && !c.IsPioneer);

                    if (male != null && female != null)
                    {
                        male.IsPioneer = female.IsPioneer = true;
                        male.AssignedSettlementId = female.AssignedSettlementId = emptyNode.Id;
                        emptyNode.IsEstablished = true;
                        emptyNode.WarTargetId = -1;
                        emptyNode.LocalMorality = sourceNode.LocalMorality;
                        emptyNode.Creed = sourceNode.Creed;

                        male.SpeechText = female.SpeechText = $"Settling {emptyNode.Name}!";
                        male.SpeechTimer = female.SpeechTimer = 3.5f;

                        LogWorldEvent($"Settlement: Pioneers from {sourceNode.Name} reclaimed {emptyNode.Name}!", Color.FromArgb(100, 200, 255));
                    }
                }
            }
        }

        static void UpdateEntityPositions(List<HumanEntity> citizens, SettlementPriority prio, float night)
        {
            for (int i = 0; i < citizens.Count; i++)
            {
                var c = citizens[i];
                c.WalkCycle += (c.Stage == LifeStage.Baby ? 0.25f : 0.15f);
                SettlementNode home = Settlements.Find(s => s.Id == c.AssignedSettlementId) ?? Settlements[1];

                if (c.SpeechTimer > 0) { if ((c.SpeechTimer -= 0.03f) <= 0) c.SpeechText = ""; }
                else if ((c.NextSpeechCooldown -= 0.03f) <= 0)
                {
                    c.SpeechTimer = 2.5f; c.NextSpeechCooldown = 4f + (float)(rng.NextDouble() * 7f);
                    c.SpeechText = c.IsLeader ? $"Decree of {c.FirstName}!" : home.IsAtWar ? "To War!" : "Tending land";
                }

                if (c.Stage == LifeStage.Baby)
                {
                    c.TargetX = home.X + (float)Math.Sin(c.WalkCycle * 0.4) * 8f;
                    c.CurrentX += (c.TargetX - c.CurrentX) * 0.12f;
                    continue;
                }

                if (home.IsAtWar)
                {
                    SettlementNode enemy = Settlements.Find(s => s.Id == home.WarTargetId);
                    if (enemy != null) { c.TargetX = (home.X + enemy.X) / 2f + (i % 4 - 2) * 8f; c.HasTorch = true; c.CurrentX += (c.TargetX - c.CurrentX) * 0.09f; continue; }
                }

                c.TargetX = (prio == SettlementPriority.FoodAndForaging) ? home.X + ((i % 2 == 0 ? -1f : 1f) * (28f + (i % 4) * 10f)) :
                            home.X + (float)Math.Cos(i * (2 * Math.PI / Math.Max(1, citizens.Count))) * (14f + (i % 3) * 4f);
                c.HasTorch = night > 0.4f;
                c.CurrentX += (c.TargetX - c.CurrentX) * 0.08f;
            }

            for (int i = 0; i < Graves.Count; i++)
                if (Graves[i].FadeTimer > 0) Graves[i].FadeTimer -= 0.03f;
        }
    }
}