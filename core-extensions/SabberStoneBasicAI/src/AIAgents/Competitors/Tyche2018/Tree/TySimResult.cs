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
    class TySimResult
    {	
		public POGame state;
		public PlayerTask task;
		public float value;

		public bool IsBuggy { get { return state == null; } }
		
		public TySimResult(POGame state, PlayerTask task, float value)
		{
			this.state = state;
			this.value = value;
			this.task = task;
		}
	}
}
