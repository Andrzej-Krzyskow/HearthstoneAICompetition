/*
 * Copyright (c) 2020, Alejandro Romero.
 * All rights reserved.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or(at your option) any later version.
 *
 * Contributors:
 * Antonio M. Mora García
 * Dept. Signal Theory, Telematics and Communications.
 * University of Granada
 *
 *
 *		GretiveComp.cs
 *
 *		It contains the definition and specification of the agent.
 *		It implements a tree search (MCTS) based on the implementation of the previous champion Tom Heimbrodt.
 *		The weighting system (located and managed by the GretiveDispatcher class) comes from an evolutionary optimization process,
 *		for which a subset of the possible factors to be evaluated have been used.
 *		
 *
 */


using SabberStoneBasicAI.PartialObservation;
using SabberStoneBasicAI.Score;
using SabberStoneCore.Enums;
using SabberStoneCore.Tasks.PlayerTasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SabberStoneBasicAI.AIAgents.Gretive
{
	class GretiveComp : AbstractAgent
	{

		private GretiveScore _score;

		private bool _initialized=false;
		private SabberStoneCore.Model.Entities.Controller _player;

		public GretiveComp() { }
		public GretiveComp(CardClass hero, Profile profile = Profile.DEFAULT_BY_HERO) { InitByHero(hero, profile); }

		public override void FinalizeAgent() { }
		public override void FinalizeGame(){}
		public override void InitializeAgent() { }
		public override void InitializeGame() { }

		public override PlayerTask GetMove(POGame poGame)
		{
			_player = poGame.CurrentPlayer;
			if (!_initialized) InitByHero(_player.HeroClass);


			// Implement a simple Mulligan Rule (in this case, midrange by default)
			if (_player.MulliganState == Mulligan.INPUT)
			{
				List<int> mulligan = new MidRangeScore().MulliganRule().Invoke(_player.Choice.Choices.Select(p => poGame.getGame().IdEntityDic[p]).ToList());
				return ChooseTask.Mulligan(_player, mulligan);
			}


			var options = poGame.Simulate(_player.Options()).Where(x => x.Value != null);

			int count = options.Count();

			
			if (!options.Any())
				return _player.Options().First(x => x.PlayerTaskType == PlayerTaskType.END_TURN);
	

			int depth = count > 32 ? 1 : (count < 12  || (poGame.Turn > 12 && count < 22) ? 3 : 2); 

			return options.Select(x => TreeSearch(x, depth)).OrderBy(x => x.Value).Last().Key;

		}


		private void InitByHero(CardClass heroClass, Profile profile = Profile.DEFAULT_BY_HERO)
		{
			_score = GretiveDispatcher.Score(heroClass, profile);
			_initialized = true;
		}

		private int Rate(POGame game)
		{
			_score.Controller = game.CurrentPlayer.PlayerId == _player.PlayerId ? game.CurrentPlayer : game.CurrentOpponent;
			return _score.Rate();
		}

		KeyValuePair<PlayerTask, int> TreeSearch(KeyValuePair<PlayerTask, POGame> gameState, int depth = 2)
		{
			
			if (depth == 0 || gameState.Value.CurrentPlayer.PlayerId != _player.PlayerId)
				return new KeyValuePair<PlayerTask, int>(gameState.Key, Rate(gameState.Value));

			int bestValue = Int32.MinValue;
			var options = gameState.Value.Simulate(gameState.Value.CurrentPlayer.Options()).Where(x => x.Value != null);

			foreach (var option in options)
				bestValue = Math.Max(bestValue, TreeSearch(option, depth - 1).Value);

			return new KeyValuePair<PlayerTask, int>(gameState.Key, bestValue);
		}

	}

}
