#region copyright
// SabberStone, Hearthstone Simulator in C# .NET Core
// Copyright (C) 2017-2019 SabberStone Team, darkfriend77 & rnilva
//
// SabberStone is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License.
// SabberStone is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
#endregion
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStoneBasicAI.Meta;
using SabberStoneBasicAI.Nodes;
using SabberStoneBasicAI.Score;
using SabberStoneBasicAI.AIAgents;
using SabberStoneBasicAI.PartialObservation;
using SabberStoneBasicAI.CompetitionEvaluation;
using System.IO;
using SabberStoneCoreAi.Tyche2;
using SabberStoneBasicAI.AIAgents.Gretive;

namespace SabberStoneBasicAI
{
	internal class Program
	{
		private static readonly Random Rnd = new Random();

		// ─── Konfiguracja turnieju ────────────────────────────────────────────────
		private const int DefaultNumRuns = 1;
		private const int GamesPerMatchup = 1000;
		private const int NumThreads = 12;
		// ─────────────────────────────────────────────────────────────────────────

		private static string FindProjectRoot()
		{
			var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
			while (dir != null)
			{
				if (dir.GetFiles("*.csproj").Length > 0)
					return dir.FullName;
				dir = dir.Parent;
			}
			return Path.GetFullPath(
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
		}

		private static readonly string ProjectRoot = FindProjectRoot();
		private static readonly string ResultsDir = Path.Combine(ProjectRoot, "src", "results");
		private static readonly string RunTimestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

		private sealed class ParsedMatchRow
		{
			public int Player1 { get; set; }
			public int Player2 { get; set; }
			public int Deck1 { get; set; }
			public int Deck2 { get; set; }
			public int WinsPlayer1 { get; set; }
			public int WinsPlayer2 { get; set; }
			public int ExceptionsPlayer1 { get; set; }
			public int ExceptionsPlayer2 { get; set; }
			public int TurnsPlayer1ToWin { get; set; }
			public int TurnsPlayer1ToLose { get; set; }
			public int HealthDiffWhenP1Wins { get; set; }
			public int HealthDiffWhenP1Loses { get; set; }
			public int GamesPlayed => WinsPlayer1 + WinsPlayer2 + ExceptionsPlayer1 + ExceptionsPlayer2;
		}

		private sealed class ParsedRunData
		{
			public string CompetitionType { get; set; } = string.Empty;
			public List<string> Agents { get; } = new List<string>();
			public List<string> Decks { get; } = new List<string>();
			public List<ParsedMatchRow> Rows { get; } = new List<ParsedMatchRow>();
		}

		private sealed class AgentLabel
		{
			public string Raw { get; set; } = string.Empty;
			public string Short { get; set; } = string.Empty;
			public string Long { get; set; } = string.Empty;
		}

		// ─── Klasa reprezentująca grupę turniejową ────────────────────────────────
		public class TournamentGroup
		{
			public string Name { get; }
			public List<Agent> Agents { get; }
			public TournamentGroup(string name, List<Agent> agents)
			{
				Name = name;
				Agents = agents;
			}
		}
		// ─────────────────────────────────────────────────────────────────────────

		// ═══════════════════════════════════════════════════════════════════════════
		// ─── GRUPY TURNIEJOWE — dodawaj / edytuj tutaj ────────────────────────────
		// ═══════════════════════════════════════════════════════════════════════════
		private static List<TournamentGroup> BuildTournamentGroups() => new List<TournamentGroup>
		{

			//new TournamentGroup("ParametricBase-subphase2", new List<Agent>
			//{
			//	new Agent(typeof(ParametricGreedyAgent),   "ParametricBase-best-coev-1"),
			//	new Agent(typeof(ParametricGreedyAgent_2), "ParametricBase-best-shade-like-2"),
			//	new Agent(typeof(ParametricGreedyAgent_3), "ParametricBase-best-shade-pure-3"),
			//}),

			//new TournamentGroup("MPA63-subphase2", new List<Agent>
			// {
			// 	new Agent(typeof(ModifiedParametricGreedyAgent63),   "MPA63-best-coev-1"),
			// 	new Agent(typeof(ModifiedParametricGreedyAgent63_2), "MPA63-best-shade-like-2"),
			// 	new Agent(typeof(ModifiedParametricGreedyAgent63_3), "MPA63-best-shade-pure-3"),
			// }),

			//new TournamentGroup("MPA63Smooth-subphase2", new List<Agent>
			//{
			//	new Agent(typeof(ModifiedParametricGreedyAgent63Smooth),   "MPA63Smooth-best-coev-1"),
			//	new Agent(typeof(ModifiedParametricGreedyAgent63Smooth_2), "MPA63Smooth-best-shade-like-2"),
			//	new Agent(typeof(ModifiedParametricGreedyAgent63Smooth_3), "MPA63Smooth-best-shade-pure-3"),
			//}),


			//new TournamentGroup("MPA28-subphase2", new List<Agent>
			//{
			//	new Agent(typeof(ModifiedParametricGreedyAgent28),   "MPA28-best-coev-1"),
			//	new Agent(typeof(ModifiedParametricGreedyAgent28_2), "MPA28-best-shade-like-2"),
			//	new Agent(typeof(ModifiedParametricGreedyAgent28_3), "MPA28-best-shade-pure-3"),
			//}),

			//new TournamentGroup("MPA28Norm-subphase2", new List<Agent>
			//{
			//	new Agent(typeof(ModifiedParametricGreedyAgent28Normalaized),   "MPA28Norm-best-coev-1"),
			//	new Agent(typeof(ModifiedParametricGreedyAgent28Normalaized_2), "MPA28Norm-best-shade-like-2"),
			//	new Agent(typeof(ModifiedParametricGreedyAgent28Normalaized_3), "MPA28Norm-best-shade-pure-3"),
			//}),


			//new TournamentGroup("MPA21Depth-subphase2", new List<Agent>
			// {
			// 	new Agent(typeof(ModifiedParametricGreedyAgent21Depth),   "MPA21Depth-best-coev-1"),
			// 	new Agent(typeof(ModifiedParametricGreedyAgent21Depth_2), "MPA21Depth-best-shade-like-2"),
			// 	new Agent(typeof(ModifiedParametricGreedyAgent21Depth_3), "MPA21Depth-best-shade-pure-3"),
			// }),


			//new TournamentGroup("Phase2", new List<Agent>
			// {
			//	new Agent(typeof(ParametricGreedyAgent_2), "ParametricBase-best-shade-like-2"),
			//	new Agent(typeof(ModifiedParametricGreedyAgent63_2), "MPA63-best-shade-like-2"),
			//	new Agent(typeof(ModifiedParametricGreedyAgent63Smooth_2), "MPA63Smooth-best-shade-like-2"),
			//	new Agent(typeof(ModifiedParametricGreedyAgent28_2), "MPA28-best-shade-like-2"),
			//	new Agent(typeof(ModifiedParametricGreedyAgent28Normalaized_2), "MPA28Norm-best-shade-like-2"),
			// 	new Agent(typeof(ModifiedParametricGreedyAgent21Depth_3), "MPA21Depth-best-shade-pure-3"),
			// }),


			//new TournamentGroup("Phase3", new List<Agent>
			// {
			//	new Agent(typeof(ModifiedParametricGreedyAgent21Depth_3),   "MPA21Depth-best-shade-pure-3"),
			//	new Agent(typeof(MyAgentSebastianMiller2), "MyAgentSebastianMiller2Naive"),
			//	// new Agent(typeof(TycheAgentCompetition), "TycheAgentCompetition"),
			//	// new Agent(typeof(GretiveComp), "GretiveComp"),
			// }),
			new TournamentGroup("Phase3-baseline", new List<Agent>
			 {
				new Agent(typeof(ParametricGreedyAgent),   "ParametricGreedyBaseline"),
				new Agent(typeof(MyAgentSebastianMiller2), "MyAgentSebastianMiller2Naive"),
				// new Agent(typeof(TycheAgentCompetition), "TycheAgentCompetition"),
				// new Agent(typeof(GretiveComp), "GretiveComp"),
			 }),

			// works - lookahead 2020
			//new TournamentGroup("Competition", new List<Agent>
			//{
			//	new Agent(typeof(ParametricGreedyAgent), "ParametricGreedyAgent"),
			//	new Agent(typeof(MyAgentSebastianMiller2), "MyAgentSebastianMiller2Naive"),


			// should be ok - MCTS tyche 2018
			//new TournamentGroup("Competition", new List<Agent>
			//{
			//	new Agent(typeof(ParametricGreedyAgent), "ParametricGreedyAgent"),
			//	new Agent(typeof(TycheAgentCompetition), "TycheAgentCompetition"),
			//}),


			// works minimax tree search - remero 2020
			//new TournamentGroup("Competition", new List<Agent>
			//{
			//	new Agent(typeof(ParametricGreedyAgent), "ParametricGreedyAgent"),
			//	new Agent(typeof(GretiveComp), "GretiveComp"),
			//}),


			// new TournamentGroup("Pure", new List<Agent>
			// {
			// 	new Agent(typeof(ParametricGreedyAgentPure051), "Pure051"),
			// 	new Agent(typeof(ParametricGreedyAgentPure052), "Pure052"),
			// 	new Agent(typeof(ParametricGreedyAgentPure101), "Pure101"),
			// 	new Agent(typeof(ParametricGreedyAgentPure102), "Pure102"),
			// 	new Agent(typeof(ParametricGreedyAgentPure151), "Pure151"),
			// 	new Agent(typeof(ParametricGreedyAgentPure152), "Pure152"),
			// }),

			// new TournamentGroup("Like", new List<Agent>
			// {
			// 	new Agent(typeof(ParametricGreedyAgentLike051), "Like051"),
			// 	new Agent(typeof(ParametricGreedyAgentLike052), "Like052"),
			// 	new Agent(typeof(ParametricGreedyAgentLike101), "Like101"),
			// 	new Agent(typeof(ParametricGreedyAgentLike102), "Like102"),
			// 	new Agent(typeof(ParametricGreedyAgentLike151), "Like151"),
			// 	new Agent(typeof(ParametricGreedyAgentLike152), "Like152"),
			// }),

			// new TournamentGroup("Misc", new List<Agent>
			// {
			// 	new Agent(typeof(ShadeLikeParametricGreedyAgent), "ShadeLike"),
			// 	new Agent(typeof(RandomAgent),                    "Random"),
			// 	new Agent(typeof(GreedyAgent),                    "Greedy"),
			// }),
		};
		// ═══════════════════════════════════════════════════════════════════════════

		private static void Main(string[] args)
		{
			Console.WriteLine("Starting test setup.");

			Directory.CreateDirectory(ResultsDir);

			if (TryRunAnalyzeMode(args))
				return;
			
			var groups = BuildTournamentGroups();
			Console.WriteLine($"Tournaments to run: {groups.Count}");

			foreach (var group in groups)
			{
				Console.WriteLine($"\n{new string('=', 60)}");
				Console.WriteLine($"TOURNAMENT GROUP: {group.Name}");
				Console.WriteLine(new string('=', 60));
				RunTournament(group, args);
			}

			Console.WriteLine("\nAll tournaments ended!");
			Console.ReadLine();
		}

		public static void RunTournament(TournamentGroup group, string[] args)
		{
			int numRuns = DefaultNumRuns;
			if (args.Length > 0 && int.TryParse(args[0], out int parsed))
				numRuns = parsed;

			List<Agent> agents = group.Agents;

			string groupSummaryFile = Path.Combine(ResultsDir,
				$"summary_{group.Name}_{RunTimestamp}.txt");

			CompetitionEvaluation.Deck[] decks =
			{
				new CompetitionEvaluation.Deck(Decks.RenoKazakusMage,    CardClass.MAGE,    "Mage"),
				new CompetitionEvaluation.Deck(Decks.AggroPirateWarrior, CardClass.WARRIOR, "Warrior"),
				new CompetitionEvaluation.Deck(Decks.MidrangeJadeShaman, CardClass.SHAMAN,  "Shaman"),
			};

			int[] totalWins = new int[agents.Count];
			int[] totalGames = new int[agents.Count];
			var runTimes = new List<long>();

			string header =
				$"Tournament: {group.Name} – {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
				$"Runs: {numRuns} | Games per matchup: {GamesPerMatchup} | Threads: {NumThreads}\n" +
				$"Agents: {string.Join(", ", agents.Select(a => a.AgentAuthor))}\n" +
				new string('=', 60) + "\n";

			File.WriteAllText(groupSummaryFile, header);
			Console.Write(header);

			for (int run = 1; run <= numRuns; run++)
			{
				string runFile = Path.Combine(ResultsDir,
					$"run_{group.Name}_r{run}_{RunTimestamp}.txt");

				string runHeader =
					$"\n{new string('─', 60)}\nRUN {run}/{numRuns}\n{new string('─', 60)}\n";
				AppendTo(groupSummaryFile, runHeader);

				var sw = Stopwatch.StartNew();

				var competition = new RoundRobinCompetition(agents.ToArray(), decks, runFile);
				competition.CreateTasks(GamesPerMatchup);
				competition.startEvaluation(NumThreads);

				sw.Stop();
				runTimes.Add(sw.ElapsedMilliseconds);

				for (int i = 0; i < agents.Count; i++)
				{
					var (wins, games) = competition.GetAgentStats(i);
					totalWins[i] += wins;
					totalGames[i] += games;
				}

				competition.PrintAgentStats();

				string runFooter =
					$"Run {run} done in {sw.ElapsedMilliseconds / 1000.0:F1}s | " +
					$"Total games: {competition.GetTotalGamesPlayed()}\n";
				AppendTo(groupSummaryFile, runFooter);

				var (detailedTxt, detailedTex, heatmapTex, dominanceTex, sensitivityTex, polarizationTex) = GenerateDetailedReportsFromRunFile(runFile);
				string detailsFooter =
					$"Detailed report (txt): {Path.GetFileName(detailedTxt)}\n" +
					$"Detailed report (tex): {Path.GetFileName(detailedTex)}\n" +
					$"Heatmap report (tex): {Path.GetFileName(heatmapTex)}\n" +
					$"Dominance graph (tex): {Path.GetFileName(dominanceTex)}\n" +
					$"Deck sensitivity (tex): {Path.GetFileName(sensitivityTex)}\n" +
					$"Polarization index (tex): {Path.GetFileName(polarizationTex)}\n";
				AppendTo(groupSummaryFile, detailsFooter);
			}

			string summary = BuildSummary(group.Name, agents, totalWins, totalGames, numRuns, runTimes);
			AppendTo(groupSummaryFile, summary);
			Console.Write(summary);
		}

		private static void AppendTo(string filePath, string message)
		{
			Console.Write(message);
			File.AppendAllText(filePath, message);
		}

		private static string BuildSummary(
			string groupName,
			List<Agent> agents,
			int[] totalWins,
			int[] totalGames,
			int numRuns,
			List<long> runTimes)
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"\n{new string('=', 60)}");
			sb.AppendLine($"AGGREGATED SUMMARY – {groupName} ({numRuns} run(s))");
			sb.AppendLine(new string('=', 60));
			sb.AppendLine($"{"Agent",-40} {"Wins",8} {"Games",8} {"Win%",8}");
			sb.AppendLine(new string('-', 60));

			for (int i = 0; i < agents.Count; i++)
			{
				double pct = totalGames[i] > 0 ? totalWins[i] * 100.0 / totalGames[i] : 0.0;
				sb.AppendLine($"{agents[i].AgentAuthor,-40} {totalWins[i],8} {totalGames[i],8} {pct,7:F2}%");
			}

			sb.AppendLine(new string('-', 60));
			sb.AppendLine($"Total wall time : {runTimes.Sum() / 1000.0:F1}s");
			sb.AppendLine($"Avg  per run    : {runTimes.Average() / 1000.0:F1}s");
			sb.AppendLine(new string('=', 60));
			return sb.ToString();
		}

		private static bool TryRunAnalyzeMode(string[] args)
		{
			if (args.Length < 2 || !string.Equals(args[0], "--analyze", StringComparison.OrdinalIgnoreCase))
				return false;

			string inputRunFile = args[1];
			if (!Path.IsPathRooted(inputRunFile))
				inputRunFile = Path.GetFullPath(Path.Combine(ProjectRoot, inputRunFile));

			var (detailedTxt, detailedTex, heatmapTex, dominanceTex, sensitivityTex, polarizationTex) = GenerateDetailedReportsFromRunFile(inputRunFile);
			Console.WriteLine("Detailed reports generated:");
			Console.WriteLine($"- {detailedTxt}");
			Console.WriteLine($"- {detailedTex}");
			Console.WriteLine($"- {heatmapTex}");
			Console.WriteLine($"- {dominanceTex}");
			Console.WriteLine($"- {sensitivityTex}");
			Console.WriteLine($"- {polarizationTex}");
			Console.ReadLine();
			return true;
		}

		private static (string detailedTxtPath, string detailedTexPath, string heatmapTexPath, string dominanceTexPath, string sensitivityTexPath, string polarizationTexPath) GenerateDetailedReportsFromRunFile(string runFilePath)
		{
			if (!File.Exists(runFilePath))
				throw new FileNotFoundException($"Run file not found: {runFilePath}");

			ParsedRunData data = ParseRunFile(runFilePath);
			string stem = Path.GetFileNameWithoutExtension(runFilePath);
			string folder = Path.GetDirectoryName(runFilePath) ?? ResultsDir;

			string detailedTxtPath = Path.Combine(folder, $"details_{stem}.txt");
			string detailedTexPath = Path.Combine(folder, $"details_{stem}.tex");
			string heatmapTexPath = Path.Combine(folder, $"heatmap_{stem}.tex");
			string dominanceTexPath = Path.Combine(folder, $"dominance_{stem}.tex");
			string sensitivityTexPath = Path.Combine(folder, $"deck_sensitivity_{stem}.tex");
			string polarizationTexPath = Path.Combine(folder, $"polarization_{stem}.tex");

			File.WriteAllText(detailedTxtPath, BuildDetailedTextReport(data, stem));
			File.WriteAllText(detailedTexPath, BuildDetailedLatexReport(data, stem));
			File.WriteAllText(heatmapTexPath, BuildHeatmapLatexReport(data, stem));
			File.WriteAllText(dominanceTexPath, BuildDominanceGraphLatexReport(data, stem));
			File.WriteAllText(sensitivityTexPath, BuildDeckSensitivityLatexReport(data, stem));
			File.WriteAllText(polarizationTexPath, BuildPolarizationLatexReport(data, stem));

			return (detailedTxtPath, detailedTexPath, heatmapTexPath, dominanceTexPath, sensitivityTexPath, polarizationTexPath);
		}

		private static ParsedRunData ParseRunFile(string runFilePath)
		{
			var data = new ParsedRunData();
			var latestRows = new Dictionary<(int p1, int p2, int d1, int d2), ParsedMatchRow>();
			string section = string.Empty;
			bool firstLine = true;

			foreach (string rawLine in File.ReadLines(runFilePath))
			{
				string line = rawLine.Trim();
				if (firstLine)
				{
					data.CompetitionType = line;
					firstLine = false;
					continue;
				}

				if (line.Length == 0)
					continue;

				if (line == "Agents")
				{
					section = "Agents";
					continue;
				}
				if (line == "Decks")
				{
					section = "Decks";
					continue;
				}
				if (line == "Match Results")
				{
					section = "Match Results";
					continue;
				}

				if (section == "Agents")
				{
					data.Agents.Add(line);
					continue;
				}

				if (section == "Decks")
				{
					data.Decks.Add(line);
					continue;
				}

				if (section == "Match Results" && line.StartsWith("Match Result:", StringComparison.Ordinal))
				{
					string payload = line.Substring("Match Result:".Length).Trim();
					string[] tokens = payload.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					if (tokens.Length < 12)
						continue;

					int[] v = tokens.Select(int.Parse).ToArray();
					var row = new ParsedMatchRow
					{
						Player1 = v[0],
						Player2 = v[1],
						Deck1 = v[2],
						Deck2 = v[3],
						WinsPlayer1 = v[4],
						WinsPlayer2 = v[5],
						ExceptionsPlayer1 = v[6],
						ExceptionsPlayer2 = v[7],
						TurnsPlayer1ToWin = v[8],
						TurnsPlayer1ToLose = v[9],
						HealthDiffWhenP1Wins = v[10],
						HealthDiffWhenP1Loses = v[11],
					};

					// For interrupted runs the same key appears multiple times.
					// Last seen value is the current state of that matchup key.
					latestRows[(row.Player1, row.Player2, row.Deck1, row.Deck2)] = row;
				}
			}

			foreach (var row in latestRows.Values)
				data.Rows.Add(row);

			return data;
		}

		private static string BuildDetailedTextReport(ParsedRunData data, string reportName)
		{
			var labels = BuildAgentLabels(data.Agents);
			var map = data.Rows.ToDictionary(
				r => (r.Player1, r.Player2, r.Deck1, r.Deck2),
				r => r);

			ParsedMatchRow GetRow(int p1, int p2, int d1, int d2)
			{
				return map.TryGetValue((p1, p2, d1, d2), out var row)
					? row
					: new ParsedMatchRow();
			}

			(int winsA, int winsB, int games) AggregateDeckCell(int a, int b, int da, int db)
			{
				var forward = GetRow(a, b, da, db);
				var reverse = GetRow(b, a, db, da);

				int winsA = forward.WinsPlayer1 + reverse.WinsPlayer2;
				int winsB = forward.WinsPlayer2 + reverse.WinsPlayer1;
				int games = forward.GamesPlayed + reverse.GamesPlayed;
				return (winsA, winsB, games);
			}

			(int winsA, int winsB, int games) AggregatePair(int a, int b)
			{
				int winsA = 0, winsB = 0, games = 0;
				for (int da = 0; da < data.Decks.Count; da++)
				{
					for (int db = 0; db < data.Decks.Count; db++)
					{
						var cell = AggregateDeckCell(a, b, da, db);
						winsA += cell.winsA;
						winsB += cell.winsB;
						games += cell.games;
					}
				}
				return (winsA, winsB, games);
			}

			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"Detailed report: {reportName}");
			sb.AppendLine($"Competition type: {data.CompetitionType}");
			sb.AppendLine($"Agents: {data.Agents.Count} | Decks: {data.Decks.Count}");
			sb.AppendLine(new string('=', 90));
			sb.AppendLine();
			sb.AppendLine("AGENT LABELS (SHORT -> FULL THESIS NAME)");
			sb.AppendLine(new string('-', 90));
			for (int i = 0; i < labels.Count; i++)
			{
				sb.AppendLine($"{labels[i].Short,-24} -> {labels[i].Long}");
			}
			sb.AppendLine();

			sb.AppendLine("PAIRWISE (ALL DECKS AGGREGATED)");
			sb.AppendLine(new string('-', 90));
			sb.AppendLine($"{ "Agent A",-40} {"Agent B",-40} {"WinsA",6} {"WinsB",6} {"Games",6} {"WR_A",7}");
			sb.AppendLine(new string('-', 90));
			for (int a = 0; a < data.Agents.Count; a++)
			{
				for (int b = a + 1; b < data.Agents.Count; b++)
				{
					var pair = AggregatePair(a, b);
					double wrA = pair.games > 0 ? pair.winsA * 100.0 / pair.games : 0.0;
					sb.AppendLine($"{labels[a].Short,-40} {labels[b].Short,-40} {pair.winsA,6} {pair.winsB,6} {pair.games,6} {wrA,6:F2}%");
				}
			}
			sb.AppendLine();

			sb.AppendLine("DECK x DECK BREAKDOWN FOR EACH AGENT PAIR");
			sb.AppendLine(new string('-', 90));
			for (int a = 0; a < data.Agents.Count; a++)
			{
				for (int b = a + 1; b < data.Agents.Count; b++)
				{
					sb.AppendLine($"{labels[a].Short}  VS  {labels[b].Short}");
					sb.AppendLine($"{ "Deck A",-18} {"Deck B",-18} {"WinsA",6} {"WinsB",6} {"Games",6} {"WR_A",7}");
					for (int da = 0; da < data.Decks.Count; da++)
					{
						for (int db = 0; db < data.Decks.Count; db++)
						{
							var cell = AggregateDeckCell(a, b, da, db);
							double wrA = cell.games > 0 ? cell.winsA * 100.0 / cell.games : 0.0;
							sb.AppendLine($"{data.Decks[da],-18} {data.Decks[db],-18} {cell.winsA,6} {cell.winsB,6} {cell.games,6} {wrA,6:F2}%");
						}
					}
					sb.AppendLine(new string('-', 90));
				}
			}

			return sb.ToString();
		}

		private static Dictionary<(int a, int b), (int winsA, int winsB, int games)> BuildPairAggregateMap(ParsedRunData data)
		{
			var map = BuildRawMatchMap(data);

			var pairStats = new Dictionary<(int a, int b), (int winsA, int winsB, int games)>();
			for (int a = 0; a < data.Agents.Count; a++)
			{
				for (int b = a + 1; b < data.Agents.Count; b++)
				{
					int winsA = 0, winsB = 0, games = 0;
					for (int da = 0; da < data.Decks.Count; da++)
					{
						for (int db = 0; db < data.Decks.Count; db++)
						{
							var (winsDeckA, winsDeckB, gamesDeck) = AggregateDeckCellFromMap(map, a, b, da, db);

							winsA += winsDeckA;
							winsB += winsDeckB;
							games += gamesDeck;
						}
					}

					pairStats[(a, b)] = (winsA, winsB, games);
				}
			}

			return pairStats;
		}

		private static Dictionary<(int p1, int p2, int d1, int d2), ParsedMatchRow> BuildRawMatchMap(ParsedRunData data)
		{
			return data.Rows.ToDictionary(
				r => (r.Player1, r.Player2, r.Deck1, r.Deck2),
				r => r);
		}

		private static (int winsA, int winsB, int games) AggregateDeckCellFromMap(
			Dictionary<(int p1, int p2, int d1, int d2), ParsedMatchRow> map,
			int a,
			int b,
			int deckA,
			int deckB)
		{
			ParsedMatchRow forward = map.TryGetValue((a, b, deckA, deckB), out var fw) ? fw : new ParsedMatchRow();
			ParsedMatchRow reverse = map.TryGetValue((b, a, deckB, deckA), out var rv) ? rv : new ParsedMatchRow();

			int winsA = forward.WinsPlayer1 + reverse.WinsPlayer2;
			int winsB = forward.WinsPlayer2 + reverse.WinsPlayer1;
			int games = forward.GamesPlayed + reverse.GamesPlayed;
			return (winsA, winsB, games);
		}

		private static Dictionary<(int a, int b, int deckA, int deckB), (int winsA, int winsB, int games)> BuildDeckCellAggregateMap(ParsedRunData data)
		{
			var map = BuildRawMatchMap(data);
			var deckCellStats = new Dictionary<(int a, int b, int deckA, int deckB), (int winsA, int winsB, int games)>();

			for (int a = 0; a < data.Agents.Count; a++)
			{
				for (int b = a + 1; b < data.Agents.Count; b++)
				{
					for (int deckA = 0; deckA < data.Decks.Count; deckA++)
					{
						for (int deckB = 0; deckB < data.Decks.Count; deckB++)
						{
							deckCellStats[(a, b, deckA, deckB)] = AggregateDeckCellFromMap(map, a, b, deckA, deckB);
						}
					}
				}
			}

			return deckCellStats;
		}

		private static List<AgentLabel> BuildAgentLabels(IReadOnlyList<string> rawAgents)
		{
			var labels = new List<AgentLabel>();
			foreach (string raw in rawAgents)
				labels.Add(ResolveAgentLabel(raw));
			return labels;
		}

		private static AgentLabel ResolveAgentLabel(string raw)
		{
			string n = raw.ToLowerInvariant();

			if (n.Contains("mpa28norm"))
			{
				return new AgentLabel
				{
					Raw = raw,
					Short = "28 znormalizowany",
					Long = "Wariant 28 parametrow z normalizacja cech"
				};
			}

			if (n.Contains("mpa28"))
			{
				return new AgentLabel
				{
					Raw = raw,
					Short = "28 parametrow",
					Long = "Wariant 28 parametrow - rozszerzony zestaw cech"
				};
			}

			if (n.Contains("mpa63smooth"))
			{
				return new AgentLabel
				{
					Raw = raw,
					Short = "63 plynny",
					Long = "Wariant 63 parametrow z plynnym przejsciem"
				};
			}

			if (n.Contains("mpa63"))
			{
				return new AgentLabel
				{
					Raw = raw,
					Short = "63 parametry",
					Long = "Wariant 63 parametrow - trojfazowy agent"
				};
			}

			if (n.Contains("mpa21depth"))
			{
				return new AgentLabel
				{
					Raw = raw,
					Short = "21 glebokosciowy",
					Long = "Wariant 21 parametrow z przeszukiwaniem glebokosciowym"
				};
			}

			if (n.Contains("parametricbase"))
			{
				return new AgentLabel
				{
					Raw = raw,
					Short = "bazowy 21",
					Long = "Wariant bazowy 21 parametrow"
				};
			}

			return new AgentLabel
			{
				Raw = raw,
				Short = raw,
				Long = raw
			};
		}

		private static string EscapeLatex(string text)
		{
			return text
				.Replace(@"\", @"\textbackslash{}")
				.Replace("&", @"\&")
				.Replace("%", @"\%")
				.Replace("$", @"\$")
				.Replace("#", @"\#")
				.Replace("_", @"\_")
				.Replace("{", @"\{")
				.Replace("}", @"\}")
				.Replace("~", @"\textasciitilde{}")
				.Replace("^", @"\textasciicircum{}");
		}

		private static string BuildDetailedLatexReport(ParsedRunData data, string reportName)
		{
			var labels = BuildAgentLabels(data.Agents);
			var map = data.Rows.ToDictionary(
				r => (r.Player1, r.Player2, r.Deck1, r.Deck2),
				r => r);

			ParsedMatchRow GetRow(int p1, int p2, int d1, int d2)
			{
				return map.TryGetValue((p1, p2, d1, d2), out var row)
					? row
					: new ParsedMatchRow();
			}

			(int winsA, int winsB, int games) AggregateDeckCell(int a, int b, int da, int db)
			{
				var forward = GetRow(a, b, da, db);
				var reverse = GetRow(b, a, db, da);

				int winsA = forward.WinsPlayer1 + reverse.WinsPlayer2;
				int winsB = forward.WinsPlayer2 + reverse.WinsPlayer1;
				int games = forward.GamesPlayed + reverse.GamesPlayed;
				return (winsA, winsB, games);
			}

			(int winsA, int winsB, int games) AggregatePair(int a, int b)
			{
				int winsA = 0, winsB = 0, games = 0;
				for (int da = 0; da < data.Decks.Count; da++)
				{
					for (int db = 0; db < data.Decks.Count; db++)
					{
						var cell = AggregateDeckCell(a, b, da, db);
						winsA += cell.winsA;
						winsB += cell.winsB;
						games += cell.games;
					}
				}
				return (winsA, winsB, games);
			}

			var sb = new System.Text.StringBuilder();
			sb.AppendLine("% Requires packages: booktabs, longtable");
			sb.AppendLine($"% Auto-generated detailed tournament report: {EscapeLatex(reportName)}");
			sb.AppendLine();
			sb.AppendLine(@"\begin{table}[ht]");
			sb.AppendLine(@"\centering");
			sb.AppendLine(@"\small");
			sb.AppendLine(@"\begin{tabular}{llrrr}");
			sb.AppendLine(@"\toprule");
			sb.AppendLine(@"Agent A & Agent B & Wins A & Wins B & WR A (\%) \\");
			sb.AppendLine(@"\midrule");
			for (int a = 0; a < data.Agents.Count; a++)
			{
				for (int b = a + 1; b < data.Agents.Count; b++)
				{
					var pair = AggregatePair(a, b);
					double wrA = pair.games > 0 ? pair.winsA * 100.0 / pair.games : 0.0;
					sb.AppendLine($"{EscapeLatex(labels[a].Short)} & {EscapeLatex(labels[b].Short)} & {pair.winsA} & {pair.winsB} & {wrA:F2} \\\\");
				}
			}
			sb.AppendLine(@"\bottomrule");
			sb.AppendLine(@"\end{tabular}");
			sb.AppendLine($@"\caption{{Pairwise results for {EscapeLatex(reportName)} (all decks aggregated).}}");
			sb.AppendLine(@"\end{table}");
			sb.AppendLine();
			sb.AppendLine(@"\small");
			sb.AppendLine(@"\setlength{\LTleft}{0pt}");
			sb.AppendLine(@"\setlength{\LTright}{0pt}");
			sb.AppendLine(@"\begin{longtable}{llllrrr}");
			sb.AppendLine(@"\caption{Deck-level matchup breakdown (seat-normalized).}\\");
			sb.AppendLine(@"\toprule");
			sb.AppendLine(@"Agent A & Agent B & Deck A & Deck B & Wins A & Wins B & WR A (\%) \\");
			sb.AppendLine(@"\midrule");
			sb.AppendLine(@"\endfirsthead");
			sb.AppendLine(@"\toprule");
			sb.AppendLine(@"Agent A & Agent B & Deck A & Deck B & Wins A & Wins B & WR A (\%) \\");
			sb.AppendLine(@"\midrule");
			sb.AppendLine(@"\endhead");
			sb.AppendLine(@"\bottomrule");
			sb.AppendLine(@"\endfoot");
			sb.AppendLine(@"\bottomrule");
			sb.AppendLine(@"\endlastfoot");

			for (int a = 0; a < data.Agents.Count; a++)
			{
				for (int b = a + 1; b < data.Agents.Count; b++)
				{
					for (int da = 0; da < data.Decks.Count; da++)
					{
						for (int db = 0; db < data.Decks.Count; db++)
						{
							var cell = AggregateDeckCell(a, b, da, db);
							double wrA = cell.games > 0 ? cell.winsA * 100.0 / cell.games : 0.0;
							sb.AppendLine(
								$"{EscapeLatex(labels[a].Short)} & {EscapeLatex(labels[b].Short)} & " +
								$"{EscapeLatex(data.Decks[da])} & {EscapeLatex(data.Decks[db])} & " +
								$"{cell.winsA} & {cell.winsB} & {wrA:F2}\\\\");
						}
					}
				}
			}
			sb.AppendLine(@"\end{longtable}");
			sb.AppendLine();
			sb.AppendLine(@"\begin{table}[ht]");
			sb.AppendLine(@"\centering");
			sb.AppendLine(@"\footnotesize");
			sb.AppendLine(@"\begin{tabular}{ll}");
			sb.AppendLine(@"\toprule");
			sb.AppendLine(@"Skrot & Pelna nazwa wariantu \\");
			sb.AppendLine(@"\midrule");
			for (int i = 0; i < labels.Count; i++)
			{
				sb.AppendLine($@"{EscapeLatex(labels[i].Short)} & {EscapeLatex(labels[i].Long)} \\");
			}
			sb.AppendLine(@"\bottomrule");
			sb.AppendLine(@"\end{tabular}");
			sb.AppendLine(@"\caption{Mapowanie skrotow agentow na nazwy wariantow z pracy.}");
			sb.AppendLine(@"\end{table}");
			return sb.ToString();
		}

		private static string BuildHeatmapLatexReport(ParsedRunData data, string reportName)
		{
			var labels = BuildAgentLabels(data.Agents);
			var pairStats = BuildPairAggregateMap(data);
			int n = data.Agents.Count;
			double[,] wr = new double[n, n];

			for (int i = 0; i < n; i++)
			{
				wr[i, i] = 50.0;
			}

			foreach (var kv in pairStats)
			{
				int a = kv.Key.a;
				int b = kv.Key.b;
				var stats = kv.Value;
				double wrA = stats.games > 0 ? stats.winsA * 100.0 / stats.games : 0.0;
				double wrB = stats.games > 0 ? stats.winsB * 100.0 / stats.games : 0.0;
				wr[a, b] = wrA;
				wr[b, a] = wrB;
			}

			var sb = new System.Text.StringBuilder();
			sb.AppendLine("% Requires packages: xcolor[table], colortbl, booktabs, graphicx");
			sb.AppendLine($"% Auto-generated heatmap report: {EscapeLatex(reportName)}");
			sb.AppendLine(@"\definecolor{wrLow}{RGB}{178,24,43}");
			sb.AppendLine(@"\definecolor{wrMid}{RGB}{247,247,247}");
			sb.AppendLine(@"\definecolor{wrHigh}{RGB}{33,102,172}");
			sb.AppendLine();
			sb.AppendLine(@"\begin{table}[ht]");
			sb.AppendLine(@"\centering");
			sb.AppendLine(@"\scriptsize");
			sb.AppendLine(@"\setlength{\tabcolsep}{3pt}");
			sb.AppendLine($@"\caption{{Agent-vs-agent win-rate heatmap for {EscapeLatex(reportName)} (rows = first agent, columns = opponent).}}");
			sb.Append(@"\begin{tabular}{l");
			for (int i = 0; i < n; i++)
				sb.Append("r");
			sb.AppendLine("}");
			sb.AppendLine(@"\toprule");
			sb.Append("Agent");
			for (int col = 0; col < n; col++)
			{
				sb.Append(" & ");
				sb.Append($@"\rotatebox{{60}}{{{EscapeLatex(labels[col].Short)}}}");
			}
			sb.AppendLine(@" \\");
			sb.AppendLine(@"\midrule");

			for (int row = 0; row < n; row++)
			{
				sb.Append(EscapeLatex(labels[row].Short));
				for (int col = 0; col < n; col++)
				{
					double value = wr[row, col];
					sb.Append(" & ");
					if (row == col)
					{
						sb.Append(@"\cellcolor{black!8}--");
					}
					else if (value >= 50.0)
					{
						int strength = Math.Min(100, (int)Math.Round((value - 50.0) * 4.0)); // 50->0, 75->100
						sb.Append($@"\cellcolor{{wrHigh!{strength}}}{value:F1}");
					}
					else
					{
						int strength = Math.Min(100, (int)Math.Round((50.0 - value) * 4.0)); // 50->0, 25->100
						sb.Append($@"\cellcolor{{wrLow!{strength}}}{value:F1}");
					}
				}
				sb.AppendLine(@" \\");
			}

			sb.AppendLine(@"\bottomrule");
			sb.AppendLine(@"\end{tabular}");
			sb.AppendLine(@"\end{table}");
			sb.AppendLine();
			sb.AppendLine("% Optional legend:");
			sb.AppendLine("% red = below 50% (bad matchup), blue = above 50% (good matchup), stronger color = stronger deviation.");
			return sb.ToString();
		}

		private static string BuildDominanceGraphLatexReport(ParsedRunData data, string reportName)
		{
			var labels = BuildAgentLabels(data.Agents);
			var pairStats = BuildPairAggregateMap(data);
			int n = data.Agents.Count;
			const double radius = 4.2;

			var sb = new System.Text.StringBuilder();
			sb.AppendLine("% Requires packages: tikz");
			sb.AppendLine("% Recommended tikz libs: arrows.meta, positioning");
			sb.AppendLine($"% Auto-generated dominance graph: {EscapeLatex(reportName)}");
			sb.AppendLine(@"\begin{figure}[ht]");
			sb.AppendLine(@"\centering");
			sb.AppendLine(@"\begin{tikzpicture}[>=Stealth, every node/.style={font=\scriptsize}]");

			for (int i = 0; i < n; i++)
			{
				double angle = (2.0 * Math.PI * i / Math.Max(1, n)) - Math.PI / 2.0;
				double x = radius * Math.Cos(angle);
				double y = radius * Math.Sin(angle);
				sb.AppendLine($@"\node[draw, circle, minimum size=10mm, fill=white] (A{i}) at ({x:F3},{y:F3}) {{{EscapeLatex(labels[i].Short)}}};");
			}

			foreach (var kv in pairStats)
			{
				int a = kv.Key.a;
				int b = kv.Key.b;
				var stats = kv.Value;
				if (stats.games <= 0)
					continue;

				double wrA = stats.winsA * 100.0 / stats.games;
				double wrB = stats.winsB * 100.0 / stats.games;
				double marginA = wrA - 50.0;
				double marginB = wrB - 50.0;

				if (marginA > 0.0)
				{
					double lineW = 0.6 + (Math.Min(50.0, marginA) / 50.0) * 2.6;
					int colorStrength = 35 + (int)Math.Round(Math.Min(50.0, marginA) / 50.0 * 65.0);
					sb.AppendLine(
						$@"\draw[->, line width={lineW:F2}pt, draw=blue!{colorStrength}] " +
						$@"(A{a}) to[bend left=12] node[midway, fill=white, inner sep=1pt] {{{wrA:F1}\%}} (A{b});");
				}

				if (marginB > 0.0)
				{
					double lineW = 0.6 + (Math.Min(50.0, marginB) / 50.0) * 2.6;
					int colorStrength = 35 + (int)Math.Round(Math.Min(50.0, marginB) / 50.0 * 65.0);
					sb.AppendLine(
						$@"\draw[->, line width={lineW:F2}pt, draw=blue!{colorStrength}] " +
						$@"(A{b}) to[bend left=12] node[midway, fill=white, inner sep=1pt] {{{wrB:F1}\%}} (A{a});");
				}
			}

			sb.AppendLine(@"\end{tikzpicture}");
			sb.AppendLine($@"\caption{{Directed dominance graph for {EscapeLatex(reportName)}. Edge A $\to$ B exists when WR(A,B) $>$ 50\%, and edge width scales with WR-50\%.}}");
			sb.AppendLine(@"\end{figure}");
			return sb.ToString();
		}

		private static string BuildDeckSensitivityLatexReport(ParsedRunData data, string reportName)
		{
			var labels = BuildAgentLabels(data.Agents);
			var deckCellStats = BuildDeckCellAggregateMap(data);
			int nAgents = data.Agents.Count;
			int nDecks = data.Decks.Count;
			var rows = new List<(int idx, string name, double min, double mean, double max, double std, double range)>();

			for (int a = 0; a < nAgents; a++)
			{
				var wrByOwnDeck = new List<double>();
				for (int ownDeck = 0; ownDeck < nDecks; ownDeck++)
				{
					int wins = 0;
					int games = 0;

					for (int other = 0; other < nAgents; other++)
					{
						if (other == a) continue;

						if (a < other)
						{
							for (int oppDeck = 0; oppDeck < nDecks; oppDeck++)
							{
								var cell = deckCellStats[(a, other, ownDeck, oppDeck)];
								wins += cell.winsA;
								games += cell.games;
							}
						}
						else
						{
							for (int oppDeck = 0; oppDeck < nDecks; oppDeck++)
							{
								var cell = deckCellStats[(other, a, oppDeck, ownDeck)];
								wins += cell.winsB;
								games += cell.games;
							}
						}
					}

					double wr = games > 0 ? wins * 100.0 / games : 0.0;
					wrByOwnDeck.Add(wr);
				}

				double mean = wrByOwnDeck.Average();
				double min = wrByOwnDeck.Min();
				double max = wrByOwnDeck.Max();
				double variance = wrByOwnDeck.Select(v => (v - mean) * (v - mean)).Average();
				double std = Math.Sqrt(variance);
				rows.Add((a, labels[a].Short, min, mean, max, std, max - min));
			}

			var sb = new System.Text.StringBuilder();
			sb.AppendLine("% Requires packages: tikz, booktabs");
			sb.AppendLine($"% Auto-generated deck sensitivity profile: {EscapeLatex(reportName)}");
			sb.AppendLine(@"\begin{table}[ht]");
			sb.AppendLine(@"\centering");
			sb.AppendLine(@"\small");
			sb.AppendLine(@"\begin{tabular}{lrrrrr}");
			sb.AppendLine(@"\toprule");
			sb.AppendLine(@"Agent & Min WR (\%) & Mean WR (\%) & Max WR (\%) & StdDev & Range \\");
			sb.AppendLine(@"\midrule");
			foreach (var row in rows.OrderByDescending(r => r.range))
			{
				sb.AppendLine($"{EscapeLatex(row.name)} & {row.min:F2} & {row.mean:F2} & {row.max:F2} & {row.std:F2} & {row.range:F2}\\\\");
			}
			sb.AppendLine(@"\bottomrule");
			sb.AppendLine(@"\end{tabular}");
			sb.AppendLine($@"\caption{{Deck sensitivity summary for {EscapeLatex(reportName)}. Larger range/std means stronger deck dependence (meta-fragility).}}");
			sb.AppendLine(@"\end{table}");
			sb.AppendLine();
			sb.AppendLine(@"\begin{figure}[ht]");
			sb.AppendLine(@"\centering");
			sb.AppendLine(@"\begin{tikzpicture}[x=0.12cm,y=0.9cm]");
			sb.AppendLine(@"\draw[->] (0,0) -- (102,0) node[right] {Win rate (\%)};");
			sb.AppendLine(@"\foreach \x in {0,10,...,100} \draw (\x,0.08) -- (\x,-0.08) node[below, font=\tiny] {\x};");

			int y = 1;
			foreach (var row in rows.OrderByDescending(r => r.mean))
			{
				sb.AppendLine($@"\draw[thick, gray!70] ({row.min:F2},{y}) -- ({row.max:F2},{y});");
				sb.AppendLine($@"\filldraw[blue!70] ({row.mean:F2},{y}) circle (1.7pt);");
				sb.AppendLine($@"\node[anchor=east, font=\tiny] at (-1,{y}) {{{EscapeLatex(row.name)}}};");
				y++;
			}

			sb.AppendLine(@"\end{tikzpicture}");
			sb.AppendLine($@"\caption{{Deck-sensitivity interval profile for {EscapeLatex(reportName)}. Gray segment = [min,max] WR across own deck choices, blue dot = mean.}}");
			sb.AppendLine(@"\end{figure}");
			return sb.ToString();
		}

		private static string BuildPolarizationLatexReport(ParsedRunData data, string reportName)
		{
			var labels = BuildAgentLabels(data.Agents);
			var pairStats = BuildPairAggregateMap(data);
			var deckCellStats = BuildDeckCellAggregateMap(data);
			var rows = new List<(int a, int b, double wr, double bias, double deckStd, double index)>();

			for (int a = 0; a < data.Agents.Count; a++)
			{
				for (int b = a + 1; b < data.Agents.Count; b++)
				{
					var pair = pairStats[(a, b)];
					double wr = pair.games > 0 ? pair.winsA * 100.0 / pair.games : 0.0;
					double bias = Math.Abs(wr - 50.0);

					var deckValues = new List<double>();
					for (int da = 0; da < data.Decks.Count; da++)
					{
						for (int db = 0; db < data.Decks.Count; db++)
						{
							var cell = deckCellStats[(a, b, da, db)];
							double wrDeck = cell.games > 0 ? cell.winsA * 100.0 / cell.games : 0.0;
							deckValues.Add(wrDeck);
						}
					}

					double meanDeck = deckValues.Average();
					double varianceDeck = deckValues.Select(v => (v - meanDeck) * (v - meanDeck)).Average();
					double deckStd = Math.Sqrt(varianceDeck);
					double index = bias + deckStd;

					rows.Add((a, b, wr, bias, deckStd, index));
				}
			}

			var ranked = rows.OrderByDescending(r => r.index).ToList();
			double maxIdx = Math.Max(1e-9, ranked.Max(r => r.index));

			var sb = new System.Text.StringBuilder();
			sb.AppendLine("% Requires packages: booktabs, tikz");
			sb.AppendLine($"% Auto-generated polarization index report: {EscapeLatex(reportName)}");
			sb.AppendLine(@"\begin{table}[ht]");
			sb.AppendLine(@"\centering");
			sb.AppendLine(@"\small");
			sb.AppendLine(@"\begin{tabular}{llrrrr}");
			sb.AppendLine(@"\toprule");
			sb.AppendLine(@"Agent A & Agent B & WR(A,B) (\%) & |WR-50| & DeckStd & Polarization \\");
			sb.AppendLine(@"\midrule");
			foreach (var row in ranked)
			{
				sb.AppendLine(
					$"{EscapeLatex(labels[row.a].Short)} & {EscapeLatex(labels[row.b].Short)} & " +
					$"{row.wr:F2} & {row.bias:F2} & {row.deckStd:F2} & {row.index:F2}\\\\");
			}
			sb.AppendLine(@"\bottomrule");
			sb.AppendLine(@"\end{tabular}");
			sb.AppendLine($@"\caption{{Polarization index for {EscapeLatex(reportName)}: $\mathrm{{PI}}(A,B)=|WR(A,B)-50|+\sigma_{{deck}}(A,B)$. Higher = stronger and/or more deck-dependent matchup.}}");
			sb.AppendLine(@"\end{table}");
			sb.AppendLine();
			sb.AppendLine(@"\begin{figure}[ht]");
			sb.AppendLine(@"\centering");
			sb.AppendLine(@"\begin{tikzpicture}[x=0.18cm,y=0.55cm]");
			sb.AppendLine(@"\draw[->] (0,0) -- (102,0) node[right] {Normalized polarization index};");

			int y = 1;
			foreach (var row in ranked)
			{
				double bar = (row.index / maxIdx) * 100.0;
				string pairLabel = $"{EscapeLatex(labels[row.a].Short)} vs {EscapeLatex(labels[row.b].Short)}";
				sb.AppendLine($@"\filldraw[fill=purple!55, draw=purple!80] (0,{y}-0.25) rectangle ({bar:F2},{y}+0.25);");
				sb.AppendLine($@"\node[anchor=east, font=\tiny] at (-1,{y}) {{{pairLabel}}};");
				sb.AppendLine($@"\node[anchor=west, font=\tiny] at ({bar:F2}+0.5,{y}) {{{row.index:F2}}};");
				y++;
			}

			sb.AppendLine(@"\end{tikzpicture}");
			sb.AppendLine($@"\caption{{Polarization ranking for {EscapeLatex(reportName)} (bars normalized to the maximum PI in this run).}}");
			sb.AppendLine(@"\end{figure}");
			return sb.ToString();
		}

		// ─── Metody pomocnicze (bez zmian) ────────────────────────────────────────

		public static void TestPOGame()
		{
			Console.WriteLine("Setup gameConfig");

			var gameConfig = new GameConfig()
			{
				StartPlayer = 1,
				Player1HeroClass = CardClass.MAGE,
				Player2HeroClass = CardClass.MAGE,
				Player1Deck = Decks.RenoKazakusMage,
				Player2Deck = Decks.RenoKazakusMage,
				FillDecks = false,
				Shuffle = true,
				Logging = false
			};

			Console.WriteLine("Setup POGameHandler");
			AbstractAgent player1 = new GreedyAgent();
			AbstractAgent player2 = new GreedyAgent();
			var gameHandler = new POGameHandler(gameConfig, player1, player2, repeatDraws: false);

			Console.WriteLine("Simulate Games");
			gameHandler.PlayGames(nr_of_games: 1000, addResultToGameStats: true, debug: false);
			GameStats gameStats = gameHandler.getGameStats();

			gameStats.printResults();

			Console.WriteLine("Test successful");
			Console.ReadLine();
		}

		public static void RandomGames()
		{
			int total = 1;
			var watch = Stopwatch.StartNew();

			var gameConfig = new GameConfig()
			{
				StartPlayer = -1,
				Player1Name = "FitzVonGerald",
				Player1HeroClass = CardClass.PALADIN,
				Player1Deck = new List<Card>()
				{
					Cards.FromName("Blessing of Might"),
					Cards.FromName("Blessing of Might"),
					Cards.FromName("Gnomish Inventor"),
					Cards.FromName("Gnomish Inventor"),
					Cards.FromName("Goldshire Footman"),
					Cards.FromName("Goldshire Footman"),
					Cards.FromName("Hammer of Wrath"),
					Cards.FromName("Hammer of Wrath"),
					Cards.FromName("Hand of Protection"),
					Cards.FromName("Hand of Protection"),
					Cards.FromName("Holy Light"),
					Cards.FromName("Holy Light"),
					Cards.FromName("Ironforge Rifleman"),
					Cards.FromName("Ironforge Rifleman"),
					Cards.FromName("Light's Justice"),
					Cards.FromName("Light's Justice"),
					Cards.FromName("Lord of the Arena"),
					Cards.FromName("Lord of the Arena"),
					Cards.FromName("Nightblade"),
					Cards.FromName("Nightblade"),
					Cards.FromName("Raid Leader"),
					Cards.FromName("Raid Leader"),
					Cards.FromName("Stonetusk Boar"),
					Cards.FromName("Stonetusk Boar"),
					Cards.FromName("Stormpike Commando"),
					Cards.FromName("Stormpike Commando"),
					Cards.FromName("Stormwind Champion"),
					Cards.FromName("Stormwind Champion"),
					Cards.FromName("Stormwind Knight"),
					Cards.FromName("Stormwind Knight"),
				},
				Player2Name = "RehHausZuckFuchs",
				Player2HeroClass = CardClass.PALADIN,
				Player2Deck = new List<Card>()
				{
					Cards.FromName("Blessing of Might"),
					Cards.FromName("Blessing of Might"),
					Cards.FromName("Gnomish Inventor"),
					Cards.FromName("Gnomish Inventor"),
					Cards.FromName("Goldshire Footman"),
					Cards.FromName("Goldshire Footman"),
					Cards.FromName("Hammer of Wrath"),
					Cards.FromName("Hammer of Wrath"),
					Cards.FromName("Hand of Protection"),
					Cards.FromName("Hand of Protection"),
					Cards.FromName("Holy Light"),
					Cards.FromName("Holy Light"),
					Cards.FromName("Ironforge Rifleman"),
					Cards.FromName("Ironforge Rifleman"),
					Cards.FromName("Light's Justice"),
					Cards.FromName("Light's Justice"),
					Cards.FromName("Lord of the Arena"),
					Cards.FromName("Lord of the Arena"),
					Cards.FromName("Nightblade"),
					Cards.FromName("Nightblade"),
					Cards.FromName("Raid Leader"),
					Cards.FromName("Raid Leader"),
					Cards.FromName("Stonetusk Boar"),
					Cards.FromName("Stonetusk Boar"),
					Cards.FromName("Stormpike Commando"),
					Cards.FromName("Stormpike Commando"),
					Cards.FromName("Stormwind Champion"),
					Cards.FromName("Stormwind Champion"),
					Cards.FromName("Stormwind Knight"),
					Cards.FromName("Stormwind Knight"),
				},
				FillDecks = false,
				Shuffle = true,
				SkipMulligan = false,
				Logging = true,
				History = true
			};

			int turns = 0;
			int[] wins = new[] { 0, 0 };
			for (int i = 0; i < total; i++)
			{
				var game = new Game(gameConfig);
				game.StartGame();

				game.Process(ChooseTask.Mulligan(game.Player1, new List<int>()));
				game.Process(ChooseTask.Mulligan(game.Player2, new List<int>()));

				game.MainReady();

				while (game.State != State.COMPLETE)
				{
					List<PlayerTask> options = game.CurrentPlayer.Options();
					PlayerTask option = options[Rnd.Next(options.Count)];
					game.Process(option);
				}

				turns += game.Turn;
				if (game.Player1.PlayState == PlayState.WON) wins[0]++;
				if (game.Player2.PlayState == PlayState.WON) wins[1]++;

				Console.WriteLine("game ended");

				using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"powerhistory.log"))
					file.WriteLine(game.PowerHistory.Print());

				using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"logger.log"))
					foreach (LogEntry log in game.Logs)
						file.WriteLine(log.ToString());
			}

			watch.Stop();
			Console.WriteLine($"{total} games with {turns} turns took {watch.ElapsedMilliseconds} ms => " +
							  $"Avg. {watch.ElapsedMilliseconds / total} per game " +
							  $"and {watch.ElapsedMilliseconds / (total * turns)} per turn!");
			Console.WriteLine($"playerA {wins[0] * 100 / total}% vs. playerB {wins[1] * 100 / total}%!");
		}

		public static void OneTurn()
		{
			var game = new Game(
				new GameConfig()
				{
					StartPlayer = 1,
					Player1Name = "FitzVonGerald",
					Player1HeroClass = CardClass.WARRIOR,
					Player1Deck = Decks.AggroPirateWarrior,
					Player2Name = "RehHausZuckFuchs",
					Player2HeroClass = CardClass.SHAMAN,
					Player2Deck = Decks.MidrangeJadeShaman,
					FillDecks = false,
					Shuffle = false,
					SkipMulligan = false
				});
			game.Player1.BaseMana = 10;
			game.StartGame();

			var aiPlayer1 = new AggroScore();
			var aiPlayer2 = new AggroScore();

			game.Process(ChooseTask.Mulligan(game.Player1,
				aiPlayer1.MulliganRule().Invoke(
					game.Player1.Choice.Choices.Select(p => game.IdEntityDic[p]).ToList())));
			game.Process(ChooseTask.Mulligan(game.Player2,
				aiPlayer2.MulliganRule().Invoke(
					game.Player2.Choice.Choices.Select(p => game.IdEntityDic[p]).ToList())));

			game.MainReady();

			while (game.CurrentPlayer == game.Player1)
			{
				Console.WriteLine($"* Calculating solutions *** Player 1 ***");
				List<OptionNode> solutions = OptionNode.GetSolutions(game, game.Player1.Id, aiPlayer1, 10, 500);
				var solution = new List<PlayerTask>();
				solutions.OrderByDescending(p => p.Score).First().PlayerTasks(ref solution);
				Console.WriteLine($"- Player 1 - <{game.CurrentPlayer.Name}> ---------------------------");
				foreach (PlayerTask task in solution)
				{
					Console.WriteLine(task.FullPrint());
					game.Process(task);
					if (game.CurrentPlayer.Choice != null) break;
				}
			}

			Console.WriteLine(game.Player1.HandZone.FullPrint());
			Console.WriteLine(game.Player1.BoardZone.FullPrint());
		}

		public static void FullGame()
		{
			var game = new Game(
				new GameConfig()
				{
					StartPlayer = 1,
					Player1Name = "FitzVonGerald",
					Player1HeroClass = CardClass.WARRIOR,
					Player1Deck = Decks.AggroPirateWarrior,
					Player2Name = "RehHausZuckFuchs",
					Player2HeroClass = CardClass.WARRIOR,
					Player2Deck = Decks.AggroPirateWarrior,
					FillDecks = false,
					Shuffle = true,
					SkipMulligan = false,
					History = false
				});
			game.StartGame();

			var aiPlayer1 = new AggroScore();
			var aiPlayer2 = new AggroScore();

			List<int> mulligan1 = aiPlayer1.MulliganRule().Invoke(
				game.Player1.Choice.Choices.Select(p => game.IdEntityDic[p]).ToList());
			List<int> mulligan2 = aiPlayer2.MulliganRule().Invoke(
				game.Player2.Choice.Choices.Select(p => game.IdEntityDic[p]).ToList());

			Console.WriteLine($"Player1: Mulligan {String.Join(",", mulligan1)}");
			Console.WriteLine($"Player2: Mulligan {String.Join(",", mulligan2)}");

			game.Process(ChooseTask.Mulligan(game.Player1, mulligan1));
			game.Process(ChooseTask.Mulligan(game.Player2, mulligan2));

			game.MainReady();

			while (game.State != State.COMPLETE)
			{
				Console.WriteLine("");
				Console.WriteLine($"Player1: {game.Player1.PlayState} / Player2: {game.Player2.PlayState} - " +
								  $"ROUND {(game.Turn + 1) / 2} - {game.CurrentPlayer.Name}");
				Console.WriteLine($"Hero[P1]: {game.Player1.Hero.Health} / Hero[P2]: {game.Player2.Hero.Health}");
				Console.WriteLine("");

				while (game.State == State.RUNNING && game.CurrentPlayer == game.Player1)
				{
					Console.WriteLine($"* Calculating solutions *** Player 1 ***");
					List<OptionNode> solutions = OptionNode.GetSolutions(game, game.Player1.Id, aiPlayer1, 10, 500);
					var solution = new List<PlayerTask>();
					solutions.OrderByDescending(p => p.Score).First().PlayerTasks(ref solution);
					Console.WriteLine($"- Player 1 - <{game.CurrentPlayer.Name}> ---------------------------");
					foreach (PlayerTask task in solution)
					{
						Console.WriteLine(task.FullPrint());
						game.Process(task);
						if (game.CurrentPlayer.Choice != null)
						{
							Console.WriteLine($"* Recaclulating due to a final solution ...");
							break;
						}
					}
				}

				Console.WriteLine($"- Player 2 - <{game.CurrentPlayer.Name}> ---------------------------");
				while (game.State == State.RUNNING && game.CurrentPlayer == game.Player2)
				{
					Console.WriteLine($"* Calculating solutions *** Player 2 ***");
					List<OptionNode> solutions = OptionNode.GetSolutions(game, game.Player2.Id, aiPlayer2, 10, 500);
					var solution = new List<PlayerTask>();
					solutions.OrderByDescending(p => p.Score).First().PlayerTasks(ref solution);
					Console.WriteLine($"- Player 2 - <{game.CurrentPlayer.Name}> ---------------------------");
					foreach (PlayerTask task in solution)
					{
						Console.WriteLine(task.FullPrint());
						game.Process(task);
						if (game.CurrentPlayer.Choice != null)
						{
							Console.WriteLine($"* Recaclulating due to a final solution ...");
							break;
						}
					}
				}
			}
			Console.WriteLine($"Game: {game.State}, Player1: {game.Player1.PlayState} / Player2: {game.Player2.PlayState}");
		}

		public static void TestFullGames()
		{
			int maxGames = 100;
			int maxDepth = 10;
			int maxWidth = 14;
			int[] player1Stats = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			int[] player2Stats = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

			var gameConfig = new GameConfig()
			{
				StartPlayer = -1,
				Player1Name = "FitzVonGerald",
				Player1HeroClass = CardClass.PALADIN,
				Player1Deck = new List<Card>()
				{
					Cards.FromName("Blessing of Might"),
					Cards.FromName("Blessing of Might"),
					Cards.FromName("Gnomish Inventor"),
					Cards.FromName("Gnomish Inventor"),
					Cards.FromName("Goldshire Footman"),
					Cards.FromName("Goldshire Footman"),
					Cards.FromName("Hammer of Wrath"),
					Cards.FromName("Hammer of Wrath"),
					Cards.FromName("Hand of Protection"),
					Cards.FromName("Hand of Protection"),
					Cards.FromName("Holy Light"),
					Cards.FromName("Holy Light"),
					Cards.FromName("Ironforge Rifleman"),
					Cards.FromName("Ironforge Rifleman"),
					Cards.FromName("Light's Justice"),
					Cards.FromName("Light's Justice"),
					Cards.FromName("Lord of the Arena"),
					Cards.FromName("Lord of the Arena"),
					Cards.FromName("Nightblade"),
					Cards.FromName("Nightblade"),
					Cards.FromName("Raid Leader"),
					Cards.FromName("Raid Leader"),
					Cards.FromName("Stonetusk Boar"),
					Cards.FromName("Stonetusk Boar"),
					Cards.FromName("Stormpike Commando"),
					Cards.FromName("Stormpike Commando"),
					Cards.FromName("Stormwind Champion"),
					Cards.FromName("Stormwind Champion"),
					Cards.FromName("Stormwind Knight"),
					Cards.FromName("Stormwind Knight"),
				},
				Player2Name = "RehHausZuckFuchs",
				Player2HeroClass = CardClass.PALADIN,
				Player2Deck = new List<Card>()
				{
					Cards.FromName("Blessing of Might"),
					Cards.FromName("Blessing of Might"),
					Cards.FromName("Gnomish Inventor"),
					Cards.FromName("Gnomish Inventor"),
					Cards.FromName("Goldshire Footman"),
					Cards.FromName("Goldshire Footman"),
					Cards.FromName("Hammer of Wrath"),
					Cards.FromName("Hammer of Wrath"),
					Cards.FromName("Hand of Protection"),
					Cards.FromName("Hand of Protection"),
					Cards.FromName("Holy Light"),
					Cards.FromName("Holy Light"),
					Cards.FromName("Ironforge Rifleman"),
					Cards.FromName("Ironforge Rifleman"),
					Cards.FromName("Light's Justice"),
					Cards.FromName("Light's Justice"),
					Cards.FromName("Lord of the Arena"),
					Cards.FromName("Lord of the Arena"),
					Cards.FromName("Nightblade"),
					Cards.FromName("Nightblade"),
					Cards.FromName("Raid Leader"),
					Cards.FromName("Raid Leader"),
					Cards.FromName("Stonetusk Boar"),
					Cards.FromName("Stonetusk Boar"),
					Cards.FromName("Stormpike Commando"),
					Cards.FromName("Stormpike Commando"),
					Cards.FromName("Stormwind Champion"),
					Cards.FromName("Stormwind Champion"),
					Cards.FromName("Stormwind Knight"),
					Cards.FromName("Stormwind Knight"),
				},
				FillDecks = false,
				Shuffle = true,
				SkipMulligan = false,
				Logging = false,
				History = false
			};

			for (int i = 0; i < maxGames; i++)
			{
				var game = new Game(gameConfig);
				game.StartGame();

				var aiPlayer1 = new AggroScore();
				var aiPlayer2 = new AggroScore();

				List<int> mulligan1 = aiPlayer1.MulliganRule().Invoke(
					game.Player1.Choice.Choices.Select(p => game.IdEntityDic[p]).ToList());
				List<int> mulligan2 = aiPlayer2.MulliganRule().Invoke(
					game.Player2.Choice.Choices.Select(p => game.IdEntityDic[p]).ToList());

				game.Process(ChooseTask.Mulligan(game.Player1, mulligan1));
				game.Process(ChooseTask.Mulligan(game.Player2, mulligan2));

				game.MainReady();

				while (game.State != State.COMPLETE)
				{
					while (game.State == State.RUNNING && game.CurrentPlayer == game.Player1)
					{
						List<OptionNode> solutions = OptionNode.GetSolutions(game, game.Player1.Id, aiPlayer1, maxDepth, maxWidth);
						var solution = new List<PlayerTask>();
						solutions.OrderByDescending(p => p.Score).First().PlayerTasks(ref solution);
						foreach (PlayerTask task in solution)
						{
							game.Process(task);
							if (game.CurrentPlayer.Choice != null) break;
						}
					}
					while (game.State == State.RUNNING && game.CurrentPlayer == game.Player2)
					{
						List<OptionNode> solutions = OptionNode.GetSolutions(game, game.Player2.Id, aiPlayer2, maxDepth, maxWidth);
						var solution = new List<PlayerTask>();
						solutions.OrderByDescending(p => p.Score).First().PlayerTasks(ref solution);
						foreach (PlayerTask task in solution)
						{
							game.Process(task);
							if (game.CurrentPlayer.Choice != null) break;
						}
					}
				}

				player1Stats[(int)game.Player1.PlayState]++;
				player2Stats[(int)game.Player2.PlayState]++;

				Console.WriteLine($"{i}.Game: {game.State}, Player1: {game.Player1.PlayState} / Player2: {game.Player2.PlayState}");
			}

			Console.WriteLine($"Player1: {String.Join(",", player1Stats)}");
			Console.WriteLine($"Player2: {String.Join(",", player2Stats)}");
		}
	}
}
