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

			new TournamentGroup("ParametricBase-subphase2", new List<Agent>
			{
				new Agent(typeof(ParametricGreedyAgent),   "ParametricBase-best-coev-1"),
				new Agent(typeof(ParametricGreedyAgent_2), "ParametricBase-best-shade-like-2"),
				new Agent(typeof(ParametricGreedyAgent_3), "ParametricBase-best-shade-pure-3"),
			}),

			new TournamentGroup("MPA63-subphase2", new List<Agent>
			 {
			 	new Agent(typeof(ModifiedParametricGreedyAgent63),   "MPA63-best-coev-1"),
			 	new Agent(typeof(ModifiedParametricGreedyAgent63_2), "MPA63-best-shade-like-2"),
			 	new Agent(typeof(ModifiedParametricGreedyAgent63_3), "MPA63-best-shade-pure-3"),
			 }),

			new TournamentGroup("MPA63Smooth-subphase2", new List<Agent>
			{
				new Agent(typeof(ModifiedParametricGreedyAgent63Smooth),   "MPA63Smooth-best-coev-1"),
				new Agent(typeof(ModifiedParametricGreedyAgent63Smooth_2), "MPA63Smooth-best-shade-like-2"),
				new Agent(typeof(ModifiedParametricGreedyAgent63Smooth_3), "MPA63Smooth-best-shade-pure-3"),
			}),


			new TournamentGroup("MPA28-subphase2", new List<Agent>
			{
				new Agent(typeof(ModifiedParametricGreedyAgent28),   "MPA28-best-coev-1"),
				new Agent(typeof(ModifiedParametricGreedyAgent28_2), "MPA28-best-shade-like-2"),
				new Agent(typeof(ModifiedParametricGreedyAgent28_3), "MPA28-best-shade-pure-3"),
			}),

			new TournamentGroup("MPA28Norm-subphase2", new List<Agent>
			{
				new Agent(typeof(ModifiedParametricGreedyAgent28Normalaized),   "MPA28Norm-best-coev-1"),
				new Agent(typeof(ModifiedParametricGreedyAgent28Normalaized_2), "MPA28Norm-best-shade-like-2"),
				new Agent(typeof(ModifiedParametricGreedyAgent28Normalaized_3), "MPA28Norm-best-shade-pure-3"),
			}),


			new TournamentGroup("MPA21Depth-subphase2", new List<Agent>
			 {
			 	new Agent(typeof(ModifiedParametricGreedyAgent21Depth),   "MPA21Depth-best-coev-1"),
			 	new Agent(typeof(ModifiedParametricGreedyAgent21Depth_2), "MPA21Depth-best-shade-like-2"),
			 	new Agent(typeof(ModifiedParametricGreedyAgent21Depth_3), "MPA21Depth-best-shade-pure-3"),
			 }),


			//new TournamentGroup("Phase2", new List<Agent>
			// {
				//new Agent(typeof(ParametricGreedyAgent_2), "ParametricBase-best-shade-like-2"),
				//new Agent(typeof(ModifiedParametricGreedyAgent63_2), "MPA63-best-shade-like-2"),
				//new Agent(typeof(ModifiedParametricGreedyAgent63Smooth_2), "MPA63Smooth-best-shade-like-2"),
				//new Agent(typeof(ModifiedParametricGreedyAgent28_2), "MPA28-best-shade-like-2"),
				//new Agent(typeof(ModifiedParametricGreedyAgent28Normalaized_2), "MPA28Norm-best-shade-like-2"),
				//new Agent(typeof(ModifiedParametricGreedyAgent21Depth),   "MPA21Depth"),
			// }),


			//new TournamentGroup("Phase3", new List<Agent>
			// {
			//	new Agent(typeof(ModifiedParametricGreedyAgent21Depth),   "MPA21Depth"),
			//	new Agent(typeof(MyAgentSebastianMiller2), "MyAgentSebastianMiller2Naive"),
			//	new Agent(typeof(TycheAgentCompetition), "TycheAgentCompetition"),
			//	new Agent(typeof(GretiveComp), "GretiveComp"),
			// }),


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
