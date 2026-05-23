using SabberStoneCore.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using SabberStoneBasicAI.Meta;
using SabberStoneBasicAI.AIAgents;
using SabberStoneBasicAI.PartialObservation;
using SabberStoneCore.Tasks.PlayerTasks;
namespace SabberStoneCoreAi.Tyche2
{
	/// <summary> A unique node for a given PlayerTask. </summary>
    class TyTaskNode
    {	
		private TyStateAnalyzer _analyzer;

		private PlayerTask _task;
		public PlayerTask Task { get { return _task; } }

		private float _totalValue;
		public float TotalValue { get { return _totalValue; } }

		private int _visits;
		public int Visits { get { return _visits; } }

		private TySimTree _tree;

		public TyTaskNode(TySimTree tree, TyStateAnalyzer analyzer, PlayerTask task, float totalValue)
		{
			_tree = tree;
			_analyzer = analyzer;
			_task = task;
			_totalValue = totalValue;
			_visits = 0;
		}

		public void Explore(TySimResult simResult, System.Random random)
		{
			if (simResult.IsBuggy)
			{
				AddValue(simResult.value);
				return;
			}

			var game = simResult.state;
			var options = game.CurrentPlayer.Options();

			// Guard: no options means terminal state
			if (options == null || options.Count == 0)
			{
				AddValue(simResult.value);
				return;
			}

			var task = options.GetUniformRandom(random);
			var childState = TyStateUtility.GetSimulatedGame(game, task, _analyzer);

			if (childState.task.PlayerTaskType != PlayerTaskType.END_TURN)
				Explore(childState, random);
			else
				AddValue(simResult.value);
		}

		public void AddValue(float value)
		{
			_totalValue += value;
			_visits++;
		}

		public float GetAverage()
		{
			return _totalValue / _visits;
		}
	}
}
